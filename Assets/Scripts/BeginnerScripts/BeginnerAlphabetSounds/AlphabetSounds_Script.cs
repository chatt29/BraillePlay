using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlphabetSounds_Script : MonoBehaviour
{
    [Serializable]
    public class AlphabetEntry
    {
        [Header("Letter Data")]
        public string letter;
        public string objectName;
        [TextArea(2, 4)] public string bubbleMessage;
        public Sprite image;
        public AudioClip audioClip;
    }

    public enum LessonState
    {
        Intro,
        WaitingToStart,
        PlayingLetter,
        WaitingAfterLetterChoice,
        WaitingForReplayChoice,
        Ended
    }

    [Header("UI References")]
    public TMP_Text bubbleText;
    public TMP_Text letterText;
    public TMP_Text objectNameText;
    public Image objectImage;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Intro Sequence")]
    [TextArea(2, 4)]
    public string welcomeMessage = "Welcome to Alphabet Sounds!";
    public AudioClip welcomeAudio;

    [TextArea(2, 4)]
    public string instructionMessage = "Let's learn the individual sounds of each letter in the alphabet.";
    public AudioClip instructionAudio;

    [Header("Per Letter Prompt")]
    [TextArea(2, 4)]
    public string nextOrRepeatMessage = "Press Yes for the next letter, or press Repeat to hear the current letter again.";
    public AudioClip nextOrRepeatAudio;

    [Header("Completion Sequence")]
    [TextArea(2, 4)]
    public string completedMessage = "Great job! You finished the alphabet sounds.";
    public AudioClip completedAudio;

    [TextArea(2, 4)]
    public string replayQuestionMessage = "Do you want to repeat the alphabet sounds starting from A? Press Yes for repeat or No to end.";
    public AudioClip replayQuestionAudio;

    [TextArea(2, 4)]
    public string endMessage = "Okay! Ending the alphabet sounds lesson. Great job!";
    public AudioClip endAudio;

    [Header("Alphabet Content")]
    public List<AlphabetEntry> alphabetEntries = new List<AlphabetEntry>();

    [Header("Options")]
    public bool playOnStart = true;
    public bool loopAroundLetters = false;
    public float extraWaitAfterAudio = 0.15f;

    private int currentIndex = 0;
    private Coroutine sequenceRoutine;

    public LessonState CurrentState { get; private set; } = LessonState.Intro;
    public int CurrentIndex => currentIndex;

    private void Start()
    {
        if (objectImage != null)
        {
            objectImage.sprite = null;
            objectImage.enabled = false;
        }

        if (playOnStart)
        {
            StartLesson();
        }
    }

    public void StartLesson()
    {
        if (alphabetEntries == null || alphabetEntries.Count == 0)
        {
            Debug.LogWarning("AlphabetSounds_Script: No alphabet entries assigned.");
            return;
        }

        StopRunningRoutine();
        sequenceRoutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        CurrentState = LessonState.Intro;

        SetBubbleOnly(welcomeMessage);
        ClearLetterFields();
        HideObjectImage();
        PlayAudio(welcomeAudio);
        yield return WaitForAudio(welcomeAudio);

        SetBubbleOnly(instructionMessage);
        HideObjectImage();
        PlayAudio(instructionAudio);
        yield return WaitForAudio(instructionAudio);

        currentIndex = 0;
        ShowCurrentEntry(true);
    }

    public void ShowCurrentEntry(bool playAudio = true)
    {
        if (alphabetEntries == null || alphabetEntries.Count == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, alphabetEntries.Count - 1);
        AlphabetEntry entry = alphabetEntries[currentIndex];

        if (bubbleText != null)
            bubbleText.text = entry.bubbleMessage;

        if (letterText != null)
            letterText.text = entry.letter;

        if (objectNameText != null)
            objectNameText.text = entry.objectName;

        if (objectImage != null)
        {
            objectImage.sprite = entry.image;
            objectImage.enabled = entry.image != null;
        }

        if (playAudio)
        {
            StopRunningRoutine();
            sequenceRoutine = StartCoroutine(PlayLetterThenAskChoice(entry.audioClip));
        }
    }

    private IEnumerator PlayLetterThenAskChoice(AudioClip letterClip)
    {
        CurrentState = LessonState.PlayingLetter;

        PlayAudio(letterClip);
        yield return WaitForAudio(letterClip);

        if (CurrentState == LessonState.Ended)
            yield break;

        SetBubbleOnly(nextOrRepeatMessage);

        // The player can now press Yes or Repeat immediately,
        // even while the prompt audio is still playing.
        CurrentState = LessonState.WaitingAfterLetterChoice;

        PlayAudio(nextOrRepeatAudio);
        yield return WaitForAudio(nextOrRepeatAudio);
    }

    public void NextLetterOrConfirmYes()
    {
        if (alphabetEntries == null || alphabetEntries.Count == 0)
            return;

        if (CurrentState == LessonState.WaitingToStart)
        {
            currentIndex = 0;
            ShowCurrentEntry(true);
            return;
        }

        if (CurrentState == LessonState.WaitingAfterLetterChoice)
        {
            if (currentIndex < alphabetEntries.Count - 1)
            {
                currentIndex++;
                ShowCurrentEntry(true);
            }
            else
            {
                StopRunningRoutine();
                sequenceRoutine = StartCoroutine(FinishAndAskReplay());
            }

            return;
        }

        if (CurrentState == LessonState.WaitingForReplayChoice)
        {
            RestartFromBeginning();
            return;
        }
    }

    public void PreviousLetter()
    {
        if (CurrentState != LessonState.WaitingAfterLetterChoice &&
            CurrentState != LessonState.PlayingLetter)
            return;

        if (currentIndex > 0)
        {
            currentIndex--;
        }
        else if (loopAroundLetters)
        {
            currentIndex = alphabetEntries.Count - 1;
        }
        else
        {
            currentIndex = 0;
        }

        ShowCurrentEntry(true);
    }

    public void RepeatCurrent()
    {
        if (CurrentState == LessonState.WaitingAfterLetterChoice ||
            CurrentState == LessonState.PlayingLetter)
        {
            ShowCurrentEntry(true);
        }
        else if (CurrentState == LessonState.WaitingForReplayChoice)
        {
            SetBubbleOnly(replayQuestionMessage);
            PlayAudio(replayQuestionAudio);
        }
        else if (CurrentState == LessonState.Ended)
        {
            SetBubbleOnly(endMessage);
            PlayAudio(endAudio);
        }
    }

    public void NoOrEndLesson()
    {
        if (CurrentState == LessonState.WaitingForReplayChoice)
        {
            StopRunningRoutine();
            sequenceRoutine = StartCoroutine(EndLessonRoutine());
        }
    }

    private IEnumerator FinishAndAskReplay()
    {
        CurrentState = LessonState.Intro;

        SetBubbleOnly(completedMessage);
        HideObjectImage();
        ClearLetterFields();
        PlayAudio(completedAudio);
        yield return WaitForAudio(completedAudio);

        SetBubbleOnly(replayQuestionMessage);
        PlayAudio(replayQuestionAudio);
        yield return WaitForAudio(replayQuestionAudio);

        CurrentState = LessonState.WaitingForReplayChoice;
    }

    private IEnumerator EndLessonRoutine()
    {
        CurrentState = LessonState.Ended;

        SetBubbleOnly(endMessage);
        HideObjectImage();
        ClearLetterFields();
        PlayAudio(endAudio);
        yield return WaitForAudio(endAudio);

        Debug.Log("Alphabet sounds lesson ended.");
    }

    private void RestartFromBeginning()
    {
        StopRunningRoutine();
        currentIndex = 0;
        ShowCurrentEntry(true);
    }

    private void SetBubbleOnly(string message)
    {
        if (bubbleText != null)
            bubbleText.text = message;
    }

    private void ClearLetterFields()
    {
        if (letterText != null)
            letterText.text = "";

        if (objectNameText != null)
            objectNameText.text = "";
    }

    private void HideObjectImage()
    {
        if (objectImage != null)
        {
            objectImage.sprite = null;
            objectImage.enabled = false;
        }
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