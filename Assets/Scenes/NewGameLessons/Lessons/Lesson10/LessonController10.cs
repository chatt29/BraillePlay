using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LESSON / PRACTICE portion of the scene (audio-only — no visual UI).
///
/// Owns:  intro pages -> welcome -> "let's learn" -> for each lesson:
///        prompt audio -> story lines.
/// Hands off to <see cref="QuizController10"/> once a lesson's story is done,
/// and starts the next lesson when the quiz reports that lesson is complete.
///
/// Communication:
///   Lesson -> Quiz : quizController.StartLessonQuiz(index)
///                    quizController.BeginFinalResults()
///                    quizController.PlayRepeatQuestionConfirmPrompt()
///   Quiz -> Lesson : LessonQuizCompleted, StoryReplayRequested,
///                    StoryReplayCancelled, RestartRequested (C# events)
/// </summary>
public class LessonController10 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Data classes (Inspector-editable)
    // Text fields are TRANSCRIPTS only: they are never displayed, just logged
    // when Log Debug is on. The AudioClip is what the player actually hears.
    // -------------------------------------------------------------------------

    [Serializable]
    public class IntroLessonPage
    {
        [TextArea(2, 4)]
        public string pageText;

        public AudioClip pageAudio;
    }

    [Serializable]
    public class StoryLine
    {
        [TextArea(2, 4)]
        public string storyText;

        public AudioClip storyAudio;
    }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;

        [Header("Intro (transcript + audio)")]
        [TextArea(2, 4)]
        public string promptMessage;

        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("1. Story Section (4 lines)")]
        public List<StoryLine> storyLines = new List<StoryLine>();
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Quiz Controller (required)")]
    public QuizController10 quizController;

    [Header("Audio")]
    [Tooltip("Assign the SAME AudioSource that QuizController10 uses.")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    [Header("Scene Transcripts (never displayed — debug log only)")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;

    [Tooltip("How long to pause when a step has no audio clip assigned.")]
    public float noAudioDelay = 2f;
    public float delayBetweenStoryLines = 0.5f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;

    // True from StartLesson() until the lesson hands off to the quiz.
    // While true, the Repeat button replays the Story (see HandleRepeat).
    private bool lessonPhaseActive = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (voiceAudioSource == null)
            voiceAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        BrailleMapping.OnRepeat += HandleRepeat;

        if (quizController != null)
        {
            quizController.LessonQuizCompleted += HandleLessonQuizCompleted;
            quizController.StoryReplayRequested += HandleStoryReplayRequested;
            quizController.StoryReplayCancelled += HandleStoryReplayCancelled;
            quizController.RestartRequested += HandleQuizRestartRequested;
        }
    }

    private void OnDisable()
    {
        BrailleMapping.OnRepeat -= HandleRepeat;

        if (quizController != null)
        {
            quizController.LessonQuizCompleted -= HandleLessonQuizCompleted;
            quizController.StoryReplayRequested -= HandleStoryReplayRequested;
            quizController.StoryReplayCancelled -= HandleStoryReplayCancelled;
            quizController.RestartRequested -= HandleQuizRestartRequested;
        }
    }

    private void Start()
    {
        if (quizController == null)
        {
            Debug.LogError("[LessonController10] No QuizController10 assigned - the lesson cannot hand off to the quiz.");
            return;
        }

        if (quizController.LessonQuizCount != lessons.Count)
        {
            Debug.LogWarning($"[LessonController10] Lesson count ({lessons.Count}) does not match QuizController10 lesson quiz count ({quizController.LessonQuizCount}). Quizzes are matched to lessons by list index.");
        }

        if (logDebug)
            Debug.Log("[LessonController10] Scene started.");

        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Coroutine Helpers
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

    // -------------------------------------------------------------------------
    // Scene Flow
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        lessonPhaseActive = false;

        // Intro Lesson — plays first, before the Welcome Message.
        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(welcomeMessage, welcomeAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(letsLearnMessage, letsLearnAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    /// <summary>
    /// Plays each intro page one at a time. Empty page slots (no text and no
    /// audio) are skipped. Runs once, before the Welcome Message.
    /// </summary>
    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null) continue;
            if (string.IsNullOrWhiteSpace(page.pageText) && page.pageAudio == null) continue;

            yield return PlayVoice(page.pageText, page.pageAudio);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            // All lessons done -> the quiz controller announces the results.
            lessonPhaseActive = false;
            StopFlow();
            quizController.BeginFinalResults();
            return;
        }

        currentLessonIndex = index;
        lessonPhaseActive = true;

        if (logDebug)
            Debug.Log($"[LessonController10] Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    //   1. Prompt Message (intro + instruction audio)
    //   2. Story Section — one line + audio at a time
    //   3. Hand off to QuizController10 for this lesson's questions
    //
    // The quiz calls back (LessonQuizCompleted) when every question of the
    // lesson has been answered correctly, which starts the next lesson.
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        // Step 1: Prompt Message + audio
        yield return PlayPrompt(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 2: Story Section
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Hand off to the quiz
        lessonPhaseActive = false;
        quizController.StartLessonQuiz(currentLessonIndex);
    }

    /// <summary>Prompt Message: intro audio followed by instruction audio.</summary>
    private IEnumerator PlayPrompt(BrailleLesson lesson)
    {
        string transcript = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : lesson.displayLabel;

        LogTranscript(transcript);

        yield return PlayClip(lesson.introAudio);
        yield return PlayClip(lesson.instructionAudio);
    }

    /// <summary>
    /// Plays each story line one at a time and waits for its audio to finish.
    /// Empty slots (no text and no audio) are skipped.
    /// </summary>
    private IEnumerator PlayStory(BrailleLesson lesson)
    {
        if (lesson.storyLines == null) yield break;

        foreach (StoryLine line in lesson.storyLines)
        {
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line.storyText) && line.storyAudio == null) continue;

            yield return PlayVoice(line.storyText, line.storyAudio);
            yield return new WaitForSeconds(delayBetweenStoryLines);
        }
    }

    // -------------------------------------------------------------------------
    // Repeat handling — LESSON phase (before the quiz has started)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeat pressed while the lesson prompt/story is still playing: replay
    /// the Story, then continue on to the quiz as normal. (During the quiz,
    /// QuizController10 owns the Repeat button — see HandleStoryReplayRequested.)
    /// </summary>
    private void HandleRepeat()
    {
        if (!lessonPhaseActive) return;
        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count) return;

        RunFlow(ReplayStoryThenStartQuiz(lessons[currentLessonIndex]));
    }

    private IEnumerator ReplayStoryThenStartQuiz(BrailleLesson lesson)
    {
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        lessonPhaseActive = false;
        quizController.StartLessonQuiz(currentLessonIndex);
    }

    // -------------------------------------------------------------------------
    // Quiz -> Lesson event handlers
    // -------------------------------------------------------------------------

    /// <summary>Every question of a lesson was answered correctly.</summary>
    private void HandleLessonQuizCompleted(int lessonIndex)
    {
        StartLesson(lessonIndex + 1);
    }

    /// <summary>
    /// The player pressed Repeat during the quiz (or after a wrong answer):
    /// replay the Story only, then ask the quiz to play its
    /// "Press Next to continue" confirmation. Score, mistake count and the
    /// current question are untouched.
    /// </summary>
    private void HandleStoryReplayRequested(int lessonIndex)
    {
        if (lessonIndex < 0 || lessonIndex >= lessons.Count)
        {
            quizController.PlayRepeatQuestionConfirmPrompt();
            return;
        }

        RunFlow(ReplayStoryForQuiz(lessons[lessonIndex]));
    }

    private IEnumerator ReplayStoryForQuiz(BrailleLesson lesson)
    {
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        quizController.PlayRepeatQuestionConfirmPrompt();
    }

    /// <summary>The player pressed Next while the story replay was playing.</summary>
    private void HandleStoryReplayCancelled()
    {
        StopFlow();
    }

    /// <summary>The player chose to play the whole thing again (restart from lesson 0).</summary>
    private void HandleQuizRestartRequested()
    {
        StartLesson(0);
    }

    // -------------------------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------------------------

    /// <summary>Plays a clip and waits for it to finish. Skips silently if there is no clip.</summary>
    private IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null) yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    /// <summary>
    /// Plays a clip and waits for it to finish. If no clip is assigned, waits
    /// <see cref="noAudioDelay"/> instead so the flow timing stays the same.
    /// The transcript is only logged, never displayed.
    /// </summary>
    private IEnumerator PlayVoice(string transcript, AudioClip clip)
    {
        LogTranscript(transcript);

        if (clip != null && voiceAudioSource != null)
            yield return PlayClip(clip);
        else
            yield return new WaitForSeconds(noAudioDelay);
    }

    private void LogTranscript(string transcript)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[LessonController10] {transcript}");
    }
}