using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RimesEmEllEb : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Standard Grade-1 English Braille alphabet (A-Z), used to build the
    // expected sequence of dot patterns for a whole word, one per letter.
    // Each string is 6 characters long, one per dot in order (dot 1 .. dot 6),
    // matching the pattern format already produced by BrailleMapping
    // (e.g. "100000" = dot 1 only).
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
    public static readonly Dictionary<string, char> PatternToLetter =
    new Dictionary<string, char>()
{
    { "100000",'A'},
    { "110000",'B'},
    { "100100",'C'},
    { "100110",'D'},
    { "100010",'E'},
    { "110100",'F'},
    { "110110",'G'},
    { "110010",'H'},
    { "010100",'I'},
    { "010110",'J'},
    { "101000",'K'},
    { "111000",'L'},
    { "101100",'M'},
    { "101110",'N'},
    { "101010",'O'},
    { "111100",'P'},
    { "111110",'Q'},
    { "111010",'R'},
    { "011100",'S'},
    { "011110",'T'},
    { "101001",'U'},
    { "111001",'V'},
    { "010111",'W'},
    { "101101",'X'},
    { "101111",'Y'},
    { "101011",'Z'}
};

    // -------------------------------------------------------------------------
    // Lesson Pages — each page can be Information Only (just teaches, no
    // input required) or Interactive Practice (teaches, then requires the
    // learner to type a specific word in Braille, capital sign included,
    // before moving on). Every page can show any number of "beats" — a
    // message + its audio — played one after another, so a single page can
    // hold an entire mini-lecture like the Lesson 9 example.
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

    // -------------------------------------------------------------------------
    // Quiz Data — one entry per WORD (not per letter). The player must type
    // every letter of the word, one Braille cell at a time; the whole word is
    // only validated once all of its letters have been entered.
    // -------------------------------------------------------------------------
    [Serializable]
    public class BrailleWordLesson
    {
        [Header("Identity")]
        public string displayLabel;

        [TextArea(2, 4)]
        public string categoryLabel = "BRAILLE WORD";

        [Header("Target Word")]
        [Tooltip("The word to type. Capitalize only the first letter (e.g. 'Bell', 'Cat', 'Apple').")]
        public string word = "Bell";

        [Header("Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;

        /// <summary>The expected Braille pattern for each letter of the word, in order.</summary>
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
                    patterns.Add(BrailleCapitalIndicatorPattern);
                    first = false;
                }

                char upper = char.ToUpperInvariant(c);

                if (BrailleAlphabetPatterns.TryGetValue(upper, out string pattern))
                    patterns.Add(pattern);
            }

            return patterns;
        }
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
    public string highScoreKey = "RimesEmEllEbHighScore";

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
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's type some words.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [TextArea(2, 5)]
    public string lessonChoiceMessage =
    "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";

    public AudioClip lessonChoiceAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Pages (Information Only / Interactive Practice)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Flow - Words to Type (Quiz)")]
    public List<BrailleWordLesson> lessons = new List<BrailleWordLesson>();
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

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForChoiceAnswer = false;
    private bool waitingForLessonChoice = false;

    // Braille patterns submitted so far for the QUIZ word currently being
    // typed, one entry per completed letter/chord, in the order entered.
    private readonly List<string> currentTypedPatterns = new List<string>();
    private string currentTypedWord = "";
    private bool waitingForCapitalIndicator = true;

    // --- Lesson Page interactive-practice state (separate from the quiz's,
    //     so the two never interfere with each other) ---
    private int currentPageIndex = -1;
    private bool waitingForPagePracticeAnswer = false;
    private int pagePracticeMistakeCount = 0;
    private readonly List<string> currentPagePracticeTypedPatterns = new List<string>();
    private string currentPagePracticeTypedWord = "";
    private bool pageWaitingForCapitalIndicator = true;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

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
            Debug.Log("RimesEmEllEb started.");

        ResetQuizScore();
        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Score (Quiz only — Lesson Page practice is never scored)
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

    /// <summary>
    /// Stops whatever flow coroutine is currently running and starts a new one.
    /// Centralizing this avoids repeating the stop/start boilerplate everywhere.
    /// </summary>
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

        StartLessonPage(0);
        yield break;
    }

    // -------------------------------------------------------------------------
    // LESSON PAGES — Information Only or Interactive Practice
    //
    // Each page: play its information beats in order, then (if it's an
    // Interactive Practice page) ask the learner to type the practice word
    // and don't move on until they get it right. Pages progress one at a
    // time via StartLessonPage(index), the same flattened pattern the quiz
    // below uses, so a wrong-answer retry never has to "resume" a suspended
    // outer coroutine — it just re-asks directly.
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

        // First input must be Capital Indicator
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

        // Convert pattern into visible letter
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

    // -------------------------------------------------------------------------
    // QUIZ — unchanged from before (scored, whole-word Braille typing).
    // -------------------------------------------------------------------------

    private IEnumerator StartQuizAfterLesson()
    {
        yield return ShowBubbleMessageSynced(
            welcomeMessage,
            welcomeAudio,
            noAudioTextDelay);

        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(
            letsLearnMessage,
            letsLearnAudio,
            noAudioTextDelay);

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
        currentTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"Starting word {currentLessonIndex}: {lessons[currentLessonIndex].word}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Quiz Word Sequence
    //
    // Exact order:
    //   1. Display Label
    //   2. Category Label
    //   3. Prompt Message (+ audio)  -> e.g. "Can you type the word Bell?"
    //   4. Display Image    (shown alongside the prompt)
    //   5. Wait for the player to type the WHOLE word, one letter at a time
    //      (the word is validated only once every letter has been entered)
    //   6. Success Message  -> handled in HandleCorrectAnswer
    //   7. Wrong Message    -> handled in HandleWrongAnswer
    //   8. Support Message (+ audio) -> only after 3 consecutive mistakes
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleWordLesson lesson)
    {
        ResetAnswerState();
        currentTypedPatterns.Clear();

        // Steps 1, 2, 4: Display Label, Category Label, Display Image
        ApplyLessonDisplay(lesson);

        // Step 3: Prompt Message + audio
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 5: Wait for the whole word to be typed
        yield return AskForWordInput(lesson);
    }

    /// <summary>Steps 1, 2, 4 — Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(BrailleWordLesson lesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = !string.IsNullOrWhiteSpace(lesson.displayLabel)
                ? lesson.displayLabel
                : lesson.word;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel)
                ? "BRAILLE WORD"
                : lesson.categoryLabel;
    }

    /// <summary>Step 3 — Prompt Message together with its intro/instruction audio.</summary>
    private IEnumerator ShowPromptMessage(BrailleWordLesson lesson)
    {
        string introMessage = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : $"Can you type the word {lesson.word}?";

        yield return ShowBubbleMessageWithAudioSequence(
            introMessage,
            noAudioTextDelay,
            lesson.introAudio,
            lesson.instructionAudio
        );
    }

    /// <summary>
    /// Step 5 — marks the game as waiting for Braille input and clears the
    /// in-progress letter buffer so the word is typed (and later validated)
    /// from scratch. Used for the first ask and every re-ask after a wrong
    /// answer or a support message.
    /// </summary>
    private IEnumerator AskForWordInput(BrailleWordLesson lesson)
    {
        currentTypedPatterns.Clear();

        currentTypedWord = "";
        waitingForCapitalIndicator = true;

        if (typedWordText != null)
            typedWordText.text = "";
        waitingForChoiceAnswer = true;

        yield break;
    }

    // -------------------------------------------------------------------------
    // Input Handling — routes to Lesson Page practice or Quiz depending on
    // which one is currently waiting for an answer. The two never overlap in
    // time, but keeping separate flags/buffers means neither can interfere
    // with the other's state.
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (waitingForPagePracticeAnswer)
        {
            HandlePagePracticeLetterInput(submittedPattern);
            return;
        }

        if (!lessonActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForChoiceAnswer)
        {
            HandleWordLetterInput(submittedPattern);
            return;
        }
    }

    /// <summary>
    /// Accumulates one completed Braille letter/chord into the QUIZ word
    /// currently being typed. The word is only validated once as many
    /// letters have been entered as the target word requires.
    /// </summary>
    private void HandleWordLetterInput(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        BrailleWordLesson lesson = lessons[currentLessonIndex];
        List<string> targetPatterns = lesson.GetTargetPatterns();

        // First input must be the Capital Indicator
        if (waitingForCapitalIndicator)
        {
            if (pattern != BrailleCapitalIndicatorPattern)
            {
                // Wrong first input
                waitingForChoiceAnswer = false;
                currentMistakeCount++;
                AddMistake();
                RunFlow(HandleWrongAnswer(lessons[currentLessonIndex]));
                return;
            }

            waitingForCapitalIndicator = false;
            currentTypedPatterns.Add(pattern);
            return;
        }

        // Store the typed letter
        currentTypedPatterns.Add(pattern);

        // Convert pattern into a visible letter
        if (PatternToLetter.TryGetValue(pattern, out char letter))
        {
            if (currentTypedWord.Length == 0)
                currentTypedWord += char.ToUpper(letter);
            else
                currentTypedWord += char.ToLower(letter);

            if (typedWordText != null)
                typedWordText.text = currentTypedWord;
        }

        if (currentTypedPatterns.Count < targetPatterns.Count)
            return; // still typing the word — keep waiting for the remaining letters

        // The word is fully typed — validate the whole thing now.
        waitingForChoiceAnswer = false;

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
            lessonActive = false;

            SetAnswerState(true);
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
    // Correct / Wrong / Support (Quiz)
    // -------------------------------------------------------------------------

    /// <summary>Step 6 — Success Message, then advance to the next word.</summary>
    private IEnumerator HandleCorrectAnswer(BrailleWordLesson lesson)
    {
        SaveHighScoreIfNeeded();

        string message = !string.IsNullOrWhiteSpace(lesson.successMessage)
            ? lesson.successMessage
            : $"Correct! That is {lesson.word}.";

        AudioClip clip = lesson.successAudio != null
            ? lesson.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentTypedWord = "";

        if (typedWordText != null)
            typedWordText.text = "";

        StartLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 7 — Wrong Message, then re-ask the same word (no full lesson restart).</summary>
    private IEnumerator HandleWrongAnswer(BrailleWordLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.wrongMessage)
            ? lesson.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.wrongAudio != null
            ? lesson.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);

        // Restate which word to type before listening again — typing a whole
        // word takes longer than a single letter, so a reminder helps.
        currentTypedWord = "";
        waitingForCapitalIndicator = true;

        if (typedWordText != null)
            typedWordText.text = "";

        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskForWordInput(lesson);
    }

    /// <summary>
    /// Step 8 — after 3 consecutive mistakes, play the Support Message + audio
    /// to help the player, reset the mistake streak, then re-ask the same word.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(BrailleWordLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.supportMessage)
            ? lesson.supportMessage
            : $"Here is some help. Listen carefully and try typing {lesson.word} again.";

        yield return ShowBubbleMessageSynced(message, lesson.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskForWordInput(lesson);
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
            StartLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next word is
        // in progress (lessonActive is false during that window). Without this
        // guard, a Repeat trigger fired here would stop the in-flight
        // HandleCorrectAnswer coroutine before it calls StartLesson(index + 1),
        // replaying the just-answered word instead of advancing.
        if (!lessonActive)
            return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleWordLesson lesson = lessons[currentLessonIndex];

        // Reset this word's state so it plays exactly like a fresh start.
        lessonActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;
        currentTypedPatterns.Clear();

        RunFlow(PlayLessonFromBeginning(lesson));
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

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return ShowBubbleMessageSynced(finalMessage, genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[RimesEmEllEb] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
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

        while (!typingFinished)
            yield return null;

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