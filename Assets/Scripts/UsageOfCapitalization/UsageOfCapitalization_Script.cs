using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UsageOfCapitalization_Script : MonoBehaviour
{
    public enum LessonState
    {
        StageIntro,
        WaitingForCapitalizationInput,
        WaitingForSentenceTyping,
        AskingRepeatOrContinue,
        AskingRestartOrNextLesson,
        Ended
    }

    public enum CapitalizationStage
    {
        Letter = 0,
        Word = 1,
        Sentence = 2
    }

    [Serializable]
    public class LessonLine
    {
        [TextArea(2, 4)]
        public string message;
        public AudioClip audioClip;
    }

    [Header("UI References")]
    public TMP_Text bubbleText;
    public TMP_Text translationText;
    public TMP_Text resultText;
    public TMP_Text stageTypeText;
    public Image lessonImage;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Lesson Visuals")]
    public Sprite dot6Image;

    [Header("Start")]
    public LessonLine startLessonLine;

    [Header("Stage Intro Lines")]
    public LessonLine letterStageIntroLine;
    public LessonLine wordStageIntroLine;
    public LessonLine sentenceStageIntroLine;

    [Header("Stage Prompt Lines")]
    public LessonLine letterStagePromptLine;
    public LessonLine wordStagePromptLine;
    public LessonLine sentenceStagePromptLine;

    [Header("Stage Progress Lines")]
    public LessonLine wordStageSecondPressLine;
    public LessonLine sentenceStageSecondPressLine;
    public LessonLine sentenceStageThirdPressLine;

    [Header("Typing Prompt Lines")]
    public LessonLine letterTypingPromptLine;
    public LessonLine wordTypingPromptLine;
    public LessonLine sentenceTypingPromptLine;

    [Header("Wrong Input Lines")]
    public LessonLine wrongLetterStageLine;
    public LessonLine wrongWordStageLine;
    public LessonLine wrongSentenceStageLine;
    public LessonLine wrongTypingLine;

    [Header("Stage Success Lines")]
    public LessonLine letterStageSuccessLine;
    public LessonLine wordStageSuccessLine;
    public LessonLine sentenceStageSuccessLine;

    [Header("Repeat Or Continue")]
    public LessonLine stageDecisionLine;

    [Header("Lesson Completion")]
    public LessonLine completedLessonLine;
    public LessonLine restartOrNextLessonLine;
    public LessonLine endLessonLine;

    [Header("Sentence Content")]
    public string baseSentence = "the cat is cute.";
    public string letterSentence = "The cat is cute.";
    public string wordSentence = "THE cat is cute.";
    public string sentenceSentence = "THE CAT IS CUTE.";

    [Header("Options")]
    public bool playOnStart = true;
    public float extraWaitAfterAudio = 0.15f;
    public string nextLessonSceneName = "";

    private const string DOT_6 = "000001";
    private const string LETTERS_ONLY_SENTENCE = "thecatiscute";
    private const string PRESS_TEXT = "PRESS";

    private Coroutine sequenceRoutine;
    private int currentStageIndex = 0;
    private int currentDot6Count = 0;
    private string typedLetters = "";
    private bool pendingCapitalSignDuringTyping = false;

    private Dictionary<string, char> brailleToLetter;

    public LessonState CurrentState { get; private set; } = LessonState.StageIntro;
    public CapitalizationStage CurrentStage => (CapitalizationStage)currentStageIndex;

    private void Awake()
    {
        BuildBrailleDictionary();
    }

    private void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleSecretAutoType;
    }

    private void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleSecretAutoType;
    }

    private void Start()
    {
        if (playOnStart)
            StartLesson();
    }

    public void StartLesson()
    {
        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(StartLessonRoutine());
    }

    private IEnumerator StartLessonRoutine()
    {
        currentStageIndex = 0;
        currentDot6Count = 0;
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;

        SetStageType("");
        SetTranslation(baseSentence);
        SetResult("");
        SetLessonImage(dot6Image);

        sequenceRoutine = StartCoroutine(BeginStageRoutine());
        yield break;
    }

    public void NextOrConfirmYes()
    {
        switch (CurrentState)
        {
            case LessonState.AskingRepeatOrContinue:
                ContinueToNextStageOrFinish();
                break;

            case LessonState.AskingRestartOrNextLesson:
                RestartLessonFromBeginning();
                break;
        }
    }

    public void NoOrEndLesson()
    {
        switch (CurrentState)
        {
            case LessonState.AskingRepeatOrContinue:
                RepeatCurrentStage();
                break;

            case LessonState.AskingRestartOrNextLesson:
                ContinueToNextLesson();
                break;

            case LessonState.WaitingForSentenceTyping:
                typedLetters = "";
                pendingCapitalSignDuringTyping = false;
                SetTranslation(GetTargetSentenceForStage(CurrentStage));
                SetResult("Typing cleared. Try again.");
                ShowLine(GetTypingPromptLine(CurrentStage));
                break;
        }
    }

    public void GoBackOrRestartPrompt()
    {
        switch (CurrentState)
        {
            case LessonState.WaitingForCapitalizationInput:
                RepeatCurrentStage();
                break;

            case LessonState.WaitingForSentenceTyping:
                typedLetters = "";
                pendingCapitalSignDuringTyping = false;
                currentDot6Count = 0;
                SetTranslation(baseSentence);
                CurrentState = LessonState.WaitingForCapitalizationInput;
                ReplayCurrentStagePrompt();
                break;

            case LessonState.AskingRepeatOrContinue:
                ShowLine(stageDecisionLine);
                break;

            case LessonState.AskingRestartOrNextLesson:
                ShowLine(restartOrNextLessonLine);
                break;
        }
    }

    public void RepeatCurrent()
    {
        switch (CurrentState)
        {
            case LessonState.StageIntro:
            case LessonState.WaitingForCapitalizationInput:
            case LessonState.WaitingForSentenceTyping:
                ReplayCurrentStagePrompt();
                break;

            case LessonState.AskingRepeatOrContinue:
                ShowLine(stageDecisionLine);
                break;

            case LessonState.AskingRestartOrNextLesson:
                ShowLine(restartOrNextLessonLine);
                break;

            case LessonState.Ended:
                ShowLine(endLessonLine);
                break;
        }
    }

    public void HandleBrailleInput(string pattern)
    {
        if (CurrentState == LessonState.WaitingForCapitalizationInput)
        {
            HandleCapitalizationInput(pattern);
            return;
        }

        if (CurrentState == LessonState.WaitingForSentenceTyping)
        {
            HandleSentenceTyping(pattern);
        }
    }

    private IEnumerator BeginStageRoutine()
    {
        CurrentState = LessonState.StageIntro;
        currentDot6Count = 0;
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;

        SetLessonImage(dot6Image);
        SetResult("");
        SetStageType(GetStageLabel(CurrentStage));
        SetTranslation(baseSentence);

        if (CurrentStage == CapitalizationStage.Letter)
        {
            ShowLine(startLessonLine);
            yield return WaitForAudio(startLessonLine != null ? startLessonLine.audioClip : null);
        }

        LessonLine introLine = GetStageIntroLine(CurrentStage);
        ShowLine(introLine);
        yield return WaitForAudio(introLine != null ? introLine.audioClip : null);

        LessonLine promptLine = GetStagePromptLine(CurrentStage);
        SetResult(PRESS_TEXT);
        ShowLine(promptLine);
        yield return WaitForAudio(promptLine != null ? promptLine.audioClip : null);

        CurrentState = LessonState.WaitingForCapitalizationInput;
    }

    private void HandleCapitalizationInput(string pattern)
    {
        if (pattern != DOT_6)
        {
            ResetCapitalizationAttempt();
            SetResult("Incorrect.");
            ShowLine(GetWrongStageLine(CurrentStage));
            return;
        }

        currentDot6Count++;

        switch (CurrentStage)
        {
            case CapitalizationStage.Letter:
                if (currentDot6Count == 1)
                {
                    BeginTypingPhase();
                    return;
                }
                break;

            case CapitalizationStage.Word:
                if (currentDot6Count == 1)
                {
                    SetResult(PRESS_TEXT);
                    ShowLine(wordStageSecondPressLine);
                    return;
                }

                if (currentDot6Count == 2)
                {
                    BeginTypingPhase();
                    return;
                }
                break;

            case CapitalizationStage.Sentence:
                if (currentDot6Count == 1)
                {
                    SetResult(PRESS_TEXT);
                    ShowLine(sentenceStageSecondPressLine);
                    return;
                }

                if (currentDot6Count == 2)
                {
                    SetResult(PRESS_TEXT);
                    ShowLine(sentenceStageThirdPressLine);
                    return;
                }

                if (currentDot6Count == 3)
                {
                    BeginTypingPhase();
                    return;
                }
                break;
        }

        ResetCapitalizationAttempt();
        SetResult("Incorrect.");
        ShowLine(GetWrongStageLine(CurrentStage));
    }

    private void BeginTypingPhase()
    {
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;
        SetTranslation(GetTargetSentenceForStage(CurrentStage));

        if (CurrentStage == CapitalizationStage.Sentence)
        {
            SetResult("Now type the sentence. In a sentence, the first word always starts with a capital letter.");
        }
        else
        {
            SetResult("Now type the sentence.");
        }

        ShowLine(GetTypingPromptLine(CurrentStage));
        CurrentState = LessonState.WaitingForSentenceTyping;
    }

    private void HandleSentenceTyping(string pattern)
    {
        if (pattern == DOT_6)
        {
            pendingCapitalSignDuringTyping = true;
            SetResult(GetCapitalSignExplanation());
            return;
        }

        if (!brailleToLetter.TryGetValue(pattern, out char typedLetter))
        {
            FailTypingAttempt();
            return;
        }

        int nextIndex = typedLetters.Length;

        if (nextIndex >= LETTERS_ONLY_SENTENCE.Length)
            return;

        char expected = LETTERS_ONLY_SENTENCE[nextIndex];

        if (typedLetter != expected)
        {
            Debug.Log("Typing mismatch. Expected: " + expected + " but got: " + typedLetter + " from pattern: " + pattern);
            FailTypingAttempt();
            return;
        }

        typedLetters += typedLetter;
        pendingCapitalSignDuringTyping = false;

        string progress = FormatTypedProgress(typedLetters, CurrentStage);
        SetTranslation(progress);

        if (ShouldBeCapitalAtIndex(nextIndex, CurrentStage))
        {
            SetResult("Typed correctly: capital " + char.ToUpper(typedLetter) + ".");
        }
        else
        {
            SetResult("Typed correctly.");
        }

        if (typedLetters.Length >= LETTERS_ONLY_SENTENCE.Length)
        {
            StopRunningRoutine();
            sequenceRoutine = StartCoroutine(FinishCurrentStageRoutine(GetSuccessLine(CurrentStage)));
        }
    }

    private string GetCapitalSignExplanation()
    {
        int nextIndex = typedLetters.Length;

        if (nextIndex >= LETTERS_ONLY_SENTENCE.Length)
            return "Capital sign entered.";

        char nextLetter = char.ToUpper(LETTERS_ONLY_SENTENCE[nextIndex]);

        string message = "Capital sign entered. The next letter is capital " + nextLetter + ".";

        if (CurrentStage == CapitalizationStage.Sentence)
        {
            message += " In a sentence, the first word always starts with a capital letter.";
        }

        return message;
    }

    private bool ShouldBeCapitalAtIndex(int index, CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter:
                return index == 0;

            case CapitalizationStage.Word:
                return index >= 0 && index <= 2;

            case CapitalizationStage.Sentence:
                return true;

            default:
                return false;
        }
    }

    private void HandleSecretAutoType()
    {
        if (CurrentState != LessonState.WaitingForSentenceTyping)
            return;

        typedLetters = LETTERS_ONLY_SENTENCE;
        pendingCapitalSignDuringTyping = false;

        SetTranslation(GetTargetSentenceForStage(CurrentStage));

        if (CurrentStage == CapitalizationStage.Sentence)
        {
            SetResult("Sentence completed. In a sentence, the first word always starts with a capital letter.");
        }
        else
        {
            SetResult("Sentence completed.");
        }

        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(FinishCurrentStageRoutine(GetSuccessLine(CurrentStage)));
    }

    private IEnumerator FinishCurrentStageRoutine(LessonLine successLine)
    {
        CurrentState = LessonState.StageIntro;

        SetTranslation(GetTargetSentenceForStage(CurrentStage));

        if (CurrentStage == CapitalizationStage.Sentence)
        {
            SetResult("Correct! In a sentence, the first word always starts with a capital letter.");
        }
        else
        {
            SetResult("Correct!");
        }

        ShowLine(successLine);
        yield return WaitForAudio(successLine != null ? successLine.audioClip : null);

        CurrentState = LessonState.AskingRepeatOrContinue;
        ShowLine(stageDecisionLine);
        yield return WaitForAudio(stageDecisionLine != null ? stageDecisionLine.audioClip : null);
    }

    private void ContinueToNextStageOrFinish()
    {
        if (CurrentState != LessonState.AskingRepeatOrContinue)
            return;

        if (currentStageIndex < 2)
        {
            currentStageIndex++;
            StopRunningRoutine();
            sequenceRoutine = StartCoroutine(BeginStageRoutine());
        }
        else
        {
            StopRunningRoutine();
            sequenceRoutine = StartCoroutine(FinishLessonRoutine());
        }
    }

    private void RepeatCurrentStage()
    {
        currentDot6Count = 0;
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;
        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(BeginStageRoutine());
    }

    private IEnumerator FinishLessonRoutine()
    {
        CurrentState = LessonState.StageIntro;

        SetStageType("Sentence");
        SetTranslation(sentenceSentence);
        SetLessonImage(dot6Image);
        SetResult("In a sentence, the first word always starts with a capital letter.");

        ShowLine(completedLessonLine);
        yield return WaitForAudio(completedLessonLine != null ? completedLessonLine.audioClip : null);

        CurrentState = LessonState.AskingRestartOrNextLesson;
        ShowLine(restartOrNextLessonLine);
        yield return WaitForAudio(restartOrNextLessonLine != null ? restartOrNextLessonLine.audioClip : null);
    }

    private IEnumerator EndLessonRoutine()
    {
        CurrentState = LessonState.Ended;
        ShowLine(endLessonLine);
        yield return WaitForAudio(endLessonLine != null ? endLessonLine.audioClip : null);
    }

    private void RestartLessonFromBeginning()
    {
        StopRunningRoutine();
        currentStageIndex = 0;
        currentDot6Count = 0;
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;
        sequenceRoutine = StartCoroutine(StartLessonRoutine());
    }

    private void ContinueToNextLesson()
    {
        StopRunningRoutine();

        if (!string.IsNullOrEmpty(nextLessonSceneName))
        {
            SceneManager.LoadScene(nextLessonSceneName);
            return;
        }

        CurrentState = LessonState.Ended;
        SetResult(" ");
        ShowLine(endLessonLine);
        Debug.Log("No next lesson scene assigned yet. Showing end lesson line instead.");
    }

    private void ReplayCurrentStagePrompt()
    {
        SetLessonImage(dot6Image);
        SetStageType(GetStageLabel(CurrentStage));
        SetResult(PRESS_TEXT);

        if (CurrentState == LessonState.WaitingForSentenceTyping)
        {
            SetTranslation(GetTargetSentenceForStage(CurrentStage));

            if (CurrentStage == CapitalizationStage.Sentence)
            {
                SetResult("In a sentence, the first word always starts with a capital letter.");
            }

            ShowLine(GetTypingPromptLine(CurrentStage));
            return;
        }

        switch (CurrentStage)
        {
            case CapitalizationStage.Letter:
                SetTranslation(baseSentence);
                ShowLine(letterStagePromptLine);
                break;

            case CapitalizationStage.Word:
                SetTranslation(baseSentence);
                if (currentDot6Count == 0) ShowLine(wordStagePromptLine);
                else ShowLine(wordStageSecondPressLine);
                break;

            case CapitalizationStage.Sentence:
                SetTranslation(baseSentence);

                if (currentDot6Count == 0) ShowLine(sentenceStagePromptLine);
                else if (currentDot6Count == 1) ShowLine(sentenceStageSecondPressLine);
                else ShowLine(sentenceStageThirdPressLine);
                break;
        }
    }

    private void ResetCapitalizationAttempt()
    {
        currentDot6Count = 0;
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;
        SetLessonImage(dot6Image);
        SetTranslation(baseSentence);
    }

    private void FailTypingAttempt()
    {
        typedLetters = "";
        pendingCapitalSignDuringTyping = false;
        SetTranslation(GetTargetSentenceForStage(CurrentStage));
        SetResult("Incorrect. Try typing the sentence again.");
        ShowLine(wrongTypingLine);
    }

    private LessonLine GetStageIntroLine(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return letterStageIntroLine;
            case CapitalizationStage.Word: return wordStageIntroLine;
            default: return sentenceStageIntroLine;
        }
    }

    private LessonLine GetStagePromptLine(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return letterStagePromptLine;
            case CapitalizationStage.Word: return wordStagePromptLine;
            default: return sentenceStagePromptLine;
        }
    }

    private LessonLine GetTypingPromptLine(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return letterTypingPromptLine;
            case CapitalizationStage.Word: return wordTypingPromptLine;
            default: return sentenceTypingPromptLine;
        }
    }

    private LessonLine GetWrongStageLine(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return wrongLetterStageLine;
            case CapitalizationStage.Word: return wrongWordStageLine;
            default: return wrongSentenceStageLine;
        }
    }

    private LessonLine GetSuccessLine(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return letterStageSuccessLine;
            case CapitalizationStage.Word: return wordStageSuccessLine;
            default: return sentenceStageSuccessLine;
        }
    }

    private string GetStageLabel(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return "Letter";
            case CapitalizationStage.Word: return "Word";
            default: return "Sentence";
        }
    }

    private string GetTargetSentenceForStage(CapitalizationStage stage)
    {
        switch (stage)
        {
            case CapitalizationStage.Letter: return letterSentence;
            case CapitalizationStage.Word: return wordSentence;
            default: return sentenceSentence;
        }
    }

    private string FormatTypedProgress(string rawLetters, CapitalizationStage stage)
    {
        char[] chars = rawLetters.ToCharArray();

        switch (stage)
        {
            case CapitalizationStage.Letter:
                if (chars.Length > 0)
                    chars[0] = char.ToUpper(chars[0]);
                break;

            case CapitalizationStage.Word:
                for (int i = 0; i < chars.Length && i < 3; i++)
                    chars[i] = char.ToUpper(chars[i]);
                break;

            case CapitalizationStage.Sentence:
                for (int i = 0; i < chars.Length; i++)
                    chars[i] = char.ToUpper(chars[i]);
                break;
        }

        string noSpaces = new string(chars);

        if (noSpaces.Length == 0) return "";
        if (noSpaces.Length <= 3) return noSpaces;
        if (noSpaces.Length <= 6) return noSpaces.Substring(0, 3) + " " + noSpaces.Substring(3);
        if (noSpaces.Length <= 8) return noSpaces.Substring(0, 3) + " " + noSpaces.Substring(3, 3) + " " + noSpaces.Substring(6);

        string formatted =
            noSpaces.Substring(0, 3) + " " +
            noSpaces.Substring(3, 3) + " " +
            noSpaces.Substring(6, 2) + " " +
            noSpaces.Substring(8);

        if (rawLetters.Length == LETTERS_ONLY_SENTENCE.Length)
            formatted += ".";

        return formatted;
    }

    private void ShowLine(LessonLine line)
    {
        if (bubbleText != null)
            bubbleText.text = line != null ? line.message : "";

        PlayAudio(line != null ? line.audioClip : null);
    }

    private void SetTranslation(string value)
    {
        if (translationText != null)
            translationText.text = value;
    }

    private void SetResult(string value)
    {
        if (resultText != null)
            resultText.text = value;
    }

    private void SetStageType(string value)
    {
        if (stageTypeText != null)
            stageTypeText.text = value;
    }

    private void SetLessonImage(Sprite sprite)
    {
        if (lessonImage == null) return;

        lessonImage.sprite = sprite;
        lessonImage.enabled = sprite != null;
        lessonImage.preserveAspect = true;
        lessonImage.color = Color.white;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator WaitForAudio(AudioClip clip)
    {
        if (clip == null)
            yield break;

        yield return new WaitForSeconds(clip.length + extraWaitAfterAudio);
    }

    private void StopRunningRoutine()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }

    private void BuildBrailleDictionary()
    {
        brailleToLetter = new Dictionary<string, char>
        {
            { "100000", 'a' },
            { "110000", 'b' },
            { "100100", 'c' },
            { "100110", 'd' },
            { "100010", 'e' },
            { "110100", 'f' },
            { "110110", 'g' },
            { "110010", 'h' },
            { "010100", 'i' },
            { "010110", 'j' },
            { "101000", 'k' },
            { "111000", 'l' },
            { "101100", 'm' },
            { "101110", 'n' },
            { "101010", 'o' },
            { "111100", 'p' },
            { "111110", 'q' },
            { "111010", 'r' },
            { "011100", 's' },
            { "011110", 't' },
            { "101001", 'u' },
            { "111001", 'v' },
            { "010111", 'w' },
            { "101101", 'x' },
            { "101111", 'y' },
            { "101011", 'z' }
        };
    }
}