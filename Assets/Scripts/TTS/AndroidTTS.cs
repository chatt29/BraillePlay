using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using System;
#endif

public class AndroidTTS : MonoBehaviour, ITTS
{
#if UNITY_ANDROID && !UNITY_EDITOR

    private AndroidJavaObject textToSpeech;
    private AndroidJavaObject activity;

#endif

    private bool initialized = false;
    private bool speaking = false;

    public bool IsSpeaking => speaking;

    public void Initialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        try
        {
            AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            textToSpeech = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                activity,
                new TTSInitListener(this)
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Android TTS initialization failed:\n" + e);
        }

#else

        initialized = true;

#endif
    }

    public void Speak(string message)
    {
        if (!initialized)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR

        if (textToSpeech == null)
            return;

        speaking = true;

        textToSpeech.Call<int>(
            "speak",
            message,
            0,
            null,
            "BraillePlaySpeech"
        );

#else

        Debug.Log("[AndroidTTS] " + message);

#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (textToSpeech != null)
        {
            textToSpeech.Call("stop");
        }

#endif

        speaking = false;
    }

    public void Shutdown()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (textToSpeech != null)
        {
            textToSpeech.Call("stop");
            textToSpeech.Call("shutdown");

            textToSpeech.Dispose();
            textToSpeech = null;
        }

#endif

        speaking = false;
    }

    public void SetRate(float rate)
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (textToSpeech != null)
            textToSpeech.Call<int>("setSpeechRate", rate);

#endif
    }

    public void SetPitch(float pitch)
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (textToSpeech != null)
            textToSpeech.Call<int>("setPitch", pitch);

#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR

    public void OnInitialized()
    {
        initialized = true;

        if (textToSpeech != null)
        {
            AndroidJavaClass locale =
                new AndroidJavaClass("java.util.Locale");

            textToSpeech.Call<int>(
                "setLanguage",
                locale.GetStatic<AndroidJavaObject>("US")
            );
        }

        Debug.Log("Android TTS Ready");
    }

    private class TTSInitListener : AndroidJavaProxy
    {
        private AndroidTTS owner;

        public TTSInitListener(AndroidTTS owner)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.owner = owner;
        }

        public void onInit(int status)
        {
            if (status == 0)
            {
                owner.OnInitialized();
            }
            else
            {
                Debug.LogError("Android TTS failed to initialize.");
            }
        }
    }

#endif
}