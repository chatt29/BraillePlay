using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus status = task.Result;

            if (status == DependencyStatus.Available)
            {
                Debug.Log(" Firebase initialized successfully!");
            }
            else
            {
                Debug.LogError(" Firebase failed to initialize: " + status);
            }
        });
    }
}