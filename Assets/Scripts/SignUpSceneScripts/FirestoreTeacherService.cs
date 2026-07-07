using System;
using System.Threading.Tasks;
using Firebase.Firestore;

/// <summary>
/// All Firestore access for the Teachers collection, isolated from
/// UI/accessibility code per the README ("Keep Firebase code separate").
/// Teacher document ID = username, matching the pattern used for students
/// (document ID = student number) so lookups by the field that's actually
/// unique don't require a query.
/// </summary>
public class FirestoreTeacherService
{
    private const string CollectionName = "Teachers";

    private readonly FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

    /// <summary>True if a teacher with this username already exists.</summary>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(username).GetSnapshotAsync();
        return snapshot.Exists;
    }

    /// <summary>Creates a new teacher document. Caller is responsible for checking UsernameExistsAsync first.</summary>
    public async Task CreateTeacherAsync(TeacherData teacher)
    {
        await EnsureReadyAsync();
        await db.Collection(CollectionName).Document(teacher.Username).SetAsync(teacher);
    }

    /// <summary>Loads a teacher by username, or null if none exists. Useful for the login scene.</summary>
    public async Task<TeacherData> LoadTeacherAsync(string username)
    {
        await EnsureReadyAsync();
        DocumentSnapshot snapshot = await db.Collection(CollectionName).Document(username).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<TeacherData>() : null;
    }

    private static async Task EnsureReadyAsync()
    {
        if (FirebaseManager.Instance == null)
            throw new Exception("[FirestoreTeacherService] FirebaseManager isn't in the scene - add it to a scene that loads before any Firestore call.");

        bool ready = await FirebaseManager.Instance.WaitUntilReadyAsync();
        if (!ready)
            throw new Exception("[FirestoreTeacherService] Firebase failed to initialize: " + FirebaseManager.Instance.Status);
    }
}