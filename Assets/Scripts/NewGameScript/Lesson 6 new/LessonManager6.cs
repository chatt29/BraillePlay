using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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
    // Cross-script link
    // -------------------------------------------------------------------------

    [Header("Quiz Manager")]
    [Tooltip("Assign the QuizManager component. If left empty, Awake() will try GetComponent<QuizManager6>() on this same GameObject.")]
    public QuizManager6 quizManager;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    [Tooltip("Shared AudioSource. Must be the SAME AudioSource assigned on QuizManager so voice lines never overlap.")]
    public AudioSource voiceAudioSource;

    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    // -------------------------------------------------------------------------
    // Intro Lesson (plays before the Welcome Message)
    // -------------------------------------------------------------------------

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    // -------------------------------------------------------------------------
    // Scene Text (kept only as editor documentation / potential future
    // subtitle or logging hook — nothing displays these on screen)
    // -------------------------------------------------------------------------

    [Header("Scene Text (documentation only — no on-screen display)")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;
    public float delayBetweenStoryLines = 0.5f;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Public read-only state (QuizManager reads these to guard quiz input)
    // -------------------------------------------------------------------------

    public bool LessonActive => lessonActive;
    public bool SceneFinished => sceneFinished;
    public bool WaitingForRepeatChoice => waitingForRepeatChoice;

    public BrailleLesson CurrentLesson =>
        (currentLessonIndex >= 0 && currentLessonIndex < lessons.Count) ? lessons[currentLessonIndex] : null;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (quizManager == null)
            quizManager = GetComponent<QuizManager6>();
    }

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
            Debug.Log("LessonManager started.");

        if (quizManager == null)
        {
            Debug.LogError("[LessonManager] No QuizManager assigned/found — quiz flow cannot run.");
            return;
        }

        quizManager.ResetQuizScore();
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
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayAudioMessage(welcomeAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayAudioMessage(letsLearnAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null) continue;
            if (string.IsNullOrWhiteSpace(page.pageText) && page.pageAudio == null) continue;

            yield return PlayAudioMessage(page.pageAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(CompleteScene());
            return;
        }

        currentLessonIndex = index;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    //   1. Prompt Message (+ audio)
    //   2. Story Section        -> plays each story line's audio in turn
    //   3. Question Section     -> handed off to QuizManager.BeginQuestions()
    //
    // Reused both when a lesson first starts and whenever QuizManager asks
    // to replay just the story (see PlayStorySection below), so there is one
    // source of truth for "what a lesson's story looks like".
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayStorySection(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Hand off to the Quiz script for the question round. It will call
        // OnLessonQuizComplete() once all questions for this lesson are
        // answered correctly.
        quizManager.BeginQuestions(lesson, OnLessonQuizComplete);
    }

    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        yield return PlayAudioSequence(noAudioFallbackDelay, lesson.introAudio, lesson.instructionAudio);
    }

    /// <summary>
    /// Plays the Story section for a lesson. Public because QuizManager calls
    /// this directly (as a nested coroutine, not via StartCoroutine) when the
    /// player asks to replay the story — either after a wrong answer or via
    /// the mid-lesson Repeat button.
    /// </summary>
    public IEnumerator PlayStorySection(BrailleLesson lesson)
    {
        if (lesson?.storyLines == null) yield break;

        foreach (StoryLine line in lesson.storyLines)
        {
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line.storyText) && line.storyAudio == null) continue;

            yield return PlayAudioMessage(line.storyAudio, noAudioFallbackDelay);
            yield return new WaitForSeconds(delayBetweenStoryLines);
        }
    }

    /// <summary>Called by QuizManager once every question in the current lesson has been answered correctly.</summary>
    private void OnLessonQuizComplete()
    {
        StartLesson(currentLessonIndex + 1);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers — this script is the single subscriber to
    // BrailleMapping's Repeat/Next events. It handles the end-of-scene
    // "repeat everything?" prompt itself, and delegates anything mid-lesson
    // to QuizManager (since that's about replaying the story and/or
    // re-asking the current question).
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            quizManager.ResetQuizScore();
            StartLesson(0);
            return;
        }

        if (!lessonActive || sceneFinished) return;
        if (CurrentLesson == null) return;

        quizManager.RequestRepeatStory();
    }

    private void HandleNext()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeSceneCompletion());
            return;
        }

        if (!lessonActive || sceneFinished) return;

        quizManager.RequestNext();
    }

    // -------------------------------------------------------------------------
    // Scene Completion
    // -------------------------------------------------------------------------

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        quizManager.SaveHighScoreIfNeeded();

        yield return PlayAudioMessage(genericCompletedAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return quizManager.PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForRepeatChoice = true;
        yield return PlayAudioMessage(repeatQuestionAudio, noAudioFallbackDelay);
    }

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForRepeatChoice = false;

        quizManager.SaveHighScoreIfNeeded();

        yield return PlayAudioMessage(genericCompletedAudio, noAudioFallbackDelay);
        yield return quizManager.PlayFinalScoreAudio();

        quizManager.ReportFinalScore();
    }

    // -------------------------------------------------------------------------
    // Audio helpers (playback + pacing only — no on-screen text/typewriter)
    // -------------------------------------------------------------------------

    /// <summary>Plays a clip and waits for its length, or waits a fallback duration if the clip is missing.</summary>
    public IEnumerator PlayAudioMessage(AudioClip clip, float fallbackWait)
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

    /// <summary>Plays a sequence of clips back-to-back (skipping nulls); falls back to a single wait if none play.</summary>
    private IEnumerator PlayAudioSequence(float fallbackWait, params AudioClip[] clips)
    {
        bool playedAny = false;

        if (voiceAudioSource != null && clips != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                playedAny = true;
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}
