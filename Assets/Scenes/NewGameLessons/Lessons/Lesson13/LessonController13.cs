using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the lesson/practice side of the scene: intro pages, welcome audio,
/// per-lesson prompt, story playback, and moving between lessons.
/// Also owns the shared voice AudioSource (QuizController13 plays through it
/// via PlayVoice so only one script ever touches the AudioSource).
/// Audio-only: no UI, images, or typewriter text.
/// </summary>
public class LessonController13 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Serializable]
    public class IntroLessonPage
    {
        [Tooltip("Optional note for authors / debug log only. Never shown to the player.")]
        [TextArea(2, 4)] public string transcript;
        public AudioClip audio;
    }

    [Serializable]
    public class StoryLine
    {
        [Tooltip("Optional note for authors / debug log only. Never shown to the player.")]
        [TextArea(2, 4)] public string transcript;
        public AudioClip audio;
    }

    [Serializable]
    public class LessonData
    {
        [Tooltip("For your own reference. Order MUST match QuizController13.lessonQuizzes.")]
        public string lessonName;

        [TextArea(2, 4)] public string promptTranscript;
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Story Section")]
        public List<StoryLine> storyLines = new List<StoryLine>();
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("References")]
    public QuizController13 quizController;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    [Header("Lessons")]
    public List<LessonData> lessons = new List<LessonData>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float delayBetweenStoryLines = 0.5f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Public API (used by QuizController13)
    // -------------------------------------------------------------------------

    public float DelayAfterVoice => delayAfterVoice;
    public int LessonCount => lessons != null ? lessons.Count : 0;

    /// <summary>Plays one clip on the shared voice source and waits for it to finish.
    /// A null clip returns immediately. Yield this from any coroutine.</summary>
    public IEnumerator PlayVoice(AudioClip clip, string transcript = null)
    {
        if (logDebug && !string.IsNullOrWhiteSpace(transcript))
            Debug.Log($"[Voice] {transcript}");

        if (clip == null || voiceAudioSource == null)
            yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    public void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    /// <summary>Plays the story lines of the given lesson, one at a time.
    /// Also used by the quiz when the player presses Repeat.</summary>
    public IEnumerator PlayStory(int lessonIndex)
    {
        if (!IsValidLesson(lessonIndex)) yield break;

        List<StoryLine> lines = lessons[lessonIndex].storyLines;
        if (lines == null) yield break;

        foreach (StoryLine line in lines)
        {
            if (line == null) continue;

            if (line.audio == null)
            {
                if (logDebug && !string.IsNullOrWhiteSpace(line.transcript))
                    Debug.LogWarning($"[Lesson] Story line has a transcript but no audio, skipped: {line.transcript}");
                continue;
            }

            yield return PlayVoice(line.audio, line.transcript);
            yield return new WaitForSeconds(delayBetweenStoryLines);
        }
    }

    /// <summary>Called by the quiz when every question of the current lesson is answered correctly.</summary>
    public void AdvanceToNextLesson() => StartLesson(currentLessonIndex + 1);

    /// <summary>Called by the quiz when the player chooses to play again.
    /// Restarts at lesson 0 (intro pages and welcome are not replayed, same as before).</summary>
    public void RestartFromFirstLesson() => StartLesson(0);

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private bool lessonPhaseActive = false;   // true while prompt/story is playing
    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnRepeat -= HandleRepeat;
    }

    private void Start()
    {
        if (quizController == null)
            Debug.LogError("[LessonController13] quizController is not assigned.");

        if (logDebug)
            Debug.Log("LessonController13 started.");

        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Flow helper
    // -------------------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        StopVoice();
        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Scene flow
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        lessonPhaseActive = false;

        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(welcomeAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(letsLearnAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null || page.audio == null) continue;

            yield return PlayVoice(page.audio, page.transcript);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    // -------------------------------------------------------------------------
    // Lesson sequence:  prompt -> story -> hand off to quiz
    // -------------------------------------------------------------------------

    private void StartLesson(int index)
    {
        if (index < 0 || index >= LessonCount)
        {
            // All lessons done -> quiz announces results.
            lessonPhaseActive = false;
            quizController.BeginFinalResults();
            return;
        }

        currentLessonIndex = index;
        lessonPhaseActive = true;

        if (logDebug)
            Debug.Log($"Starting lesson {index}: {lessons[index].lessonName}");

        RunFlow(PlayLessonIntro(index));
    }

    private IEnumerator PlayLessonIntro(int index)
    {
        LessonData lesson = lessons[index];

        // Prompt (intro audio, then instruction audio)
        yield return PlayVoice(lesson.introAudio, lesson.promptTranscript);
        yield return PlayVoice(lesson.instructionAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        // Story
        yield return PlayStory(index);
        yield return new WaitForSeconds(delayAfterVoice);

        HandOffToQuiz();
    }

    private void HandOffToQuiz()
    {
        lessonPhaseActive = false;
        quizController.BeginQuestions(currentLessonIndex);
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeat while the lesson (prompt/story) is still playing: replay the
    /// story, then continue into the questions. Repeat during the quiz is
    /// handled by QuizController13.
    /// </summary>
    private void HandleRepeat()
    {
        if (!lessonPhaseActive || !IsValidLesson(currentLessonIndex))
            return;

        RunFlow(RepeatStoryThenQuiz());
    }

    private IEnumerator RepeatStoryThenQuiz()
    {
        yield return PlayStory(currentLessonIndex);
        yield return new WaitForSeconds(delayAfterVoice);
        HandOffToQuiz();
    }

    private bool IsValidLesson(int index) => lessons != null && index >= 0 && index < lessons.Count;
}