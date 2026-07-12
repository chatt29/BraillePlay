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

        DocumentReference studentDoc =
            db.Collection(CollectionName).Document(studentNumber);

        // Create student document
        await studentDoc.SetAsync(student);

        // Create Lessons subcollection
        CollectionReference lessons =
            studentDoc.Collection("Lessons");

        for (int lesson = 1; lesson <= 5; lesson++)
        {
            await lessons.Document($"Lesson{lesson}").SetAsync(
                new Dictionary<string, object>
                {
                { "Quiz1Score", 0 },
                { "Quiz2Score", 0 },
                { "Quiz3Score", 0 },
                { "Quiz4Score", 0 },
                { "Quiz5Score", 0 }
                });
        }
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

    /// <summary>Loads every student in the collection, for the teacher-facing student list. Returns (studentNumber, data) pairs since StudentData itself doesn't carry its own document ID.</summary>
    public async Task<List<(string StudentNumber, StudentData Data)>> ListAllStudentsAsync()
    {
        await EnsureReadyAsync();

        QuerySnapshot snapshot = await db.Collection(CollectionName).GetSnapshotAsync();

        var results = new List<(string, StudentData)>();
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Exists)
                results.Add((doc.Id, doc.ConvertTo<StudentData>()));
        }

        return results;
    }

    /// <summary>Deletes a student's document AND their Lessons/Progress subcollections. Caller should confirm with the teacher first - this can't be undone.</summary>
    public async Task DeleteStudentAsync(string studentNumber)
    {
        await EnsureReadyAsync();

        DocumentReference studentDoc = db.Collection(CollectionName).Document(studentNumber);

        // Firestore doesn't cascade-delete subcollections automatically -
        // each one has to be cleared out document by document first, or
        // Lessons/Progress data will silently linger as an orphaned
        // subcollection under a document ID that no longer "exists".
        await DeleteSubcollectionAsync(studentDoc.Collection("Lessons"));
        await DeleteSubcollectionAsync(studentDoc.Collection("Progress"));

        await studentDoc.DeleteAsync();
    }

    private static async Task DeleteSubcollectionAsync(CollectionReference collection)
    {
        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        foreach (DocumentSnapshot doc in snapshot.Documents)
            await doc.Reference.DeleteAsync();
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