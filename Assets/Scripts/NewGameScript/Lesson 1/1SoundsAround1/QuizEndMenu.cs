using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Shown once a quiz finishes: announces the score via TTS and on-screen
/// text, then waits for the student to choose Repeat / Next Quiz / Back to
/// Menu using the same global braille commands as the rest of the app
/// (R = repeat, Y = next/yes, Back = return to menu) so there's nothing new
/// to learn. Reuses BrailleMapping.OnRepeat/OnYesOrNext/OnBack rather than
/// inventing new input - BrailleSoundsAround1's own handlers for those
/// events are already inert by the time this shows (lessonActive is false),
/// so there's no double-handling.
///
/// Requires a TTSManager to exist in THIS scene specifically (add one here
/// with Dont Destroy On Load unchecked) since quiz scenes normally destroy
/// the persistent TTSManager via TTSBoundary for embedded-voice-clip lessons.
/// </summary>
public class QuizEndMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    private Action onRepeat;
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

    public void Show(int scorePercent, bool hasNextQuiz, Action onRepeat, Action onNextQuiz, Action onBackToMenu)
    {
        this.hasNextQuiz = hasNextQuiz;
        this.onRepeat = onRepeat;
        this.onNextQuiz = onNextQuiz;
        this.onBackToMenu = onBackToMenu;

        string message = hasNextQuiz
            ? $"Quiz complete. Your score is {scorePercent} percent. Press R to repeat this quiz, Y to continue to the next quiz, or Back to return to the menu."
            : $"Quiz complete. Your score is {scorePercent} percent. Press R to repeat this quiz, or Back to return to the menu.";

        if (messageText != null)
            messageText.text = message;

        if (panel != null)
            panel.SetActive(true);

        if (TTSManager.Instance != null)
            TTSManager.Instance.Speak(message);
        else
            Debug.LogWarning("[QuizEndMenu] No TTSManager found in this scene - the choice won't be spoken, only shown as text.");

        awaitingChoice = true;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnBack += HandleBack;

        if (hasNextQuiz)
            BrailleMapping.OnYesOrNext += HandleNextQuiz;
    }

    private void HandleRepeat()
    {
        if (!awaitingChoice) return;
        StopListening();
        onRepeat?.Invoke();
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
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnBack -= HandleBack;

        if (hasNextQuiz)
            BrailleMapping.OnYesOrNext -= HandleNextQuiz;
    }
}