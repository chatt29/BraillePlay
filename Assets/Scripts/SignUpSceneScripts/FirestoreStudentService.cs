using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Auth;
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
    public async Task<bool> StudentExistsAsync(string studentID)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(studentID).GetSnapshotAsync();
        return snapshot.Exists;
    }

    /// <summary>Creates a new student document at Students/{studentID}. Caller is responsible for checking StudentExistsAsync first.</summary>
    public async Task CreateStudentAsync(string studentID, StudentData student)
    {
        await EnsureReadyAsync();

        DocumentReference studentDoc =
            db.Collection(CollectionName).Document(studentID);

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
    public async Task<StudentData> LoadStudentAsync(string studentID)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(studentID).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<StudentData>() : null;
    }

    /// <summary>Updates a student's score/lesson-progress fields without touching the rest of the document.</summary>
    public async Task UpdateProgressAsync(string studentID, int totalScore, int highestScore, int completedLessons, int currentLesson, int currentQuiz)
    {
        await EnsureReadyAsync();

        DocumentReference doc = db.Collection(CollectionName).Document(studentID);

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

    /// <summary>Loads every student in the collection, for the teacher-facing student list. Returns (studentID, data) pairs since StudentData itself doesn't carry its own document ID.</summary>
    public async Task<List<(string studentID, StudentData Data)>> ListAllStudentsAsync()
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

    /// <summary>
    /// Saves edited student fields, handling a Student Number change as a
    /// full migration since the student number IS the document ID and
    /// Firestore has no "rename a document" operation. Copies the Lessons
    /// and Progress subcollections to the new document ID, then deletes the
    /// old document, so no progress is lost when a teacher corrects a typo
    /// in a student's number.
    ///
    /// If the number is unchanged, this just overwrites the existing
    /// document's fields directly - no migration needed.
    /// </summary>
    public async Task RenameStudentAsync(string oldstudentID, string newstudentID, StudentData updatedData)
    {
        await EnsureReadyAsync();

        if (oldstudentID == newstudentID)
        {
            await db.Collection(CollectionName).Document(oldstudentID).SetAsync(updatedData);
            return;
        }

        if (await StudentExistsAsync(newstudentID))
            throw new Exception($"A student with number {newstudentID} already exists.");

        DocumentReference oldDoc = db.Collection(CollectionName).Document(oldstudentID);
        DocumentReference newDoc = db.Collection(CollectionName).Document(newstudentID);

        await newDoc.SetAsync(updatedData);
        await CopySubcollectionAsync(oldDoc.Collection("Lessons"), newDoc.Collection("Lessons"));
        await CopySubcollectionAsync(oldDoc.Collection("Progress"), newDoc.Collection("Progress"));

        await DeleteSubcollectionAsync(oldDoc.Collection("Lessons"));
        await DeleteSubcollectionAsync(oldDoc.Collection("Progress"));
        await oldDoc.DeleteAsync();
    }

    /// <summary>Deletes a student's document AND their Lessons/Progress subcollections. Caller should confirm with the teacher first - this can't be undone.</summary>
    public async Task DeleteStudentAsync(string studentID)
    {
        await EnsureReadyAsync();

        DocumentReference studentDoc = db.Collection(CollectionName).Document(studentID);

        // Firestore doesn't cascade-delete subcollections automatically -
        // each one has to be cleared out document by document first, or
        // Lessons/Progress data will silently linger as an orphaned
        // subcollection under a document ID that no longer "exists".
        await DeleteSubcollectionAsync(studentDoc.Collection("Lessons"));
        await DeleteSubcollectionAsync(studentDoc.Collection("Progress"));

        await studentDoc.DeleteAsync();
    }

    private static async Task CopySubcollectionAsync(CollectionReference source, CollectionReference destination)
    {
        QuerySnapshot snapshot = await source.GetSnapshotAsync();
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Exists)
                await destination.Document(doc.Id).SetAsync(doc.ToDictionary());
        }
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

    // Firestore rules require request.auth != null, so make sure we're signed in.
    FirebaseAuth auth = FirebaseAuth.DefaultInstance;
    if (auth.CurrentUser == null)
        await auth.SignInAnonymouslyAsync();
}
}