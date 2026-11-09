using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Lesson / Practice portion of the Braille Onset-Rime experience.
///
/// This script is fully audio-driven — it has no dependency on any visual UI
/// (no TMP_Text, no Image, no typewriter effect), since the target audience is
/// blind / visually impaired learners. Content fields such as "title" and
/// "lessonText" are kept purely as design-time / documentation reference for
/// whoever is authoring lessons in the Inspector; they are never displayed.
///
/// When the lesson finishes, this script hands control over to QuizManager
/// via the public BeginQuizPhase() method — this is the only point where the
/// two scripts talk to each other.
/// </summary>
public class LessonManager : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [Header("Content (design-time reference only — not displayed)")]
        public string title;

        [TextArea(5, 10)]
        public string lessonText;

        [Header("Audio (what the learner actually hears)")]
        public AudioClip lessonAudio;
    }

    // -------------------------------------------------------------------------
    // Cross-Script Link
    // -------------------------------------------------------------------------

    [Header("Linked Quiz Script")]
    [Tooltip("Assign the QuizManager that should take over once the lesson ends.")]
    public QuizManager quizManager;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio Source")]
    [Tooltip("Can be the same AudioSource assigned to QuizManager.")]
    public AudioSource voiceAudioSource;

    [Header("Phase Audio")]
    public AudioClip welcomeAudio;   // Reserved for future intro use (unused in current flow, preserved from original script)
    public AudioClip letsLearnAudio; // Reserved for future intro use (unused in current flow, preserved from original script)
    public AudioClip endOfLessonAudio;

    [Header("Phase Text (design-time reference only — not displayed)")]
    [TextArea(2, 5)]
    public string endOfLessonPromptMessage = "You have completed the lesson. Press Space to begin the Quiz, or press R to rewatch the lesson.";

    // -------------------------------------------------------------------------
    // Content Setup
    // -------------------------------------------------------------------------

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Flow Delays")]
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private bool waitingForLessonEndChoice = false;
    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events & Braille Mapping Subscriptions
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
            Debug.Log("LessonManager initialized.");

        RunFlow(PlayLessonPages());
    }

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonPages()
    {
        waitingForLessonEndChoice = false;

        foreach (LessonPage page in lessonPages)
        {
            yield return PlayClipAndWait(page.lessonAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonEndChoice = true;
        yield return PlayClipAndWait(endOfLessonAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Navigation Action Triggers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        // Only react while we're actually waiting at the end-of-lesson prompt.
        // During the quiz phase this flag is false, so QuizManager's own
        // HandleRepeat is the one that responds instead.
        if (!waitingForLessonEndChoice)
            return;

        waitingForLessonEndChoice = false;
        RunFlow(PlayLessonPages());
    }

    private void HandleNext()
    {
        if (!waitingForLessonEndChoice)
            return;

        waitingForLessonEndChoice = false;

        if (quizManager != null)
            quizManager.BeginQuizPhase();
        else
            Debug.LogWarning("[LessonManager] No QuizManager assigned — cannot start the quiz phase.");
    }

    // -------------------------------------------------------------------------
    // Audio Helper
    // -------------------------------------------------------------------------

    private IEnumerator PlayClipAndWait(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else if (fallbackWait > 0f)
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }
}