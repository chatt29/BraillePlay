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
///
/// Also actually pauses the scene: Time.timeScale alone doesn't stop an
/// AudioSource from playing, so without this the quiz's own narration kept
/// talking over the confirm prompt - useless for a blind student who can't
/// fall back to reading the overlay. gameplayAudioSource gets Pause()'d (not
/// stopped) so Cancel can resume the exact same clip from where it left off.
/// </summary>
public class QuizBackHandler : MonoBehaviour
{
    [SerializeField] private QuizResultReporter resultReporter;
    [SerializeField] private GameObject confirmOverlay;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private string confirmMessage = "Quit lesson? Press Space for yes. Press Backspace for no.";

    [Tooltip("The quiz's own narration/voice AudioSource (same one the quiz script plays welcome/question/score clips through). Paused while the confirm prompt is up, resumed on Cancel.")]
    [SerializeField] private AudioSource gameplayAudioSource;

    private bool awaitingConfirmation;
    private float savedTimeScale = 1f;

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

        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (gameplayAudioSource != null && gameplayAudioSource.isPlaying)
            gameplayAudioSource.Pause();

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

        // Scene is unloading - restore timeScale so the next scene doesn't
        // silently start out paused.
        Time.timeScale = savedTimeScale;

        resultReporter.QuitWithoutReporting();
    }

    private void HandleCancel()
    {
        StopListeningForConfirmation();
        if (confirmOverlay != null) confirmOverlay.SetActive(false);

        Time.timeScale = savedTimeScale;

        if (gameplayAudioSource != null)
            gameplayAudioSource.UnPause();

        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.Announce("Resumed.");
    }

    private void StopListeningForConfirmation()
    {
        if (!awaitingConfirmation) return;

        awaitingConfirmation = false;
        BrailleMapping.OnYesOrNext -= HandleConfirm;
        BrailleMapping.OnDeleteOrNo -= HandleCancel;
    }
}