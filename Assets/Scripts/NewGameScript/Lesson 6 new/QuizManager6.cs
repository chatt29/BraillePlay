using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the quiz portion of the scene: for each lesson it plays the
/// prompt and the story, then asks the multiple choice questions, judges
/// Braille chord answers (Dot 1 = A, Dot 2 = B, Dot 3 = C), tracks the score
/// and reports the result back to the game menu. Audio-only.
///
/// LessonManager6 calls StartQuiz(0) once the intro is done; from that point
/// this script owns the rest of the flow. The lesson data (story lines and
/// the "lessons" list) is read from LessonManager6.
///
/// Flow (same shape as QuizController1):
///   1. For each lesson: prompt, story, then the questions.
///   2. Correct -> success audio, next question.
///      Wrong -> try-again audio, support audio after N mistakes, then ask
///      whether to repeat the story (Repeat = replay the story, Next = re-ask).
///   3. After the last lesson: completed audio, score + high score, THEN the
///      repeat-question audio. The learner decides:
///        Repeat -> reset the score and play the quiz again
///        Next   -> report the score and return to the main menu
///        Back   -> same as Next, with an optional confirmation cue first
/// </summary>
public class QuizManager6 : MonoBehaviour
{
    public enum AnswerChoice { A, B, C }

    // -------------------------------------------------------------------------
    // Quiz Question (3 choices: A, B, C - answered via Braille input)
    // -------------------------------------------------------------------------

    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Answer Choices (data only - read aloud via questionAudio)")]
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
    // Lesson Data Link
    // -------------------------------------------------------------------------

    [Header("Lesson Data")]
    [Tooltip("Assign the LessonManager6 that holds the lessons list. If left empty, Awake() will try GetComponent<LessonManager6>() on this same GameObject.")]
    public LessonManager6 lessonManager;

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Back To Main Menu")]
    [Tooltip("Only used if no QuizResultReporter is assigned. Must be added in File > Build Settings.")]
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Optional cue, e.g. 'Going back to the main menu.' Played only when Back is pressed at the end of the quiz.")]
    public AudioClip backToMenuAudio;

    // -------------------------------------------------------------------------
    // Questions 1-5 (audio + correct answer set directly on this manager)
    // -------------------------------------------------------------------------

    [Header("Questions 1-5")]
    [Tooltip("If ON and the list below has entries, these questions are used instead of the lesson's own question list.")]
    public bool useManagerQuestions = true;

    [Tooltip("Element 0 = Question 1 ... Element 4 = Question 5. Assign each Question Audio and pick the Correct Answer.")]
    public List<QuizQuestion> questionSet = new List<QuizQuestion>
    {
        new QuizQuestion { correctAnswer = AnswerChoice.C }, // Question 1 -> C
        new QuizQuestion { correctAnswer = AnswerChoice.A }, // Question 2 (set in Inspector)
        new QuizQuestion { correctAnswer = AnswerChoice.A }, // Question 3 (set in Inspector)
        new QuizQuestion { correctAnswer = AnswerChoice.A }, // Question 4 (set in Inspector)
        new QuizQuestion { correctAnswer = AnswerChoice.A }  // Question 5 (set in Inspector)
    };

    /// <summary>The question list actually in use: this manager's set, or the lesson's list as a fallback.</summary>
    private List<QuizQuestion> ActiveQuestions
    {
        get
        {
            if (useManagerQuestions && questionSet != null && questionSet.Count > 0)
                return questionSet;

            return currentLesson != null ? currentLesson.questions : null;
        }
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    [Tooltip("Shared AudioSource. Must be the SAME AudioSource assigned on LessonManager6 so voice lines never overlap.")]
    public AudioSource voiceAudioSource;

    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Welcome / Completion / Repeat Audio")]
    [Tooltip("Played after the story, before the first question of each lesson.")]
    public AudioClip welcomeAudio;

    [Tooltip("Played after the last question of a lesson is answered correctly.")]
    public AudioClip genericCompletedAudio;

    [Tooltip("Played before a question is asked again (after the player presses Next following a wrong answer or story replay).")]
    public AudioClip repeatedQuestionAudio;

    [Header("End Of Quiz (falls back to the clips on LessonManager6 if left empty)")]
    [Tooltip("Played after the last lesson, before the score.")]
    public AudioClip finalCompletedAudio;
    [Tooltip("Played after the score: 'Press Repeat to play again or Next to finish.'")]
    public AudioClip endRepeatQuestionAudio;

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
    private int currentLessonIndex = -1;
    private int currentQuestionIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForQuizAnswer = false;

    // True while waiting for the player to choose Repeat (replay the story)
    // or Next (continue) - either after a wrong answer, or after replaying
    // the story via the mid-lesson Repeat button.
    private bool waitingForRepeatConfirmation = false;

    private bool waitingForRepeatChoice = false;   // end-of-quiz: Repeat, Next or Back
    private bool returningToMenu = false;          // guards against Next/Back/Repeat firing twice

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
    // Entry Point (called by LessonManager6)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by LessonManager6 once the intro is finished.
    /// Resets the score and begins the quiz at the given lesson index.
    /// </summary>
    public void StartQuiz(int startIndex)
    {
        if (logDebug)
            Debug.Log("QuizManager6 starting quiz.");

        if (lessonManager == null)
        {
            Debug.LogError("[QuizManager6] No LessonManager6 assigned/found - cannot read the lessons list.");
            return;
        }

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
    // Lesson Sequence
    //
    //   1. Prompt audio (intro + instruction)
    //   2. Story section (each story line's audio)
    //   3. Welcome audio, then the questions
    //   4. Success / Wrong handling
    //   5. Support audio -> only after N consecutive mistakes
    // -------------------------------------------------------------------------

    private void StartLesson(int index)
    {
        List<LessonManager6.BrailleLesson> lessons = lessonManager != null ? lessonManager.lessons : null;

        if (lessons == null || index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeQuizCompletion());
            return;
        }

        currentLessonIndex = index;
        currentLesson = lessons[index];
        currentQuestionIndex = 0;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;
        waitingForRepeatChoice = false;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {currentLesson.displayLabel}");

        RunFlow(PlayLessonFromBeginning(currentLesson));
    }

    private IEnumerator PlayLessonFromBeginning(LessonManager6.BrailleLesson lesson)
    {
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayStorySection(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        if (welcomeAudio != null)
        {
            yield return PlayVoiceClip(welcomeAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        yield return AskQuizQuestion(currentQuestionIndex);
    }

    private IEnumerator ShowPromptMessage(LessonManager6.BrailleLesson lesson)
    {
        yield return PlayVoiceClipSequence(noAudioFallbackDelay, lesson.introAudio, lesson.instructionAudio);
    }

    /// <summary>
    /// Plays the Story section for a lesson. Used when a lesson first starts
    /// and whenever the player asks to replay the story (after a wrong answer
    /// or via the mid-lesson Repeat button).
    /// </summary>
    private IEnumerator PlayStorySection(LessonManager6.BrailleLesson lesson)
    {
        if (lesson == null || lesson.storyLines == null) yield break;

        float pause = lessonManager != null ? lessonManager.delayBetweenStoryLines : 0.5f;

        foreach (LessonManager6.StoryLine line in lesson.storyLines)
        {
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line.storyText) && line.storyAudio == null) continue;

            yield return PlayVoiceClip(line.storyAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(pause);
        }
    }

    /// <summary>
    /// Asks a question. If the index is past the last question, plays the
    /// completed audio and moves on to the next lesson. When isRepeat is
    /// true, the repeated-question audio plays before the question.
    /// </summary>
    private IEnumerator AskQuizQuestion(int questionIndex, bool isRepeat = false)
    {
        List<QuizQuestion> questions = ActiveQuestions;

        if (logDebug)
            Debug.Log($"[QuizManager6] AskQuizQuestion index={questionIndex}, questionCount={(questions != null ? questions.Count : -1)}, repeat={isRepeat}");

        if (questions == null || questionIndex < 0 || questionIndex >= questions.Count)
        {
            // No (more) questions - play the completed audio, then next lesson.
            if (genericCompletedAudio != null)
            {
                yield return PlayVoiceClip(genericCompletedAudio, noAudioFallbackDelay);
                yield return new WaitForSeconds(delayAfterVoice);
            }

            StartLesson(currentLessonIndex + 1);
            yield break;
        }

        // Block input while any intro audio plays.
        waitingForQuizAnswer = false;
        QuizQuestion question = questions[questionIndex];

        if (isRepeat && repeatedQuestionAudio != null)
        {
            yield return PlayVoiceClip(repeatedQuestionAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (question.soundEffectAudio != null && voiceAudioSource != null)
        {
            yield return PlayVoiceClip(question.soundEffectAudio, 0f);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForQuizAnswer = true;
        yield return PlayVoiceClip(question.questionAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (logDebug)
            Debug.Log($"[QuizManager6] chord={submittedPattern} lessonActive={lessonActive} sceneFinished={sceneFinished} waitingForQuizAnswer={waitingForQuizAnswer} waitingForRepeatConfirmation={waitingForRepeatConfirmation} waitingForRepeatChoice={waitingForRepeatChoice}");

        if (!lessonActive || sceneFinished || waitingForRepeatChoice || waitingForRepeatConfirmation)
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

        List<QuizQuestion> questions = ActiveQuestions;
        if (questions == null || currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count) return;

        QuizQuestion question = questions[currentQuestionIndex];
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

    /// <summary>Success feedback, then advance to the next question (or the next lesson after the last one).</summary>
    private IEnumerator HandleCorrectAnswer(QuizQuestion question)
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = question.successAudio != null ? question.successAudio : genericCorrectAudio;
        yield return PlayVoiceClip(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentQuestionIndex++;
        yield return AskQuizQuestion(currentQuestionIndex);
    }

    /// <summary>
    /// Wrong feedback, then - once the player has made
    /// <see cref="mistakesBeforeSupport"/> consecutive mistakes on this
    /// question - the Support Message. After that, every wrong answer asks
    /// whether they want to repeat the story (Repeat / Next resolve it).
    /// </summary>
    private IEnumerator HandleWrongAnswer(QuizQuestion question)
    {
        yield return PlayVoiceClip(genericTryAgainAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        if (currentMistakeCount >= mistakesBeforeSupport)
        {
            AudioClip supportClip = question.supportAudio != null ? question.supportAudio : currentLesson.supportAudio;

            yield return PlayVoiceClip(supportClip, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);

            if (resetMistakesAfterSupport)
                currentMistakeCount = 0;
        }

        // Ask whether to repeat the story - every wrong answer, regardless of
        // mistake count. Stays here until the player presses Repeat or Next.
        waitingForRepeatConfirmation = true;
        yield return PlayVoiceClip(repeatStoryPromptAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next / Back handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mid-quiz: replays the story, then asks whether to repeat the current
    /// question (score and mistake count are untouched).
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

        if (!lessonActive || sceneFinished) return;
        if (currentLesson == null) return;

        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = true;
        RunFlow(RepeatStoryThenConfirm());
    }

    private IEnumerator RepeatStoryThenConfirm()
    {
        yield return PlayStorySection(currentLesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoiceClip(repeatQuestionConfirmAudio, noAudioFallbackDelay);

        // Stays here - waitingForRepeatConfirmation remains true until the
        // player presses Next.
    }

    private void HandleNext()
    {
        if (returningToMenu) return;

        // End of quiz: finish and return to the menu.
        if (waitingForRepeatChoice)
        {
            if (logDebug)
                Debug.Log("[QuizManager6] Next pressed at end of quiz.");

            returningToMenu = true;
            waitingForRepeatChoice = false;

            StopFlow();
            StopVoice();

            ReturnToMenu();
            return;
        }

        // Mid-quiz: resolve a pending repeat-confirmation by re-asking the
        // current question. Does nothing if nothing is pending.
        if (!lessonActive || sceneFinished) return;
        if (!waitingForRepeatConfirmation) return;

        waitingForRepeatConfirmation = false;
        RunFlow(AskQuizQuestion(currentQuestionIndex, true));
    }

    private void HandleBack()
    {
        // Back only works at the very end of the quiz (after the score is announced).
        if (!waitingForRepeatChoice || returningToMenu) return;

        if (logDebug)
            Debug.Log("[QuizManager6] Back pressed at end of quiz.");

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
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // 1. "Great job! You finished the lesson."
        AudioClip completedClip = finalCompletedAudio != null
            ? finalCompletedAudio
            : (lessonManager != null ? lessonManager.genericCompletedAudio : null);

        yield return PlayVoiceClip(completedClip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // 3. "Do you want to repeat?" The flag is set right before the question
        //    so the learner can answer (Repeat / Next / Back) while it plays.
        AudioClip repeatClip = endRepeatQuestionAudio != null
            ? endRepeatQuestionAudio
            : (lessonManager != null ? lessonManager.repeatQuestionAudio : null);

        waitingForRepeatChoice = true;
        yield return PlayVoiceClip(repeatClip, noAudioFallbackDelay);

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

        Debug.Log($"[QuizManager6] ReturnToMenu called. resultReporter assigned={resultReporter != null}, score={totalScore}");

        if (resultReporter != null)
        {
            resultReporter.ReportScoreAndReturn(totalScore);   // saves score + returns to GameMenu
        }
        else
        {
            Debug.LogWarning("[QuizManager6] No QuizResultReporter assigned - loading menu directly.");
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

        // Each clip waits until the previous one has fully finished.
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

    /// <summary>
    /// Plays a clip and waits until the AudioSource has REALLY finished
    /// playing it, or waits a fallback duration if the clip is missing.
    /// </summary>
    private IEnumerator PlayVoiceClip(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.loop = false;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();

            // Wait until the AudioSource has actually started (max 1 sec grace).
            float grace = 0f;
            while (!voiceAudioSource.isPlaying && grace < 1f)
            {
                grace += Time.unscaledDeltaTime;
                yield return null;
            }

            // Wait until the clip has REALLY finished playing.
            while (voiceAudioSource.isPlaying)
                yield return null;
        }
        else if (fallbackWait > 0f)
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    /// <summary>Plays each non-null clip in order back to back, or waits a fallback duration if all are null.</summary>
    private IEnumerator PlayVoiceClipSequence(float fallbackWait, params AudioClip[] clips)
    {
        bool playedAny = false;

        if (clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                playedAny = true;
                yield return PlayVoiceClip(clip, fallbackWait);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}