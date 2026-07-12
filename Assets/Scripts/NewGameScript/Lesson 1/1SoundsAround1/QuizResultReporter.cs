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

    [Tooltip("Disabled once the quiz naturally finishes, so the long-press-back quit prompt can't pop up on top of QuizEndMenu and double-fire on the same Space/Backspace press.")]
    [SerializeField] private QuizBackHandler backHandler;

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

    /// <summary>
    /// Call once when the quiz finishes with a final score (0-100). Records
    /// progress immediately, then shows the end-of-quiz choice menu.
    ///
    /// scoreAlreadyAnnounced: pass true (the default) when the quiz scene
    /// already spoke the score itself (e.g. BrailleSoundsAround1's own
    /// recorded-audio score readout) so QuizEndMenu only asks the
    /// continue/back question instead of repeating the score via TTS.
    /// </summary>
    public void ReportScoreAndReturn(int scorePercent, bool scoreAlreadyAnnounced = true)
    {
        if (reported) return;
        reported = true;

        // The quiz is genuinely over now - there's nothing left to "quit
        // early" out of, so stop listening for the long-press-back gesture
        // entirely. Otherwise it can pop its own confirm overlay on top of
        // QuizEndMenu below and both end up listening for the same
        // Space/Backspace press at once.
        if (backHandler != null)
            backHandler.enabled = false;

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
                onNextQuiz: GoToNextQuiz,
                onBackToMenu: GoBackToMenu,
                announceScore: !scoreAlreadyAnnounced);
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