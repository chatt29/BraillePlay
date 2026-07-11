using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BrailleOnsetRimeTime : MonoBehaviour
{
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

    [Serializable]
    public class OnsetRimeQuizQuestion
    {
        [Header("Identity")]
        public string displayLabel; 
        public string categoryLabel = "QUIZ MODE";

        [Header("Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Display Image (For Educators/Sighted Support)")]
        public Sprite displayImage;

        [Header("Audio Layout")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip successAudio;

        [Header("Auditory Onset Identification")]
        [Tooltip("Audio clip of the full target word (e.g., 'Cat')")]
        public AudioClip targetWordAudio; 

        [TextArea(2, 4)]
        public string onsetAudioPromptMessage; 

        [Tooltip("Audio clip explicitly asking for the target onset sound (e.g., 'What is the starting sound you hear in cat? Write the letter that makes that sound.')")]
        public AudioClip onsetAudioPromptClip; 
        
        [Header("Answer Configuration")]
        [Tooltip("The 6-dot Braille pattern string required for the identified onset sound (e.g., '100100' for /k/ sound mapped to 'c')")]
        public string correctOnsetBraillePattern; 

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

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
    public string highScoreKey = "BrailleOnsetRimeHighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip letsQuizAudio;
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
    public string endOfLessonPromptMessage = "You have completed the lesson. Press Space to begin the Quiz, or press R to rewatch the lesson.";

    [TextArea(2, 5)]
    public string letsQuizInstructionMessage = "Great job learning! Now let's test your skills with a quiz.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the onset and rime quiz.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the quiz. Do you want to repeat it? Press R to restart or Space to finish.";

    public AudioClip endOfLessonAudio;

    // -------------------------------------------------------------------------
    // Content Setup
    // -------------------------------------------------------------------------

    [Header("Phase 1: Lesson Pages")]
    public List<LessonPage> lessonPages = new List<LessonPage>();

    [Header("Phase 2: Quiz Questions")]
    public List<OnsetRimeQuizQuestion> quizQuestions = new List<OnsetRimeQuizQuestion>();
    
    [Header("Flow Delays")]
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

    private int currentQuizIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool quizActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForQuestionInput = false;
    private bool waitingForLessonEndChoice = false;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

    // -------------------------------------------------------------------------
    // Unity Events & Braille Mapping Subscriptions
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
            Debug.Log("BrailleOnsetRimeTime initialized.");

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

        if (BrailleMapping.Instance != null)
        {
            if (isCorrect) BrailleMapping.Instance.PlayCorrectSfx();
            else BrailleMapping.Instance.PlayWrongSfx();
        }
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
        quizActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForLessonEndChoice = false;

        yield return PlayLessonPages();
    }

    private IEnumerator PlayLessonPages()
    {
        foreach (LessonPage page in lessonPages)
        {
            if (displayLabelText != null) displayLabelText.text = page.title;
            if (displayImageUI != null)
            {
                displayImageUI.sprite = page.lessonImage;
                displayImageUI.enabled = page.lessonImage != null;
            }
            if (categoryText != null) categoryText.text = "LESSON";

            yield return ShowBubbleMessageSynced(page.lessonText, page.lessonAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        waitingForLessonEndChoice = true;
        yield return ShowBubbleMessageSynced(endOfLessonPromptMessage, endOfLessonAudio, noAudioTextDelay);
    }

    private IEnumerator TransitionToQuizPhase()
    {
        waitingForLessonEndChoice = false;

        yield return ShowBubbleMessageSynced(letsQuizInstructionMessage, letsQuizAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        ResetQuizScore();
        StartQuizQuestion(0);
    }

    private void StartQuizQuestion(int index)
    {
        if (index < 0 || index >= quizQuestions.Count)
        {
            RunFlow(CompleteSceneQuiz());
            return;
        }

        currentQuizIndex = index;
        currentMistakeCount = 0;
        quizActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForQuestionInput = false;

        RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
    }

    private IEnumerator PlayQuizFromBeginning(OnsetRimeQuizQuestion question)
    {
        ResetAnswerState();
        ApplyQuizDisplay(question);

        string introMessage = !string.IsNullOrWhiteSpace(question.promptMessage) ? question.promptMessage : question.displayLabel;
        yield return ShowBubbleMessageWithAudioSequence(introMessage, noAudioTextDelay, question.introAudio, question.instructionAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskQuizQuestionInput(question);
    }

    private void ApplyQuizDisplay(OnsetRimeQuizQuestion question)
    {
        if (displayLabelText != null) displayLabelText.text = question.displayLabel;
        if (displayImageUI != null)
        {
            displayImageUI.sprite = question.displayImage;
            displayImageUI.enabled = question.displayImage != null;
        }
        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(question.categoryLabel) ? "QUIZ" : question.categoryLabel;
    }

    private IEnumerator AskQuizQuestionInput(OnsetRimeQuizQuestion question)
    {
        waitingForQuestionInput = true;

        if (question.targetWordAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = question.targetWordAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(question.targetWordAudio.length);
        }

        yield return new WaitForSeconds(delayAfterVoice);

        // Deliver the audio focus cue asking for the onset sound identification
        yield return ShowBubbleMessageSynced(question.onsetAudioPromptMessage, question.onsetAudioPromptClip, noAudioTextDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizActive || sceneFinished || waitingForRepeatChoice || !waitingForQuestionInput)
            return;

        OnsetRimeQuizQuestion question = quizQuestions[currentQuizIndex];
        waitingForQuestionInput = false;

        // Directly check if the chord matches the correct configuration for the target onset sound
        if (submittedPattern.Trim() == question.correctOnsetBraillePattern.Trim())
        {
            currentMistakeCount = 0;
            quizActive = false;
            SetAnswerState(true);
            RunFlow(HandleCorrectAnswer(question));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(question));
            else
                RunFlow(HandleWrongAnswer(question));
        }
    }

    private IEnumerator HandleCorrectAnswer(OnsetRimeQuizQuestion question)
    {
        SaveHighScoreIfNeeded();
        string message = !string.IsNullOrWhiteSpace(question.successMessage) ? question.successMessage : "Correct match!";
        AudioClip clip = question.successAudio != null ? question.successAudio : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartQuizQuestion(currentQuizIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(OnsetRimeQuizQuestion question)
    {
        string message = !string.IsNullOrWhiteSpace(question.wrongMessage) ? question.wrongMessage : "Try again.";
        yield return ShowBubbleMessageSynced(message, genericTryAgainAudio, noAudioTextDelay);
        yield return AskQuizQuestionInput(question);
    }

    private IEnumerator HandleSupportThenRetry(OnsetRimeQuizQuestion question)
    {
        string message = !string.IsNullOrWhiteSpace(question.supportMessage) ? question.supportMessage : "Let's chunk it together.";
        yield return ShowBubbleMessageSynced(message, question.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuizQuestionInput(question);
    }

    // -------------------------------------------------------------------------
    // Navigation Action Triggers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        if (waitingForLessonEndChoice)
        {
            waitingForLessonEndChoice = false;
            RunFlow(PlayLessonPages());
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartQuizQuestion(0);
            return;
        }

        if (quizActive && currentQuizIndex >= 0 && currentQuizIndex < quizQuestions.Count)
        {
            RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
        }
    }

    private void HandleNext()
    {
        if (waitingForLessonEndChoice)
        {
            RunFlow(TransitionToQuizPhase());
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeSceneCompletion());
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Termination Flow
    // -------------------------------------------------------------------------

    private IEnumerator CompleteSceneQuiz()
    {
        quizActive = false;
        waitingForRepeatChoice = false;
        SaveHighScoreIfNeeded();

        if (displayImageUI != null) displayImageUI.enabled = false;
        if (displayLabelText != null) displayLabelText.text = string.Empty;
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
        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        
        yield break;
    }

    // -------------------------------------------------------------------------
    // Final Score Calculation Audio Sequences
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
        if (numberAudios == null || numberAudios.Count == 0 || number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Bubble Text Renderers
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

        while (!typingFinished) yield return null;
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
            while (!typingFinished) yield return null;
            yield return new WaitForSeconds(fallbackWait);
        }

        while (!typingFinished) yield return null;
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

        if (visibleLength <= 0 || audioDuration <= 0f) return defaultTypewriterCharacterDelay;
        return Mathf.Clamp(audioDuration / visibleLength, minSyncedCharacterDelay, maxSyncedCharacterDelay);
    }

    private int GetVisibleCharacterCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;
        int count = 0;
        foreach (char c in message) if (!char.IsWhiteSpace(c)) count++;
        return Mathf.Max(1, count);
    }

    private float EstimatedTypingDuration(string message, float characterDelay) => GetVisibleCharacterCount(message) * characterDelay;
    private float GetClipDuration(AudioClip clip) => clip != null ? clip.length : 0f;
    private float GetTotalClipDuration(params AudioClip[] clips)
    {
        float total = 0f;
        if (clips == null) return total;
        foreach (AudioClip c in clips) if (c != null) total += c.length;
        return total;
    }
}