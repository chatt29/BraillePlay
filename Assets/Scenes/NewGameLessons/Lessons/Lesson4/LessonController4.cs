using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LessonController4 : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [TextArea(5, 10)]
        [Tooltip("Optional reference text for designers/testers. Not shown on screen - this is an audio-only experience. Useful for Debug logging when testing without audio clips assigned.")]
        public string lessonText;

        [Header("Audio")]
        public AudioClip lessonAudio;
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

        [Header("Support After N Mistakes")]
        [Tooltip("e.g. 'Remember, Letter A uses Dot 1. Press Dot 1, then submit.' Leave blank to auto-generate.")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;

        /// <summary>Looks up the correct 6-dot Braille pattern for this letter, from the shared table on QuizController4.</summary>
        public string GetCorrectPattern()
        {
            char upper = char.ToUpperInvariant(letter);
            return QuizController4.BrailleAlphabetPatterns.TryGetValue(upper, out string pattern)
                ? pattern
                : "000000";
        }
    }

    // -------------------------------------------------------------------------
    // Cross-script link
    // -------------------------------------------------------------------------

    [Header("Quiz Hand-off")]
    [Tooltip("Assign the QuizController4 that should take over once Lesson Mode is complete and the learner chooses Next.")]
    public QuizController4 quizController;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

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

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("How long to wait after a line if no audio clip is assigned for it.")]
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

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

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    /// <summary>True from the moment this script starts driving the game until it hands off to the quiz.</summary>
    private bool lessonPhaseActive = true;

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;

    /// <summary>True while a specific letter's teach-and-ask sequence owns the flow (guards against stray Repeat input mid-transition).</summary>
    private bool letterActive = false;
    private bool waitingForChoiceAnswer = false;
    private bool waitingForQuizTransition = false;
    private bool canAcceptQuizChoice = false;

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
            Debug.Log("LessonController4 started.");

        lessonPhaseActive = true;
        RunFlow(BeginSceneFlow());
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
    // Scene Flow — Intro Pages -> Lesson Mode -> hand off to Quiz
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        waitingForQuizTransition = false;
        canAcceptQuizChoice = false;

        yield return PlayLessonPages();
    }

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            yield return PlayVoiceLine(page.lessonText, page.lessonAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        StartLetterLesson(0);
    }

    private void StartLetterLesson(int index)
    {
        if (index < 0 || index >= lessonLetters.Count)
        {
            RunFlow(CompleteLessonMode());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        letterActive = true;
        waitingForChoiceAnswer = false;
        waitingForQuizTransition = false;

        if (logDebug)
            Debug.Log($"[Lesson] Starting letter {index}: {lessonLetters[index].letter}");

        RunFlow(TeachAndAskLessonLetter(lessonLetters[index]));
    }

    /// <summary>Lesson Mode finished — offer to practice again or move on to the quiz.</summary>
    private IEnumerator CompleteLessonMode()
    {
        letterActive = false;

        yield return PlayVoiceLine(lessonCompleteMessage, lessonCompleteAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(0.5f);

        waitingForQuizTransition = true;
        canAcceptQuizChoice = true;
    }

    // -------------------------------------------------------------------------
    // Lesson Mode Letter Sequence
    //
    // Exact order (per letter, A through Z):
    //   1. Introduce the letter (+ audio)              e.g. "This is Letter A."
    //   2. Letter sound (+ audio)                        e.g. "Letter A says 'Ahhh'."
    //   3. Braille dot instruction (+ audio)             e.g. "Letter A uses Dot 1."
    //   4. Prompt the learner (+ audio)                  e.g. "Can you type Letter A?"
    //   5. Wait for the Braille answer
    //   6. Validate:
    //        correct -> success message, auto-advance to the next letter
    //        wrong   -> wrong message, retry (NO full replay of steps 1-4)
    //                   after N misses -> support message (dot reminder), retry
    //
    // No scoring of any kind happens anywhere in this sequence.
    // -------------------------------------------------------------------------

    private IEnumerator TeachAndAskLessonLetter(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        // Step 1: Introduction
        string intro = !string.IsNullOrWhiteSpace(letter.introductionMessage)
            ? letter.introductionMessage
            : $"This is Letter {upper}.";

        yield return PlayVoiceLine(intro, letter.introAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 2: Letter Sound
        string soundMessage = !string.IsNullOrWhiteSpace(letter.letterSoundMessage)
            ? letter.letterSoundMessage
            : $"Letter {upper} makes its own sound.";

        yield return PlayVoiceLine(soundMessage, letter.letterSoundAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Braille Dot Instruction
        string brailleMessage = !string.IsNullOrWhiteSpace(letter.brailleInstructionMessage)
            ? letter.brailleInstructionMessage
            : $"Letter {upper} uses {QuizController4.DescribeDots(letter.GetCorrectPattern())}.";

        yield return PlayVoiceLine(brailleMessage, letter.brailleInstructionAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Prompt
        string prompt = !string.IsNullOrWhiteSpace(letter.promptMessage)
            ? letter.promptMessage
            : $"Now it's your turn. Can you type Letter {upper}?";

        yield return PlayVoiceLine(prompt, letter.promptAudio, noAudioFallbackDelay);

        // Step 5: Start waiting for the Braille answer.
        waitingForChoiceAnswer = true;
    }

    private void HandleLessonAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        LessonLetter letter = lessonLetters[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (pattern == letter.GetCorrectPattern())
        {
            currentMistakeCount = 0;
            letterActive = false;

            RunFlow(HandleLessonCorrectAnswer(letter));
        }
        else
        {
            currentMistakeCount++;
            // No score changes here — Lesson Mode never scores.

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleLessonSupportThenRetry(letter));
            else
                RunFlow(HandleLessonWrongAnswer(letter));
        }
    }

    /// <summary>Step 6 (correct) — Success Message, then automatically advance to the next letter.</summary>
    private IEnumerator HandleLessonCorrectAnswer(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        string message = !string.IsNullOrWhiteSpace(letter.successMessage)
            ? letter.successMessage
            : $"Excellent! That is Letter {upper}.";

        AudioClip clip = letter.successAudio != null ? letter.successAudio : genericCorrectAudio;

        yield return PlayVoiceLine(message, clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLetterLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 6 (wrong) — Wrong Message, then re-ask the same letter (no re-teaching, no score hit).</summary>
    private IEnumerator HandleLessonWrongAnswer(LessonLetter letter)
    {
        string message = !string.IsNullOrWhiteSpace(letter.wrongMessage)
            ? letter.wrongMessage
            : "That's not correct. Try again.";

        AudioClip clip = letter.wrongAudio != null ? letter.wrongAudio : genericTryAgainAudio;

        yield return PlayVoiceLine(message, clip, noAudioFallbackDelay);
        waitingForChoiceAnswer = true;
    }

    /// <summary>
    /// Step 6 (support) — after N consecutive mistakes, remind the learner
    /// which dots make up this letter, reset the mistake streak, then let them
    /// try again. No scoring involved.
    /// </summary>
    private IEnumerator HandleLessonSupportThenRetry(LessonLetter letter)
    {
        char upper = char.ToUpperInvariant(letter.letter);

        string message = !string.IsNullOrWhiteSpace(letter.supportMessage)
            ? letter.supportMessage
            : $"Remember, Letter {upper} uses {QuizController4.DescribeDots(letter.GetCorrectPattern())}. Press the dot(s), then submit.";

        yield return PlayVoiceLine(message, letter.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        waitingForChoiceAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonPhaseActive) return;
        if (!letterActive || waitingForQuizTransition) return;
        if (!waitingForChoiceAnswer) return;

        HandleLessonAnswer(submittedPattern);
    }

    private void HandleRepeat()
    {
        if (!lessonPhaseActive) return;

        if (waitingForQuizTransition && canAcceptQuizChoice)
        {
            waitingForQuizTransition = false;
            canAcceptQuizChoice = false;
            StartLetterLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next letter is
        // already in progress (letterActive is false during that window).
        if (!letterActive) return;
        if (currentLessonIndex < 0 || currentLessonIndex >= lessonLetters.Count) return;

        LessonLetter letter = lessonLetters[currentLessonIndex];

        letterActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;

        RunFlow(TeachAndAskLessonLetter(letter));
    }

    private void HandleNext()
    {
        if (!lessonPhaseActive) return;
        if (!(waitingForQuizTransition && canAcceptQuizChoice)) return;

        waitingForQuizTransition = false;
        canAcceptQuizChoice = false;
        lessonPhaseActive = false;

        if (quizController != null)
            quizController.StartQuiz();
        else
            Debug.LogWarning("[LessonController4] No QuizController4 assigned - cannot start the quiz.");
    }

    // -------------------------------------------------------------------------
    // Voice Line Playback (audio-only — no text UI)
    // -------------------------------------------------------------------------

    private IEnumerator PlayVoiceLine(string debugMessage, AudioClip clip, float fallbackWait)
    {
        if (logDebug && !string.IsNullOrEmpty(debugMessage))
            Debug.Log($"[Lesson VO] {debugMessage}");

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