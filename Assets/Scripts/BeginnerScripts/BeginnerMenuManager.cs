using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BeginnerMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform selectorArrow;
    public Button[] buttons;
    public TMP_Text dialogueText;

    [Header("Dialogue Messages")]
    public string[] messages;

    [Header("Selector Settings")]
    public float offsetX = -60f;

    private int currentIndex = 0;
    private Vector2 targetPos;

    void Start()
{
    Canvas.ForceUpdateCanvases(); // 🔥 ensures UI is fully laid out

    UpdateSelection();
    selectorArrow.anchoredPosition = targetPos;
}
    void OnEnable()
    {
        BrailleMapping.OnYesOrNext += MoveDown;
        BrailleMapping.OnDeleteOrNo += MoveUp;
        BrailleMapping.OnSubmit += ActivateButton;
        BrailleMapping.OnLogin += ActivateButton;
        BrailleMapping.OnRepeat += RepeatDialogue;
        BrailleMapping.OnBack += GoBack;
    }

    void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= MoveDown;
        BrailleMapping.OnDeleteOrNo -= MoveUp;
        BrailleMapping.OnSubmit -= ActivateButton;
        BrailleMapping.OnLogin -= ActivateButton;
        BrailleMapping.OnRepeat -= RepeatDialogue;
        BrailleMapping.OnBack -= GoBack;
    }

    void Update()
    {
        // Smooth arrow movement
        selectorArrow.anchoredPosition = Vector2.Lerp(
            selectorArrow.anchoredPosition,
            targetPos,
            10f * Time.deltaTime
        );
    }

    // 🔽 Move Down (Y key)
    void MoveDown()
    {
        currentIndex++;
        if (currentIndex >= buttons.Length)
            currentIndex = 0;

        UpdateSelection();
    }

    // 🔼 Move Up (Backspace)
    void MoveUp()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = buttons.Length - 1;

        UpdateSelection();
    }

    // 🎯 Update Arrow + Dialogue
    void UpdateSelection()
{
    RectTransform btnRect = buttons[currentIndex].GetComponent<RectTransform>();
    RectTransform arrowParent = selectorArrow.parent as RectTransform;

    // Convert button position to world position
    Vector3 worldPos = btnRect.position;

    // Convert world position to local position relative to arrow's parent
    Vector2 localPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        arrowParent,
        RectTransformUtility.WorldToScreenPoint(null, worldPos),
        null,
        out localPoint
    );

    // Apply offset so arrow stays beside button
    targetPos = new Vector2(localPoint.x + offsetX, localPoint.y);

    // Update dialogue
    if (messages.Length > currentIndex)
        dialogueText.text = messages[currentIndex];
}
    // ✅ Select option (Space / Enter)
    void ActivateButton()
    {
        switch (currentIndex)
        {
            case 0:
                SceneManager.LoadScene("BeginnerAlphabetScene");
                break;

            case 1:
                SceneManager.LoadScene("NumbersScene");
                break;

            case 2:
                SceneManager.LoadScene("CombinationsScene");
                break;
        }
    }

    // 🔁 Repeat dialogue (R key)
    void RepeatDialogue()
    {
        if (messages.Length > currentIndex)
        {
            dialogueText.text = messages[currentIndex];
            Debug.Log("Repeat: " + messages[currentIndex]);
        }
    }

    // 🔙 Back (ESC key)
    void GoBack()
    {
        SceneManager.LoadScene("MainMenu"); // change if needed
    }

    // 🖱️ Optional UI button support
    public void SelectButton(int index)
    {
        currentIndex = index;
        UpdateSelection();
        ActivateButton();
    }
}