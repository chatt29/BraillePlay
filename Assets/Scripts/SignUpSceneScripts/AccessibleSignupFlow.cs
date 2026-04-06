using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AccessibleSignupFlow : MonoBehaviour
{
    [System.Serializable]
    public class MessageEntry
    {
        [TextArea(2, 5)]
        public string message;
        public AudioClip audioClip;
    }

    private enum SignupStage
    {
        Intro,
        FirstName,
        LastName,
        Username,
        Password,
        SubmitButton
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button submitButton;

    [Header("Optional Highlight Targets")]
    [SerializeField] private RectTransform firstNameHighlightTarget;
    [SerializeField] private RectTransform lastNameHighlightTarget;
    [SerializeField] private RectTransform usernameHighlightTarget;
    [SerializeField] private RectTransform passwordHighlightTarget;
    [SerializeField] private RectTransform submitHighlightTarget;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Intro Messages")]
    [SerializeField] private List<MessageEntry> introMessages = new List<MessageEntry>();

    [Header("Typing Settings")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float delayAfterMessage = 0.75f;
    [SerializeField] private bool requireNextButtonForIntroAdvance = true;

    [Header("Validation Settings")]
    [SerializeField] private int minFirstNameLength = 2;
    [SerializeField] private int minLastNameLength = 2;
    [SerializeField] private int minUsernameLength = 4;
    [SerializeField] private int minPasswordLength = 6;
    [SerializeField] private bool usernameAllowLetters = true;
    [SerializeField] private bool usernameAllowNumbers = true;
    [SerializeField] private bool usernameAllowUnderscore = true;
    [SerializeField] private bool usernameAllowSpaces = false;

    [Header("Focus Pulse")]
    [SerializeField] private bool usePulseEffect = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScaleAmount = 0.04f;

    [Header("Messages")]
    [SerializeField] private MessageEntry readyForFirstNameMessage;
    [SerializeField] private MessageEntry firstNameFocusMessage;
    [SerializeField] private MessageEntry firstNameEmptyMessage;
    [SerializeField] private MessageEntry firstNameTooShortMessage;

    [SerializeField] private MessageEntry lastNameFocusMessage;
    [SerializeField] private MessageEntry lastNameEmptyMessage;
    [SerializeField] private MessageEntry lastNameTooShortMessage;

    [SerializeField] private MessageEntry usernameFocusMessage;
    [SerializeField] private MessageEntry usernameEmptyMessage;
    [SerializeField] private MessageEntry usernameTooShortMessage;
    [SerializeField] private MessageEntry usernameInvalidCharactersMessage;

    [SerializeField] private MessageEntry passwordFocusMessage;
    [SerializeField] private MessageEntry passwordEmptyMessage;
    [SerializeField] private MessageEntry passwordTooShortMessage;

    [SerializeField] private MessageEntry submitFocusMessage;

    private SignupStage currentStage = SignupStage.Intro;
    private Coroutine typingRoutine;
    private int currentIntroIndex = 0;
    private bool isTyping;

    private RectTransform currentPulseTarget;

    private Vector3 firstNameBaseScale;
    private Vector3 lastNameBaseScale;
    private Vector3 usernameBaseScale;
    private Vector3 passwordBaseScale;
    private Vector3 submitBaseScale;

    private string lastAnnouncedMessage = "";
    private AudioClip lastAnnouncedClip = null;

    private void Awake()
    {
        if (firstNameHighlightTarget == null && firstNameInput != null)
            firstNameHighlightTarget = firstNameInput.GetComponent<RectTransform>();

        if (lastNameHighlightTarget == null && lastNameInput != null)
            lastNameHighlightTarget = lastNameInput.GetComponent<RectTransform>();

        if (usernameHighlightTarget == null && usernameInput != null)
            usernameHighlightTarget = usernameInput.GetComponent<RectTransform>();

        if (passwordHighlightTarget == null && passwordInput != null)
            passwordHighlightTarget = passwordInput.GetComponent<RectTransform>();

        if (submitHighlightTarget == null && submitButton != null)
            submitHighlightTarget = submitButton.GetComponent<RectTransform>();

        if (firstNameHighlightTarget != null) firstNameBaseScale = firstNameHighlightTarget.localScale;
        if (lastNameHighlightTarget != null) lastNameBaseScale = lastNameHighlightTarget.localScale;
        if (usernameHighlightTarget != null) usernameBaseScale = usernameHighlightTarget.localScale;
        if (passwordHighlightTarget != null) passwordBaseScale = passwordHighlightTarget.localScale;
        if (submitHighlightTarget != null) submitBaseScale = submitHighlightTarget.localScale;
    }

    private void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnRepeat -= HandleRepeat;
    }

    private void Start()
    {
        ConfigureInputFields();
        ResetHighlights();
        StartIntroSequence();
    }

    private void Update()
    {
        UpdatePulse();
    }

    private void ConfigureInputFields()
    {
        if (firstNameInput != null)
            firstNameInput.inputType = TMP_InputField.InputType.Standard;

        if (lastNameInput != null)
            lastNameInput.inputType = TMP_InputField.InputType.Standard;

        if (usernameInput != null)
            usernameInput.inputType = TMP_InputField.InputType.Standard;

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }
    }

    private void StartIntroSequence()
    {
        currentStage = SignupStage.Intro;
        currentIntroIndex = 0;

        if (introMessages == null || introMessages.Count == 0)
        {
            FinishIntroAndFocusFirstName();
            return;
        }

        ShowIntroMessage(currentIntroIndex);
    }

    private void ShowIntroMessage(int index)
    {
        if (index < 0 || index >= introMessages.Count)
        {
            FinishIntroAndFocusFirstName();
            return;
        }

        MessageEntry entry = introMessages[index];
        Announce(entry, false);

        if (!requireNextButtonForIntroAdvance)
            StartCoroutine(AutoAdvanceIntroAfterTyping());
    }

    private IEnumerator AutoAdvanceIntroAfterTyping()
    {
        while (isTyping)
            yield return null;

        yield return new WaitForSeconds(delayAfterMessage);

        currentIntroIndex++;

        if (currentIntroIndex >= introMessages.Count)
            FinishIntroAndFocusFirstName();
        else
            ShowIntroMessage(currentIntroIndex);
    }

    private void FinishIntroAndFocusFirstName()
    {
        currentStage = SignupStage.FirstName;
        FocusFirstNameFieldSilently();
        Announce(readyForFirstNameMessage, false);
    }

    private void HandleNext()
    {
        if (currentStage != SignupStage.Intro)
            return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        currentIntroIndex++;

        if (currentIntroIndex >= introMessages.Count)
            FinishIntroAndFocusFirstName();
        else
            ShowIntroMessage(currentIntroIndex);
    }

    private void HandleSubmit()
    {
        if (currentStage == SignupStage.Intro)
        {
            if (isTyping)
                SkipTyping();
            else
                HandleNext();

            return;
        }

        switch (currentStage)
        {
            case SignupStage.FirstName:
                ValidateFirstNameAndContinue();
                break;

            case SignupStage.LastName:
                ValidateLastNameAndContinue();
                break;

            case SignupStage.Username:
                ValidateUsernameAndContinue();
                break;

            case SignupStage.Password:
                ValidatePasswordAndContinue();
                break;

            case SignupStage.SubmitButton:
                TriggerSubmit();
                break;
        }
    }

    private void HandleRepeat()
    {
        RepeatCurrentMessage();
    }

    public void RepeatCurrentMessage()
    {
        if (string.IsNullOrWhiteSpace(lastAnnouncedMessage) && lastAnnouncedClip == null)
            return;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;

        if (messageText != null)
            messageText.text = lastAnnouncedMessage;

        if (audioSource != null)
            audioSource.Stop();

        PlayClip(lastAnnouncedClip);
    }

    private void ValidateFirstNameAndContinue()
    {
        string value = firstNameInput != null ? firstNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(value))
        {
            Announce(firstNameEmptyMessage, true);
            FocusFirstNameFieldSilently();
            return;
        }

        if (value.Length < minFirstNameLength)
        {
            Announce(firstNameTooShortMessage, true);
            FocusFirstNameFieldSilently();
            return;
        }

        FocusLastNameField();
    }

    private void ValidateLastNameAndContinue()
    {
        string value = lastNameInput != null ? lastNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(value))
        {
            Announce(lastNameEmptyMessage, true);
            FocusLastNameFieldSilently();
            return;
        }

        if (value.Length < minLastNameLength)
        {
            Announce(lastNameTooShortMessage, true);
            FocusLastNameFieldSilently();
            return;
        }

        FocusUsernameField();
    }

    private void ValidateUsernameAndContinue()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(username))
        {
            Announce(usernameEmptyMessage, true);
            FocusUsernameFieldSilently();
            return;
        }

        if (username.Length < minUsernameLength)
        {
            Announce(usernameTooShortMessage, true);
            FocusUsernameFieldSilently();
            return;
        }

        if (!IsUsernameValid(username))
        {
            Announce(usernameInvalidCharactersMessage, true);
            FocusUsernameFieldSilently();
            return;
        }

        FocusPasswordField();
    }

    private void ValidatePasswordAndContinue()
    {
        string password = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrEmpty(password))
        {
            Announce(passwordEmptyMessage, true);
            FocusPasswordFieldSilently();
            return;
        }

        if (password.Length < minPasswordLength)
        {
            Announce(passwordTooShortMessage, true);
            FocusPasswordFieldSilently();
            return;
        }

        FocusSubmitButton();
    }

    public void GoToPreviousField()
    {
        switch (currentStage)
        {
            case SignupStage.FirstName:
                FocusFirstNameField();
                break;

            case SignupStage.LastName:
                FocusFirstNameField();
                break;

            case SignupStage.Username:
                FocusLastNameField();
                break;

            case SignupStage.Password:
                FocusUsernameField();
                break;

            case SignupStage.SubmitButton:
                FocusPasswordField();
                break;
        }
    }

    public void GoToNextField()
    {
        switch (currentStage)
        {
            case SignupStage.FirstName:
                FocusLastNameField();
                break;

            case SignupStage.LastName:
                FocusUsernameField();
                break;

            case SignupStage.Username:
                FocusPasswordField();
                break;

            case SignupStage.Password:
                FocusSubmitButton();
                break;
        }
    }

    private void FocusFirstNameField()
    {
        currentStage = SignupStage.FirstName;
        SetSelected(firstNameInput);
        SetPulseTarget(firstNameHighlightTarget);
        Announce(firstNameFocusMessage, false);
    }

    private void FocusLastNameField()
    {
        currentStage = SignupStage.LastName;
        SetSelected(lastNameInput);
        SetPulseTarget(lastNameHighlightTarget);
        Announce(lastNameFocusMessage, false);
    }

    private void FocusUsernameField()
    {
        currentStage = SignupStage.Username;
        SetSelected(usernameInput);
        SetPulseTarget(usernameHighlightTarget);
        Announce(usernameFocusMessage, false);
    }

    private void FocusPasswordField()
    {
        currentStage = SignupStage.Password;
        SetSelected(passwordInput);
        SetPulseTarget(passwordHighlightTarget);
        Announce(passwordFocusMessage, false);
    }

    private void FocusSubmitButton()
    {
        currentStage = SignupStage.SubmitButton;

        if (submitButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(submitButton.gameObject);

        SetPulseTarget(submitHighlightTarget);
        Announce(submitFocusMessage, false);
    }

    private void FocusFirstNameFieldSilently()
    {
        currentStage = SignupStage.FirstName;
        SetSelected(firstNameInput);
        SetPulseTarget(firstNameHighlightTarget);
    }

    private void FocusLastNameFieldSilently()
    {
        currentStage = SignupStage.LastName;
        SetSelected(lastNameInput);
        SetPulseTarget(lastNameHighlightTarget);
    }

    private void FocusUsernameFieldSilently()
    {
        currentStage = SignupStage.Username;
        SetSelected(usernameInput);
        SetPulseTarget(usernameHighlightTarget);
    }

    private void FocusPasswordFieldSilently()
    {
        currentStage = SignupStage.Password;
        SetSelected(passwordInput);
        SetPulseTarget(passwordHighlightTarget);
    }

    private void TriggerSubmit()
    {
        if (submitButton != null)
            submitButton.onClick.Invoke();
    }

    private void SetSelected(TMP_InputField field)
    {
        if (field == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(field.gameObject);
        field.ActivateInputField();
        field.Select();
        MoveCaretToEnd(field);
    }

    private void MoveCaretToEnd(TMP_InputField field)
    {
        if (field == null)
            return;

        int length = field.text.Length;
        field.caretPosition = length;
        field.stringPosition = length;
        field.selectionAnchorPosition = length;
        field.selectionFocusPosition = length;
    }

    private bool IsUsernameValid(string username)
    {
        return Regex.IsMatch(username, BuildUsernameRegexPattern());
    }

    private string BuildUsernameRegexPattern()
    {
        string allowed = "";

        if (usernameAllowLetters) allowed += "A-Za-z";
        if (usernameAllowNumbers) allowed += "0-9";
        if (usernameAllowUnderscore) allowed += "_";
        if (usernameAllowSpaces) allowed += " ";

        if (string.IsNullOrEmpty(allowed))
            allowed = "A-Za-z0-9_";

        return "^[" + allowed + "]+$";
    }

    private void Announce(MessageEntry entry, bool overwriteImmediately)
    {
        string text = entry != null ? entry.message : "";
        AudioClip clip = entry != null ? entry.audioClip : null;

        lastAnnouncedMessage = text;
        lastAnnouncedClip = clip;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (messageText != null)
        {
            if (useTypewriter && !overwriteImmediately)
                typingRoutine = StartCoroutine(TypeText(text));
            else
            {
                messageText.text = text;
                isTyping = false;
            }
        }

        if (audioSource != null)
            audioSource.Stop();

        PlayClip(clip);
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;

        if (messageText != null)
            messageText.text = "";

        if (!useTypewriter || charactersPerSecond <= 0f)
        {
            if (messageText != null)
                messageText.text = fullText;

            isTyping = false;
            yield break;
        }

        float delay = 1f / charactersPerSecond;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (messageText != null)
                messageText.text += fullText[i];

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingRoutine = null;
    }

    private void SkipTyping()
    {
        if (!isTyping)
            return;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (messageText != null)
            messageText.text = lastAnnouncedMessage;

        isTyping = false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void SetPulseTarget(RectTransform target)
    {
        ResetHighlights();
        currentPulseTarget = target;
    }

    private void ResetHighlights()
    {
        currentPulseTarget = null;

        if (firstNameHighlightTarget != null) firstNameHighlightTarget.localScale = firstNameBaseScale;
        if (lastNameHighlightTarget != null) lastNameHighlightTarget.localScale = lastNameBaseScale;
        if (usernameHighlightTarget != null) usernameHighlightTarget.localScale = usernameBaseScale;
        if (passwordHighlightTarget != null) passwordHighlightTarget.localScale = passwordBaseScale;
        if (submitHighlightTarget != null) submitHighlightTarget.localScale = submitBaseScale;
    }

    private void UpdatePulse()
    {
        if (!usePulseEffect || currentPulseTarget == null)
            return;

        Vector3 baseScale = Vector3.one;

        if (currentPulseTarget == firstNameHighlightTarget) baseScale = firstNameBaseScale;
        else if (currentPulseTarget == lastNameHighlightTarget) baseScale = lastNameBaseScale;
        else if (currentPulseTarget == usernameHighlightTarget) baseScale = usernameBaseScale;
        else if (currentPulseTarget == passwordHighlightTarget) baseScale = passwordBaseScale;
        else if (currentPulseTarget == submitHighlightTarget) baseScale = submitBaseScale;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScaleAmount;
        currentPulseTarget.localScale = baseScale * pulse;
    }
}