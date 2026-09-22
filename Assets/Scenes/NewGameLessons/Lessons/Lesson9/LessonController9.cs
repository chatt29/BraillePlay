using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LessonController9 — the LESSON phase of Lesson 9 (audio + Braille input only, no visuals).
///
/// Flow:
///   Lesson pages (Information Only / Interactive Practice)
///     -> "Repeat the lesson pages or go to the quiz?" choice
///        Repeat -> restart the pages      Next -> hands control to QuizController9.BeginQuiz()
///
/// This script never scores anything. All scoring lives in QuizController9.
/// It also owns the shared Braille tables, which QuizController9 reads.
/// </summary>
public class LessonController9 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Shared Braille data (public static so QuizController9 can reuse it).
    // Each pattern is 6 characters, dot 1 .. dot 6 ("100000" = dot 1 only).
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

    /// <summary>
    /// Builds the expected pattern sequence for a word: the capital indicator
    /// (if requested) before the first letter, then one pattern per letter.
    /// </summary>
    public static List<string> BuildTargetPatterns(string word, bool capitalizeFirstLetter)
    {
        var patterns = new List<string>();
        if (string.IsNullOrEmpty(word)) return patterns;

        bool isFirstLetter = true;

        foreach (char c in word)
        {
            if (!char.IsLetter(c)) continue;

            if (isFirstLetter && capitalizeFirstLetter)
                patterns.Add(BrailleCapitalIndicatorPattern);

            char upper = char.ToUpperInvariant(c);
            patterns.Add(BrailleAlphabetPatterns.TryGetValue(upper, out string pattern) ? pattern : "000000");

            isFirstLetter = false;
        }

        return patterns;
    }

    /// <summary>True when both sequences have the same length and identical entries.</summary>
    public static bool PatternsMatch(List<string> typed, List<string> target)
    {
        if (typed.Count != target.Count) return false;

        for (int i = 0; i < target.Count; i++)
        {
            if (typed[i] != target[i]) return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Lesson Page data — each page is Information Only (just teaches) or
    // Interactive Practice (teaches, then requires the learner to type a word).
    // Message strings are transcripts only: they are written to the Console
    // when Log Debug is on. The audio clips are what the learner actually hears.
    // -------------------------------------------------------------------------

    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonInfoBeat
    {
        [TextArea(2, 4)]
        [Tooltip("Transcript of the audio (Console log only).")]
        public string message;
        public AudioClip audio;
    }

    [Serializable]
    public class LessonPage
    {
        [Tooltip("Editor label only (helps you tell pages apart in the list).")]
        public string title;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats below, then moves on automatically.\nInteractive Practice: plays the beats below, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        public List<LessonInfoBeat> informationBeats = new List<LessonInfoBeat>();

        [Header("Interactive Practice (used only if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Bell'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be typed as a capital — preceded by the Braille capital indicator (Dot 6).")]
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
            => BuildTargetPatterns(practiceWord, requireCapitalFirstLetter);
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Hand-off to Quiz")]
    [Tooltip("Called when the learner chooses 'Next' after the last lesson page. Must be an ENABLED object in the scene.")]
    public QuizController9 quizController;

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Choice (after the last page)")]
    [TextArea(2, 5)]
    public string lessonChoiceMessage =
        "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";
    public AudioClip lessonChoiceAudio;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("Pause used instead of audio when a clip is missing.")]
    public float missingAudioPause = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool lessonPhaseActive;          // false once we hand off to the quiz
    private int currentPageIndex = -1;
    private bool waitingForPracticeAnswer;
    private bool waitingForCapitalIndicator;
    private bool waitingForLessonChoice;
    private int practiceMistakeCount;

    // Braille entries typed so far for the practice word (capital indicator included).
    private readonly List<string> typedPatterns = new List<string>();

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

    private void Start()
    {
        if (logDebug)
            Debug.Log("LessonController9 started.");

        lessonPhaseActive = true;
        StartLessonPage(0);
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

    /// <summary>
    /// Plays a clip and waits for it to finish. If there is no clip (or no
    /// AudioSource) it waits missingAudioPause instead so the pacing survives.
    /// The transcript is only logged.
    /// </summary>
    private IEnumerator Speak(string transcript, AudioClip clip)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[LessonController9] {transcript}");

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(missingAudioPause);
        }
    }

    // -------------------------------------------------------------------------
    // LESSON PAGES
    //
    // Each page plays its information beats in order, then (for an Interactive
    // Practice page) asks the learner to type the practice word and doesn't
    // move on until they get it right. Pages advance via StartLessonPage(index),
    // so a wrong-answer retry re-asks directly instead of "resuming" an
    // outer coroutine.
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
            Debug.Log($"Starting lesson page {index}: {lessonPages[index].title}");

        RunFlow(PlayLessonPage(lessonPages[index]));
    }

    private IEnumerator PlayLessonPage(LessonPage page)
    {
        // The "mini-lecture" part: every information beat, in order.
        foreach (LessonInfoBeat beat in page.informationBeats)
        {
            yield return Speak(beat.message, beat.audio);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (page.pageType == LessonPageType.InteractivePractice)
        {
            if (page.GetTargetPatterns().Count == 0)
            {
                // Safety net: an empty practice word could never be answered.
                Debug.LogWarning($"[LessonController9] Page {currentPageIndex} ('{page.title}') is Interactive Practice but has no practice word. Skipping the practice.");
                StartLessonPage(currentPageIndex + 1);
                yield break;
            }

            // Sets waitingForPracticeAnswer; further progress is driven by
            // HandleBrailleChordSubmitted from here on.
            yield return AskPracticeInput(page);
        }
        else
        {
            StartLessonPage(currentPageIndex + 1);
        }
    }

    /// <summary>
    /// Speaks the practice prompt, then starts listening for the practice word.
    /// Used for the first ask and every re-ask after a wrong answer or support.
    /// </summary>
    private IEnumerator AskPracticeInput(LessonPage page)
    {
        yield return Speak(page.promptMessage, page.promptAudio);

        typedPatterns.Clear();

        // Only demand the capital indicator when the page asks for it — this
        // keeps the expected input in sync with GetTargetPatterns().
        waitingForCapitalIndicator = page.requireCapitalFirstLetter;
        waitingForPracticeAnswer = true;
    }

    /// <summary>
    /// Accumulates one completed Braille chord into the practice word. The
    /// word is validated once as many entries have been typed as the target needs.
    /// </summary>
    private void HandlePracticeInput(string pattern)
    {
        if (!waitingForPracticeAnswer) return;

        LessonPage page = lessonPages[currentPageIndex];
        List<string> targetPatterns = page.GetTargetPatterns();

        // The first input must be the capital indicator (when required).
        if (waitingForCapitalIndicator)
        {
            if (pattern != BrailleCapitalIndicatorPattern)
            {
                waitingForPracticeAnswer = false;
                RegisterPracticeMistake(page);
                return;
            }

            waitingForCapitalIndicator = false;
            typedPatterns.Add(pattern);
            return;
        }

        typedPatterns.Add(pattern);

        if (typedPatterns.Count < targetPatterns.Count)
            return; // still typing — wait for the remaining letters

        waitingForPracticeAnswer = false;

        if (PatternsMatch(typedPatterns, targetPatterns))
        {
            practiceMistakeCount = 0;
            RunFlow(HandlePracticeCorrect(page));
        }
        else
        {
            RegisterPracticeMistake(page);
        }
    }

    private void RegisterPracticeMistake(LessonPage page)
    {
        practiceMistakeCount++;

        if (practiceMistakeCount >= mistakesBeforeSupport)
            RunFlow(HandlePracticeSupportThenRetry(page));
        else
            RunFlow(HandlePracticeWrong(page));
    }

    private IEnumerator HandlePracticeCorrect(LessonPage page)
    {
        AudioClip clip = page.successAudio != null ? page.successAudio : genericCorrectAudio;

        yield return Speak(page.successMessage, clip);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePracticeWrong(LessonPage page)
    {
        AudioClip clip = page.wrongAudio != null ? page.wrongAudio : genericTryAgainAudio;

        yield return Speak(page.wrongMessage, clip);
        yield return AskPracticeInput(page);
    }

    private IEnumerator HandlePracticeSupportThenRetry(LessonPage page)
    {
        yield return Speak(page.supportMessage, page.supportAudio);

        if (resetMistakesAfterSupport)
            practiceMistakeCount = 0;

        yield return AskPracticeInput(page);
    }

    // -------------------------------------------------------------------------
    // Lesson choice + hand-off to the quiz
    // -------------------------------------------------------------------------

    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        // The flag is raised before the audio so the learner can answer
        // (Repeat / Next) without waiting for the whole announcement.
        waitingForLessonChoice = true;

        yield return Speak(lessonChoiceMessage, lessonChoiceAudio);

        while (waitingForLessonChoice)
            yield return null;
    }

    // -------------------------------------------------------------------------
    // Input handlers
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonPhaseActive || !waitingForPracticeAnswer)
            return;

        HandlePracticeInput(submittedPattern);
    }

    private void HandleRepeat()
    {
        if (!lessonPhaseActive)
            return;

        if (waitingForPracticeAnswer)
        {
            // Restart the current page's practice prompt from scratch.
            waitingForPracticeAnswer = false;
            practiceMistakeCount = 0;
            RunFlow(AskPracticeInput(lessonPages[currentPageIndex]));
            return;
        }

        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            StartLessonPage(0);
            return;
        }

        // Repeat is ignored while information beats or feedback are playing.
    }

    private void HandleNext()
    {
        if (!lessonPhaseActive || !waitingForLessonChoice)
            return;

        if (quizController == null)
        {
            Debug.LogError("[LessonController9] No QuizController9 assigned - cannot begin the quiz.");
            return;
        }

        // Lesson phase is over: stop listening, silence the lesson audio, hand over.
        waitingForLessonChoice = false;
        lessonPhaseActive = false;
        StopFlow();

        quizController.BeginQuiz();
    }
}