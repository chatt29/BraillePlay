using System;
using System.Collections.Generic;
using UnityEngine;

namespace BraillePlay.GameMenu
{
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

    [Serializable]
    public class LessonData
    {
        [SerializeField] private string lessonTitle;
        [Tooltip("Recording of the lesson title (our own voice).")]
        [SerializeField] private AudioClip titleClip;
        [SerializeField] private int lessonNumber = 1;
        [SerializeField] private List<QuizDefinition> quizzes = new List<QuizDefinition>();

        public string LessonTitle => lessonTitle;

        /// <summary>Recorded voice for the title.</summary>
        public AudioClip TitleClip => titleClip;

        public int LessonNumber => lessonNumber;
        public IReadOnlyList<QuizDefinition> Quizzes => quizzes;
        public int QuizCount => quizzes.Count;

        public QuizDefinition GetQuiz(int index)
        {
            return (index >= 0 && index < quizzes.Count) ? quizzes[index] : null;
        }
    }
}