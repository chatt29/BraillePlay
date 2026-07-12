using UnityEngine;

/// <summary>
/// Quiz scenes with embedded voice-clip lessons (e.g. BrailleSoundsAround1)
/// narrate everything themselves via recorded AudioClips, not TTS. But
/// GameMenu keeps a persistent TTSManager alive under DontDestroyOnLoad, so
/// without this boundary it survives the scene change and keeps talking
/// (e.g. announcing the button label you just pressed) right on top of the
/// lesson's own welcome audio.
///
/// This destroys that incoming persistent TTSManager as soon as the quiz
/// scene wakes up, before it gets a chance to speak here. The scene-local
/// TTSManager (the one on QuizFlowBridge, with Dont Destroy On Load
/// unchecked) then becomes the singleton instead, and stays around purely
/// for QuizEndMenu / QuizBackHandler's spoken prompts later on.
///
/// SETUP: attach this to the QuizFlowBridge GameObject, and drag it ABOVE
/// the TTS Manager component in the Inspector. Unity runs Awake() for
/// components on the same GameObject top-to-bottom in Inspector order, so
/// this must claim/clear TTSManager.Instance before the local TTSManager's
/// own Awake() runs its singleton check - otherwise the local one sees the
/// old instance still present and destroys itself instead.
/// </summary>
public class TTSBoundary : MonoBehaviour
{
    private void Awake()
    {
        TTSManager incoming = TTSManager.Instance;

        // A persistent TTSManager lives in the special DontDestroyOnLoad
        // scene, not in this scene - that's how we tell "carried over from
        // GameMenu" apart from "already the local one on this GameObject".
        if (incoming != null && incoming.gameObject.scene != gameObject.scene)
        {
            incoming.StopSpeaking();
            Destroy(incoming.gameObject);
        }
    }
}