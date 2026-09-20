using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QUIZ half of the old SpeechSoundsScript.
///
/// Flow (started by LessonController7 through BeginQuiz()):
///   1. Intro audio ("Welcome..." then "First, let's learn the pattern.")
///   2. For each word: play the word, play the prompt, then the learner spells
///      it letter by letter in Braille. Wrong letters cost score, and after
///      enough mistakes a support message plays and the word restarts.
///   3. Announce the result: completed audio, "Your score is N, while your
///      highest score is M".
///   4. Play the "repeat question" audio and let the learner decide:
///        Repeat -> play the quiz again
///        Next/Y -> report the score and return to the game menu
///
/// Owns everything about scoring (score, wrong-answer count, high score).
/// Audio only - there is no visual UI in this script.
/// </summary>
public class QuizController7 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz word data
    // -------------------------------------------------------------------------
    [Serializable]
    public class SpellingWord
    {
        [Header("Word")]
        [Tooltip("The letters the learner must spell. Also serves as the list label in the Inspector.")]
        public string word = "jet";
        [Tooltip("The spoken word itself, e.g. 'Jet'.")]
        public AudioClip wordAudio;

        [Header("Prompt")]
        [Tooltip("e.g. 'Listen to the word, then spell it.'")]
        public AudioClip promptAudio;

        [Header("Feedback")]
        public AudioClip successAudio;
        public AudioClip wrongLetterAudio;
        [Tooltip("Played after the learner reaches the mistake limit; the word then restarts.")]
        public AudioClip supportAudio;

        [Header("Per-Letter Audio (played when that letter is typed correctly)")]
        public AudioClip letter1Audio;
        public AudioClip letter2Audio;
        public AudioClip letter3Audio;

        public AudioClip GetLetterAudio(int letterIndex)
        {
            switch (letterIndex)
            {
                case 0: return letter1Audio;
                case 1: return letter2Audio;
                case 2: return letter3Audio;
                default: return null;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    [Tooltip("e.g. 'Welcome to Speech Sounds!' - played when the quiz starts.")]
    public AudioClip welcomeAudio;
    [Tooltip("e.g. 'First, let's learn the pattern.' - played after the welcome.")]
    public AudioClip letsLearnAudio;
    [Tooltip("e.g. 'Great job! You finished all the words.'")]
    public AudioClip genericCompletedAudio;
    [Tooltip("e.g. 'Do you want to practice again? Press R to repeat or Y to finish.' - played after the score announcement.")]
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100 (index = number)")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Quiz Words")]
    public List<SpellingWord> words = new List<SpellingWord>();

    [Header("Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "SpeechSoundsHighScore";

    [Header("Flow Settings")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("How long to pause when a step has no audio clip assigned, so the flow never stalls.")]
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    [Tooltip("If true, the welcome + 'let's learn' intro plays again when the learner chooses to replay the quiz.")]
    public bool playIntroOnReplay = false;

    [Header("Capitalization")]
    [Tooltip("If true, the learner must type the Braille capital indicator (Dot 6) before the first letter of every word. A first letter typed without it counts as a mistake.")]
    public bool requireCapitalFirstLetter = true;
    [Tooltip("Optional. Played instead of the word's 'wrong letter' audio when the capital sign was missing, e.g. 'Remember to start with the capital sign.'")]
    public AudioClip missingCapitalAudio;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Read-only score access (for other scripts, if ever needed)
    // -------------------------------------------------------------------------
    public int TotalScore => totalScore;
    public int HighScore => highScore;
    public int TotalWrongCount => totalWrongCount;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // Every 3 wrong letters cost 'deductionPerMistake' points (unchanged from the original).
    private const int MistakesPerDeduction = 3;

    // Score
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    // Word progress
    private int currentWordIndex = -1;
    private int currentLetterIndex = 0;
    private int currentMistakeCount = 0;
    private string currentTargetWord = "";

    // Flow flags
    private bool wordActive = false;              // a word is in progress (was 'lessonActive')
    private bool waitingForSpelling = false;      // the prompt has finished; letters are accepted
    private bool waitingForRepeatChoice = false;  // end of quiz: waiting for Repeat or Next/Y
    private bool waitingForCapitalIndicator = false; // the next input must be the capital sign (start of a word)

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity events
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
        // The quiz stays idle until LessonController7 calls BeginQuiz().
        if (logDebug)
            Debug.Log("[QuizController7] Ready - waiting for the lesson to finish.");

        ResetQuizScore();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Called by LessonController7 when the learner chooses "Next" after the lesson.</summary>
    public void BeginQuiz()
    {
        StartQuizRun(playIntro: true);
    }

    // -------------------------------------------------------------------------
    // Coroutine / audio helpers
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

    /// <summary>
    /// Plays a clip and waits until it has finished. If there is no clip (or no
    /// AudioSource) it waits noAudioFallbackDelay instead so the flow keeps going.
    /// </summary>
    private IEnumerator PlayVoice(AudioClip clip, string context)
    {
        if (clip == null || voiceAudioSource == null)
        {
            if (logDebug)
                Debug.LogWarning($"[QuizController7] No audio for: {context}");

            yield return new WaitForSeconds(noAudioFallbackDelay);
            yield break;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();

        yield return new WaitForSeconds(clip.length);
    }

    /// <summary>Plays a clip and waits for it, or silently skips it if it isn't assigned (no fallback wait).</summary>
    private IEnumerator PlayVoiceIfPresent(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null)
            yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();

        yield return new WaitForSeconds(clip.length);
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
    // Quiz start
    // -------------------------------------------------------------------------

    /// <summary>Resets score and progress, then runs the intro (optional) and the first word.</summary>
    private void StartQuizRun(bool playIntro)
    {
        wordActive = false;
        waitingForSpelling = false;
        waitingForRepeatChoice = false;
        currentWordIndex = -1;

        ResetQuizScore();

        RunFlow(QuizIntroThenFirstWord(playIntro));
    }

    private IEnumerator QuizIntroThenFirstWord(bool playIntro)
    {
        if (playIntro)
        {
            yield return PlayVoice(welcomeAudio, "quiz welcome");
            yield return new WaitForSeconds(delayAfterVoice);

            yield return PlayVoice(letsLearnAudio, "quiz 'let's learn'");
            yield return new WaitForSeconds(delayAfterVoice);
        }

        StartWord(0);
    }

    private void StartWord(int index)
    {
        if (index < 0 || index >= words.Count)
        {
            RunFlow(FinishQuizAndAskToRepeat());
            return;
        }

        currentWordIndex = index;
        currentLetterIndex = 0;
        currentMistakeCount = 0;
        wordActive = true;
        waitingForCapitalIndicator = requireCapitalFirstLetter;
        waitingForRepeatChoice = false;
        waitingForSpelling = false;

        SpellingWord word = words[currentWordIndex];
        currentTargetWord = word.word.ToLowerInvariant();

        if (logDebug)
            Debug.Log($"[QuizController7] Starting word {currentWordIndex}: {currentTargetWord}");

        RunFlow(PlayWordPrompt(word));
    }

    // -------------------------------------------------------------------------
    // Word sequence
    // -------------------------------------------------------------------------
    private IEnumerator PlayWordPrompt(SpellingWord word)
    {
        // 1. Play the actual word: "Jet"
        yield return PlayVoice(word.wordAudio, $"word '{word.word}'");
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. Play the prompt: "Spell the word you just heard."
        yield return PlayVoice(word.promptAudio, $"prompt for '{word.word}'");
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
    }

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!wordActive || !waitingForSpelling)
            return;

        HandleSpellingInput(submittedPattern);
    }

    private void HandleSpellingInput(string pattern)
    {
        if (currentLetterIndex >= currentTargetWord.Length) return;

        // The first input of every word must be the capital indicator (if required).
        // Anything else - even the correct first letter - is wrong.
        if (waitingForCapitalIndicator)
        {
            if (pattern == BrailleAlphabet7.CapitalIndicator)
                waitingForCapitalIndicator = false;
            else
                RegisterMistake(missingCapital: true);

            return;
        }

        char targetLetter = currentTargetWord[currentLetterIndex];
        if (!BrailleAlphabet7.TryGetPattern(targetLetter, out string expectedPattern))
            expectedPattern = "";

        if (pattern == expectedPattern)
        {
            currentLetterIndex++;
            currentMistakeCount = 0;

            PlayLetterAudio(currentLetterIndex - 1);

            if (currentLetterIndex >= currentTargetWord.Length)
            {
                waitingForSpelling = false;
                wordActive = false;
                RunFlow(HandleWordComplete(words[currentWordIndex]));
            }
        }
        else
        {
            RegisterMistake(missingCapital: false);
        }
    }

    /// <summary>
    /// Counts a wrong input (costs score) and plays either the wrong-input
    /// audio or, after enough mistakes, the support audio.
    /// </summary>
    private void RegisterMistake(bool missingCapital)
    {
        currentMistakeCount++;
        AddMistake();

        SpellingWord word = words[currentWordIndex];

        if (currentMistakeCount >= mistakesBeforeSupport)
            RunFlow(HandleSupportThenRetry(word));
        else
            RunFlow(HandleWrongLetter(word, missingCapital));
    }

    /// <summary>Sends the word back to its first input (capital sign, then first letter).</summary>
    private void RestartWordInput()
    {
        currentLetterIndex = 0;
        waitingForCapitalIndicator = requireCapitalFirstLetter;
    }

    private void PlayLetterAudio(int letterIndex)
    {
        AudioClip clip = words[currentWordIndex].GetLetterAudio(letterIndex);

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------
    private IEnumerator HandleWordComplete(SpellingWord word)
    {
        SaveHighScoreIfNeeded();

        yield return PlayVoice(word.successAudio, $"success for '{word.word}'");
        yield return new WaitForSeconds(delayAfterCorrect);

        StartWord(currentWordIndex + 1);
    }

    private IEnumerator HandleWrongLetter(SpellingWord word, bool missingCapital)
    {
        AudioClip clip = word.wrongLetterAudio;
        if (missingCapital && missingCapitalAudio != null)
            clip = missingCapitalAudio;

        yield return PlayVoice(clip, missingCapital
            ? $"missing capital sign for '{word.word}'"
            : $"wrong letter for '{word.word}'");
        yield return new WaitForSeconds(delayAfterVoice);
    }

    private IEnumerator HandleSupportThenRetry(SpellingWord word)
    {
        yield return PlayVoice(word.supportAudio, $"support for '{word.word}'");

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        // The word starts over (capital sign first, if required).
        RestartWordInput();

        yield return new WaitForSeconds(delayAfterVoice);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------
    private void HandleRepeat()
    {
        // End of quiz: "play the quiz again".
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StopVoice();

            if (logDebug)
                Debug.Log("[QuizController7] Learner chose to play the quiz again.");

            StartQuizRun(playIntroOnReplay);
            return;
        }

        // Mid-quiz: replay the current word's audio and start the word over.
        if (!wordActive)
            return;

        waitingForSpelling = false;
        currentMistakeCount = 0;
        RestartWordInput();

        RunFlow(PlayWordPrompt(words[currentWordIndex]));
    }

    private void HandleNext()
    {
        // Only meaningful at the end of the quiz ("Y to finish").
        if (!waitingForRepeatChoice)
            return;

        waitingForRepeatChoice = false;

        if (logDebug)
            Debug.Log("[QuizController7] Learner chose to finish the quiz.");

        FinishAndReturn();
    }

    // -------------------------------------------------------------------------
    // Quiz completion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Announces the result, then asks whether the learner wants to play again.
    /// The learner's answer is handled by HandleRepeat (play again) and
    /// HandleNext (finish).
    /// </summary>
    private IEnumerator FinishQuizAndAskToRepeat()
    {
        wordActive = false;
        waitingForSpelling = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // "Great job! You finished all the words."
        yield return PlayVoice(genericCompletedAudio, "quiz completed");

        // "Your score is <N>, while your highest score is <M>."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // "Do you want to practice again? Press R to repeat or Y to finish."
        // The learner may answer as soon as the question starts playing.
        waitingForRepeatChoice = true;
        yield return PlayVoice(repeatQuestionAudio, "repeat question");
    }

    /// <summary>Learner chose to finish: save/report the score and return to the menu.</summary>
    private void FinishAndReturn()
    {
        StopFlow();
        StopVoice();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController7] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final score audio
    // -------------------------------------------------------------------------
    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        yield return PlayVoiceIfPresent(yourScoreIsAudio);
        yield return PlayVoiceIfPresent(GetNumberAudio(totalScore));
        yield return PlayVoiceIfPresent(whileYourHighestScoreIsAudio);
        yield return PlayVoiceIfPresent(GetNumberAudio(highScore));
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }
}