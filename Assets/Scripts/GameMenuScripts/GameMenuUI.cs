using System;
using UnityEngine;
using UnityEngine.UI;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// All main-menu visuals: which row is focused, the Guide/Lesson title
    /// carousels, and the Custom Lessons button. Never reads input.
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

        [Header("Custom Lessons Button")]
        [SerializeField] private Button customLessonsButton;
        [Tooltip("Any GameObject that shows the button is focused (outline/glow image). Shown only while the button is selected by keyboard/braille.")]
        [SerializeField] private GameObject customLessonsFocusFrame;

        [Header("Row Switch Animation")]
        [SerializeField] private float rowSwitchDistance = 2f;
        [SerializeField] private float rowSwitchDuration = 0.25f;
        [SerializeField] private AnimationCurve rowSwitchEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0f, 1f)][SerializeField] private float unfocusedRowAlpha = 0.45f;

        /// <summary>Raised when a teacher/student clicks the button with a mouse or touch.</summary>
        public event Action OnCustomLessonsClicked;

        private Vector2 guideFocusedPos, guideUnfocusedPos;
        private Vector2 lessonFocusedPos, lessonUnfocusedPos;
        private bool positionsCaptured;

        private void Awake()
        {
            if (customLessonsButton != null)
                customLessonsButton.onClick.AddListener(() => OnCustomLessonsClicked?.Invoke());

            SetCustomFocus(false);
        }

        private void SetCustomFocus(bool focused)
        {
            if (customLessonsFocusFrame != null) customLessonsFocusFrame.SetActive(focused);
        }

        private void CapturePositions()
        {
            if (positionsCaptured) return;

            guideFocusedPos = guideRowRoot.anchoredPosition;
            guideUnfocusedPos = guideFocusedPos + new Vector2(0f, rowSwitchDistance);

            lessonFocusedPos = lessonRowRoot.anchoredPosition;
            lessonUnfocusedPos = lessonFocusedPos + new Vector2(0f, -rowSwitchDistance);

            positionsCaptured = true;
        }

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

            SetCustomFocus(false);
        }

        /// <summary>Vertical focus-switch animation between the Guide and Lesson rows.</summary>
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

        /// <summary>
        /// Moves focus Lessons -> Custom Lessons button (focusCustom = true)
        /// or back (false). The rows don't move; the Lessons row just dims
        /// while the button shows its focus frame.
        /// </summary>
        public void PlayCustomFocus(bool focusCustom, Action onComplete)
        {
            SetCustomFocus(focusCustom);

            float from = focusCustom ? 1f : unfocusedRowAlpha;
            float to = focusCustom ? unfocusedRowAlpha : 1f;

            StartCoroutine(UIAnimatorParallel(onComplete,
                UIAnimator.FadeCanvasGroup(lessonRowGroup, from, to, rowSwitchDuration, rowSwitchEase)));
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