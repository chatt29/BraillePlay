using UnityEngine;
using UnityEngine.UI;

public class BeginnerAlphabetMenuController : MonoBehaviour
{
    public enum MenuType
    {
        Learn,
        Quiz,
        JumbleLetters,
        BasicWords
    }

    public enum FocusArea
    {
        MainMenu,
        ContentPanel
    }

    [Header("Current Menu")]
    public MenuType currentMenu = MenuType.Learn;

    [Header("Current Focus")]
    public FocusArea currentFocus = FocusArea.MainMenu;

    [Header("Arrow Indicator")]
    public RectTransform arrowIndicator;

    [Header("Menu Buttons")]
    public RectTransform learnBtn;
    public RectTransform quizBtn;
    public RectTransform jumbleLettersBtn;
    public RectTransform basicWordsBtn;

    [Header("Lock States (optional for locked modes)")]
    public LockableUIButton quizLock;
    public LockableUIButton jumbleLettersLock;
    public LockableUIButton basicWordsLock;

    [Header("Content Groups")]
    public GameObject learnContent;
    public GameObject quizContent;
    public GameObject jumbleLettersContent;
    public GameObject basicWordsContent;

    [Header("Content Buttons")]
    public RectTransform[] learnButtons;
    public RectTransform[] quizButtons;
    public RectTransform[] jumbleLettersButtons;
    public RectTransform[] basicWordsButtons;

    [Header("Hover Effect")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.08f, 1.08f, 1f);

    private int currentContentIndex = 0;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnDeleteOrNo += HandlePrevious;
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnRepeat += HandleCancel;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnDeleteOrNo -= HandlePrevious;
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnRepeat -= HandleCancel;
    }

    private void Start()
    {
        RefreshMenu();
    }

    public void RefreshMenu()
    {
        UpdateMenuUI();
        MoveArrowToCurrentMenu();
        UpdateContentHover();
    }

    public void SelectLearn()
    {
        currentMenu = MenuType.Learn;
        RefreshMenu();
    }

    public void SelectQuiz()
    {
        if (quizLock != null && !quizLock.IsUnlocked()) return;

        currentMenu = MenuType.Quiz;
        RefreshMenu();
    }

    public void SelectJumbleLetters()
    {
        if (jumbleLettersLock != null && !jumbleLettersLock.IsUnlocked()) return;

        currentMenu = MenuType.JumbleLetters;
        RefreshMenu();
    }

    public void SelectBasicWords()
    {
        if (basicWordsLock != null && !basicWordsLock.IsUnlocked()) return;

        currentMenu = MenuType.BasicWords;
        RefreshMenu();
    }

    public void SelectNextUnlockedMenu()
    {
        MenuType[] order = { MenuType.Learn, MenuType.Quiz, MenuType.JumbleLetters, MenuType.BasicWords };

        int currentIndex = System.Array.IndexOf(order, currentMenu);

        for (int i = 1; i <= order.Length; i++)
        {
            int nextIndex = (currentIndex + i) % order.Length;
            MenuType candidate = order[nextIndex];

            if (IsMenuUnlocked(candidate))
            {
                currentMenu = candidate;
                RefreshMenu();
                return;
            }
        }
    }

    public void SelectPreviousUnlockedMenu()
    {
        MenuType[] order = { MenuType.Learn, MenuType.Quiz, MenuType.JumbleLetters, MenuType.BasicWords };

        int currentIndex = System.Array.IndexOf(order, currentMenu);

        for (int i = 1; i <= order.Length; i++)
        {
            int prevIndex = (currentIndex - i + order.Length) % order.Length;
            MenuType candidate = order[prevIndex];

            if (IsMenuUnlocked(candidate))
            {
                currentMenu = candidate;
                RefreshMenu();
                return;
            }
        }
    }

    private bool IsMenuUnlocked(MenuType menu)
    {
        switch (menu)
        {
            case MenuType.Learn:
                return true;

            case MenuType.Quiz:
                return quizLock == null || quizLock.IsUnlocked();

            case MenuType.JumbleLetters:
                return jumbleLettersLock == null || jumbleLettersLock.IsUnlocked();

            case MenuType.BasicWords:
                return basicWordsLock == null || basicWordsLock.IsUnlocked();
        }

        return false;
    }

    private void UpdateMenuUI()
    {
        if (learnContent != null) learnContent.SetActive(currentMenu == MenuType.Learn);
        if (quizContent != null) quizContent.SetActive(currentMenu == MenuType.Quiz);
        if (jumbleLettersContent != null) jumbleLettersContent.SetActive(currentMenu == MenuType.JumbleLetters);
        if (basicWordsContent != null) basicWordsContent.SetActive(currentMenu == MenuType.BasicWords);
    }

    private void MoveArrowToCurrentMenu()
    {
        if (arrowIndicator == null) return;

        RectTransform target = GetCurrentMenuButton();
        if (target == null) return;

        Vector2 pos = arrowIndicator.anchoredPosition;
        pos.y = target.anchoredPosition.y;
        arrowIndicator.anchoredPosition = pos;
    }

    private RectTransform GetCurrentMenuButton()
    {
        switch (currentMenu)
        {
            case MenuType.Learn:
                return learnBtn;
            case MenuType.Quiz:
                return quizBtn;
            case MenuType.JumbleLetters:
                return jumbleLettersBtn;
            case MenuType.BasicWords:
                return basicWordsBtn;
        }

        return learnBtn;
    }

    private RectTransform[] GetCurrentContentButtons()
    {
        switch (currentMenu)
        {
            case MenuType.Learn:
                return learnButtons;
            case MenuType.Quiz:
                return quizButtons;
            case MenuType.JumbleLetters:
                return jumbleLettersButtons;
            case MenuType.BasicWords:
                return basicWordsButtons;
        }

        return null;
    }

    private void HandleNext()
    {
        if (currentFocus == FocusArea.MainMenu)
        {
            SelectNextUnlockedMenu();
        }
        else
        {
            RectTransform[] buttons = GetCurrentContentButtons();
            if (buttons == null || buttons.Length == 0) return;

            currentContentIndex = (currentContentIndex + 1) % buttons.Length;
            UpdateContentHover();
        }
    }

    private void HandlePrevious()
    {
        if (currentFocus == FocusArea.MainMenu)
        {
            SelectPreviousUnlockedMenu();
        }
        else
        {
            RectTransform[] buttons = GetCurrentContentButtons();
            if (buttons == null || buttons.Length == 0) return;

            currentContentIndex = (currentContentIndex - 1 + buttons.Length) % buttons.Length;
            UpdateContentHover();
        }
    }

    private void HandleSubmit()
    {
        if (currentFocus == FocusArea.MainMenu)
        {
            RectTransform[] buttons = GetCurrentContentButtons();
            if (buttons == null || buttons.Length == 0)
            {
                Debug.Log("No content buttons assigned for: " + currentMenu);
                return;
            }

            currentFocus = FocusArea.ContentPanel;
            currentContentIndex = 0;
            UpdateContentHover();
            Debug.Log("Entered content panel: " + currentMenu);
            return;
        }

        RectTransform[] currentButtons = GetCurrentContentButtons();
        if (currentButtons == null || currentButtons.Length == 0) return;

        RectTransform selectedButton = currentButtons[currentContentIndex];
        Debug.Log("Confirmed content button: " + selectedButton.name);

        Button btn = selectedButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.Invoke();
        }
    }

    private void HandleCancel()
    {
        if (currentFocus == FocusArea.ContentPanel)
        {
            currentFocus = FocusArea.MainMenu;
            ClearContentHover();
            RefreshMenu();
            Debug.Log("Returned to main menu.");
            return;
        }

        currentMenu = MenuType.Learn;
        RefreshMenu();
        Debug.Log("Menu selection canceled, returned to Learn.");
    }

    private void UpdateContentHover()
    {
        ClearContentHover();

        if (currentFocus != FocusArea.ContentPanel) return;

        RectTransform[] buttons = GetCurrentContentButtons();
        if (buttons == null || buttons.Length == 0) return;

        if (currentContentIndex < 0 || currentContentIndex >= buttons.Length)
            currentContentIndex = 0;

        RectTransform selected = buttons[currentContentIndex];
        if (selected == null) return;

        selected.localScale = hoverScale;
    }

    private void ClearContentHover()
    {
        ResetButtonArray(learnButtons);
        ResetButtonArray(quizButtons);
        ResetButtonArray(jumbleLettersButtons);
        ResetButtonArray(basicWordsButtons);
    }

    private void ResetButtonArray(RectTransform[] buttons)
    {
        if (buttons == null) return;

        foreach (RectTransform btn in buttons)
        {
            if (btn == null) continue;
            btn.localScale = normalScale;
        }
    }
}