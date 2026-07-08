using System;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Owns everything about the Lesson Popup: which quiz is selected,
    /// reading that quiz's progress, and handing the chosen quiz to
    /// SceneLoader. Input arrives pre-filtered from GameMenuManager (this
    /// class doesn't subscribe to GameMenuNavigator itself, so there's only
    /// ever one place deciding what the current input state means).
    /// </summary>
    public class LessonPopupManager : MonoBehaviour
    {
        [SerializeField] private LessonPopupUI ui;
        [SerializeField] private GameMenuAccessibility accessibility;
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private LessonDatabase lessonDatabase;

        public bool IsOpen { get; private set; }

        private LessonData currentLesson;
        private int currentQuizIndex;

        public void Open(LessonData lesson, MonoBehaviour host, Action onOpened)
        {
            currentLesson = lesson;
            currentQuizIndex = 0;
            IsOpen = true;

            ui.PlayOpen(lesson.LessonTitle, host, () =>
            {
                ShowCurrentQuizImmediate();
                accessibility.AnnounceLessonPopupOpened(lesson.LessonTitle, lesson.QuizCount);
                onOpened?.Invoke();
            });
        }

        public void Close(MonoBehaviour host, Action onClosed)
        {
            IsOpen = false;
            ui.PlayClose(host, () =>
            {
                accessibility.AnnouncePopupClosed();
                onClosed?.Invoke();
            });
        }

        public void HandleLeft(Action onComplete) => Move(-1, onComplete);
        public void HandleRight(Action onComplete) => Move(1, onComplete);

        private void Move(int direction, Action onComplete)
        {
            int newIndex = Mathf.Clamp(currentQuizIndex + direction, 0, currentLesson.QuizCount - 1);
            if (newIndex == currentQuizIndex)
            {
                onComplete?.Invoke();
                return;
            }

            currentQuizIndex = newIndex;

            QuizCardViewData data = BuildViewData(currentQuizIndex);
            bool hasPrevious = currentQuizIndex > 0;
            bool hasNext = currentQuizIndex < currentLesson.QuizCount - 1;

            ui.PlayQuizCarousel(data, direction, hasPrevious, hasNext, () =>
            {
                accessibility.AnnounceQuizFocused(currentQuizIndex + 1, data);
                onComplete?.Invoke();
            });
        }

        public void HandleEnter()
        {
            QuizDefinition quiz = currentLesson.GetQuiz(currentQuizIndex);
            if (quiz == null) return;

            QuizProgress progress = ProgressManager.Instance.GetQuizProgress(currentLesson.LessonNumber, quiz.QuizNumber);
            if (!progress.Unlocked)
            {
                accessibility.AnnounceQuizFocused(currentQuizIndex + 1, BuildViewData(currentQuizIndex));
                return;
            }

            sceneLoader.LoadQuiz(quiz.SceneName, currentLesson.LessonNumber, quiz.QuizNumber);
        }

        private void ShowCurrentQuizImmediate()
        {
            QuizCardViewData data = BuildViewData(currentQuizIndex);
            bool hasPrevious = currentQuizIndex > 0;
            bool hasNext = currentQuizIndex < currentLesson.QuizCount - 1;
            ui.SetQuizImmediate(data, hasPrevious, hasNext);
        }

        private QuizCardViewData BuildViewData(int quizIndex)
        {
            QuizDefinition quiz = currentLesson.GetQuiz(quizIndex);
            if (quiz == null) return default;

            QuizProgress progress = ProgressManager.Instance.GetQuizProgress(currentLesson.LessonNumber, quiz.QuizNumber);

            if (!progress.Unlocked)
            {
                string previousQuizTitle = quizIndex > 0 ? currentLesson.GetQuiz(quizIndex - 1)?.QuizTitle : null;
                string reason = string.IsNullOrEmpty(previousQuizTitle)
                    ? "Complete the previous quiz first."
                    : "Complete " + previousQuizTitle + " first.";
                return QuizCardViewData.Locked_(quiz.QuizTitle, reason);
            }

            return QuizCardViewData.Available(quiz.QuizTitle, progress.Completed, progress.HighestScore);
        }
    }
}