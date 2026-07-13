using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BrailleSoundsAround2 : MonoBehaviour
{
    [Serializable]
    public class PictureSoundLesson
    {
        [Header("Identity")]
        public string displayLabel; // "Cat", "Bird", etc

        [Header("Picture + Word Audio")]
        public Sprite displayImage;
        public AudioClip wordAudio; // Audio that says "Cat"

        [Header("Messages")]
        [TextArea(2, 4)]
        public string introMessage = "What sound does this make?";
        public AudioClip introAudio;

        [TextArea(2, 4)]
        public string successMessage = "Correct! A cat says";
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage = "Try again.";
        public AudioClip wrongAudio;

        [Header("Correct Sound Effect")]
        public AudioClip correctSoundEffect; // The actual "meeoow-meeoow" clip
        public string correctSoundBraillePattern = "100000";

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage = "Remember, a cat says";
        public AudioClip supportAudio;
    }

    [Serializable]
    public class SoundChoice
    {
        [Header("Choice Setup")]
        public string choiceName; // "tweeet-tweeet"
        public AudioClip choiceAudio; // Audio clip of the sound
        public string braillePattern = "100000"; // Braille device: Dot 1, Dot 2, etc
        public KeyCode keyboardKey = KeyCode.Alpha1; // Keyboard testing: 1-5 keys
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

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
    public string highScoreKey = "BrailleSoundsAround2HighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource;
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
    // Scene Text - All editable with audio
    // -------------------------------------------------------------------------

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Picture Sounds!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Listen to the word, then pick what sound it makes.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [TextArea(2, 5)]
    public string pressNextMessage = "Press Next to continue.";
    public AudioClip pressNextAudio;

    [TextArea(2, 5)]
    public string listenPromptMessage = "Listen to the word, then pick what sound it makes.";
    public AudioClip listenPromptAudio;

    [TextArea(2, 5)]
    public string pickPromptMessage = "Pick the sound by pressing the braille dots.";
    public AudioClip pickPromptAudio;

    // -------------------------------------------------------------------------
    // Instruction Audio Parts - Record 5 separate clips
    // -------------------------------------------------------------------------

    [Header("Instruction Audio Parts")]
    public AudioClip pressDot1IfTheAudio; // "Press dot 1 if the"
    public AudioClip pressDot2IfTheAudio; // "Press dot 2 if the"
    public AudioClip pressDot3IfTheAudio; // "Press dot 3 if the"
    public AudioClip pressDot4IfTheAudio; // "Press dot 4 if the"
    public AudioClip pressDot5IfTheAudio; // "Press dot 5 if the"
    public AudioClip goesAudio; // "goes"

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<PictureSoundLesson> lessons = new List<PictureSoundLesson>();

    [Header("Sound Choices - Same for all lessons")]
    public List<SoundChoice> soundChoices = new List<SoundChoice>();

    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    public float delayBetweenChoiceAudio = 0.3f;

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

        // Keyboard testing - works alongside braille device
        if (waitingForChoice && lessonActive)
        {
            for (int i = 0; i < soundChoices.Count; i++)
            {
                if (Input.GetKeyDown(soundChoices[i].keyboardKey))
                {
                    if (logDebug) Debug.Log($"Keyboard key pressed for choice {i + 1}");
                    HandleBrailleChordSubmitted(soundChoices[i].braillePattern);
                }
            }
        }
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("BrailleSoundsAround2 started.");

        ResetQuizScore();
        SetupChoicesText();

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(BeginSceneFlow());
    }

    private void SetupChoicesText()
    {
        if (choicesText == null) return;

        string display = "";
        for (int i = 0; i < soundChoices.Count; i++)
        {
            display += $"{i + 1}. {soundChoices[i].choiceName} ";
        }
        choicesText.text = display;
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

        PictureSoundLesson lesson = lessons[currentLessonIndex];

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lesson.displayLabel}");

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(StartLessonSequence(lesson));
    }

    private IEnumerator StartLessonSequence(PictureSoundLesson lesson)
    {
        // Step 1: Show picture + say the word
        yield return ShowBubbleMessageSynced(lesson.displayLabel, lesson.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 2: Say "What sound does this make?"
        yield return ShowBubbleMessageSynced(lesson.introMessage, lesson.introAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Instructions before choices - now editable
        yield return ShowBubbleMessageSynced(listenPromptMessage, listenPromptAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Play all choices with dot instructions
        yield return PlayAllChoiceSounds(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForChoice = true;
        yield return ShowBubbleMessageSynced(pickPromptMessage, pickPromptAudio, noAudioTextDelay);
    }

    private IEnumerator PlayAllChoiceSounds(PictureSoundLesson lesson)
    {
        if (voiceAudioSource == null)
        {
            Debug.LogError("voiceAudioSource is NULL! Assign it in Inspector.");
            yield break;
        }
        if (sfxAudioSource == null)
        {
            Debug.LogError("sfxAudioSource is NULL! Assign it in Inspector.");
            yield break;
        }

        AudioClip[] pressDotClips = new AudioClip[]
        {
        pressDot1IfTheAudio,
        pressDot2IfTheAudio,
        pressDot3IfTheAudio,
        pressDot4IfTheAudio,
        pressDot5IfTheAudio
        };

        for (int i = 0; i < soundChoices.Count && i < pressDotClips.Length; i++)
        {
            Debug.Log($"--- Playing choice {i + 1}: {soundChoices[i].choiceName} ---");

            string instructionText = $"Press dot {i + 1} if the {lesson.displayLabel.ToLower()} goes {soundChoices[i].choiceName}";
            if (bubbleMessageText != null)
                bubbleMessageText.text = instructionText;

            // Step 1: Press dot X if the
            if (pressDotClips[i] != null)
            {
                Debug.Log($"Playing: {pressDotClips[i].name}");
                voiceAudioSource.Stop();
                voiceAudioSource.clip = pressDotClips[i];
                voiceAudioSource.Play();
                yield return new WaitForSeconds(pressDotClips[i].length);
            }
            else
            {
                Debug.LogWarning($"pressDot{i + 1}IfTheAudio is NULL!");
            }

            // Step 2: Animal word
            if (lesson.wordAudio != null)
            {
                Debug.Log($"Playing word: {lesson.wordAudio.name}");
                voiceAudioSource.Stop();
                voiceAudioSource.clip = lesson.wordAudio;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(lesson.wordAudio.length + 0.1f);
            }
            else
            {
                Debug.LogWarning("lesson.wordAudio is NULL!");
            }

            // Step 3: "goes"
            if (goesAudio != null)
            {
                Debug.Log($"Playing: {goesAudio.name}");
                voiceAudioSource.Stop();
                voiceAudioSource.clip = goesAudio;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(goesAudio.length);
            }
            else
            {
                Debug.LogWarning("goesAudio is NULL!");
            }

            // Step 4: The actual sound - THIS IS WHERE IT'S FAILING
            if (soundChoices[i].choiceAudio != null)
            {
                Debug.Log($"Playing choiceAudio: {soundChoices[i].choiceAudio.name} on sfxAudioSource");
                Debug.Log($"sfxAudioSource enabled: {sfxAudioSource.enabled}, volume: {sfxAudioSource.volume}");

                sfxAudioSource.Stop();
                sfxAudioSource.clip = soundChoices[i].choiceAudio;
                sfxAudioSource.Play();

                Debug.Log($"sfxAudioSource.isPlaying: {sfxAudioSource.isPlaying}");
                yield return new WaitForSeconds(soundChoices[i].choiceAudio.length + delayBetweenChoiceAudio);
            }
            else
            {
                Debug.LogError($"soundChoices[{i}].choiceAudio is NULL! Check Inspector.");
            }
        }
    }

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice || waitingForNextLesson || !waitingForChoice)
            return;

        PictureSoundLesson lesson = lessons[currentLessonIndex];

        if (submittedPattern == lesson.correctSoundBraillePattern)
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
            bool validChoice = false;
            foreach (var choice in soundChoices)
            {
                if (choice.braillePattern == submittedPattern)
                {
                    validChoice = true;
                    break;
                }
            }

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

    private IEnumerator HandleCorrectAnswer(PictureSoundLesson lesson)
    {
        SaveHighScoreIfNeeded();
        SetAnswerState(true);

        // Step 1: Say "Correct! A [animal] says" - now uses lesson.successMessage
        string message = lesson.successMessage;

        // Use successAudio if you have it recorded, otherwise use genericCorrectAudio
        AudioClip introClip = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, introClip, noAudioTextDelay);
        yield return new WaitForSeconds(0.2f); // Small pause before sound effect

        // Step 2: Play the actual sound effect - meeoow, tweeet, etc
        if (lesson.correctSoundEffect != null && sfxAudioSource != null)
        {
            // Show just the sound name in text while audio plays
            if (bubbleMessageText != null)
            {
                string soundName = GetSoundNameFromLesson(lesson);
                bubbleMessageText.text = $"{message} {soundName}";
            }

            sfxAudioSource.Stop();
            sfxAudioSource.clip = lesson.correctSoundEffect;
            sfxAudioSource.Play();
            yield return new WaitForSeconds(lesson.correctSoundEffect.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        // CHANGED: Instead of auto-advancing, wait for Next button
        waitingForNextLesson = true;
        yield return ShowBubbleMessageSynced(pressNextMessage, pressNextAudio, noAudioTextDelay);
    }

    private string GetSoundNameFromLesson(PictureSoundLesson lesson)
    {
        // Match the sound effect to a display name
        foreach (var choice in soundChoices)
        {
            if (choice.braillePattern == lesson.correctSoundBraillePattern)
                return choice.choiceName;
        }
        return "";
    }

    private IEnumerator HandleWrongAnswer(PictureSoundLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.wrongMessage) ? lesson.wrongMessage : "Try again.";
        yield return ShowBubbleMessageSynced(message, lesson.wrongAudio != null ? lesson.wrongAudio : genericTryAgainAudio, noAudioTextDelay);
        yield return PlayAllChoiceSounds(lesson);
        waitingForChoice = true;
    }

    private IEnumerator HandleSupportThenRetry(PictureSoundLesson lesson)
    {
        SetAnswerState(false);

        // Step 1: Say "Remember, a [animal] says" - now uses lesson.supportMessage
        string message = lesson.supportMessage;

        // Use supportAudio if you have it, otherwise no audio for intro
        yield return ShowBubbleMessageSynced(message, lesson.supportAudio, noAudioTextDelay);
        yield return new WaitForSeconds(0.2f); // Small pause

        // Step 2: Play the actual sound effect - meeoow, tweeet, etc
        if (lesson.correctSoundEffect != null && sfxAudioSource != null)
        {
            // Show the sound name in text while audio plays
            if (bubbleMessageText != null)
            {
                string soundName = GetSoundNameFromLesson(lesson);
                bubbleMessageText.text = $"{message} {soundName}";
            }

            sfxAudioSource.Stop();
            sfxAudioSource.clip = lesson.correctSoundEffect;
            sfxAudioSource.Play();
            yield return new WaitForSeconds(lesson.correctSoundEffect.length);
        }

        yield return new WaitForSeconds(0.3f);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        // Replay the word + choices
        yield return ShowBubbleMessageSynced(lesson.displayLabel, lesson.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);
        yield return PlayAllChoiceSounds(lesson);
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

    private IEnumerator RestartCurrentLesson(PictureSoundLesson lesson)
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
        // CHANGED: Handle both end-of-scene and next lesson
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
            Debug.LogWarning("[BrailleSoundsAround2] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
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