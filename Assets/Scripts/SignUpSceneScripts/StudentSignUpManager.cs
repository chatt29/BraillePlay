using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the student account-creation scene end to end: greeting,
/// instructions, accessible field-by-field braille entry (first name, last
/// name, student number), and saving the new student to Firestore.
///
/// Attach to the "BrailleSignupManager" object in the student SignUp scene
/// and wire up the four fields below in the inspector.
/// </summary>
public class StudentSignUpManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField firstNameField;
    public BrailleInputField lastNameField;
    public BrailleInputField studentNumberField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    [Tooltip("Scene to load if the student presses Back before finishing signup.")]
    public string previousSceneName = "MainMenu";
    [Tooltip("Scene to load after a successful signup, per the README's 'Return to Login' step.")]
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
        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome to account creation.");
        yield return AccessibilityManager.Instance.AnnounceAndWait("Let's create your account.");

        SetupFields();

        navigator.OnBackRequested += HandleBackRequested;
        navigator.Setup(BuildElements());
        navigator.BeginNavigation();
    }

    private void SetupFields()
    {
        firstNameField.Configure("First name", "Type your first name.", BrailleInputMode.Letters);
        lastNameField.Configure("Last name", "Type your last name.", BrailleInputMode.Letters);
        studentNumberField.Configure("Student number", "Type your student number.", BrailleInputMode.Numbers);

        firstNameField.Validator = value => FormValidator.ValidateLettersOnly("First name", value, 2);
        lastNameField.Validator = value => FormValidator.ValidateLettersOnly("Last name", value, 2);
        studentNumberField.Validator = value => FormValidator.ValidateDigitsOnly("Student number", value, 1);
    }

    private List<IAccessibleFormElement> BuildElements()
    {
        var submitElement = new SubmitButtonElement(
            "Submit button",
            "Press submit to create your account.",
            ValidateAllFields,
            HandleSubmitActivated);

        return new List<IAccessibleFormElement>
        {
            firstNameField,
            lastNameField,
            studentNumberField,
            submitElement
        };
    }

    private string ValidateAllFields()
    {
        return firstNameField.Validate()
            ?? lastNameField.Validate()
            ?? studentNumberField.Validate();
    }

    private void HandleSubmitActivated()
    {
        StartCoroutine(SubmitStudentCoroutine());
    }

    private IEnumerator SubmitStudentCoroutine()
    {
        // Defense in depth - the navigator already refuses to leave an
        // invalid field, so this should always pass by the time it's reachable.
        string error = ValidateAllFields();
        if (error != null)
        {
            AccessibilityManager.Instance.Announce(error);
            yield break;
        }

        string studentNumber = studentNumberField.Value;

        Task<bool> existsTask = studentService.StudentExistsAsync(studentNumber);
        yield return new WaitUntil(() => existsTask.IsCompleted);

        if (existsTask.Exception != null)
        {
            AccessibilityManager.Instance.Announce("Something went wrong checking that student number. Please try again.");
            Debug.LogException(existsTask.Exception);
            yield break;
        }

        if (existsTask.Result)
        {
            AccessibilityManager.Instance.Announce("That student number is already registered. Please use a different one.");
            yield break;
        }

        var data = new StudentData
        {
            FirstName = firstNameField.Value,
            LastName = lastNameField.Value,
            TotalScore = 0,
            HighestScore = 0,
            CompletedLessons = 0,
            CurrentLesson = 0,
            CurrentQuiz = 0
        };

        Task createTask = studentService.CreateStudentAsync(studentNumber, data);
        yield return new WaitUntil(() => createTask.IsCompleted);

        if (createTask.Exception != null)
        {
            AccessibilityManager.Instance.Announce("Something went wrong creating your account. Please try again.");
            Debug.LogException(createTask.Exception);
            yield break;
        }

        yield return AccessibilityManager.Instance.AnnounceAndWait("Account created successfully.");
        yield return AccessibilityManager.Instance.AnnounceAndWait("Returning to login.");

        SceneManager.LoadScene(loginSceneName);
    }

    private void HandleBackRequested()
    {
        AccessibilityManager.Instance.Announce("Going back.");
        SceneManager.LoadScene(previousSceneName);
    }

    private void OnDestroy()
    {
        if (navigator != null)
            navigator.OnBackRequested -= HandleBackRequested;
    }
}