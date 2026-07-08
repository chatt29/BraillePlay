using System.Collections.Generic;
using Firebase.Firestore;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// A student's full progress across every lesson: a map of lesson key
    /// ("lesson1", "lesson2", ...) to <see cref="LessonProgress"/>.
    ///
    /// Stored as its own document (Students/{studentNumber}/Progress/current -
    /// see FirestoreProgressService) rather than as extra fields on the
    /// existing StudentData document, so this feature stays isolated from
    /// the login system's schema per the Single Responsibility rule in the
    /// README ("Progress is stored only inside StudentProgress").
    /// </summary>
    [FirestoreData]
    public class StudentProgress
    {
        [FirestoreProperty("lessons")]
        public Dictionary<string, LessonProgress> Lessons { get; set; } = new Dictionary<string, LessonProgress>();

        public static string LessonKey(int lessonNumber) => "lesson" + lessonNumber;

        /// <summary>Gets existing progress for a lesson, or a fresh empty entry if none exists yet.</summary>
        public LessonProgress GetOrCreate(int lessonNumber)
        {
            string key = LessonKey(lessonNumber);
            if (!Lessons.TryGetValue(key, out LessonProgress progress))
            {
                progress = new LessonProgress();
                Lessons[key] = progress;
            }
            return progress;
        }

        /// <summary>Quiz 1 of Lesson 1 is always available - everything else unlocks through play.</summary>
        public static StudentProgress CreateDefault()
        {
            var progress = new StudentProgress();
            progress.GetOrCreate(1).GetOrCreate(1).Unlocked = true;
            return progress;
        }
    }
}