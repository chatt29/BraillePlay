using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Shared Braille data
// -----------------------------------------------------------------------------
// One alphabet table used by BOTH LessonController7 and QuizController7 so the
// dot patterns only ever have to be maintained in one place.
// (Lives in this file on purpose so the project only needs the two scripts.)
// =============================================================================
public static class BrailleAlphabet7
{
    /// <summary>Braille capital indicator (Dot 6), typed immediately before a capitalized letter.</summary>
    public const string CapitalIndicator = "000001";

    private static readonly Dictionary<char, string> Patterns = new Dictionary<char, string>
    {
        { 'A', "100000" }, { 'B', "110000" }, { 'C', "100100" }, { 'D', "100110" },
        { 'E', "100010" }, { 'F', "110100" }, { 'G', "110110" }, { 'H', "110010" },
        { 'I', "010100" }, { 'J', "010110" }, { 'K', "101000" }, { 'L', "111000" },
        { 'M', "101100" }, { 'N', "101110" }, { 'O', "101010" }, { 'P', "111100" },
        { 'Q', "111110" }, { 'R', "111010" }, { 'S', "011100" }, { 'T', "011110" },
        { 'U', "101001" }, { 'V', "111001" }, { 'W', "010111" }, { 'X', "101101" },
        { 'Y', "101111" }, { 'Z', "101011" },
    };

    /// <summary>Looks up the 6-dot pattern for a letter. Case-insensitive.</summary>
    public static bool TryGetPattern(char letter, out string pattern)
    {
        return Patterns.TryGetValue(char.ToUpperInvariant(letter), out pattern);
    }
}

/// <summary>
/// LESSON half of the old SpeechSoundsScript.
///
/// Plays a series of lesson pages. Each page is either
///   - Information Only:     plays its audio "beats" in order, then moves on, or
///   - Interactive Practice: plays its beats, then asks the learner to type a
///                           practice word in Braille (capital sign included)
///                           and won't move on until it is typed correctly.
///
/// When every page has been played, the learner hears the "lesson choice"
/// audio and picks: Repeat (replay the lesson) or Next (start the quiz).
/// Choosing Next hands control to QuizController7.BeginQuiz().
///
/// Audio only - there is no visual UI in this script.
/// </summary>
public class LessonController7 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Lesson page data
    // -------------------------------------------------------------------------
    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonPage
    {
        [Tooltip("Editor/debug label only. Never shown or spoken.")]
        public string title;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats below, then moves on automatically.\nInteractive Practice: plays the beats below, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        [Tooltip("Each clip is one 'beat', played one after another. For an Interactive Practice page these play BEFORE the practice prompt below.")]
        public List<AudioClip> informationAudio = new List<AudioClip>();

        [Header("Interactive Practice (used only if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Bell'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be typed as a capital - preceded by the Braille capital indicator (Dot 6) - followed by the remaining letters in lowercase.")]
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
                    patterns.Add(BrailleAlphabet7.CapitalIndicator);

                patterns.Add(BrailleAlphabet7.TryGetPattern(c, out string pattern) ? pattern : "000000");

                isFirstLetter = false;
            }

            return patterns;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Hand-off to the Quiz")]
    [Tooltip("Started when the learner chooses 'Next' after the lesson pages.")]
    public QuizController7 quizController;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    [Tooltip("Played after the last page: 'Press repeat to repeat the lesson or next to begin the quiz.'")]
    public AudioClip lessonChoiceAudio;
    [Tooltip("Used when a practice page has no success audio of its own.")]
    public AudioClip genericCorrectAudio;
    [Tooltip("Used when a practice page has no wrong-answer audio of its own.")]
    public AudioClip genericTryAgainAudio;

    [Header("Lesson Pages (Information Only / Interactive Practice)")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("How long to pause when a step has no audio clip assigned, so the flow never stalls.")]
    public float noAudioFallbackDelay = 2f;
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
    private bool waitingForLessonChoice = false;

    private bool waitingForPracticeAnswer = false;
    private bool waitingForCapitalIndicator = false;
    private int practiceMistakeCount = 0;
    private readonly List<string> typedPatterns = new List<string>();

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
            Debug.Log("[LessonController7] Started.");

        if (quizController == null)
            Debug.LogWarning("[LessonController7] No QuizController7 assigned - the quiz cannot start after the lesson.");

        BeginLesson();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Starts (or restarts) the lesson from page 0.</summary>
    public void BeginLesson()
    {
        waitingForLessonChoice = false;
        waitingForPracticeAnswer = false;

        StartLessonPage(0);
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
                Debug.LogWarning($"[LessonController7] No audio for: {context}");

            yield return new WaitForSeconds(noAudioFallbackDelay);
            yield break;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();

        yield return new WaitForSeconds(clip.length);
    }

    /// <summary>Returns 'preferred' if assigned, otherwise 'fallback'.</summary>
    private static AudioClip ClipOrFallback(AudioClip preferred, AudioClip fallback)
    {
        return preferred != null ? preferred : fallback;
    }

    // -------------------------------------------------------------------------
    // Lesson pages
    //
    // Each page: play its information beats in order, then (if it's an
    // Interactive Practice page) ask the learner to type the practice word and
    // don't move on until they get it right. Pages progress one at a time via
    // StartLessonPage(index), so a wrong-answer retry never has to "resume" a
    // suspended outer coroutine - it just re-asks directly.
    // -------------------------------------------------------------------------
    private void StartLessonPage(int index)
    {
        if (index < 0 || index >= lessonPages.Count)
        {
            RunFlow(FinishLessonPagesAndWaitForChoice());
            return;
        }

        currentPageIndex = index;
        waitingForPracticeAnswer = false;
        practiceMistakeCount = 0;
        typedPatterns.Clear();

        if (logDebug)
            Debug.Log($"[LessonController7] Starting lesson page {index}: {lessonPages[index].title}");

        RunFlow(PlayLessonPage(lessonPages[index]));
    }

    private IEnumerator PlayLessonPage(LessonPage page)
    {
        // The "mini-lecture" part: every information beat, in order.
        for (int i = 0; i < page.informationAudio.Count; i++)
        {
            yield return PlayVoice(page.informationAudio[i], $"page {currentPageIndex} ({page.title}), beat {i}");
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (page.pageType == LessonPageType.InteractivePractice)
        {
            // Safety net: with no practice word there is nothing to type, and a
            // player who can't see the screen would be stuck forever.
            if (page.GetTargetPatterns().Count == 0)
            {
                Debug.LogWarning($"[LessonController7] Page {currentPageIndex} ({page.title}) is Interactive Practice but has no practice word - skipping practice.");
                StartLessonPage(currentPageIndex + 1);
                yield break;
            }

            // Sets waitingForPracticeAnswer and returns; further progress is
            // driven by HandleBrailleChordSubmitted from here on.
            yield return AskPracticeInput(page);
        }
        else
        {
            StartLessonPage(currentPageIndex + 1);
        }
    }

    // -------------------------------------------------------------------------
    // Interactive practice
    // -------------------------------------------------------------------------

    /// <summary>
    /// Speaks the practice prompt, then starts waiting for the learner to type
    /// the practice word. Used for the first ask and every re-ask after a wrong
    /// answer, a support message, or a Repeat press.
    /// </summary>
    private IEnumerator AskPracticeInput(LessonPage page)
    {
        // Fresh attempt: input is ignored while the prompt is playing.
        waitingForPracticeAnswer = false;
        typedPatterns.Clear();
        waitingForCapitalIndicator = page.requireCapitalFirstLetter;

        yield return PlayVoice(page.promptAudio, $"practice prompt, page {currentPageIndex} ({page.title})");

        waitingForPracticeAnswer = true;
    }

    /// <summary>
    /// Accumulates one completed Braille letter/chord (or the capital
    /// indicator) into the practice word currently being typed. The word is
    /// only validated once as many entries have been typed as the target needs.
    /// </summary>
    private void HandlePracticeChord(string pattern)
    {
        LessonPage page = lessonPages[currentPageIndex];
        List<string> targetPatterns = page.GetTargetPatterns();

        // The first input must be the capital indicator (if required).
        if (waitingForCapitalIndicator)
        {
            if (pattern != BrailleAlphabet7.CapitalIndicator)
            {
                RegisterWrongPracticeAnswer(page);
                return;
            }

            waitingForCapitalIndicator = false;
            typedPatterns.Add(pattern);
            return;
        }

        typedPatterns.Add(pattern);

        if (typedPatterns.Count < targetPatterns.Count)
            return;

        waitingForPracticeAnswer = false;

        bool isCorrect = true;
        for (int i = 0; i < targetPatterns.Count; i++)
        {
            if (typedPatterns[i] != targetPatterns[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            practiceMistakeCount = 0;
            RunFlow(HandlePracticeCorrect(page));
        }
        else
        {
            RegisterWrongPracticeAnswer(page);
        }
    }

    /// <summary>Counts the mistake, then plays either the wrong-answer or the support flow.</summary>
    private void RegisterWrongPracticeAnswer(LessonPage page)
    {
        waitingForPracticeAnswer = false;
        practiceMistakeCount++;

        if (practiceMistakeCount >= mistakesBeforeSupport)
            RunFlow(HandlePracticeSupportThenRetry(page));
        else
            RunFlow(HandlePracticeWrong(page));
    }

    private IEnumerator HandlePracticeCorrect(LessonPage page)
    {
        AudioClip clip = ClipOrFallback(page.successAudio, genericCorrectAudio);

        yield return PlayVoice(clip, $"practice success, page {currentPageIndex} ({page.title})");
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePracticeWrong(LessonPage page)
    {
        AudioClip clip = ClipOrFallback(page.wrongAudio, genericTryAgainAudio);

        yield return PlayVoice(clip, $"practice wrong answer, page {currentPageIndex} ({page.title})");
        yield return AskPracticeInput(page);
    }

    private IEnumerator HandlePracticeSupportThenRetry(LessonPage page)
    {
        yield return PlayVoice(page.supportAudio, $"practice support, page {currentPageIndex} ({page.title})");

        if (resetMistakesAfterSupport)
            practiceMistakeCount = 0;

        yield return AskPracticeInput(page);
    }

    // -------------------------------------------------------------------------
    // End of lesson: Repeat or Next?
    // -------------------------------------------------------------------------
    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        // The learner may answer while the choice audio is still playing.
        waitingForLessonChoice = true;

        yield return PlayVoice(lessonChoiceAudio, "lesson choice");
    }

    // -------------------------------------------------------------------------
    // Input handling
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!waitingForPracticeAnswer)
            return;

        HandlePracticeChord(submittedPattern);
    }

    private void HandleRepeat()
    {
        if (waitingForPracticeAnswer)
        {
            // Restart the current page's practice prompt from scratch.
            practiceMistakeCount = 0;
            RunFlow(AskPracticeInput(lessonPages[currentPageIndex]));
            return;
        }

        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            StopVoice();
            StartLessonPage(0);
        }
    }

    private void HandleNext()
    {
        if (!waitingForLessonChoice)
            return;

        waitingForLessonChoice = false;
        StopFlow();
        StopVoice();

        if (quizController != null)
            quizController.BeginQuiz();
        else
            Debug.LogError("[LessonController7] Cannot start the quiz: no QuizController7 assigned.");
    }
}