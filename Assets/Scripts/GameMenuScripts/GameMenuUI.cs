using System;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// All main-menu visuals: which row is focused, and the Guide/Lesson
    /// title carousels. Never reads input, never touches Firestore/progress -
    /// GameMenuManager tells it what to show and it shows it.
    ///
    /// Each row has two fixed positions: its resting "focused" spot, and an
    /// "unfocused" spot shifted away from the other row (guide shifts up,
    /// lesson shifts down). Switching focus always moves a row between
    /// these two known points - never toward the other row - so the rows
    /// can't visually collide or converge no matter the distance value.
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        [Header("Rows")]
        [SerializeField] private MenuRow guideRow;
        [SerializeField] private MenuRow lessonRow;
        [SerializeField] private RectTransform guideRowRoot;
        [SerializeField] private RectTransform lessonRowRoot;
        [SerializeField] private CanvasGroup guideRowGroup;
        [SerializeField] private CanvasGroup lessonRowGroup;

        [Header("Row Switch Animation")]
        [SerializeField] private float rowSwitchDistance = 2f;
        [SerializeField] private float rowSwitchDuration = 0.25f;
        [SerializeField] private AnimationCurve rowSwitchEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("Alpha of the row that just lost focus - dimmed, not hidden, since both rows stay on screen.")]
        [Range(0f, 1f)][SerializeField] private float unfocusedRowAlpha = 0.45f;

        private Vector2 guideFocusedPos, guideUnfocusedPos;
        private Vector2 lessonFocusedPos, lessonUnfocusedPos;
        private bool positionsCaptured;

        private void CapturePositions()
        {
            if (positionsCaptured) return;

            guideFocusedPos = guideRowRoot.anchoredPosition;
            guideUnfocusedPos = guideFocusedPos + new Vector2(0f, rowSwitchDistance); // guide's "away" = up, away from Lessons below it

            lessonFocusedPos = lessonRowRoot.anchoredPosition;
            lessonUnfocusedPos = lessonFocusedPos + new Vector2(0f, -rowSwitchDistance); // lesson's "away" = down, away from Guides above it

            positionsCaptured = true;
        }

        /// <summary>Sets up the very first frame of the menu with no animation.</summary>
        public void Initialize(GuideData firstGuide, bool guideHasPrev, bool guideHasNext,
                                LessonData firstLesson, bool lessonHasPrev, bool lessonHasNext,
                                bool startOnGuides)
        {
            CapturePositions();

            guideRow.SetImmediate(firstGuide != null ? firstGuide.GuideTitle : string.Empty, guideHasPrev, guideHasNext);
            lessonRow.SetImmediate(firstLesson != null ? firstLesson.LessonTitle : string.Empty, lessonHasPrev, lessonHasNext);

            guideRowRoot.anchoredPosition = startOnGuides ? guideFocusedPos : guideUnfocusedPos;
            lessonRowRoot.anchoredPosition = startOnGuides ? lessonUnfocusedPos : lessonFocusedPos;

            guideRowGroup.alpha = startOnGuides ? 1f : unfocusedRowAlpha;
            lessonRowGroup.alpha = startOnGuides ? unfocusedRowAlpha : 1f;
        }

        /// <summary>Plays the vertical focus-switch animation between the Guide and Lesson rows.</summary>
        public void PlayRowSwitch(bool movingToLessons, Action onComplete)
        {
            CapturePositions();

            if (movingToLessons)
            {
                StartCoroutine(UIAnimatorParallel(onComplete,
                    UIAnimator.MoveAnchored(guideRowRoot, guideFocusedPos, guideUnfocusedPos, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.FadeCanvasGroup(guideRowGroup, 1f, unfocusedRowAlpha, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.MoveAnchored(lessonRowRoot, lessonUnfocusedPos, lessonFocusedPos, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.FadeCanvasGroup(lessonRowGroup, unfocusedRowAlpha, 1f, rowSwitchDuration, rowSwitchEase)));
            }
            else
            {
                StartCoroutine(UIAnimatorParallel(onComplete,
                    UIAnimator.MoveAnchored(lessonRowRoot, lessonFocusedPos, lessonUnfocusedPos, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.FadeCanvasGroup(lessonRowGroup, 1f, unfocusedRowAlpha, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.MoveAnchored(guideRowRoot, guideUnfocusedPos, guideFocusedPos, rowSwitchDuration, rowSwitchEase),
                    UIAnimator.FadeCanvasGroup(guideRowGroup, unfocusedRowAlpha, 1f, rowSwitchDuration, rowSwitchEase)));
            }
        }

        private System.Collections.IEnumerator UIAnimatorParallel(Action onComplete, params System.Collections.IEnumerator[] routines)
        {
            yield return UIAnimator.RunParallel(this, onComplete, routines);
        }

        public void PlayGuideCarousel(GuideData newGuide, int direction, bool hasPrevious, bool hasNext, Action onComplete)
        {
            guideRow.PlayTitleTransition(this, newGuide != null ? newGuide.GuideTitle : string.Empty, direction, hasPrevious, hasNext, onComplete);
        }

        public void PlayLessonCarousel(LessonData newLesson, int direction, bool hasPrevious, bool hasNext, Action onComplete)
        {
            lessonRow.PlayTitleTransition(this, newLesson != null ? newLesson.LessonTitle : string.Empty, direction, hasPrevious, hasNext, onComplete);
        }
    }
}