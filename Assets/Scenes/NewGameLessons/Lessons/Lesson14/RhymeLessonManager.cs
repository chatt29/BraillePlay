using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Lesson portion of the Rhyme experience.
/// Fully audio-driven (no visual UI). Text fields are design-time reference only
/// (use them as your voice-over recording script).
/// When the lesson ends, control is handed to RhymeQuizManager via BeginQuizPhase().
/// </summary>
public class RhymeLessonManager : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [Header("Content (design-time reference only - not displayed)")]
        public string title;

        [TextArea(3, 8)]
        public string lessonText;

        [Header("Audio (what the learner actually hears)")]
        public AudioClip lessonAudio;
    }

    // -------------------------------------------------------------------------
    // Cross-Script Link
    // -------------------------------------------------------------------------

    [Header("Linked Quiz Script")]
    [Tooltip("Assign the RhymeQuizManager that should take over once the lesson ends.")]
    public RhymeQuizManager quizManager;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio Source")]
    [Tooltip("Can be the same AudioSource assigned to RhymeQuizManager.")]
    public AudioSource voiceAudioSource;

    [Header("Phase Audio")]
    public AudioClip welcomeAudio;   // Reserved for future intro use
    public AudioClip letsLearnAudio; // Reserved for future intro use
    public AudioClip endOfLessonAudio;

    [Header("Phase Text (design-time reference only)")]
    [TextArea(2, 5)]
    public string endOfLessonPromptMessage = "You have completed the lesson. Press Space to begin the Quiz, or press R to listen to the lesson again.";

    // -------------------------------------------------------------------------
    // Content
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
            Debug.Log("RhymeLessonManager initialized.");

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
    // Navigation
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
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
            Debug.LogWarning("[RhymeLessonManager] No RhymeQuizManager assigned - cannot start the quiz phase.");
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
