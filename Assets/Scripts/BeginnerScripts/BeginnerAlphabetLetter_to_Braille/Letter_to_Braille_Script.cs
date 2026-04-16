using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Letter_to_Braille_Script : MonoBehaviour
{
    [Serializable]
    public class LetterLesson
    {
        public string letter;
        public int[] dots;

        [Header("Visual")]
        public Sprite lessonSprite;

        [Header("Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip successAudio;
    }

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text translationLetterText;
    public TMP_Text pressText;
    public TMP_Text alphabetsText;
    public Image characterBubbleImage;
    public Image lessonImage;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Intro Messages")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Play!";
    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's learn how to translate letters into Braille.";

    [Header("Lesson Flow")]
    public List<LetterLesson> lessons = new List<LetterLesson>();
    public bool autoBuildAlphabetLessons = true;
    public bool playInstructionOnLetterStart = true;
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Typewriter Sync")]
    public bool useTypewriterEffect = true;
    [Min(0.005f)] public float defaultTypewriterCharacterDelay = 0.03f;
    [Min(0.001f)] public float minSyncedCharacterDelay = 0.01f;
    [Min(0.001f)] public float maxSyncedCharacterDelay = 0.12f;
    public bool waitForFullAudioBeforeContinuing = true;

    [Header("Debug")]
    public bool logDebug = true;

    private int currentLessonIndex = -1;
    private bool lessonActive;
    private bool waitingForNext;
    private bool sceneFinished;
    private bool waitingForRepeatChoice;
    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

    private readonly Dictionary<string, string> braillePatterns = new Dictionary<string, string>();

    private void Awake()
    {
        BuildBraillePatternDictionary();

        if (autoBuildAlphabetLessons && (lessons == null || lessons.Count == 0))
        {
            AutoCreateAlphabetLessons();
        }
    }

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

    private void Start()
    {
        if (logDebug)
            Debug.Log("Letter_to_Braille_Script started");

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
        lessonActive = true;
        waitingForNext = false;
        waitingForRepeatChoice = false;
        sceneFinished = false;

        LetterLesson lesson = lessons[currentLessonIndex];

        if (translationLetterText != null)
            translationLetterText.text = lesson.letter;

        if (alphabetsText != null)
            alphabetsText.text = "ALPHABETS";

        if (pressText != null)
            pressText.text = "PRESS!";

        if (lessonImage != null)
        {
            lessonImage.sprite = lesson.lessonSprite;
            lessonImage.enabled = lesson.lessonSprite != null;
        }

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(StartLessonSequence(lesson));
    }

    private IEnumerator StartLessonSequence(LetterLesson lesson)
    {
        string dotLabel = GetDotsDisplay(lesson.dots);
        string message = $"Letter {lesson.letter}. Press {dotLabel}.";

        if (logDebug)
            Debug.Log($"Starting lesson for {lesson.letter} -> {PatternFromDots(lesson.dots)}");

        if (playInstructionOnLetterStart)
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

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || waitingForNext || sceneFinished || waitingForRepeatChoice)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        LetterLesson lesson = lessons[currentLessonIndex];
        string expectedPattern = PatternFromDots(lesson.dots);

        if (logDebug)
            Debug.Log($"Submitted: {submittedPattern} | Expected: {expectedPattern}");

        if (submittedPattern == expectedPattern)
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

            flowRoutine = StartCoroutine(HandleWrongAnswer(lesson));
        }
    }

    private IEnumerator HandleCorrectAnswer(LetterLesson lesson)
    {
        string message = $"Correct! {lesson.letter} is {GetDotsDisplay(lesson.dots)}. Press Y for next.";
        AudioClip clipToUse = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clipToUse, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);
    }

    private IEnumerator HandleWrongAnswer(LetterLesson lesson)
    {
        string message = $"Try again. Letter {lesson.letter} uses {GetDotsDisplay(lesson.dots)}.";
        yield return ShowBubbleMessageSynced(message, genericTryAgainAudio, noAudioTextDelay);
    }

    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            if (logDebug)
                Debug.Log("Repeat selected after lesson completion");

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

        LetterLesson lesson = lessons[currentLessonIndex];

        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(RepeatCurrentInstruction(lesson));
    }

    private IEnumerator RepeatCurrentInstruction(LetterLesson lesson)
    {
        string message = $"Repeat: Letter {lesson.letter}. Press {GetDotsDisplay(lesson.dots)}.";
        yield return ShowBubbleMessageSynced(message, lesson.instructionAudio, noAudioTextDelay);
    }

    private void HandleNext()
    {
        if (logDebug)
            Debug.Log("Y pressed / HandleNext called");

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;

            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(FinalizeSceneCompletion());
            return;
        }

        if (sceneFinished)
            return;

        if (!waitingForNext)
        {
            if (logDebug)
                Debug.Log("Not waiting for next yet");
            return;
        }

        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        waitingForNext = false;
        sceneFinished = false;
        waitingForRepeatChoice = true;

        if (translationLetterText != null)
            translationLetterText.text = "-";

        if (pressText != null)
            pressText.text = "Press!";

        if (lessonImage != null)
            lessonImage.enabled = false;

        string message = "You finished all letters. Do you want to repeat the lesson? Press R to repeat or next to finish.";
        yield return ShowBubbleMessageSynced(message, repeatQuestionAudio, noAudioTextDelay);
    }

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForNext = false;
        waitingForRepeatChoice = false;

        if (translationLetterText != null)
            translationLetterText.text = "-";

        if (pressText != null)
            pressText.text = "DONE!";

        if (lessonImage != null)
            lessonImage.enabled = false;

        string message = "Great job! You finished the Letter to Braille lesson.";
        yield return ShowBubbleMessageSynced(message, genericCompletedAudio, noAudioTextDelay);
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

    private void BuildBraillePatternDictionary()
    {
        braillePatterns.Clear();

        braillePatterns["A"] = "100000"; // 1
        braillePatterns["B"] = "110000"; // 1,2
        braillePatterns["C"] = "100100"; // 1,4
        braillePatterns["D"] = "100110"; // 1,4,5
        braillePatterns["E"] = "100010"; // 1,5
        braillePatterns["F"] = "110100"; // 1,2,4
        braillePatterns["G"] = "110110"; // 1,2,4,5
        braillePatterns["H"] = "110010"; // 1,2,5
        braillePatterns["I"] = "010100"; // 2,4
        braillePatterns["J"] = "010110"; // 2,4,5
        braillePatterns["K"] = "101000"; // 1,3
        braillePatterns["L"] = "111000"; // 1,2,3
        braillePatterns["M"] = "101100"; // 1,3,4
        braillePatterns["N"] = "101110"; // 1,3,4,5
        braillePatterns["O"] = "101010"; // 1,3,5
        braillePatterns["P"] = "111100"; // 1,2,3,4
        braillePatterns["Q"] = "111110"; // 1,2,3,4,5
        braillePatterns["R"] = "111010"; // 1,2,3,5
        braillePatterns["S"] = "011100"; // 2,3,4
        braillePatterns["T"] = "011110"; // 2,3,4,5
        braillePatterns["U"] = "101001"; // 1,3,6
        braillePatterns["V"] = "111001"; // 1,2,3,6
        braillePatterns["W"] = "010111"; // 2,4,5,6
        braillePatterns["X"] = "101101"; // 1,3,4,6
        braillePatterns["Y"] = "101111"; // 1,3,4,5,6
        braillePatterns["Z"] = "101011"; // 1,3,5,6
    }

    private void AutoCreateAlphabetLessons()
    {
        lessons = new List<LetterLesson>();

        foreach (var kvp in braillePatterns)
        {
            lessons.Add(new LetterLesson
            {
                letter = kvp.Key,
                dots = DotsFromPattern(kvp.Value),
                lessonSprite = null,
                introAudio = null,
                instructionAudio = null,
                successAudio = null
            });
        }

        lessons.Sort((a, b) => string.Compare(a.letter, b.letter, StringComparison.Ordinal));
    }

    private int[] DotsFromPattern(string pattern)
    {
        List<int> dots = new List<int>();

        for (int i = 0; i < pattern.Length && i < 6; i++)
        {
            if (pattern[i] == '1')
                dots.Add(i + 1);
        }

        return dots.ToArray();
    }

    private string PatternFromDots(int[] dots)
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

    private string GetDotsDisplay(int[] dots)
    {
        if (dots == null || dots.Length == 0)
            return "no dots";

        if (dots.Length == 1)
            return $"Dot {dots[0]}";

        return "Dots " + string.Join(", ", dots);
    }
}