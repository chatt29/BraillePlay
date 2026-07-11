using UnityEngine;
using UnityEngine.SceneManagement;
using BraillePlay.GameMenu;

/// <summary>
/// Bridges a quiz scene back to GameMenu's ProgressManager/SceneLoader.
/// Records the score immediately once the quiz finishes, then hands off to
/// QuizEndMenu to ask the student what to do next (repeat / next quiz /
/// back to menu) instead of navigating away right away.
/// </summary>
public class QuizResultReporter : MonoBehaviour
{
    [Tooltip("Used only if this scene was opened directly (e.g. from the Editor) with no pending quiz context.")]
    [SerializeField] private string fallbackReturnSceneName = "GameMenu";

    [SerializeField] private QuizEndMenu endMenu;

    private int lessonNumber;
    private int quizNumber;
    private string returnSceneName;
    private string nextQuizSceneName;
    private bool hasContext;
    private bool reported;

    private void Awake()
    {
        SceneLoader.QuizLaunchContext? pending = SceneLoader.ConsumePendingQuiz();

        if (pending.HasValue)
        {
            lessonNumber = pending.Value.LessonNumber;
            quizNumber = pending.Value.QuizNumber;
            returnSceneName = pending.Value.ReturnSceneName;
            nextQuizSceneName = pending.Value.NextQuizSceneName;
            hasContext = true;
        }
        else
        {
            Debug.LogWarning("[QuizResultReporter] No pending quiz context found - was this scene opened directly? Returning to " + fallbackReturnSceneName + " with no progress recorded, no Next Quiz option.");
            returnSceneName = fallbackReturnSceneName;
        }
    }

    /// <summary>Call once when the quiz finishes with a final score (0-100). Records progress immediately, then shows the end-of-quiz choice menu.</summary>
    public void ReportScoreAndReturn(int scorePercent)
    {
        if (reported) return;
        reported = true;

        if (hasContext)
        {
            if (ProgressManager.Instance != null)
                ProgressManager.Instance.RecordQuizResult(lessonNumber, quizNumber, scorePercent);
            else
                Debug.LogWarning("[QuizResultReporter] ProgressManager.Instance is null - score not saved.");
        }

        if (endMenu != null)
        {
            endMenu.Show(
                scorePercent,
                hasNextQuiz: !string.IsNullOrEmpty(nextQuizSceneName),
                onRepeat: RepeatThisQuiz,
                onNextQuiz: GoToNextQuiz,
                onBackToMenu: GoBackToMenu);
        }
        else
        {
            Debug.LogWarning("[QuizResultReporter] No QuizEndMenu assigned - returning straight to GameMenu instead of asking.");
            GoBackToMenu();
        }
    }

    /// <summary>Call to quit early (e.g. long-press-back mid-quiz) without recording a result.</summary>
    public void QuitWithoutReporting()
    {
        SceneManager.LoadScene(returnSceneName);
    }

    private void RepeatThisQuiz()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToNextQuiz()
    {
        if (string.IsNullOrEmpty(nextQuizSceneName))
        {
            Debug.LogWarning("[QuizResultReporter] GoToNextQuiz called with no next quiz available - going back to menu instead.");
            GoBackToMenu();
            return;
        }

        SceneManager.LoadScene(nextQuizSceneName);
    }

    private void GoBackToMenu()
    {
        SceneManager.LoadScene(returnSceneName);
    }
}