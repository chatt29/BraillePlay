using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lesson 15 – Quiz (type each word in Braille) + scoring.
/// Starts when LessonController15 fires OnLessonCompleted.
/// Audio + Braille input only.
/// </summary>
public class QuizController15 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz data – one entry per WORD
    // -------------------------------------------------------------------------
    [Serializable]
    public class BrailleWordLesson
    {
        [Header("Target Word")]
        [Tooltip("The word to type. Capitalize only the first letter (e.g. 'Hit', 'Pig', 'Bin').")]
        public string word = "Hit";

        [Header("Transcripts (debug log only)")]
        [TextArea(2, 4)] public string promptMessage;
        [TextArea(2, 4)] public string successMessage;
        [TextArea(2, 4)] public string wrongMessage;
        [TextArea(2, 4)] public string supportMessage;

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Support After Mistakes")]
        public AudioClip supportAudio;

        public List<string> GetTargetPatterns()
        {
            var patterns = new List<string>();
            if (string.IsNullOrEmpty(word)) return patterns;

            bool first = true;

            foreach (char c in word)
            {
                if (!char.IsLetter(c)) continue;

                if (first)
                {
                    patterns.Add(LessonController15.BrailleCapitalIndicatorPattern);
                    first = false;
                }

                char upper = char.ToUpperInvariant(c);

                if (LessonController15.BrailleAlphabetPatterns.TryGetValue(upper, out string pattern))
                    patterns.Add(pattern);
            }

            return patterns;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Lesson Link")]
    [Tooltip("The quiz starts when this lesson fires OnLessonCompleted.")]
    public LessonController15 lessonController;

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "Lesson15_RimesHighScore";

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Scene Text (transcripts – debug log only)")]
    [TextArea(2, 5)] public string welcomeMessage = "Welcome to Lesson 15: I Word Families!";
    [TextArea(2, 5)] public string letsLearnMessage = "Let's find the letter I in the middle of words.";
    [TextArea(2, 5)] public string completedMessage = "Amazing! You spelled all the -it, -ip, -ig, -id, -in, and -ill words!";
    [TextArea(2, 5)] public string repeatQuestionMessage = "You finished Lesson 15. Do you want to practice again? Press R to repeat or Y to finish.";

    [Header("Quiz Flow - Words to Type")]
    public List<BrailleWordLesson> lessons = new List<BrailleWordLesson>();
    public float delayAfterVoice = 0.35f;
    [Tooltip("Wait time used when a message has no audio clip.")]
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool questionActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForWordInput = false;
    private bool waitingForCapitalIndicator = true;

    private readonly List<string> currentTypedPatterns = new List<string>();

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------
    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;

        if (lessonController != null)
            lessonController.OnLessonCompleted += BeginQuiz;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;

        if (lessonController != null)
            lessonController.OnLessonCompleted -= BeginQuiz;
    }

    private void Start()
    {
        if (lessonController == null)
            Debug.LogWarning("[Quiz15] No LessonController15 assigned - quiz will never start.");

        ResetQuizScore();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------
    public void BeginQuiz()
    {
        if (logDebug)
            Debug.Log("[Quiz15] Quiz starting.");

        RunFlow(StartQuizFlow());
    }

    // -------------------------------------------------------------------------
    // Scoring
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
    // Flow
    // -------------------------------------------------------------------------
    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    private IEnumerator StartQuizFlow()
    {
        questionActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForWordInput = false;

        ResetQuizScore();

        yield return PlaySpeech(welcomeAudio, welcomeMessage, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlaySpeech(letsLearnAudio, letsLearnMessage, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartQuestion(0);
    }

    private void StartQuestion(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(CompleteQuiz());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        questionActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForWordInput = false;
        currentTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"[Quiz15] Starting word {currentLessonIndex}: {lessons[currentLessonIndex].word}");

        RunFlow(PlayQuestionFromBeginning(lessons[currentLessonIndex]));
    }

    private IEnumerator PlayQuestionFromBeginning(BrailleWordLesson lesson)
    {
        currentTypedPatterns.Clear();

        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskForWordInput();
    }

    private IEnumerator ShowPromptMessage(BrailleWordLesson lesson)
    {
        string intro = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : $"Can you type the word {lesson.word}?";

        yield return PlaySpeechSequence(intro, noAudioTextDelay, lesson.introAudio, lesson.instructionAudio);
    }

    private IEnumerator AskForWordInput()
    {
        currentTypedPatterns.Clear();
        waitingForCapitalIndicator = true;
        waitingForWordInput = true;
        yield break;
    }

    // -------------------------------------------------------------------------
    // Braille input
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!questionActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForWordInput)
            HandleWordLetterInput(submittedPattern);
    }

    private void HandleWordLetterInput(string pattern)
    {
        if (!waitingForWordInput) return;

        BrailleWordLesson lesson = lessons[currentLessonIndex];
        List<string> targetPatterns = lesson.GetTargetPatterns();

        if (waitingForCapitalIndicator)
        {
            if (pattern != LessonController15.BrailleCapitalIndicatorPattern)
            {
                waitingForWordInput = false;
                currentMistakeCount++;
                AddMistake();
                RunFlow(HandleWrongAnswer(lesson));
                return;
            }

            waitingForCapitalIndicator = false;
            currentTypedPatterns.Add(pattern);
            return;
        }

        currentTypedPatterns.Add(pattern);

        if (currentTypedPatterns.Count < targetPatterns.Count)
            return;

        waitingForWordInput = false;

        bool isCorrect = currentTypedPatterns.Count == targetPatterns.Count;
        if (isCorrect)
        {
            for (int i = 0; i < targetPatterns.Count; i++)
            {
                if (currentTypedPatterns[i] != targetPatterns[i])
                {
                    isCorrect = false;
                    break;
                }
            }
        }

        if (isCorrect)
        {
            currentMistakeCount = 0;
            questionActive = false;
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
    // Answer handlers
    // -------------------------------------------------------------------------
    private IEnumerator HandleCorrectAnswer(BrailleWordLesson lesson)
    {
        SaveHighScoreIfNeeded();

        string message = !string.IsNullOrWhiteSpace(lesson.successMessage)
            ? lesson.successMessage
            : $"Correct! That is {lesson.word}.";

        AudioClip clip = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return PlaySpeech(clip, message, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartQuestion(currentLessonIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(BrailleWordLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.wrongMessage)
            ? lesson.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.wrongAudio != null ? lesson.wrongAudio : genericTryAgainAudio;

        yield return PlaySpeech(clip, message, noAudioTextDelay);

        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskForWordInput();
    }

    private IEnumerator HandleSupportThenRetry(BrailleWordLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.supportMessage)
            ? lesson.supportMessage
            : $"Here is some help. Listen carefully and try typing {lesson.word} again.";

        yield return PlaySpeech(lesson.supportAudio, message, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskForWordInput();
    }

    // -------------------------------------------------------------------------
    // Repeat / Next (Y)
    // -------------------------------------------------------------------------
    private void HandleRepeat()
    {
        // After the final score: R = play the quiz again
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartQuestion(0);
            return;
        }

        // During a question: R = replay the current prompt
        if (!questionActive) return;
        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count) return;

        BrailleWordLesson lesson = lessons[currentLessonIndex];

        waitingForWordInput = false;
        currentMistakeCount = 0;
        currentTypedPatterns.Clear();

        RunFlow(PlayQuestionFromBeginning(lesson));
    }

    private void HandleNext()
    {
        // After the final score: Y = finish and return to the menu
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinishQuiz());
        }
    }

    // -------------------------------------------------------------------------
    // Completion
    // -------------------------------------------------------------------------
    private IEnumerator CompleteQuiz()
    {
        questionActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // 1. "Amazing! You spelled all the words!"
        yield return PlaySpeech(genericCompletedAudio, completedMessage, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // 3. Only now does the learner get to decide: R = again, Y = finish
        waitingForRepeatChoice = true;
        yield return PlaySpeech(repeatQuestionAudio, repeatQuestionMessage, noAudioTextDelay);
    }

    private IEnumerator FinishQuiz()
    {
        sceneFinished = true;
        questionActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController15] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");

        yield break;
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        yield return PlayClipAndWait(yourScoreIsAudio);
        yield return PlayClipAndWait(finalScoreClip);
        yield return PlayClipAndWait(whileYourHighestScoreIsAudio);
        yield return PlayClipAndWait(highScoreClip);
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------------------------
    private IEnumerator PlayClipAndWait(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null) yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    private IEnumerator PlaySpeech(AudioClip clip, string transcript, float fallbackWait)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[Quiz15] {transcript}");

        if (clip != null && voiceAudioSource != null)
            yield return PlayClipAndWait(clip);
        else
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator PlaySpeechSequence(string transcript, float fallbackWait, params AudioClip[] clips)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[Quiz15] {transcript}");

        bool playedAny = false;

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;
                playedAny = true;
                yield return PlayClipAndWait(clip);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}