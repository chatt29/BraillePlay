using UnityEngine;
using TMPro;

/// <summary>
/// Lets the player quit a quiz scene at any time via the global 3-second
/// long-press-back gesture, with a confirmation step so an accidental hold
/// doesn't throw away progress. Uses the same Space = yes / Backspace = no
/// convention as the rest of the app (quiz-end continue prompt, Game Menu
/// logout prompt). Speaks the prompt via AccessibilityManager - this scene
/// already has a local TTSManager for QuizEndMenu, so there's no reason for
/// this confirmation to be visual-only.
/// </summary>
public class QuizBackHandler : MonoBehaviour
{
    [SerializeField] private QuizResultReporter resultReporter;
    [SerializeField] private GameObject confirmOverlay;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private string confirmMessage = "Quit lesson? Press Space for yes. Press Backspace for no.";

    private bool awaitingConfirmation;

    private void Awake()
    {
        if (confirmOverlay != null)
            confirmOverlay.SetActive(false);
    }

    private void OnEnable()
    {
        if (LongPressBackDetector.Instance != null)
            LongPressBackDetector.Instance.OnLongPressBack += HandleLongPressBack;
    }

    private void OnDisable()
    {
        if (LongPressBackDetector.Instance != null)
            LongPressBackDetector.Instance.OnLongPressBack -= HandleLongPressBack;

        StopListeningForConfirmation();
    }

    private void HandleLongPressBack()
    {
        if (awaitingConfirmation) return;

        awaitingConfirmation = true;

        if (confirmText != null) confirmText.text = confirmMessage;
        if (confirmOverlay != null) confirmOverlay.SetActive(true);

        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.Announce(confirmMessage);

        BrailleMapping.OnYesOrNext += HandleConfirm;
        BrailleMapping.OnDeleteOrNo += HandleCancel;
    }

    private void HandleConfirm()
    {
        StopListeningForConfirmation();
        if (confirmOverlay != null) confirmOverlay.SetActive(false);

        resultReporter.QuitWithoutReporting();
    }

    private void HandleCancel()
    {
        StopListeningForConfirmation();
        if (confirmOverlay != null) confirmOverlay.SetActive(false);
    }

    private void StopListeningForConfirmation()
    {
        if (!awaitingConfirmation) return;

        awaitingConfirmation = false;
        BrailleMapping.OnYesOrNext -= HandleConfirm;
        BrailleMapping.OnDeleteOrNo -= HandleCancel;
    }
}