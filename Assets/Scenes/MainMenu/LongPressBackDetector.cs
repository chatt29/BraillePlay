using System;
using UnityEngine;

/// <summary>
/// Global "hold Backspace to go back" gesture, as a singleton so any script
/// in the scene (QuizBackHandler, SceneBackButton, etc.) can subscribe via
/// LongPressBackDetector.Instance without needing a direct Inspector
/// reference - matching the BrailleMapping/AccessibilityManager singleton
/// pattern already used elsewhere in this project.
///
/// Only measures the hold and fires OnLongPressBack once it completes. It
/// never decides what "going back" means - that's left to listeners.
/// </summary>
public class LongPressBackDetector : MonoBehaviour
{
    public static LongPressBackDetector Instance { get; private set; }

    [Tooltip("How many seconds Backspace must be held down before OnLongPressBack fires.")]
    [SerializeField] private float holdSeconds = 3f;

    [Tooltip("While true, holding Backspace is ignored entirely (e.g. set this while a confirm overlay that gives Backspace its own meaning, like Cancel, is open).")]
    public bool suppressed = false;

    /// <summary>Fires once per press-and-hold cycle, the moment the hold reaches holdSeconds.</summary>
    public event Action OnLongPressBack;

    private float heldTime;
    private bool alreadyTriggeredThisPress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        KeyCode backKey = BrailleMapping.Instance != null ? BrailleMapping.Instance.deleteOrNoKey : KeyCode.Backspace;

        if (suppressed || !Input.GetKey(backKey))
        {
            heldTime = 0f;
            alreadyTriggeredThisPress = false;
            return;
        }

        heldTime += Time.deltaTime;

        if (!alreadyTriggeredThisPress && heldTime >= holdSeconds)
        {
            alreadyTriggeredThisPress = true;
            OnLongPressBack?.Invoke();
        }
    }

    /// <summary>Lets a listener re-arm the detector without requiring the key to be released first.</summary>
    public void ResetHold()
    {
        heldTime = 0f;
        alreadyTriggeredThisPress = false;
    }
}