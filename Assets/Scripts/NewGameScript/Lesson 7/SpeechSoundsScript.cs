using System;
using System.Collections; // <--- THIS WAS MISSING
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpeechSoundsScript : MonoBehaviour
{
    [Serializable]
    public class SpellingWord
    {
        [Header("Word Info")]
        public string word = "jet";
        public Sprite displayImage;
        public AudioClip wordAudio;

        [Header("Prompt")]
        [TextArea(2, 4)]
        public string promptMessage = "Listen to the word, then spell it.";
        public AudioClip promptAudio;

        [Header("Success")]
        [TextArea(2, 4)]
        public string successMessage = "Correct! You spelled J-E-T, jet!";
        public AudioClip successAudio;

        [Header("Wrong Letter")]
        [TextArea(2, 4)]
        public string wrongLetterMessage = "That's not the right letter. Try again.";
        public AudioClip wrongLetterAudio;

        [Header("Support After 3 Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage = "J - E - T, Jet! The loud jet took off into the clouds.";
        public AudioClip supportAudio;

        [Header("Per-Letter Audio")]
        public AudioClip letter1Audio;
        public AudioClip letter2Audio;
        public AudioClip letter3Audio;
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

    [Header("Spelling Progress UI")]
    public TMP_Text spelledWordText;
    public TMP_Text currentLetterPromptText;

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
    public string highScoreKey = "SpeechSoundsHighScore";

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

    [Header("Lesson Audio")]
    public AudioClip lessonIntroAudio;
    [TextArea(2, 4)]
    public string lessonIntroMessage = "Words like BED, LEG, and RED have E in the middle. C-E-C pattern.";

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
    public string welcomeMessage = "Welcome to Speech Sounds!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "First, let's learn the pattern.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished all the words.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "Do you want to practice again? Press R to repeat or Y to finish.";

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------
    [Header("Lesson Flow")]
    public List<SpellingWord> words = new List<SpellingWord>();
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
    private int currentWordIndex = -1;
    private int currentLetterIndex = 0;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForSpelling = false;
    private bool inLessonPhase = true;

    private string currentTargetWord = "";
    private char[] currentSpelledLetters;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

    // Braille letter patterns
    private Dictionary<char, string> braillePatterns = new Dictionary<char, string>()
    {
        {'a', "100000"}, {'b', "110000"}, {'c', "100100"}, {'d', "100110"}, {'e', "100010"},
        {'f', "110100"}, {'g', "110110"}, {'h', "110010"}, {'i', "010100"}, {'j', "010110"},
        {'k', "101000"}, {'l', "111000"}, {'m', "101100"}, {'n', "101110"}, {'o', "101010"},
        {'p', "111100"}, {'q', "111110"}, {'r', "111010"}, {'s', "011100"}, {'t', "011110"},
        {'u', "101001"}, {'v', "111001"}, {'w', "010111"}, {'x', "101101"}, {'y', "101111"}, {'z', "101011"}
    };

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
            Debug.Log("SpeechSoundsScript started.");

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
        inLessonPhase = true;

        yield return ShowBubbleMessageSynced(welcomeMessage, welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(letsLearnMessage, letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(lessonIntroMessage, lessonIntroAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice * 2);

        inLessonPhase = false;
        StartWord(0);
    }

    private void StartWord(int index)
    {
        if (index < 0 || index >= words.Count)
        {
            RunFlow(CompleteScene());
            return;
        }

        currentWordIndex = index;
        currentLetterIndex = 0;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForSpelling = false;

        SpellingWord word = words[currentWordIndex];
        currentTargetWord = word.word.ToLower();
        currentSpelledLetters = new char[currentTargetWord.Length];
        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();

        if (logDebug)
            Debug.Log($"Starting word {currentWordIndex}: {currentTargetWord}");

        RunFlow(PlayWordPrompt(word));
    }

    // -------------------------------------------------------------------------
    // Word Sequence
    // -------------------------------------------------------------------------
    private IEnumerator PlayWordPrompt(SpellingWord word)
    {
        ResetAnswerState();

        if (displayLabelText != null)
            displayLabelText.text = "";

        if (displayImageUI != null)
        {
            displayImageUI.sprite = word.displayImage;
            displayImageUI.enabled = word.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = "SPELL";

        // 1. Play the actual word: "Jet"
        yield return ShowBubbleMessageSynced(word.word, word.wordAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. Play the prompt: "Spell the word you just heard."
        yield return ShowBubbleMessageSynced(word.promptMessage, word.promptAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForSpelling = true;
        UpdateLetterPromptUI();
    }

    private void UpdateSpelledWordUI()
    {
        if (spelledWordText != null)
            spelledWordText.text = new string(currentSpelledLetters).ToUpper().Replace('_', '_');
    }

    private void UpdateLetterPromptUI()
    {
        if (currentLetterPromptText == null) return;

        string[] ordinals = { "first", "second", "third" };
        if (currentLetterIndex < ordinals.Length)
            currentLetterPromptText.text = $"Spell the {ordinals[currentLetterIndex]} letter";
    }

    // -------------------------------------------------------------------------
    // Input Handling - Braille Spelling
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice || !waitingForSpelling)
            return;

        HandleSpellingInput(submittedPattern);
    }

    private void HandleSpellingInput(string pattern)
    {
        if (currentLetterIndex >= currentTargetWord.Length) return;

        char targetLetter = currentTargetWord[currentLetterIndex];
        string expectedPattern = braillePatterns.ContainsKey(targetLetter) ? braillePatterns[targetLetter] : "";

        if (pattern == expectedPattern)
        {
            currentSpelledLetters[currentLetterIndex] = targetLetter;
            currentLetterIndex++;
            currentMistakeCount = 0;

            UpdateSpelledWordUI();
            SetAnswerState(true);
            PlayLetterAudio(currentLetterIndex - 1);

            if (currentLetterIndex >= currentTargetWord.Length)
            {
                waitingForSpelling = false;
                lessonActive = false;
                RunFlow(HandleWordComplete(words[currentWordIndex]));
            }
            else
            {
                UpdateLetterPromptUI();
            }
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(words[currentWordIndex]));
            else
                RunFlow(HandleWrongLetter(words[currentWordIndex]));
        }
    }

    private void PlayLetterAudio(int letterIndex)
    {
        SpellingWord word = words[currentWordIndex];
        AudioClip clip = null;

        if (letterIndex == 0) clip = word.letter1Audio;
        else if (letterIndex == 1) clip = word.letter2Audio;
        else if (letterIndex == 2) clip = word.letter3Audio;

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------
    private IEnumerator HandleWordComplete(SpellingWord word)
    {
        SaveHighScoreIfNeeded();
        yield return ShowBubbleMessageSynced(word.successMessage, word.successAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);
        StartWord(currentWordIndex + 1);
    }

    private IEnumerator HandleWrongLetter(SpellingWord word)
    {
        yield return ShowBubbleMessageSynced(word.wrongLetterMessage, word.wrongLetterAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);
    }

    private IEnumerator HandleSupportThenRetry(SpellingWord word)
    {
        yield return ShowBubbleMessageSynced(word.supportMessage, word.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        currentLetterIndex = 0;
        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();
        UpdateLetterPromptUI();
        yield return new WaitForSeconds(delayAfterVoice);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            RunFlow(BeginSceneFlow());
            return;
        }

        if (!lessonActive || inLessonPhase) return;

        if (sceneFinished || currentWordIndex < 0 || currentWordIndex >= words.Count)
            return;

        SpellingWord word = words[currentWordIndex];
        lessonActive = true;
        waitingForSpelling = false;
        currentMistakeCount = 0;
        currentLetterIndex = 0;

        for (int i = 0; i < currentSpelledLetters.Length; i++)
            currentSpelledLetters[i] = '_';

        UpdateSpelledWordUI();
        RunFlow(PlayWordPrompt(word));
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

        if (displayImageUI != null) displayImageUI.enabled = false;
        if (displayLabelText != null) displayLabelText.text = string.Empty;
        if (spelledWordText != null) spelledWordText.text = string.Empty;
        if (currentLetterPromptText != null) currentLetterPromptText.text = string.Empty;
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

        if (displayImageUI != null) displayImageUI.enabled = false;
        if (displayLabelText != null) displayLabelText.text = string.Empty;
        if (spelledWordText != null) spelledWordText.text = string.Empty;
        if (currentLetterPromptText != null) currentLetterPromptText.text = string.Empty;
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