using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AccessibleLoginFlow : MonoBehaviour
{
    [System.Serializable]
    public class MessageEntry
    {
        [TextArea(2, 5)]
        public string message;
        public AudioClip audioClip;
    }
    

    private enum LoginStage
    {
        Intro,
        Username,
        Password,
        LoginButton
    }

    [Header("Database")]
[SerializeField] private string loginURL = "http://localhost/brailleplay/login.php";


    [Header("UI References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;

    [Header("Optional Highlight Targets")]
    [SerializeField] private RectTransform usernameHighlightTarget;
    [SerializeField] private RectTransform passwordHighlightTarget;
    [SerializeField] private RectTransform loginHighlightTarget;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Intro Messages")]
    [SerializeField] private List<MessageEntry> introMessages = new List<MessageEntry>();
    [Header("Intro Timing")]
    [SerializeField] private float firstIntroDelay = 1.5f;

    [Header("Typing Settings")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float delayAfterMessage = 0.75f;
    [SerializeField] private bool requireNextButtonForIntroAdvance = true;

    [Header("Validation Settings")]
    [SerializeField] private bool usernameAllowLetters = true;
    [SerializeField] private bool usernameAllowNumbers = true;
    [SerializeField] private bool usernameAllowUnderscore = true;
    [SerializeField] private bool usernameAllowSpaces = false;

    [Header("Focus Pulse")]
    [SerializeField] private bool usePulseEffect = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseScaleAmount = 0.04f;

    [Header("Messages")]
    [SerializeField] private MessageEntry readyForUsernameMessage;
    [SerializeField] private MessageEntry usernameFocusMessage;
    [SerializeField] private MessageEntry usernameEmptyMessage;
    [SerializeField] private MessageEntry passwordFocusMessage;
    [SerializeField] private MessageEntry passwordEmptyMessage;
    [SerializeField] private MessageEntry loginFocusMessage;
    [SerializeField] private MessageEntry loginFailedMessage;
    [SerializeField] private MessageEntry loginSuccessMessage;

    private LoginStage currentStage = LoginStage.Intro;
    private Coroutine typingRoutine;
    private int currentIntroIndex = 0;
    private bool isTyping;

    private RectTransform currentPulseTarget;
    private Vector3 usernameBaseScale;
    private Vector3 passwordBaseScale;
    private Vector3 loginBaseScale;

    private string lastAnnouncedMessage = "";
    private AudioClip lastAnnouncedClip = null;
    public static string LoggedInUsername; // global access (important)

    private void Awake()
    {
        if (usernameHighlightTarget == null && usernameInput != null)
            usernameHighlightTarget = usernameInput.GetComponent<RectTransform>();

        if (passwordHighlightTarget == null && passwordInput != null)
            passwordHighlightTarget = passwordInput.GetComponent<RectTransform>();

        if (loginHighlightTarget == null && loginButton != null)
            loginHighlightTarget = loginButton.GetComponent<RectTransform>();

        if (usernameHighlightTarget != null) usernameBaseScale = usernameHighlightTarget.localScale;
        if (passwordHighlightTarget != null) passwordBaseScale = passwordHighlightTarget.localScale;
        if (loginHighlightTarget != null) loginBaseScale = loginHighlightTarget.localScale;
    }

    private void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnLogin += HandleLoginAction;
    }

    private void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnLogin -= HandleLoginAction;
    }

    private void Start()
    {
        if (usernameInput != null)
            usernameInput.inputType = TMP_InputField.InputType.Standard;

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        ResetHighlights();
        StartIntroSequence();
    }

    private void Update()
    {
        UpdatePulse();
    }

    private void StartIntroSequence()
    {
        currentStage = LoginStage.Intro;
        currentIntroIndex = 0;

        if (introMessages == null || introMessages.Count == 0)
        {
            FinishIntroAndFocusUsername();
            return;
        }

        StartCoroutine(StartIntroWithDelay());
    }
    private IEnumerator LoginUser(string username, string password)
{
    WWWForm form = new WWWForm();
    form.AddField("username", username);
    form.AddField("password", password);

    using (UnityEngine.Networking.UnityWebRequest www =
        UnityEngine.Networking.UnityWebRequest.Post(loginURL, form))
    {
        yield return www.SendWebRequest();

        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
            www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Login Error: " + www.error);
            StartCoroutine(HandleFailedLoginFlow());
        }
        else
        {
            string response = www.downloadHandler.text.Trim();
            Debug.Log("Server Response: " + response);

            if (response == "SUCCESS")
        {
            LoggedInUsername = username;

             PlayerPrefs.SetString("username", username);
            PlayerPrefs.SetInt("isLoggedIn", 1);
            PlayerPrefs.Save();

            Debug.Log("Login success. Saved username: " + username);

            StartCoroutine(HandleSuccessfulLoginFlow());

            }
            else
            {
                StartCoroutine(HandleFailedLoginFlow());
            }
        }
    }
}

    private IEnumerator StartIntroWithDelay()
    {
        yield return new WaitForSeconds(firstIntroDelay);

        ShowIntroMessage(currentIntroIndex);
    }
    private void ShowIntroMessage(int index)
    {
        if (index < 0 || index >= introMessages.Count)
        {
            FinishIntroAndFocusUsername();
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
            FinishIntroAndFocusUsername();
        else
            ShowIntroMessage(currentIntroIndex);
    }

    private void FinishIntroAndFocusUsername()
    {
        currentStage = LoginStage.Username;
        FocusUsernameFieldSilently();
        Announce(readyForUsernameMessage, false);
    }

    private void FocusUsernameField()
    {
        currentStage = LoginStage.Username;
        SetSelected(usernameInput);
        SetPulseTarget(usernameHighlightTarget);
        Announce(usernameFocusMessage, false);
    }

    private void FocusPasswordField()
    {
        currentStage = LoginStage.Password;
        SetSelected(passwordInput);
        SetPulseTarget(passwordHighlightTarget);
        Announce(passwordFocusMessage, false);
    }

    private void FocusLoginButton()
    {
        currentStage = LoginStage.LoginButton;

        if (loginButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(loginButton.gameObject);

        SetPulseTarget(loginHighlightTarget);
        Announce(loginFocusMessage, false);
    }

    private void HandleNext()
    {
        if (currentStage != LoginStage.Intro)
            return;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        currentIntroIndex++;

        if (currentIntroIndex >= introMessages.Count)
            FinishIntroAndFocusUsername();
        else
            ShowIntroMessage(currentIntroIndex);
    }

    private void HandleSubmit()
    {
        if (currentStage == LoginStage.Intro)
        {
            if (isTyping)
                SkipTyping();
            else
                HandleNext();

            return;
        }

        switch (currentStage)
        {
            case LoginStage.Username:
                ValidateUsernameAndContinue();
                break;

            case LoginStage.Password:
                ValidatePasswordAndContinue();
                break;

            case LoginStage.LoginButton:
                TriggerLogin();
                break;
        }
    }

    private void HandleLoginAction()
    {
        if (currentStage == LoginStage.LoginButton)
            TriggerLogin();
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

    private void ValidateUsernameAndContinue()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(username))
        {
            Announce(usernameEmptyMessage, true);
            FocusUsernameFieldSilently();
            return;
        }

        // No validation here anymore — go straight to password
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

        // Instead of length checks → go to login
        FocusLoginButton();
    }

    public void GoToPreviousField()
    {
        switch (currentStage)
        {
            case LoginStage.Username:
                FocusUsernameField();
                break;

            case LoginStage.Password:
                FocusUsernameField();
                break;

            case LoginStage.LoginButton:
                FocusPasswordField();
                break;
        }
    }

    public void GoToNextField()
    {
        switch (currentStage)
        {
            case LoginStage.Username:
                FocusPasswordField();
                break;

            case LoginStage.Password:
                FocusLoginButton();
                break;
        }
    }

    private void FocusUsernameFieldSilently()
    {
        currentStage = LoginStage.Username;
        SetSelected(usernameInput);
        SetPulseTarget(usernameHighlightTarget);
    }

    private void FocusPasswordFieldSilently()
    {
        currentStage = LoginStage.Password;
        SetSelected(passwordInput);
        SetPulseTarget(passwordHighlightTarget);
    }

    private bool IsUsernameValid(string username)
    {
        string pattern = BuildUsernameRegexPattern();
        return Regex.IsMatch(username, pattern);
    }

    private string BuildUsernameRegexPattern()
    {
        string allowed = "";

        if (usernameAllowLetters)
            allowed += "A-Za-z";

        if (usernameAllowNumbers)
            allowed += "0-9";

        if (usernameAllowUnderscore)
            allowed += "_";

        if (usernameAllowSpaces)
            allowed += " ";

        if (string.IsNullOrEmpty(allowed))
            allowed = "A-Za-z0-9_";

        return "^[" + allowed + "]+$";
    }

   private void TriggerLogin()
{
    string username = usernameInput != null ? usernameInput.text.Trim() : "";
    string password = passwordInput != null ? passwordInput.text : "";

    StartCoroutine(LoginUser(username, password));
}
    //Paki bago ito if may database na tayo ng accounts
    private bool CheckLoginCredentials()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text : "";

        // TEMP logic (replace later with real backend)
        return username == "aaaa" && password == "aaaa";
    }
    private IEnumerator HandleFailedLoginFlow()
    {
        Announce(loginFailedMessage, false);

        while (isTyping)
            yield return null;

        float audioWait = 0f;
        if (loginFailedMessage != null && loginFailedMessage.audioClip != null)
            audioWait = loginFailedMessage.audioClip.length;

        if (audioWait > 0f)
            yield return new WaitForSeconds(audioWait);

        ClearLoginFields();
        FocusUsernameField();
    }

    private IEnumerator HandleSuccessfulLoginFlow()
    {
        Announce(loginSuccessMessage, false);

        while (isTyping)
            yield return null;

        float audioWait = 0f;
        if (loginSuccessMessage != null && loginSuccessMessage.audioClip != null)
            audioWait = loginSuccessMessage.audioClip.length;

        if (audioWait > 0f)
            yield return new WaitForSeconds(audioWait);

        // 👉 NEXT STEP: load scene here
        // SceneManager.LoadScene("MainMenu");
    }

    private void ClearLoginFields()
    {
        if (usernameInput != null)
        {
            usernameInput.text = "";
            usernameInput.ForceLabelUpdate();
        }

        if (passwordInput != null)
        {
            passwordInput.text = "";
            passwordInput.ForceLabelUpdate();
        }
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

        if (usernameHighlightTarget != null) usernameHighlightTarget.localScale = usernameBaseScale;
        if (passwordHighlightTarget != null) passwordHighlightTarget.localScale = passwordBaseScale;
        if (loginHighlightTarget != null) loginHighlightTarget.localScale = loginBaseScale;
    }

    private void UpdatePulse()
    {
        if (!usePulseEffect || currentPulseTarget == null)
            return;

        Vector3 baseScale = Vector3.one;

        if (currentPulseTarget == usernameHighlightTarget)
            baseScale = usernameBaseScale;
        else if (currentPulseTarget == passwordHighlightTarget)
            baseScale = passwordBaseScale;
        else if (currentPulseTarget == loginHighlightTarget)
            baseScale = loginBaseScale;

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScaleAmount;
        currentPulseTarget.localScale = baseScale * pulse;
    }
}