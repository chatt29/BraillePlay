using System;
using System.Collections;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Reusable "modern carousel" building block: slides its content root
    /// out one side while fading, lets the caller repopulate the content,
    /// then slides/fades it back in from the other side. Also fades its
    /// left/right arrows based on whether there's a previous/next item.
    ///
    /// Used by both MenuRow (Guide/Lesson titles) and QuizCard (quiz
    /// popup), so the horizontal-carousel animation only exists once.
    /// </summary>
    public class CarouselPanel : MonoBehaviour
    {
        [Header("Content")]
        [Tooltip("The RectTransform that slides. Should contain everything that changes per item (title, score text, etc).")]
        public RectTransform contentRoot;
        public CanvasGroup contentGroup;

        [Header("Arrows")]
        public CanvasGroup leftArrowGroup;
        public CanvasGroup rightArrowGroup;
        [Tooltip("Alpha used for a disabled/unavailable arrow.")]
        [Range(0f, 1f)] public float disabledArrowAlpha = 0.25f;

        [Header("Animation")]
        public float slideDistance = 120f;
        public float slideDuration = 0.25f;
        public AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector2 restingPosition;
        private bool initialized;

        private void EnsureInitialized()
        {
            if (initialized) return;
            if (contentRoot != null)
                restingPosition = contentRoot.anchoredPosition;
            initialized = true;
        }

        /// <summary>Sets arrow availability instantly (no animation) - use for first-load state.</summary>
        public void SetArrowsImmediate(bool hasPrevious, bool hasNext)
        {
            if (leftArrowGroup != null) leftArrowGroup.alpha = hasPrevious ? 1f : disabledArrowAlpha;
            if (rightArrowGroup != null) rightArrowGroup.alpha = hasNext ? 1f : disabledArrowAlpha;
        }

        /// <summary>
        /// Plays a full carousel transition: current content slides/fades
        /// out in <paramref name="direction"/> (-1 = came from left / exits
        /// right is reversed depending on caller's convention - see Move
        /// below), <paramref name="populate"/> updates the now-off-screen
        /// content, then it slides/fades back in from the opposite side.
        /// </summary>
        public void PlayTransition(MonoBehaviour host, int direction, Action populate, bool hasPrevious, bool hasNext, Action onComplete)
        {
            EnsureInitialized();
            host.StartCoroutine(TransitionRoutine(host, direction, populate, hasPrevious, hasNext, onComplete));
        }

        private IEnumerator TransitionRoutine(MonoBehaviour host, int direction, Action populate, bool hasPrevious, bool hasNext, Action onComplete)
        {
            // direction > 0 means "moving right" (RIGHT pressed): old content exits to the left, new content enters from the right.
            Vector2 exitOffset = new Vector2(-direction * slideDistance, 0f);
            Vector2 enterOffset = new Vector2(direction * slideDistance, 0f);

            yield return UIAnimator.RunParallel(host, null,
                UIAnimator.MoveAnchored(contentRoot, restingPosition, restingPosition + exitOffset, slideDuration * 0.5f, slideEase),
                UIAnimator.FadeCanvasGroup(contentGroup, 1f, 0f, slideDuration * 0.5f, slideEase));

            populate?.Invoke();
            SetArrowsImmediate(hasPrevious, hasNext);

            contentRoot.anchoredPosition = restingPosition + enterOffset;

            yield return UIAnimator.RunParallel(host, onComplete,
                UIAnimator.MoveAnchored(contentRoot, restingPosition + enterOffset, restingPosition, slideDuration * 0.5f, slideEase),
                UIAnimator.FadeCanvasGroup(contentGroup, 0f, 1f, slideDuration * 0.5f, slideEase));
        }
    }
}