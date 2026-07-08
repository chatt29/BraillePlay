using System;
using TMPro;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// All Lesson Popup visuals: open/close scale+fade, background overlay,
    /// the quiz carousel, and the popup's lesson-title label. No input, no
    /// progress lookups - LessonPopupManager tells it what to show.
    /// </summary>
    public class LessonPopupUI : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private RectTransform popupBox;
        [SerializeField] private CanvasGroup popupGroup;
        [SerializeField] private CanvasGroup backgroundOverlay;
        [SerializeField] private TMP_Text lessonTitleLabel;
        [SerializeField] private QuizCard quizCard;

        [Header("Open/Close Animation")]
        [SerializeField] private float openDuration = 0.3f;
        [Tooltip("Ease Out Back-style curve per the README's Popup Open Animation requirement.")]
        [SerializeField] private AnimationCurve openEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Vector3 openStartScale = new Vector3(0.85f, 0.85f, 0.85f);

        public void PlayOpen(string lessonTitle, MonoBehaviour host, Action onComplete)
        {
            gameObject.SetActive(true);
            if (lessonTitleLabel != null) lessonTitleLabel.text = lessonTitle;

            StartCoroutine(UIAnimator.RunParallel(host, onComplete,
                UIAnimator.ScaleTo(popupBox, openStartScale, Vector3.one, openDuration, openEase),
                UIAnimator.FadeCanvasGroup(popupGroup, 0f, 1f, openDuration, openEase),
                UIAnimator.FadeCanvasGroup(backgroundOverlay, 0f, 1f, openDuration, openEase)));
        }

        public void PlayClose(MonoBehaviour host, Action onComplete)
        {
            StartCoroutine(UIAnimator.RunParallel(host, () =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            },
                UIAnimator.ScaleTo(popupBox, Vector3.one, openStartScale, openDuration, openEase),
                UIAnimator.FadeCanvasGroup(popupGroup, 1f, 0f, openDuration, openEase),
                UIAnimator.FadeCanvasGroup(backgroundOverlay, 1f, 0f, openDuration, openEase)));
        }

        public void SetQuizImmediate(QuizCardViewData data, bool hasPrevious, bool hasNext)
        {
            quizCard.SetImmediate(data, hasPrevious, hasNext);
        }

        public void PlayQuizCarousel(QuizCardViewData data, int direction, bool hasPrevious, bool hasNext, Action onComplete)
        {
            quizCard.PlayCardTransition(this, data, direction, hasPrevious, hasNext, onComplete);
        }
    }
}