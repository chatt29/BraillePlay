using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Student sign-up scene. All speech is recorded voice via VoiceManager
/// (no TTS). Fields: first name, last name, student number, then submit.
/// </summary>
public class StudentSignUpManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField firstNameField;
    public BrailleInputField lastNameField;
    public BrailleInputField studentIDField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    [Tooltip("Scene to load if the student presses Back before finishing signup.")]
    public string previousSceneName = "MainMenu";
    [Tooltip("Scene to load after a successful signup.")]
    public string loginSceneName = "LoginStudent";

    private FirestoreStudentService studentService;

    private void Awake()
    {
        studentService = new FirestoreStudentService();
    }

    private void Start()
    {
        StartCoroutine(RunSignupFlow());
    }

    private IEnumerator RunSignupFlow()
    {
        yield return VoiceManager.Instance.PlaySequence(
            VoiceKey.SignupWelcome, VoiceKey.SignupInstructions);

        SetupFields();

        navigator.OnBackRequested += HandleBackRequested;
        navigator.Setup(BuildElements());
        navigator.BeginNavigation();
    }

    private void SetupFields()
    {
        firstNameField.Configure(VoiceKey.FieldFirstName, VoiceKey.HintFirstName, BrailleInputMode.Letters);
        lastNameField.Configure(VoiceKey.FieldLastName, VoiceKey.HintLastName, BrailleInputMode.Letters);
        studentIDField.Configure(VoiceKey.FieldStudentNumber, VoiceKey.HintStudentNumber, BrailleInputMode.Numbers);

        firstNameField.VoiceValidator = value => FormValidator.ValidateFirstNameVoice(value, 2);
        lastNameField.VoiceValidator = value => FormValidator.ValidateLastNameVoice(value, 2);
        studentIDField.VoiceValidator = value => FormValidator.ValidateStudentNumberVoice(value);
    }

    private List<IAccessibleFormElement> BuildElements()
    {
        var submitElement = new SubmitButtonElement(
            VoiceKey.ButtonSubmit,
            VoiceKey.HintSubmitSignup,
            ValidateAllFields,
            HandleSubmitActivated);

        return new List<IAccessibleFormElement>
        {
            firstNameField,
            lastNameField,
            studentIDField,
            submitElement
        };
    }

    private VoiceKey? ValidateAllFields()
    {
        return firstNameField.ValidateVoice()
            ?? lastNameField.ValidateVoice()
            ?? studentIDField.ValidateVoice();
    }

    private void HandleSubmitActivated()
    {
        StartCoroutine(SubmitStudentCoroutine());
    }

    private IEnumerator SubmitStudentCoroutine()
    {
        if (string.IsNullOrWhiteSpace(firstNameField.Value) &&
            string.IsNullOrWhiteSpace(lastNameField.Value) &&
            string.IsNullOrWhiteSpace(studentIDField.Value))
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

        string studentID = studentIDField.Value;

        Task<bool> existsTask = studentService.StudentExistsAsync(studentID);
        yield return new WaitUntil(() => existsTask.IsCompleted);

        if (existsTask.Exception != null)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrGeneric);
            Debug.LogException(existsTask.Exception);
            yield break;
        }

        if (existsTask.Result)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrStudentNumberTaken);
            yield break;
        }

        var data = new StudentData
        {
            FirstName = firstNameField.Value,
            LastName = lastNameField.Value,
            studentID = studentID,
            TotalScore = 0,
            HighestScore = 0,
            CompletedLessons = 0,
            CurrentLesson = 0,
            CurrentQuiz = 0
        };

        Task createTask = studentService.CreateStudentAsync(studentID, data);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.Exception != null)
        {
            VoiceManager.Instance.Play(VoiceKey.ErrGeneric);
            Debug.LogException(createTask.Exception);
            yield break;
        }

        yield return VoiceManager.Instance.PlaySequence(
            VoiceKey.AccountCreated, VoiceKey.ReturningToLogin);

        SceneManager.LoadScene(loginSceneName);
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