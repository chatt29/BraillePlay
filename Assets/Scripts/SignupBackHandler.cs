using UnityEngine;
using UnityEngine.SceneManagement;

public class SignupBackHandler : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [Tooltip("What gets spoken - can differ from the actual scene name above.")]
    [SerializeField] private string mainMenuSpokenName = "the main menu";

    private void OnEnable()
    {
        if (LongPressBackDetector.Instance == null)
        {
            Debug.LogError("[SignupBackHandler] LongPressBackDetector.Instance is null - it hasn't been created yet in this scene run.");
            return;
        }

        LongPressBackDetector.Instance.OnLongPressBack += GoBack;
    }

    private void OnDisable()
    {
        if (LongPressBackDetector.Instance != null)
            LongPressBackDetector.Instance.OnLongPressBack -= GoBack;
    }

    private void GoBack()
    {
        Debug.Log("[SignupBackHandler] Going back to main menu.");

        if (AccessibilityManager.Instance != null)
            AccessibilityManager.Instance.Announce("Back button pressed. Returning to " + mainMenuSpokenName + ".");

        SceneManager.LoadScene(mainMenuSceneName);
    }
}