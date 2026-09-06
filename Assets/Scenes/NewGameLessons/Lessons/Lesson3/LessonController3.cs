using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the non-quiz "teaching" portion of the experience:
///   - The informational lesson pages shown before the quiz begins.
///   - Per-instrument introduction (intro message + instrument sound effect)
///     that plays right before the quiz questions for that instrument.
///   - Advancing from one instrument to the next once QuizController3 reports
///     that both of its questions have been answered correctly.
///
/// All visual/UI display (TMP_Text, Image, Sprites) has been removed. The
/// app is designed for blind/visually impaired learners, so every step
/// communicates purely through audio clips and Braille chord input.
///
/// Communication with QuizController3 (see that script for its half):
///   - Lesson calls quizController.BeginQuestions(lesson) once an
///     instrument's intro/sound has played, handing control to the quiz.
///   - Quiz raises OnLessonQuestionsComplete once both questions for that
///     instrument are answered correctly; Lesson listens for this and
///     advances to the next instrument.
///   - Lesson calls quizController.FinalizeQuiz() when there are no more
///     instruments left.
///   - Lesson reads quizController.LessonActive / WaitingForRepeatChoice to
///     decide how the Repeat button should behave at any given moment.
///   - Quiz raises OnQuizRepeatRequested if the learner chooses to replay
///     the whole quiz from the final score prompt; Lesson listens for this
///     and restarts from the first instrument.
/// </summary>
public class LessonController3 : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Data
    // ---------------------------------------------------------------

    [Serializable]
    public class LessonPage
    {
        [Header("Display")]
        public string title;

        [TextArea(5, 10)]
        public string lessonText;

        [Header("Audio")]
        public AudioClip lessonAudio;
    }

    [Serializable]
    public class InstrumentLesson
    {
        [Header("Identity")]
        public string displayLabel; // e.g. "Number 1" - kept for logging/debug only now

        [Header("Category Labels (kept for logging / future captioning use)")]
        [TextArea(2, 4)]
        public string question1Category = "LOUD OR SOFT";

        [TextArea(2, 4)]
        public string question2Category = "HIGH OR LOW PITCH";

        [Header("Introduction")]
        [TextArea(2, 4)]
        public string introductionMessage;
        public AudioClip introductionAudio;

        [Header("Instrument Sound Effect")]
        public AudioClip instrumentSoundEffect;

        // The question data lives in QuizController3's serializable classes,
        // but is authored here per-instrument since each instrument owns
        // exactly one Volume question and one Pitch question.
        [Header("Question 1 - Loud or Soft")]
        public QuizController3.VolumeQuestion volumeQuestion = new QuizController3.VolumeQuestion();

        [Header("Question 2 - High Pitch or Low Pitch")]
        public QuizController3.PitchQuestion pitchQuestion = new QuizController3.PitchQuestion();
    }

    // ---------------------------------------------------------------
    // Inspector fields
    // ---------------------------------------------------------------

    [Header("Companion Script")]
    public QuizController3 quizController;

    [Header("Audio")]
    public AudioSource voiceAudioSource;   // narrator voice - same AudioSource can be shared with QuizController3
    public AudioSource sfxAudioSource;     // dedicated source for the instrument sound effect
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip lessonChoiceAudio;

    [Header("Scene Text (kept for logging / future captioning use)")]
    [TextArea(2, 5)] public string welcomeMessage = "Welcome to Instrument Sounds!";
    [TextArea(2, 5)] public string letsLearnMessage = "Let's identify some sounds.";
    [TextArea(2, 5)]
    public string lessonChoiceMessage =
        "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Flow")]
    public List<InstrumentLesson> lessons = new List<InstrumentLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;

    [Header("Debug")]
    public bool logDebug = true;

    // ---------------------------------------------------------------
    // Private state
    // ---------------------------------------------------------------

    private int currentLessonIndex = -1;
    private bool waitingForLessonChoice = false;
    private Coroutine flowRoutine;

    // ---------------------------------------------------------------
    // Unity events
    // ---------------------------------------------------------------

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
            Debug.Log("LessonController3 started.");

        if (quizController != null)
        {
            quizController.OnLessonQuestionsComplete += HandleLessonQuestionsComplete;
            quizController.OnQuizRepeatRequested += HandleQuizRepeatRequested;
        }

        quizController?.ResetQuizScore();
        RunFlow(PlayLessonPages());
    }

    private void OnDestroy()
    {
        if (quizController != null)
        {
            quizController.OnLessonQuestionsComplete -= HandleLessonQuestionsComplete;
            quizController.OnQuizRepeatRequested -= HandleQuizRepeatRequested;
        }
    }

    // ---------------------------------------------------------------
    // Coroutine helper
    // ---------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    /// <summary>Plays a clip (if any) and waits for it; otherwise waits the fallback duration.</summary>
    private IEnumerator PlayClipAndWait(AudioClip clip, float fallbackWait)
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

    // ---------------------------------------------------------------
    // Lesson pages flow
    // ---------------------------------------------------------------

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            yield return PlayClipAndWait(page.lessonAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonChoice = true;
        yield return PlayClipAndWait(lessonChoiceAudio, noAudioTextDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    private IEnumerator StartQuizAfterLesson()
    {
        yield return PlayClipAndWait(welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayClipAndWait(letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    // ---------------------------------------------------------------
    // Per-instrument flow
    // ---------------------------------------------------------------

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            quizController?.FinalizeQuiz();
            return;
        }

        currentLessonIndex = index;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    private IEnumerator PlayLessonFromBeginning(InstrumentLesson lesson)
    {
        if (!string.IsNullOrWhiteSpace(lesson.introductionMessage) || lesson.introductionAudio != null)
        {
            yield return PlayClipAndWait(lesson.introductionAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        yield return PlayInstrumentSound(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Hand off to the quiz for this instrument's two questions.
        quizController?.BeginQuestions(lesson);
    }

    private IEnumerator PlayInstrumentSound(InstrumentLesson lesson)
    {
        AudioSource source = sfxAudioSource != null ? sfxAudioSource : voiceAudioSource;

        if (source == null || lesson.instrumentSoundEffect == null)
            yield break;

        source.Stop();
        source.clip = lesson.instrumentSoundEffect;
        source.Play();
        yield return new WaitForSeconds(lesson.instrumentSoundEffect.length);
    }

    /// <summary>Called when QuizController3 signals both questions were answered correctly.</summary>
    private void HandleLessonQuestionsComplete()
    {
        StartLesson(currentLessonIndex + 1);
    }

    // ---------------------------------------------------------------
    // Repeat / Next handlers
    // ---------------------------------------------------------------

    private void HandleRepeat()
    {
        // 1) Still choosing whether to replay the intro lesson pages.
        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            RunFlow(PlayLessonPages());
            return;
        }

        // 2) At the final score prompt, waiting to hear whether the learner
        // wants to play the whole quiz again.
        if (quizController != null && quizController.WaitingForRepeatChoice)
        {
            quizController.ConfirmRepeatChoice();
            return;
        }

        // 3) Ignore Repeat while transitioning to the next instrument
        // (QuizController3.LessonActive is false during that window).
        if (quizController == null || !quizController.LessonActive)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        // Repeat the current instrument from the very beginning: intro,
        // sound effect, then Question 1 again.
        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    private void HandleNext()
    {
        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            RunFlow(StartQuizAfterLesson());
            return;
        }

        // At the final score prompt, the learner chose to finish instead of repeat.
        if (quizController != null && quizController.WaitingForRepeatChoice)
        {
            quizController.ConfirmFinishChoice();
        }
    }

    /// <summary>Called when QuizController3 signals the learner chose to repeat the whole quiz.</summary>
    private void HandleQuizRepeatRequested()
    {
        StartLesson(0);
    }
}