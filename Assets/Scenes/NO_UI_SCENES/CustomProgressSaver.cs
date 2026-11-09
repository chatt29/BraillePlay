using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public static class CustomProgressSaver
{
    public static async Task Save(string studentId, string lessonId, string title,
                                  string teacherId, int correct, int total)
    {
        if (string.IsNullOrEmpty(studentId))
        {
            Debug.LogWarning("[CustomProgress] No student id, result not saved.");
            return;
        }

        DocumentReference doc = FirebaseFirestore.DefaultInstance
            .Collection("Students").Document(studentId)
            .Collection("Progress").Document("customed");

        int score = total > 0 ? Mathf.RoundToInt(100f * correct / total) : 0;
        int attempts = 0;
        int best = 0;

        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (snap.Exists
            && snap.TryGetValue("lessons", out Dictionary<string, object> lessons)
            && lessons.TryGetValue(lessonId, out object entryObj)
            && entryObj is Dictionary<string, object> entry)
        {
            if (entry.TryGetValue("attempts", out object a)) attempts = Convert.ToInt32(a);
            if (entry.TryGetValue("bestScore", out object b)) best = Convert.ToInt32(b);
        }

        var lessonEntry = new Dictionary<string, object>
        {
            { "title", title },
            { "teacherId", teacherId },
            { "bestScore", Mathf.Max(best, score) },
            { "lastScore", score },
            { "attempts", attempts + 1 },
            { "totalQuestions", total },
            { "lastPlayedAt", FieldValue.ServerTimestamp },
        };

        var update = new Dictionary<string, object>
        {
            { "studentID", studentId },
            { "lessons", new Dictionary<string, object> { { lessonId, lessonEntry } } },
        };

        // MergeAll updates only this lesson's entry and leaves the others alone.
        await doc.SetAsync(update, SetOptions.MergeAll);
    }
}