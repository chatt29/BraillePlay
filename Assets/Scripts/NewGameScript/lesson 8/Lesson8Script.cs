using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Lesson8Script : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Intro Lesson Page (plays before the Welcome Message, one page at a time)
    // -------------------------------------------------------------------------

    [Serializable]
    public class IntroLessonPage
    {
        [TextArea(2, 4)]
        public string pageText;

        public AudioClip pageAudio;
    }

    // -------------------------------------------------------------------------
    // Story Line
    // -------------------------------------------------------------------------

    [Serializable]
    public class StoryLine
    {
        [TextArea(2, 4)]
        public string storyText;

        public AudioClip storyAudio;
    }

    [Serializable]
    public class LetterFeedback
    {
        [Header("Wrong Letter Feedback")]
        [TextArea(2, 4)]
        public string wrongMessage = "Try again.";
        public AudioClip wrongAudio;

        [Header("Correct Letter Feedback")]
        [TextArea(2, 4)]
        public string correctMessage = "Correct!";
        public AudioClip correctAudio;
    }

    [Serializable]
    public class SpellingQuestion
    {
        [Header("Word to Spell")]
        public string word;

        [Header("Word Description")]
        [Tooltip("Optional description of the word (e.g. what it means), shown AFTER the player spells the word correctly — right before the success message.")]
        [TextArea(2, 6)]
        public string description;

        public AudioClip descriptionAudio;

        [TextArea(2, 4)]
        public string instruction;

        public AudioClip instructionAudio;

        [Header("Guide")]
        [TextArea(2, 6)]
        public string helpMessage;

        public AudioClip helpAudio;

        [Header("Feedback")]
        public string successMessage = "Correct!";
        public AudioClip successAudio;

        [Header("Per-Letter Feedback")]
        [Tooltip("One entry per letter position of the word (index 0 = 1st letter, index 1 = 2nd letter, etc). The wrong side is shown when the player types the wrong letter at that position; the correct side is shown when they type the right letter at that position (including right after a wrong attempt, which replaces the wrong message).")]
        public List<LetterFeedback> letterWrongFeedback = new List<LetterFeedback>();
    }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;
        public string categoryLabel = "BRAILLE";

        [Header("Intro Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string repeatMessage;

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Intro Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip repeatAudio;

        [Header("1. Story Section (4 lines)")]
        public List<StoryLine> storyLines = new List<StoryLine>();

        [Header("2. Spelling Quiz")]
        public List<SpellingQuestion> spellingQuestions = new List<SpellingQuestion>();

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
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
    // Intro Lesson (plays before the Welcome Message)
    // -------------------------------------------------------------------------

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    // -------------------------------------------------------------------------
    // Scene Text
    // -------------------------------------------------------------------------

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Repeat-Story Confirmation (mid-lesson Repeat button)")]
    [TextArea(2, 5)]
    public string repeatQuestionConfirmMessage = "Do you want to repeat the question? Press Next to continue.";
    public AudioClip repeatQuestionConfirmAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    public float delayBetweenStoryLines = 0.5f;

    [Header("Support Settings")]
    public bool resetMistakesAfterSupport = true;

    [Tooltip("Number of consecutive wrong answers on the same question before the Support Message plays. The 'repeat the story?' prompt still shows after every wrong answer, regardless of this count.")]
    public int mistakesBeforeSupport = 3;

    [Header("Repeat-Story Prompt (shown after every wrong answer)")]
    [TextArea(2, 5)]
    public string repeatStoryPromptMessage = "Do you want to hear the story again? Press Repeat to hear it again, or press Next to continue with the question.";
    public AudioClip repeatStoryPromptAudio;

    [Header("Spelling Settings")]
    [Tooltip("Only this many letters of the target word are checked/required (your spelling words are 3-letter words).")]
    public int spellingLettersToCheck = 3;

    [Header("Capitalization Settings")]
    [Tooltip("Pressing dot 6 by itself marks the next letter as capitalized. Put capital letters directly in a Spelling Question's Word field (e.g. \"Bed\") to require this on that letter.")]
    [TextArea(2, 4)]
    public string capitalizeNeededMessage = "Use capitalize the letter first by pressing dot 6 to capitalize the letter.";
    public AudioClip capitalizeNeededAudio;

    [Tooltip("Shown when the player presses dot 6 before a letter that did NOT need to be capitalized (e.g. capitalizing the 'e' in \"Bed\").")]
    [TextArea(2, 4)]
    public string capitalizeNotNeededMessage = "That letter doesn't need to be capitalized. Try again without pressing dot 6.";
    public AudioClip capitalizeNotNeededAudio;

    [Header("Repeat-Word Prompt (shown after a word is spelled correctly)")]
    [TextArea(2, 5)]
    public string repeatSpellingPromptMessage = "Do you want to spell that word again? Press Repeat to spell it again, or press Next to continue.";
    public AudioClip repeatSpellingPromptAudio;

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
    private int currentSpellingIndex = 0;

    private bool waitingForSpelling = false;

    private string currentTypedWord = "";
    private string targetWord = "";
    private int currentLetterIndex = 0;

    // True after the player presses dot 6 by itself; consumed by the next
    // letter they type (that letter is compared as uppercase).
    private bool pendingCapitalize = false;

    // Dot 6 alone (Dot1=idx0 ... Dot6=idx5) — the "capitalize next letter" chord.
    private const string CapitalizeDotPattern = "000001";

    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;

    // True whenever the player is being asked whether to repeat the Story —
    // either right after a wrong answer or after replaying the Story via the
    // mid-lesson Repeat button (repeatQuestionConfirmMessage). Does NOT touch
    // currentSpellingIndex, score, or mistake count — it only gates
    // HandleNext()/HandleRepeat() into re-asking the current spelling word or
    // replaying the story again.
    private bool waitingForRepeatConfirmation = false;

    // True after a word has been spelled correctly, while the player is
    // being asked whether to spell that same word again (Repeat) or move on
    // (Next). Gates HandleRepeat()/HandleNext() into replaying the current
    // spelling word or advancing to the next one/the next lesson.
    private bool waitingForSpellingRepeatChoice = false;

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
            Debug.Log("BrailleSoundsAround1 started.");

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

        // Intro Lesson — plays first, before the Welcome Message.
        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(welcomeMessage, welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(letsLearnMessage, letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    /// <summary>
    /// Plays each configured intro page one at a time: shows the text in the
    /// message bubble, plays its matching audio clip, waits for it to finish,
    /// then moves on to the next page. Empty/unused page slots (no text and
    /// no audio) are skipped. Runs once, before the Welcome Message.
    /// </summary>
    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null) continue;
            if (string.IsNullOrWhiteSpace(page.pageText) && page.pageAudio == null) continue;

            yield return ShowBubbleMessageSynced(page.pageText, page.pageAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeSceneCompletion());
            return;
        }

        currentLessonIndex = index;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForRepeatConfirmation = false;
        waitingForSpellingRepeatChoice = false;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    // Exact order:
    //   1. Display Label / Category Label / Display Image
    //   2. Prompt Message (+ audio)
    //   3. Story Section        -> 4 lines, one text box + audio at a time
    //   4. Spelling Section     -> for each spelling word: instruction, then
    //        per-letter input, then (once correct) the optional Word
    //        Description followed by the success message
    //   5. After the last spelling word is completed -> next lesson
    //
    // This single coroutine is reused both when a lesson first starts and
    // whenever the player asks to repeat the current lesson, so there is one
    // source of truth for "what the beginning of a lesson looks like".
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        ResetAnswerState();

        // Step 1: Display Label, Category Label, Display Image
        ApplyLessonDisplay(lesson);

        // Step 2: Prompt Message + audio
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Story Section
        yield return PlayStory(lesson);

        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Spelling Section
        currentSpellingIndex = 0;
        yield return AskSpellingQuestion(lesson, currentSpellingIndex);
    }

    /// <summary>Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(BrailleLesson lesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = lesson.displayLabel;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel)
                ? "BRAILLE"
                : lesson.categoryLabel;

        // displayLabel is also used as a fallback for the prompt bubble text
        // (see ShowPromptMessage) if promptMessage is left empty.
    }

    /// <summary>Prompt Message together with its intro/instruction audio.</summary>
    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        string introMessage = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : lesson.displayLabel;

        yield return ShowBubbleMessageWithAudioSequence(
            introMessage,
            noAudioTextDelay,
            lesson.introAudio,
            lesson.instructionAudio
        );
    }

    /// <summary>
    /// Step 3 — Story Section. Plays each of the lesson's story lines one at a
    /// time: shows the text box, plays its matching audio clip, waits for it
    /// to finish, then moves on to the next line. Empty/unused story line
    /// slots (no text and no audio) are skipped.
    /// </summary>
    private IEnumerator PlayStory(BrailleLesson lesson)
    {
        if (lesson.storyLines == null) yield break;

        foreach (StoryLine line in lesson.storyLines)
        {
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line.storyText) && line.storyAudio == null) continue;

            yield return ShowBubbleMessageSynced(line.storyText, line.storyAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayBetweenStoryLines);
        }
    }

    /// <summary>
    /// Sets up the spelling question at the given index: assigns the target
    /// word being checked against (capped to spellingLettersToCheck letters),
    /// resets the player's in-progress typed word, and shows the spelling
    /// instruction ("Spell the word..."). The player spells the word first —
    /// the Word Description (if any) is shown afterwards, once the word has
    /// been spelled correctly (see HandleCorrectSpelling). Once the last
    /// spelling word has been asked, moves on to the next lesson.
    /// </summary>
    private IEnumerator AskSpellingQuestion(BrailleLesson lesson, int index)
    {
        if (index >= lesson.spellingQuestions.Count)
        {
            StartLesson(currentLessonIndex + 1);
            yield break;
        }

        SpellingQuestion question = lesson.spellingQuestions[index];

        string word = question.word ?? string.Empty;
        int lettersToCheck = Mathf.Min(spellingLettersToCheck, word.Length);
        // Case is preserved (not lowercased) so capital letters in the Word
        // field require the player to press dot 6 before typing that letter.
        targetWord = word.Substring(0, Mathf.Max(0, lettersToCheck));

        currentTypedWord = "";
        currentLetterIndex = 0;
        pendingCapitalize = false;

        displayLabelText.text = "Spell the word";
        categoryText.text = question.word;

        // Spelling instruction — the player spells the word from this alone;
        // the Description (if any) is saved for after they get it right.
        yield return ShowBubbleMessageSynced(
            question.instruction,
            question.instructionAudio,
            noAudioTextDelay
        );

        waitingForSpelling = true;
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice || waitingForRepeatConfirmation || waitingForSpellingRepeatChoice)
            return;

        if (waitingForSpelling)
        {
            HandleSpelling(submittedPattern);
            return;
        }
    }

    /// <summary>
    /// Checks each typed letter against the expected letter at that position
    /// in the target word (capped to spellingLettersToCheck letters — the
    /// spelling words are 3-letter words). As soon as a wrong letter is
    /// typed, the wrong message configured for that letter position is
    /// shown and the player retries just that letter (progress on earlier
    /// letters is kept). When the correct letter is typed at a position —
    /// whether on the first try or after a wrong attempt — that position's
    /// correct message is shown instead (replacing any wrong message that
    /// was just shown), before moving on to the next letter. Once all
    /// required letters are typed correctly, the overall success message is
    /// shown and the player is asked whether to repeat the word or continue.
    /// </summary>
    private void HandleSpelling(string submittedPattern)
    {
        if (!waitingForSpelling) return;

        // Dot 6 by itself — flags the next letter typed as a capital.
        // Doesn't consume a letter position or count as an attempt.
        if (submittedPattern == CapitalizeDotPattern)
        {
            pendingCapitalize = true;
            return;
        }

        char letter = GetBrailleLetter(submittedPattern);
        if (letter == '\0') return;

        BrailleLesson lesson = lessons[currentLessonIndex];
        SpellingQuestion question = lesson.spellingQuestions[currentSpellingIndex];

        bool capitalizeWasRequested = pendingCapitalize;
        pendingCapitalize = false; // consumed by this letter attempt either way

        char typedLetter = capitalizeWasRequested ? char.ToUpper(letter) : letter;
        char expectedLetter = currentLetterIndex < targetWord.Length ? targetWord[currentLetterIndex] : '\0';

        if (typedLetter != expectedLetter)
        {
            waitingForSpelling = false;
            AddMistake();

            // Letter itself is right but it needed a capital and dot 6
            // wasn't pressed first — give the specific capitalize reminder
            // instead of the normal per-letter wrong message.
            bool neededCapitalize = char.IsUpper(expectedLetter)
                && !capitalizeWasRequested
                && char.ToLower(typedLetter) == char.ToLower(expectedLetter);

            // Letter itself is right but dot 6 was pressed even though this
            // letter did NOT need to be capitalized.
            bool capitalizeNotNeeded = capitalizeWasRequested
                && char.IsLower(expectedLetter)
                && char.ToLower(typedLetter) == char.ToLower(expectedLetter);

            if (neededCapitalize)
                RunFlow(HandleNeedsCapitalize(lesson, question));
            else if (capitalizeNotNeeded)
                RunFlow(HandleCapitalizeNotNeeded(lesson, question));
            else
                RunFlow(HandleWrongSpelling(lesson, question));

            return;
        }

        // Correct letter — remember which position this was for, since
        // currentLetterIndex advances right after.
        int correctedLetterIndex = currentLetterIndex;

        waitingForSpelling = false;
        SetAnswerState(true);

        currentTypedWord += typedLetter;
        currentLetterIndex++;

        if (logDebug)
            Debug.Log("Current Word: " + currentTypedWord);

        if (currentLetterIndex >= targetWord.Length)
        {
            // Last letter of the word — go straight to the overall success
            // message rather than an individual per-letter one.
            RunFlow(HandleCorrectSpelling(lesson, question));
        }
        else
        {
            // Show this letter's correct message (replacing any wrong
            // message that was on screen), then let the player continue
            // typing the next letter.
            RunFlow(HandleCorrectLetter(lesson, question, correctedLetterIndex));
        }
    }

    /// <summary>Shows the wrong message for the specific letter position that was typed incorrectly, then lets the player retry that letter.</summary>
    private IEnumerator HandleWrongSpelling(BrailleLesson lesson, SpellingQuestion question)
    {
        LetterFeedback feedback = (question.letterWrongFeedback != null && currentLetterIndex < question.letterWrongFeedback.Count)
            ? question.letterWrongFeedback[currentLetterIndex]
            : null;

        string wrongMessage = (feedback != null && !string.IsNullOrWhiteSpace(feedback.wrongMessage))
            ? feedback.wrongMessage
            : "Try again.";

        AudioClip wrongClip = (feedback != null && feedback.wrongAudio != null)
            ? feedback.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(wrongMessage, wrongClip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
    }

    /// <summary>
    /// Shown when the player types the right letter for the current position
    /// but that letter needed to be capitalized (dot 6) first and wasn't.
    /// Tells them to press dot 6 to capitalize, then lets them retry the
    /// same letter (progress on earlier letters is kept).
    /// </summary>
    private IEnumerator HandleNeedsCapitalize(BrailleLesson lesson, SpellingQuestion question)
    {
        yield return ShowBubbleMessageSynced(capitalizeNeededMessage, capitalizeNeededAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
    }

    /// <summary>
    /// Shown when the player types the right letter for the current position
    /// but pressed dot 6 first even though that letter did NOT need to be
    /// capitalized (e.g. capitalizing the "e" in "Bed"). Lets them retry the
    /// same letter without dot 6 (progress on earlier letters is kept).
    /// </summary>
    private IEnumerator HandleCapitalizeNotNeeded(BrailleLesson lesson, SpellingQuestion question)
    {
        yield return ShowBubbleMessageSynced(capitalizeNotNeededMessage, capitalizeNotNeededAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
    }

    /// <summary>
    /// Shows the correct message for the specific letter position that was
    /// just typed correctly (falling back to a generic "Correct!" if that
    /// position has no message configured), then lets the player continue
    /// with the next letter. If this letter had a wrong message showing a
    /// moment ago, this message replaces it on screen.
    /// </summary>
    private IEnumerator HandleCorrectLetter(BrailleLesson lesson, SpellingQuestion question, int letterIndex)
    {
        LetterFeedback feedback = (question.letterWrongFeedback != null && letterIndex < question.letterWrongFeedback.Count)
            ? question.letterWrongFeedback[letterIndex]
            : null;

        string correctMessage = (feedback != null && !string.IsNullOrWhiteSpace(feedback.correctMessage))
            ? feedback.correctMessage
            : "Correct!";

        AudioClip correctClip = (feedback != null && feedback.correctAudio != null)
            ? feedback.correctAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(correctMessage, correctClip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
    }

    /// <summary>
    /// Shows the per-question success message first, then the word's
    /// Description (if any) now that it's been spelled correctly, then asks
    /// the player whether they want to spell the same word again (Repeat) or
    /// move on (Next). See HandleRepeat()/HandleNext() for what each choice
    /// does.
    /// </summary>
    private IEnumerator HandleCorrectSpelling(BrailleLesson lesson, SpellingQuestion question)
    {
        SaveHighScoreIfNeeded();

        string successMessage = !string.IsNullOrWhiteSpace(question.successMessage)
            ? question.successMessage
            : "Correct!";

        AudioClip successClip = question.successAudio != null
            ? question.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(successMessage, successClip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Word Description (optional) — shown after the success message.
        if (!string.IsNullOrWhiteSpace(question.description) || question.descriptionAudio != null)
        {
            yield return ShowBubbleMessageSynced(question.description, question.descriptionAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        waitingForSpellingRepeatChoice = true;

        yield return ShowBubbleMessageSynced(
            repeatSpellingPromptMessage,
            repeatSpellingPromptAudio,
            noAudioTextDelay
        );

        // Stays here — waitingForSpellingRepeatChoice remains true until the
        // player presses Repeat or Next.
    }

    /// <summary>Advances past the current spelling word to the next one, or into the next lesson if none remain.</summary>
    private IEnumerator AdvancePastSpellingWord(BrailleLesson lesson)
    {
        currentSpellingIndex++;

        if (currentSpellingIndex < lesson.spellingQuestions.Count)
        {
            yield return AskSpellingQuestion(lesson, currentSpellingIndex);
        }
        else
        {
            if (logDebug)
                Debug.Log("All spelling words completed!");

            StartLesson(currentLessonIndex + 1);
        }
    }

    private char GetBrailleLetter(string pattern)
    {
        switch (pattern)
        {
            case "100000": return 'a';
            case "110000": return 'b';
            case "100100": return 'c';
            case "100110": return 'd';
            case "100010": return 'e';
            case "110100": return 'f';
            case "110110": return 'g';
            case "110010": return 'h';
            case "010100": return 'i';
            case "010110": return 'j';

            case "101000": return 'k';
            case "111000": return 'l';
            case "101100": return 'm';
            case "101110": return 'n';
            case "101010": return 'o';
            case "111100": return 'p';
            case "111110": return 'q';
            case "111010": return 'r';
            case "011100": return 's';
            case "011110": return 't';

            case "101001": return 'u';
            case "111001": return 'v';
            case "010111": return 'w';
            case "101101": return 'x';
            case "101111": return 'y';
            case "101011": return 'z';
        }

        return '\0';
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mid-lesson Repeat behavior:
    ///   - Replays the Story section only (Display Label/Image and the
    ///     current spelling index are left untouched — no full lesson
    ///     restart, no score reset).
    ///   - Afterwards asks the player whether they want to repeat the
    ///     current spelling word ("Press Next to continue").
    ///   - The current spelling word is only re-asked once HandleNext() fires.
    ///
    /// Separately, if the player is at the end-of-scene repeat prompt
    /// (waitingForRepeatChoice), Repeat still restarts the whole scene from
    /// lesson 0, exactly as before.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        if (waitingForSpellingRepeatChoice)
        {
            waitingForSpellingRepeatChoice = false;
            RunFlow(AskSpellingQuestion(lessons[currentLessonIndex], currentSpellingIndex));
            return;
        }

        if (!lessonActive || sceneFinished)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        // Cancel any pending spelling input wait — we're repeating the story
        // instead of waiting on a letter right now.
        waitingForSpelling = false;
        waitingForRepeatConfirmation = true;

        RunFlow(RepeatStorySection(lesson));
    }

    /// <summary>
    /// Replays just the Story section for the current lesson, then asks the
    /// player if they want to repeat the current spelling word. Does not
    /// reset currentSpellingIndex or score.
    /// </summary>
    private IEnumerator RepeatStorySection(BrailleLesson lesson)
    {
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(
            repeatQuestionConfirmMessage,
            repeatQuestionConfirmAudio,
            noAudioTextDelay
        );

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Next (see HandleNext()).
    }

    private void HandleNext()
    {
        if (waitingForSpellingRepeatChoice)
        {
            waitingForSpellingRepeatChoice = false;
            RunFlow(AdvancePastSpellingWord(lessons[currentLessonIndex]));
            return;
        }

        if (waitingForRepeatConfirmation)
        {
            waitingForRepeatConfirmation = false;
            RunFlow(AskSpellingQuestion(lessons[currentLessonIndex], currentSpellingIndex));
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

    private IEnumerator ShowBubbleMessageSynced(string message, AudioClip clip, float fallbackWait, TMP_Text targetText = null)
    {
        TMP_Text target = targetText != null ? targetText : bubbleMessageText;
        if (target == null) yield break;

        StopBubbleTyping();

        float audioDuration = GetClipDuration(clip);
        float charDelay = GetCharacterDelayForMessage(message, audioDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, target, () => typingFinished = true));

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
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, bubbleMessageText, () => typingFinished = true));

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

    private IEnumerator TypeBubbleText(string message, float characterDelay, TMP_Text target, Action onComplete = null)
    {
        if (target == null) yield break;

        if (!useTypewriterEffect)
        {
            target.text = message;
            onComplete?.Invoke();
            yield break;
        }

        target.text = string.Empty;

        if (string.IsNullOrEmpty(message))
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            target.text += message[i];
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
