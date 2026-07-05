public interface ITTS
{
    bool IsSpeaking { get; }
    void Initialize();
    void Speak(string text);
    void Stop();
    void Shutdown();
}
