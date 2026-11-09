using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the intro portion of the scene: the intro lesson pages, then the
/// welcome and "let's learn" audio. Once those are done it hands control to
/// <see cref="QuizManager6"/> by calling StartQuiz(0); from that point the
/// quiz manager owns the rest of the flow (story, questions, score, and the
/// repeat / next / back choice at the end).
///
/// This script also holds the lesson DATA (story lines, intro audio and the
/// question lists) so the Inspector assignments stay in one place; the quiz
/// manager reads the "lessons" list from here.
///
/// Audio-only - there is no visual UI, since the app targets blind/visually
/// impaired learners.
/// </summary>
public class LessonManager6 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Intro Lesson Page (plays before the Welcome Message, one page at a time)
    // -------------------------------------------------------------------------

    [Serializable]
    public class IntroLessonPage
    {
        [TextArea(2, 4)]
        [Tooltip("Kept for editor bookkeeping only; nothing renders this on screen.")]
        public string pageText;

        public AudioClip pageAudio;
    }

    // -------------------------------------------------------------------------
    // Story Line
    // -------------------------------------------------------------------------

    [Serializable]
    public class StoryLine
    {
        [TextArea(2, 4)]
        [Tooltip("Kept for editor bookkeeping only; nothing renders this on screen.")]
        public string storyText;

        public AudioClip storyAudio;
    }

    // -------------------------------------------------------------------------
    // Lesson definition (story data lives here; question data lives in
    // QuizManager6.QuizQuestion, referenced from this list)
    // -------------------------------------------------------------------------

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;

        [Header("Intro Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        [Tooltip("Currently unused by the flow; carried over from the original script for parity.")]
        public string repeatMessage;

        [Header("Intro Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Tooltip("Currently unused by the flow; carried over from the original script for parity.")]
        public AudioClip repeatAudio;

        [Header("1. Story Section (4 lines)")]
        public List<StoryLine> storyLines = new List<StoryLine>();

        [Header("2. Question Section (5 questions)")]
        public List<QuizManager6.QuizQuestion> questions = new List<QuizManager6.QuizQuestion>();

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Link
    // -------------------------------------------------------------------------

    [Header("Quiz Link")]
    [Tooltip("Assign the QuizManager6 that should take over once the intro is finished. If left empty, Awake() will try GetComponent<QuizManager6>() on this same GameObject.")]
    public QuizManager6 quizManager;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    [Tooltip("Shared AudioSource. Must be the SAME AudioSource assigned on QuizManager6 so voice lines never overlap.")]
    public AudioSource voiceAudioSource;

    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;

    [Tooltip("End-of-quiz clips. QuizManager6 uses these if its own copies are left empty.")]
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    // -------------------------------------------------------------------------
    // Intro Lesson (plays before the Welcome Message)
    // -------------------------------------------------------------------------

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    // -------------------------------------------------------------------------
    // Scene Text (documentation only - nothing displays these on screen)
    // -------------------------------------------------------------------------

    [Header("Scene Text (documentation only - no on-screen display)")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Next to finish.";

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;
    [Tooltip("Pause between story lines. QuizManager6 reads this value when it plays the story.")]
    public float delayBetweenStoryLines = 0.5f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (quizManager == null)
            quizManager = GetComponent<QuizManager6>();

        // Make sure the shared voice source never loops or auto-plays on its own.
        if (voiceAudioSource != null)
        {
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
        }
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("LessonManager6 started.");

        if (quizManager == null)
        {
            Debug.LogError("[LessonManager6] No QuizManager6 assigned/found - quiz flow cannot run.");
            return;
        }

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
        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayWelcomeAndLetsLearn();

        // Hand off to the quiz.
        StartQuiz();
    }

    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null) continue;
            if (string.IsNullOrWhiteSpace(page.pageText) && page.pageAudio == null) continue;

            yield return PlayVoiceClip(page.pageAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    private IEnumerator PlayWelcomeAndLetsLearn()
    {
        yield return PlayVoiceClip(welcomeAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoiceClip(letsLearnAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);
    }

    private void StartQuiz()
    {
        if (quizManager == null)
        {
            Debug.LogWarning("[LessonManager6] No QuizManager6 assigned - cannot start the quiz.");
            return;
        }

        quizManager.StartQuiz(0);
    }

    // -------------------------------------------------------------------------
    // Audio Helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays a clip and waits until the AudioSource has REALLY finished
    /// playing it (not just clip.length), so the next audio never cuts it
    /// off. If the clip is missing, waits a fallback duration instead.
    /// </summary>
    private IEnumerator PlayVoiceClip(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.loop = false;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();

            // Wait until the AudioSource has actually started (max 1 sec grace).
            float grace = 0f;
            while (!voiceAudioSource.isPlaying && grace < 1f)
            {
                grace += Time.unscaledDeltaTime;
                yield return null;
            }

            // Wait until the clip has REALLY finished playing.
            while (voiceAudioSource.isPlaying)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }
}