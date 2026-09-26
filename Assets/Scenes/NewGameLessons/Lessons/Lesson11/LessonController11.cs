using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// Shared Braille data (lives in this file so you only have two scripts).
// Used by LessonController11 and QuizController11.
// -----------------------------------------------------------------------------
public static class BrailleCodes11
{
    public const string CapitalIndicatorPattern = "000001";

    public static readonly Dictionary<char, string> AlphabetPatterns = new Dictionary<char, string>
    {
        { 'A', "100000" }, { 'B', "110000" }, { 'C', "100100" }, { 'D', "100110" },
        { 'E', "100010" }, { 'F', "110100" }, { 'G', "110110" }, { 'H', "110010" },
        { 'I', "010100" }, { 'J', "010110" }, { 'K', "101000" }, { 'L', "111000" },
        { 'M', "101100" }, { 'N', "101110" }, { 'O', "101010" }, { 'P', "111100" },
        { 'Q', "111110" }, { 'R', "111010" }, { 'S', "011100" }, { 'T', "011110" },
        { 'U', "101001" }, { 'V', "111001" }, { 'W', "010111" }, { 'X', "101101" },
        { 'Y', "101111" }, { 'Z', "101011" },
    };
}

/// <summary>
/// Lesson portion of the game (audio + Braille input only).
/// Plays each lesson page, handles interactive practice pages, then asks the
/// learner to repeat the lesson or continue. Pressing Next hands off to QuizController11.
/// </summary>
public class LessonController11 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------
    public enum LessonPageType { InformationOnly, InteractivePractice }

    [Serializable]
    public class LessonInfoBeat
    {
        public AudioClip audio;
    }

    [Serializable]
    public class LessonPage
    {
        [Header("Editor Label (never shown to the player)")]
        public string title;

        [Header("Page Type")]
        [Tooltip("Information Only: plays the beats, then moves on automatically.\nInteractive Practice: plays the beats, then requires the learner to type 'Practice Word' correctly before moving on.")]
        public LessonPageType pageType = LessonPageType.InformationOnly;

        [Header("Information Beats (played in order)")]
        public List<LessonInfoBeat> informationBeats = new List<LessonInfoBeat>();

        [Header("Interactive Practice (only used if Page Type = Interactive Practice)")]
        [Tooltip("The word the learner must type, e.g. 'Ham'.")]
        public string practiceWord = "";

        [Tooltip("If true, the first letter must be preceded by the Braille capital indicator (Dot 6).")]
        public bool requireCapitalFirstLetter = true;

        public AudioClip promptAudio;
        public AudioClip successAudio;
        public AudioClip wrongAudio;
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
                    patterns.Add(BrailleCodes11.CapitalIndicatorPattern);

                char upper = char.ToUpperInvariant(c);
                patterns.Add(BrailleCodes11.AlphabetPatterns.TryGetValue(upper, out string pattern)
                    ? pattern
                    : "000000");

                isFirstLetter = false;
            }

            return patterns;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Hand-off")]
    [Tooltip("Started when the learner presses Next after the lesson pages.")]
    public QuizController11 quizController;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip lessonChoiceAudio;      // "Press repeat to repeat the lesson or next to begin the quiz."
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float delayAfterCorrect = 0.75f;
    [Tooltip("Short pause used when an audio clip is missing, so the flow never stalls.")]
    public float missingAudioDelay = 0.5f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    private bool lessonRunning = false;
    private bool waitingForLessonChoice = false;
    private bool waitingForPagePracticeAnswer = false;
    private bool pageWaitingForCapitalIndicator = true;

    private int currentPageIndex = -1;
    private int pagePracticeMistakeCount = 0;
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
        if (logDebug) Debug.Log("LessonController11 started.");

        lessonRunning = true;
        waitingForLessonChoice = false;
        waitingForPagePracticeAnswer = false;

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
        foreach (LessonInfoBeat beat in page.informationBeats)
        {
            yield return PlayVoice(beat.audio);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        if (page.pageType == LessonPageType.InteractivePractice)
            yield return AskPagePracticeInput(page);
        else
            StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator AskPagePracticeInput(LessonPage page)
    {
        yield return PlayVoice(page.promptAudio);

        currentPagePracticeTypedPatterns.Clear();
        pageWaitingForCapitalIndicator = true;
        waitingForPagePracticeAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Practice input
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonRunning) return;

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
            if (pattern != BrailleCodes11.CapitalIndicatorPattern)
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
        yield return PlayVoice(page.successAudio != null ? page.successAudio : genericCorrectAudio);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLessonPage(currentPageIndex + 1);
    }

    private IEnumerator HandlePagePracticeWrongAnswer(LessonPage page)
    {
        yield return PlayVoice(page.wrongAudio != null ? page.wrongAudio : genericTryAgainAudio);

        pageWaitingForCapitalIndicator = true;
        yield return AskPagePracticeInput(page);
    }

    private IEnumerator HandlePagePracticeSupportThenRetry(LessonPage page)
    {
        yield return PlayVoice(page.supportAudio);

        if (resetMistakesAfterSupport)
            pagePracticeMistakeCount = 0;

        pageWaitingForCapitalIndicator = true;
        yield return AskPagePracticeInput(page);
    }

    // -------------------------------------------------------------------------
    // End of lesson: Repeat or Next (Next -> quiz)
    // -------------------------------------------------------------------------
    private IEnumerator FinishLessonPagesAndWaitForChoice()
    {
        waitingForLessonChoice = true;
        yield return PlayVoice(lessonChoiceAudio);
    }

    private void HandleRepeat()
    {
        if (!lessonRunning) return;

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
        if (!lessonRunning || !waitingForLessonChoice) return;

        waitingForLessonChoice = false;
        lessonRunning = false;   // lesson input is now ignored; the quiz owns the input

        if (quizController != null)
            quizController.BeginQuiz();
        else
            Debug.LogWarning("[LessonController11] No QuizController11 assigned - the quiz cannot start.");
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------
    private IEnumerator PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null)
        {
            if (logDebug) Debug.LogWarning("[LessonController11] Missing audio clip or AudioSource.");
            yield return new WaitForSeconds(missingAudioDelay);
            yield break;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }
}