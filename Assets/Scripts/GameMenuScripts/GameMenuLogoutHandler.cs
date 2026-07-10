using UnityEngine;
using UnityEngine.SceneManagement;
using BraillePlay.GameMenu;

public class GameMenuLogoutHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("What gets spoken - can differ from the actual scene name above.")]
    [SerializeField] private string mainMenuSpokenName = "the main menu";
    [SerializeField] private GameMenuNavigator navigator;

    private bool awaitingConfirmation;

    private void OnEnable()
    {
        if (LongPressBackDetector.Instance == null)
        {
            Debug.LogError("[GameMenuLogoutHandler] LongPressBackDetector.Instance is null - it hasn't been created yet in this scene run.");
            return;
        }

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
        navigator.InputLocked = true;

        Debug.Log("[GameMenuLogoutHandler] Asking for logout confirmation.");
        AccessibilityManager.Instance.Announce("Log out? Press Enter to confirm. Press Escape to cancel.");

        BrailleMapping.OnSubmit += HandleConfirm;
        BrailleMapping.OnBack += HandleCancel;
    }

    private void HandleConfirm()
    {
        StopListeningForConfirmation();

        AccessibilityManager.Instance.Announce("Logging out. Back button pressed. Returning to " + mainMenuSpokenName + ".");

        UserSession.Clear();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandleCancel()
    {
        StopListeningForConfirmation();
        AccessibilityManager.Instance.Announce("Log out cancelled.");
        navigator.InputLocked = false;
    }

    private void StopListeningForConfirmation()
    {
        if (!awaitingConfirmation) return;

        awaitingConfirmation = false;
        BrailleMapping.OnSubmit -= HandleConfirm;
        BrailleMapping.OnBack -= HandleCancel;
    }
}