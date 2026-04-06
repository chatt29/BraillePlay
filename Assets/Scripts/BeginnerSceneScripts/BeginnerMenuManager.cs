using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginnerMenuManager : MonoBehaviour
{
    public enum MenuOption
    {
        Alphabet,
        Numbers,
        Combinations
    }

    [System.Serializable]
    public class BubbleMessage
    {
        [TextArea(2, 4)]
        public string text;
        public AudioClip audioClip;
    }

    [Header("Arrow Indicator")]
    public RectTransform arrowIndicator;

    [Header("Button Targets")]
    public RectTransform alphabetTarget;
    public RectTransform numbersTarget;
    public RectTransform combinationsTarget;

    [Header("Buttons")]
    public Button alphabetButton;
    public Button numbersButton;
    public Button combinationsButton;

    [Header("Button Hover Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    [Header("Scene Names")]
    public string alphabetScene = "BeginnerAlphabetScene";
    public string numbersScene = "BeginnerNumbersScene";
    public string combinationsScene = "BeginnerCombinationsScene";

    [Header("Speech Bubble")]
    public TMP_Text bubbleText;
    public AudioSource bubbleAudioSource;

    [Header("Bubble Messages")]
    public BubbleMessage introMessage;
    public BubbleMessage alphabetMessage;
    public BubbleMessage numbersMessage;
    public BubbleMessage combinationsMessage;

    [Header("Typewriter")]
    public float characterDelay = 0.03f;

    [Header("Options")]
    public bool wrapSelection = true;
    public bool logActions = true;
    public bool playIntroOnStart = true;

    private MenuOption currentSelection = MenuOption.Alphabet;
    private Coroutine typewriterRoutine;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += MoveSelectionDown;
        BrailleMapping.OnDeleteOrNo += MoveSelectionUp;
        BrailleMapping.OnSubmit += ConfirmSelection;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= MoveSelectionDown;
        BrailleMapping.OnDeleteOrNo -= MoveSelectionUp;
        BrailleMapping.OnSubmit -= ConfirmSelection;
    }

    private void Start()
    {
        UpdateArrowPosition();
        UpdateButtonVisuals();

        if (playIntroOnStart)
        {
            ShowMessage(introMessage);
        }
        else
        {
            ShowMessageForSelection();
        }
    }

    private void MoveSelectionUp()
    {
        switch (currentSelection)
        {
            case MenuOption.Alphabet:
                currentSelection = wrapSelection ? MenuOption.Combinations : MenuOption.Alphabet;
                break;

            case MenuOption.Numbers:
                currentSelection = MenuOption.Alphabet;
                break;

            case MenuOption.Combinations:
                currentSelection = MenuOption.Numbers;
                break;
        }

        if (logActions) Debug.Log("Selected: " + currentSelection);

        UpdateArrowPosition();
        UpdateButtonVisuals();
        ShowMessageForSelection();
    }

    private void MoveSelectionDown()
    {
        switch (currentSelection)
        {
            case MenuOption.Alphabet:
                currentSelection = MenuOption.Numbers;
                break;

            case MenuOption.Numbers:
                currentSelection = MenuOption.Combinations;
                break;

            case MenuOption.Combinations:
                currentSelection = wrapSelection ? MenuOption.Alphabet : MenuOption.Combinations;
                break;
        }

        if (logActions) Debug.Log("Selected: " + currentSelection);

        UpdateArrowPosition();
        UpdateButtonVisuals();
        ShowMessageForSelection();
    }

    private void UpdateArrowPosition()
    {
        if (arrowIndicator == null) return;

        RectTransform target = null;

        switch (currentSelection)
        {
            case MenuOption.Alphabet:
                target = alphabetTarget;
                break;

            case MenuOption.Numbers:
                target = numbersTarget;
                break;

            case MenuOption.Combinations:
                target = combinationsTarget;
                break;
        }

        if (target != null)
        {
            Vector3 arrowPos = arrowIndicator.position;
            arrowPos.y = target.position.y;
            arrowIndicator.position = arrowPos;
        }
    }

    private void UpdateButtonVisuals()
    {
        SetButtonColor(alphabetButton, currentSelection == MenuOption.Alphabet ? hoverColor : normalColor);
        SetButtonColor(numbersButton, currentSelection == MenuOption.Numbers ? hoverColor : normalColor);
        SetButtonColor(combinationsButton, currentSelection == MenuOption.Combinations ? hoverColor : normalColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    private void ShowMessageForSelection()
    {
        switch (currentSelection)
        {
            case MenuOption.Alphabet:
                ShowMessage(alphabetMessage);
                break;

            case MenuOption.Numbers:
                ShowMessage(numbersMessage);
                break;

            case MenuOption.Combinations:
                ShowMessage(combinationsMessage);
                break;
        }
    }

    private void ShowMessage(BubbleMessage message)
    {
        if (bubbleText == null || message == null)
            return;

        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = StartCoroutine(TypeText(message.text));

        if (bubbleAudioSource != null)
        {
            bubbleAudioSource.Stop();

            if (message.audioClip != null)
            {
                bubbleAudioSource.clip = message.audioClip;
                bubbleAudioSource.Play();
            }
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        bubbleText.text = "";

        if (string.IsNullOrEmpty(fullText))
            yield break;

        foreach (char c in fullText)
        {
            bubbleText.text += c;
            yield return new WaitForSeconds(characterDelay);
        }
    }

    public void ConfirmSelection()
    {
        switch (currentSelection)
        {
            case MenuOption.Alphabet:
                SceneManager.LoadScene(alphabetScene);
                break;

            case MenuOption.Numbers:
                SceneManager.LoadScene(numbersScene);
                break;

            case MenuOption.Combinations:
                SceneManager.LoadScene(combinationsScene);
                break;
        }
    }

    public void SelectAlphabet()
    {
        currentSelection = MenuOption.Alphabet;
        UpdateArrowPosition();
        UpdateButtonVisuals();
        ShowMessageForSelection();
    }

    public void SelectNumbers()
    {
        currentSelection = MenuOption.Numbers;
        UpdateArrowPosition();
        UpdateButtonVisuals();
        ShowMessageForSelection();
    }

    public void SelectCombinations()
    {
        currentSelection = MenuOption.Combinations;
        UpdateArrowPosition();
        UpdateButtonVisuals();
        ShowMessageForSelection();
    }

    public void ShowIntroMessage()
    {
        ShowMessage(introMessage);
    }
}