using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;

/// <summary>
/// All Firestore access for the Students collection, isolated from
/// UI/accessibility code per the README ("Keep Firebase code separate").
/// Student document ID = student number (StudentData itself carries no
/// student-number field - see StudentData.cs).
/// </summary>
public class FirestoreStudentService
{
    private const string CollectionName = "Students";

    private readonly FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

    /// <summary>True if a student with this student number already exists.</summary>
    public async Task<bool> StudentExistsAsync(string studentNumber)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(studentNumber).GetSnapshotAsync();
        return snapshot.Exists;
    }

    /// <summary>Creates a new student document at Students/{studentNumber}. Caller is responsible for checking StudentExistsAsync first.</summary>
    public async Task CreateStudentAsync(string studentNumber, StudentData student)
    {
        await EnsureReadyAsync();
        await db.Collection(CollectionName).Document(studentNumber).SetAsync(student);
    }

    /// <summary>Loads a student by student number, or null if none exists.</summary>
    public async Task<StudentData> LoadStudentAsync(string studentNumber)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(studentNumber).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<StudentData>() : null;
    }

    /// <summary>Updates a student's score/lesson-progress fields without touching the rest of the document.</summary>
    public async Task UpdateProgressAsync(string studentNumber, int totalScore, int highestScore, int completedLessons, int currentLesson, int currentQuiz)
    {
        await EnsureReadyAsync();

        DocumentReference doc = db.Collection(CollectionName).Document(studentNumber);

        var updates = new Dictionary<string, object>
        {
            { "totalScore", totalScore },
            { "highestScore", highestScore },
            { "completedLessons", completedLessons },
            { "currentLesson", currentLesson },
            { "currentQuiz", currentQuiz }
        };

        await doc.UpdateAsync(updates);
    }

    private static async Task EnsureReadyAsync()
    {
        if (FirebaseManager.Instance == null)
            throw new Exception("[FirestoreStudentService] FirebaseManager isn't in the scene - add it to a scene that loads before any Firestore call.");

        bool ready = await FirebaseManager.Instance.WaitUntilReadyAsync();
        if (!ready)
            throw new Exception("[FirestoreStudentService] Firebase failed to initialize: " + FirebaseManager.Instance.Status);
    }
}