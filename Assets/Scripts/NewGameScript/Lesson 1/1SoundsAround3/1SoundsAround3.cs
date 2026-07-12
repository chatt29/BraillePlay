using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SoundsAround3 : MonoBehaviour
{
    [Serializable]
    public class LoudSoftLesson
    {
        [Header("Word")]
        public string displayLabel; // "Telephone", "Drum", etc
        public AudioClip wordAudio; // Audio that says "Telephone"

        [Header("Picture")]
        public Sprite displayImage; // Image for the lesson

        [Header("Answer")]
        public bool isLoud; // true = loud, false = soft

        [Header("Support After 3 Mistakes")]
        [TextArea(2, 3)]
        public string supportMessage = "This is a soft sound.";
        public AudioClip supportAudio; // Audio that says "This is a soft sound."
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text livePatternText;
    public Image displayImageUI;

    [Header("Choices Display")]
    public TMP_Text choicesText;

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
    public string highScoreKey = "SoundsAround3HighScore";

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Scene Audio")]
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Generic Audio")]
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;

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
    public string welcomeMessage = "Welcome to Loud and Soft Sounds!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Listen to the sound, then decide if it's loud or soft.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [TextArea(2, 5)]
    public string pressNextMessage = "Press Next to continue.";
    public AudioClip pressNextAudio;

    [TextArea(2, 5)]
    public string pickPromptMessage = "Press dot 1 for Loud, dot 2 for Soft.";
    public AudioClip pickPromptAudio;

    [TextArea(2, 5)]
    public string correctMessage = "Correct!";
    public AudioClip correctAudio;

    [TextArea(2, 5)]
    public string wrongMessage = "Try again.";
    public AudioClip wrongAudio;

    [TextArea(2, 5)]
    public string loudResultMessage = "This makes a loud sound.";
    public AudioClip loudResultAudio;

    [TextArea(2, 5)]
    public string softResultMessage = "This makes a soft sound.";
    public AudioClip softResultAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<LoudSoftLesson> lessons = new List<LoudSoftLesson>();

    [Header("Choice Settings")]
    public string loudBraillePattern = "100000"; // Dot 1
    public string softBraillePattern = "010000"; // Dot 2
    public KeyCode loudKey = KeyCode.Alpha1;
    public KeyCode softKey = KeyCode.Alpha2;

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
    private bool waitingForChoice = false;
    private bool waitingForNextLesson = false;

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

        if (waitingForChoice && lessonActive)
        {
            if (Input.GetKeyDown(loudKey))
            {
                if (logDebug) Debug.Log("Keyboard: Loud pressed");
                HandleBrailleChordSubmitted(loudBraillePattern);
            }
            if (Input.GetKeyDown(softKey))
            {
                if (logDebug) Debug.Log("Keyboard: Soft pressed");
                HandleBrailleChordSubmitted(softBraillePattern);
            }
        }
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("SoundsAround3 started.");

        ResetQuizScore();
        SetupChoicesText();

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(BeginSceneFlow());
    }

    private void SetupChoicesText()
    {
        if (choicesText == null) return;
        choicesText.text = "1. Loud 2. Soft";
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

    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForNextLesson = false;

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
            if (flowRoutine != null)
                StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(CompleteScene());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForChoice = false;
        waitingForNextLesson = false;

        ResetAnswerState();

        LoudSoftLesson lesson = lessons[currentLessonIndex];

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lesson.displayLabel} - {(lesson.isLoud ? "Loud" : "Soft")}");

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(StartLessonSequence(lesson));
    }

    private IEnumerator StartLessonSequence(LoudSoftLesson lesson)
    {
        // Step 1: Show text + say the word only
        yield return ShowBubbleMessageSynced(lesson.displayLabel, lesson.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 2: Ask for answer
        waitingForChoice = true;
        yield return ShowBubbleMessageSynced(pickPromptMessage, pickPromptAudio, noAudioTextDelay);
    }

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice || waitingForNextLesson || !waitingForChoice)
            return;

        LoudSoftLesson lesson = lessons[currentLessonIndex];

        bool isCorrectAnswer = false;
        if (lesson.isLoud && submittedPattern == loudBraillePattern)
            isCorrectAnswer = true;
        else if (!lesson.isLoud && submittedPattern == softBraillePattern)
            isCorrectAnswer = true;

        if (isCorrectAnswer)
        {
            waitingForChoice = false;
            currentMistakeCount = 0;
            lessonActive = false;
            SetAnswerState(true);
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(HandleCorrectAnswer(lesson));
        }
        else
        {
            bool validChoice = submittedPattern == loudBraillePattern || submittedPattern == softBraillePattern;
            if (!validChoice) return;

            currentMistakeCount++;
            AddMistake();

            if (flowRoutine != null) StopCoroutine(flowRoutine);

            if (currentMistakeCount >= mistakesBeforeSupport)
                flowRoutine = StartCoroutine(HandleSupportThenRetry(lesson));
            else
                flowRoutine = StartCoroutine(HandleWrongAnswer(lesson));
        }
    }

    private IEnumerator HandleCorrectAnswer(LoudSoftLesson lesson)
    {
        SaveHighScoreIfNeeded();
        SetAnswerState(true);

        // Step 1: Say "Correct!"
        yield return ShowBubbleMessageSynced(correctMessage, correctAudio != null ? correctAudio : genericCorrectAudio, noAudioTextDelay);
        yield return new WaitForSeconds(0.2f);

        // Step 2: Say if it's loud or soft
        string resultMsg = lesson.isLoud ? loudResultMessage : softResultMessage;
        AudioClip resultAudio = lesson.isLoud ? loudResultAudio : softResultAudio;
        yield return ShowBubbleMessageSynced(resultMsg, resultAudio, noAudioTextDelay);

        yield return new WaitForSeconds(delayAfterCorrect);

        // Wait for Next button
        waitingForNextLesson = true;
        yield return ShowBubbleMessageSynced(pressNextMessage, pressNextAudio, noAudioTextDelay);
    }

    private IEnumerator HandleWrongAnswer(LoudSoftLesson lesson)
    {
        yield return ShowBubbleMessageSynced(wrongMessage, wrongAudio != null ? wrongAudio : genericTryAgainAudio, noAudioTextDelay);
        waitingForChoice = true;
    }

    private IEnumerator HandleSupportThenRetry(LoudSoftLesson lesson)
    {
        SetAnswerState(false);

        // Use the per-lesson support message/audio
        string message = !string.IsNullOrWhiteSpace(lesson.supportMessage) ? lesson.supportMessage : (lesson.isLoud ? loudResultMessage : softResultMessage);
        AudioClip audio = lesson.supportAudio != null ? lesson.supportAudio : (lesson.isLoud ? loudResultAudio : softResultAudio);
        yield return ShowBubbleMessageSynced(message, audio, noAudioTextDelay);

        yield return new WaitForSeconds(0.3f);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        // Replay the word
        yield return ShowBubbleMessageSynced(lesson.displayLabel, lesson.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForChoice = true;
    }

    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(RestartCurrentLesson(lessons[currentLessonIndex]));
    }

    private IEnumerator RestartCurrentLesson(LoudSoftLesson lesson)
    {
        lessonActive = true;
        waitingForChoice = false;
        currentMistakeCount = 0;
        waitingForNextLesson = false;
        ResetAnswerState();
        yield return StartLessonSequence(lesson);
    }

    private void HandleNext()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            flowRoutine = StartCoroutine(FinalizeSceneCompletion());
        }
        else if (waitingForNextLesson)
        {
            waitingForNextLesson = false;
            if (flowRoutine != null) StopCoroutine(flowRoutine);
            StartLesson(currentLessonIndex + 1);
        }
    }

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForNextLesson = false;
        SaveHighScoreIfNeeded();
        if (displayImageUI != null) displayImageUI.enabled = false;
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
        waitingForNextLesson = false;
        SaveHighScoreIfNeeded();
        if (displayImageUI != null) displayImageUI.enabled = false;
        ResetAnswerState();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";
        yield return ShowBubbleMessageSynced(finalMessage, genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[SoundsAround3] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

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

        while (!typingFinished) yield return null;
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