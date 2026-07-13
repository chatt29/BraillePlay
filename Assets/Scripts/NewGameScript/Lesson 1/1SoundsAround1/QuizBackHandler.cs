using UnityEngine;
using TMPro;

/// <summary>
/// Lets the player quit a quiz scene at any time via the global 3-second
/// long-press-back gesture, with a confirmation step so an accidental hold
/// doesn't throw away progress. This scene has its own local TTSManager
/// (kept alive by TTSBoundary specifically so QuizEndMenu and this handler
/// can speak) - so the prompt IS spoken via TTS, not visual-only.
/// </summary>
public class QuizBackHandler : MonoBehaviour
{
    [SerializeField] private QuizResultReporter resultReporter;
    [SerializeField] private GameObject confirmOverlay;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private string confirmMessage = "Quit lesson? Press Enter to confirm. Press Backspace to cancel.";

    private bool awaitingConfirmation;

    private void Awake()
    {
        if (confirmOverlay != null)
            confirmOverlay.SetActive(false);
    }

    private void OnEnable()
    {
        // FIX (Bug 2): Unity only guarantees all Awake() calls finish before
        // any Start() - it does NOT guarantee Awake() order between unrelated
        // scripts. Previously this subscribed directly here, so if
        // LongPressBackDetector.Awake() (which sets Instance) hadn't run yet
        // at the moment this OnEnable() ran, the null-check just silently
        // failed and never retried - the gesture was dead for the rest of the
        // scene. Polling in a coroutine makes it safe regardless of ordering,
        // and also makes it safe to re-enable this object later.
        StartCoroutine(SubscribeWhenReady());
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        while (LongPressBackDetector.Instance == null)
            yield return null;

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

        // Pause (not stop) whatever the quiz was narrating right away, so it
        // doesn't compete with the TTS prompt below - Pause keeps playback
        // position, so HandleCancel can resume from the same spot instead
        // of restarting the clip from zero.
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            source.Pause();

        if (TTSManager.Instance != null)
            TTSManager.Instance.Speak(confirmMessage);

        BrailleMapping.OnSubmit += HandleConfirm;
        BrailleMapping.OnBack += HandleCancel;
    }

    private void HandleConfirm()
    {
        StopListeningForConfirmation();
        if (confirmOverlay != null) confirmOverlay.SetActive(false);

        // Fully stop (not just pause) before leaving - there's no coming
        // back to this scene's audio, so no need to preserve position.
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            source.Stop();

        if (TTSManager.Instance != null)
            TTSManager.Instance.StopSpeaking();

        resultReporter.QuitWithoutReporting();
    }

    private void HandleCancel()
    {
        StopListeningForConfirmation();
        if (confirmOverlay != null) confirmOverlay.SetActive(false);

        // Resume the lesson audio from where it was paused, since the
        // student is staying in the quiz.
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            source.UnPause();
    }

    private void StopListeningForConfirmation()
    {
        if (!awaitingConfirmation) return;

        awaitingConfirmation = false;
        BrailleMapping.OnSubmit -= HandleConfirm;
        BrailleMapping.OnBack -= HandleCancel;
    }
}