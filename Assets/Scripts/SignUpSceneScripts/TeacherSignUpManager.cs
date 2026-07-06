using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the teacher account-creation scene end to end: greeting,
/// instructions, accessible field-by-field braille entry (first name, last
/// name, username, password), and saving the new teacher to Firestore.
///
/// Attach to the "BrailleSignupManager" object in the teacher SignUp scene
/// and wire up the four fields below in the inspector.
/// </summary>
public class TeacherSignUpManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField firstNameField;
    public BrailleInputField lastNameField;
    public BrailleInputField usernameField;
    public BrailleInputField passwordField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    [Tooltip("Scene to load if the teacher presses Back before finishing signup.")]
    public string previousSceneName = "MainMenu";
    [Tooltip("Scene to load after a successful signup.")]
    public string loginSceneName = "LoginTeacher";

    private FirestoreTeacherService teacherService;

    private void Awake()
    {
        teacherService = new FirestoreTeacherService();
    }

    private void Start()
    {
        StartCoroutine(RunSignupFlow());
    }

    private IEnumerator RunSignupFlow()
    {
        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome to teacher account creation.");
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
        usernameField.Configure("Username", "Type a username.", BrailleInputMode.Any);
        passwordField.Configure("Password", "Type a password. It must be at least six characters.", BrailleInputMode.Any, spacesAllowed: false, passwordField: true);

        firstNameField.Validator = value => FormValidator.ValidateLettersOnly("First name", value, 2);
        lastNameField.Validator = value => FormValidator.ValidateLettersOnly("Last name", value, 2);
        usernameField.Validator = value => FormValidator.ValidateMinLength("Username", value, 3);
        passwordField.Validator = value => FormValidator.ValidateMinLength("Password", value, 6);
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
            usernameField,
            passwordField,
            submitElement
        };
    }

    private string ValidateAllFields()
    {
        return firstNameField.Validate()
            ?? lastNameField.Validate()
            ?? usernameField.Validate()
            ?? passwordField.Validate();
    }

    private void HandleSubmitActivated()
    {
        StartCoroutine(SubmitTeacherCoroutine());
    }

    private IEnumerator SubmitTeacherCoroutine()
    {
        // Defense in depth - the navigator already refuses to leave an
        // invalid field, so this should always pass by the time it's reachable.
        string error = ValidateAllFields();
        if (error != null)
        {
            AccessibilityManager.Instance.Announce(error);
            yield break;
        }

        string username = usernameField.Value;

        Task<bool> existsTask = teacherService.UsernameExistsAsync(username);
        yield return new WaitUntil(() => existsTask.IsCompleted);

        if (existsTask.Exception != null)
        {
            AccessibilityManager.Instance.Announce("Something went wrong checking that username. Please try again.");
            Debug.LogException(existsTask.Exception);
            yield break;
        }

        if (existsTask.Result)
        {
            AccessibilityManager.Instance.Announce("That username is already taken. Please choose another one.");
            yield break;
        }

        var data = new TeacherData
        {
            FirstName = firstNameField.Value,
            LastName = lastNameField.Value,
            Username = username,
            Password = passwordField.Value
        };

        Task createTask = teacherService.CreateTeacherAsync(data);
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