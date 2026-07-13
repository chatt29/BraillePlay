using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpeechSoundsScript : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Standard Grade-1 English Braille alphabet (A-Z) and the capital
    // indicator, ported from RimesEmEllEb so the Lesson Page system below
    // works exactly the same way here.
    // -------------------------------------------------------------------------
    public static readonly Dictionary<char, string> BrailleAlphabetPatterns = new Dictionary<char, string>
    {
        { 'A', "100000" }, { 'B', "110000" }, { 'C', "100100" }, { 'D', "100110" },
        { 'E', "100010" }, { 'F', "110100" }, { 'G', "110110" }, { 'H', "110010" },
        { 'I', "010100" }, { 'J', "010110" }, { 'K', "101000" }, { 'L', "111000" },
        { 'M', "101100" }, { 'N', "101110" }, { 'O', "101010" }, { 'P', "111100" },
        { 'Q', "111110" }, { 'R', "111010" }, { 'S', "011100" }, { 'T', "011110" },
        { 'U', "101001" }, { 'V', "111001" }, { 'W', "010111" }, { 'X', "101101" },
        { 'Y', "101111" }, { 'Z', "101011" },
    };

    /// <summary>Braille capital indicator (Dot 6), typed immediately before a capitalized letter.</summary>
    public const string BrailleCapitalIndicatorPattern = "000001";

    public static readonly Dictionary<string, char> PatternToLetter = new Dictionary<string, char>()
    {
        { "100000", 'A' }, { "110000", 'B' }, { "100100", 'C' }, { "100110", 'D' },
        { "100010", 'E' }, { "110100", 'F' }, { "110110", 'G' }, { "110010", 'H' },
        { "010100", 'I' }, { "010110", 'J' }, { "101000", 'K' }, { "111000", 'L' },
        { "101100", 'M' }, { "101110", 'N' }, { "101010", 'O' }, { "111100", 'P' },
        { "111110", 'Q' }, { "111010", 'R' }, { "011100", 'S' }, { "011110", 'T' },
        { "101001", 'U' }, { "111001", 'V' }, { "010111", 'W' }, { "101101", 'X' },
        { "101111", 'Y' }, { "101011", 'Z' }
    };

    // -------------------------------------------------------------------------
    // Lesson Pages — ported from RimesEmEllEb, unchanged. Each page can be
    // Information Only (just teaches, no input required) or Interactive
    // Practice (teaches, then requires the learner to type a specific word
    // in Braille, capital sign included, before moving on). Every page can
    // show any number of "beats" — a message + its audio — played one after
    // another.
    // -------------------------------------------------------------------------

    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonInfoBeat
    {
        [TextArea(2, 4)]
        public string message;
        public AudioClip audio;
    }

    [Serializable]
    public class LessonPage
    {
        [Header("Display")]
        public string title;

        [TextArea(2, 4)]
        public string categoryLabel = "LESSON";

        public Sprite lessonImage;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats below, then moves on automatically.\nInteractive Practice: plays the beats below, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        [Tooltip("Each beat is one bubble message + its audio, played in sequence. For an Interactive Practice page, these play BEFORE the practice prompt below.")]
        public List<LessonInfoBeat> informationBeats = new List<LessonInfoBeat>();

        [Header("Interactive Practice (used only if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Bell'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be typed as a capital — preceded by the Braille capital indicator (Dot 6) — followed by the remaining letters in lowercase.")]
        public bool requireCapitalFirstLetter = true;

        [TextArea(2, 4)]
        public string promptMessage;
        public AudioClip promptAudio;

        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;

        /// <summary>
        /// The expected Braille pattern sequence for practiceWord. If
        /// requireCapitalFirstLetter is true, the capital indicator (Dot 6)
        /// is inserted immediately before the first letter's pattern.
        /// </summary>
        public List<string> GetTargetPatterns()
        {
            var patterns = new List<string>();
            if (string.IsNullOrEmpty(practiceWord)) return patterns;

            bool isFirstLetter = true;

            foreach (char c in practiceWord)
            {
                if (!char.IsLetter(c)) continue;

                if (isFirstLetter && requireCapitalFirstLetter)
                    patterns.Add(BrailleCapitalIndicatorPattern);

                char upper = char.ToUpperInvariant(c);
                patterns.Add(BrailleAlphabetPatterns.TryGetValue(upper, out string pattern) ? pattern : "000000");

                isFirstLetter = false;
            }

            return patterns;
        }
    }

    [Serializable]
    public class SpellingWord
    {
        [Header("Word Info")]
        public string word = "jet";
        public Sprite displayImage;
        public AudioClip wordAudio;

        [Header("Prompt")]
        [TextArea(2, 4)]
        public string promptMessage = "Listen to the word, then spell it.";
        public AudioClip promptAudio;

        [Header("Success")]
        [TextArea(2, 4)]
        public string successMessage = "Correct! You spelled J-E-T, jet!";
        public AudioClip successAudio;

        [Header("Wrong Letter")]
        [TextArea(2, 4)]
        public string wrongLetterMessage = "That's not the right letter. Try again.";
        public AudioClip wrongLetterAudio;

        [Header("Support After 3 Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage = "J - E - T, Jet! The loud jet took off into the clouds.";
        public AudioClip supportAudio;

        [Header("Per-Letter Audio")]
        public AudioClip letter1Audio;
        public AudioClip letter2Audio;
        public AudioClip letter3Audio;
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------
    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text displayLabelText;
    public TMP_Text categoryText;
    public TMP_Text livePatternText;
    public TMP_Text typedWordText;
    public Image displayImageUI;

    [Header("Spelling Progress UI")]
    public TMP_Text spelledWordText;
    public TMP_Text currentLetterPromptText;

    [Header("Answer State Image")]
    public Image answerStateImage;
    public Sprite correctStateSprite;
    public Sprite wrongStateSprite;
    public bool hideAnswerStateAtStart = false;

    [Header("Quiz Score UI")]
    public TMP_Text fixScoreText;
    public TMP_Text wrongScoreText;
    public TMP_Text totalScoreText;
    public TMP_Text highScoreText;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "SpeechSoundsHighScore";

    [Header("Quiz Result Reporting")]
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

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Scene Text
    // -------------------------------------------------------------------------
    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Speech Sounds!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "First, let's learn the pattern.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished all the words.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "Do you want to practice again? Press R to repeat or Y to finish.";

    [TextArea(2, 5)]
    public string lessonChoiceMessage =
    "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";

    public AudioClip lessonChoiceAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Pages (Information Only / Interactive Practice)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Flow")]
    public List<SpellingWord> words = new List<SpellingWord>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Typewriter Sync")]
    public bool useTypewriterEffect = true;
    [Min(0.005f)] public float defaultTypewriterCharacterDelay = 0.03f;
    [Min(0.001f)] public float minSyncedCharacterDelay = 0.01f;
    [Min(0.001f)] public float maxSyncedCharacterDelay = 0.12f;
    public bool waitForFullAudioBeforeContinuing = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------
    private int currentWordIndex = -1;
    private int currentLetterIndex = 0;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForSpelling = false;
    private bool inLessonPhase = true;

    private string currentTargetWord = "";
    private char[] currentSpelledLetters;

    // --- Lesson Page state (ported from RimesEmEllEb, kept fully separate
    //     from the spelling-quiz state above so neither can interfere) ---
    private bool waitingForLessonChoice = false;
    private int currentPageIndex = -1;
    private bool waitingForPagePracticeAnswer = false;
    private int pagePracticeMistakeCount = 0;
    private readonly List<string> currentPagePracticeTypedPatterns = new List<string>();
    private string currentPagePracticeTypedWord = "";
    private bool pageWaitingForCapitalIndicator = true;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

    // Braille letter patterns (spelling quiz — unchanged, lowercase-keyed).
    private Dictionary<char, string> braillePatterns = new Dictionary<char, string>()
    {
        {'a', "100000"}, {'b', "110000"}, {'c', "100100"}, {'d', "100110"}, {'e', "100010"},
        {'f', "110100"}, {'g', "110110"}, {'h', "110010"}, {'i', "010100"}, {'j', "010110"},
        {'k', "101000"}, {'l', "111000"}, {'m', "101100"}, {'n', "101110"}, {'o', "101010"},
        {'p', "111100"}, {'q', "111110"}, {'r', "111010"}, {'s', "011100"}, {'t', "011110"},
        {'u', "101001"}, {'v', "111001"}, {'w', "010111"}, {'x', "101101"}, {'y', "101111"}, {'z', "101011"}
    };

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

    private void Update()
    {
        if (livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("SpeechSoundsScript started.");

        ResetQuizScore();
        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------
    private void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
        UpdateScoreUI();
        ResetAnswerState();
    }

    private void AddMistake()
    {
        totalWrongCount++;
        int deductions = totalWrongCount / 3;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));
        UpdateScoreUI();
        SetAnswerState(false);
    }

    private void UpdateScoreUI()
    {
        if (fixScoreText != null) fixScoreText.text = fixedScore.ToString();
        if (wrongScoreText != null) wrongScoreText.text = totalWrongCount.ToString();
        if (totalScoreText != null) totalScoreText.text = totalScore.ToString();
        if (highScoreText != null) highScoreText.text = highScore.ToString();
    }

    private void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
        UpdateScoreUI();
    }

    // -------------------------------------------------------------------------
    // Answer State Image
    // -------------------------------------------------------------------------
    private void SetAnswerState(bool isCorrect)
    {
        if (answerStateImage == null) return;
        answerStateImage.enabled = true;
        answerStateImage.sprite = isCorrect ? correctStateSprite : wrongStateSprite;
    }

    private void ResetAnswerState()
    {
        if (answerStateImage == null) return;
        if (hideAnswerStateAtStart)
        {
            answerStateImage.enabled = false;
        }
        else
        {
            answerStateImage.enabled = true;
            answerStateImage.sprite = wrongStateSprite;
        }
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
    // Scene Flow
    // -------------------------------------------------------------------------
    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForLessonChoice = false;
        waitingForPagePracticeAnswer = false;
        inLessonPhase = true;

        StartLessonPage(0);
        yield break;
    }

    // -------------------------------------------------------------------------
    // LESSON PAGES — ported from RimesEmEllEb.
    //
    // Each page: play its information beats in order, then (if it's an
    // Interactive Practice page) ask the learner to type the practice word
    // and don't move on until they get it right. Pages progress one at a
    // time via StartLessonPage(index) so a wrong-answer retry never has to
    // "resume" a suspended outer coroutine — it just re-asks directly.
    // -------------------------------------------------------------------------

    private void StartLessonPage(int index)
    {
        if (index < 0 || index >= lessonPages.Count)
        {
            RunFlow(FinishLessonPagesAndWaitForChoice());
            return;
        }

        currentPageIndex = index;
        waitingForPagePracticeAnswer = false;
        pagePracticeMistakeCount = 0;
        currentPagePracticeTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"Starting lesson page {index}: {lessonPages[index].title}");

        RunFlow(PlayLessonPage(lessonPages[index]));
    }

    private IEnumerator PlayLessonPage(LessonPage page)
    {
        ApplyLessonPageDisplay(page);

        // Play every information beat in order (the "mini-lecture" part).
        foreach (LessonInfoBeat beat in page.informationBeats)
        {
            yield return ShowBubbleMessageSynced(beat.message, beat.audio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (page.pageType == LessonPageType.InteractivePractice)
        {
            // Sets waitingForPagePracticeAnswer and returns; further progress
            // is driven by HandleBrailleChordSubmitted from here on.
            yield return AskPagePracticeInput(page);
        }
        else
        {
            StartLessonPage(currentPageIndex + 1);
        }
    }

    private void ApplyLessonPageDisplay(LessonPage page)
    {
        if (displayLabelText != null)
            displayLabelText.text = page.title;

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(page.categoryLabel)
                ? "LESSON"
                : page.categoryLabel;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = page.lessonImage;
            displayImageUI.enabled = page.lessonImage != null;
        }
    }

    /// <summary>
    /// Shows/speaks the practice prompt and starts waiting for the learner to
    /// type the practice word. Used for the first ask and every re-ask after
    /// a wrong answer or a support message.
    /// </summary>
    private IEnumerator AskPagePracticeInput(LessonPage page)
    {
        string prompt = !string.IsNullOrWhiteSpace(page.promptMessage)
            ? page.promptMessage
            : $"Now it's your turn. Can you type the word {page.practiceWord}?";

        yield return ShowBubbleMessageWithAudioSequence(prompt, noAudioTextDelay, page.promptAudio);

        currentPagePracticeTypedPatterns.Clear();

        currentPagePracticeTypedWord = "";
        pageWaitingForCapitalIndicator = true;

        if (typedWordText != null)
            typedWordText.text = "";

        waitingForPagePracticeAnswer = true;
    }

    /// <summary>
    /// Accumulates one completed Braille letter/chord (or the capital
    /// indicator) into the practice word currently being typed. Validated
    /// only once as many entries have been typed as the target requires.
    /// </summary>
    private void HandlePagePracticeLetterInput(string pattern)
    {
        if (!waitingForPagePracticeAnswer)
            return;

        LessonPage page = lessonPages[currentPageIndex];
        List<string> targetPatterns = page.GetTargetPatterns();

        // First input must be the Capital Indicator (if required).
        if (pageWaitingForCapitalIndicator)
        {
            if (pattern != BrailleCapitalIndicatorPattern)
            {
                waitingForPagePracticeAnswer = false;
                pagePracticeMistakeCount++;
                SetAnswerState(false);

                if (pagePracticeMistakeCount >= mistakesBeforeSupport)
                    RunFlow(HandlePagePracticeSupportThenRetry(page));
                else
                    RunFlow(HandlePagePracticeWrongAnswer(page));

                return;
            }

            pageWaitingForCapitalIndicator = false;
            currentPagePracticeTypedPatterns.Add(pattern);
            return;
        }

        currentPagePracticeTypedPatterns.Add(pattern);

        // Convert pattern into visible letter for the on-screen progress text.
        if (PatternToLetter.TryGetValue(pattern, out char letter))
        {
            if (currentPagePracticeTypedWord.Length == 0)
                currentPagePracticeTypedWord += char.ToUpper(letter);
            else
                currentPagePracticeTypedWord += char.ToLower(letter);

            if (typedWordText != null)
                typedWordText.text = currentPagePracticeTypedWord;
        }

        if (currentPagePracticeTypedPatterns.Count < targetPatterns.Count)
            return;

        waitingForPagePracticeAnswer = false;

        bool isCorrect = true;

        for (int i = 0; i < targetPatterns.Count; i++)
        {
            if (currentPagePracticeTypedPatterns[i] != targetPatterns[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            pagePracticeMistakeCount = 0;
            SetAnswerState(true);
            RunFlow(HandlePagePracticeCorrectAnswer(page));
        }
        else
        {
            pagePracticeMistakeCount++;
            SetAnswerState(false);

            if (pagePracticeMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandlePagePracticeSupportThenRetry(page));
            else
                RunFlow(HandlePagePracticeWrongAnswer(page));
        }
    }

    private IEnumerator HandlePagePracticeCorrectAnswer(LessonPage page)
    {
        string message = !string.IsNullOrWhiteSpace(page.successMessage)
            ? page.successMessage
            : $"Correct! That is {page.practiceWord}.";

        AudioClip clip = page.successAudio != null
            ? page.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentPagePracticeTypedWord = "";

        if (typedWordText != null)
            typedWordText.text = "";

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePagePracticeWrongAnswer(LessonPage page)
    {
        string message = !string.IsNullOrWhiteSpace(page.wrongMessage)
            ? page.wrongMessage
            : "That's not correct. Try again.";

        AudioClip clip = page.wrongAudio != null
            ? page.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);

        currentPagePracticeTypedWord = "";
        pageWaitingForCapitalIndicator = true;

        if (typedWordText != null)
            typedWordText.text = "";

        yield return AskPagePracticeInput(page);
    }

    private IEnumerator HandlePagePracticeSupportThenRetry(LessonPage page)
    {
        string message;

        if (!string.IsNullOrWhiteSpace(page.supportMessage))
        {
            message = page.supportMessage;
        }
        else
        {
            message = page.requireCapitalFirstLetter
                ? $"Here is some help. Remember to spell {page.practiceWord}, starting with a capital letter."
                : $"Here is some help. Remember to spell {page.practiceWord}.";
        }

        yield return ShowBubbleMessageSynced(message, page.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            pagePracticeMistakeCount = 0;

        currentPagePracticeTypedWord = "";
        pageWaitingForCapitalIndicator = true;

        if (typedWordText != null)
            typedWordText.text = "";

        yield return AskPagePracticeInput(page);
    }

    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        waitingForLessonChoice = true;

        yield return ShowBubbleMessageSynced(
            lessonChoiceMessage,
            lessonChoiceAudio,
            noAudioTextDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    /// <summary>
    /// Welcome/intro messages (Lesson Intro message removed, per request),
    /// then starts the first spelling word.
    /// </summary>
    private IEnumerator StartQuizAfterLesson()
    {
        inLessonPhase = false;

        yield return ShowBubbleMessageSynced(welcomeMessage, welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(letsLearnMessage, letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartWord(0);
    }

    private void StartWord(int index)
    {
        if (index < 0 || index >= words.Count)
        {
            RunFlow(FinalizeSceneCompletion());
            return;
        }

        currentWordIndex = index;
        currentLetterIndex = 0;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForSpelling = false;

        SpellingWord word = words[currentWordIndex];
        currentTargetWord = word.word.ToLower();
        currentSpelledLetters = new char[currentTargetWord.Length];
        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();

        if (logDebug)
            Debug.Log($"Starting word {currentWordIndex}: {currentTargetWord}");

        RunFlow(PlayWordPrompt(word));
    }

    // -------------------------------------------------------------------------
    // Word Sequence
    // -------------------------------------------------------------------------
    private IEnumerator PlayWordPrompt(SpellingWord word)
    {
        ResetAnswerState();

        if (displayLabelText != null)
            displayLabelText.text = "";

        if (displayImageUI != null)
        {
            displayImageUI.sprite = word.displayImage;
            displayImageUI.enabled = word.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = "SPELL";

        // 1. Play the actual word: "Jet"
        yield return ShowBubbleMessageSynced(word.word, word.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. Play the prompt: "Spell the word you just heard."
        yield return ShowBubbleMessageSynced(word.promptMessage, word.promptAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
        UpdateLetterPromptUI();
    }

    private void UpdateSpelledWordUI()
    {
        if (spelledWordText != null)
            spelledWordText.text = new string(currentSpelledLetters).ToUpper().Replace('_', '_');
    }

    private void UpdateLetterPromptUI()
    {
        if (currentLetterPromptText == null) return;

        string[] ordinals = { "first", "second", "third" };
        if (currentLetterIndex < ordinals.Length)
            currentLetterPromptText.text = $"Spell the {ordinals[currentLetterIndex]} letter";
    }

    // -------------------------------------------------------------------------
    // Input Handling — routes to Lesson Page practice or the spelling quiz
    // depending on which one is currently waiting for an answer. The two
    // never overlap in time, but separate flags/buffers mean neither can
    // interfere with the other's state.
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (waitingForPagePracticeAnswer)
        {
            HandlePagePracticeLetterInput(submittedPattern);
            return;
        }

        if (!lessonActive || sceneFinished || waitingForRepeatChoice || !waitingForSpelling)
            return;

        HandleSpellingInput(submittedPattern);
    }

    private void HandleSpellingInput(string pattern)
    {
        if (currentLetterIndex >= currentTargetWord.Length) return;

        char targetLetter = currentTargetWord[currentLetterIndex];
        string expectedPattern = braillePatterns.ContainsKey(targetLetter) ? braillePatterns[targetLetter] : "";

        if (pattern == expectedPattern)
        {
            currentSpelledLetters[currentLetterIndex] = targetLetter;
            currentLetterIndex++;
            currentMistakeCount = 0;

            UpdateSpelledWordUI();
            SetAnswerState(true);
            PlayLetterAudio(currentLetterIndex - 1);

            if (currentLetterIndex >= currentTargetWord.Length)
            {
                waitingForSpelling = false;
                lessonActive = false;
                RunFlow(HandleWordComplete(words[currentWordIndex]));
            }
            else
            {
                UpdateLetterPromptUI();
            }
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(words[currentWordIndex]));
            else
                RunFlow(HandleWrongLetter(words[currentWordIndex]));
        }
    }

    private void PlayLetterAudio(int letterIndex)
    {
        SpellingWord word = words[currentWordIndex];
        AudioClip clip = null;

        if (letterIndex == 0) clip = word.letter1Audio;
        else if (letterIndex == 1) clip = word.letter2Audio;
        else if (letterIndex == 2) clip = word.letter3Audio;

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support (Spelling Quiz)
    // -------------------------------------------------------------------------
    private IEnumerator HandleWordComplete(SpellingWord word)
    {
        SaveHighScoreIfNeeded();
        yield return ShowBubbleMessageSynced(word.successMessage, word.successAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);
        StartWord(currentWordIndex + 1);
    }

    private IEnumerator HandleWrongLetter(SpellingWord word)
    {
        yield return ShowBubbleMessageSynced(word.wrongLetterMessage, word.wrongLetterAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);
    }

    private IEnumerator HandleSupportThenRetry(SpellingWord word)
    {
        yield return ShowBubbleMessageSynced(word.supportMessage, word.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        currentLetterIndex = 0;
        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();
        UpdateLetterPromptUI();
        yield return new WaitForSeconds(delayAfterVoice);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------
    private void HandleRepeat()
    {
        if (waitingForPagePracticeAnswer)
        {
            // Restart the current page's practice prompt from scratch.
            LessonPage page = lessonPages[currentPageIndex];
            pagePracticeMistakeCount = 0;
            RunFlow(AskPagePracticeInput(page));
            return;
        }

        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            StartLessonPage(0);
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            RunFlow(BeginSceneFlow());
            return;
        }

        if (!lessonActive || inLessonPhase) return;

        if (sceneFinished || currentWordIndex < 0 || currentWordIndex >= words.Count)
            return;

        SpellingWord word = words[currentWordIndex];
        lessonActive = true;
        waitingForSpelling = false;
        currentMistakeCount = 0;
        currentLetterIndex = 0;

        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();
        RunFlow(PlayWordPrompt(word));
    }

    private void HandleNext()
    {
        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            RunFlow(StartQuizAfterLesson());
            return;
        }
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

        if (displayImageUI != null) displayImageUI.enabled = false;
        if (displayLabelText != null) displayLabelText.text = string.Empty;
        if (spelledWordText != null) spelledWordText.text = string.Empty;
        if (currentLetterPromptText != null) currentLetterPromptText.text = string.Empty;
        ResetAnswerState();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";
        yield return ShowBubbleMessageSynced(finalMessage, genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[SpeechSoundsScript] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final Score Audio
    // -------------------------------------------------------------------------
    private IEnumerator PlayFinalScoreAudio()
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
    // Bubble Text / Typewriter
    // -------------------------------------------------------------------------
    private IEnumerator ShowBubbleMessageSynced(string message, AudioClip clip, float fallbackWait)
    {
        if (bubbleMessageText == null) yield break;
        StopBubbleTyping();

        float audioDuration = GetClipDuration(clip);
        float charDelay = GetCharacterDelayForMessage(message, audioDuration);
        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, () => typingFinished = true));

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        while (!typingFinished) yield return null;
        bubbleTypeRoutine = null;

        if (clip != null && voiceAudioSource != null && waitForFullAudioBeforeContinuing)
            yield return new WaitForSeconds(Mathf.Max(0f, clip.length - EstimatedTypingDuration(message, charDelay)));
        else if (clip == null)
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator ShowBubbleMessageWithAudioSequence(string message, float fallbackWait, params AudioClip[] clips)
    {
        if (bubbleMessageText == null) yield break;

        StopBubbleTyping();

        float totalDuration = GetTotalClipDuration(clips);
        float charDelay = GetCharacterDelayForMessage(message, totalDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, () => typingFinished = true));

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }
        else
        {
            while (!typingFinished)
                yield return null;

            yield return new WaitForSeconds(fallbackWait);
        }

        while (!typingFinished)
            yield return null;

        bubbleTypeRoutine = null;
    }

    private IEnumerator TypeBubbleText(string message, float characterDelay, Action onComplete = null)
    {
        if (bubbleMessageText == null) yield break;
        if (!useTypewriterEffect)
        {
            bubbleMessageText.text = message;
            onComplete?.Invoke();
            yield break;
        }

        bubbleMessageText.text = string.Empty;
        if (string.IsNullOrEmpty(message))
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            bubbleMessageText.text += message[i];
            yield return new WaitForSeconds(characterDelay);
        }
        onComplete?.Invoke();
    }

    private void StopBubbleTyping()
    {
        if (bubbleTypeRoutine != null)
        {
            StopCoroutine(bubbleTypeRoutine);
            bubbleTypeRoutine = null;
        }
    }

    private float GetCharacterDelayForMessage(string message, float audioDuration)
    {
        if (!useTypewriterEffect) return 0f;
        int visibleLength = GetVisibleCharacterCount(message);
        if (visibleLength <= 0) return defaultTypewriterCharacterDelay;
        if (audioDuration <= 0f) return defaultTypewriterCharacterDelay;
        float syncedDelay = audioDuration / visibleLength;
        return Mathf.Clamp(syncedDelay, minSyncedCharacterDelay, maxSyncedCharacterDelay);
    }

    private int GetVisibleCharacterCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        int count = 0;
        foreach (char c in message)
        {
            if (!char.IsWhiteSpace(c)) count++;
        }
        return Mathf.Max(1, count);
    }

    private float EstimatedTypingDuration(string message, float characterDelay)
        => GetVisibleCharacterCount(message) * characterDelay;

    private float GetClipDuration(AudioClip clip)
        => clip != null ? clip.length : 0f;

    private float GetTotalClipDuration(params AudioClip[] clips)
    {
        float total = 0f;
        if (clips == null) return total;
        foreach (AudioClip c in clips)
            if (c != null) total += c.length;
        return total;
    }
}