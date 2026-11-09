using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One row in the Custom Lessons list.</summary>
[Serializable]
public class CustomLessonEntry
{
    [Tooltip("Firestore lesson id (filled in automatically when loading from Firestore).")]
    public string lessonId;

    [Tooltip("Shown on screen for sighted helpers and used in Console logs. Never spoken.")]
    public string title;

    [Tooltip("The teacher's recording of this lesson's title. This is what the student hears.")]
    public AudioClip titleClip;

    [Tooltip("Only for hand-made test entries. Leave empty for Firestore lessons - they use Quiz Scene Name.")]
    public string sceneName;
}

/// <summary>
/// Custom Lessons menu. One vertical list, no animations, NO TTS - every
/// word the student hears is a recorded clip.
///
///   Up / Down   -> previous / next lesson (plays that lesson's recorded title)
///   Enter       -> open the selected lesson
///   Backspace   -> go back
///   R (Repeat)  -> play the current lesson's title again
///
/// Lessons come from Firestore ("CustomLessons", published only) unless
/// Load From Firestore is off, in which case the Inspector list is used
/// (handy for testing without internet).
/// </summary>
public class CustomLessonMenuManager : MonoBehaviour
{
    [Header("Lessons")]
    [Tooltip("ON: load published lessons from Firestore. OFF: use the list below as-is (offline testing).")]
    public bool loadFromFirestore = true;
    public List<CustomLessonEntry> lessons = new List<CustomLessonEntry>();

    [Header("Recorded prompts")]
    [Tooltip("\"Custom lessons.\"")] public AudioClip introClip;
    [Tooltip("\"Loading lessons. Please wait.\"")] public AudioClip loadingClip;
    [Tooltip("\"Could not load lessons. Check your internet connection. Press Backspace to go back.\"")] public AudioClip loadErrorClip;
    [Tooltip("\"Press Up or Down to browse. Press Enter to open. Press Backspace to go back.\"")] public AudioClip instructionsClip;
    [Tooltip("\"This is the first lesson.\"")] public AudioClip firstLessonClip;
    [Tooltip("\"This is the last lesson.\"")] public AudioClip lastLessonClip;
    [Tooltip("\"There are no custom lessons yet. Press Backspace to go back.\"")] public AudioClip noLessonsClip;
    [Tooltip("\"Opening.\" (the lesson's title clip plays right after it)")] public AudioClip openingClip;
    [Tooltip("\"Going back.\"")] public AudioClip goingBackClip;
    [Tooltip("\"Sorry, this lesson is not available.\"")] public AudioClip notAvailableClip;
    [Tooltip("Fallback when a lesson has no usable title recording, e.g. \"Lesson without a title.\"")] public AudioClip missingTitleClip;

    [Header("Visuals (optional, for sighted troubleshooting)")]
    public TMP_Text currentTitleText;   // ListPlaceholderText
    public GameObject arrowUp;          // ArrowUp
    public GameObject arrowDown;        // ArrowDown

    [Header("Scene flow")]
    public string previousSceneName = "GameMenu";
    [Tooltip("Shared scene that plays any Firestore lesson; it reads CustomLessonSession.SelectedLessonId.")]
    public string quizSceneName = "CustomQuiz";

    [Header("Debug")]
    public bool debugLogs = true;

    private AudioSource source;
    private Coroutine seqRoutine;
    private int index;
    private bool busy;     // leaving the scene - ignore all keys
    private bool loading;  // fetching lessons - only Back works

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnSubmit += HandleEnter;
        BrailleMapping.OnDeleteOrNo += HandleBack;
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnSubmit -= HandleEnter;
        BrailleMapping.OnDeleteOrNo -= HandleBack;
        BrailleMapping.OnRepeat -= HandleRepeat;
    }

    private void Start()
    {
        if (BrailleMapping.Instance == null)
            Debug.LogError("[CustomLessons] No BrailleMapping in this scene. Input will not work.");

        StartCoroutine(StartupRoutine());
    }

    private IEnumerator StartupRoutine()
    {
        if (loadFromFirestore)
        {
            loading = true;
            Play(introClip, loadingClip);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("[CustomLessons] No internet.");
                FailLoad();
                yield break;
            }

            List<CustomLessonEntry> loaded = null;
            bool finished = false;
            yield return CustomLessonLoader.LoadPublished(r => { loaded = r; finished = true; });

            if (!finished || loaded == null)
            {
                FailLoad();
                yield break;
            }

            lessons = loaded;
            loading = false;
            Log("Loaded " + lessons.Count + " lessons.");
        }

        for (int i = 0; i < lessons.Count; i++)
        {
            if (lessons[i].titleClip == null)
                Debug.LogWarning("[CustomLessons] Lesson " + (i + 1) + " ('" + lessons[i].title + "') has no usable title recording.");
        }

        index = 0;
        RefreshVisuals();

        if (lessons.Count == 0) Play(introClip, noLessonsClip);
        else Play(introClip, TitleClipFor(index), instructionsClip);
    }

    private void FailLoad()
    {
        loading = false; // Back is allowed again
        lessons = new List<CustomLessonEntry>();
        RefreshVisuals();
        Play(loadErrorClip);
    }

    // ---- input ----

    private void HandleUp() => Move(-1);
    private void HandleDown() => Move(1);

    private void HandleRepeat()
    {
        if (busy || loading) return;
        if (lessons.Count == 0) { Play(noLessonsClip); return; }
        Play(TitleClipFor(index));
    }

    private void HandleEnter()
    {
        if (busy || loading) return;
        if (lessons.Count == 0) { Play(noLessonsClip); return; }
        StartCoroutine(OpenLessonRoutine(lessons[index]));
    }

    private void HandleBack()
    {
        if (busy) return;
        StartCoroutine(GoBackRoutine());
    }

    // ---- navigation ----

    private void Move(int direction)
    {
        if (busy || loading) return;

        if (lessons.Count == 0) { Play(noLessonsClip); return; }

        int newIndex = Mathf.Clamp(index + direction, 0, lessons.Count - 1);
        if (newIndex == index)
        {
            Play(direction < 0 ? firstLessonClip : lastLessonClip);
            return;
        }

        index = newIndex;
        Log("Selected " + (index + 1) + "/" + lessons.Count + ": " + lessons[index].title);
        RefreshVisuals();
        Play(TitleClipFor(index));
    }

    private IEnumerator OpenLessonRoutine(CustomLessonEntry entry)
    {
        bool isFirestoreLesson = !string.IsNullOrEmpty(entry.lessonId);
        string targetScene = isFirestoreLesson ? quizSceneName : entry.sceneName;

        if (string.IsNullOrEmpty(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError("[CustomLessons] Scene '" + targetScene + "' for '" + entry.title + "' is empty or not in Build Settings.");
            Play(notAvailableClip);
            yield break;
        }

        busy = true;
        CustomLessonSession.SelectedLessonId = isFirestoreLesson ? entry.lessonId : null;

        StopSequence();
        yield return SequenceRoutine(openingClip, TitleClipFor(index));
        SceneManager.LoadScene(targetScene);
    }

    private IEnumerator GoBackRoutine()
    {
        busy = true;
        StopSequence();
        yield return SequenceRoutine(goingBackClip);
        SceneManager.LoadScene(previousSceneName);
    }

    // ---- recorded audio ----

    private AudioClip TitleClipFor(int i)
    {
        AudioClip clip = lessons[i].titleClip;
        if (clip == null)
        {
            Debug.LogWarning("[CustomLessons] No title recording for lesson " + (i + 1) + " ('" + lessons[i].title + "').");
            return missingTitleClip;
        }
        return clip;
    }

    private void Play(params AudioClip[] clips)
    {
        StopSequence();
        source.Stop();
        seqRoutine = StartCoroutine(SequenceRoutine(clips));
    }

    private IEnumerator SequenceRoutine(params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            source.Stop();
            source.clip = clip;
            source.Play();

            yield return null;
            yield return new WaitWhile(() => source.isPlaying);
        }
    }

    private void StopSequence()
    {
        if (seqRoutine != null)
        {
            StopCoroutine(seqRoutine);
            seqRoutine = null;
        }
    }

    // ---- visuals ----

    private void RefreshVisuals()
    {
        if (currentTitleText != null)
            currentTitleText.text = lessons.Count == 0 ? "No custom lessons" : lessons[index].title;

        if (arrowUp != null) arrowUp.SetActive(lessons.Count > 0 && index > 0);
        if (arrowDown != null) arrowDown.SetActive(lessons.Count > 0 && index < lessons.Count - 1);
    }

    private void Log(string message)
    {
        if (debugLogs) Debug.Log("[CustomLessons] " + message);
    }
}