using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUIZ PART of the scene (audio + Braille input only, no visuals).
///
/// Waits silently until LessonController12 raises OnLessonPagesFinished, then:
///   1. Welcome audio, "Let's learn" audio
///   2. For each quiz word: prompt audio -> learner types the WHOLE word in Braille
///      (capital indicator first, then one cell per letter) -> validated only when
///      every cell has been entered -> success / wrong / support audio
///   3. Completed audio -> score + high score announced -> repeat-question audio
///   4. Learner decides:
///        Repeat -> score is reset and the quiz starts again from the first word
///        Next   -> score is reported and the game returns to the menu
///
/// This class owns all scoring and the high score.
/// </summary>
public class QuizController12 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz data: one entry per WORD
    // -------------------------------------------------------------------------

    [Serializable]
    public class QuizWord
    {
        [Tooltip("The word to type. Capitalize only the first letter (e.g. 'Bell', 'Cat', 'Apple').")]
        public string word = "Bell";

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Support After Mistakes")]
        public AudioClip supportAudio;

        /// <summary>
        /// The expected Braille pattern for each cell of the word, in order.
        /// The capital indicator (Dot 6) always comes first, then one pattern per letter.
        /// </summary>
        public List<string> GetTargetPatterns()
        {
            var patterns = new List<string>();

            if (string.IsNullOrEmpty(word))
                return patterns;

            bool first = true;

            foreach (char c in word)
            {
                if (!char.IsLetter(c))
                    continue;

                if (first)
                {
                    patterns.Add(LessonController12.BrailleCapitalIndicatorPattern);
                    first = false;
                }

                char upper = char.ToUpperInvariant(c);

                if (LessonController12.BrailleAlphabetPatterns.TryGetValue(upper, out string pattern))
                    patterns.Add(pattern);
            }

            return patterns;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Lesson Link")]
    [Tooltip("The quiz begins when this lesson controller finishes (learner presses Next at the lesson choice).")]
    public LessonController12 lessonController;

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;

    [Tooltip("Played AFTER the score and high score are announced: 'Press repeat to play again or next to finish.'")]
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100 (index = number)")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Quiz Words to Type")]
    public List<QuizWord> quizWords = new List<QuizWord>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("Silent wait used when an audio clip is missing, so the flow keeps its pacing.")]
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

    private int currentWordIndex = -1;
    private int currentMistakeCount = 0;   // consecutive mistakes on the current word (drives support)
    private int totalWrongCount = 0;       // all mistakes in this quiz run (drives score)
    private int totalScore = 100;
    private int highScore = 0;

    private bool wordActive = false;              // a quiz word is in progress
    private bool waitingForWordAnswer = false;    // ready to receive Braille cells
    private bool waitingForRepeatChoice = false;  // end-of-quiz: Repeat or Next
    private bool waitingForCapitalIndicator = true;

    // Braille cells submitted so far for the word being typed, in order.
    private readonly List<string> currentTypedPatterns = new List<string>();

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;

        if (lessonController != null)
            lessonController.OnLessonPagesFinished += BeginQuiz;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;

        if (lessonController != null)
            lessonController.OnLessonPagesFinished -= BeginQuiz;
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("QuizController12 ready (waiting for the lesson to finish).");

        if (lessonController == null)
            Debug.LogWarning("[QuizController12] No LessonController12 assigned - the quiz will only start if BeginQuiz() is called from elsewhere.");

        ResetQuizScore();
    }

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------

    /// <summary>Starts the quiz: welcome audio, "let's learn" audio, then the first word.</summary>
    public void BeginQuiz()
    {
        wordActive = false;
        waitingForWordAnswer = false;
        waitingForRepeatChoice = false;

        RunFlow(StartQuizAfterLesson());
    }

    private IEnumerator StartQuizAfterLesson()
    {
        yield return PlayVoice(noAudioTextDelay, welcomeAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(noAudioTextDelay, letsLearnAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        StartQuizWord(0);
    }

    // -------------------------------------------------------------------------
    // Coroutine / audio helpers
    // -------------------------------------------------------------------------

    /// <summary>Stops the current flow coroutine (if any) and starts a new one.</summary>
    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    /// <summary>
    /// Plays the given clips one after another and waits for each to finish.
    /// If nothing could be played (no clips / no AudioSource), waits fallbackWait
    /// so the flow keeps its pacing.
    /// </summary>
    private IEnumerator PlayVoice(float fallbackWait, params AudioClip[] clips)
    {
        bool playedAny = false;

        if (voiceAudioSource != null && clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                playedAny = true;

                yield return new WaitForSeconds(clip.length);
            }
        }

        if (!playedAny && fallbackWait > 0f)
            yield return new WaitForSeconds(fallbackWait);
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
    // Quiz word sequence
    //   1. Prompt audio (intro + instruction)  -> "Can you type the word Bell?"
    //   2. Wait for the player to type the WHOLE word, cell by cell
    //   3. Success audio   -> HandleCorrectAnswer
    //   4. Wrong audio     -> HandleWrongAnswer
    //   5. Support audio   -> only after 'mistakesBeforeSupport' consecutive mistakes
    // -------------------------------------------------------------------------

    private void StartQuizWord(int index)
    {
        if (index < 0 || index >= quizWords.Count)
        {
            RunFlow(CompleteQuiz());
            return;
        }

        currentWordIndex = index;
        currentMistakeCount = 0;
        wordActive = true;
        waitingForWordAnswer = false;
        waitingForRepeatChoice = false;
        currentTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"Starting quiz word {currentWordIndex}: {quizWords[currentWordIndex].word}");

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private IEnumerator PlayWordFromBeginning(QuizWord quizWord)
    {
        currentTypedPatterns.Clear();

        yield return PlayPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    private IEnumerator PlayPrompt(QuizWord quizWord)
    {
        yield return PlayVoice(noAudioTextDelay, quizWord.introAudio, quizWord.instructionAudio);
    }

    /// <summary>
    /// Clears the in-progress buffer and starts listening for Braille cells, so the
    /// word is typed (and later validated) from scratch. Used for the first ask and
    /// every re-ask after a wrong answer or a support message.
    /// </summary>
    private void AskForWordInput()
    {
        currentTypedPatterns.Clear();
        waitingForCapitalIndicator = true;
        waitingForWordAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!waitingForWordAnswer)
            return;

        HandleWordLetterInput(submittedPattern);
    }

    /// <summary>
    /// Accumulates one completed Braille cell into the word being typed. The word
    /// is validated only once as many cells have been entered as the target needs.
    /// </summary>
    private void HandleWordLetterInput(string pattern)
    {
        QuizWord quizWord = quizWords[currentWordIndex];
        List<string> targetPatterns = quizWord.GetTargetPatterns();

        // First input must be the capital indicator.
        if (waitingForCapitalIndicator)
        {
            if (pattern != LessonController12.BrailleCapitalIndicatorPattern)
            {
                waitingForWordAnswer = false;
                RegisterWrongAnswer(quizWord);
                return;
            }

            waitingForCapitalIndicator = false;
            currentTypedPatterns.Add(pattern);
            return;
        }

        currentTypedPatterns.Add(pattern);

        if (currentTypedPatterns.Count < targetPatterns.Count)
            return; // still typing the word

        // Whole word typed: validate now.
        waitingForWordAnswer = false;

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
            wordActive = false;
            RunFlow(HandleCorrectAnswer(quizWord));
        }
        else
        {
            RegisterWrongAnswer(quizWord);
        }
    }

    /// <summary>Counts the mistake (streak + score) and plays wrong or support feedback.</summary>
    private void RegisterWrongAnswer(QuizWord quizWord)
    {
        currentMistakeCount++;
        AddMistake();

        if (currentMistakeCount >= mistakesBeforeSupport)
            RunFlow(HandleSupportThenRetry(quizWord));
        else
            RunFlow(HandleWrongAnswer(quizWord));
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    private IEnumerator HandleCorrectAnswer(QuizWord quizWord)
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = quizWord.successAudio != null ? quizWord.successAudio : genericCorrectAudio;

        yield return PlayVoice(noAudioTextDelay, clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartQuizWord(currentWordIndex + 1);
    }

    /// <summary>Wrong audio, then re-ask the same word (no full restart).</summary>
    private IEnumerator HandleWrongAnswer(QuizWord quizWord)
    {
        AudioClip clip = quizWord.wrongAudio != null ? quizWord.wrongAudio : genericTryAgainAudio;

        yield return PlayVoice(noAudioTextDelay, clip);

        // Restate which word to type before listening again.
        yield return PlayPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    /// <summary>After enough consecutive mistakes: support audio, reset the streak, re-ask the word.</summary>
    private IEnumerator HandleSupportThenRetry(QuizWord quizWord)
    {
        yield return PlayVoice(noAudioTextDelay, quizWord.supportAudio);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return PlayPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    // -------------------------------------------------------------------------
    // Quiz completion:
    //   completed audio -> score + high score -> repeat-question audio -> choice
    // -------------------------------------------------------------------------

    private IEnumerator CompleteQuiz()
    {
        wordActive = false;
        waitingForWordAnswer = false;
        waitingForRepeatChoice = false;

        // Save first so the announced high score already includes this run.
        SaveHighScoreIfNeeded();

        yield return PlayVoice(noAudioTextDelay, genericCompletedAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // The flag is set right before the question so the learner can answer
        // (Repeat / Next) even while the question is still playing.
        waitingForRepeatChoice = true;
        yield return PlayVoice(noAudioTextDelay, repeatQuestionAudio);
    }

    private IEnumerator FinishQuiz()
    {
        wordActive = false;
        waitingForWordAnswer = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController12] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");

        yield break;
    }

    // -------------------------------------------------------------------------
    // Final score audio: "Your score is <N> while your highest score is <M>"
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        yield return PlayVoice(0f, yourScoreIsAudio);
        yield return PlayVoice(0f, GetNumberAudio(totalScore));
        yield return PlayVoice(0f, whileYourHighestScoreIsAudio);
        yield return PlayVoice(0f, GetNumberAudio(highScore));
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        // End of quiz: play the quiz again from the first word.
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartQuizWord(0);
            return;
        }

        // Ignore Repeat while no word is active. This includes the short window after a
        // correct answer, where a Repeat would stop the in-flight HandleCorrectAnswer
        // before it advances and replay the just-answered word instead.
        if (!wordActive)
            return;

        if (currentWordIndex < 0 || currentWordIndex >= quizWords.Count)
            return;

        // Restart the current word so it plays exactly like a fresh start.
        waitingForWordAnswer = false;
        currentMistakeCount = 0;
        currentTypedPatterns.Clear();

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private void HandleNext()
    {
        // End of quiz: the learner chose to finish.
        if (waitingForRepeatChoice)
            RunFlow(FinishQuiz());
    }
}