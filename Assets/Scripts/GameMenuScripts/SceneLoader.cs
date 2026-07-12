using UnityEngine;
using UnityEngine.SceneManagement;

namespace BraillePlay.GameMenu
{
    public class SceneLoader : MonoBehaviour
    {
        public readonly struct QuizLaunchContext
        {
            public readonly int LessonNumber;
            public readonly int QuizNumber;
            public readonly string ReturnSceneName;
            public readonly string NextQuizSceneName; // empty/null if this was the very last quiz in the game

            public QuizLaunchContext(int lessonNumber, int quizNumber, string returnSceneName, string nextQuizSceneName)
            {
                LessonNumber = lessonNumber;
                QuizNumber = quizNumber;
                ReturnSceneName = returnSceneName;
                NextQuizSceneName = nextQuizSceneName;
            }
        }

        public static QuizLaunchContext? PendingQuiz { get; private set; }

        [Tooltip("Scene this GameMenu instance should return to after a quiz finishes.")]
        public string gameMenuSceneName = "GameMenu";

        [Tooltip("Same LessonDatabase asset GameMenu uses to build its lesson list. Needed to resolve which quiz scene comes next, including chaining into the following lesson.")]
        [SerializeField] private LessonDatabase lessonDatabase;

        public void LoadGuide(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] LoadGuide called with an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Preferred way to launch a quiz: looks the scene up from
        /// LessonDatabase and resolves NextQuizSceneName itself - if this is
        /// the last quiz in the lesson, it chains into the next lesson's
        /// first quiz automatically instead of leaving NextQuizSceneName
        /// empty.
        /// </summary>
        public void LoadQuiz(int lessonNumber, int quizNumber)
        {
            if (lessonDatabase == null)
            {
                Debug.LogWarning("[SceneLoader] No LessonDatabase assigned - can't resolve quiz scenes.");
                return;
            }

            QuizDefinition quiz = FindQuiz(lessonNumber, quizNumber);

            if (quiz == null || string.IsNullOrEmpty(quiz.SceneName))
            {
                Debug.LogWarning($"[SceneLoader] No quiz scene found for Lesson {lessonNumber}, Quiz {quizNumber}.");
                return;
            }

            string nextQuizSceneName = ResolveNextQuizSceneName(lessonNumber, quizNumber);

            PendingQuiz = new QuizLaunchContext(lessonNumber, quizNumber, gameMenuSceneName, nextQuizSceneName);
            SceneManager.LoadScene(quiz.SceneName);
        }

        /// <summary>
        /// Older explicit-args version, kept so existing callers keep
        /// compiling. Doesn't auto-chain into the next lesson - whoever
        /// calls this is responsible for passing the right
        /// nextQuizSceneName. Prefer LoadQuiz(lessonNumber, quizNumber)
        /// above for new code.
        /// </summary>
        public void LoadQuiz(string sceneName, int lessonNumber, int quizNumber, string nextQuizSceneName = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] LoadQuiz called with an empty scene name.");
                return;
            }

            PendingQuiz = new QuizLaunchContext(lessonNumber, quizNumber, gameMenuSceneName, nextQuizSceneName);
            SceneManager.LoadScene(sceneName);
        }

        public static QuizLaunchContext? ConsumePendingQuiz()
        {
            QuizLaunchContext? context = PendingQuiz;
            PendingQuiz = null;
            return context;
        }

        // -------------------------------------------------------------------
        // LessonDatabase lookups
        // -------------------------------------------------------------------

        private QuizDefinition FindQuiz(int lessonNumber, int quizNumber)
        {
            LessonData lesson = FindLesson(lessonNumber);
            if (lesson == null) return null;

            foreach (QuizDefinition quiz in lesson.Quizzes)
            {
                if (quiz.QuizNumber == quizNumber)
                    return quiz;
            }

            return null;
        }

        private LessonData FindLesson(int lessonNumber)
        {
            for (int i = 0; i < lessonDatabase.Count; i++)
            {
                LessonData lesson = lessonDatabase.Get(i);
                if (lesson != null && lesson.LessonNumber == lessonNumber)
                    return lesson;
            }

            return null;
        }

        private string ResolveNextQuizSceneName(int lessonNumber, int quizNumber)
        {
            LessonData currentLesson = FindLesson(lessonNumber);
            if (currentLesson == null) return null;

            // Next quiz within the same lesson, if one exists - every lesson
            // only has 1 quiz right now, so this normally finds nothing, but
            // it keeps this working automatically if that ever changes.
            foreach (QuizDefinition quiz in currentLesson.Quizzes)
            {
                if (quiz.QuizNumber == quizNumber + 1)
                    return quiz.SceneName;
            }

            // Otherwise chain into the next lesson's first quiz.
            LessonData nextLesson = FindLesson(lessonNumber + 1);
            if (nextLesson != null && nextLesson.QuizCount > 0)
                return nextLesson.GetQuiz(0)?.SceneName;

            // No more quizzes anywhere - this really was the last one.
            return null;
        }
    }
}