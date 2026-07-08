using System;
using System.Collections.Generic;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Static definition of a single quiz within a lesson (title + scene +
    /// its stable number). The number is what StudentProgress keys off of,
    /// so it must stay fixed once content ships even if quizzes are
    /// reordered in the Inspector.
    /// </summary>
    [Serializable]
    public class QuizDefinition
    {
        [SerializeField] private string quizTitle;
        [SerializeField] private string sceneName;
        [SerializeField] private int quizNumber = 1;

        public string QuizTitle => quizTitle;
        public string SceneName => sceneName;
        public int QuizNumber => quizNumber;
    }

    /// <summary>
    /// One entry in <see cref="LessonDatabase"/>: a lesson title plus its
    /// (up to) five quiz definitions. Plain serializable data only.
    /// </summary>
    [Serializable]
    public class LessonData
    {
        [SerializeField] private string lessonTitle;
        [SerializeField] private int lessonNumber = 1;
        [SerializeField] private List<QuizDefinition> quizzes = new List<QuizDefinition>();

        public string LessonTitle => lessonTitle;

        /// <summary>Stable lesson number, used as the StudentProgress key - keep fixed once shipped.</summary>
        public int LessonNumber => lessonNumber;

        public IReadOnlyList<QuizDefinition> Quizzes => quizzes;

        public int QuizCount => quizzes.Count;

        public QuizDefinition GetQuiz(int index)
        {
            return (index >= 0 && index < quizzes.Count) ? quizzes[index] : null;
        }
    }
}