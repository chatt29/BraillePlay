using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UsageOfCapitalization_Script : MonoBehaviour
{
    public enum LessonState
    {
        Intro,
        Teaching,
        WaitingForCapitalSign,
        WaitingForLetterInput,
        Success,
        WaitingForReplayChoice,
        Ended
    }

    [Serializable]
    public class LessonLine
    {
        [TextArea(2, 4)] public string message;
        public AudioClip audioClip;
    }

    [Header("UI References")]
    public TMP_Text bubbleText;
    public TMP_Text translationText;
    public TMP_Text resultText;
    public Image lessonImage;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Lesson Visuals")]
    public Sprite capitalizationImage;
    public Sprite letterABrailleImage;

    [Header("Intro Sequence")]
    public LessonLine welcomeLine;
    public LessonLine instructionLine;

    [Header("Teaching / Practice")]
    public LessonLine promptCapitalSignLine;
    public LessonLine promptLetterLine;
    public LessonLine wrongCapitalSignLine;
    public LessonLine wrongLetterLine;
    public LessonLine successLine;

    [Header("Completion Sequence")]
    public LessonLine completedLine;
    public LessonLine replayQuestionLine;
    public LessonLine endLine;

    [Header("Lesson Content")]
    public string baseLetter = "a";
    public string capitalLetter = "A";

    [Header("Options")]
    public bool playOnStart = true;
    public float extraWaitAfterAudio = 0.15f;

    private const string DOT_1 = "100000";
    private const string DOT_6 = "000001";

    private Coroutine sequenceRoutine;
    private bool capitalSignEntered = false;

    public LessonState CurrentState { get; private set; } = LessonState.Intro;

    private void Start()
    {
        if (playOnStart)
            StartLesson();
    }

    public void StartLesson()
    {
        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        CurrentState = LessonState.Intro;
        capitalSignEntered = false;

        SetTranslation(baseLetter);
        SetResult("");
        SetLessonImage(capitalizationImage);

        ShowLine(welcomeLine);
        yield return WaitForAudio(welcomeLine.audioClip);

        ShowLine(instructionLine);
        yield return WaitForAudio(instructionLine.audioClip);

        CurrentState = LessonState.Teaching;
        ShowCapitalSignStep();

        yield return WaitForAudio(promptCapitalSignLine.audioClip);

        CurrentState = LessonState.WaitingForCapitalSign;
    }

    public void HandleBrailleInput(string pattern)
    {
        if (CurrentState == LessonState.WaitingForCapitalSign)
        {
            if (pattern == DOT_6)
            {
                capitalSignEntered = true;
                SetResult("Correct!");
                CurrentState = LessonState.WaitingForLetterInput;
                ShowLetterStep();
            }
            else
            {
                capitalSignEntered = false;
                SetResult("Incorrect");
                ShowCapitalSignStep(wrongCapitalSignLine);
            }

            return;
        }

        if (CurrentState == LessonState.WaitingForLetterInput)
        {
            if (capitalSignEntered && pattern == DOT_1)
            {
                capitalSignEntered = false;
                SetTranslation(capitalLetter);
                SetResult("Correct!");
                SetLessonImage(letterABrailleImage);

                CurrentState = LessonState.Success;

                StopRunningRoutine();
                sequenceRoutine = StartCoroutine(SuccessSequence());
            }
            else
            {
                capitalSignEntered = false;
                SetTranslation(baseLetter);
                SetResult("Incorrect");
                ShowCapitalSignStep(wrongLetterLine);
                CurrentState = LessonState.WaitingForCapitalSign;
            }
        }
    }

    private IEnumerator SuccessSequence()
    {
        ShowLine(successLine);
        yield return WaitForAudio(successLine.audioClip);

        ShowLine(completedLine);
        yield return WaitForAudio(completedLine.audioClip);

        ShowLine(replayQuestionLine);
        yield return WaitForAudio(replayQuestionLine.audioClip);

        CurrentState = LessonState.WaitingForReplayChoice;
    }

    public void NextOrConfirmYes()
    {
        if (CurrentState == LessonState.WaitingForReplayChoice)
            RestartLesson();
    }

    public void GoBackOrRestartPrompt()
    {
        if (CurrentState == LessonState.WaitingForLetterInput)
        {
            capitalSignEntered = false;
            SetTranslation(baseLetter);
            SetResult("");
            ShowCapitalSignStep();
            CurrentState = LessonState.WaitingForCapitalSign;
        }
        else if (CurrentState == LessonState.WaitingForCapitalSign)
        {
            ShowCapitalSignStep();
        }
    }

    public void RepeatCurrent()
    {
        switch (CurrentState)
        {
            case LessonState.Intro:
                ShowLine(welcomeLine);
                break;

            case LessonState.Teaching:
            case LessonState.WaitingForCapitalSign:
                ShowCapitalSignStep();
                break;

            case LessonState.WaitingForLetterInput:
                ShowLetterStep();
                break;

            case LessonState.Success:
                SetLessonImage(letterABrailleImage);
                ShowLine(successLine);
                break;

            case LessonState.WaitingForReplayChoice:
                SetLessonImage(letterABrailleImage);
                ShowLine(replayQuestionLine);
                break;

            case LessonState.Ended:
                ShowLine(endLine);
                break;
        }
    }

    public void NoOrEndLesson()
    {
        if (CurrentState != LessonState.WaitingForReplayChoice)
            return;

        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(EndLessonRoutine());
    }

    private IEnumerator EndLessonRoutine()
    {
        CurrentState = LessonState.Ended;
        ShowLine(endLine);
        yield return WaitForAudio(endLine.audioClip);
    }

    private void RestartLesson()
    {
        StopRunningRoutine();
        capitalSignEntered = false;
        SetTranslation(baseLetter);
        SetResult("");
        StartLesson();
    }

    private void ShowCapitalSignStep()
    {
        ShowCapitalSignStep(promptCapitalSignLine);
    }

    private void ShowCapitalSignStep(LessonLine line)
    {
        SetLessonImage(capitalizationImage);
        ShowLine(line);
    }

    private void ShowLetterStep()
    {
        SetLessonImage(letterABrailleImage);
        ShowLine(promptLetterLine);
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
}