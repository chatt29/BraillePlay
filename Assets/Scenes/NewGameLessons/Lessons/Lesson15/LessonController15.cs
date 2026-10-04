using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lesson 15 – lesson pages (information beats + interactive practice).
/// Audio + Braille input only. When the learner presses NEXT on the
/// "repeat or begin quiz" prompt, it fires OnLessonCompleted.
/// </summary>
public class LessonController15 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Shared Braille data (QuizController15 reads these too)
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

    public const string BrailleCapitalIndicatorPattern = "000001";

    // -------------------------------------------------------------------------
    // Lesson page data
    // -------------------------------------------------------------------------
    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonInfoBeat
    {
        [Tooltip("Transcript of the audio (debug log only – nothing is displayed).")]
        [TextArea(2, 4)] public string message;
        public AudioClip audio;
    }

    [Serializable]
    public class LessonPage
    {
        [Header("Identity")]
        public string title;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats, then moves on automatically.\nInteractive Practice: plays the beats, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        public List<LessonInfoBeat> informationBeats = new List<LessonInfoBeat>();

        [Header("Interactive Practice (used only if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Hit'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be preceded by the Braille capital indicator (Dot 6).")]
        public bool requireCapitalFirstLetter = true;

        [TextArea(2, 4)] public string promptMessage;
        public AudioClip promptAudio;

        [TextArea(2, 4)] public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)] public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)] public string supportMessage;
        public AudioClip supportAudio;

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
    // Events (QuizController15 listens to this)
    // -------------------------------------------------------------------------
    /// <summary>Fired when the learner chooses NEXT after the lesson pages.</summary>
    public event Action OnLessonCompleted;

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Lesson Choice Prompt")]
    [Tooltip("Transcript only (debug log).")]
    [TextArea(2, 5)]
    public string lessonChoiceMessage =
        "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";
    public AudioClip lessonChoiceAudio;

    [Header("Lesson Pages (Information Only / Interactive Practice)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("Wait time used when a message has no audio clip.")]
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Startup")]
    [Tooltip("Start the lesson automatically in Start().")]
    public bool autoStart = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------
    private int currentPageIndex = -1;
    private int pagePracticeMistakeCount = 0;

    private bool waitingForPagePracticeAnswer = false;
    private bool waitingForLessonChoice = false;
    private bool pageWaitingForCapitalIndicator = true;

    private readonly List<string> currentPagePracticeTypedPatterns = new List<string>();

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
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
        if (logDebug)
            Debug.Log("[Lesson15] LessonController15 started.");

        if (autoStart)
            BeginLesson();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------
    public void BeginLesson()
    {
        waitingForPagePracticeAnswer = false;
        waitingForLessonChoice = false;
        StartLessonPage(0);
    }

    // -------------------------------------------------------------------------
    // Flow helpers
    // -------------------------------------------------------------------------
    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

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
            Debug.Log($"[Lesson15] Starting lesson page {index}: {lessonPages[index].title}");

        RunFlow(PlayLessonPage(lessonPages[index]));
    }

    private IEnumerator PlayLessonPage(LessonPage page)
    {
        foreach (LessonInfoBeat beat in page.informationBeats)
        {
            yield return PlaySpeech(beat.audio, beat.message, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (page.pageType == LessonPageType.InteractivePractice)
            yield return AskPagePracticeInput(page);
        else
            StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator AskPagePracticeInput(LessonPage page)
    {
        string prompt = !string.IsNullOrWhiteSpace(page.promptMessage)
            ? page.promptMessage
            : $"Now it's your turn. Can you type the word {page.practiceWord}?";

        yield return PlaySpeech(page.promptAudio, prompt, noAudioTextDelay);

        currentPagePracticeTypedPatterns.Clear();
        pageWaitingForCapitalIndicator = true;
        waitingForPagePracticeAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Braille input – practice pages
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (waitingForPagePracticeAnswer)
            HandlePagePracticeLetterInput(submittedPattern);
    }

    private void HandlePagePracticeLetterInput(string pattern)
    {
        if (!waitingForPagePracticeAnswer) return;

        LessonPage page = lessonPages[currentPageIndex];
        List<string> targetPatterns = page.GetTargetPatterns();

        if (pageWaitingForCapitalIndicator)
        {
            if (pattern != BrailleCapitalIndicatorPattern)
            {
                waitingForPagePracticeAnswer = false;
                pagePracticeMistakeCount++;

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
            RunFlow(HandlePagePracticeCorrectAnswer(page));
        }
        else
        {
            pagePracticeMistakeCount++;

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

        AudioClip clip = page.successAudio != null ? page.successAudio : genericCorrectAudio;

        yield return PlaySpeech(clip, message, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePagePracticeWrongAnswer(LessonPage page)
    {
        string message = !string.IsNullOrWhiteSpace(page.wrongMessage)
            ? page.wrongMessage
            : "That's not correct. Try again.";

        AudioClip clip = page.wrongAudio != null ? page.wrongAudio : genericTryAgainAudio;

        yield return PlaySpeech(clip, message, noAudioTextDelay);
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

        yield return PlaySpeech(page.supportAudio, message, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            pagePracticeMistakeCount = 0;

        yield return AskPagePracticeInput(page);
    }

    // -------------------------------------------------------------------------
    // End of lesson: repeat or go to quiz
    // -------------------------------------------------------------------------
    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        waitingForLessonChoice = true;

        yield return PlaySpeech(lessonChoiceAudio, lessonChoiceMessage, noAudioTextDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    private void HandleRepeat()
    {
        if (waitingForPagePracticeAnswer)
        {
            LessonPage page = lessonPages[currentPageIndex];
            pagePracticeMistakeCount = 0;
            RunFlow(AskPagePracticeInput(page));
            return;
        }

        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            StartLessonPage(0);
        }
    }

    private void HandleNext()
    {
        if (!waitingForLessonChoice) return;

        waitingForLessonChoice = false;

        if (logDebug)
            Debug.Log("[Lesson15] Lesson finished – handing over to quiz.");

        OnLessonCompleted?.Invoke();
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------
    private IEnumerator PlaySpeech(AudioClip clip, string transcript, float fallbackWait)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[Lesson15] {transcript}");

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }
}