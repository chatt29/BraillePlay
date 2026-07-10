using Firebase.Firestore;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Per-quiz progress for a single student. Mirrors StudentData.cs's
    /// style: explicit lowerCamelCase FirestoreProperty names so the schema
    /// is stable regardless of C# property names.
    /// </summary>
    [FirestoreData]
    public class QuizProgress
    {
        [FirestoreProperty("unlocked")]
        public bool Unlocked { get; set; }

        [FirestoreProperty("completed")]
        public bool Completed { get; set; }

        [FirestoreProperty("highestScore")]
        public int HighestScore { get; set; }

        [FirestoreProperty("attempts")]
        public int Attempts { get; set; }

        /// <summary>Records a new attempt, keeping the best score seen so far. Does not touch Unlocked.</summary>
        public void RecordAttempt(int scorePercent)
        {
            Attempts++;
            if (scorePercent > HighestScore)
                HighestScore = scorePercent;
            Completed = true;
        }
    }
}