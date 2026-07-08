using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// GameMenu-specific spoken phrasing, built on top of the project's
    /// shared AccessibilityManager (never replaces it - matches the
    /// "Accessibility never controls gameplay" rule: this class only speaks,
    /// it never changes menu state itself).
    /// </summary>
    public class GameMenuAccessibility : MonoBehaviour
    {
        public void AnnounceWelcome(string firstName)
        {
            AccessibilityManager.Instance.Announce(string.IsNullOrEmpty(firstName) ? "Welcome." : "Welcome, " + firstName + ".");
        }

        public void AnnounceRowSelected(bool isGuidesRow, string currentItemTitle)
        {
            string rowName = isGuidesRow ? "Guides." : "Lessons.";
            string hint = isGuidesRow
                ? " Press Left or Right to browse. Press Down for Lessons."
                : " Press Left or Right to browse. Press Up for Guides.";

            AccessibilityManager.Instance.Announce(rowName + " " + currentItemTitle + "." + hint);
        }

        public void AnnounceCarouselItem(string title)
        {
            AccessibilityManager.Instance.Announce(title + ".");
        }

        public void AnnounceLessonPopupOpened(string lessonTitle, int quizCount)
        {
            AccessibilityManager.Instance.Announce(
                lessonTitle + ". " + quizCount + " quizzes. Press Left or Right to browse quizzes. Press Enter to start. Press Escape to return.");
        }

        public void AnnounceQuizFocused(int quizNumber, QuizCardViewData data)
        {
            string header = "Quiz " + quizNumber + ".";

            if (data.Locked)
            {
                AccessibilityManager.Instance.Announce(header + " Locked. " + data.LockedReason);
                return;
            }

            string status = data.Completed
                ? "Completed. Score " + data.HighestScorePercent + " percent."
                : "Not yet attempted.";

            AccessibilityManager.Instance.Announce(header + " " + status + " Press Enter to start.");
        }

        public void AnnouncePopupClosed()
        {
            AccessibilityManager.Instance.Announce("Closed. Back to the main menu.");
        }
    }
}