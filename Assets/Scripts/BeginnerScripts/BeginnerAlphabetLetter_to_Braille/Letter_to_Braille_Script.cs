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

    [Header("Debug")]
    public bool logDebug = true;

    private int currentLessonIndex = -1;
    private bool lessonActive;
    private bool waitingForNext;
    private bool sceneFinished;
    private Coroutine flowRoutine;

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
        lessonActive = true;
        waitingForNext = false;

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

        string dotLabel = GetDotsDisplay(lesson.dots);
        SetBubbleMessage($"Letter {lesson.letter}. Press {dotLabel}.");

        if (logDebug)
            Debug.Log($"Starting lesson for {lesson.letter} -> {PatternFromDots(lesson.dots)}");

        if (playInstructionOnLetterStart)
        {
            if (flowRoutine != null)
                StopCoroutine(flowRoutine);

            flowRoutine = StartCoroutine(PlayLessonInstruction(lesson));
        }
    }

    private IEnumerator PlayLessonInstruction(LetterLesson lesson)
    {
        if (lesson.introAudio != null)
            yield return PlayClipAndWait(lesson.introAudio);

        if (lesson.instructionAudio != null)
            yield return PlayClipAndWait(lesson.instructionAudio);
    }

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || waitingForNext || sceneFinished)
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
        SetBubbleMessage($"Correct! {lesson.letter} is {GetDotsDisplay(lesson.dots)}. Press Y for next.");

        if (lesson.successAudio != null)
            yield return PlayClipAndWait(lesson.successAudio);
        else if (genericCorrectAudio != null)
            yield return PlayClipAndWait(genericCorrectAudio);

        yield return new WaitForSeconds(delayAfterCorrect);
    }

    private IEnumerator HandleWrongAnswer(LetterLesson lesson)
    {
        SetBubbleMessage($"Try again. Letter {lesson.letter} uses {GetDotsDisplay(lesson.dots)}.");

        if (genericTryAgainAudio != null)
            yield return PlayClipAndWait(genericTryAgainAudio);
    }

    private void HandleRepeat()
    {
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
        SetBubbleMessage($"Repeat: Letter {lesson.letter}. Press {GetDotsDisplay(lesson.dots)}.");

        if (lesson.instructionAudio != null)
            yield return PlayClipAndWait(lesson.instructionAudio);
        else
            yield return new WaitForSeconds(noAudioTextDelay);
    }

    private void HandleNext()
    {
        if (logDebug)
            Debug.Log("Y pressed / HandleNext called");

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

    private void CompleteScene()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForNext = false;

        if (translationLetterText != null)
            translationLetterText.text = "-";

        if (pressText != null)
            pressText.text = "DONE!";

        SetBubbleMessage("Great job! You finished the Letter to Braille lesson.");

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
        {
            yield return PlayClipAndWait(clip);
        }
        else
        {
            yield return new WaitForSeconds(fallbackWait);
        }
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