using UnityEngine;
using UnityEngine.SceneManagement;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Loads scenes and nothing else, per the README ("Scene loading happens
    /// only inside SceneLoader"). Also carries the small bit of context a
    /// Quiz scene needs to know which lesson/quiz it's running, and that the
    /// GameMenu needs on return to record the result - this is metadata
    /// about the scene transition itself, not gameplay logic, so it lives
    /// here rather than in a separate manager.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>Set right before loading a quiz scene; read by the Quiz scene and by GameMenuManager on return.</summary>
        public readonly struct QuizLaunchContext
        {
            public readonly int LessonNumber;
            public readonly int QuizNumber;
            public readonly string ReturnSceneName;

            public QuizLaunchContext(int lessonNumber, int quizNumber, string returnSceneName)
            {
                LessonNumber = lessonNumber;
                QuizNumber = quizNumber;
                ReturnSceneName = returnSceneName;
            }
        }

        /// <summary>Non-null only between LoadQuiz() being called and the GameMenu scene reading/clearing it on return.</summary>
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

        public void LoadQuiz(string sceneName, int lessonNumber, int quizNumber)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] LoadQuiz called with an empty scene name.");
                return;
            }

            PendingQuiz = new QuizLaunchContext(lessonNumber, quizNumber, gameMenuSceneName);
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Called once by GameMenuManager on load to consume any pending quiz-result context.</summary>
        public static QuizLaunchContext? ConsumePendingQuiz()
        {
            QuizLaunchContext? context = PendingQuiz;
            PendingQuiz = null;
            return context;
        }
    }
}