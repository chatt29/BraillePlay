using System.Collections.Generic;
using Firebase.Firestore;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Per-lesson progress for a single student: a map of quiz key
    /// ("quiz1".."quiz5", see <see cref="QuizKey"/>) to <see cref="QuizProgress"/>.
    /// A map rather than a fixed-size array so LessonData can grow past five
    /// quizzes later without a schema migration.
    /// </summary>
    [FirestoreData]
    public class LessonProgress
    {
        [FirestoreProperty("quizzes")]
        public Dictionary<string, QuizProgress> Quizzes { get; set; } = new Dictionary<string, QuizProgress>();

        public static string QuizKey(int quizNumber) => "quiz" + quizNumber;

        /// <summary>Gets existing progress for a quiz, or a fresh locked/incomplete entry if none exists yet.</summary>
        public QuizProgress GetOrCreate(int quizNumber)
        {
            string key = QuizKey(quizNumber);
            if (!Quizzes.TryGetValue(key, out QuizProgress progress))
            {
                progress = new QuizProgress();
                Quizzes[key] = progress;
            }
            return progress;
        }
    }
}