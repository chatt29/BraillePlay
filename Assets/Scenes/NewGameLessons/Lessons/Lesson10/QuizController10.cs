using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUIZ portion of the scene (audio-only — no visual UI).
///
/// Owns:  multiple-choice + true/false questions, Braille answer input,
///        correct / wrong / support feedback, the "repeat the story?" prompt,
///        the scoring system, high score, result reporting, and the final
///        "play again or finish?" choice.
///
/// Communication (Quiz -> Lesson, via C# events; the quiz has no reference to
/// the lesson controller):
///   LessonQuizCompleted(int)   all questions of a lesson answered correctly
///   StoryReplayRequested(int)  player pressed Repeat -> lesson replays story
///   StoryReplayCancelled       player pressed Next during the replay
///   RestartRequested           player chose to play again -> restart at lesson 0
/// </summary>
public class QuizController10 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz Question — Multiple Choice (answered via Braille Dot1=A, Dot2=B, Dot3=C)
    // Text fields are TRANSCRIPTS only (never displayed, logged when Log Debug is on).
    // -------------------------------------------------------------------------

    public enum AnswerChoice { A, B, C }

    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Correct Answer (Braille: Dot1=A, Dot2=B, Dot3=C)")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Support (played after enough wrong answers; falls back to the lesson quiz's Support Message/Audio if left empty)")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Question — True or False (answered via Braille Dot1=True, Dot2=False)
    // -------------------------------------------------------------------------

    public enum TrueFalseChoice { True, False }

    [Serializable]
    public class TrueFalseQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Correct Answer (Braille: Dot1=True, Dot2=False)")]
        public TrueFalseChoice correctAnswer = TrueFalseChoice.True;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Support (played after enough wrong answers; falls back to the lesson quiz's Support Message/Audio if left empty)")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    /// <summary>
    /// All quiz content for ONE lesson. Entry N in <see cref="lessonQuizzes"/>
    /// belongs to entry N in LessonController10.lessons.
    /// </summary>
    [Serializable]
    public class LessonQuiz
    {
        [Header("Identity (should match the lesson at the same index)")]
        public string lessonLabel;

        // The two lists below are asked back-to-back in order: all Multiple
        // Choice questions first, then all True/False questions.
        [Header("2a. Question Section — Multiple Choice")]
        public List<QuizQuestion> questions = new List<QuizQuestion>();

        [Header("2b. Question Section — True or False")]
        public List<TrueFalseQuestion> trueFalseQuestions = new List<TrueFalseQuestion>();

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Quiz Data (index must match LessonController10.lessons)")]
    public List<LessonQuiz> lessonQuizzes = new List<LessonQuiz>();

    [Header("Audio")]
    [Tooltip("Assign the SAME AudioSource that LessonController10 uses.")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("End-of-Quiz Choice (played AFTER the high score is announced)")]
    [Tooltip("Plays after the high score. Player then presses Repeat to play again, or Yes/Next to finish.")]
    public AudioClip repeatQuestionAudio;

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;

    [Tooltip("Total wrong answers needed for each deduction (original behaviour: 3).")]
    public int mistakesPerDeduction = 3;
    public string highScoreKey = "Lesson10HighScore";

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Support Settings")]
    public bool resetMistakesAfterSupport = true;

    [Tooltip("Number of consecutive wrong answers on the same question before the Support Message plays. The 'repeat the story?' prompt still plays after every wrong answer, regardless of this count.")]
    public int mistakesBeforeSupport = 3;

    [Header("Repeat-Story Prompt (played after every wrong answer)")]
    [TextArea(2, 5)]
    public string repeatStoryPromptMessage = "Do you want to hear the story again? Press Repeat to hear it again, or press Next to continue with the question.";
    public AudioClip repeatStoryPromptAudio;

    [Header("Repeat-Story Confirmation (played after the story replay)")]
    [TextArea(2, 5)]
    public string repeatQuestionConfirmMessage = "Do you want to repeat the question? Press Next to continue.";
    public AudioClip repeatQuestionConfirmAudio;

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;

    [Tooltip("How long to pause when a step has no audio clip assigned.")]
    public float noAudioDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Events (Quiz -> Lesson)
    // -------------------------------------------------------------------------

    /// <summary>All questions of the given lesson were answered correctly.</summary>
    public event Action<int> LessonQuizCompleted;

    /// <summary>Player pressed Repeat during the quiz: replay the given lesson's story.</summary>
    public event Action<int> StoryReplayRequested;

    /// <summary>Player pressed Next while the story replay was playing: stop the replay.</summary>
    public event Action StoryReplayCancelled;

    /// <summary>Player chose to play again: restart from the first lesson.</summary>
    public event Action RestartRequested;

    // -------------------------------------------------------------------------
    // Public read-only info
    // -------------------------------------------------------------------------

    public int LessonQuizCount => lessonQuizzes?.Count ?? 0;
    public int TotalScore => totalScore;
    public int HighScore => highScore;
    public int TotalWrongCount => totalWrongCount;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private int currentQuestionIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    // True while a lesson's questions are in progress.
    private bool quizActive = false;
    private bool waitingForQuizAnswer = false;

    // True whenever the player is being asked whether to repeat the Story —
    // right after a wrong answer, or after the story replay. Only gates
    // HandleNext()/HandleRepeat(); does not touch the question index, score
    // or mistake count.
    private bool waitingForRepeatConfirmation = false;

    // True once the final score + high score have been announced and the
    // player is deciding: Repeat = play again, Yes/Next = finish.
    private bool waitingForRepeatChoice = false;

    private Coroutine flowRoutine;

    // Read-only snapshot of "whichever question is currently active", built
    // from either a QuizQuestion or a TrueFalseQuestion so the rest of the
    // flow (asking / correct / wrong / support) doesn't care which list the
    // question lives in.
    private struct ActiveQuestionData
    {
        public string questionText;
        public AudioClip questionAudio;
        public AudioClip soundEffectAudio;
        public string successMessage;
        public AudioClip successAudio;
        public string wrongMessage;
        public string supportMessage;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (voiceAudioSource == null)
            voiceAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
    }

    private void Start()
    {
        ResetQuizScore();
    }

    // -------------------------------------------------------------------------
    // Coroutine Helpers
    // -------------------------------------------------------------------------

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

        int deductions = totalWrongCount / Mathf.Max(1, mistakesPerDeduction);
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        if (logDebug)
            Debug.Log($"[QuizController10] AddMistake: totalWrongCount={totalWrongCount}, deductions={deductions}, deductionPerMistake={deductionPerMistake}, totalScore={totalScore}");
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
    // Public entry points (called by LessonController10)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts the question section for the lesson at <paramref name="lessonIndex"/>.
    /// If that lesson has no questions configured it is treated as complete.
    /// </summary>
    public void StartLessonQuiz(int lessonIndex)
    {
        currentLessonIndex = lessonIndex;
        currentQuestionIndex = 0;
        currentMistakeCount = 0;
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;
        waitingForRepeatChoice = false;

        if (!TryGetLessonQuiz(lessonIndex, out LessonQuiz quiz) || GetTotalQuestionCount(quiz) == 0)
        {
            quizActive = false;
            LessonQuizCompleted?.Invoke(lessonIndex);
            return;
        }

        quizActive = true;

        if (logDebug)
            Debug.Log($"[QuizController10] Starting quiz for lesson {lessonIndex}: {quiz.lessonLabel}");

        RunFlow(AskQuestionRoutine(currentQuestionIndex));
    }

    /// <summary>
    /// Called by the lesson controller once every lesson is done: announces
    /// the score and high score, plays the repeat-question audio, then waits
    /// for the player to choose (Repeat = play again, Yes/Next = finish).
    /// </summary>
    public void BeginFinalResults()
    {
        quizActive = false;
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;
        waitingForRepeatChoice = false;

        RunFlow(FinalResultsRoutine());
    }

    /// <summary>
    /// Called by the lesson controller after it has replayed the story:
    /// plays "Do you want to repeat the question? Press Next to continue".
    /// </summary>
    public void PlayRepeatQuestionConfirmPrompt()
    {
        if (!waitingForRepeatConfirmation) return;

        RunFlow(PlayVoice(repeatQuestionConfirmMessage, repeatQuestionConfirmAudio));
        // waitingForRepeatConfirmation stays true until the player presses Next or Repeat.
    }

    // -------------------------------------------------------------------------
    // Question lookup (Multiple Choice first, then True/False)
    //
    // Multiple Choice questions occupy indices [0, mcCount); True/False
    // questions occupy [mcCount, mcCount + tfCount).
    // -------------------------------------------------------------------------

    private bool TryGetLessonQuiz(int lessonIndex, out LessonQuiz quiz)
    {
        quiz = null;
        if (lessonQuizzes == null || lessonIndex < 0 || lessonIndex >= lessonQuizzes.Count) return false;

        quiz = lessonQuizzes[lessonIndex];
        return quiz != null;
    }

    private int GetTotalQuestionCount(LessonQuiz quiz)
    {
        int mcCount = quiz.questions?.Count ?? 0;
        int tfCount = quiz.trueFalseQuestions?.Count ?? 0;
        return mcCount + tfCount;
    }

    private ActiveQuestionData GetActiveQuestionData(LessonQuiz quiz, int index)
    {
        int mcCount = quiz.questions?.Count ?? 0;

        if (index < mcCount)
        {
            QuizQuestion q = quiz.questions[index];
            return new ActiveQuestionData
            {
                questionText = q.questionText,
                questionAudio = q.questionAudio,
                soundEffectAudio = q.soundEffectAudio,
                successMessage = q.successMessage,
                successAudio = q.successAudio,
                wrongMessage = q.wrongMessage,
                supportMessage = q.supportMessage,
                supportAudio = q.supportAudio
            };
        }

        TrueFalseQuestion tf = quiz.trueFalseQuestions[index - mcCount];
        return new ActiveQuestionData
        {
            questionText = tf.questionText,
            questionAudio = tf.questionAudio,
            soundEffectAudio = tf.soundEffectAudio,
            successMessage = tf.successMessage,
            successAudio = tf.successAudio,
            wrongMessage = tf.wrongMessage,
            supportMessage = tf.supportMessage,
            supportAudio = tf.supportAudio
        };
    }

    // -------------------------------------------------------------------------
    // Asking a question
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays the optional sound effect, then the question audio. Used for the
    /// first ask of a question and every re-ask after a wrong answer or
    /// story replay.
    /// </summary>
    private IEnumerator AskQuestionRoutine(int questionIndex)
    {
        if (!TryGetLessonQuiz(currentLessonIndex, out LessonQuiz quiz)
            || questionIndex < 0
            || questionIndex >= GetTotalQuestionCount(quiz))
        {
            // No (more) questions configured — treat lesson quiz as complete.
            CompleteLessonQuiz();
            yield break;
        }

        // The player may answer as soon as the question starts playing.
        waitingForQuizAnswer = true;
        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        if (data.soundEffectAudio != null && voiceAudioSource != null)
        {
            yield return PlayClip(data.soundEffectAudio);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        yield return PlayVoice(data.questionText, data.questionAudio);
    }

    private void CompleteLessonQuiz()
    {
        quizActive = false;
        waitingForQuizAnswer = false;
        LessonQuizCompleted?.Invoke(currentLessonIndex);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizActive || waitingForRepeatChoice || waitingForRepeatConfirmation)
            return;

        if (waitingForQuizAnswer)
            HandleQuizAnswer(submittedPattern);
    }

    /// <summary>
    /// Validates the submitted Braille pattern against the current question.
    /// Multiple Choice: Dot 1 = A, Dot 2 = B, Dot 3 = C.
    /// True/False:      Dot 1 = True, Dot 2 = False.
    /// </summary>
    private void HandleQuizAnswer(string pattern)
    {
        if (!waitingForQuizAnswer) return;
        if (!TryGetLessonQuiz(currentLessonIndex, out LessonQuiz quiz)) return;

        int mcCount = quiz.questions?.Count ?? 0;
        bool isCorrect;

        if (currentQuestionIndex < mcCount)
        {
            AnswerChoice userAnswer;

            if (pattern == "100000") userAnswer = AnswerChoice.A;
            else if (pattern == "010000") userAnswer = AnswerChoice.B;
            else if (pattern == "001000") userAnswer = AnswerChoice.C;
            else return;

            isCorrect = userAnswer == quiz.questions[currentQuestionIndex].correctAnswer;
        }
        else
        {
            TrueFalseChoice userAnswer;

            if (pattern == "100000") userAnswer = TrueFalseChoice.True;
            else if (pattern == "010000") userAnswer = TrueFalseChoice.False;
            else return;

            isCorrect = userAnswer == quiz.trueFalseQuestions[currentQuestionIndex - mcCount].correctAnswer;
        }

        waitingForQuizAnswer = false;

        if (isCorrect)
        {
            currentMistakeCount = 0;
            RunFlow(HandleCorrectAnswer(quiz, currentQuestionIndex));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();
            RunFlow(HandleWrongAnswer(quiz, currentQuestionIndex));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    /// <summary>
    /// Success feedback for the current question, then the next question — or,
    /// after the last question, tells the lesson controller this lesson is done.
    /// </summary>
    private IEnumerator HandleCorrectAnswer(LessonQuiz quiz, int questionIndex)
    {
        SaveHighScoreIfNeeded();

        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        string message = !string.IsNullOrWhiteSpace(data.successMessage)
            ? data.successMessage
            : $"Correct! {quiz.lessonLabel}.";

        AudioClip clip = data.successAudio != null
            ? data.successAudio
            : genericCorrectAudio;

        yield return PlayVoice(message, clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentQuestionIndex++;

        if (currentQuestionIndex >= GetTotalQuestionCount(quiz))
            CompleteLessonQuiz();
        else
            RunFlow(AskQuestionRoutine(currentQuestionIndex));
    }

    /// <summary>
    /// Wrong feedback, then — only after <see cref="mistakesBeforeSupport"/>
    /// consecutive mistakes — the Support message. After that, EVERY wrong
    /// answer asks whether to hear the story again: Repeat replays it,
    /// Next re-asks the current question.
    /// </summary>
    private IEnumerator HandleWrongAnswer(LessonQuiz quiz, int questionIndex)
    {
        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        string wrongMessage = !string.IsNullOrWhiteSpace(data.wrongMessage)
            ? data.wrongMessage
            : "Try again.";

        yield return PlayVoice(wrongMessage, genericTryAgainAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        if (currentMistakeCount >= mistakesBeforeSupport)
        {
            string supportMessage = !string.IsNullOrWhiteSpace(data.supportMessage)
                ? data.supportMessage
                : (!string.IsNullOrWhiteSpace(quiz.supportMessage)
                    ? quiz.supportMessage
                    : "Here is some help. Listen carefully and try again.");

            AudioClip supportClip = data.supportAudio != null
                ? data.supportAudio
                : quiz.supportAudio;

            yield return PlayVoice(supportMessage, supportClip);
            yield return new WaitForSeconds(delayAfterVoice);

            if (resetMistakesAfterSupport)
                currentMistakeCount = 0;
        }

        // Ask whether to repeat the Story — every wrong answer.
        waitingForRepeatConfirmation = true;

        yield return PlayVoice(repeatStoryPromptMessage, repeatStoryPromptAudio);

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Repeat or Next.
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeat button.
    ///   - At the end-of-quiz choice: play everything again (score reset,
    ///     restart at lesson 0).
    ///   - Mid-quiz: ask the lesson controller to replay the Story only. The
    ///     current question, score and mistake count are untouched; the
    ///     question is re-asked once Next is pressed.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StopFlow();
            StopVoice();

            ResetQuizScore();
            RestartRequested?.Invoke();
            return;
        }

        if (!quizActive) return;

        // Cancel any pending answer wait — we're replaying the story instead.
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = true;
        StopFlow();

        StoryReplayRequested?.Invoke(currentLessonIndex);
    }

    /// <summary>
    /// Yes / Next button.
    ///   - At the end-of-quiz choice: finish, report the score and return.
    ///   - After a wrong answer / story replay: re-ask the current question.
    /// </summary>
    private void HandleNext()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            FinishAndReturn();
            return;
        }

        if (waitingForRepeatConfirmation)
        {
            waitingForRepeatConfirmation = false;

            // If the story replay is still playing, stop it.
            StoryReplayCancelled?.Invoke();

            RunFlow(AskQuestionRoutine(currentQuestionIndex));
        }
    }

    // -------------------------------------------------------------------------
    // Final results
    // -------------------------------------------------------------------------

    private IEnumerator FinalResultsRoutine()
    {
        SaveHighScoreIfNeeded();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return PlayVoice(finalMessage, genericCompletedAudio);
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // After the high score: ask whether to play again. The choice is
        // accepted from the moment this prompt starts, so the player may
        // answer without waiting for it to finish.
        waitingForRepeatChoice = true;
        yield return PlayVoice(repeatQuestionMessage, repeatQuestionAudio);

        // Stays here — waitingForRepeatChoice remains true until the player
        // presses Repeat (play again) or Yes/Next (finish).
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        yield return PlayClip(yourScoreIsAudio);
        yield return PlayClip(GetNumberAudio(totalScore));
        yield return PlayClip(whileYourHighestScoreIsAudio);
        yield return PlayClip(GetNumberAudio(highScore));
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    private void FinishAndReturn()
    {
        StopFlow();
        StopVoice();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController10] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------------------------

    private void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    /// <summary>Plays a clip and waits for it to finish. Skips silently if there is no clip.</summary>
    private IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null) yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    /// <summary>
    /// Plays a clip and waits for it to finish. If no clip is assigned, waits
    /// <see cref="noAudioDelay"/> instead so the flow timing stays the same.
    /// The transcript is only logged, never displayed.
    /// </summary>
    private IEnumerator PlayVoice(string transcript, AudioClip clip)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[QuizController10] {transcript}");

        if (clip != null && voiceAudioSource != null)
            yield return PlayClip(clip);
        else
            yield return new WaitForSeconds(noAudioDelay);
    }
}