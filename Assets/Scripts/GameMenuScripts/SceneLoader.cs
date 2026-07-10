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
            public readonly string NextQuizSceneName; // empty/null if this was the last quiz in the lesson

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

        public void LoadGuide(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] LoadGuide called with an empty scene name.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

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
    }
}