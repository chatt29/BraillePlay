using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class QuizManager6 : MonoBehaviour
{
    public enum AnswerChoice { A, B, C }

    // -------------------------------------------------------------------------
    // Quiz Question (3 choices: A, B, C — answered via Braille input)
    // -------------------------------------------------------------------------

    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Answer Choices (data only — read aloud via questionAudio)")]
        public string choiceAText = "A";
        public string choiceBText = "B";
        public string choiceCText = "C";

        [Header("Correct Answer (Braille: Dot1=A, Dot2=B, Dot3=C)")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Support (falls back to the lesson's Support Message/Audio if left empty)")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Cross-script link
    // -------------------------------------------------------------------------

    [Header("Lesson Manager")]
    [Tooltip("Assign the LessonManager component. If left empty, Awake() will try GetComponent<LessonManager6>() on this same GameObject.")]
    public LessonManager6 lessonManager;

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    [Tooltip("Shared AudioSource. Must be the SAME AudioSource assigned on LessonManager so voice lines never overlap.")]
    public AudioSource voiceAudioSource;

    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Score Settings
    // -------------------------------------------------------------------------

    [Header("Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    // -------------------------------------------------------------------------
    // Support / Repeat Settings
    // -------------------------------------------------------------------------

    [Header("Support Settings")]
    public bool resetMistakesAfterSupport = true;

    [Tooltip("Number of consecutive wrong answers on the same question before the Support Message plays. The 'repeat the story?' prompt still shows after every wrong answer, regardless of this count.")]
    public int mistakesBeforeSupport = 3;

    [Header("Repeat-Story Prompt (shown after every wrong answer)")]
    public AudioClip repeatStoryPromptAudio;

    [Header("Repeat-Story Confirmation (mid-lesson Repeat button)")]
    public AudioClip repeatQuestionConfirmAudio;

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private LessonManager6.BrailleLesson currentLesson;
    private int currentQuestionIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool waitingForQuizAnswer = false;

    // True while waiting for the player to choose Repeat (replay the story)
    // or Next (continue) — either after a wrong answer, or after replaying
    // the story via the mid-lesson Repeat button.
    private bool waitingForRepeatConfirmation = false;

    private Action onAllQuestionsComplete;
    private Coroutine flowRoutine;

    public int TotalScore => totalScore;
    public int HighScore => highScore;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (lessonManager == null)
            lessonManager = GetComponent<LessonManager6>();
    }

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
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

    public void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    public void ReportFinalScore()
    {
        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizManager] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Question Flow (called by LessonManager)
    // -------------------------------------------------------------------------

    /// <summary>Starts the question round for a lesson. onComplete fires once every question has been answered correctly.</summary>
    public void BeginQuestions(LessonManager6.BrailleLesson lesson, Action onComplete)
    {
        currentLesson = lesson;
        onAllQuestionsComplete = onComplete;
        currentQuestionIndex = 0;
        currentMistakeCount = 0;
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;

        RunFlow(AskQuizQuestion(currentQuestionIndex));
    }

    private IEnumerator AskQuizQuestion(int questionIndex)
    {
        if (currentLesson?.questions == null || questionIndex < 0 || questionIndex >= currentLesson.questions.Count)
        {
            // No (more) questions configured — treat lesson as complete.
            onAllQuestionsComplete?.Invoke();
            yield break;
        }

        waitingForQuizAnswer = true;
        QuizQuestion question = currentLesson.questions[questionIndex];

        if (question.soundEffectAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = question.soundEffectAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(question.soundEffectAudio.length);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        yield return PlayAudioMessage(question.questionAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (lessonManager == null) return;

        if (!lessonManager.LessonActive || lessonManager.SceneFinished ||
            lessonManager.WaitingForRepeatChoice || waitingForRepeatConfirmation)
            return;

        if (waitingForQuizAnswer)
            HandleQuizAnswer(submittedPattern);
    }

    /// <summary>
    /// Validates the submitted Braille pattern against the current question's
    /// correct answer. Dot 1 = A, Dot 2 = B, Dot 3 = C.
    /// </summary>
    private void HandleQuizAnswer(string pattern)
    {
        if (!waitingForQuizAnswer) return;

        AnswerChoice userAnswer;

        if (pattern == "100000") userAnswer = AnswerChoice.A;
        else if (pattern == "010000") userAnswer = AnswerChoice.B;
        else if (pattern == "001000") userAnswer = AnswerChoice.C;
        else return;

        QuizQuestion question = currentLesson.questions[currentQuestionIndex];
        waitingForQuizAnswer = false;

        if (userAnswer == question.correctAnswer)
        {
            currentMistakeCount = 0;
            RunFlow(HandleCorrectAnswer(question));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();
            RunFlow(HandleWrongAnswer(question));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    /// <summary>Success feedback, then advance to the next question — or, if that was the last question, notify LessonManager the lesson's quiz is complete.</summary>
    private IEnumerator HandleCorrectAnswer(QuizQuestion question)
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = question.successAudio != null ? question.successAudio : genericCorrectAudio;
        yield return PlayAudioMessage(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentQuestionIndex++;
        yield return AskQuizQuestion(currentQuestionIndex);
    }

    /// <summary>
    /// Wrong feedback, then — only once the player has made
    /// <see cref="mistakesBeforeSupport"/> consecutive mistakes on this
    /// question — the Support Message. After that, every wrong answer asks
    /// the player whether they want to repeat the Story (handled by
    /// RequestRepeatStory / RequestNext, called from LessonManager).
    /// </summary>
    private IEnumerator HandleWrongAnswer(QuizQuestion question)
    {
        yield return PlayAudioMessage(genericTryAgainAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        if (currentMistakeCount >= mistakesBeforeSupport)
        {
            AudioClip supportClip = question.supportAudio != null ? question.supportAudio : currentLesson.supportAudio;

            yield return PlayAudioMessage(supportClip, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);

            if (resetMistakesAfterSupport)
                currentMistakeCount = 0;
        }

        // Ask whether to repeat the Story — every wrong answer, regardless
        // of mistake count. RequestRepeatStory()/RequestNext() (triggered by
        // LessonManager from the Repeat/Next input events) resolve this.
        waitingForRepeatConfirmation = true;
        yield return PlayAudioMessage(repeatStoryPromptAudio, noAudioFallbackDelay);

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Repeat or Next.
    }

    // -------------------------------------------------------------------------
    // Repeat / Next (delegated from LessonManager's input handlers)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Replays the Story section (via LessonManager), then asks whether to
    /// repeat the current question. Does not touch currentQuestionIndex,
    /// score, or mistake count. Called whenever the player presses Repeat
    /// while a lesson is active — mirrors the original script's "Repeat
    /// works any time mid-lesson" behavior.
    /// </summary>
    public void RequestRepeatStory()
    {
        if (currentLesson == null || lessonManager == null) return;

        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = true;
        RunFlow(RepeatStoryThenConfirm());
    }

    private IEnumerator RepeatStoryThenConfirm()
    {
        // Runs LessonManager's story coroutine inline — no StartCoroutine
        // needed, it just executes as part of this coroutine.
        yield return lessonManager.PlayStorySection(currentLesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayAudioMessage(repeatQuestionConfirmAudio, noAudioFallbackDelay);

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Next (see RequestNext()).
    }

    /// <summary>Resolves a pending repeat-confirmation by re-asking the current question. No-ops if nothing is pending.</summary>
    public void RequestNext()
    {
        if (!waitingForRepeatConfirmation) return;

        waitingForRepeatConfirmation = false;
        RunFlow(AskQuizQuestion(currentQuestionIndex));
    }

    // -------------------------------------------------------------------------
    // Final Score Audio (called by LessonManager at scene-completion points)
    // -------------------------------------------------------------------------

    public IEnumerator PlayFinalScoreAudio()
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

    // -------------------------------------------------------------------------
    // Audio helper
    // -------------------------------------------------------------------------

    /// <summary>Plays a clip and waits for its length, or waits a fallback duration if the clip is missing.</summary>
    private IEnumerator PlayAudioMessage(AudioClip clip, float fallbackWait)
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
}
