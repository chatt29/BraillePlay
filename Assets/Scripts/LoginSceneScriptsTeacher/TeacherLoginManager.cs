using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the teacher login scene: greeting, instructions, accessible
/// username + password entry, and matching both against the teacher's
/// document in Firestore.
/// </summary>
public class TeacherLoginManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField usernameField;
    public BrailleInputField passwordField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    [Tooltip("Scene to load if the teacher presses Back before logging in.")]
    public string previousSceneName = "MainMenu";
    [Tooltip("Scene to load after a successful login.")]
    public string teacherHomeSceneName = "MainMenu";

    private FirestoreTeacherService teacherService;

    private void Awake()
    {
        teacherService = new FirestoreTeacherService();
    }

    private void Start()
    {
        StartCoroutine(RunLoginFlow());
    }

    private IEnumerator RunLoginFlow()
    {
        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome back. This is the login page.");
        yield return AccessibilityManager.Instance.AnnounceAndWait(
            "Use the direction pad to move between your username and password fields. " +
            "Press submit to check a field, or to log in once both are filled in.");

        SetupFields();

        navigator.OnBackRequested += HandleBackRequested;
        navigator.Setup(BuildElements());
        navigator.BeginNavigation();
    }

    private void SetupFields()
    {
        usernameField.Configure("Username", "Type your username.", BrailleInputMode.Any);
        passwordField.Configure("Password", "Type your password.", BrailleInputMode.Any, spacesAllowed: false, passwordField: true);

        usernameField.Validator = value => FormValidator.ValidateRequired("Username", value);
        passwordField.Validator = value => FormValidator.ValidateRequired("Password", value);
    }

    private List<IAccessibleFormElement> BuildElements()
    {
        var submitElement = new SubmitButtonElement(
            "Submit button",
            "Press submit to log in.",
            ValidateAllFields,
            HandleSubmitActivated);

        return new List<IAccessibleFormElement> { usernameField, passwordField, submitElement };
    }

    private string ValidateAllFields()
    {
        return usernameField.Validate() ?? passwordField.Validate();
    }

    private void HandleSubmitActivated()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        string error = ValidateAllFields();
        if (error != null)
        {
            AccessibilityManager.Instance.Announce(error);
            yield break;
        }

        string username = usernameField.Value;
        string enteredPassword = passwordField.Value;

        Task<TeacherData> loadTask = teacherService.LoadTeacherAsync(username);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.Exception != null)
        {
            AccessibilityManager.Instance.Announce("Something went wrong logging you in. Please try again.");
            Debug.LogException(loadTask.Exception);
            yield break;
        }

        TeacherData teacher = loadTask.Result;

        // Plain-text comparison, matching how the password is currently stored
        // (see the security note in TeacherData.cs) - swap for a hashed
        // comparison once that's addressed.
        bool matches = teacher != null && teacher.Password == enteredPassword;

        if (!matches)
        {
            AccessibilityManager.Instance.Announce("We couldn't find a matching account. Please check your username and password.");
            yield break;
        }

        UserSession.SetTeacher(username);

        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome back, " + teacher.FirstName + "!");

        SceneManager.LoadScene(teacherHomeSceneName);
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