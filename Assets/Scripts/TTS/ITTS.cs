public interface ITTS
{
    bool IsSpeaking { get; }

    void Initialize();

    void Speak(string message);

    void Stop();

    void Shutdown();

    void SetRate(float rate);

    void SetPitch(float pitch);
}