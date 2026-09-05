using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the quiz portion of the scene: the welcome/intro, each multiple
/// choice question, mistake tracking and support hints, scoring, and the
/// final score report. Started externally by <see cref="LessonController"/>
/// calling BeginQuiz() once the lesson pages are finished.
///
/// This script no longer touches any visual UI - it is entirely audio and
/// input driven, since the app is designed for blind/visually impaired
/// learners. Correctness feedback that used to be a sprite swap is now
/// carried entirely by the success/wrong audio clips.
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

        [Header("Messages")]
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

    // -------------------------------------------------------------------------
    // Result Reporting
    // -------------------------------------------------------------------------

    [Header("Result Reporting")]
    public QuizResultReporter resultReporter;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
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

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Scene Text (kept as authoring/documentation data; nothing renders it)
    // -------------------------------------------------------------------------

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    // -------------------------------------------------------------------------
    // Quiz Content / Flow
    // -------------------------------------------------------------------------

    [Header("Quiz Questions")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------

    [Header("Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    // True while THIS script owns player input (see LessonController for the
    // matching flag on the lesson side).
    private bool isActive = false;

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForChoiceAnswer = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleYesOrNext;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleYesOrNext;
    }

    /// <summary>Called by LessonController once the lesson pages are finished.</summary>
    public void BeginQuiz()
    {
        if (logDebug)
            Debug.Log("QuizController starting.");

        isActive = true;
        ResetQuizScore();
        RunFlow(StartQuizIntro());
    }

    // -------------------------------------------------------------------------
    // Coroutine Helper
    // -------------------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
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
    // Quiz Intro / Question Flow
    // -------------------------------------------------------------------------

    private IEnumerator StartQuizIntro()
    {
        yield return PlayAudioOrWait(welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayAudioOrWait(letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeSceneCompletion());
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

    // -------------------------------------------------------------------------
    // Question Sequence
    //
    // Exact order (unchanged from the original single script):
    //   1. Prompt Audio (intro + instruction)
    //   2. Answer Choices A / B / C (letter + animal sound each)
    //   3. Success Audio  -> handled in HandleCorrectAnswer
    //   4. Wrong Audio    -> handled in HandleWrongAnswer
    //   5. Support Audio  -> only after 3 consecutive mistakes
    //
    // Reused both when a question first starts and whenever the player asks
    // to repeat the current question.
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskMultipleChoiceQuestion(lesson);
    }

    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        yield return PlayAudioSequence(noAudioTextDelay, lesson.introAudio, lesson.instructionAudio);
    }

    private IEnumerator AskMultipleChoiceQuestion(BrailleLesson lesson)
    {
        waitingForChoiceAnswer = true;
        yield return PlayAnswerChoices(lesson);
    }

    private IEnumerator PlayAnswerChoices(BrailleLesson lesson)
    {
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
        if (!isActive) return;
        if (!lessonActive || sceneFinished || waitingForRepeatChoice) return;
        if (!waitingForChoiceAnswer) return;

        HandleMultipleChoiceAnswer(submittedPattern);
    }

    private void HandleMultipleChoiceAnswer(string pattern)
    {
        // Dot 1 = Choice A   |   Dot 2 = Choice B   |   Dot 3 = Choice C
        AnswerChoice? selected = null;

        if (pattern == "100000") selected = AnswerChoice.A;
        else if (pattern == "010000") selected = AnswerChoice.B;
        else if (pattern == "001000") selected = AnswerChoice.C;

        if (selected == null) return; // Unrecognized pattern: keep waiting for a valid answer.

        BrailleLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected.Value == lesson.correctAnswer)
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

        AudioClip clip = lesson.successAudio != null
            ? lesson.successAudio
            : genericCorrectAudio;

        yield return PlayAudioOrWait(clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(BrailleLesson lesson)
    {
        AudioClip clip = lesson.wrongAudio != null
            ? lesson.wrongAudio
            : genericTryAgainAudio;

        yield return PlayAudioOrWait(clip, noAudioTextDelay);
        yield return AskMultipleChoiceQuestion(lesson);
    }

    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        yield return PlayAudioOrWait(lesson.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskMultipleChoiceQuestion(lesson);
    }

    // -------------------------------------------------------------------------
    // Repeat Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeats ONLY the current question from the very beginning: Prompt
    /// Audio, then the three Answer Choices. Never advances to the next
    /// question and never replays a previous one.
    /// </summary>
    private void HandleRepeat()
    {
        if (!isActive) return;

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next question
        // is in progress (lessonActive is false during that window). Without
        // this guard, a Repeat trigger here would stop the in-flight
        // HandleCorrectAnswer coroutine before it calls StartLesson(index + 1),
        // replaying the just-answered question instead of advancing.
        if (!lessonActive) return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        lessonActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;

        RunFlow(PlayLessonFromBeginning(lesson));
    }

    // -------------------------------------------------------------------------
    // Scene Completion
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        yield return PlayAudioOrWait(genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();

        // Ask whether to play the quiz again or finish, then wait for the
        // player's choice. HandleRepeat and HandleYesOrNext resolve this wait.
        waitingForRepeatChoice = true;

        yield return PlayAudioOrWait(repeatQuestionAudio, noAudioTextDelay);

        while (waitingForRepeatChoice)
            yield return null;
    }

    /// <summary>Player chose to finish instead of repeating the quiz - report the final score.</summary>
    private void HandleYesOrNext()
    {
        if (!isActive) return;
        if (!waitingForRepeatChoice) return;

        waitingForRepeatChoice = false;
        isActive = false;

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final Score Audio
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        yield return PlayAudioOrWait(yourScoreIsAudio, 0f);
        yield return PlayAudioOrWait(finalScoreClip, 0f);
        yield return PlayAudioOrWait(whileYourHighestScoreIsAudio, 0f);
        yield return PlayAudioOrWait(highScoreClip, 0f);
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

    /// <summary>Plays a clip and waits for it to finish, or waits a fallback duration if there is no clip.</summary>
    private IEnumerator PlayAudioOrWait(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else if (clip == null)
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    /// <summary>Plays each non-null clip in order back to back, or waits a fallback duration if all are null.</summary>
    private IEnumerator PlayAudioSequence(float fallbackWait, params AudioClip[] clips)
    {
        if (voiceAudioSource == null)
        {
            yield return new WaitForSeconds(fallbackWait);
            yield break;
        }

        bool playedAny = false;
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;
            playedAny = true;

            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}