using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

/// <summary>Remembers which published lesson the menu picked, for the quiz scene to read.</summary>
public static class CustomLessonSession
{
    public static string SelectedLessonId { get; set; }
}

/// <summary>
/// Reads published lessons from Firestore "CustomLessons" (written by the
/// Teacher UI's publish_lesson) and turns each lesson's base64 title_audio
/// into an AudioClip. Menu-only: it fetches the title, id and title clip -
/// the quiz scene loads the rest of the lesson by id.
///
/// ASSUMPTION: Unity uses the Firebase Firestore SDK (as the existing
/// FirestoreStudentService appears to). If it wraps Firestore differently,
/// swap just the query below.
/// </summary>
public static class CustomLessonLoader
{
    public const string Collection = "CustomLessons";

    /// <summary>Calls onDone with the list (empty if none) or null if the load failed.</summary>
    public static IEnumerator LoadPublished(Action<List<CustomLessonEntry>> onDone)
    {
        var query = FirebaseFirestore.DefaultInstance.Collection(Collection).WhereEqualTo("published", true);
        var task = query.GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogException(task.Exception);
            onDone?.Invoke(null);
            yield break;
        }

        var result = new List<CustomLessonEntry>();

        foreach (DocumentSnapshot doc in task.Result.Documents)
        {
            doc.TryGetValue("title", out string title);
            doc.TryGetValue("title_audio", out string titleAudio);

            AudioClip clip = null;
            if (!string.IsNullOrEmpty(titleAudio))
            {
                try { clip = WavDecoder.ToClip(doc.Id + "_title", Convert.FromBase64String(titleAudio)); }
                catch (FormatException e) { Debug.LogWarning("[CustomLessons] Bad base64 title audio for " + doc.Id + ": " + e.Message); }
            }

            result.Add(new CustomLessonEntry
            {
                lessonId = doc.Id,
                title = string.IsNullOrWhiteSpace(title) ? "Untitled lesson" : title,
                titleClip = clip
            });

            yield return null; // decode one lesson per frame so a big list doesn't freeze the scene
        }

        result.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));
        onDone?.Invoke(result);
    }
}