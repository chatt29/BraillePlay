using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// GameMenu spoken feedback using RECORDED VOICE CLIPS only (no TTS).
    /// Sentences are built by chaining clips, e.g.
    /// [Quiz] [3] [Completed] [Score] [85] [percent].
    /// It only speaks - it never changes menu state.
    /// </summary>
    public class GameMenuAccessibility : MonoBehaviour
    {
        [SerializeField] private VoicePlayer voice;

        [Header("Main menu clips")]
        [SerializeField] private AudioClip welcomeClip;          // "Welcome."
        [SerializeField] private AudioClip guidesClip;           // "Guides."
        [SerializeField] private AudioClip lessonsClip;          // "Lessons."
        [SerializeField] private AudioClip customLessonsClip;    // "Custom Lessons."
        [SerializeField] private AudioClip guidesHintClip;       // "Press Left or Right to browse. Press Down for Lessons."
        [SerializeField] private AudioClip lessonsHintClip;      // "Press Left or Right to browse. Press Up for Guides. Press Down for Custom Lessons."
        [SerializeField] private AudioClip customLessonsHintClip;// "Press Enter to open. Press Up for Lessons."

        [Header("Lesson popup clips")]
        [SerializeField] private AudioClip quizzesWordClip;      // "quizzes."
        [SerializeField] private AudioClip popupHintClip;        // "Press Left or Right to browse quizzes. Press Enter to start. Press Backspace to return."
        [SerializeField] private AudioClip quizWordClip;         // "Quiz"
        [SerializeField] private AudioClip lockedClip;           // "Locked."
        [SerializeField] private AudioClip completePreviousClip; // "Complete the previous quiz first."
        [SerializeField] private AudioClip completedClip;        // "Completed."
        [SerializeField] private AudioClip scoreClip;            // "Score"
        [SerializeField] private AudioClip percentClip;          // "percent."
        [SerializeField] private AudioClip notAttemptedClip;     // "Not yet attempted."
        [SerializeField] private AudioClip pressEnterClip;       // "Press Enter to start."
        [SerializeField] private AudioClip closedClip;           // "Closed. Back to the main menu."

        [Header("Numbers")]
        [Tooltip("Element 0 = \"zero\", element 1 = \"one\" ... element 100 = \"one hundred\". Used for quiz numbers, quiz counts and scores.")]
        [SerializeField] private AudioClip[] numberClips = new AudioClip[101];

        private AudioClip Number(int n)
        {
            if (numberClips != null && n >= 0 && n < numberClips.Length) return numberClips[n];
            Debug.LogWarning("[GameMenuAccessibility] No number clip for " + n);
            return null;
        }

        /// <summary>Plays "Welcome." on its own.</summary>
        public void AnnounceWelcome() => voice.Play("Welcome.", welcomeClip);

        /// <summary>
        /// Announces the focused row + its current item. includeWelcome puts
        /// "Welcome." in front of it in the same sentence (a second Play()
        /// call would cut the first one off).
        /// </summary>
        public void AnnounceRowSelected(MenuRowSelection row, GuideData guide, LessonData lesson, bool includeWelcome = false)
        {
            AudioClip welcome = includeWelcome ? welcomeClip : null;
            string welcomeText = includeWelcome ? "Welcome. " : "";

            switch (row)
            {
                case MenuRowSelection.Guides:
                    voice.Play(welcomeText + "Guides. " + guide?.GuideTitle + ". Press Left or Right to browse. Press Down for Lessons.",
                        welcome, guidesClip, guide?.TitleClip, guidesHintClip);
                    break;
                case MenuRowSelection.Lessons:
                    voice.Play(welcomeText + "Lessons. " + lesson?.LessonTitle + ". Press Left or Right to browse. Press Up for Guides. Press Down for Custom Lessons.",
                        welcome, lessonsClip, lesson?.TitleClip, lessonsHintClip);
                    break;
                case MenuRowSelection.CustomLessons:
                    voice.Play("Custom Lessons. Press Enter to open. Press Up for Lessons.",
                        customLessonsClip, customLessonsHintClip);
                    break;
            }
        }

        public void AnnounceCarouselItem(string title, AudioClip titleClip) => voice.Play(title + ".", titleClip);

        public void AnnounceLessonPopupOpened(LessonData lesson, QuizCardViewData firstQuiz)
        {
            int quizNumber = 1;

            if (firstQuiz.Completed)
            {
                int score = Mathf.Clamp(firstQuiz.HighestScorePercent, 0, 100);

                voice.Play(
                    "Lesson " + lesson.LessonTitle +
                    ". Quiz 1. Completed. Your score is " + score +
                    ". Press Enter to start the lesson.",

                    lesson.TitleClip,
                    quizWordClip,
                    Number(quizNumber),
                    completedClip,
                    scoreClip,
                    Number(score),
                    pressEnterClip
                );
            }
            else if (firstQuiz.Locked)
            {
                voice.Play(
                    "Lesson " + lesson.LessonTitle +
                    ". Quiz 1. Locked. " + firstQuiz.LockedReason,

                    lesson.TitleClip,
                    quizWordClip,
                    Number(quizNumber),
                    lockedClip,
                    completePreviousClip
                );
            }
            else
            {
                voice.Play(
                    "Lesson " + lesson.LessonTitle +
                    ". Quiz 1. Not yet attempted. Press Enter to start the lesson.",

                    lesson.TitleClip,
                    quizWordClip,
                    Number(quizNumber),
                    notAttemptedClip,
                    pressEnterClip
                );
            }
        }

        public void AnnounceQuizFocused(int quizNumber, QuizCardViewData data)
        {
            string header = "Quiz " + quizNumber + ".";

            if (data.Locked)
            {
                voice.Play(header + " Locked. " + data.LockedReason,
                    quizWordClip, Number(quizNumber), lockedClip, completePreviousClip);
                return;
            }

            if (data.Completed)
                voice.Play(header + " Completed. Score " + data.HighestScorePercent + " percent. Press Enter to start.",
                    quizWordClip, Number(quizNumber), completedClip, scoreClip, Number(data.HighestScorePercent), percentClip, pressEnterClip);
            else
                voice.Play(header + " Not yet attempted. Press Enter to start.",
                    quizWordClip, Number(quizNumber), notAttemptedClip, pressEnterClip);
        }

        public void AnnouncePopupClosed() => voice.Play("Closed. Back to the main menu.", closedClip);
    }
}