using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LESSON PART of the scene (audio + Braille input only, no visuals).
///
/// Flow:
///   Start -> lesson page 0 -> page 1 -> ... -> last page
///         -> plays "lesson choice" audio and waits:
///              Repeat -> restart the lesson pages from page 0
///              Next   -> stops here and raises OnLessonPagesFinished
///                        (QuizController12 listens to that and begins the quiz)
///
/// Each page is either:
///   - InformationOnly      : plays its beats (audio clips) in order, then moves on.
///   - InteractivePractice  : plays its beats, then asks the learner to type the
///                            practice word in Braille (capital indicator first when
///                            required) and does not move on until it is correct.
///
/// Lesson practice is never scored.
/// This class also owns the shared Braille alphabet data, which QuizController12 reuses.
/// </summary>
public class LessonController12 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Shared Braille data (also used by QuizController12)
    // Each string has 6 characters, one per dot (dot 1 .. dot 6), matching the
    // pattern format produced by BrailleMapping ("100000" = dot 1 only).
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

    // -------------------------------------------------------------------------
    // Communication with the quiz
    // -------------------------------------------------------------------------

    /// <summary>
    /// Raised when the learner presses Next after finishing the lesson pages.
    /// QuizController12 subscribes to this to start the quiz.
    /// </summary>
    public event Action OnLessonPagesFinished;

    // -------------------------------------------------------------------------
    // Lesson page data
    // -------------------------------------------------------------------------

    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonInfoBeat
    {
        [Tooltip("Developer reference only: what the audio says. Never shown or spoken.")]
        [TextArea(2, 4)]
        public string transcript;

        public AudioClip audio;
    }

    [Serializable]
    public class LessonPage
    {
        [Header("Identity")]
        [Tooltip("Developer label used in the Inspector and debug logs only.")]
        public string title;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats below, then moves on automatically.\nInteractive Practice: plays the beats below, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        [Tooltip("Each beat is one audio clip, played in sequence. For an Interactive Practice page, these play BEFORE the practice prompt below.")]
        public List<LessonInfoBeat> informationBeats = new List<LessonInfoBeat>();

        [Header("Interactive Practice (used only if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Bell'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be typed as a capital: preceded by the Braille capital indicator (Dot 6), followed by the remaining letters.")]
        public bool requireCapitalFirstLetter = true;

        public AudioClip promptAudio;
        public AudioClip successAudio;
        public AudioClip wrongAudio;
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
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Tooltip("Played after the last page: 'Press repeat to repeat the lesson or next to begin the quiz.'")]
    public AudioClip lessonChoiceAudio;

    [Header("Lesson Pages (Information Only / Interactive Practice)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

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

    private int currentPageIndex = -1;
    private int pagePracticeMistakeCount = 0;

    private bool waitingForPagePracticeAnswer = false;
    private bool waitingForLessonChoice = false;
    private bool pageWaitingForCapitalIndicator = true;

    private readonly List<string> currentPagePracticeTypedPatterns = new List<string>();

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
        if (logDebug)
            Debug.Log("LessonController12 started.");

        waitingForPagePracticeAnswer = false;
        waitingForLessonChoice = false;

        StartLessonPage(0);
    }

    // -------------------------------------------------------------------------
    // Coroutine helpers
    // -------------------------------------------------------------------------

    /// <summary>Stops the current flow coroutine (if any) and starts a new one.</summary>
    private void RunFlow(IEnumerator routine)
    {
        StopFlow();
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
    // Lesson pages
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
        // Play every information beat in order (the "mini-lecture" part).
        foreach (LessonInfoBeat beat in page.informationBeats)
        {
            yield return PlayVoice(noAudioTextDelay, beat.audio);
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

    /// <summary>
    /// Plays the practice prompt and starts waiting for the learner to type the
    /// practice word. Used for the first ask and every re-ask after a wrong
    /// answer or a support message.
    /// </summary>
    private IEnumerator AskPagePracticeInput(LessonPage page)
    {
        yield return PlayVoice(noAudioTextDelay, page.promptAudio);

        ResetPagePracticeInput(page);
        waitingForPagePracticeAnswer = true;
    }

    private void ResetPagePracticeInput(LessonPage page)
    {
        currentPagePracticeTypedPatterns.Clear();
        pageWaitingForCapitalIndicator = page.requireCapitalFirstLetter;
    }

    // -------------------------------------------------------------------------
    // Practice input handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!waitingForPagePracticeAnswer)
            return;

        HandlePagePracticeLetterInput(submittedPattern);
    }

    /// <summary>
    /// Accumulates one completed Braille cell (capital indicator or letter) into
    /// the practice word. The word is validated once as many cells have been
    /// typed as the target requires.
    /// </summary>
    private void HandlePagePracticeLetterInput(string pattern)
    {
        LessonPage page = lessonPages[currentPageIndex];
        List<string> targetPatterns = page.GetTargetPatterns();

        // First input must be the capital indicator (when required).
        if (pageWaitingForCapitalIndicator)
        {
            if (pattern != BrailleCapitalIndicatorPattern)
            {
                waitingForPagePracticeAnswer = false;
                RegisterPagePracticeMistake(page);
                return;
            }

            pageWaitingForCapitalIndicator = false;
            currentPagePracticeTypedPatterns.Add(pattern);
            return;
        }

        currentPagePracticeTypedPatterns.Add(pattern);

        if (currentPagePracticeTypedPatterns.Count < targetPatterns.Count)
            return; // still typing the word

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
            RegisterPagePracticeMistake(page);
        }
    }

    private void RegisterPagePracticeMistake(LessonPage page)
    {
        pagePracticeMistakeCount++;

        if (pagePracticeMistakeCount >= mistakesBeforeSupport)
            RunFlow(HandlePagePracticeSupportThenRetry(page));
        else
            RunFlow(HandlePagePracticeWrongAnswer(page));
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support (practice)
    // -------------------------------------------------------------------------

    private IEnumerator HandlePagePracticeCorrectAnswer(LessonPage page)
    {
        AudioClip clip = page.successAudio != null ? page.successAudio : genericCorrectAudio;

        yield return PlayVoice(noAudioTextDelay, clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePagePracticeWrongAnswer(LessonPage page)
    {
        AudioClip clip = page.wrongAudio != null ? page.wrongAudio : genericTryAgainAudio;

        yield return PlayVoice(noAudioTextDelay, clip);

        yield return AskPagePracticeInput(page);
    }

    private IEnumerator HandlePagePracticeSupportThenRetry(LessonPage page)
    {
        yield return PlayVoice(noAudioTextDelay, page.supportAudio);

        if (resetMistakesAfterSupport)
            pagePracticeMistakeCount = 0;

        yield return AskPagePracticeInput(page);
    }

    // -------------------------------------------------------------------------
    // End of lesson: Repeat or go to the quiz
    // -------------------------------------------------------------------------

    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        waitingForLessonChoice = true;

        yield return PlayVoice(noAudioTextDelay, lessonChoiceAudio);
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
        }
    }

    private void HandleNext()
    {
        if (!waitingForLessonChoice)
            return;

        waitingForLessonChoice = false;

        // Lesson is over: silence anything still playing and hand control to the quiz.
        StopFlow();
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        if (logDebug)
            Debug.Log("LessonController12: lesson pages finished, handing over to the quiz.");

        OnLessonPagesFinished?.Invoke();
    }
}