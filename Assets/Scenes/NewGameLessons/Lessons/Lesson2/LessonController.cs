using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the lesson/practice portion of the scene: playing each lesson
/// page's audio in order, then waiting for the player to either repeat the
/// lesson or move on. Once the player chooses "next", this hands control off
/// to <see cref="QuizController"/> by calling its BeginQuiz() method.
///
/// This script no longer touches any visual UI - it is entirely audio and
/// input driven, since the app is designed for blind/visually impaired
/// learners.
/// </summary>
public class LessonController : MonoBehaviour
{
    [Serializable]
    public class LessonPage
    {
        [Header("Content")]
        public string title;

        [TextArea(5, 10)]
        public string lessonText;

        [Header("Audio")]
        public AudioClip lessonAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Link
    // -------------------------------------------------------------------------

    [Header("Quiz Link")]
    [Tooltip("The QuizController that takes over once the lesson pages finish.")]
    public QuizController quizController;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;

    // -------------------------------------------------------------------------
    // Lesson Content
    // -------------------------------------------------------------------------

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Choice Prompt")]
    [TextArea(2, 5)]
    public string lessonChoiceMessage =
        "You have finished learning about animal sounds. Press repeat to repeat the lesson or press next to begin the quiz.";
    public AudioClip lessonChoiceAudio;

    // -------------------------------------------------------------------------
    // Timing
    // -------------------------------------------------------------------------

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    // True while THIS script owns player input. Only one of
    // LessonController / QuizController is active at any given time, so both
    // scripts can safely subscribe to the same BrailleMapping events.
    private bool isActive = false;
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
            Debug.Log("LessonController started.");

        isActive = true;
        RunFlow(PlayLessonPages());
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
    // Lesson Flow
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            if (logDebug)
                Debug.Log($"Lesson page: {page.title}");

            yield return PlayAudioOrWait(page.lessonAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonChoice = true;

        yield return PlayAudioOrWait(lessonChoiceAudio, noAudioTextDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    /// <summary>Repeats the lesson pages from the very beginning.</summary>
    private void HandleRepeat()
    {
        if (!isActive) return;

        waitingForLessonChoice = false;
        RunFlow(PlayLessonPages());
    }

    /// <summary>Ends the lesson portion and hands control to the QuizController.</summary>
    private void HandleNext()
    {
        if (!isActive) return;
        if (!waitingForLessonChoice) return;

        waitingForLessonChoice = false;
        isActive = false;

        if (quizController != null)
            quizController.BeginQuiz();
        else
            Debug.LogWarning("[LessonController] No QuizController assigned - cannot start quiz.");
    }

    // -------------------------------------------------------------------------
    // Audio Helper
    // -------------------------------------------------------------------------

    /// <summary>Plays a clip and waits for it to finish, or waits a fallback duration if there is no clip.</summary>
    private IEnumerator PlayAudioOrWait(AudioClip clip, float fallbackWait)
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