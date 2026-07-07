using System.Collections;
using UnityEngine;

/// <summary>
/// Thin, reusable layer on top of TTSManager for spoken UI flows: a plain
/// fire-and-forget Announce(), plus AnnounceAndWait() so sequential lines
/// (greeting, then instructions, then the first field) don't overlap.
///
/// This does not replace or duplicate TTSManager - per the project README,
/// TTSManager.Instance.Speak(...) remains the only thing that actually talks
/// to the platform TTS. This class only sequences calls to it so every
/// script that needs "say A, wait, then say B" doesn't reimplement the same
/// polling loop.
/// </summary>
public class AccessibilityManager : MonoBehaviour
{
    public static AccessibilityManager Instance { get; private set; }

    [Tooltip("Off by default to match this project's per-scene setup (no DontDestroyOnLoad), so every scene gets its own manager instead of a stale one carried over from the last scene.")]
    public bool dontDestroyOnLoad = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Speaks a message without waiting for it to finish.</summary>
    public void Announce(string message)
    {
        TTSManager.Instance.Speak(message);
    }

    /// <summary>
    /// Speaks a message and waits for it to finish before the calling
    /// coroutine continues. Uses short polling with a timeout rather than
    /// waiting forever, since TTSManager.IsSpeaking never becomes true on
    /// non-Windows editor platforms (WindowsTTS only sets that flag under
    /// the Windows build flags) - without the timeout this would hang
    /// forever when testing in the editor on Mac/Linux.
    /// </summary>
    public IEnumerator AnnounceAndWait(string message, float maxSpeakSeconds = 8f)
    {
        Announce(message);

        float t = 0f;
        while (!TTSManager.Instance.IsSpeaking && t < 0.5f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (TTSManager.Instance.IsSpeaking && t < maxSpeakSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }
}