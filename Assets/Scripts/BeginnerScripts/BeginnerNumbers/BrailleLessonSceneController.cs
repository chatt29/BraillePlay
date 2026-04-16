using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BrailleLessonSceneController : MonoBehaviour
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

        [Header("Lesson Type")]
        public LessonKind lessonKind = LessonKind.SymbolOnly;

        [Tooltip("Used for SymbolOnly lessons")]
        public int[] dots;

        [Tooltip("Used for Sequence lessons. Example: 001111 then 100000")]
        public List<string> expectedSequencePatterns = new List<string>();

        [Tooltip("Friendly names for each sequence step. Example: # then 1")]
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

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Play!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's learn braille.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished this braille lesson.";

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public bool playInstructionOnLessonStart = true;
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    public bool showHeldDotsPattern = true;
    public bool resetSequenceToStartOnWrongAnswer = true;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    private int currentLessonIndex = -1;
    private int currentSequenceStep = 0;
    private int currentMistakeCount = 0;
    private bool lessonActive;
    private bool waitingForNext;
    private bool sceneFinished;
    private Coroutine flowRoutine;

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
        if (!showHeldDotsPattern || livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("BrailleLessonSceneController started");

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(BeginSceneFlow());
    }

    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        waitingForNext = false;
        sceneFinished = false;

        SetBubbleMessage(welcomeMessage);
        yield return PlayClipOrWait(welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        SetBubbleMessage(letsLearnMessage);
        yield return PlayClipOrWait(letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            CompleteScene();
            return;
        }

        currentLessonIndex = index;
        currentSequenceStep = 0;
        currentMistakeCount = 0;
        lessonActive = true;
        waitingForNext = false;

        BrailleLesson lesson = lessons[currentLessonIndex];

        if (translationText != null)
            translationText.text = lesson.displayLabel;

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel) ? "BRAILLE" : lesson.categoryLabel;

        if (pressText != null)
            pressText.text = "PRESS!";

        if (lessonImage != null)
        {
            lessonImage.sprite = lesson.lessonSprite;
            lessonImage.enabled = lesson.lessonSprite != null;
        }

        if (lesson.lessonKind == LessonKind.SymbolOnly)
            SetBubbleMessage(GetLessonPrompt(lesson));
        else
            SetBubbleMessage(GetSequenceStepMessage(lesson, 0));

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lesson.displayLabel}");

        if (playInstructionOnLessonStart)
        {
            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            if (lesson.lessonKind == LessonKind.SymbolOnly)
                flowRoutine = StartCoroutine(PlayLessonInstruction(lesson));
            else
                flowRoutine = StartCoroutine(PlaySequenceStepInstruction(lesson, 0));
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

    private IEnumerator PlayLessonInstruction(BrailleLesson lesson)
    {
        if (lesson.introAudio != null)
            yield return PlayClipAndWait(lesson.introAudio);

        if (lesson.instructionAudio != null)
            yield return PlayClipAndWait(lesson.instructionAudio);
        else
            yield return new WaitForSeconds(noAudioTextDelay);
    }

    private IEnumerator PlaySequenceStepInstruction(BrailleLesson lesson, int stepIndex)
    {
        string stepMessage = GetSequenceStepMessage(lesson, stepIndex);

        if (!string.IsNullOrWhiteSpace(stepMessage))
            SetBubbleMessage(stepMessage);

        AudioClip clip = GetSequenceStepAudio(lesson, stepIndex);

        if (clip != null)
            yield return PlayClipAndWait(clip);
        else
            yield return new WaitForSeconds(noAudioTextDelay);
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

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || waitingForNext || sceneFinished)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        if (lesson.lessonKind == LessonKind.SymbolOnly)
            HandleSinglePatternLesson(lesson, submittedPattern);
        else
            HandleSequenceLesson(lesson, submittedPattern);
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
            waitingForNext = true;

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
                waitingForNext = true;

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
            currentMistakeCount++;

            if (resetSequenceToStartOnWrongAnswer)
                currentSequenceStep = 0;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            if (currentMistakeCount >= mistakesBeforeSupport)
                flowRoutine = StartCoroutine(HandleSupportThenRetry(lesson));
            else
                flowRoutine = StartCoroutine(HandleWrongAnswer(lesson));
        }
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

        return $"step {stepIndex + 1}";
    }

    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson)
    {
        string message = !string.IsNullOrWhiteSpace(lesson.successMessage)
            ? lesson.successMessage
            : $"Correct! {lesson.displayLabel}. Press Y for next.";

        SetBubbleMessage(message);

        if (lesson.successAudio != null)
            yield return PlayClipAndWait(lesson.successAudio);
        else if (genericCorrectAudio != null)
            yield return PlayClipAndWait(genericCorrectAudio);

        yield return new WaitForSeconds(delayAfterCorrect);
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

        SetBubbleMessage(message);

        if (genericTryAgainAudio != null)
            yield return PlayClipAndWait(genericTryAgainAudio);
        else
            yield return new WaitForSeconds(noAudioTextDelay);

        if (lesson.lessonKind == LessonKind.Sequence)
        {
            int stepToReplay = resetSequenceToStartOnWrongAnswer ? 0 : currentSequenceStep;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(PlaySequenceStepInstruction(lesson, stepToReplay));
        }
    }

    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        string message = GetSupportMessage(lesson);
        SetBubbleMessage(message);

        if (lesson.supportAudio != null)
            yield return PlayClipAndWait(lesson.supportAudio);
        else
            yield return new WaitForSeconds(noAudioTextDelay);

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
            if (!string.IsNullOrWhiteSpace(lesson.repeatMessage))
                SetBubbleMessage(lesson.repeatMessage);
            else
                SetBubbleMessage($"Repeat: {lesson.displayLabel}. Press {GetDotsDisplay(lesson.dots)}.");

            if (lesson.instructionAudio != null)
                yield return PlayClipAndWait(lesson.instructionAudio);
            else
                yield return new WaitForSeconds(noAudioTextDelay);
        }
        else
        {
            yield return PlaySequenceStepInstruction(lesson, currentSequenceStep);
        }
    }

    private void HandleNext()
    {
        if (sceneFinished || !waitingForNext)
            return;

        StartLesson(currentLessonIndex + 1);
    }

    private void CompleteScene()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForNext = false;

        if (translationText != null)
            translationText.text = "-";

        if (pressText != null)
            pressText.text = "DONE!";

        SetBubbleMessage(completedMessage);

        if (lessonImage != null)
            lessonImage.enabled = false;

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(PlayCompletionAudio());
    }

    private IEnumerator PlayCompletionAudio()
    {
        if (genericCompletedAudio != null)
            yield return PlayClipAndWait(genericCompletedAudio);
    }

    private void SetBubbleMessage(string message)
    {
        if (bubbleMessageText != null)
            bubbleMessageText.text = message;
    }

    private IEnumerator PlayClipOrWait(AudioClip clip, float fallbackWait)
    {
        if (voiceAudioSource != null && clip != null)
            yield return PlayClipAndWait(clip);
        else
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator PlayClipAndWait(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null)
            yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();

        yield return new WaitForSeconds(clip.length);
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