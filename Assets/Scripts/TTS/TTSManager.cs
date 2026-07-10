using UnityEngine;
using System;
using System.Collections;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    /// <summary>Fires with the exact text every time Speak() is called - lets any scene's speech-bubble UI react without TTSManager knowing UI exists.</summary>
    public event Action<string> OnSpeak;

    [Header("Settings")]
    [Range(0.5f, 2f)]
    public float speechRate = 1f;
    [Range(0.5f, 2f)]
    public float pitch = 1f;

    [Tooltip("On by default so speech survives scene transitions instead of getting cut off mid-utterance when the scene that created it unloads.")]
    public bool dontDestroyOnLoad = false;

    public bool debugLogs = true;

    private ITTS platformTTS;
    private Coroutine speakCoroutine;
    private bool initialized = false;

    public bool IsInitialized => initialized;

    public bool IsSpeaking
    {
        get
        {
            if (platformTTS == null)
                return false;
            return platformTTS.IsSpeaking;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            if (transform.parent != null)
            {
                Debug.LogWarning("[TTSManager] Had a parent, which breaks DontDestroyOnLoad. Detaching to scene root.");
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (platformTTS != null)
            platformTTS.Shutdown();
    }

    private void Initialize()
    {
        if (initialized)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        platformTTS = gameObject.AddComponent<AndroidTTS>();
#else
        platformTTS = gameObject.AddComponent<WindowsTTS>();
#endif
        platformTTS.Initialize();
        platformTTS.SetRate(speechRate);
        platformTTS.SetPitch(pitch);
        initialized = true;

        if (debugLogs)
            Debug.Log("[TTS] Initialized");
    }

    public void Speak(string message)
    {
        Speak(message, true);
    }

    public void Speak(string message, bool interrupt)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!initialized)
            Initialize();

        if (interrupt)
            StopSpeaking();

        if (speakCoroutine != null)
            StopCoroutine(speakCoroutine);

        speakCoroutine = StartCoroutine(SpeakRoutine(message));
    }

    IEnumerator SpeakRoutine(string message)
    {
        yield return null;

        if (debugLogs)
            Debug.Log("[TTS] " + message);

        OnSpeak?.Invoke(message);
        platformTTS.Speak(message);
    }

    public void StopSpeaking()
    {
        if (!initialized)
            return;

        if (speakCoroutine != null)
        {
            StopCoroutine(speakCoroutine);
            speakCoroutine = null;
        }

        platformTTS.Stop();
    }

    public void SetSpeechRate(float rate)
    {
        speechRate = Mathf.Clamp(rate, 0.5f, 2f);
        if (platformTTS != null)
            platformTTS.SetRate(speechRate);
    }

    public void SetPitch(float value)
    {
        pitch = Mathf.Clamp(value, 0.5f, 2f);
        if (platformTTS != null)
            platformTTS.SetPitch(pitch);
    }
}