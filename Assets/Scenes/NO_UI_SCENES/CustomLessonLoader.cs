using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Networking;

public static class CustomLessonLoader
{
    /// <summary>
    /// Loads every published lesson as a CustomLessonEntry (id, title, title clip).
    /// Calls onDone(null) if the request fails.
    /// </summary>
    public static IEnumerator LoadPublished(Action<List<CustomLessonEntry>> onDone)
    {
        Task<QuerySnapshot> task = FirebaseFirestore.DefaultInstance
            .Collection("CustomLessons")
            .WhereEqualTo("published", true)
            .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("[CustomLessonLoader] Query failed: " + task.Exception);
            onDone(null);
            yield break;
        }

        var result = new List<CustomLessonEntry>();

        foreach (DocumentSnapshot doc in task.Result.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            var entry = new CustomLessonEntry { lessonId = doc.Id };
            entry.title = data.TryGetValue("title", out object t) && t is string ts ? ts : "";

            if (data.TryGetValue("title_audio", out object ta) && ta is string b64 && b64.Length > 0)
                yield return Base64ToClip(b64, clip => entry.titleClip = clip);

            result.Add(entry);
        }

        onDone(result);
    }

    /// <summary>
    /// Decodes a base64 mp3 string into an AudioClip. Calls done(null) on failure.
    /// Reusable from the quiz scene for question and choice audio.
    /// </summary>
    public static IEnumerator Base64ToClip(string b64, Action<AudioClip> done)
    {
        byte[] bytes = null;
        bool ok = true;
        try { bytes = Convert.FromBase64String(b64); }
        catch (FormatException) { ok = false; }

        if (!ok)
        {
            Debug.LogWarning("[CustomLessonLoader] Audio was not valid base64.");
            done(null);
            yield break;
        }

        string path = Path.Combine(Application.persistentDataPath, Guid.NewGuid() + ".mp3");
        File.WriteAllBytes(path, bytes);

        using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                done(DownloadHandlerAudioClip.GetContent(req));
            else
            {
                Debug.LogWarning("[CustomLessonLoader] Couldn't decode audio: " + req.error);
                done(null);
            }
        }

        File.Delete(path);
    }
}