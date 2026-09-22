using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QuizController9 — the QUIZ phase of Lesson 9 (audio + Braille input only, no visuals).
///
/// Started by LessonController9 through BeginQuiz().
///
/// Flow:
///   Welcome audio -> "Let's type some words" audio
///     -> for each word: prompt -> learner types the whole word -> success / wrong / support
///     -> final score + high score announcement
///     -> repeat-question audio ("Press R to repeat or Y to finish")
///        Repeat -> reset the score and replay the quiz     Next/Yes -> report the score and return
///
/// This script owns everything about scoring.
/// </summary>
public class QuizController9 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz Data — one entry per WORD. The learner types every letter, one
    // Braille cell at a time, starting with the capital indicator (Dot 6).
    // The whole word is only validated once every entry has been typed.
    // Message strings are transcripts only (Console log when Log Debug is on).
    // -------------------------------------------------------------------------

    [Serializable]
    public class QuizWord
    {
        [Tooltip("The word to type. Capitalize only the first letter (e.g. 'Bell', 'Cat', 'Apple').")]
        public string word = "Bell";

        [Header("Prompt Audio")]
        [TextArea(2, 4)] public string promptMessage;
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        [TextArea(2, 4)] public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)] public string wrongMessage;
        public AudioClip wrongAudio;

        [Header("Support After Mistakes")]
        [TextArea(2, 4)] public string supportMessage;
        public AudioClip supportAudio;

        /// <summary>Capital indicator followed by one pattern per letter.</summary>
        public List<string> GetTargetPatterns()
            => LessonController9.BuildTargetPatterns(word, true);
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Quiz Words")]
    public List<QuizWord> quizWords = new List<QuizWord>();

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    [Tooltip("Played after the score announcement: 'Do you want to repeat? Press R to repeat or Y to finish.'")]
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Transcripts (Console log only)")]
    [TextArea(2, 5)] public string welcomeMessage = "Welcome to Braille Sounds Around!";
    [TextArea(2, 5)] public string letsLearnMessage = "Let's type some words.";
    [TextArea(2, 5)] public string repeatQuestionMessage = "You finished the quiz. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("Pause used instead of audio when a clip is missing.")]
    public float missingAudioPause = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Score Settings")]
    public int fixedScore = 100;
    [Tooltip("Points removed for every 3 wrong answers (the same rule the original script used).")]
    public int deductionPerMistake = 1;
    public string highScoreKey = "RimesEmEllEbHighScore";

    [Header("Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private const int MistakesPerDeduction = 3;

    private int currentWordIndex = -1;
    private int currentMistakeCount;
    private int totalWrongCount;
    private int totalScore = 100;
    private int highScore;

    private bool quizRunning;               // BeginQuiz() was called and the quiz hasn't ended yet
    private bool wordActive;                // a word is in progress (false during the correct-answer transition)
    private bool quizFinished;              // all words done
    private bool waitingForWordInput;
    private bool waitingForCapitalIndicator = true;
    private bool waitingForRepeatChoice;

    // Braille entries typed so far for the current word (capital indicator included).
    private readonly List<string> currentTypedPatterns = new List<string>();

    private Coroutine flowRoutine;

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

    // -------------------------------------------------------------------------
    // Public entry point — called by LessonController9
    // -------------------------------------------------------------------------

    /// <summary>Starts the quiz from the welcome audio. Ignored if the quiz is already running.</summary>
    public void BeginQuiz()
    {
        if (quizRunning) return;

        if (logDebug)
            Debug.Log("QuizController9: quiz starting.");

        quizRunning = true;
        quizFinished = false;
        wordActive = false;
        waitingForWordInput = false;
        waitingForRepeatChoice = false;

        ResetQuizScore();
        RunFlow(StartQuizIntro());
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

        int deductions = totalWrongCount / MistakesPerDeduction;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        if (logDebug)
            Debug.Log($"Mistakes: {totalWrongCount}, score: {totalScore}");
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
    // Coroutine helpers
    // -------------------------------------------------------------------------

    /// <summary>Stops the current flow coroutine and any audio it was playing.</summary>
    private void StopFlow()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    /// <summary>Replaces whatever flow is running with a new one.</summary>
    private void RunFlow(IEnumerator routine)
    {
        StopFlow();
        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------------------------

    private void LogTranscript(string transcript)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[QuizController9] {transcript}");
    }

    /// <summary>Plays one clip and waits for it to finish. Does nothing if the clip is missing.</summary>
    private IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null) yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    /// <summary>Plays a clip; if it is missing, waits missingAudioPause so pacing survives.</summary>
    private IEnumerator Speak(string transcript, AudioClip clip)
    {
        LogTranscript(transcript);

        if (clip != null && voiceAudioSource != null)
            yield return PlayClip(clip);
        else
            yield return new WaitForSeconds(missingAudioPause);
    }

    /// <summary>Plays several clips back to back; if none can play, waits missingAudioPause.</summary>
    private IEnumerator SpeakSequence(string transcript, params AudioClip[] clips)
    {
        LogTranscript(transcript);

        bool playedAny = false;

        if (voiceAudioSource != null && clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                playedAny = true;
                yield return PlayClip(clip);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(missingAudioPause);
    }

    // -------------------------------------------------------------------------
    // Quiz intro
    // -------------------------------------------------------------------------

    private IEnumerator StartQuizIntro()
    {
        yield return Speak(welcomeMessage, welcomeAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return Speak(letsLearnMessage, letsLearnAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        StartWord(0);
    }

    // -------------------------------------------------------------------------
    // Quiz word sequence
    //
    //   1. Prompt (intro + instruction audio)  e.g. "Can you type the word Bell?"
    //   2. Wait for the WHOLE word, one Braille cell at a time
    //      (capital indicator first, validated only when every entry is in)
    //   3. Success -> next word          4. Wrong -> re-ask the same word
    //   5. Support audio after 3 consecutive mistakes, then re-ask
    // -------------------------------------------------------------------------

    private void StartWord(int index)
    {
        if (index < 0 || index >= quizWords.Count)
        {
            RunFlow(FinishQuiz());
            return;
        }

        if (string.IsNullOrWhiteSpace(quizWords[index].word))
        {
            // Safety net: a blank word could never be answered.
            Debug.LogWarning($"[QuizController9] Quiz word {index} is empty. Skipping it.");
            StartWord(index + 1);
            return;
        }

        currentWordIndex = index;
        currentMistakeCount = 0;
        wordActive = true;
        quizFinished = false;
        waitingForRepeatChoice = false;
        waitingForWordInput = false;
        currentTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"Starting word {currentWordIndex}: {quizWords[currentWordIndex].word}");

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private IEnumerator PlayWordFromBeginning(QuizWord quizWord)
    {
        currentTypedPatterns.Clear();

        yield return SpeakPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        BeginListeningForWord();
    }

    /// <summary>Prompt message together with its intro + instruction audio.</summary>
    private IEnumerator SpeakPrompt(QuizWord quizWord)
    {
        yield return SpeakSequence(quizWord.promptMessage, quizWord.introAudio, quizWord.instructionAudio);
    }

    /// <summary>
    /// Clears the letter buffer and starts accepting Braille input for the
    /// current word. Used for the first ask and every re-ask afterwards.
    /// </summary>
    private void BeginListeningForWord()
    {
        currentTypedPatterns.Clear();
        waitingForCapitalIndicator = true;
        waitingForWordInput = true;
    }

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizRunning || !waitingForWordInput)
            return;

        HandleWordLetterInput(submittedPattern);
    }

    /// <summary>
    /// Accumulates one completed Braille cell into the word being typed. The
    /// word is validated only once as many entries have been typed as needed.
    /// </summary>
    private void HandleWordLetterInput(string pattern)
    {
        QuizWord quizWord = quizWords[currentWordIndex];
        List<string> targetPatterns = quizWord.GetTargetPatterns();

        // The first input must be the capital indicator.
        if (waitingForCapitalIndicator)
        {
            if (pattern != LessonController9.BrailleCapitalIndicatorPattern)
            {
                waitingForWordInput = false;
                RegisterMistake(quizWord);
                return;
            }

            waitingForCapitalIndicator = false;
            currentTypedPatterns.Add(pattern);
            return;
        }

        currentTypedPatterns.Add(pattern);

        if (currentTypedPatterns.Count < targetPatterns.Count)
            return; // still typing — wait for the remaining letters

        waitingForWordInput = false;

        if (LessonController9.PatternsMatch(currentTypedPatterns, targetPatterns))
        {
            currentMistakeCount = 0;
            wordActive = false;
            RunFlow(HandleCorrectAnswer(quizWord));
        }
        else
        {
            RegisterMistake(quizWord);
        }
    }

    /// <summary>Counts a wrong answer, then plays either the support flow or the plain wrong flow.</summary>
    private void RegisterMistake(QuizWord quizWord)
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
        AudioClip clip = quizWord.successAudio != null ? quizWord.successAudio : genericCorrectAudio;

        yield return Speak(quizWord.successMessage, clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartWord(currentWordIndex + 1);
    }

    /// <summary>Wrong message, then re-ask the same word (no full restart).</summary>
    private IEnumerator HandleWrongAnswer(QuizWord quizWord)
    {
        AudioClip clip = quizWord.wrongAudio != null ? quizWord.wrongAudio : genericTryAgainAudio;

        yield return Speak(quizWord.wrongMessage, clip);

        // Restate which word to type before listening again — typing a whole
        // word takes longer than a single letter, so a reminder helps.
        yield return SpeakPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        BeginListeningForWord();
    }

    /// <summary>After enough consecutive mistakes: support audio, reset the streak, re-ask.</summary>
    private IEnumerator HandleSupportThenRetry(QuizWord quizWord)
    {
        yield return Speak(quizWord.supportMessage, quizWord.supportAudio);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return SpeakPrompt(quizWord);
        yield return new WaitForSeconds(delayAfterVoice);

        BeginListeningForWord();
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        if (!quizRunning)
            return;

        // End of quiz: "Repeat" means play the whole quiz again.
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartWord(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition is in progress
        // (wordActive is false during that window). Otherwise the in-flight
        // HandleCorrectAnswer would be stopped before it advances to the next
        // word, replaying the just-answered word instead.
        if (!wordActive)
            return;

        if (quizFinished || currentWordIndex < 0 || currentWordIndex >= quizWords.Count)
            return;

        // Replay the current word's prompt exactly like a fresh start.
        waitingForWordInput = false;
        currentMistakeCount = 0;
        currentTypedPatterns.Clear();

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private void HandleNext()
    {
        // The only "Next / Yes" the quiz cares about is the end-of-quiz choice: finish.
        if (!quizRunning || !waitingForRepeatChoice)
            return;

        waitingForRepeatChoice = false;
        EndQuizAndReturn();
    }

    // -------------------------------------------------------------------------
    // Quiz completion
    // -------------------------------------------------------------------------

    private IEnumerator FinishQuiz()
    {
        quizFinished = true;
        wordActive = false;
        waitingForWordInput = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return Speak(finalMessage, genericCompletedAudio);
        yield return PlayFinalScoreAudio();
        yield return AskPlayAgain();
    }

    /// <summary>
    /// Plays the repeat-question audio and lets the learner decide:
    /// Repeat = play the quiz again, Next/Yes = finish. The choice opens as
    /// the question starts, so the learner can answer without waiting for
    /// the whole question to finish.
    /// </summary>
    private IEnumerator AskPlayAgain()
    {
        waitingForRepeatChoice = true;

        yield return Speak(repeatQuestionMessage, repeatQuestionAudio);

        // Keep waiting silently for R or Y (handled in HandleRepeat / HandleNext).
    }

    private void EndQuizAndReturn()
    {
        StopFlow();
        quizRunning = false;

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController9] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final score audio: "Your score is <n>, while your highest score is <n>."
    // -------------------------------------------------------------------------

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
}