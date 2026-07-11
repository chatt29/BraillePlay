using UnityEngine;

/// <summary>
/// Drop one of these in every scene (Login, Game Menu, each Guide, each
/// Quiz, etc.) to give a blind student their bearings the moment the scene
/// loads: what scene they're in, then the full set of controls for it.
/// Speaks once, automatically, on Start - no wiring into each scene's own
/// gameplay script required.
///
/// This does NOT replace scene-specific content (a quiz's own
/// "Welcome to Braille Sounds Around!" line, GameMenu's "Welcome, name"
/// line, etc.) - those still matter for tone/content. This only guarantees
/// every scene states its own name and controls up front, consistently,
/// even if the scene's own script forgets to.
/// </summary>
public class SceneAnnouncer : MonoBehaviour
{
    [Tooltip("Spoken scene name, e.g. \"Game Menu\", \"Lesson 3 Quiz: Instrument Sounds\", \"Login\".")]
    [SerializeField] private string sceneName;

    [Tooltip("Full spoken instructions for how to use THIS scene - every key that does something here. Keep it complete; this is the student's only orientation.")]
    [TextArea(3, 8)]
    [SerializeField] private string instructions;

    [Tooltip("Seconds to wait before speaking, in case other Start() logic in the scene (e.g. Firebase/progress loading) needs a frame first.")]
    [SerializeField] private float delaySeconds = 0.1f;

    private void Start()
    {
        Invoke(nameof(Announce), delaySeconds);
    }

    private void Announce()
    {
        if (AccessibilityManager.Instance == null)
        {
            Debug.LogWarning("[SceneAnnouncer] No AccessibilityManager in this scene - orientation won't be spoken.");
            return;
        }

        string message = string.IsNullOrEmpty(instructions)
            ? "You are now in " + sceneName + "."
            : "You are now in " + sceneName + ". " + instructions;

        AccessibilityManager.Instance.Announce(message);
    }
}