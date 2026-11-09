using UnityEngine;

namespace BraillePlay.GameMenu
{
    public enum GameMenuState
    {
        MainMenu,
        LessonPopup,
        Loading
    }

    /// <summary>Which row/button has focus in MainMenu state, top to bottom.</summary>
    public enum MenuRowSelection
    {
        Guides,
        Lessons,
        CustomLessons
    }

    /// <summary>
    /// The brain of the menu. Owns state + selection and coordinates the
    /// other managers. Never animates, speaks, or loads scenes itself.
    /// </summary>
    public class GameMenuManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GuideDatabase guideDatabase;
        [SerializeField] private LessonDatabase lessonDatabase;

        [Header("Collaborators")]
        [SerializeField] private GameMenuNavigator navigator;
        [SerializeField] private GameMenuUI ui;
        [SerializeField] private GameMenuAccessibility accessibility;
        [SerializeField] private LessonPopupManager lessonPopup;
        [SerializeField] private SceneLoader sceneLoader;

        [Header("Startup")]
        [SerializeField] private bool announceWelcomeOnStart = true;

        private GameMenuState state = GameMenuState.Loading;
        private MenuRowSelection selectedRow = MenuRowSelection.Guides;

        private int guideIndex;
        private int lessonIndex;

        private void OnEnable()
        {
            navigator.OnUp += HandleUp;
            navigator.OnDown += HandleDown;
            navigator.OnLeft += HandleLeft;
            navigator.OnRight += HandleRight;
            navigator.OnEnter += HandleEnter;
            navigator.OnBack += HandleBack;
            ui.OnCustomLessonsClicked += HandleCustomLessonsClicked;
        }

        private void OnDisable()
        {
            navigator.OnUp -= HandleUp;
            navigator.OnDown -= HandleDown;
            navigator.OnLeft -= HandleLeft;
            navigator.OnRight -= HandleRight;
            navigator.OnEnter -= HandleEnter;
            navigator.OnBack -= HandleBack;
            ui.OnCustomLessonsClicked -= HandleCustomLessonsClicked;
        }

        private void Start()
        {
            state = GameMenuState.Loading;
            navigator.InputLocked = true;

            if (ProgressManager.Instance != null && !ProgressManager.Instance.IsLoaded)
                ProgressManager.Instance.OnProgressLoaded += BeginMenu;
            else
                BeginMenu();
        }

        private void BeginMenu()
        {
            if (ProgressManager.Instance != null)
                ProgressManager.Instance.OnProgressLoaded -= BeginMenu;

            ApplyReturnFromQuizIfAny();

            bool returningFromLesson = MenuReturn.Consume(out int finishedLessonNumber);

            guideIndex = 0;
            lessonIndex = returningFromLesson ? FindLessonIndex(finishedLessonNumber) : 0;
            selectedRow = returningFromLesson ? MenuRowSelection.Lessons : MenuRowSelection.Guides;

            ui.Initialize(
                CurrentGuide, guideIndex > 0, guideIndex < guideDatabase.Count - 1,
                CurrentLesson, lessonIndex > 0, lessonIndex < lessonDatabase.Count - 1,
                startOnGuides: !returningFromLesson);

            state = GameMenuState.MainMenu;
            navigator.InputLocked = false;

            // One combined sentence so "Welcome." isn't cut off by the row announcement.
            bool welcome = announceWelcomeOnStart && !returningFromLesson;
            accessibility.AnnounceRowSelected(selectedRow, CurrentGuide, CurrentLesson, includeWelcome: welcome);
        }

        private int FindLessonIndex(int lessonNumber)
        {
            for (int i = 0; i < lessonDatabase.Count; i++)
            {
                LessonData lesson = lessonDatabase.Get(i);
                if (lesson != null && lesson.LessonNumber == lessonNumber)
                    return i;
            }

            return 0;
        }

        private void ApplyReturnFromQuizIfAny()
        {
            SceneLoader.QuizLaunchContext? pending = SceneLoader.ConsumePendingQuiz();
            if (pending == null || ProgressManager.Instance == null) return;
        }

        private GuideData CurrentGuide => guideDatabase.Get(guideIndex);
        private LessonData CurrentLesson => lessonDatabase.Get(lessonIndex);

        private void HandleUp()
        {
            if (state != GameMenuState.MainMenu) return;

            if (selectedRow == MenuRowSelection.Lessons) SwitchRow(MenuRowSelection.Guides);
            else if (selectedRow == MenuRowSelection.CustomLessons) SwitchRow(MenuRowSelection.Lessons);
        }

        private void HandleDown()
        {
            if (state != GameMenuState.MainMenu) return;

            if (selectedRow == MenuRowSelection.Guides) SwitchRow(MenuRowSelection.Lessons);
            else if (selectedRow == MenuRowSelection.Lessons) SwitchRow(MenuRowSelection.CustomLessons);
        }

        private void SwitchRow(MenuRowSelection newRow)
        {
            MenuRowSelection oldRow = selectedRow;
            navigator.InputLocked = true;

            System.Action done = () =>
            {
                selectedRow = newRow;
                navigator.InputLocked = false;
                accessibility.AnnounceRowSelected(newRow, CurrentGuide, CurrentLesson);
            };

            bool involvesCustomButton = oldRow == MenuRowSelection.CustomLessons || newRow == MenuRowSelection.CustomLessons;

            if (involvesCustomButton)
                ui.PlayCustomFocus(newRow == MenuRowSelection.CustomLessons, done);
            else
                ui.PlayRowSwitch(newRow == MenuRowSelection.Lessons, done);
        }

        private void HandleLeft()
        {
            if (state == GameMenuState.MainMenu) { MoveSelection(-1); return; }
            if (state == GameMenuState.LessonPopup) { navigator.InputLocked = true; lessonPopup.HandleLeft(() => navigator.InputLocked = false); }
        }

        private void HandleRight()
        {
            if (state == GameMenuState.MainMenu) { MoveSelection(1); return; }
            if (state == GameMenuState.LessonPopup) { navigator.InputLocked = true; lessonPopup.HandleRight(() => navigator.InputLocked = false); }
        }

        private void MoveSelection(int direction)
        {
            // The Custom Lessons button has nothing to browse.
            if (selectedRow == MenuRowSelection.CustomLessons) return;

            if (selectedRow == MenuRowSelection.Guides)
            {
                int newIndex = Mathf.Clamp(guideIndex + direction, 0, guideDatabase.Count - 1);
                if (newIndex == guideIndex) return;

                guideIndex = newIndex;
                navigator.InputLocked = true;
                ui.PlayGuideCarousel(CurrentGuide, direction, guideIndex > 0, guideIndex < guideDatabase.Count - 1, () =>
                {
                    navigator.InputLocked = false;
                    accessibility.AnnounceCarouselItem(CurrentGuide?.GuideTitle, CurrentGuide?.TitleClip);
                });
            }
            else
            {
                int newIndex = Mathf.Clamp(lessonIndex + direction, 0, lessonDatabase.Count - 1);
                if (newIndex == lessonIndex) return;

                lessonIndex = newIndex;
                navigator.InputLocked = true;
                ui.PlayLessonCarousel(CurrentLesson, direction, lessonIndex > 0, lessonIndex < lessonDatabase.Count - 1, () =>
                {
                    navigator.InputLocked = false;
                    accessibility.AnnounceCarouselItem(CurrentLesson?.LessonTitle, CurrentLesson?.TitleClip);
                });
            }
        }

        private void HandleEnter()
        {
            if (state == GameMenuState.MainMenu)
            {
                switch (selectedRow)
                {
                    case MenuRowSelection.Guides:
                        if (CurrentGuide != null)
                            sceneLoader.LoadGuide(CurrentGuide.SceneName);
                        break;

                    case MenuRowSelection.Lessons:
                        if (CurrentLesson == null) return;
                        state = GameMenuState.LessonPopup;
                        navigator.InputLocked = true;
                        lessonPopup.Open(CurrentLesson, this, () => navigator.InputLocked = false);
                        break;

                    case MenuRowSelection.CustomLessons:
                        OpenCustomLessons();
                        break;
                }
                return;
            }

            if (state == GameMenuState.LessonPopup)
                lessonPopup.HandleEnter();
        }

        /// <summary>Mouse/touch click on the button (teachers monitoring the app).</summary>
        private void HandleCustomLessonsClicked()
        {
            if (state != GameMenuState.MainMenu) return;
            OpenCustomLessons();
        }

        private void OpenCustomLessons()
        {
            state = GameMenuState.Loading;
            navigator.InputLocked = true;
            sceneLoader.LoadCustomLessons();
        }

        private void HandleBack()
        {
            if (state != GameMenuState.LessonPopup) return;

            navigator.InputLocked = true;
            lessonPopup.Close(this, () =>
            {
                state = GameMenuState.MainMenu;
                navigator.InputLocked = false;
            });
        }
    }
}