using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Popup shown when a teacher presses Enter on a selected row in the
/// Student Scoreboard table. Lets them review/change a student's number,
/// first name, and last name using the same braille-chord BrailleInputField
/// entry as the login form.
///
/// Up/Down moves between the three fields (default focus on First Name,
/// per the classroom's usual workflow). Enter starts editing whichever
/// field is selected; pressing Enter again while editing stops editing
/// that field and returns to field-selection. Space saves all three
/// fields, Backspace cancels without saving - same Space=yes/Backspace=no
/// convention used everywhere else in this app.
///
/// This is a new, purpose-built navigator, not AccessibleFormNavigator -
/// AccessibleFormNavigator moves through elements as a flat list toward a
/// single Submit, where this panel needs a fixed 3-field grid the teacher
/// can revisit and re-edit in any order before saving once.
/// </summary>
public class StudentEditPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    [Header("Fields (index 0 = Student Number, 1 = First Name, 2 = Last Name)")]
    [SerializeField] private BrailleInputField studentNumberField;
    [SerializeField] private BrailleInputField firstNameField;
    [SerializeField] private BrailleInputField lastNameField;

    [Tooltip("Optional - shows which field is currently selected, for sighted teachers. Not required to function.")]
    [SerializeField] private TMP_Text selectedFieldLabel;

    private enum State { Selecting, Editing }
    private State state = State.Selecting;

    private BrailleInputField[] fields;
    private int selectedIndex;

    private Action<string, string, string> onSave;
    private Action onCancel;

    private void Awake()
    {
        fields = new[] { studentNumberField, firstNameField, lastNameField };

        studentNumberField.Validator = value => FormValidator.ValidateDigitsOnly("Student number", value, 1);
        firstNameField.Validator = value => FormValidator.ValidateLettersOnly("First name", value, 1);
        lastNameField.Validator = value => FormValidator.ValidateLettersOnly("Last name", value, 1);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnYesOrNext += HandleSave;
        BrailleMapping.OnDeleteOrNo += HandleCancel;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnYesOrNext -= HandleSave;
        BrailleMapping.OnDeleteOrNo -= HandleCancel;
    }

    /// <summary>Opens the panel prefilled with the given student's current data. Defaults focus to First Name.</summary>
    public void Show(string studentNumber, string firstName, string lastName, Action<string, string, string> onSave, Action onCancel)
    {
        this.onSave = onSave;
        this.onCancel = onCancel;

        studentNumberField.Configure("Student number", "Type the student's number.", BrailleInputMode.Numbers);
        firstNameField.Configure("First name", "Type the student's first name.", BrailleInputMode.Letters);
        lastNameField.Configure("Last name", "Type the student's last name.", BrailleInputMode.Letters);

        // Configure() blanks each field, so the prefill has to happen after.
        SetFieldText(studentNumberField, studentNumber);
        SetFieldText(firstNameField, firstName);
        SetFieldText(lastNameField, lastName);

        state = State.Selecting;
        selectedIndex = 1; // First Name is the default focus

        if (panelRoot != null)
            panelRoot.SetActive(true);

        AccessibilityManager.Instance.Announce(
            "Editing student. Use up and down to choose a field. Press Enter to type. " +
            "Press Space to save. Press Backspace to cancel.");

        AnnounceSelectedField(firstVisit: true);
        UpdateSelectedFieldLabel();
    }

    private void SetFieldText(BrailleInputField field, string value)
    {
        if (field.inputField != null)
            field.inputField.text = value ?? string.Empty;
    }

    private bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void HandleUp()
    {
        if (!IsOpen || state != State.Selecting) return;
        MoveSelection(-1);
    }

    private void HandleDown()
    {
        if (!IsOpen || state != State.Selecting) return;
        MoveSelection(1);
    }

    private void MoveSelection(int direction)
    {
        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, fields.Length - 1);
        if (newIndex == selectedIndex) return;

        selectedIndex = newIndex;
        AnnounceSelectedField(firstVisit: false);
        UpdateSelectedFieldLabel();
    }

    private void HandleSubmit()
    {
        if (!IsOpen) return;

        if (state == State.Selecting)
        {
            state = State.Editing;
            fields[selectedIndex].SetFocused(true);
            AccessibilityManager.Instance.Announce("Editing " + fields[selectedIndex].ElementLabel + ". Start typing, then press Enter when done.");
        }
        else
        {
            fields[selectedIndex].SetFocused(false);
            state = State.Selecting;
            AnnounceSelectedField(firstVisit: false);
        }
    }

    /// <summary>Only acts as "save everything" when field-selecting - while actively typing, Space is BrailleInputField's own "insert space" input instead.</summary>
    private void HandleSave()
    {
        if (!IsOpen || state != State.Selecting) return;

        string error = studentNumberField.Validate() ?? firstNameField.Validate() ?? lastNameField.Validate();
        if (error != null)
        {
            AccessibilityManager.Instance.Announce(error);
            return;
        }

        string newNumber = studentNumberField.Value;
        string newFirstName = firstNameField.Value;
        string newLastName = lastNameField.Value;

        Close();
        onSave?.Invoke(newNumber, newFirstName, newLastName);
    }

    /// <summary>Only acts as "cancel the panel" when field-selecting - while actively typing, Backspace is BrailleInputField's own "delete character" input instead.</summary>
    private void HandleCancel()
    {
        if (!IsOpen || state != State.Selecting) return;

        Close();
        onCancel?.Invoke();
    }

    private void Close()
    {
        foreach (BrailleInputField field in fields)
            field.SetFocused(false);

        state = State.Selecting;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void AnnounceSelectedField(bool firstVisit)
    {
        AccessibilityManager.Instance.Announce(fields[selectedIndex].GetFocusAnnouncement(firstVisit));
    }

    private void UpdateSelectedFieldLabel()
    {
        if (selectedFieldLabel != null)
            selectedFieldLabel.text = "Editing: " + fields[selectedIndex].ElementLabel;
    }
}