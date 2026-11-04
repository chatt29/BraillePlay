using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Handles the quiz portion of the scene: asking each multiple choice
/// question, judging Braille chord answers, tracking score, and reporting
/// the result back to the game menu. Audio-only - there is no visual UI.
///
/// LessonController calls StartQuiz(0) once the lesson pages are done;
/// from that point this script owns the rest of the flow.
///
/// Flow (same as QuizController1, but with A / B / C choices):
///   1. For each question: play the prompt, then the A / B / C choices.
///   2. Judge the Braille chord answer (Dot 1 = A, Dot 2 = B, Dot 3 = C).
///   3. Correct -> success audio, advance to the next question.
///      Wrong -> try-again audio, re-ask the same question.
///      3 wrong in a row -> support audio, re-ask the same question.
///   4. After the last question: completed audio, score + high score, THEN the
///      repeat-question audio. The learner decides:
///        Repeat -> reset the score and play the quiz again
///        Next   -> report the score and return to the main menu
///        Back   -> same as Next, with an optional confirmation cue first
/// </summary>
public class QuizController : MonoBehaviour
{
    public enum AnswerChoice { A, B, C }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;

        [TextArea(2, 4)]
        public string categoryLabel = "BRAILLE";

        [Header("Messages (authoring notes only)")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Multiple Choice")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Tooltip("Animal sound played after the spoken letter for choice A.")]
        public AudioClip choiceAAudio;

        [Tooltip("Animal sound played after the spoken letter for choice B.")]
        public AudioClip choiceBAudio;

        [Tooltip("Animal sound played after the spoken letter for choice C.")]
        public AudioClip choiceCAudio;

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    [Tooltip("Played AFTER the score and high score: 'Press Repeat to play again or Next to finish.'")]
    public AudioClip repeatQuestionAudio;

    [Header("Answer Choice Letter Audio")]
    [Tooltip("Spoken 'A', played before choice A's animal sound.")]
    public AudioClip letterAAudio;
    [Tooltip("Spoken 'B', played before choice B's animal sound.")]
    public AudioClip letterBAudio;
    [Tooltip("Spoken 'C', played before choice C's animal sound.")]
    public AudioClip letterCAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Scene Text (authoring notes only)")]
    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press Repeat to play again or Next to finish.";

    [Header("Back To Main Menu")]
    [Tooltip("Only used if no QuizResultReporter is assigned. Must be added in File > Build Settings.")]
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Optional cue, e.g. 'Going back to the main menu.' Played only when Back is pressed.")]
    public AudioClip backToMenuAudio;

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    [FormerlySerializedAs("noAudioTextDelay")]
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;   // end-of-quiz: Repeat, Next or Back
    private bool waitingForChoiceAnswer = false;
    private bool returningToMenu = false;          // guards against Next/Back/Repeat firing twice

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnBack += HandleBack;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnBack -= HandleBack;
    }

    // -------------------------------------------------------------------------
    // Entry Point (called by LessonController)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by LessonController once the lesson pages are finished.
    /// Resets the score and begins the quiz at the given lesson index.
    /// </summary>
    public void StartQuiz(int startIndex)
    {
        if (logDebug)
            Debug.Log("QuizController starting quiz.");

        returningToMenu = false;
        waitingForRepeatChoice = false;

        ResetQuizScore();
        StartLesson(startIndex);
    }

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------

    private void ResetQuizScore()
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

    // -------------------------------------------------------------------------
    // Coroutine / Audio Helpers
    // -------------------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

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

    // -------------------------------------------------------------------------
    // Lesson (Quiz Item) Sequence
    //
    //   1. Prompt audio (intro + instruction)
    //   2. Answer choices A / B / C (letter + animal sound each)
    //   3. Success / Wrong handling
    //   4. Support audio -> only after 3 consecutive mistakes
    // -------------------------------------------------------------------------

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeQuizCompletion());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoiceAnswer = false;

        if (logDebug)
            Debug.Log($"Starting quiz question {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskMultipleChoiceQuestion(lesson);
    }

    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        yield return PlayVoiceClipSequence(noAudioFallbackDelay, lesson.introAudio, lesson.instructionAudio);
    }

    /// <summary>
    /// Plays the A / B / C choices. Used both for the first ask and every
    /// re-ask after a wrong answer or support message.
    /// </summary>
    private IEnumerator AskMultipleChoiceQuestion(BrailleLesson lesson)
    {
        waitingForChoiceAnswer = true;

        yield return PlayLetterThenSound(letterAAudio, lesson.choiceAAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayLetterThenSound(letterBAudio, lesson.choiceBAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayLetterThenSound(letterCAudio, lesson.choiceCAudio);
    }

    private IEnumerator PlayLetterThenSound(AudioClip letterClip, AudioClip soundClip)
    {
        if (voiceAudioSource == null) yield break;

        if (letterClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = letterClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(letterClip.length);
        }

        if (soundClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = soundClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(soundClip.length);
        }
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForChoiceAnswer)
            HandleMultipleChoiceAnswer(submittedPattern);
    }

    private void HandleMultipleChoiceAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        // Dot 1 = Choice A   |   Dot 2 = Choice B   |   Dot 3 = Choice C
        AnswerChoice selected;

        if (pattern == "100000") selected = AnswerChoice.A;
        else if (pattern == "010000") selected = AnswerChoice.B;
        else if (pattern == "001000") selected = AnswerChoice.C;
        else return; // Unrecognized pattern: keep waiting for a valid answer.

        BrailleLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected == lesson.correctAnswer)
        {
            currentMistakeCount = 0;
            lessonActive = false;

            RunFlow(HandleCorrectAnswer(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(lesson));
            else
                RunFlow(HandleWrongAnswer(lesson));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson)
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return PlayVoiceClip(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(BrailleLesson lesson)
    {
        AudioClip clip = lesson.wrongAudio != null ? lesson.wrongAudio : genericTryAgainAudio;

        yield return PlayVoiceClip(clip, noAudioFallbackDelay);
        yield return AskMultipleChoiceQuestion(lesson);
    }

    /// <summary>
    /// After 3 consecutive mistakes, play the support audio, reset the
    /// mistake streak, then re-ask the question.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        yield return PlayVoiceClip(lesson.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskMultipleChoiceQuestion(lesson);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next / Back handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mid-quiz: repeats ONLY the current question from the very beginning.
    /// At the end of the quiz: plays the whole quiz again.
    /// </summary>
    private void HandleRepeat()
    {
        if (returningToMenu) return;

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StopVoice();
            StartQuiz(0);
            return;
        }

        // Ignored while a correct-answer transition is in progress
        // (lessonActive is false during that window), so Repeat can't
        // interrupt the move to the next question.
        if (!lessonActive)
            return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        lessonActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;

        RunFlow(PlayLessonFromBeginning(lesson));
    }

    private void HandleNext()
    {
        if (!waitingForRepeatChoice || returningToMenu) return;

        if (logDebug)
            Debug.Log("[QuizController] Next pressed at end of quiz.");

        returningToMenu = true;
        waitingForRepeatChoice = false;

        StopFlow();
        StopVoice();

        ReturnToMenu();
    }

    private void HandleBack()
    {
        // Back only works at the very end of the quiz (after the score is announced).
        if (!waitingForRepeatChoice || returningToMenu) return;

        if (logDebug)
            Debug.Log("[QuizController] Back pressed at end of quiz.");

        returningToMenu = true;
        waitingForRepeatChoice = false;

        RunFlow(BackToMenuFlow());
    }

    // -------------------------------------------------------------------------
    // Quiz Completion
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuizCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForChoiceAnswer = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // 1. "Great job! You finished the quiz."
        yield return PlayVoiceClip(genericCompletedAudio, noAudioFallbackDelay);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();

        // 3. "Do you want to repeat?" The flag is set right before the question
        //    so the learner can answer (Repeat / Next / Back) while it plays.
        waitingForRepeatChoice = true;
        yield return PlayVoiceClip(repeatQuestionAudio, noAudioFallbackDelay);

        while (waitingForRepeatChoice)
            yield return null;
    }

    private IEnumerator BackToMenuFlow()
    {
        // Cut off the repeat-question audio if it is still playing.
        StopVoice();

        if (backToMenuAudio != null)
            yield return PlayVoiceClip(backToMenuAudio, 0f);

        ReturnToMenu();
    }

    /// <summary>Saves the high score, then reports the score and returns to the menu.</summary>
    private void ReturnToMenu()
    {
        SaveHighScoreIfNeeded();

        // Safety net: if the Inspector slot was left empty, look for a reporter in the scene.
        if (resultReporter == null)
            resultReporter = FindFirstObjectByType<QuizResultReporter>();

        Debug.Log($"[QuizController] ReturnToMenu called. resultReporter assigned={resultReporter != null}, score={totalScore}");

        if (resultReporter != null)
        {
            resultReporter.ReportScoreAndReturn(totalScore);   // saves score + returns to GameMenu
        }
        else
        {
            Debug.LogWarning("[QuizController] No QuizResultReporter assigned - loading menu directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // -------------------------------------------------------------------------
    // Final Score Audio
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        yield return PlayVoiceClip(yourScoreIsAudio, 0f);
        yield return PlayVoiceClip(finalScoreClip, 0f);
        yield return PlayVoiceClip(whileYourHighestScoreIsAudio, 0f);
        yield return PlayVoiceClip(highScoreClip, 0f);
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Audio Helpers
    // -------------------------------------------------------------------------

    private IEnumerator PlayVoiceClip(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else if (fallbackWait > 0f)
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    private IEnumerator PlayVoiceClipSequence(float fallbackWait, params AudioClip[] clips)
    {
        bool playedAny = false;

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                playedAny = true;
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}