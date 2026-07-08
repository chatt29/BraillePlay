using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>Which single screen the GameMenu is currently showing. Only one is ever active - see the README's "no dozens of booleans" rule.</summary>
    public enum GameMenuState
    {
        MainMenu,
        LessonPopup,
        Loading
    }

    /// <summary>Which row has focus in MainMenu state.</summary>
    public enum MenuRowSelection
    {
        Guides,
        Lessons
    }

    /// <summary>
    /// The brain of the menu. Owns current state, current guide/lesson
    /// selection, and coordinates every other manager. Never animates UI
    /// itself, never speaks TTS itself, never loads scenes itself - it just
    /// tells GameMenuUI / GameMenuAccessibility / SceneLoader to do those
    /// things, per the README's "Must NOT" list.
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
        [Tooltip("Student's first name for the welcome announcement, if not already handled by CurrentUserDisplay/AccessibilityManager elsewhere.")]
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
            navigator.OnEscape += HandleEscape;
        }

        private void OnDisable()
        {
            navigator.OnUp -= HandleUp;
            navigator.OnDown -= HandleDown;
            navigator.OnLeft -= HandleLeft;
            navigator.OnRight -= HandleRight;
            navigator.OnEnter -= HandleEnter;
            navigator.OnEscape -= HandleEscape;
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

            guideIndex = 0;
            lessonIndex = 0;
            selectedRow = MenuRowSelection.Guides;

            ui.Initialize(
                CurrentGuide, guideIndex > 0, guideIndex < guideDatabase.Count - 1,
                CurrentLesson, lessonIndex > 0, lessonIndex < lessonDatabase.Count - 1,
                startOnGuides: true);

            state = GameMenuState.MainMenu;
            navigator.InputLocked = false;

            if (announceWelcomeOnStart)
                accessibility.AnnounceWelcome(UserSession.StudentNumber);

            accessibility.AnnounceRowSelected(true, CurrentGuide != null ? CurrentGuide.GuideTitle : string.Empty);
        }

        private void ApplyReturnFromQuizIfAny()
        {
            SceneLoader.QuizLaunchContext? pending = SceneLoader.ConsumePendingQuiz();
            if (pending == null || ProgressManager.Instance == null) return;

            // Placeholder hook: the Quiz scene is responsible for calling
            // ProgressManager.Instance.RecordQuizResult(...) with the actual
            // score before returning here. This method exists so
            // GameMenuManager has a clear, single place to react to "we just
            // came back from a quiz" (e.g. re-focus that lesson) if desired.
        }

        private GuideData CurrentGuide => guideDatabase.Get(guideIndex);
        private LessonData CurrentLesson => lessonDatabase.Get(lessonIndex);

        private void HandleUp()
        {
            if (state != GameMenuState.MainMenu || selectedRow == MenuRowSelection.Guides) return;
            SwitchRow(MenuRowSelection.Guides);
        }

        private void HandleDown()
        {
            if (state != GameMenuState.MainMenu || selectedRow == MenuRowSelection.Lessons) return;
            SwitchRow(MenuRowSelection.Lessons);
        }

        private void SwitchRow(MenuRowSelection newRow)
        {
            navigator.InputLocked = true;
            bool movingToLessons = newRow == MenuRowSelection.Lessons;

            ui.PlayRowSwitch(movingToLessons, () =>
            {
                selectedRow = newRow;
                navigator.InputLocked = false;

                string title = movingToLessons
                    ? (CurrentLesson != null ? CurrentLesson.LessonTitle : string.Empty)
                    : (CurrentGuide != null ? CurrentGuide.GuideTitle : string.Empty);

                accessibility.AnnounceRowSelected(!movingToLessons, title);
            });
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
            if (selectedRow == MenuRowSelection.Guides)
            {
                int newIndex = Mathf.Clamp(guideIndex + direction, 0, guideDatabase.Count - 1);
                if (newIndex == guideIndex) return;

                guideIndex = newIndex;
                navigator.InputLocked = true;
                ui.PlayGuideCarousel(CurrentGuide, direction, guideIndex > 0, guideIndex < guideDatabase.Count - 1, () =>
                {
                    navigator.InputLocked = false;
                    accessibility.AnnounceCarouselItem(CurrentGuide != null ? CurrentGuide.GuideTitle : string.Empty);
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
                    accessibility.AnnounceCarouselItem(CurrentLesson != null ? CurrentLesson.LessonTitle : string.Empty);
                });
            }
        }

        private void HandleEnter()
        {
            if (state == GameMenuState.MainMenu)
            {
                if (selectedRow == MenuRowSelection.Guides)
                {
                    if (CurrentGuide != null)
                        sceneLoader.LoadGuide(CurrentGuide.SceneName);
                }
                else
                {
                    if (CurrentLesson == null) return;

                    state = GameMenuState.LessonPopup;
                    navigator.InputLocked = true;
                    lessonPopup.Open(CurrentLesson, this, () => navigator.InputLocked = false);
                }
                return;
            }

            if (state == GameMenuState.LessonPopup)
                lessonPopup.HandleEnter();
        }

        private void HandleEscape()
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