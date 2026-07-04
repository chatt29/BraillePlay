using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AnimalSounds2 : MonoBehaviour
{
    public enum AnswerChoice { A, B, C }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;

        [TextArea(2, 4)]
        public string categoryLabel = "BRAILLE";

        [Header("Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Multiple Choice")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Tooltip("Animal sound played after the spoken letter for choice A.")]
        public AudioClip choiceAAudio;

        [Tooltip("Animal sound played after the spoken letter for choice B.")]
        public AudioClip choiceBAudio;

        [Tooltip("Animal sound played after the spoken letter for choice C.")]
        public AudioClip choiceCAudio;

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

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
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Answer Choice Letter Audio")]
    [Tooltip("Spoken 'A', played before choice A's animal sound.")]
    public AudioClip letterAAudio;
    [Tooltip("Spoken 'B', played before choice B's animal sound.")]
    public AudioClip letterBAudio;
    [Tooltip("Spoken 'C', played before choice C's animal sound.")]
    public AudioClip letterCAudio;

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

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForChoiceAnswer = false;

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
            Debug.Log("AnimalSounds2 started.");

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

    /// <summary>
    /// Stops whatever flow coroutine is currently running and starts a new one.
    /// Centralizing this avoids repeating the stop/start boilerplate everywhere.
    /// </summary>
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
            RunFlow(CompleteScene());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoiceAnswer = false;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    // Exact order:
    //   1. Display Label
    //   2. Category Label
    //   3. Prompt Message (+ audio)
    //   4. Display Image    (shown alongside the prompt)
    //   5. Answer Choices A / B / C (letter + animal sound each)
    //   6. Success Message  -> handled in HandleCorrectAnswer
    //   7. Wrong Message    -> handled in HandleWrongAnswer
    //   8. Support Message (+ audio) -> only after 3 consecutive mistakes
    //
    // This single coroutine is reused both when a lesson first starts and
    // whenever the player asks to repeat the current lesson, so there is one
    // source of truth for "what the beginning of a lesson looks like".
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        ResetAnswerState();

        // Steps 1, 2, 4: Display Label, Category Label, Display Image
        ApplyLessonDisplay(lesson);

        // Step 3: Prompt Message + audio
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 5: Multiple-choice answer options
        yield return AskMultipleChoiceQuestion(lesson);
    }

    /// <summary>Steps 1, 2, 4 — Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(BrailleLesson lesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = lesson.displayLabel;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel)
                ? "BRAILLE"
                : lesson.categoryLabel;

        // displayLabel is also used as a fallback for the prompt bubble text
        // (see ShowPromptMessage) if promptMessage is left empty.
    }

    /// <summary>Step 3 — Prompt Message together with its intro/instruction audio.</summary>
    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        string introMessage = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : lesson.displayLabel;

        yield return ShowBubbleMessageWithAudioSequence(
            introMessage,
            noAudioTextDelay,
            lesson.introAudio,
            lesson.instructionAudio
        );
    }

    /// <summary>
    /// Step 5 — marks the game as waiting for a Braille dot answer, then plays
    /// the three answer choices. Used for the first ask and every re-ask after
    /// a wrong answer or a support message, so the choice-playback logic only
    /// lives here.
    /// </summary>
    private IEnumerator AskMultipleChoiceQuestion(BrailleLesson lesson)
    {
        waitingForChoiceAnswer = true;
        yield return PlayAnswerChoices(lesson);
    }

    /// <summary>
    /// Plays choice A, then choice B, then choice C. Each choice is the spoken
    /// letter immediately followed by that choice's animal sound.
    /// </summary>
    private IEnumerator PlayAnswerChoices(BrailleLesson lesson)
    {
        yield return PlayLetterThenSound(letterAAudio, lesson.choiceAAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayLetterThenSound(letterBAudio, lesson.choiceBAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayLetterThenSound(letterCAudio, lesson.choiceCAudio);
    }

    /// <summary>Plays a spoken letter clip, then immediately plays the sound clip that follows it.</summary>
    private IEnumerator PlayLetterThenSound(AudioClip letterClip, AudioClip soundClip)
    {
        if (voiceAudioSource == null) yield break;

        if (letterClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = letterClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(letterClip.length);
        }

        if (soundClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = soundClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(soundClip.length);
        }
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForChoiceAnswer)
        {
            HandleMultipleChoiceAnswer(submittedPattern);
            return;
        }
    }

    private void HandleMultipleChoiceAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        // Dot 1 = Choice A   |   Dot 2 = Choice B   |   Dot 3 = Choice C
        AnswerChoice? selected = null;

        if (pattern == "100000") selected = AnswerChoice.A;
        else if (pattern == "010000") selected = AnswerChoice.B;
        else if (pattern == "001000") selected = AnswerChoice.C;

        if (selected == null) return; // Unrecognized pattern: keep waiting for a valid answer.

        BrailleLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected.Value == lesson.correctAnswer)
        {
            currentMistakeCount = 0;
            lessonActive = false;

            SetAnswerState(true);
            RunFlow(HandleCorrectAnswer(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(lesson));
            else
                RunFlow(HandleWrongAnswer(lesson));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    /// <summary>Step 6 — Success Message, then advance to the next lesson.</summary>
    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson)
    {
        SaveHighScoreIfNeeded();

        string message = !string.IsNullOrWhiteSpace(lesson.successMessage)
            ? lesson.successMessage
            : $"Correct! {lesson.displayLabel}.";

        AudioClip clip = lesson.successAudio != null
            ? lesson.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 7 — Wrong Message, then re-ask the same question (no full restart).</summary>
    private IEnumerator HandleWrongAnswer(BrailleLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.wrongMessage)
            ? lesson.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.wrongAudio != null
            ? lesson.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return AskMultipleChoiceQuestion(lesson);
    }

    /// <summary>
    /// Step 8 — after 3 consecutive mistakes, play the Support Message + audio
    /// to help the player, reset the mistake streak, then replay the three
    /// answer choices and re-ask the question.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.supportMessage)
            ? lesson.supportMessage
            : "Here is some help. Listen carefully to each choice.";

        yield return ShowBubbleMessageSynced(message, lesson.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskMultipleChoiceQuestion(lesson);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeats ONLY the current lesson/question from the very beginning:
    /// Display Label, Category Label, Prompt Message + audio, Display Image,
    /// and the three Answer Choices. It never advances to the next lesson and
    /// never replays a previous one.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next lesson is
        // in progress (lessonActive is false during that window). Without this
        // guard, a Repeat trigger fired here would stop the in-flight
        // HandleCorrectAnswer coroutine before it calls StartLesson(index + 1),
        // replaying the just-answered lesson instead of advancing.
        if (!lessonActive)
            return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        // Reset this lesson's state so it plays exactly like a fresh start.
        lessonActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;

        RunFlow(PlayLessonFromBeginning(lesson));
    }

    private void HandleNext()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeSceneCompletion());
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Scene Completion
    // -------------------------------------------------------------------------

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        yield return ShowBubbleMessageSynced(completedMessage, genericCompletedAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForRepeatChoice = true;
        yield return ShowBubbleMessageSynced(repeatQuestionMessage, repeatQuestionAudio, noAudioTextDelay);
    }

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

    private IEnumerator ShowBubbleMessageWithAudioSequence(string message, float fallbackWait, params AudioClip[] clips)
    {
        if (bubbleMessageText == null) yield break;

        StopBubbleTyping();

        float totalDuration = GetTotalClipDuration(clips);
        float charDelay = GetCharacterDelayForMessage(message, totalDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, () => typingFinished = true));

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }
        else
        {
            while (!typingFinished)
                yield return null;

            yield return new WaitForSeconds(fallbackWait);
        }

        while (!typingFinished)
            yield return null;

        bubbleTypeRoutine = null;
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

    private float GetTotalClipDuration(params AudioClip[] clips)
    {
        float total = 0f;
        if (clips == null) return total;
        foreach (AudioClip c in clips)
            if (c != null) total += c.length;
        return total;
    }
}