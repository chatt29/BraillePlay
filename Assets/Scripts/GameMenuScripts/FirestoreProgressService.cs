using System;
using System.Threading.Tasks;
using Firebase.Firestore;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// All Firestore access for a student's GameMenu progress, isolated from
    /// UI/gameplay code, following the same pattern as FirestoreStudentService.
    ///
    /// Stored at Students/{studentNumber}/Progress/current as its own
    /// document, separate from the existing StudentData document.
    /// </summary>
    public class FirestoreProgressService
    {
        private const string StudentsCollection = "Students";
        private const string ProgressSubcollection = "Progress";
        private const string ProgressDocId = "current";

        private readonly FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        /// <summary>Loads a student's progress, or a fresh default (Quiz 1 of Lesson 1 unlocked) if none exists yet.</summary>
        public async Task<StudentProgress> LoadProgressAsync(string studentNumber)
        {
            await EnsureReadyAsync();

            DocumentSnapshot snapshot = await ProgressDoc(studentNumber).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ConvertTo<StudentProgress>() : StudentProgress.CreateDefault();
        }

        /// <summary>Overwrites the student's entire progress document with the given (already-mutated) snapshot.</summary>
        public async Task SaveProgressAsync(string studentNumber, StudentProgress progress)
        {
            await EnsureReadyAsync();
            await ProgressDoc(studentNumber).SetAsync(progress);
        }

        private DocumentReference ProgressDoc(string studentNumber)
        {
            return db.Collection(StudentsCollection)
                .Document(studentNumber)
                .Collection(ProgressSubcollection)
                .Document(ProgressDocId);
        }

        private static async Task EnsureReadyAsync()
        {
            if (FirebaseManager.Instance == null)
                throw new Exception("[FirestoreProgressService] FirebaseManager isn't in the scene - add it to a scene that loads before any Firestore call.");

            bool ready = await FirebaseManager.Instance.WaitUntilReadyAsync();
            if (!ready)
                throw new Exception("[FirestoreProgressService] Firebase failed to initialize: " + FirebaseManager.Instance.Status);
        }
    }
}