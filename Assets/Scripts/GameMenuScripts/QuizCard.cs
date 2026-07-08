using System;
using TMPro;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Everything a QuizCard needs to draw itself - a plain view-model so
    /// LessonPopupUI never has to hand QuizCard a live QuizProgress/data
    /// object. Keeps QuizCard reusable and easy to test in isolation.
    /// </summary>
    public struct QuizCardViewData
    {
        public string QuizTitle;
        public bool Locked;
        public string LockedReason;   // e.g. "Complete Quiz Three first." - only used when Locked.
        public bool Completed;
        public int HighestScorePercent;

        public static QuizCardViewData Locked_(string title, string lockedReason) => new QuizCardViewData
        {
            QuizTitle = title,
            Locked = true,
            LockedReason = lockedReason
        };

        public static QuizCardViewData Available(string title, bool completed, int highestScorePercent) => new QuizCardViewData
        {
            QuizTitle = title,
            Locked = false,
            Completed = completed,
            HighestScorePercent = highestScorePercent
        };
    }

    /// <summary>
    /// Visuals for a single quiz card inside the Lesson Popup - title plus
    /// a status line that reads "Score N%", "Completed", or "Locked -
    /// Complete Quiz X first" depending on progress. No input, no
    /// Firestore/progress lookups: purely a renderer for QuizCardViewData.
    /// </summary>
    public class QuizCard : MonoBehaviour
    {
        [SerializeField] private CarouselPanel carousel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statusLabel;

        public void SetImmediate(QuizCardViewData data, bool hasPrevious, bool hasNext)
        {
            Render(data);
            carousel.SetArrowsImmediate(hasPrevious, hasNext);
        }

        public void PlayCardTransition(MonoBehaviour host, QuizCardViewData data, int direction, bool hasPrevious, bool hasNext, Action onComplete)
        {
            carousel.PlayTransition(host, direction, () => Render(data), hasPrevious, hasNext, onComplete);
        }

        private void Render(QuizCardViewData data)
        {
            if (titleLabel != null)
                titleLabel.text = data.QuizTitle;

            if (statusLabel == null) return;

            if (data.Locked)
                statusLabel.text = "Locked\n" + data.LockedReason;
            else if (data.Completed)
                statusLabel.text = "Completed\nScore " + data.HighestScorePercent + "%";
            else
                statusLabel.text = "Not yet attempted";
        }
    }
}