#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

public class AndroidTTS : ITTS
{
    private AndroidJavaObject tts;
    private bool ready;
    public bool IsSpeaking { get; private set; }

    public void Initialize()
    {
        var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity=player.GetStatic<AndroidJavaObject>("currentActivity");
        tts=new AndroidJavaObject("android.speech.tts.TextToSpeech",activity,new InitListener(this));
    }

    public void Speak(string text)
    {
        if(!ready||string.IsNullOrWhiteSpace(text)) return;
        Stop();
        IsSpeaking=true;
        tts.Call<int>("speak",text,0,null,"BraillePlay");
    }

    public void Stop(){ if(tts!=null) tts.Call("stop"); IsSpeaking=false; }
    public void Shutdown(){ Stop(); if(tts!=null){ tts.Call("shutdown"); tts=null;} ready=false; }

    private class InitListener:AndroidJavaProxy{
        AndroidTTS p;
        public InitListener(AndroidTTS p):base("android.speech.tts.TextToSpeech$OnInitListener"){this.p=p;}
        public void onInit(int status){ if(status==0) p.ready=true; }
    }
}
#endif
