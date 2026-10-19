using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Audio-only main menu for blind players.
/// Flow: Greeting -> Instructions -> Menu (LOG-IN / SIGN-UP / GUEST).
///
/// Input comes from BrailleMapping's static events (no direct key reads):
///   OnUp / OnDown      : previous / next item (wraps around)
///   OnRight / OnSubmit : select current item (also skips the intro)
///   OnLeft / OnRepeat  : replay the instructions, then re-announce the item
///
/// Setup: add this to an empty GameObject with its OWN AudioSource (separate
/// from BrailleMapping's, so voice and button SFX don't cut each other off).
/// A BrailleMapping must exist in the scene. Assign clips and hook up each
/// item's On Select event (e.g. load a scene).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioMainMenu : MonoBehaviour
{
    [System.Serializable]
    public class MenuItem
    {
        [Tooltip("Name for your own reference, e.g. LOG-IN")]
        public string label;

        [Tooltip("Audio slot: played when this item is focused, e.g. \"Log in\"")]
        public AudioClip audioClip;

        [Tooltip("Optional: played right before On Select fires, e.g. \"Logging in\"")]
        public AudioClip selectedClip;

        [Tooltip("Scene to load when selected. Must be added in File > Build Settings.")]
        public string sceneName;

        [Tooltip("Optional extra actions that run just before the scene loads")]
        public UnityEvent onSelect;
    }

    [Header("Intro audio")]
    [SerializeField] private AudioClip greetingClip;
    [SerializeField] private AudioClip instructionsClip;

    [Header("Menu items (order = navigation order)")]
    [SerializeField]
    private MenuItem[] items = new MenuItem[3]
    {
        new MenuItem { label = "LOG-IN" },
        new MenuItem { label = "SIGN-UP" },
        new MenuItem { label = "GUEST" },
    };

    [Header("Optional feedback sounds")]
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip wrapSound;

    [Header("Behaviour")]
    [SerializeField] private bool wrapAround = true;
    [SerializeField] private bool allowSkipIntro = true;
    [Tooltip("Seconds of silence between greeting and instructions")]
    [SerializeField] private float pauseBetweenClips = 0.4f;

    private enum State { Intro, Menu, Busy }

    private AudioSource audioSource;
    private State state = State.Intro;
    private int index = -1;
    private Coroutine running;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnLeft += HandleReplay;
        BrailleMapping.OnRepeat += HandleReplay;
        BrailleMapping.OnRight += HandleSelect;
        BrailleMapping.OnSubmit += HandleSelect;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnLeft -= HandleReplay;
        BrailleMapping.OnRepeat -= HandleReplay;
        BrailleMapping.OnRight -= HandleSelect;
        BrailleMapping.OnSubmit -= HandleSelect;
    }

    private void Start()
    {
        running = StartCoroutine(IntroRoutine());
    }

    // ------------------------------------------------------------------
    // Input handlers (called by BrailleMapping events)
    // ------------------------------------------------------------------

    private void HandleUp() { if (state == State.Menu) Move(-1); }
    private void HandleDown() { if (state == State.Menu) Move(+1); }

    private void HandleReplay()
    {
        if (state == State.Menu) Replay();
    }

    private void HandleSelect()
    {
        if (state == State.Intro && allowSkipIntro) SkipIntro();
        else if (state == State.Menu) Select();
    }

    // ------------------------------------------------------------------
    // Flow
    // ------------------------------------------------------------------

    private IEnumerator IntroRoutine()
    {
        state = State.Intro;

        yield return PlayAndWait(greetingClip);
        if (pauseBetweenClips > 0f) yield return new WaitForSecondsRealtime(pauseBetweenClips);
        yield return PlayAndWait(instructionsClip);

        EnterMenu();
    }

    private void SkipIntro()
    {
        if (running != null) StopCoroutine(running);
        audioSource.Stop();
        EnterMenu();
    }

    private void EnterMenu()
    {
        state = State.Menu;
        index = 0;
        PlayItem(index);
    }

    private void Replay()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ReplayRoutine());
    }

    private IEnumerator ReplayRoutine()
    {
        state = State.Busy;
        yield return PlayAndWait(instructionsClip);
        state = State.Menu;
        PlayItem(index);
    }

    // ------------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------------

    private void Move(int direction)
    {
        if (items.Length == 0) return;

        int next = index + direction;
        bool wrapped = false;

        if (next < 0 || next >= items.Length)
        {
            if (!wrapAround)
            {
                PlayItem(index); // edge: just re-announce where they are
                return;
            }
            next = (next + items.Length) % items.Length;
            wrapped = true;
        }

        index = next;

        AudioClip feedback = wrapped && wrapSound != null ? wrapSound : moveSound;
        PlayItem(index);
        if (feedback != null) audioSource.PlayOneShot(feedback);
    }

    private void PlayItem(int i)
    {
        if (i < 0 || i >= items.Length) return;
        Play(items[i].audioClip);
    }

    // ------------------------------------------------------------------
    // Selection
    // ------------------------------------------------------------------

    private void Select()
    {
        if (index < 0 || index >= items.Length) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(SelectRoutine(items[index]));
    }

    private IEnumerator SelectRoutine(MenuItem item)
    {
        state = State.Busy; // ignore input while confirming / transitioning

        yield return PlayAndWait(item.selectedClip);

        item.onSelect?.Invoke();

        if (!string.IsNullOrEmpty(item.sceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(item.sceneName))
            {
                SceneManager.LoadScene(item.sceneName);
                yield break; // this object is destroyed with the old scene
            }

            Debug.LogError($"AudioMainMenu: scene '{item.sceneName}' is not in Build Settings " +
                           $"or the name is misspelled (item: {item.label}).");
        }

        // No scene loaded: hand control back to the player.
        yield return null;
        state = State.Menu;
    }

    // ------------------------------------------------------------------
    // Audio helpers
    // ------------------------------------------------------------------

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator PlayAndWait(AudioClip clip)
    {
        if (clip == null) yield break;
        Play(clip);
        yield return new WaitForSecondsRealtime(clip.length);
    }
}