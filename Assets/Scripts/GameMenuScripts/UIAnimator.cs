using System;
using System.Collections;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Small, dependency-free tweening toolkit shared by every animated
    /// GameMenu element (row switch, carousels, popup open/close). Nothing
    /// here knows about menus, quizzes, or input - it only moves numbers
    /// over time - so it stays reusable and avoids duplicating the same
    /// coroutine-lerp code in five different UI scripts.
    ///
    /// All durations/easing are passed in by the caller (usually serialized
    /// AnimationCurve + float fields on GameMenuUI/LessonPopupUI), keeping
    /// every animation Inspector-configurable per the README.
    /// </summary>
    public static class UIAnimator
    {
        public static IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, AnimationCurve ease)
        {
            if (group == null) yield break;

            group.alpha = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = ease.Evaluate(Mathf.Clamp01(t / duration));
                group.alpha = Mathf.LerpUnclamped(from, to, p);
                yield return null;
            }
            group.alpha = to;
        }

        public static IEnumerator MoveAnchored(RectTransform rect, Vector2 from, Vector2 to, float duration, AnimationCurve ease)
        {
            if (rect == null) yield break;

            rect.anchoredPosition = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = ease.Evaluate(Mathf.Clamp01(t / duration));
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, p);
                yield return null;
            }
            rect.anchoredPosition = to;
        }

        public static IEnumerator ScaleTo(RectTransform rect, Vector3 from, Vector3 to, float duration, AnimationCurve ease)
        {
            if (rect == null) yield break;

            rect.localScale = from;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = ease.Evaluate(Mathf.Clamp01(t / duration));
                rect.localScale = Vector3.LerpUnclamped(from, to, p);
                yield return null;
            }
            rect.localScale = to;
        }

        /// <summary>
        /// Runs several coroutines at once on <paramref name="host"/> and
        /// invokes <paramref name="onComplete"/> once every one of them has
        /// finished. Used so "slide + fade" or "popup scale + overlay fade"
        /// play together instead of one after another.
        /// </summary>
        public static IEnumerator RunParallel(MonoBehaviour host, Action onComplete, params IEnumerator[] routines)
        {
            int remaining = routines.Length;

            if (remaining == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            foreach (IEnumerator routine in routines)
            {
                host.StartCoroutine(Wrap(routine, () => remaining--));
            }

            while (remaining > 0)
                yield return null;

            onComplete?.Invoke();
        }

        private static IEnumerator Wrap(IEnumerator routine, Action onDone)
        {
            yield return routine;
            onDone?.Invoke();
        }
    }
}