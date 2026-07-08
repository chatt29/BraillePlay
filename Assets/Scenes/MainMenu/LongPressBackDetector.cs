using System;
using UnityEngine;

/// <summary>
/// Detects a 3-second hold of Backspace and raises OnLongPressBack once per
/// hold (re-arms only after the key is released). Cross-scene: lives as a
/// persistent singleton, added once in the bootstrap scene alongside the
/// other DontDestroyOnLoad managers. Detection only - never decides what
/// "back" means, since that's different per scene.
/// </summary>
public class LongPressBackDetector : MonoBehaviour
{
    public static LongPressBackDetector Instance { get; private set; }

    public event Action OnLongPressBack;

    [SerializeField] private KeyCode backKey = KeyCode.Backspace;
    [SerializeField] private float holdSeconds = 3f;

    private float heldTime;
    private bool firedThisHold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad only works on a root GameObject - detach if it
        // was accidentally nested under Canvas or anything else.
        if (transform.parent != null)
        {
            Debug.LogWarning("[LongPressBackDetector] Had a parent, which breaks DontDestroyOnLoad. Detaching to scene root.");
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKey(backKey))
        {
            heldTime += Time.deltaTime;

            if (!firedThisHold && heldTime >= holdSeconds)
            {
                firedThisHold = true;
                Debug.Log("[LongPressBackDetector] Long press detected - firing OnLongPressBack.");
                OnLongPressBack?.Invoke();
            }
        }
        else
        {
            heldTime = 0f;
            firedThisHold = false;
        }
    }
}