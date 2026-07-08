using System;
using TMPro;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Visuals for one row of the main menu (the Guide row or the Lesson
    /// row) - just a title label and a <see cref="CarouselPanel"/> for the
    /// left/right horizontal transition. No input, no data lookups: it only
    /// displays whatever GameMenuUI tells it to.
    /// </summary>
    public class MenuRow : MonoBehaviour
    {
        [SerializeField] private CarouselPanel carousel;
        [SerializeField] private TMP_Text titleLabel;

        /// <summary>Sets the title with no animation - use once on menu load.</summary>
        public void SetImmediate(string title, bool hasPrevious, bool hasNext)
        {
            if (titleLabel != null) titleLabel.text = title;
            carousel.SetArrowsImmediate(hasPrevious, hasNext);
        }

        /// <summary>Plays the left/right carousel transition to a new title.</summary>
        public void PlayTitleTransition(MonoBehaviour host, string newTitle, int direction, bool hasPrevious, bool hasNext, Action onComplete)
        {
            carousel.PlayTransition(host, direction, () =>
            {
                if (titleLabel != null) titleLabel.text = newTitle;
            }, hasPrevious, hasNext, onComplete);
        }
    }
}