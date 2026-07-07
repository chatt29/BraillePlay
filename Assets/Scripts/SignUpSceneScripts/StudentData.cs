using Firebase.Firestore;

/// <summary>
/// Maps to a document under Students/{studentNumber} in Firestore.
///
/// Field names are pinned explicitly (lowerCamelCase) to match the schema
/// already live in the database - without an explicit name, FirestoreProperty
/// uses the C# property name as-is ("FirstName"), which would create a
/// second, incompatible set of fields alongside the existing "firstName".
///
/// StudentNumber is NOT stored as a field - the student number is the
/// document ID itself (see FirestoreStudentService), matching what's
/// already in the console.
/// </summary>
[FirestoreData]
public class StudentData
{
    [FirestoreProperty("firstName")]
    public string FirstName { get; set; }

    [FirestoreProperty("lastName")]
    public string LastName { get; set; }

    [FirestoreProperty("totalScore")]
    public int TotalScore { get; set; }

    [FirestoreProperty("highestScore")]
    public int HighestScore { get; set; }

    [FirestoreProperty("completedLessons")]
    public int CompletedLessons { get; set; }

    [FirestoreProperty("currentLesson")]
    public int CurrentLesson { get; set; }

    [FirestoreProperty("currentQuiz")]
    public int CurrentQuiz { get; set; }
}