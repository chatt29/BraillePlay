using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Student login scene. All speech is recorded voice via VoiceManager,
/// except the student's first name, which is spoken by TTS.
/// On a mismatch it deliberately doesn't say which field was wrong.
/// </summary>
public class StudentLoginManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField lastNameField;
    public BrailleInputField studentNumberField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    public string previousSceneName = "MainMenu";
    public string studentHomeSceneName = "GameMenu";

    private FirestoreStudentService studentService;

    private void Awake()
    {
        studentService = new FirestoreStudentService();
    }

    private void Start()
    {
        StartCoroutine(RunLoginFlow());
    }

    private IEnumerator RunLoginFlow()
    {
        yield return VoiceManager.Instance.PlaySequence(
            VoiceKey.LoginWelcome, VoiceKey.LoginInstructions);

        SetupFields();

        navigator.OnBackRequested += HandleBackRequested;
        navigator.Setup(BuildElements());
        navigator.BeginNavigation();
    }

    private void SetupFields()
    {
        lastNameField.Configure(VoiceKey.FieldLastName, VoiceKey.HintLastName, BrailleInputMode.Letters);
        studentNumberField.Configure(VoiceKey.FieldStudentNumber, VoiceKey.HintStudentNumber, BrailleInputMode.Numbers);

        lastNameField.VoiceValidator = value => FormValidator.ValidateLastNameVoice(value, 2);
        studentNumberField.VoiceValidator = value => FormValidator.ValidateStudentNumberVoice(value);
    }

    private List<IAccessibleFormElement> BuildElements()
    {
        var submitElement = new SubmitButtonElement(
            VoiceKey.ButtonSubmit,
            VoiceKey.HintSubmit,
            ValidateAllFields,
            HandleSubmitActivated);

        return new List<IAccessibleFormElement> { lastNameField, studentNumberField, submitElement };
    }

    private VoiceKey? ValidateAllFields()
    {
        return lastNameField.ValidateVoice() ?? studentNumberField.ValidateVoice();
    }

    private void HandleSubmitActivated()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        if (string.IsNullOrWhiteSpace(lastNameField.Value) &&
            string.IsNullOrWhiteSpace(studentNumberField.Value))
        {
            VoiceManager.Instance.Play(VoiceKey.ErrBothEmpty);
            yield break;
        }

        VoiceKey? error = ValidateAllFields();
        if (error != null)
        {
            VoiceManager.Instance.Play(error);
            yield break;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrNoInternet);
            yield break;
        }

        string studentNumber = studentNumberField.Value;
        string enteredLastName = lastNameField.Value;

        Task<StudentData> loadTask = studentService.LoadStudentAsync(studentNumber);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.Exception != null)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrGeneric);
            Debug.LogException(loadTask.Exception);
            yield break;
        }

        StudentData student = loadTask.Result;
        bool matches = student != null &&
            string.Equals(student.LastName, enteredLastName, System.StringComparison.OrdinalIgnoreCase);

        if (!matches)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrNoMatch);
            yield break;
        }

        UserSession.SetStudent(studentNumber);

        // Recorded "Welcome," then TTS says only the first name
        yield return VoiceManager.Instance.PlayAndWait(VoiceKey.WelcomeStudent);
        yield return AccessibilityManager.Instance.AnnounceAndWait(student.FirstName);

        SceneManager.LoadScene(studentHomeSceneName);
    }



    private void HandleBackRequested()
    {
        VoiceManager.Instance.Play(VoiceKey.GoingBack);
        SceneManager.LoadScene(previousSceneName);
    }

    private void OnDestroy()
    {
        if (navigator != null)
            navigator.OnBackRequested -= HandleBackRequested;
    }
}