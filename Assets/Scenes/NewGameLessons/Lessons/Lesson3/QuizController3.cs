using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the quiz portion of a single instrument: asking Question 1
/// (loud/soft), Question 2 (high/low pitch), validating Braille answers,
/// tracking mistakes/support, scoring, and reporting the final result.
///
/// All visual/UI display has been removed; everything is communicated through
/// audio clips and Braille chord input.
///
/// Communication with LessonController3:
///   - LessonController3 calls BeginQuestions(lesson) to start asking
///     questions about a given instrument.
///   - This script raises OnLessonQuestionsComplete once both questions for
///     that instrument have been answered correctly.
///   - LessonController3 calls FinalizeQuiz() once there are no more
///     instruments left: completed audio, score + high score, repeat question.
///   - At the repeat question the learner decides:
///        Repeat -> LessonController3 calls ConfirmRepeatChoice() (play again)
///        Next   -> LessonController3 calls ConfirmFinishChoice() (main menu)
///        Back   -> handled here: same as Next, with an optional cue first
/// </summary>
public class QuizController3 : MonoBehaviour
{
    public enum VolumeAnswer { Loud, Soft }
    public enum PitchAnswer { High, Low }

    [Serializable]
    public class VolumeQuestion
    {
        [TextArea(2, 4)]
        public string promptMessage = "Is the sound it produced loud or soft? Press dot 1 for Loud or dot 2 for Soft.";
        public AudioClip promptAudio;

        public VolumeAnswer correctAnswer = VolumeAnswer.Loud;

        [TextArea(2, 4)] public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)] public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)] public string supportMessage;
        public AudioClip supportAudio;
    }

    [Serializable]
    public class PitchQuestion
    {
        [TextArea(2, 4)]
        public string promptMessage = "Does the instrument produce a high-pitch sound or a low-pitch sound? Press dot 1 for High Pitch or dot 2 for Low Pitch.";
        public AudioClip promptAudio;

        public PitchAnswer correctAnswer = PitchAnswer.High;

        [TextArea(2, 4)] public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)] public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)] public string supportMessage;
        public AudioClip supportAudio;
    }

    private enum QuestionStage { None, Question1, Question2 }

    // ---------------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------------

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Audio")]
    public AudioSource voiceAudioSource; // narrator voice - can be the same AudioSource as LessonController3's
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    [Tooltip("Played AFTER the score and high score: 'Press Repeat to play again or Next to finish.'")]
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Scene Text (kept for logging / future captioning use)")]
    [TextArea(2, 5)] public string completedMessage = "Great job! You finished the lesson.";
    [TextArea(2, 5)]
    public string repeatQuestionMessage =
        "You finished the lesson. Do you want to repeat again? Press Repeat to play again or Next to finish.";

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "InstrumentSoundsHighScore";

    [Header("Back To Main Menu")]
    [Tooltip("Only used if no QuizResultReporter is assigned. Must be added in File > Build Settings.")]
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Optional cue, e.g. 'Going back to the main menu.' Played only when Back is pressed.")]
    public AudioClip backToMenuAudio;

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // ---------------------------------------------------------------
    // Public events / read-only state for LessonController3
    // ---------------------------------------------------------------

    /// <summary>Raised once both questions for the current instrument are answered correctly.</summary>
    public event Action OnLessonQuestionsComplete;

    /// <summary>Raised when the learner chooses to repeat the whole quiz from the final score prompt.</summary>
    public event Action OnQuizRepeatRequested;

    /// <summary>True while a question is actively in progress for the current instrument.</summary>
    public bool LessonActive { get; private set; }

    /// <summary>
    /// True from the moment the repeat question starts playing, while we're waiting
    /// for the learner to choose Repeat (play again), Next (finish) or Back (menu).
    /// </summary>
    public bool WaitingForRepeatChoice { get; private set; }

    public int TotalScore => totalScore;
    public int HighScore => highScore;
    public int TotalWrongCount => totalWrongCount;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private LessonController3.InstrumentLesson currentLesson;
    private QuestionStage currentStage = QuestionStage.None;
    private bool waitingForChoiceAnswer = false;
    private bool returningToMenu = false;   // guards against Next/Back/Repeat firing twice
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private Coroutine flowRoutine;

    // ---------------------------------------------------------------
    // Unity events
    // ---------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnBack += HandleBack;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnBack -= HandleBack;
    }

    // ---------------------------------------------------------------
    // Score
    // ---------------------------------------------------------------

    public void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    private void AddMistake()
    {
        totalWrongCount++;
        int deductions = totalWrongCount / 3;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));
    }

    private void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    // ---------------------------------------------------------------
    // Coroutine helpers
    // ---------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        StopFlow();
        flowRoutine = StartCoroutine(routine);
    }

    private void StopFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }
    }

    private void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    private IEnumerator PlayClipAndWait(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    // ---------------------------------------------------------------
    // Entry point called by LessonController3
    // ---------------------------------------------------------------

    public void BeginQuestions(LessonController3.InstrumentLesson lesson)
    {
        currentLesson = lesson;
        currentMistakeCount = 0;
        currentStage = QuestionStage.None;
        waitingForChoiceAnswer = false;
        LessonActive = true;

        if (logDebug)
            Debug.Log($"QuizController3 beginning questions for: {lesson.displayLabel}");

        RunFlow(AskQuestion1());
    }

    // ---------------------------------------------------------------
    // Question 1 - Loud or Soft
    // ---------------------------------------------------------------

    private IEnumerator AskQuestion1()
    {
        currentStage = QuestionStage.Question1;
        waitingForChoiceAnswer = true;

        yield return PlayClipAndWait(currentLesson.volumeQuestion.promptAudio, noAudioTextDelay);
    }

    private void HandleQuestion1Answer(string pattern)
    {
        VolumeAnswer? selected = MapDotToVolumeAnswer(pattern);
        if (selected == null) return; // Unrecognized pattern: keep waiting.

        waitingForChoiceAnswer = false;

        if (selected.Value == currentLesson.volumeQuestion.correctAnswer)
        {
            currentMistakeCount = 0;
            RunFlow(HandleQuestion1Correct());
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleQuestion1Support());
            else
                RunFlow(HandleQuestion1Wrong());
        }
    }

    private IEnumerator HandleQuestion1Correct()
    {
        AudioClip clip = currentLesson.volumeQuestion.successAudio != null
            ? currentLesson.volumeQuestion.successAudio
            : genericCorrectAudio;

        yield return PlayClipAndWait(clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentMistakeCount = 0;
        yield return AskQuestion2();
    }

    private IEnumerator HandleQuestion1Wrong()
    {
        AudioClip clip = currentLesson.volumeQuestion.wrongAudio != null
            ? currentLesson.volumeQuestion.wrongAudio
            : genericTryAgainAudio;

        yield return PlayClipAndWait(clip, noAudioTextDelay);
        yield return AskQuestion1();
    }

    private IEnumerator HandleQuestion1Support()
    {
        yield return PlayClipAndWait(currentLesson.volumeQuestion.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuestion1();
    }

    // ---------------------------------------------------------------
    // Question 2 - High Pitch or Low Pitch
    // ---------------------------------------------------------------

    private IEnumerator AskQuestion2()
    {
        currentStage = QuestionStage.Question2;
        waitingForChoiceAnswer = true;

        yield return PlayClipAndWait(currentLesson.pitchQuestion.promptAudio, noAudioTextDelay);
    }

    private void HandleQuestion2Answer(string pattern)
    {
        PitchAnswer? selected = MapDotToPitchAnswer(pattern);
        if (selected == null) return; // Unrecognized pattern: keep waiting.

        waitingForChoiceAnswer = false;

        if (selected.Value == currentLesson.pitchQuestion.correctAnswer)
        {
            currentMistakeCount = 0;
            LessonActive = false;
            RunFlow(HandleQuestion2Correct());
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleQuestion2Support());
            else
                RunFlow(HandleQuestion2Wrong());
        }
    }

    private IEnumerator HandleQuestion2Correct()
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = currentLesson.pitchQuestion.successAudio != null
            ? currentLesson.pitchQuestion.successAudio
            : genericCorrectAudio;

        yield return PlayClipAndWait(clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        // Lesson complete for this instrument -> tell LessonController3 to advance.
        OnLessonQuestionsComplete?.Invoke();
    }

    private IEnumerator HandleQuestion2Wrong()
    {
        AudioClip clip = currentLesson.pitchQuestion.wrongAudio != null
            ? currentLesson.pitchQuestion.wrongAudio
            : genericTryAgainAudio;

        yield return PlayClipAndWait(clip, noAudioTextDelay);
        yield return AskQuestion2();
    }

    private IEnumerator HandleQuestion2Support()
    {
        yield return PlayClipAndWait(currentLesson.pitchQuestion.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuestion2();
    }

    // ---------------------------------------------------------------
    // Braille dot -> answer mapping
    //
    // Dot 1 = "100000" (first choice: Loud / High Pitch)
    // Dot 2 = "010000" (second choice: Soft / Low Pitch)
    // ---------------------------------------------------------------

    private VolumeAnswer? MapDotToVolumeAnswer(string pattern)
    {
        if (pattern == "100000") return VolumeAnswer.Loud;
        if (pattern == "010000") return VolumeAnswer.Soft;
        return null;
    }

    private PitchAnswer? MapDotToPitchAnswer(string pattern)
    {
        if (pattern == "100000") return PitchAnswer.High;
        if (pattern == "010000") return PitchAnswer.Low;
        return null;
    }

    // ---------------------------------------------------------------
    // Input handling
    // ---------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!waitingForChoiceAnswer)
            return;

        switch (currentStage)
        {
            case QuestionStage.Question1:
                HandleQuestion1Answer(submittedPattern);
                break;
            case QuestionStage.Question2:
                HandleQuestion2Answer(submittedPattern);
                break;
        }
    }

    private void HandleBack()
    {
        // Back only works at the very end of the quiz (after the score is announced).
        if (!WaitingForRepeatChoice || returningToMenu)
            return;

        returningToMenu = true;
        WaitingForRepeatChoice = false;

        RunFlow(BackToMenuFlow());
    }

    // ---------------------------------------------------------------
    // Scene completion
    // ---------------------------------------------------------------

    /// <summary>Called by LessonController3 once there are no more instruments left.</summary>
    public void FinalizeQuiz()
    {
        LessonActive = false;
        returningToMenu = false;
        SaveHighScoreIfNeeded();

        RunFlow(FinalizeSceneCompletion());
    }

    private IEnumerator FinalizeSceneCompletion()
    {
        // 1. "Great job! You finished the lesson."
        yield return PlayClipAndWait(genericCompletedAudio, noAudioTextDelay);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // 3. "Do you want to repeat?" The flag is set right before the question so
        //    the learner can answer (Repeat / Next / Back) even while it is playing.
        WaitingForRepeatChoice = true;
        yield return PlayClipAndWait(repeatQuestionAudio, noAudioTextDelay);

        // LessonController3 calls ConfirmRepeatChoice() or ConfirmFinishChoice() in
        // response to the learner's input; Back is handled in HandleBack().
        while (WaitingForRepeatChoice)
            yield return null;
    }

    /// <summary>Called by LessonController3 when the learner presses Repeat at the final score prompt.</summary>
    public void ConfirmRepeatChoice()
    {
        if (!WaitingForRepeatChoice || returningToMenu) return;

        WaitingForRepeatChoice = false;
        StopFlow();
        StopVoice();

        ResetQuizScore();
        OnQuizRepeatRequested?.Invoke();
    }

    /// <summary>Called by LessonController3 when the learner presses Next/Yes at the final score prompt.</summary>
    public void ConfirmFinishChoice()
    {
        if (!WaitingForRepeatChoice || returningToMenu) return;

        returningToMenu = true;
        WaitingForRepeatChoice = false;
        StopFlow();
        StopVoice();

        ReturnToMenu();
    }

    private IEnumerator BackToMenuFlow()
    {
        // RunFlow already stopped the repeat-question coroutine; cut off its audio too.
        StopVoice();

        if (backToMenuAudio != null)
            yield return PlayClipAndWait(backToMenuAudio, 0f);

        ReturnToMenu();
    }

    /// <summary>Saves the high score, then reports the score and returns to the menu.</summary>
    private void ReturnToMenu()
    {
        SaveHighScoreIfNeeded();

        if (resultReporter != null)
        {
            resultReporter.ReportScoreAndReturn(totalScore);   // saves score + returns to GameMenu
        }
        else
        {
            Debug.LogWarning("[QuizController3] No QuizResultReporter assigned - loading menu directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        if (yourScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = yourScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(yourScoreIsAudio.length);
        }

        if (finalScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = finalScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(finalScoreClip.length);
        }

        if (whileYourHighestScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = whileYourHighestScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(whileYourHighestScoreIsAudio.length);
        }

        if (highScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = highScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(highScoreClip.length);
        }
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }
}