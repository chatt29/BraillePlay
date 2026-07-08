using UnityEngine;
using UnityEngine.SceneManagement;
using BraillePlay.GameMenu;

/// <summary>
/// Bridges a quiz scene back to GameMenu's ProgressManager/SceneLoader.
/// Reads the pending quiz context that SceneLoader.LoadQuiz() set before
/// this scene loaded (which lesson/quiz this is), exposes
/// ReportScoreAndReturn() for the quiz's own completion logic to call once,
/// and QuitWithoutReporting() for an early exit that doesn't save a result.
/// </summary>
public class QuizResultReporter : MonoBehaviour
{
    [Tooltip("Used only if this scene was opened directly (e.g. from the Editor) with no pending quiz context.")]
    [SerializeField] private string fallbackReturnSceneName = "GameMenu";

    private int lessonNumber;
    private int quizNumber;
    private string returnSceneName;
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
            hasContext = true;
        }
        else
        {
            Debug.LogWarning("[QuizResultReporter] No pending quiz context found - was this scene opened directly? Returning to " + fallbackReturnSceneName + " with no progress recorded.");
            returnSceneName = fallbackReturnSceneName;
        }
    }

    /// <summary>Call once when the quiz finishes with a final score (0-100). Records progress, then returns to GameMenu.</summary>
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

        SceneManager.LoadScene(returnSceneName);
    }

    /// <summary>Call to quit early (e.g. long-press-back mid-quiz) without recording a result.</summary>
    public void QuitWithoutReporting()
    {
        SceneManager.LoadScene(returnSceneName);
    }
}