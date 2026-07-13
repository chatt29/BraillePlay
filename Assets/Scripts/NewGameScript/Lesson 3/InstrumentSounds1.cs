using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InstrumentSounds1 : MonoBehaviour
{
    public enum VolumeAnswer { Loud, Soft }
    public enum PitchAnswer { High, Low }

    // -------------------------------------------------------------------------
    // Lesson Pages (ported from AnimalSounds2) — plain informational pages
    // shown before the quiz begins. Each page shows text + plays audio, with
    // no player input required.
    // -------------------------------------------------------------------------

    [Serializable]
    public class LessonPage
    {
        [Header("Display")]
        public string title;

        [TextArea(5, 10)]
        public string lessonText;

        public Sprite lessonImage;

        [Header("Audio")]
        public AudioClip lessonAudio;
    }

    // -------------------------------------------------------------------------
    // Question sub-classes (kept separate so each question's prompt / success /
    // wrong / support content is self-contained and easy to author in Inspector)
    // -------------------------------------------------------------------------

    [Serializable]
    public class VolumeQuestion
    {
        [TextArea(2, 4)]
        public string promptMessage = "Is the sound it produced loud or soft? Press dot 1 for Loud or dot 2 for Soft.";
        public AudioClip promptAudio;

        public VolumeAnswer correctAnswer = VolumeAnswer.Loud;

        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    [Serializable]
    public class PitchQuestion
    {
        [TextArea(2, 4)]
        public string promptMessage = "Does the instrument produce a high-pitch sound or a low-pitch sound? Press dot 1 for High Pitch or dot 2 for Low Pitch.";
        public AudioClip promptAudio;

        public PitchAnswer correctAnswer = PitchAnswer.High;

        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;
        public AudioClip wrongAudio;

        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    [Serializable]
    public class InstrumentLesson
    {
        [Header("Identity")]
        public string displayLabel;      // e.g. "Number 1"

        [Header("Category Labels")]

        [TextArea(2, 4)]
        public string question1Category = "LOUD OR SOFT";

        [TextArea(2, 4)]
        public string question2Category = "HIGH OR LOW PITCH"; // e.g. "Drum"

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Introduction")]
        [TextArea(2, 4)]
        public string introductionMessage;

        public AudioClip introductionAudio;

        [Header("Instrument Sound Effect")]
        public AudioClip instrumentSoundEffect;

        [Header("Question 1 - Loud or Soft")]
        public VolumeQuestion volumeQuestion = new VolumeQuestion();

        [Header("Question 2 - High Pitch or Low Pitch")]
        public PitchQuestion pitchQuestion = new PitchQuestion();
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text displayLabelText;
    public TMP_Text categoryText;
    public TMP_Text livePatternText;
    public Image displayImageUI;

    [Header("Answer State Image")]
    public Image answerStateImage;
    public Sprite correctStateSprite;
    public Sprite wrongStateSprite;
    public bool hideAnswerStateAtStart = false;

    [Header("Quiz Score UI")]
    public TMP_Text fixScoreText;
    public TMP_Text wrongScoreText;
    public TMP_Text totalScoreText;
    public TMP_Text highScoreText;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "InstrumentSoundsHighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource; // dedicated source for the instrument sound effect
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Scene Text
    // -------------------------------------------------------------------------

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Instrument Sounds!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [TextArea(2, 5)]
    public string lessonChoiceMessage =
    "You have finished the lesson pages. Press repeat to repeat them or press next to begin the quiz.";

    public AudioClip lessonChoiceAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Lesson Flow")]
    public List<InstrumentLesson> lessons = new List<InstrumentLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Typewriter Sync")]
    public bool useTypewriterEffect = true;
    [Min(0.005f)] public float defaultTypewriterCharacterDelay = 0.03f;
    [Min(0.001f)] public float minSyncedCharacterDelay = 0.01f;
    [Min(0.001f)] public float maxSyncedCharacterDelay = 0.12f;
    public bool waitForFullAudioBeforeContinuing = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private enum QuestionStage { None, Question1, Question2 }

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private QuestionStage currentStage = QuestionStage.None;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForChoiceAnswer = false;
    private bool waitingForLessonChoice = false;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

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

    private void Update()
    {
        if (livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("InstrumentSounds1 started.");

        ResetQuizScore();
        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------

    private void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);

        UpdateScoreUI();
        ResetAnswerState();
    }

    private void AddMistake()
    {
        totalWrongCount++;

        int deductions = totalWrongCount / 3;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        UpdateScoreUI();
        SetAnswerState(false);
    }

    private void UpdateScoreUI()
    {
        if (fixScoreText != null) fixScoreText.text = fixedScore.ToString();
        if (wrongScoreText != null) wrongScoreText.text = totalWrongCount.ToString();
        if (totalScoreText != null) totalScoreText.text = totalScore.ToString();
        if (highScoreText != null) highScoreText.text = highScore.ToString();
    }

    private void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }

        UpdateScoreUI();
    }

    // -------------------------------------------------------------------------
    // Answer State Image
    // -------------------------------------------------------------------------

    private void SetAnswerState(bool isCorrect)
    {
        if (answerStateImage == null) return;

        answerStateImage.enabled = true;
        answerStateImage.sprite = isCorrect ? correctStateSprite : wrongStateSprite;
    }

    private void ResetAnswerState()
    {
        if (answerStateImage == null) return;

        if (hideAnswerStateAtStart)
        {
            answerStateImage.enabled = false;
        }
        else
        {
            answerStateImage.enabled = true;
            answerStateImage.sprite = wrongStateSprite;
        }
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
        waitingForLessonChoice = false;

        yield return PlayLessonPages();
    }

    /// <summary>
    /// Plays each informational Lesson Page in order (text + audio, no input
    /// required), then waits for the player to choose Repeat (replay the
    /// pages) or Next (move on to the quiz).
    /// </summary>
    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            if (displayLabelText != null)
                displayLabelText.text = page.title;

            if (categoryText != null)
                categoryText.text = "LESSON";

            if (displayImageUI != null)
            {
                displayImageUI.sprite = page.lessonImage;
                displayImageUI.enabled = page.lessonImage != null;
            }

            yield return ShowBubbleMessageSynced(
                page.lessonText,
                page.lessonAudio,
                noAudioTextDelay
            );

            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonChoice = true;

        yield return ShowBubbleMessageSynced(
            lessonChoiceMessage,
            lessonChoiceAudio,
            noAudioTextDelay);

        while (waitingForLessonChoice)
            yield return null;
    }

    /// <summary>Welcome/intro messages, then starts the first instrument quiz item.</summary>
    private IEnumerator StartQuizAfterLesson()
    {
        yield return ShowBubbleMessageSynced(welcomeMessage, welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(letsLearnMessage, letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeSceneCompletion());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoiceAnswer = false;
        currentStage = QuestionStage.None;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel} / {lessons[currentLessonIndex].question1Category}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    // Exact order:
    //   1. Display Label + Category Label (lesson number + instrument name)
    //   2. Play instrument sound effect
    //   3. Ask Question 1 (Loud / Soft)      -> dot 1 = Loud, dot 2 = Soft
    //   4. Validate: correct -> success, move to Question 2
    //                wrong   -> wrong message, retry (support after 3 misses)
    //   5. Ask Question 2 (High / Low pitch) -> dot 1 = High, dot 2 = Low
    //   6. Validate: correct -> success, lesson complete, advance
    //                wrong   -> wrong message, retry (support after 3 misses)
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(InstrumentLesson lesson)
    {
        ResetAnswerState();
        currentMistakeCount = 0;

        // Step 1: Display Label, Category Label, Display Image
        ApplyLessonDisplay(lesson);

        // Step 2: Introduction message + audio
        if (!string.IsNullOrWhiteSpace(lesson.introductionMessage) || lesson.introductionAudio != null)
        {
            yield return ShowBubbleMessageSynced(
                lesson.introductionMessage,
                lesson.introductionAudio,
                noAudioTextDelay
            );

            yield return new WaitForSeconds(delayAfterVoice);
        }

        // Step 3: Instrument sound effect
        yield return PlayInstrumentSound(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Ask Question 1
        yield return AskQuestion1(lesson);
    }

    /// <summary>Step 1 — Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(InstrumentLesson lesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = lesson.displayLabel;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (categoryText != null)
        {
            categoryText.text = string.IsNullOrWhiteSpace(lesson.question1Category)
                ? "LOUD OR SOFT"
                : lesson.question1Category;
        }
    }

    /// <summary>Step 2 — Play the instrument's sound effect and wait for it to finish.</summary>
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

    // -------------------------------------------------------------------------
    // Question 1 — Loud or Soft
    // -------------------------------------------------------------------------

    private IEnumerator AskQuestion1(InstrumentLesson lesson)
    {
        currentStage = QuestionStage.Question1;
        waitingForChoiceAnswer = true;

        yield return ShowBubbleMessageSynced(
            lesson.volumeQuestion.promptMessage,
            lesson.volumeQuestion.promptAudio,
            noAudioTextDelay
        );
    }

    private void HandleQuestion1Answer(string pattern)
    {
        VolumeAnswer? selected = MapDotToVolumeAnswer(pattern);
        if (selected == null) return; // Unrecognized pattern: keep waiting.

        InstrumentLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected.Value == lesson.volumeQuestion.correctAnswer)
        {
            currentMistakeCount = 0;
            SetAnswerState(true);
            RunFlow(HandleQuestion1Correct(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleQuestion1Support(lesson));
            else
                RunFlow(HandleQuestion1Wrong(lesson));
        }
    }

    private IEnumerator HandleQuestion1Correct(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.volumeQuestion.successMessage)
            ? lesson.volumeQuestion.successMessage
            : "Correct!";

        AudioClip clip = lesson.volumeQuestion.successAudio != null
            ? lesson.volumeQuestion.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        // Immediately continue to the second question.
        currentMistakeCount = 0;
        yield return AskQuestion2(lesson);
    }

    private IEnumerator HandleQuestion1Wrong(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.volumeQuestion.wrongMessage)
            ? lesson.volumeQuestion.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.volumeQuestion.wrongAudio != null
            ? lesson.volumeQuestion.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return AskQuestion1(lesson);
    }

    private IEnumerator HandleQuestion1Support(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.volumeQuestion.supportMessage)
            ? lesson.volumeQuestion.supportMessage
            : "Here is some help. Listen carefully to the sound again.";

        yield return ShowBubbleMessageSynced(message, lesson.volumeQuestion.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuestion1(lesson);
    }

    // -------------------------------------------------------------------------
    // Question 2 — High Pitch or Low Pitch
    // -------------------------------------------------------------------------

    private IEnumerator AskQuestion2(InstrumentLesson lesson)
    {
        currentStage = QuestionStage.Question2;
        waitingForChoiceAnswer = true;

        if (categoryText != null)
        {
            categoryText.text = string.IsNullOrWhiteSpace(lesson.question2Category)
                ? "HIGH OR LOW PITCH"
                : lesson.question2Category;
        }


        yield return ShowBubbleMessageSynced(
            lesson.pitchQuestion.promptMessage,
            lesson.pitchQuestion.promptAudio,
            noAudioTextDelay
        );
    }

    private void HandleQuestion2Answer(string pattern)
    {
        PitchAnswer? selected = MapDotToPitchAnswer(pattern);
        if (selected == null) return; // Unrecognized pattern: keep waiting.

        InstrumentLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected.Value == lesson.pitchQuestion.correctAnswer)
        {
            currentMistakeCount = 0;
            lessonActive = false;

            SetAnswerState(true);
            RunFlow(HandleQuestion2Correct(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleQuestion2Support(lesson));
            else
                RunFlow(HandleQuestion2Wrong(lesson));
        }
    }

    private IEnumerator HandleQuestion2Correct(InstrumentLesson lesson)
    {
        SaveHighScoreIfNeeded();

        string message = !string.IsNullOrWhiteSpace(lesson.pitchQuestion.successMessage)
            ? lesson.pitchQuestion.successMessage
            : $"Correct! Lesson complete.";

        AudioClip clip = lesson.pitchQuestion.successAudio != null
            ? lesson.pitchQuestion.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        // Lesson complete -> advance to the next instrument.
        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator HandleQuestion2Wrong(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.pitchQuestion.wrongMessage)
            ? lesson.pitchQuestion.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.pitchQuestion.wrongAudio != null
            ? lesson.pitchQuestion.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return AskQuestion2(lesson);
    }

    private IEnumerator HandleQuestion2Support(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.pitchQuestion.supportMessage)
            ? lesson.pitchQuestion.supportMessage
            : "Here is some help. Listen carefully to the sound again.";

        yield return ShowBubbleMessageSynced(message, lesson.pitchQuestion.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuestion2(lesson);
    }

    // -------------------------------------------------------------------------
    // Braille Dot -> Answer Mapping
    //
    // Dot 1 = "100000" (first choice: Loud / High Pitch)
    // Dot 2 = "010000" (second choice: Soft / Low Pitch)
    // -------------------------------------------------------------------------

    private VolumeAnswer? MapDotToVolumeAnswer(string pattern)
    {
        if (pattern == "100000") return VolumeAnswer.Loud;
        if (pattern == "010000") return VolumeAnswer.Soft;
        return null;
    }

    private PitchAnswer? MapDotToPitchAnswer(string pattern)
    {
        if (pattern == "100000") return PitchAnswer.High;
        if (pattern == "010000") return PitchAnswer.Low;
        return null;
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (!waitingForChoiceAnswer)
            return;

        switch (currentStage)
        {
            case QuestionStage.Question1:
                HandleQuestion1Answer(submittedPattern);
                break;
            case QuestionStage.Question2:
                HandleQuestion2Answer(submittedPattern);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeats ONLY the current lesson/instrument from the very beginning:
    /// Display Label, Category Label, instrument sound effect, and Question 1.
    /// It never advances to the next lesson and never replays a previous one.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            RunFlow(PlayLessonPages());
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next lesson is
        // in progress (lessonActive is false during that window).
        if (!lessonActive)
            return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        InstrumentLesson lesson = lessons[currentLessonIndex];

        lessonActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;
        currentStage = QuestionStage.None;

        RunFlow(PlayLessonFromBeginning(lesson));
    }

    private void HandleNext()
    {
        if (waitingForLessonChoice)
        {
            waitingForLessonChoice = false;
            RunFlow(StartQuizAfterLesson());
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Scene Completion
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return ShowBubbleMessageSynced(finalMessage, genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[InstrumentSounds1] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final Score Audio
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        if (yourScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = yourScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(yourScoreIsAudio.length);
        }

        if (finalScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = finalScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(finalScoreClip.length);
        }

        if (whileYourHighestScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = whileYourHighestScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(whileYourHighestScoreIsAudio.length);
        }

        if (highScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = highScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(highScoreClip.length);
        }
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Bubble Text / Typewriter
    // -------------------------------------------------------------------------

    private IEnumerator ShowBubbleMessageSynced(string message, AudioClip clip, float fallbackWait)
    {
        if (bubbleMessageText == null) yield break;

        StopBubbleTyping();

        float audioDuration = GetClipDuration(clip);
        float charDelay = GetCharacterDelayForMessage(message, audioDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, () => typingFinished = true));

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        while (!typingFinished)
            yield return null;

        bubbleTypeRoutine = null;

        if (clip != null && voiceAudioSource != null && waitForFullAudioBeforeContinuing)
            yield return new WaitForSeconds(Mathf.Max(0f, clip.length - EstimatedTypingDuration(message, charDelay)));
        else if (clip == null)
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator TypeBubbleText(string message, float characterDelay, Action onComplete = null)
    {
        if (bubbleMessageText == null) yield break;

        if (!useTypewriterEffect)
        {
            bubbleMessageText.text = message;
            onComplete?.Invoke();
            yield break;
        }

        bubbleMessageText.text = string.Empty;

        if (string.IsNullOrEmpty(message))
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            bubbleMessageText.text += message[i];
            yield return new WaitForSeconds(characterDelay);
        }

        onComplete?.Invoke();
    }

    private void StopBubbleTyping()
    {
        if (bubbleTypeRoutine != null)
        {
            StopCoroutine(bubbleTypeRoutine);
            bubbleTypeRoutine = null;
        }
    }

    private float GetCharacterDelayForMessage(string message, float audioDuration)
    {
        if (!useTypewriterEffect) return 0f;

        int visibleLength = GetVisibleCharacterCount(message);

        if (visibleLength <= 0) return defaultTypewriterCharacterDelay;
        if (audioDuration <= 0f) return defaultTypewriterCharacterDelay;

        float syncedDelay = audioDuration / visibleLength;
        return Mathf.Clamp(syncedDelay, minSyncedCharacterDelay, maxSyncedCharacterDelay);
    }

    private int GetVisibleCharacterCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;

        int count = 0;
        foreach (char c in message)
        {
            if (!char.IsWhiteSpace(c)) count++;
        }

        return Mathf.Max(1, count);
    }

    private float EstimatedTypingDuration(string message, float characterDelay)
        => GetVisibleCharacterCount(message) * characterDelay;

    private float GetClipDuration(AudioClip clip)
        => clip != null ? clip.length : 0f;
}