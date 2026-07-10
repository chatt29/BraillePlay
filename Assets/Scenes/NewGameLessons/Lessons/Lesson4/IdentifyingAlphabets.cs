using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class IdentifyingAlphabets : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [Header("Display")]
        public string title;

        [TextArea(5, 10)]
        public string lessonText;

        public Sprite lessonImage;

        [Header("Audio")]
        public AudioClip lessonAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Mode Data (scored) — one entry per letter, A through Z.
    // -------------------------------------------------------------------------
    [Serializable]
    public class AlphabetLetter
    {
        [Header("Identity")]
        [Tooltip("The letter this entry tests (A-Z). The correct Braille pattern is derived from this automatically.")]
        public char letter = 'A';

        [TextArea(2, 4)]
        public string categoryLabel = "BRAILLE ALPHABET";

        [Header("Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Display Image")]
        [Tooltip("Optional reference image for this letter's Braille cell, shown alongside the prompt.")]
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

        /// <summary>Looks up the correct 6-dot Braille pattern for this letter.</summary>
        public string GetCorrectPattern()
        {
            char upper = char.ToUpperInvariant(letter);
            return BrailleAlphabetPatterns.TryGetValue(upper, out string pattern)
                ? pattern
                : "000000";
        }
    }

    // -------------------------------------------------------------------------
    // Lesson Mode Data (unscored, guided practice) — one entry per letter,
    // A through Z. Every field the guided-tutor sequence needs (identity,
    // introduction, letter sound, Braille dot instruction, prompt, and
    // success/wrong/support feedback) lives here so designers can author the
    // full teaching moment for each letter in one place.
    // -------------------------------------------------------------------------
    [Serializable]
    public class LessonLetter
    {
        [Header("Identity")]
        [Tooltip("The letter this entry teaches (A-Z). The correct Braille pattern is derived from this automatically.")]
        public char letter = 'A';

        [TextArea(2, 4)]
        public string categoryLabel = "BRAILLE ALPHABET - LESSON";

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("1. Introduction")]
        [Tooltip("e.g. 'This is Letter A.'")]
        [TextArea(2, 4)]
        public string introductionMessage;
        public AudioClip introAudio;

        [Header("2. Letter Sound")]
        [Tooltip("e.g. 'Letter A says \"Ahhh\".'")]
        [TextArea(2, 4)]
        public string letterSoundMessage;
        public AudioClip letterSoundAudio;

        [Header("3. Braille Dot Instruction")]
        [Tooltip("e.g. 'Letter A uses Dot 1.' Leave blank to auto-generate from the letter's pattern.")]
        [TextArea(2, 4)]
        public string brailleInstructionMessage;
        public AudioClip brailleInstructionAudio;

        [Header("4. Prompt")]
        [Tooltip("e.g. 'Now it's your turn. Can you type Letter A?'")]
        [TextArea(2, 4)]
        public string promptMessage;
        public AudioClip promptAudio;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;
        public AudioClip wrongAudio;

        [Header("Support After 3 Mistakes")]
        [Tooltip("e.g. 'Remember, Letter A uses Dot 1. Press Dot 1, then submit.' Leave blank to auto-generate.")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;

        /// <summary>Looks up the correct 6-dot Braille pattern for this letter.</summary>
        public string GetCorrectPattern()
        {
            char upper = char.ToUpperInvariant(letter);
            return BrailleAlphabetPatterns.TryGetValue(upper, out string pattern)
                ? pattern
                : "000000";
        }
    }

    /// <summary>
    /// Standard 6-dot Braille patterns for the English alphabet A-Z.
    /// Each string is 6 characters long, one per dot in order (dot 1 .. dot 6),
    /// matching the pattern format already used by BrailleMapping
    /// (e.g. "100000" = dot 1 only). Shared by both Lesson Mode and Quiz Mode.
    /// </summary>
    public static readonly Dictionary<char, string> BrailleAlphabetPatterns = new Dictionary<char, string>
    {
        { 'A', "100000" },
        { 'B', "110000" },
        { 'C', "100100" },
        { 'D', "100110" },
        { 'E', "100010" },
        { 'F', "110100" },
        { 'G', "110110" },
        { 'H', "110010" },
        { 'I', "010100" },
        { 'J', "010110" },
        { 'K', "101000" },
        { 'L', "111000" },
        { 'M', "101100" },
        { 'N', "101110" },
        { 'O', "101010" },
        { 'P', "111100" },
        { 'Q', "111110" },
        { 'R', "111010" },
        { 'S', "011100" },
        { 'T', "011110" },
        { 'U', "101001" },
        { 'V', "111001" },
        { 'W', "010111" },
        { 'X', "101101" },
        { 'Y', "101111" },
        { 'Z', "101011" },
    };

    /// <summary>Turns a pattern like "110000" into a human-readable "Dots 1 and 2".</summary>
    private static string DescribeDots(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return "no dots";

        List<int> dots = new List<int>();
        for (int i = 0; i < pattern.Length && i < 6; i++)
            if (pattern[i] == '1') dots.Add(i + 1);

        if (dots.Count == 0) return "no dots";
        if (dots.Count == 1) return $"Dot {dots[0]}";
        if (dots.Count == 2) return $"Dots {dots[0]} and {dots[1]}";

        string allButLast = string.Join(", ", dots.GetRange(0, dots.Count - 1));
        return $"Dots {allButLast}, and {dots[dots.Count - 1]}";
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text displayLabelText;
    public TMP_Text categoryText;
    public TMP_Text livePatternText;
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
    public string highScoreKey = "BrailleSoundsAroundHighScore";

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
    public string letsLearnMessage = "Let's learn the alphabet in Braille.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the quiz.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the quiz. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Lesson Mode -> Quiz Mode Transition")]
    [TextArea(2, 5)]
    public string lessonCompleteMessage =
        "Great job! You've practiced the whole alphabet. Press repeat to practice again, or press next to begin the quiz.";
    public AudioClip lessonCompleteAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Pages (Intro)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Mode - Guided Practice A-Z (no scoring)")]
    [Tooltip("One entry per letter. Use the context menu 'Auto-Fill Lesson Letters A-Z' on this component to generate all 26 entries in order.")]
    public List<LessonLetter> lessonLetters = new List<LessonLetter>();

    [Header("Quiz Mode - Scored Assessment A-Z")]
    [Tooltip("One entry per letter. Use the context menu 'Auto-Fill Alphabet A-Z' on this component to generate all 26 entries in order.")]
    public List<AlphabetLetter> alphabetLessons = new List<AlphabetLetter>();

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
    // Editor Utility
    // -------------------------------------------------------------------------

    [ContextMenu("Auto-Fill Lesson Letters A-Z")]
    private void AutoFillLessonLetters()
    {
        lessonLetters.Clear();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            lessonLetters.Add(new LessonLetter { letter = c });
        }
    }

    [ContextMenu("Auto-Fill Alphabet A-Z")]
    private void AutoFillAlphabet()
    {
        alphabetLessons.Clear();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            alphabetLessons.Add(new AlphabetLetter { letter = c });
        }
    }

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    /// <summary>Which mode the current letter belongs to — gates scoring.</summary>
    private enum Mode { Lesson, Quiz }
    private Mode currentMode;

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForChoiceAnswer = false;
    private bool waitingForQuizTransition = false;
    private bool canAcceptQuizChoice = false;

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
            Debug.Log("IdentifyingAlphabets started.");

        ResetQuizScore();
        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Score (Quiz Mode only — Lesson Mode never touches any of this)
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
    // Scene Flow — Intro Pages -> Lesson Mode -> Quiz Mode -> Completion
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForQuizTransition = false;

        yield return PlayLessonPages();
    }

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            // Show the lesson title in the Display Label
            if (displayLabelText != null)
                displayLabelText.text = page.title;

            // Clear the Category Text during lesson pages
            if (categoryText != null)
                categoryText.text = "";

            if (displayImageUI != null)
            {
                displayImageUI.sprite = page.lessonImage;
                displayImageUI.enabled = page.lessonImage != null;
            }

            yield return ShowBubbleMessageSynced(
                page.lessonText,
                page.lessonAudio,
                noAudioTextDelay
            );

            yield return new WaitForSeconds(delayAfterVoice);
        }

        // Automatically start the interactive lesson letters.
        yield return StartLessonMode();
    }

    // -------------------------------------------------------------------------
    // LESSON MODE — guided, unscored practice through the whole alphabet.
    // -------------------------------------------------------------------------
    private IEnumerator StartLessonMode()
    {
        // Start the lesson letters immediately after the lesson pages.
        StartLetterLesson(0);
        yield break;
    }

    private void StartLetterLesson(int index)
    {
        if (index < 0 || index >= lessonLetters.Count)
        {
            RunFlow(CompleteLessonMode());
            return;
        }

        currentMode = Mode.Lesson;
        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoiceAnswer = false;

        if (logDebug)
            Debug.Log($"[Lesson] Starting letter {index}: {lessonLetters[index].letter}");

        RunFlow(PlayLessonLetterFromBeginning(lessonLetters[index]));
    }

    /// <summary>Lesson Mode finished — offer to practice again or move on to the quiz.</summary>
    private IEnumerator CompleteLessonMode()
    {
        lessonActive = false;
        ResetAnswerState();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        yield return ShowBubbleMessageSynced(
            lessonCompleteMessage,
            lessonCompleteAudio,
            noAudioTextDelay);

        yield return new WaitForSeconds(0.5f);

        waitingForQuizTransition = true;
        canAcceptQuizChoice = true;
    }

    // -------------------------------------------------------------------------
    // Lesson Mode Letter Sequence
    //
    // Exact order (per letter, A through Z):
    //   1. Display the letter (Display Label / Category Label / Display Image)
    //   2. Introduce the letter (+ audio)              e.g. "This is Letter A."
    //   3. Letter sound (+ audio)                        e.g. "Letter A says 'Ahhh'."
    //   4. Braille dot instruction (+ audio)             e.g. "Letter A uses Dot 1."
    //   5. Prompt the learner (+ audio)                  e.g. "Can you type Letter A?"
    //   6. Wait for the Braille answer
    //   7. Validate:
    //        correct -> success message, auto-advance to the next letter
    //        wrong   -> wrong message, retry (NO full replay of steps 2-5)
    //                   after 3 misses -> support message (dot reminder), retry
    //
    // No scoring of any kind happens anywhere in this sequence.
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonLetterFromBeginning(LessonLetter letter)
    {
        ResetAnswerState();

        // Step 1: Display Label, Category Label, Display Image
        ApplyLessonLetterDisplay(letter);

        // Steps 2-5: Introduction, Letter Sound, Braille Instruction, Prompt
        yield return TeachAndAskLessonLetter(letter);
    }

    /// <summary>Step 1 — Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonLetterDisplay(LessonLetter letter)
    {
        if (displayLabelText != null)
            displayLabelText.text = char.ToUpperInvariant(letter.letter).ToString();

        if (displayImageUI != null)
        {
            displayImageUI.sprite = letter.displayImage;
            displayImageUI.enabled = letter.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(letter.categoryLabel)
                ? "BRAILLE ALPHABET - LESSON"
                : letter.categoryLabel;
    }

    /// <summary>
    /// Steps 2-5 — the full guided-teaching beat for a letter: introduction,
    /// letter sound, Braille dot instruction, then the prompt to try typing it.
    /// Only played once per letter (not repeated on wrong-answer retries), so
    /// the lesson doesn't re-lecture the learner every single attempt.
    /// </summary>
    private IEnumerator TeachAndAskLessonLetter(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        // Step 2: Introduction
        string intro = !string.IsNullOrWhiteSpace(letter.introductionMessage)
            ? letter.introductionMessage
            : $"This is Letter {upper}.";

        yield return ShowBubbleMessageSynced(intro, letter.introAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Letter Sound
        string soundMessage = !string.IsNullOrWhiteSpace(letter.letterSoundMessage)
            ? letter.letterSoundMessage
            : $"Letter {upper} makes its own sound.";

        yield return ShowBubbleMessageSynced(soundMessage, letter.letterSoundAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Braille Dot Instruction
        string brailleMessage = !string.IsNullOrWhiteSpace(letter.brailleInstructionMessage)
            ? letter.brailleInstructionMessage
            : $"Letter {upper} uses {DescribeDots(letter.GetCorrectPattern())}.";

        yield return ShowBubbleMessageSynced(brailleMessage, letter.brailleInstructionAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 5: Prompt
        string prompt = !string.IsNullOrWhiteSpace(letter.promptMessage)
            ? letter.promptMessage
            : $"Now it's your turn. Can you type Letter {upper}?";

        yield return ShowBubbleMessageSynced(prompt, letter.promptAudio, noAudioTextDelay);

        // Step 6: Start waiting for the Braille answer.
        waitingForChoiceAnswer = true;
    }

    /// <summary>Re-listens for input without repeating the teaching beats (used on retry).</summary>
    private IEnumerator WaitForBrailleAnswerLesson()
    {
        waitingForChoiceAnswer = true;
        yield break;
    }

    private void HandleLessonAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        LessonLetter letter = lessonLetters[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (pattern == letter.GetCorrectPattern())
        {
            currentMistakeCount = 0;
            lessonActive = false;

            SetAnswerState(true);
            RunFlow(HandleLessonCorrectAnswer(letter));
        }
        else
        {
            currentMistakeCount++;
            // No AddMistake() here — Lesson Mode never scores.
            SetAnswerState(false);

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleLessonSupportThenRetry(letter));
            else
                RunFlow(HandleLessonWrongAnswer(letter));
        }
    }

    /// <summary>Step 7 (correct) — Success Message, then automatically advance to the next letter.</summary>
    private IEnumerator HandleLessonCorrectAnswer(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        string message = !string.IsNullOrWhiteSpace(letter.successMessage)
            ? letter.successMessage
            : $"Excellent! That is Letter {upper}.";

        AudioClip clip = letter.successAudio != null
            ? letter.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLetterLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 6 (wrong) — Wrong Message, then re-ask the same letter (no re-teaching, no score hit).</summary>
    private IEnumerator HandleLessonWrongAnswer(LessonLetter letter)
    {
        string message = !string.IsNullOrWhiteSpace(letter.wrongMessage)
            ? letter.wrongMessage
            : "That's not correct. Try again.";

        AudioClip clip = letter.wrongAudio != null
            ? letter.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return WaitForBrailleAnswerLesson();
    }

    /// <summary>
    /// Step 7 (support) — after 3 consecutive mistakes, remind the learner
    /// which dots make up this letter, reset the mistake streak, then let them
    /// try again. No scoring involved.
    /// </summary>
    private IEnumerator HandleLessonSupportThenRetry(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        string message = !string.IsNullOrWhiteSpace(letter.supportMessage)
            ? letter.supportMessage
            : $"Remember, Letter {upper} uses {DescribeDots(letter.GetCorrectPattern())}. Press the dot(s), then submit.";

        yield return ShowBubbleMessageSynced(message, letter.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return WaitForBrailleAnswerLesson();
    }

    // -------------------------------------------------------------------------
    // QUIZ MODE — scored assessment through the whole alphabet.
    // -------------------------------------------------------------------------

    private IEnumerator StartQuizAfterLesson()
    {
        ResetQuizScore();

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
        if (index < 0 || index >= alphabetLessons.Count)
        {
            RunFlow(CompleteScene());
            return;
        }

        currentMode = Mode.Quiz;
        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoiceAnswer = false;

        if (logDebug)
            Debug.Log($"[Quiz] Starting letter {currentLessonIndex}: {alphabetLessons[currentLessonIndex].letter}");

        RunFlow(PlayLessonFromBeginning(alphabetLessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Quiz Mode Letter Sequence
    //
    // Exact order (per letter, A through Z):
    //   1. Display Label   -> shows the current letter (e.g. "A")
    //   2. Category Label
    //   3. Prompt Message (+ audio)
    //   4. Display Image    (optional Braille reference, shown alongside the prompt)
    //   5. Wait for the player to type the matching Braille pattern
    //   6. Success Message  -> handled in HandleCorrectAnswer, then auto-advance
    //                           to the next letter (or finish after Z)
    //   7. Wrong Message    -> handled in HandleWrongAnswer, re-asks same letter
    //   8. Support Message (+ audio) -> only after 3 consecutive mistakes
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(AlphabetLetter letterLesson)
    {
        ResetAnswerState();

        // Steps 1, 2, 4: Display Label, Category Label, Display Image
        ApplyLessonDisplay(letterLesson);

        // Step 3: Prompt Message + audio
        yield return ShowPromptMessage(letterLesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 5: Wait for the player to type the Braille answer
        yield return WaitForBrailleAnswer();
    }

    /// <summary>Steps 1, 2, 4 — Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(AlphabetLetter letterLesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = char.ToUpperInvariant(letterLesson.letter).ToString();

        if (displayImageUI != null)
        {
            // Store the image but keep it hidden.
            displayImageUI.sprite = letterLesson.displayImage;
            displayImageUI.enabled = false;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(letterLesson.categoryLabel)
                ? "BRAILLE ALPHABET"
                : letterLesson.categoryLabel;
    }

    /// <summary>Step 3 — Prompt Message together with its intro/instruction audio.</summary>
    private IEnumerator ShowPromptMessage(AlphabetLetter letterLesson)
    {
        char upper = char.ToUpperInvariant(letterLesson.letter);

        string introMessage = !string.IsNullOrWhiteSpace(letterLesson.promptMessage)
            ? letterLesson.promptMessage
            : $"This is the letter {upper}. Type it in Braille.";

        yield return ShowBubbleMessageWithAudioSequence(
            introMessage,
            noAudioTextDelay,
            letterLesson.introAudio,
            letterLesson.instructionAudio
        );
    }

    /// <summary>
    /// Step 5 — marks the game as waiting for a Braille answer for the current
    /// letter. Used for the first ask and every re-ask after a wrong answer or
    /// a support message, so the "now listening for input" logic only lives here.
    /// </summary>
    private IEnumerator WaitForBrailleAnswer()
    {
        waitingForChoiceAnswer = true;
        yield break;
    }

    private void HandleAlphabetAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        AlphabetLetter letterLesson = alphabetLessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (pattern == letterLesson.GetCorrectPattern())
        {
            currentMistakeCount = 0;
            lessonActive = false;

            SetAnswerState(true);
            RunFlow(HandleCorrectAnswer(letterLesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(letterLesson));
            else
                RunFlow(HandleWrongAnswer(letterLesson));
        }
    }

    /// <summary>Step 6 — Success Message, then automatically advance to the next letter.</summary>
    private IEnumerator HandleCorrectAnswer(AlphabetLetter letterLesson)
    {
        SaveHighScoreIfNeeded();

        char upper = char.ToUpperInvariant(letterLesson.letter);

        string message = !string.IsNullOrWhiteSpace(letterLesson.successMessage)
            ? letterLesson.successMessage
            : $"Correct! That is {upper}.";

        AudioClip clip = letterLesson.successAudio != null
            ? letterLesson.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);

        // Show the Braille image after the correct answer.
        if (displayImageUI != null)
        {
            displayImageUI.sprite = letterLesson.displayImage;
            displayImageUI.enabled = letterLesson.displayImage != null;
        }

        // Let the learner see the image.
        yield return new WaitForSeconds(2f);

        // Automatically proceed to the next letter until A-Z is complete.
        StartLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 7 — Wrong Message, then re-ask the same letter (no full restart).</summary>
    private IEnumerator HandleWrongAnswer(AlphabetLetter letterLesson)
    {
        string message = !string.IsNullOrWhiteSpace(letterLesson.wrongMessage)
            ? letterLesson.wrongMessage
            : "Try again.";

        AudioClip clip = letterLesson.wrongAudio != null
            ? letterLesson.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return WaitForBrailleAnswer();
    }

    /// <summary>
    /// Step 8 — after 3 consecutive mistakes, play the Support Message + audio
    /// to help the player, reset the mistake streak, then re-ask the same letter.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(AlphabetLetter letterLesson)
    {
        string message = !string.IsNullOrWhiteSpace(letterLesson.supportMessage)
            ? letterLesson.supportMessage
            : "Here is some help. Listen carefully and try typing the letter again.";

        yield return ShowBubbleMessageSynced(message, letterLesson.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return WaitForBrailleAnswer();
    }

    // -------------------------------------------------------------------------
    // Input Handling — dispatches to Lesson Mode or Quiz Mode based on currentMode.
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice ||
            waitingForQuizTransition)
            return;

        if (!waitingForChoiceAnswer)
            return;

        if (currentMode == Mode.Lesson)
            HandleLessonAnswer(submittedPattern);
        else
            HandleAlphabetAnswer(submittedPattern);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        if (waitingForQuizTransition && canAcceptQuizChoice)
        {
            waitingForQuizTransition = false;
            canAcceptQuizChoice = false;

            RunFlow(StartLessonMode());
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0); // restart the quiz from A
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next letter is
        // in progress (lessonActive is false during that window). Without this
        // guard, a Repeat trigger fired here would stop the in-flight
        // "correct answer" coroutine before it advances, replaying the
        // just-answered letter instead of advancing.
        if (!lessonActive)
            return;

        if (sceneFinished)
            return;

        if (currentMode == Mode.Lesson)
        {
            if (currentLessonIndex < 0 || currentLessonIndex >= lessonLetters.Count)
                return;

            LessonLetter letter = lessonLetters[currentLessonIndex];

            lessonActive = true;
            waitingForChoiceAnswer = false;
            currentMistakeCount = 0;

            RunFlow(PlayLessonLetterFromBeginning(letter));
        }
        else
        {
            if (currentLessonIndex < 0 || currentLessonIndex >= alphabetLessons.Count)
                return;

            AlphabetLetter letterLesson = alphabetLessons[currentLessonIndex];

            lessonActive = true;
            waitingForChoiceAnswer = false;
            currentMistakeCount = 0;

            RunFlow(PlayLessonFromBeginning(letterLesson));
        }
    }

    private void HandleNext()
    {
        if (waitingForQuizTransition && canAcceptQuizChoice)
        {
            waitingForQuizTransition = false;
            canAcceptQuizChoice = false;

            RunFlow(StartQuizAfterLesson());
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeSceneCompletion());
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Scene Completion (after Quiz Mode finishes)
    // -------------------------------------------------------------------------

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        yield return ShowBubbleMessageSynced(completedMessage, genericCompletedAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForRepeatChoice = true;
        yield return ShowBubbleMessageSynced(repeatQuestionMessage, repeatQuestionAudio, noAudioTextDelay);
    }

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