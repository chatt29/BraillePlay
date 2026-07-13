using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InstrumentSounds2 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Question sub-class
    //
    // Each lesson now has exactly one question: "Which sound is the <name>?"
    // The prompt supports a "{0}" placeholder that is replaced with the
    // lesson's instrument name at runtime.
    // -------------------------------------------------------------------------

    [Serializable]
    public class MatchingQuestion
    {
        [Tooltip("Use {0} as a placeholder for the instrument name.")]
        [TextArea(2, 4)]
        public string promptMessage = "Which sound is the {0}? Press dot 1 for Sound A, dot 2 for Sound B, or dot 3 for Sound C.";
        public AudioClip promptAudio;

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

        [Header("Instrument Name (announced + used in the question)")]
        public string instrumentName = "Drum"; // e.g. "Drum"

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Introduction")]
        [TextArea(2, 4)]
        public string introductionMessage;
        public AudioClip introductionAudio;

        [Header("Matching Sounds")]
        [Tooltip("The sound that actually belongs to this instrument (the correct answer).")]
        public AudioClip correctSound;

        [Tooltip("A distractor sound from a different instrument.")]
        public AudioClip distractorSoundA;

        [Tooltip("Another distractor sound from a different instrument.")]
        public AudioClip distractorSoundB;

        [Header("Matching Question")]
        public MatchingQuestion matchingQuestion = new MatchingQuestion();
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
    public string highScoreKey = "InstrumentSoundsHighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource; // dedicated source for instrument sound effects
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
    public string letsLearnMessage = "Let's find the matching sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<InstrumentLesson> lessons = new List<InstrumentLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Matching Sound Playback")]
    [Tooltip("Pause inserted between each of the three sounds during initial presentation.")]
    public float pauseBetweenSounds = 1f;

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

    private enum QuestionStage { None, Matching }

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

    // Randomized sound order for the CURRENT presentation of the current lesson.
    // Index 0 = Sound A (dot 1), Index 1 = Sound B (dot 2), Index 2 = Sound C (dot 3).
    private AudioClip[] currentShuffledSounds = new AudioClip[3];
    private int correctSoundIndex = -1;

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
            Debug.Log("InstrumentSounds2 started.");

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
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel} / {lessons[currentLessonIndex].instrumentName}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence — "Find the Matching Sound"
    //
    // Exact order:
    //   1. Display Label + instrument name (announced)
    //   2. Introduction message/audio ("Listen carefully.")
    //   3. Randomize the order of [correct sound, distractor A, distractor B]
    //      and remember which position holds the correct answer.
    //   4. Play the three sounds once, in the randomized order, with a pause
    //      between each (Sound A / Sound B / Sound C = dot 1 / dot 2 / dot 3).
    //   5. Ask "Which sound is the <instrument>? ..." and wait for input.
    //   6. Validate: correct -> success, advance to next lesson.
    //                wrong   -> wrong message, re-ask the question WITHOUT
    //                           replaying the three sounds. After 3 misses,
    //                           play a support message first, then re-ask.
    //   The three sounds are only replayed if the lesson itself is restarted
    //   (see HandleRepeat), never as part of answering incorrectly.
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(InstrumentLesson lesson)
    {
        ResetAnswerState();
        currentMistakeCount = 0;
        waitingForChoiceAnswer = false;
        currentStage = QuestionStage.None;

        // Step 1: Display Label, instrument name, Display Image
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

        // Step 3: Randomize sound order for this presentation of the lesson.
        ShuffleLessonSounds(lesson);

        // Step 4: Play the three sounds, one at a time, with a pause between each.
        yield return PlayThreeSounds();
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 5 & 6: Ask the matching question and wait for Braille input.
        yield return AskMatchingQuestion(lesson);
    }

    /// <summary>Step 1 — Display Label, instrument name, Display Image.</summary>
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
            categoryText.text = lesson.instrumentName ?? string.Empty;
    }

    /// <summary>
    /// Step 3 — Build the randomized [Sound A, Sound B, Sound C] order for this
    /// lesson presentation and remember which position is correct.
    /// </summary>
    private void ShuffleLessonSounds(InstrumentLesson lesson)
    {
        List<AudioClip> pool = new List<AudioClip>
        {
            lesson.correctSound,
            lesson.distractorSoundA,
            lesson.distractorSoundB
        };

        // Fisher-Yates shuffle.
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        currentShuffledSounds = pool.ToArray();
        correctSoundIndex = Array.IndexOf(currentShuffledSounds, lesson.correctSound);

        if (logDebug)
            Debug.Log($"[Lesson {lesson.displayLabel}] Correct sound placed at position {correctSoundIndex} (dot {correctSoundIndex + 1}).");
    }

    /// <summary>Step 4 — Play the three shuffled sounds once, pausing between each.</summary>
    private IEnumerator PlayThreeSounds()
    {
        AudioSource source = sfxAudioSource != null ? sfxAudioSource : voiceAudioSource;
        if (source == null) yield break;

        for (int i = 0; i < currentShuffledSounds.Length; i++)
        {
            AudioClip clip = currentShuffledSounds[i];
            if (clip == null) continue;

            source.Stop();
            source.clip = clip;
            source.Play();
            yield return new WaitForSeconds(clip.length);

            bool isLastSound = i == currentShuffledSounds.Length - 1;
            if (!isLastSound)
                yield return new WaitForSeconds(pauseBetweenSounds);
        }
    }

    // -------------------------------------------------------------------------
    // Matching Question
    // -------------------------------------------------------------------------

    private IEnumerator AskMatchingQuestion(InstrumentLesson lesson)
    {
        currentStage = QuestionStage.Matching;
        waitingForChoiceAnswer = true;

        string prompt = BuildMatchingPrompt(lesson);

        // ShowBubbleMessageSynced is this project's existing bubble-text +
        // audio sequencing system (equivalent to ShowBubbleMessageWithAudioSequence);
        // reused here unchanged so success/wrong/support messages stay in sync
        // with their audio exactly as before.
        yield return ShowBubbleMessageSynced(prompt, lesson.matchingQuestion.promptAudio, noAudioTextDelay);
    }

    private string BuildMatchingPrompt(InstrumentLesson lesson)
    {
        string template = string.IsNullOrWhiteSpace(lesson.matchingQuestion.promptMessage)
            ? "Which sound is the {0}? Press dot 1 for Sound A, dot 2 for Sound B, or dot 3 for Sound C."
            : lesson.matchingQuestion.promptMessage;

        return template.Replace("{0}", lesson.instrumentName);
    }

    private void HandleMatchingAnswer(string pattern)
    {
        int? selected = MapDotToSoundIndex(pattern);
        if (selected == null) return; // Unrecognized pattern: keep waiting.

        InstrumentLesson lesson = lessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (selected.Value == correctSoundIndex)
        {
            currentMistakeCount = 0;
            lessonActive = false;

            SetAnswerState(true);
            RunFlow(HandleMatchingCorrect(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleMatchingSupport(lesson));
            else
                RunFlow(HandleMatchingWrong(lesson));
        }
    }

    private IEnumerator HandleMatchingCorrect(InstrumentLesson lesson)
    {
        SaveHighScoreIfNeeded();

        string message = !string.IsNullOrWhiteSpace(lesson.matchingQuestion.successMessage)
            ? lesson.matchingQuestion.successMessage
            : "Correct! Lesson complete.";

        AudioClip clip = lesson.matchingQuestion.successAudio != null
            ? lesson.matchingQuestion.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        // Lesson complete -> advance to the next instrument.
        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator HandleMatchingWrong(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.matchingQuestion.wrongMessage)
            ? lesson.matchingQuestion.wrongMessage
            : "Try again.";

        AudioClip clip = lesson.matchingQuestion.wrongAudio != null
            ? lesson.matchingQuestion.wrongAudio
            : genericTryAgainAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);

        // IMPORTANT: do NOT replay the three sounds — only re-ask the question.
        yield return AskMatchingQuestion(lesson);
    }

    private IEnumerator HandleMatchingSupport(InstrumentLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.matchingQuestion.supportMessage)
            ? lesson.matchingQuestion.supportMessage
            : "Here is some help. Listen carefully to the sounds again.";

        yield return ShowBubbleMessageSynced(message, lesson.matchingQuestion.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        // Still do not auto-replay the three sounds — only re-ask the question.
        yield return AskMatchingQuestion(lesson);
    }

    // -------------------------------------------------------------------------
    // Braille Dot -> Sound Position Mapping
    //
    // Dot 1 = "100000" -> Sound A (index 0)
    // Dot 2 = "010000" -> Sound B (index 1)
    // Dot 3 = "001000" -> Sound C (index 2)
    // -------------------------------------------------------------------------

    private int? MapDotToSoundIndex(string pattern)
    {
        if (pattern == "100000") return 0;
        if (pattern == "010000") return 1;
        if (pattern == "001000") return 2;
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
            case QuestionStage.Matching:
                HandleMatchingAnswer(submittedPattern);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeats ONLY the current lesson/instrument from the very beginning:
    /// Display Label, instrument name, introduction, a freshly re-randomized
    /// playback of the three sounds, and the question. This is the one case
    /// where the three sounds ARE replayed, since the lesson itself is being
    /// restarted. It never advances to the next lesson and never replays a
    /// previous one.
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
    // Bubble Text / Typewriter (existing audio sequencing system, unchanged)
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