using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

/// <summary>
/// Initializes Firebase once per app run and exposes a readiness signal so
/// any script that talks to Firestore (or Auth, etc later) can wait for it
/// instead of racing FirebaseApp.CheckAndFixDependenciesAsync() - which is
/// exactly what was happening before: FirestoreStudentService/TeacherService
/// could touch FirebaseFirestore.DefaultInstance before this finished,
/// especially on Android where it may first need to prompt a Google Play
/// Services update.
///
/// Place this on a persistent object in the first scene that loads (e.g.
/// MainMenu) with DontDestroyOnLoad, so it's already initializing well
/// before the player reaches a sign-up or login scene.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Tooltip("On by default so this survives scene loads and every later scene reuses the same initialized instance instead of re-running Firebase init redundantly.")]
    public bool dontDestroyOnLoad = true;

    public bool IsReady { get; private set; }
    public DependencyStatus Status { get; private set; } = DependencyStatus.UnavailableOther;

    private readonly TaskCompletionSource<bool> readyTcs = new TaskCompletionSource<bool>();

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
            // DontDestroyOnLoad only works on a root-level GameObject (no
            // parent). If this was dropped inside a Canvas or any other
            // parented object, detach it first so persistence actually
            // works instead of silently failing (or logging a warning and
            // being destroyed on the next scene load anyway).
            if (transform.parent != null)
            {
                Debug.LogWarning("[FirebaseManager] This GameObject had a parent, which breaks DontDestroyOnLoad. Detaching it to the scene root automatically.");
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Status = task.Result;
            IsReady = Status == DependencyStatus.Available;

            if (IsReady)
                Debug.Log("[Firebase] Initialized successfully.");
            else
                Debug.LogError("[Firebase] Failed to initialize: " + Status);

            readyTcs.TrySetResult(IsReady);
        });
    }

    /// <summary>
    /// Awaitable from any Firestore/Auth call: completes once Firebase has
    /// finished initializing, whether that succeeded or failed. Check
    /// IsReady/Status afterward to see which.
    /// </summary>
    public Task<bool> WaitUntilReadyAsync() => readyTcs.Task;
}