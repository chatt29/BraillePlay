using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Shown once a quiz finishes: announces the score via TTS and on-screen
/// text, then asks "Would you like to continue to the next quiz?" - Space
/// (BrailleMapping.OnYesOrNext) to continue, Backspace
/// (BrailleMapping.OnDeleteOrNo) to return to the Game Menu. Same
/// yes/no convention used everywhere else in the app (e.g. the Game Menu
/// logout prompt), so there's nothing new to learn.
///
/// Each lesson now has only 1 quiz, so there's no "repeat this quiz" choice
/// here anymore - if the student wants to redo a quiz they re-enter it from
/// the Game Menu.
///
/// Requires a TTSManager to exist in THIS scene specifically (add one here
/// with Dont Destroy On Load unchecked) since quiz scenes normally destroy
/// the persistent TTSManager via TTSBoundary for embedded-voice-clip lessons.
///
/// Some quiz scenes (BrailleSoundsAround1 and friends) already speak the
/// score themselves with recorded audio clips before handing off here - so
/// this only re-speaks the score if told to. Pass announceScore: false when
/// the caller already said it, so the student doesn't hear it twice back to
/// back. The score is always shown as on-screen text either way.
/// </summary>
public class QuizEndMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    private Action onNextQuiz;
    private Action onBackToMenu;
    private bool hasNextQuiz;
    private bool awaitingChoice;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDisable()
    {
        StopListening();
    }

    public void Show(int scorePercent, bool hasNextQuiz, Action onNextQuiz, Action onBackToMenu, bool announceScore = true)
    {
        this.hasNextQuiz = hasNextQuiz;
        this.onNextQuiz = onNextQuiz;
        this.onBackToMenu = onBackToMenu;

        string scoreMessage = $"Quiz complete. Your score is {scorePercent} percent.";

        string choiceMessage = hasNextQuiz
            ? "Would you like to continue to the next quiz? Press Space for yes, or Backspace for no."
            : "That was the last quiz. Press Backspace to return to the menu.";

        string fullMessage = $"{scoreMessage} {choiceMessage}";

        // On-screen text always shows the score, regardless of whether it
        // gets spoken here - sighted users/parents watching still benefit.
        if (messageText != null)
            messageText.text = fullMessage;

        if (panel != null)
            panel.SetActive(true);

        if (TTSManager.Instance != null)
            TTSManager.Instance.Speak(announceScore ? fullMessage : choiceMessage);
        else
            Debug.LogWarning("[QuizEndMenu] No TTSManager found in this scene - the choice won't be spoken, only shown as text.");

        awaitingChoice = true;
        BrailleMapping.OnDeleteOrNo += HandleBack;

        if (hasNextQuiz)
            BrailleMapping.OnYesOrNext += HandleNextQuiz;
    }

    private void HandleNextQuiz()
    {
        if (!awaitingChoice) return;
        StopListening();
        onNextQuiz?.Invoke();
    }

    private void HandleBack()
    {
        if (!awaitingChoice) return;
        StopListening();
        onBackToMenu?.Invoke();
    }

    private void StopListening()
    {
        if (!awaitingChoice) return;

        awaitingChoice = false;
        BrailleMapping.OnDeleteOrNo -= HandleBack;

        if (hasNextQuiz)
            BrailleMapping.OnYesOrNext -= HandleNextQuiz;
    }
}