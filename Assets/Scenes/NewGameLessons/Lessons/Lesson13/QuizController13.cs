using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the quiz side of the scene: Multiple Choice + True/False questions,
/// Braille answer input, wrong/support/repeat-story handling, scoring, high
/// score, final announcement, and the "play again or finish?" decision.
/// Audio-only: no UI. Plays all audio through LessonController13.PlayVoice.
/// </summary>
public class QuizController13 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    public enum AnswerChoice { A, B, C }            // Braille Dot1 / Dot2 / Dot3
    public enum TrueFalseChoice { True, False }     // Braille Dot1 / Dot2

    [Serializable]
    public class QuizQuestion
    {
        [Tooltip("Optional note for authors / debug log only.")]
        [TextArea(2, 4)] public string questionTranscript;
        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Correct Answer (Braille: Dot1=A, Dot2=B, Dot3=C)")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Header("Feedback")]
        public AudioClip successAudio;

        [Header("Support (falls back to the lesson's Support Audio if empty)")]
        public AudioClip supportAudio;
    }

    [Serializable]
    public class TrueFalseQuestion
    {
        [Tooltip("Optional note for authors / debug log only.")]
        [TextArea(2, 4)] public string questionTranscript;
        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Correct Answer (Braille: Dot1=True, Dot2=False)")]
        public TrueFalseChoice correctAnswer = TrueFalseChoice.True;

        [Header("Feedback")]
        public AudioClip successAudio;

        [Header("Support (falls back to the lesson's Support Audio if empty)")]
        public AudioClip supportAudio;
    }

    [Serializable]
    public class LessonQuiz
    {
        [Tooltip("For your own reference. Order MUST match LessonController13.lessons.")]
        public string lessonName;

        [Header("Multiple Choice (asked first)")]
        public List<QuizQuestion> questions = new List<QuizQuestion>();

        [Header("True or False (asked after Multiple Choice)")]
        public List<TrueFalseQuestion> trueFalseQuestions = new List<TrueFalseQuestion>();

        [Header("Support After Mistakes (lesson-level fallback)")]
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("References")]
    public LessonController13 lessonController;
    public QuizResultReporter resultReporter;

    [Header("Quiz Data (one entry per lesson, same order as LessonController13.lessons)")]
    public List<LessonQuiz> lessonQuizzes = new List<LessonQuiz>();

    [Header("Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    [Tooltip("Points are deducted once per this many total wrong answers (was hard-coded to 3).")]
    public int wrongAnswersPerDeduction = 3;
    public string highScoreKey = "Lesson13HighScore";

    [Header("Feedback Audio")]
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Repeat-Story Prompt (after every wrong answer)")]
    public AudioClip repeatStoryPromptAudio;

    [Header("Repeat-Story Confirmation (after the Repeat button replays the story)")]
    public AudioClip repeatQuestionConfirmAudio;

    [Header("Final Score Audio")]
    public AudioClip genericCompletedAudio;
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;
    [Tooltip("Index = number. Needs entries 0-100.")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Play Again? (played after the high score announcement)")]
    [Tooltip("e.g. \"Do you want to repeat again? Press Repeat to play again or Next to finish.\"")]
    public AudioClip repeatQuestionAudio;

    [Header("Flow Settings")]
    public float delayAfterCorrect = 0.75f;
    [Tooltip("Consecutive wrong answers on the same question before the Support audio plays. The repeat-story prompt still plays after EVERY wrong answer.")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private enum QuizState
    {
        Inactive,                   // lesson phase, or between lessons
        AwaitingAnswer,             // question audio playing / listening for Braille answer
        CorrectFeedback,            // success audio playing (Repeat ignored)
        WrongFeedback,              // wrong / support / repeat-story prompt playing
        AwaitingRepeatConfirmation, // Next re-asks the question, Repeat replays the story
        AwaitingEndDecision         // final prompt: Repeat = play again, Next = finish
    }

    private QuizState state = QuizState.Inactive;

    private int currentLessonIndex = -1;
    private int currentQuestionIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private Coroutine flowRoutine;

    private struct ActiveQuestionData
    {
        public string questionTranscript;
        public AudioClip questionAudio;
        public AudioClip soundEffectAudio;
        public AudioClip successAudio;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

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
        if (lessonController == null)
            Debug.LogError("[QuizController13] lessonController is not assigned.");
        else if (lessonQuizzes.Count != lessonController.LessonCount)
            Debug.LogWarning($"[QuizController13] lessonQuizzes ({lessonQuizzes.Count}) and lessons ({lessonController.LessonCount}) have different counts. They must match by index.");

        ResetQuizScore();
    }

    // -------------------------------------------------------------------------
    // Public API (used by LessonController13)
    // -------------------------------------------------------------------------

    /// <summary>Called by the lesson once its prompt + story are finished.</summary>
    public void BeginQuestions(int lessonIndex)
    {
        currentLessonIndex = lessonIndex;
        currentQuestionIndex = 0;
        currentMistakeCount = 0;

        if (logDebug)
            Debug.Log($"[Quiz] Starting questions for lesson {lessonIndex}");

        RunFlow(AskQuestion(currentQuestionIndex));
    }

    /// <summary>Called by the lesson after the last lesson has finished.</summary>
    public void BeginFinalResults()
    {
        RunFlow(FinalizeQuiz());
    }

    // -------------------------------------------------------------------------
    // Flow helper
    // -------------------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        StopFlow();
        lessonController.StopVoice();
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

    private void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    private void AddMistake()
    {
        totalWrongCount++;

        int deductions = totalWrongCount / Mathf.Max(1, wrongAnswersPerDeduction);
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        if (logDebug)
            Debug.Log($"[AddMistake] totalWrongCount={totalWrongCount}, deductions={deductions}, totalScore={totalScore}");
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
    // Question lookup (Multiple Choice first, then True/False)
    // -------------------------------------------------------------------------

    private LessonQuiz GetLessonQuiz(int lessonIndex)
    {
        if (lessonQuizzes == null || lessonIndex < 0 || lessonIndex >= lessonQuizzes.Count)
            return null;
        return lessonQuizzes[lessonIndex];
    }

    private int GetTotalQuestionCount(LessonQuiz quiz)
    {
        if (quiz == null) return 0;
        return (quiz.questions?.Count ?? 0) + (quiz.trueFalseQuestions?.Count ?? 0);
    }

    private ActiveQuestionData GetActiveQuestionData(LessonQuiz quiz, int index)
    {
        int mcCount = quiz.questions?.Count ?? 0;

        if (index < mcCount)
        {
            QuizQuestion q = quiz.questions[index];
            return new ActiveQuestionData
            {
                questionTranscript = q.questionTranscript,
                questionAudio = q.questionAudio,
                soundEffectAudio = q.soundEffectAudio,
                successAudio = q.successAudio,
                supportAudio = q.supportAudio
            };
        }

        TrueFalseQuestion tf = quiz.trueFalseQuestions[index - mcCount];
        return new ActiveQuestionData
        {
            questionTranscript = tf.questionTranscript,
            questionAudio = tf.questionAudio,
            soundEffectAudio = tf.soundEffectAudio,
            successAudio = tf.successAudio,
            supportAudio = tf.supportAudio
        };
    }

    // -------------------------------------------------------------------------
    // Asking a question
    // -------------------------------------------------------------------------

    private IEnumerator AskQuestion(int questionIndex)
    {
        LessonQuiz quiz = GetLessonQuiz(currentLessonIndex);

        if (questionIndex < 0 || questionIndex >= GetTotalQuestionCount(quiz))
        {
            // No (more) questions -> lesson complete.
            CompleteCurrentLesson();
            yield break;
        }

        state = QuizState.AwaitingAnswer;
        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        if (data.soundEffectAudio != null)
        {
            yield return lessonController.PlayVoice(data.soundEffectAudio);
            yield return new WaitForSeconds(lessonController.DelayAfterVoice);
        }

        yield return lessonController.PlayVoice(data.questionAudio, data.questionTranscript);
    }

    private void CompleteCurrentLesson()
    {
        state = QuizState.Inactive;
        lessonController.AdvanceToNextLesson();
    }

    // -------------------------------------------------------------------------
    // Input: Braille answers
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string pattern)
    {
        if (state != QuizState.AwaitingAnswer) return;

        LessonQuiz quiz = GetLessonQuiz(currentLessonIndex);
        if (quiz == null) return;

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

        if (isCorrect)
        {
            currentMistakeCount = 0;
            state = QuizState.CorrectFeedback;
            RunFlow(HandleCorrectAnswer(currentQuestionIndex));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();
            state = QuizState.WrongFeedback;
            RunFlow(HandleWrongAnswer(currentQuestionIndex));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    private IEnumerator HandleCorrectAnswer(int questionIndex)
    {
        SaveHighScoreIfNeeded();

        LessonQuiz quiz = GetLessonQuiz(currentLessonIndex);
        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        AudioClip clip = data.successAudio != null ? data.successAudio : genericCorrectAudio;

        yield return lessonController.PlayVoice(clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentQuestionIndex++;

        if (currentQuestionIndex >= GetTotalQuestionCount(quiz))
            CompleteCurrentLesson();
        else
            RunFlow(AskQuestion(currentQuestionIndex));
    }

    private IEnumerator HandleWrongAnswer(int questionIndex)
    {
        LessonQuiz quiz = GetLessonQuiz(currentLessonIndex);
        ActiveQuestionData data = GetActiveQuestionData(quiz, questionIndex);

        yield return lessonController.PlayVoice(genericTryAgainAudio);
        yield return new WaitForSeconds(lessonController.DelayAfterVoice);

        if (currentMistakeCount >= mistakesBeforeSupport)
        {
            AudioClip supportClip = data.supportAudio != null ? data.supportAudio : quiz.supportAudio;

            if (supportClip != null)
            {
                yield return lessonController.PlayVoice(supportClip);
                yield return new WaitForSeconds(lessonController.DelayAfterVoice);
            }

            if (resetMistakesAfterSupport)
                currentMistakeCount = 0;
        }

        // Ask whether to hear the story again, after EVERY wrong answer.
        state = QuizState.AwaitingRepeatConfirmation;
        yield return lessonController.PlayVoice(repeatStoryPromptAudio);

        // Stays here until the player presses Repeat or Next.
    }

    // -------------------------------------------------------------------------
    // Input: Repeat / Next
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        switch (state)
        {
            case QuizState.AwaitingEndDecision:
                // Player wants to play again: reset score, restart at lesson 0.
                StopFlow();
                lessonController.StopVoice();
                ResetQuizScore();
                state = QuizState.Inactive;
                lessonController.RestartFromFirstLesson();
                return;

            case QuizState.AwaitingAnswer:
            case QuizState.WrongFeedback:
            case QuizState.AwaitingRepeatConfirmation:
                // Replay the story only. Question index, score, and mistake count are untouched.
                state = QuizState.AwaitingRepeatConfirmation;
                RunFlow(RepeatStorySection());
                return;
        }
    }

    private IEnumerator RepeatStorySection()
    {
        yield return lessonController.PlayStory(currentLessonIndex);
        yield return new WaitForSeconds(lessonController.DelayAfterVoice);

        yield return lessonController.PlayVoice(repeatQuestionConfirmAudio);

        // Stays here until the player presses Next.
    }

    private void HandleNext()
    {
        switch (state)
        {
            case QuizState.AwaitingRepeatConfirmation:
                RunFlow(AskQuestion(currentQuestionIndex));
                return;

            case QuizState.AwaitingEndDecision:
                FinishAndReturn();
                return;
        }
    }

    // -------------------------------------------------------------------------
    // Final results
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuiz()
    {
        state = QuizState.Inactive;

        SaveHighScoreIfNeeded();

        if (logDebug)
            Debug.Log($"[Quiz] Your score is {totalScore}, while your highest score is {highScore}.");

        yield return lessonController.PlayVoice(genericCompletedAudio);
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(lessonController.DelayAfterVoice);

        // After the high score: ask whether to play again. The player can answer
        // while this prompt is still playing.
        state = QuizState.AwaitingEndDecision;
        yield return lessonController.PlayVoice(repeatQuestionAudio);

        // Stays here until the player presses Repeat (play again) or Next (finish).
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        AudioClip[] sequence =
        {
            yourScoreIsAudio,
            GetNumberAudio(totalScore),
            whileYourHighestScoreIsAudio,
            GetNumberAudio(highScore)
        };

        foreach (AudioClip clip in sequence)
        {
            if (clip != null)
                yield return lessonController.PlayVoice(clip);
        }
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    private void FinishAndReturn()
    {
        state = QuizState.Inactive;
        StopFlow();
        lessonController.StopVoice();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController13] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }
}