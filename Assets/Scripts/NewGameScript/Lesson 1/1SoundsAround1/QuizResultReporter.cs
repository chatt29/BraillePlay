using UnityEngine;
using UnityEngine.SceneManagement;
using BraillePlay.GameMenu;

/// <summary>
/// Bridges a quiz scene back to GameMenu's ProgressManager/SceneLoader.
/// Records the score immediately once the quiz finishes, then hands off to
/// QuizEndMenu so the student can go back to the game menu. There is no
/// "next quiz" option.
///
/// If the scene was NOT launched through SceneLoader (no pending quiz
/// context), the Inspector fallback lesson/quiz numbers are used so the
/// score is still saved.
/// </summary>
public class QuizResultReporter : MonoBehaviour
{
    [Header("Fallback (used when there is no pending quiz context)")]
    [Tooltip("Lesson number to save to if the scene has no launch context. 0 = don't save without a context.")]
    [SerializeField] private int fallbackLessonNumber = 0;
    [Tooltip("Quiz number to save to if the scene has no launch context. 0 = don't save without a context.")]
    [SerializeField] private int fallbackQuizNumber = 0;
    [Tooltip("Used only if this scene was opened directly (e.g. from the Editor) with no pending quiz context.")]
    [SerializeField] private string fallbackReturnSceneName = "GameMenu";

    [Header("End Of Quiz")]
    [SerializeField] private QuizEndMenu endMenu;

    [Tooltip("Disabled once the quiz naturally finishes, so the long-press-back quit prompt can't pop up on top of QuizEndMenu and double-fire on the same Space/Backspace press.")]
    [SerializeField] private QuizBackHandler backHandler;

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
            lessonNumber = fallbackLessonNumber;
            quizNumber = fallbackQuizNumber;
            returnSceneName = fallbackReturnSceneName;
            hasContext = lessonNumber > 0 && quizNumber > 0;

            Debug.LogWarning("[QuizResultReporter] No pending quiz context found - was this scene opened directly? Using fallback lesson=" + lessonNumber + ", quiz=" + quizNumber + ", saving=" + hasContext + ".");
        }

        if (string.IsNullOrEmpty(returnSceneName))
            returnSceneName = fallbackReturnSceneName;

        Debug.Log($"[QuizResultReporter] Ready. hasContext={hasContext}, lesson={lessonNumber}, quiz={quizNumber}, return={returnSceneName}");
    }

    /// <summary>
    /// Call once when the quiz finishes with a final score (0-100). Records
    /// progress immediately, then shows the end-of-quiz menu.
    ///
    /// scoreAlreadyAnnounced: pass true (the default) when the quiz scene
    /// already spoke the score itself so QuizEndMenu doesn't repeat it via TTS.
    /// </summary>
    public void ReportScoreAndReturn(int scorePercent, bool scoreAlreadyAnnounced = true)
    {
        if (reported) return;
        reported = true;

        Debug.Log($"[QuizResultReporter] ReportScoreAndReturn hasContext={hasContext}, lesson={lessonNumber}, quiz={quizNumber}, score={scorePercent}");

        if (backHandler != null)
            backHandler.enabled = false;

        if (hasContext)
        {
            if (ProgressManager.Instance != null)
                ProgressManager.Instance.RecordQuizResult(lessonNumber, quizNumber, scorePercent);
            else
                Debug.LogWarning("[QuizResultReporter] ProgressManager.Instance is null - score not saved.");
        }
        else
        {
            Debug.LogWarning("[QuizResultReporter] No lesson/quiz number available - score not saved. Set the fallback numbers in the Inspector.");
        }

        if (endMenu != null)
        {
            // No next-quiz option: hasNextQuiz is always false. The callback is
            // still required by QuizEndMenu, so it just goes back to the menu.
            endMenu.Show(
                scorePercent,
                hasNextQuiz: false,
                onNextQuiz: GoBackToMenu,
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
        Debug.Log("[QuizResultReporter] QuitWithoutReporting called - no score recorded.");
        SceneManager.LoadScene(returnSceneName);
    }

    private void GoBackToMenu()
    {
        SceneManager.LoadScene(returnSceneName);
    }
}