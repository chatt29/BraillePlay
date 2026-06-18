using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UniversalBackButtonHandler : MonoBehaviour
{
    [Header("Hold Settings")]
    [SerializeField] private float holdTime = 1.5f;

    [Header("Optional Haptics")]
    [SerializeField] private GameObject wirelessHapticsObject;

    private static UniversalBackButtonHandler instance;
    private static Stack<string> sceneHistory = new Stack<string>();

    private float holdTimer = 0f;
    private bool triggered = false;
    private string currentSceneName;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != currentSceneName)
        {
            sceneHistory.Push(currentSceneName);
            currentSceneName = scene.name;
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Backspace))
        {
            holdTimer += Time.unscaledDeltaTime;

            if (!triggered && holdTimer >= holdTime)
            {
                triggered = true;
                GoBack();
            }
        }
        else
        {
            holdTimer = 0f;
            triggered = false;
        }
    }

    private void GoBack()
    {
        if (sceneHistory.Count == 0)
            return;

        TriggerHaptic();

        string previousScene = sceneHistory.Pop();
        SceneManager.LoadScene(previousScene);
    }

    private void TriggerHaptic()
    {
        if (wirelessHapticsObject != null)
        {
            wirelessHapticsObject.SendMessage(
                "TriggerHaptic",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}