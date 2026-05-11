using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BeginnerAlphabetMenuController : MonoBehaviour
{
    public enum MenuType
    {
        Learn,
        Quiz,
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

    [Header("Content Groups")]
    public GameObject learnContent;
    public GameObject quizContent;

    [Header("Content Buttons")]
    public RectTransform[] learnButtons;
    public RectTransform[] quizButtons;

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
        currentMenu = MenuType.Quiz;
        RefreshMenu();
    }

    public void SelectNextMenu()
    {
        currentMenu = (currentMenu == MenuType.Learn) ? MenuType.Quiz : MenuType.Learn;
        RefreshMenu();
    }

    public void SelectPreviousMenu()
    {
        currentMenu = (currentMenu == MenuType.Learn) ? MenuType.Quiz : MenuType.Learn;
        RefreshMenu();
    }

    private void UpdateMenuUI()
    {
        if (learnContent != null) learnContent.SetActive(currentMenu == MenuType.Learn);
        if (quizContent != null) quizContent.SetActive(currentMenu == MenuType.Quiz);
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
        return currentMenu == MenuType.Learn ? learnBtn : quizBtn;
    }

    private RectTransform[] GetCurrentContentButtons()
    {
        return currentMenu == MenuType.Learn ? learnButtons : quizButtons;
    }

    private void HandleNext()
    {
        if (currentFocus == FocusArea.MainMenu)
        {
            SelectNextMenu();
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
            SelectPreviousMenu();
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
    // MAIN MENU
    if (currentFocus == FocusArea.MainMenu)
    {
        // Enter content panel
        RectTransform[] buttons = GetCurrentContentButtons();
        if (buttons == null || buttons.Length == 0) return;

        currentFocus = FocusArea.ContentPanel;
        currentContentIndex = 0;
        UpdateContentHover();
        return;
    }

    // CONTENT PANEL
    RectTransform[] buttonsPanel = GetCurrentContentButtons();
    if (buttonsPanel == null || buttonsPanel.Length == 0) return;

    RectTransform selected = buttonsPanel[currentContentIndex];

    if (selected == null)
        return;

    Debug.Log("Selected: " + selected.name);

    // =========================
    // LEARN PANEL SCENES
    // =========================
    if (selected.name == "AbcSongBtn")
    {
        SceneManager.LoadScene("AbcSongScene");
        return;
    }

    if (selected.name == "LetterToBrailleBtn")
    {
        SceneManager.LoadScene("LetterToBrailleScene");
        return;
    }

    if (selected.name == "AbcSoundsBtn")
    {
        SceneManager.LoadScene("AbcSoundsScene");
        return;
    }

    // =========================
    // QUIZ PANEL SCENES
    // =========================
    if (selected.name == "AbcFlow")
    {
        SceneManager.LoadScene("AbcFlowA");
        return;
    }

    if (selected.name == "JumbledLetters")
    {
        SceneManager.LoadScene("JumbledLetters");
        return;
    }

    // fallback
    Button btn = selected.GetComponent<Button>();
    if (btn != null)
        btn.onClick.Invoke();
}
    private void HandleCancel()
    {
        if (currentFocus == FocusArea.ContentPanel)
        {
            currentFocus = FocusArea.MainMenu;
            ClearContentHover();
            RefreshMenu();
            return;
        }

        currentMenu = MenuType.Learn;
        RefreshMenu();
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