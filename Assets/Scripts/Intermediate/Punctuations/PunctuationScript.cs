using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PunctuationScript : MonoBehaviour
{
    public enum LessonKind
    {
        SymbolOnly,
        Sequence
    }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;
        public string categoryLabel = "BRAILLE";

        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string repeatMessage;

        [TextArea(2, 4)]
        public string successMessage;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Description")]
        [TextArea(2, 4)]
        public string descriptionMessage;

        public AudioClip descriptionAudio;

        [Header("Lesson Type")]
        public LessonKind lessonKind = LessonKind.SymbolOnly;

        [Tooltip("Used for SymbolOnly lessons")]
        public int[] dots;

        [Tooltip("Used for Sequence lessons. Example: 001111 then 100000 then 000000 for space")]
        public List<string> expectedSequencePatterns = new List<string>();

        [Tooltip("Friendly names for each sequence step. Example: # then 1 then space")]
        public List<string> expectedSequenceNames = new List<string>();

        [Header("Visual")]
        public Sprite lessonSprite;

        [Header("General Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip successAudio;

        [Header("Sequence Step Audio")]
        public List<AudioClip> sequenceStepAudios = new List<AudioClip>();

        [Header("Sequence Step Messages")]
        [TextArea(2, 4)]
        public List<string> sequenceStepMessages = new List<string>();

        [Header("Sequence Step Wrong Audio")]
        public List<AudioClip> sequenceStepWrongAudios = new List<AudioClip>();

        [Header("Sequence Step Wrong Messages")]
        [TextArea(2, 4)]
        public List<string> sequenceStepWrongMessages = new List<string>();

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text translationText;
    public TMP_Text pressText;
    public TMP_Text categoryText;
    public TMP_Text livePatternText;
    public Image lessonImage;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Play!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's learn punctuation.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished this punctuation lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished this punctuation lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public bool playInstructionOnLessonStart = true;
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    public bool showHeldDotsPattern = true;
    public bool resetSequenceToStartOnWrongAnswer = true;
    public bool allowSpaceAsSequenceInput = true;
    public string spaceSequencePattern = "000000";

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

    private int currentLessonIndex = -1;
    private int currentSequenceStep = 0;
    private int currentMistakeCount = 0;
    private bool lessonActive;
    private bool waitingForNext;
    private bool sceneFinished;
    private bool waitingForRepeatChoice;
    private bool waitingForDescriptionChoice;
    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;
    private BrailleLesson currentLessonForDescription;

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnSpace += HandleSpaceInput;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnSpace -= HandleSpaceInput;
    }

    private void Update()
    {
        if (!showHeldDotsPattern || livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("PunctuationScript started");

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(BeginSceneFlow());
    }

    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        waitingForNext = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForDescriptionChoice = false;
        currentLessonForDescription = null;

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
        currentSequenceStep = 0;
        currentMistakeCount = 0;
        lessonActive = true;
        waitingForNext = false;
        waitingForRepeatChoice = false;
        waitingForDescriptionChoice = false;
        sceneFinished = false;
        currentLessonForDescription = null;

        BrailleLesson lesson = lessons[currentLessonIndex];

        if (translationText != null)
            translationText.text = lesson.displayLabel;

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel) ? "PUNCTUATION" : lesson.categoryLabel;

        if (pressText != null)
            pressText.text = "PRESS!";

        if (lessonImage != null)
        {
            lessonImage.sprite = lesson.lessonSprite;
            lessonImage.enabled = lesson.lessonSprite != null;
        }

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lesson.displayLabel}");

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(StartLessonSequence(lesson));
    }

    private IEnumerator StartLessonSequence(BrailleLesson lesson)
    {
        if (lesson.lessonKind == LessonKind.SymbolOnly)
        {
            string message = GetLessonPrompt(lesson);

            if (playInstructionOnLessonStart)
            {
                yield return ShowBubbleMessageWithAudioSequence(
                    message,
                    noAudioTextDelay,
                    lesson.introAudio,
                    lesson.instructionAudio
                );
            }
            else
            {
                yield return ShowBubbleMessageSynced(message, null, noAudioTextDelay);
            }
        }
        else
        {
            yield return PlaySequenceStepInstruction(lesson, 0);
        }
    }

    private string GetLessonPrompt(BrailleLesson lesson)
    {
        if (!string.IsNullOrWhiteSpace(lesson.promptMessage))
            return lesson.promptMessage;

        if (lesson.lessonKind == LessonKind.SymbolOnly)
            return $"{lesson.displayLabel}. Press {GetDotsDisplay(lesson.dots)}.";

        if (lesson.expectedSequenceNames != null && lesson.expectedSequenceNames.Count > 0)
            return $"{lesson.displayLabel}. Step 1: press {lesson.expectedSequenceNames[0]}.";

        return $"{lesson.displayLabel}. Enter the braille sequence.";
    }

    private IEnumerator PlaySequenceStepInstruction(BrailleLesson lesson, int stepIndex)
    {
        string stepMessage = GetSequenceStepMessage(lesson, stepIndex);
        AudioClip clip = GetSequenceStepAudio(lesson, stepIndex);

        yield return ShowBubbleMessageSynced(stepMessage, clip, noAudioTextDelay);
    }

    private string GetSequenceStepMessage(BrailleLesson lesson, int stepIndex)
    {
        if (lesson.sequenceStepMessages != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.sequenceStepMessages.Count &&
            !string.IsNullOrWhiteSpace(lesson.sequenceStepMessages[stepIndex]))
        {
            return lesson.sequenceStepMessages[stepIndex];
        }

        string stepName = GetSequenceStepName(lesson, stepIndex);
        return $"Press {stepName}.";
    }

    private AudioClip GetSequenceStepAudio(BrailleLesson lesson, int stepIndex)
    {
        if (lesson.sequenceStepAudios != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.sequenceStepAudios.Count)
        {
            return lesson.sequenceStepAudios[stepIndex];
        }

        return null;
    }

    private string GetSequenceStepWrongMessage(BrailleLesson lesson, int stepIndex)
    {
        if (lesson.sequenceStepWrongMessages != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.sequenceStepWrongMessages.Count &&
            !string.IsNullOrWhiteSpace(lesson.sequenceStepWrongMessages[stepIndex]))
        {
            return lesson.sequenceStepWrongMessages[stepIndex];
        }

        string stepName = GetSequenceStepName(lesson, stepIndex);

        if (resetSequenceToStartOnWrongAnswer)
            return $"That was not correct. Start again from {GetSequenceStepName(lesson, 0)}.";

        return $"That was not correct. Try {stepName} again.";
    }

    private AudioClip GetSequenceStepWrongAudio(BrailleLesson lesson, int stepIndex)
    {
        if (lesson.sequenceStepWrongAudios != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.sequenceStepWrongAudios.Count)
        {
            return lesson.sequenceStepWrongAudios[stepIndex];
        }

        return genericTryAgainAudio;
    }

    private string GetSequenceStepName(BrailleLesson lesson, int stepIndex)
    {
        if (lesson.expectedSequenceNames != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.expectedSequenceNames.Count &&
            !string.IsNullOrWhiteSpace(lesson.expectedSequenceNames[stepIndex]))
        {
            return lesson.expectedSequenceNames[stepIndex];
        }

        if (lesson.expectedSequencePatterns != null &&
            stepIndex >= 0 &&
            stepIndex < lesson.expectedSequencePatterns.Count &&
            lesson.expectedSequencePatterns[stepIndex] == spaceSequencePattern)
        {
            return "space";
        }

        return $"step {stepIndex + 1}";
    }

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        HandleSubmittedPattern(submittedPattern);
    }

    private void HandleSpaceInput()
    {
        if (!allowSpaceAsSequenceInput)
            return;

        if (logDebug)
            Debug.Log("Space submitted as sequence pattern: " + spaceSequencePattern);

        HandleSubmittedPattern(spaceSequencePattern);
    }

    private void HandleSubmittedPattern(string submittedPattern)
    {
        if (!lessonActive || waitingForNext || sceneFinished || waitingForRepeatChoice || waitingForDescriptionChoice)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        if (lesson.lessonKind == LessonKind.SymbolOnly)
        {
            if (submittedPattern == spaceSequencePattern && allowSpaceAsSequenceInput)
            {
                if (logDebug)
                    Debug.Log("Ignoring space input for SymbolOnly lesson");
                return;
            }

            HandleSinglePatternLesson(lesson, submittedPattern);
        }
        else
        {
            HandleSequenceLesson(lesson, submittedPattern);
        }
    }

    private void HandleSinglePatternLesson(BrailleLesson lesson, string submittedPattern)
    {
        string expectedPattern = PatternFromDots(lesson.dots);

        if (logDebug)
            Debug.Log($"Submitted: {submittedPattern} | Expected: {expectedPattern}");

        if (submittedPattern == expectedPattern)
        {
            currentMistakeCount = 0;
            lessonActive = false;
            waitingForNext = false;
            waitingForDescriptionChoice = false;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(HandleCorrectAnswer(lesson));
        }
        else
        {
            currentMistakeCount++;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            if (currentMistakeCount >= mistakesBeforeSupport)
                flowRoutine = StartCoroutine(HandleSupportThenRetry(lesson));
            else
                flowRoutine = StartCoroutine(HandleWrongAnswer(lesson));
        }
    }

    private void HandleSequenceLesson(BrailleLesson lesson, string submittedPattern)
    {
        if (lesson.expectedSequencePatterns == null || lesson.expectedSequencePatterns.Count == 0)
        {
            Debug.LogWarning($"Lesson '{lesson.displayLabel}' has no sequence patterns.");
            return;
        }

        if (currentSequenceStep < 0 || currentSequenceStep >= lesson.expectedSequencePatterns.Count)
        {
            Debug.LogWarning($"Lesson '{lesson.displayLabel}' has invalid currentSequenceStep: {currentSequenceStep}");
            currentSequenceStep = 0;
        }

        string expected = lesson.expectedSequencePatterns[currentSequenceStep];

        if (logDebug)
            Debug.Log($"Sequence step {currentSequenceStep}: submitted {submittedPattern} | expected {expected}");

        if (submittedPattern == expected)
        {
            currentMistakeCount = 0;
            currentSequenceStep++;

            if (currentSequenceStep >= lesson.expectedSequencePatterns.Count)
            {
                lessonActive = false;
                waitingForNext = false;
                waitingForDescriptionChoice = false;

                if (flowRoutine != null)
                    StopCoroutine(flowRoutine);

                flowRoutine = StartCoroutine(HandleCorrectAnswer(lesson));
            }
            else
            {
                if (flowRoutine != null)
                    StopCoroutine(flowRoutine);

                flowRoutine = StartCoroutine(PlaySequenceStepInstruction(lesson, currentSequenceStep));
            }
        }
        else
        {
            int failedStepIndex = currentSequenceStep;
            currentMistakeCount++;

            if (resetSequenceToStartOnWrongAnswer)
                currentSequenceStep = 0;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            if (currentMistakeCount >= mistakesBeforeSupport)
                flowRoutine = StartCoroutine(HandleSupportThenRetry(lesson));
            else
                flowRoutine = StartCoroutine(HandleSequenceWrongAnswer(lesson, failedStepIndex));
        }
    }

    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson)
    {
        currentLessonForDescription = lesson;

        string message = !string.IsNullOrWhiteSpace(lesson.successMessage)
            ? lesson.successMessage
            : $"Correct! {lesson.displayLabel}.";

        AudioClip clipToUse = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clipToUse, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        yield return StartCoroutine(ShowLessonDescription(lesson));
    }

    private IEnumerator ShowLessonDescription(BrailleLesson lesson)
    {
        waitingForNext = false;
        waitingForDescriptionChoice = true;

        string description = !string.IsNullOrWhiteSpace(lesson.descriptionMessage)
            ? lesson.descriptionMessage
            : $"{lesson.displayLabel} is used in punctuation.";

        if (pressText != null)
            pressText.text = "R = REPEAT | Y = NEXT";

        yield return ShowBubbleMessageSynced(description, lesson.descriptionAudio, noAudioTextDelay);
    }

    private IEnumerator HandleWrongAnswer(BrailleLesson lesson)
    {
        string message;

        if (!string.IsNullOrWhiteSpace(lesson.wrongMessage))
        {
            message = lesson.wrongMessage;
        }
        else if (lesson.lessonKind == LessonKind.Sequence)
        {
            if (resetSequenceToStartOnWrongAnswer)
                message = $"Try again. Start again from {GetSequenceStepName(lesson, 0)}.";
            else
                message = $"Try again. Press {GetSequenceStepName(lesson, currentSequenceStep)}.";
        }
        else
        {
            message = $"Try again. {lesson.displayLabel}.";
        }

        yield return ShowBubbleMessageSynced(message, genericTryAgainAudio, noAudioTextDelay);

        if (lesson.lessonKind == LessonKind.Sequence)
        {
            int stepToReplay = resetSequenceToStartOnWrongAnswer ? 0 : currentSequenceStep;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(PlaySequenceStepInstruction(lesson, stepToReplay));
        }
    }

    private IEnumerator HandleSequenceWrongAnswer(BrailleLesson lesson, int failedStepIndex)
    {
        string message = GetSequenceStepWrongMessage(lesson, failedStepIndex);
        AudioClip clip = GetSequenceStepWrongAudio(lesson, failedStepIndex);

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);

        int stepToReplay = resetSequenceToStartOnWrongAnswer ? 0 : currentSequenceStep;

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(PlaySequenceStepInstruction(lesson, stepToReplay));
    }

    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        string message = GetSupportMessage(lesson);

        yield return ShowBubbleMessageSynced(message, lesson.supportAudio, noAudioTextDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        if (lesson.lessonKind == LessonKind.Sequence)
        {
            int stepToReplay = resetSequenceToStartOnWrongAnswer ? 0 : currentSequenceStep;
            yield return PlaySequenceStepInstruction(lesson, stepToReplay);
        }
        else
        {
            yield return RepeatCurrentInstruction(lesson);
        }
    }

    private string GetSupportMessage(BrailleLesson lesson)
    {
        if (!string.IsNullOrWhiteSpace(lesson.supportMessage))
            return lesson.supportMessage;

        if (lesson.lessonKind == LessonKind.Sequence)
        {
            if (resetSequenceToStartOnWrongAnswer)
                return $"Here is some help. Start again from {GetSequenceStepName(lesson, 0)}.";
            else
                return $"Here is some help. Press {GetSequenceStepName(lesson, currentSequenceStep)}.";
        }

        return $"Here is some help. {lesson.displayLabel} uses {GetDotsDisplay(lesson.dots)}.";
    }

    private void HandleRepeat()
    {
        if (waitingForDescriptionChoice)
        {
            if (currentLessonForDescription == null)
                return;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(ShowLessonDescription(currentLessonForDescription));
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            StartLesson(0);
            return;
        }

        if (sceneFinished)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(RepeatCurrentInstruction(lesson));
    }

    private IEnumerator RepeatCurrentInstruction(BrailleLesson lesson)
    {
        if (lesson.lessonKind == LessonKind.SymbolOnly)
        {
            string message = !string.IsNullOrWhiteSpace(lesson.repeatMessage)
                ? lesson.repeatMessage
                : $"Repeat: {lesson.displayLabel}. Press {GetDotsDisplay(lesson.dots)}.";

            yield return ShowBubbleMessageSynced(message, lesson.instructionAudio, noAudioTextDelay);
        }
        else
        {
            yield return PlaySequenceStepInstruction(lesson, currentSequenceStep);
        }
    }

    private void HandleNext()
    {
        if (waitingForDescriptionChoice)
        {
            waitingForDescriptionChoice = false;
            waitingForNext = false;

            if (pressText != null)
                pressText.text = "PRESS!";

            StartLesson(currentLessonIndex + 1);
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(FinalizeSceneCompletion());
            return;
        }

        if (sceneFinished || !waitingForNext)
            return;

        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        waitingForNext = false;
        waitingForDescriptionChoice = false;
        sceneFinished = false;
        waitingForRepeatChoice = true;

        if (translationText != null)
            translationText.text = "-";

        if (pressText != null)
            pressText.text = "R = REPEAT | Y = FINISH";

        if (lessonImage != null)
            lessonImage.enabled = false;

        yield return ShowBubbleMessageSynced(repeatQuestionMessage, repeatQuestionAudio, noAudioTextDelay);
    }

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForNext = false;
        waitingForRepeatChoice = false;
        waitingForDescriptionChoice = false;

        if (translationText != null)
            translationText.text = "-";

        if (pressText != null)
            pressText.text = "DONE!";

        if (lessonImage != null)
            lessonImage.enabled = false;

        yield return ShowBubbleMessageSynced(completedMessage, genericCompletedAudio, noAudioTextDelay);
    }

    private IEnumerator ShowBubbleMessageSynced(string message, AudioClip clip, float fallbackWait)
    {
        if (bubbleMessageText == null)
            yield break;

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
        if (bubbleMessageText == null)
            yield break;

        StopBubbleTyping();

        float totalDuration = GetTotalClipDuration(clips);
        float charDelay = GetCharacterDelayForMessage(message, totalDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, () => typingFinished = true));

        if (voiceAudioSource != null)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[i];
                if (clip == null)
                    continue;

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
        if (bubbleMessageText == null)
            yield break;

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
        if (!useTypewriterEffect)
            return 0f;

        int visibleLength = GetVisibleCharacterCount(message);

        if (visibleLength <= 0)
            return defaultTypewriterCharacterDelay;

        if (audioDuration <= 0f)
            return defaultTypewriterCharacterDelay;

        float syncedDelay = audioDuration / visibleLength;
        return Mathf.Clamp(syncedDelay, minSyncedCharacterDelay, maxSyncedCharacterDelay);
    }

    private int GetVisibleCharacterCount(string message)
    {
        if (string.IsNullOrEmpty(message))
            return 0;

        int count = 0;

        for (int i = 0; i < message.Length; i++)
        {
            if (!char.IsWhiteSpace(message[i]))
                count++;
        }

        return Mathf.Max(1, count);
    }

    private float EstimatedTypingDuration(string message, float characterDelay)
    {
        return GetVisibleCharacterCount(message) * characterDelay;
    }

    private float GetClipDuration(AudioClip clip)
    {
        return clip != null ? clip.length : 0f;
    }

    private float GetTotalClipDuration(params AudioClip[] clips)
    {
        float total = 0f;

        if (clips == null)
            return total;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                total += clips[i].length;
        }

        return total;
    }

    public static string PatternFromDots(int[] dots)
    {
        char[] pattern = { '0', '0', '0', '0', '0', '0' };

        if (dots != null)
        {
            foreach (int dot in dots)
            {
                if (dot >= 1 && dot <= 6)
                    pattern[dot - 1] = '1';
            }
        }

        return new string(pattern);
    }

    public static int[] DotsFromPattern(string pattern)
    {
        List<int> dots = new List<int>();

        if (string.IsNullOrEmpty(pattern))
            return dots.ToArray();

        for (int i = 0; i < pattern.Length && i < 6; i++)
        {
            if (pattern[i] == '1')
                dots.Add(i + 1);
        }

        return dots.ToArray();
    }

    public static string GetDotsDisplay(int[] dots)
    {
        if (dots == null || dots.Length == 0)
            return "no dots";

        if (dots.Length == 1)
            return $"Dot {dots[0]}";

        return "Dots " + string.Join(", ", dots);
    }
}