using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the lesson / practice portion of the scene: the intro lesson
/// pages that play before the quiz starts. This script is audio-only -
/// there is no visual UI, since the app targets blind/visually impaired
/// learners. All progression is driven by audio playback and Braille
/// chord input (via BrailleMapping's Repeat / Yes-or-Next events).
///
/// Flow (unchanged from the original combined script):
///   1. Play each lesson page's audio in order.
///   2. Ask the player whether to repeat the lesson pages or move on
///      (R = repeat, Y = start the quiz).
///   3. Play the welcome + "let's learn" audio.
///   4. Hand off control to QuizController1 to begin the quiz.
/// </summary>
public class LessonController1 : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [Header("Audio")]
        public AudioClip lessonAudio;
    }

    [Header("Quiz Link")]
    [Tooltip("Assign the QuizController1 that should take over once the lesson pages are finished.")]
    public QuizController1 quizController;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip repeatLessonAudio;

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool waitingForLessonChoice = false;
    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
    }

    private void OnDisable()
    {
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("LessonController1 started.");

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
    // Scene Flow
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        waitingForLessonChoice = false;

        // Play all lesson pages first, then wait for the player's repeat/next choice.
        yield return PlayLessonPages();

        yield return PlayWelcomeAndLetsLearn();

        // Hand off to the quiz.
        StartQuiz();
    }

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            yield return PlayVoiceClip(page.lessonAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonChoice = true;

        yield return PlayVoiceClip(repeatLessonAudio, noAudioFallbackDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    private IEnumerator PlayWelcomeAndLetsLearn()
    {
        yield return PlayVoiceClip(welcomeAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoiceClip(letsLearnAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);
    }

    private IEnumerator StartQuizAfterLesson()
    {
        yield return PlayWelcomeAndLetsLearn();
        StartQuiz();
    }

    private void StartQuiz()
    {
        if (quizController == null)
        {
            Debug.LogWarning("[LessonController1] No QuizController1 assigned - cannot start the quiz.");
            return;
        }

        quizController.StartQuiz(0);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        if (!waitingForLessonChoice)
            return;

        waitingForLessonChoice = false;
        RunFlow(PlayLessonPages());
    }

    private void HandleNext()
    {
        if (!waitingForLessonChoice)
            return;

        waitingForLessonChoice = false;
        RunFlow(StartQuizAfterLesson());
    }

    // -------------------------------------------------------------------------
    // Audio Helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays a single audio clip and waits for its length; if no clip is
    /// assigned, waits the given fallback delay instead. No text/UI involved.
    /// </summary>
    private IEnumerator PlayVoiceClip(AudioClip clip, float fallbackWait)
    {
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