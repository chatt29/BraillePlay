using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the student login scene: greeting, instructions, accessible
/// last-name + student-number entry, and matching both against the
/// student's document in Firestore.
///
/// On a mismatch this deliberately doesn't say which field was wrong (no
/// student number vs. wrong last name) - just that the two don't match
/// together - so a blind student trying different numbers can't use the
/// error to discover whether a given student number is registered. If
/// enumeration isn't a concern for your classroom setup, feel free to split
/// that into two distinct messages instead.
/// </summary>
public class StudentLoginManager : MonoBehaviour
{
    [Header("Input fields (see BrailleInputField)")]
    public BrailleInputField lastNameField;
    public BrailleInputField studentNumberField;

    [Header("Navigation")]
    public AccessibleFormNavigator navigator;

    [Header("Scene flow")]
    [Tooltip("Scene to load if the student presses Back before logging in.")]
    public string previousSceneName = "MainMenu";
    [Tooltip("Scene to load after a successful login.")]
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
        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome back. This is the login page.");
        yield return AccessibilityManager.Instance.AnnounceAndWait(
            "Use the direction pad to move between your last name and student number fields. " +
            "Press submit to check a field, or to log in once both are filled in.");

        SetupFields();

        navigator.OnBackRequested += HandleBackRequested;
        navigator.Setup(BuildElements());
        navigator.BeginNavigation();
    }

    private void SetupFields()
    {
        lastNameField.Configure("Last name", "Type your last name.", BrailleInputMode.Letters);
        studentNumberField.Configure("Student number", "Type your student number.", BrailleInputMode.Numbers);

        lastNameField.Validator = value => FormValidator.ValidateLettersOnly("Last name", value, 2);
        studentNumberField.Validator = value => FormValidator.ValidateDigitsOnly("Student number", value, 1);
    }

    private List<IAccessibleFormElement> BuildElements()
    {
        var submitElement = new SubmitButtonElement(
            "Submit button",
            "Press submit to log in.",
            ValidateAllFields,
            HandleSubmitActivated);

        return new List<IAccessibleFormElement> { lastNameField, studentNumberField, submitElement };
    }

    private string ValidateAllFields()
    {
        return lastNameField.Validate() ?? studentNumberField.Validate();
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

        string studentNumber = studentNumberField.Value;
        string enteredLastName = lastNameField.Value;

        Task<StudentData> loadTask = studentService.LoadStudentAsync(studentNumber);
        yield return new WaitUntil(() => loadTask.IsCompleted);

        if (loadTask.Exception != null)
        {
            AccessibilityManager.Instance.Announce("Something went wrong logging you in. Please try again.");
            Debug.LogException(loadTask.Exception);
            yield break;
        }

        StudentData student = loadTask.Result;
        bool matches = student != null &&
            string.Equals(student.LastName, enteredLastName, System.StringComparison.OrdinalIgnoreCase);

        if (!matches)
        {
            AccessibilityManager.Instance.Announce("We couldn't find a matching student account. Please check your last name and student number.");
            yield break;
        }

        UserSession.SetStudent(studentNumber);

        yield return AccessibilityManager.Instance.AnnounceAndWait("Welcome, " + student.FirstName + "!");

        SceneManager.LoadScene(studentHomeSceneName);
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