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
/// Place this on a persistent object in the first scene that loads (e.g.
/// before the player reaches a sign-up or login scene.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public bool IsReady { get; private set; }
    public DependencyStatus Status { get; private set; } = DependencyStatus.UnavailableOther;

    private readonly TaskCompletionSource<bool> readyTcs = new TaskCompletionSource<bool>();

    private void Awake()
    {
        Instance = this;
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