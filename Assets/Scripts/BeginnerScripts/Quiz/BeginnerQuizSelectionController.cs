using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BeginnerQuizSelectionController : MonoBehaviour
{
    public enum QuizOption
    {
        JumbledLetters,
        
        AbcFlowA
    }
    [Header("Arrow Indicator")]
public RectTransform arrowIndicator;

    [Header("Buttons")]
    public RectTransform flowBtn;
    public RectTransform jumbledBtn;
    

    [Header("Hover Effect")]
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    private QuizOption currentOption = QuizOption.JumbledLetters;

    // 👉 Optional: store selected option globally
    public static QuizOption selectedOption;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnDeleteOrNo += HandlePrevious;
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnRepeat += HandleBack;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnDeleteOrNo -= HandlePrevious;
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnRepeat -= HandleBack;
    }

    private void Start()
    {
        UpdateSelection();
    }

    // 👉 Navigation
    private void HandleNext()
    {
        ToggleOption();
    }

    private void HandlePrevious()
    {
        ToggleOption();
    }

    private void ToggleOption()
    {
        currentOption = (currentOption == QuizOption.JumbledLetters)
            ? QuizOption.AbcFlowA
            : QuizOption.JumbledLetters;

        UpdateSelection();
    }

    // 👉 Submit (NO scene change)
   private void HandleSubmit()
{
    selectedOption = currentOption;

    Debug.Log("Selected Quiz: " + currentOption);

    switch (currentOption)
    {
         case QuizOption.AbcFlowA:
        PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
            SceneManager.LoadScene("AbcFlowA");
            break;
        case QuizOption.JumbledLetters:
            PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
            SceneManager.LoadScene("JumbledLetters");
            break;
    }
}

    // 👉 Back
    private void HandleBack()
    {
        Debug.Log("Back pressed (no scene change)");
    }

private void MoveArrow(RectTransform target)
{
    if (arrowIndicator == null || target == null) return;

    Vector2 pos = arrowIndicator.anchoredPosition;
    pos.y = target.anchoredPosition.y;
    arrowIndicator.anchoredPosition = pos;
}
    // 👉 Visual highlight
    private void UpdateSelection()
{
    ResetScale();

    RectTransform target = null;

    if (currentOption == QuizOption.JumbledLetters && jumbledBtn != null)
    {
        jumbledBtn.localScale = hoverScale;
        target = jumbledBtn;
    }

    if (currentOption == QuizOption.AbcFlowA && flowBtn != null)
    {
        flowBtn.localScale = hoverScale;
        target = flowBtn;
    }

    MoveArrow(target);
}
    

    private void ResetScale()
    {
        if (jumbledBtn != null) jumbledBtn.localScale = normalScale;
        if (flowBtn != null) flowBtn.localScale = normalScale;
    }
}