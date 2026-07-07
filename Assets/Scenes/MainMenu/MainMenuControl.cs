using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuControl : MonoBehaviour
{
    [System.Serializable]
    public class MenuOption
    {
        [Header("Button")]
        public Button button;

        [Header("Scene")]
        public string sceneName;

        [Header("Prince Speech")]
        [TextArea(2, 4)]
        public string selectedSpeech;
    }

    [Header("Menu Options")]
    public MenuOption[] options = new MenuOption[3];

    [Header("Prince Speech Bubble")]
    public TMP_Text speechBubbleText;
    public GameObject speechBubbleObject;

    [Header("Startup Speech")]
    [TextArea(2, 4)]
    public string welcomeSpeech = "Welcome to Braille Play.";

    [TextArea(2, 4)]
    public string instructionSpeech = "Use up and down to choose an option. Press submit to select.";

    [Header("Selection Visual Effect")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.85f, 0.25f);
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.08f, 1.08f, 1f);

    [Header("Timing")]
    public float startupDelay = 0.5f;
    public float enableButtonsAfterSeconds = 3.5f;

    [Header("Keyboard Backup")]
    public KeyCode keyboardUp = KeyCode.UpArrow;
    public KeyCode keyboardDown = KeyCode.DownArrow;
    public KeyCode keyboardSubmit = KeyCode.Return;

    [Header("Direction Fix")]
    [Tooltip("Turn this ON if pressing Down goes up, or pressing Up goes down.")]
    public bool flipVerticalInput = true;

    private int currentIndex = 0;
    private bool menuEnabled = false;

    private void Awake()
    {
        DisableAllButtons();
    }

    private void Start()
    {
        StartCoroutine(StartMainMenuSequence());
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUpInput;
        BrailleMapping.OnDown += HandleDownInput;
        BrailleMapping.OnSubmit += SelectCurrentOption;
        BrailleMapping.OnLogin += SelectCurrentOption;
        BrailleMapping.OnRepeat += RepeatCurrentOption;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUpInput;
        BrailleMapping.OnDown -= HandleDownInput;
        BrailleMapping.OnSubmit -= SelectCurrentOption;
        BrailleMapping.OnLogin -= SelectCurrentOption;
        BrailleMapping.OnRepeat -= RepeatCurrentOption;

    }

    private void Update()
    {
        if (!menuEnabled)
            return;

        if (Input.GetKeyDown(keyboardUp))
            HandleUpInput();

        if (Input.GetKeyDown(keyboardDown))
            HandleDownInput();

        if (Input.GetKeyDown(keyboardSubmit))
            SelectCurrentOption();
    }

    private IEnumerator StartMainMenuSequence()
    {
        menuEnabled = false;
        DisableAllButtons();
        ClearSelectionEffect();

        ShowSpeechBubble(true);

        yield return new WaitForSeconds(startupDelay);

        PrinceSpeak(welcomeSpeech + " " + instructionSpeech);

        yield return new WaitForSeconds(enableButtonsAfterSeconds);

        menuEnabled = true;
        EnableAllButtons();

        currentIndex = 0;
        SelectOption(0, true);
    }

    private void DisableAllButtons()
    {
        if (options == null)
            return;

        foreach (MenuOption option in options)
        {
            if (option != null && option.button != null)
                option.button.interactable = false;
        }
    }

    private void EnableAllButtons()
    {
        if (options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            int index = i;

            if (options[i] == null || options[i].button == null)
                continue;

            options[i].button.interactable = true;
            options[i].button.onClick.RemoveAllListeners();
            options[i].button.onClick.AddListener(() =>
            {
                SelectOption(index, true);
                SelectCurrentOption();
            });
        }
    }

    private void HandleUpInput()
    {
        if (flipVerticalInput)
            MoveDown();
        else
            MoveUp();
    }

    private void HandleDownInput()
    {
        if (flipVerticalInput)
            MoveUp();
        else
            MoveDown();
    }

    private void MoveUp()
    {
        if (!menuEnabled)
            return;

        int nextIndex = currentIndex - 1;

        if (nextIndex < 0)
            nextIndex = options.Length - 1;

        SelectOption(nextIndex, true);
    }

    private void MoveDown()
    {
        if (!menuEnabled)
            return;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= options.Length)
            nextIndex = 0;

        SelectOption(nextIndex, true);
    }

    private void SelectOption(int index, bool speak)
    {
        if (options == null || options.Length == 0)
            return;

        if (index < 0 || index >= options.Length)
            return;

        currentIndex = index;

        ClearSelectionEffect();
        ApplySelectionEffect(currentIndex);

        if (options[currentIndex].button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(options[currentIndex].button.gameObject);

        if (speak)
            PrinceSpeak(options[currentIndex].selectedSpeech);
    }

    private void ClearSelectionEffect()
    {
        if (options == null)
            return;

        foreach (MenuOption option in options)
        {
            if (option == null || option.button == null)
                continue;

            Image image = option.button.GetComponent<Image>();

            if (image != null)
                image.color = normalColor;

            option.button.transform.localScale = normalScale;
        }
    }

    private void ApplySelectionEffect(int index)
    {
        if (options[index] == null || options[index].button == null)
            return;

        Image image = options[index].button.GetComponent<Image>();

        if (image != null)
            image.color = selectedColor;

        options[index].button.transform.localScale = selectedScale;
    }

    private void SelectCurrentOption()
    {
        if (!menuEnabled)
            return;

        if (options == null || options.Length == 0)
            return;

        MenuOption selectedOption = options[currentIndex];

        if (selectedOption == null)
            return;

        PrinceSpeak("Selected. " + selectedOption.selectedSpeech);

        if (!string.IsNullOrEmpty(selectedOption.sceneName))
            SceneManager.LoadScene(selectedOption.sceneName);
        else
            Debug.LogWarning("No scene name assigned for selected menu option.");
    }

    private void RepeatCurrentOption()
    {
        if (!menuEnabled)
            return;

        if (options == null || options.Length == 0)
            return;

        PrinceSpeak(options[currentIndex].selectedSpeech);
    }

    private void ShowSpeechBubble(bool show)
    {
        if (speechBubbleObject != null)
            speechBubbleObject.SetActive(show);
    }

    private void PrinceSpeak(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        ShowSpeechBubble(true);

        if (speechBubbleText != null)
            speechBubbleText.text = message;

        if (TTSManager.Instance != null)
            TTSManager.Instance.Speak(message);
    }

}

#if UNITY_ANDROID && !UNITY_EDITOR
    private class TextToSpeechInitListener : AndroidJavaProxy
    {
        private MainMenuControl mainMenuControl;

        public TextToSpeechInitListener(MainMenuControl control)
            : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            mainMenuControl = control;
        }

        public void onInit(int status)
        {
            if (status == 0)
                mainMenuControl.OnTextToSpeechReady();
        }
    }
#endif
