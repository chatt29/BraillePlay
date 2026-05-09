using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SignupBrailleInput : MonoBehaviour
{
    [Header("Scene Input References")]
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Flow Reference")]
    [SerializeField] private AccessibleSignupFlow signupFlow;

    [Header("Options")]
    [SerializeField] private bool blockPhysicalKeyboardTyping = true;
    [SerializeField] private bool logBrailleLetters = false;
    [SerializeField] private bool allowSpaceCharacter = false;

    private TMP_InputField activeField;
    private bool numberMode = false;
    private void Update()
    {
        UpdateActiveField();

        if (blockPhysicalKeyboardTyping)
        {
            SetReadOnly(firstNameInput, true);
            SetReadOnly(lastNameInput, true);
            SetReadOnly(usernameInput, true);
            SetReadOnly(passwordInput, true);
        }
    }

    private void SetReadOnly(TMP_InputField field, bool value)
    {
        if (field != null)
            field.readOnly = value;
    }

    private void UpdateActiveField()
    {
        if (EventSystem.current == null)
        {
            activeField = null;
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            activeField = null;
            return;
        }

        activeField = selected.GetComponent<TMP_InputField>();
    }


    private bool CanInsertIntoActiveField(string value)
    {
        if (activeField == null)
            return false;

        if (activeField == passwordInput)
            return true;

        if (activeField == usernameInput)
        {
            if (value == " ")
                return false;

            return true;
        }

        if (activeField == firstNameInput || activeField == lastNameInput)
        {
            if (value == " ")
                return allowSpaceCharacter;

            return true;
        }

        return true;
    }

    private void HandleDelete()
    {
        if (activeField == null)
        {
            if (signupFlow != null)
                signupFlow.GoToPreviousField();

            numberMode = false;
            return;
        }

        string text = activeField.text;

        if (string.IsNullOrEmpty(text))
        {
            if (signupFlow != null)
                signupFlow.GoToPreviousField();

            numberMode = false;
            return;
        }

        int caret = activeField.stringPosition;
        if (caret <= 0 || caret > text.Length)
            caret = text.Length;

        text = text.Remove(caret - 1, 1);
        activeField.text = text;

        SetCaret(activeField, caret - 1);
        numberMode = false;
    }

    private void InsertText(string value)
{
    if (activeField == null || string.IsNullOrEmpty(value))
        return;

    string text = activeField.text;
    int caret = activeField.stringPosition;

    if (caret < 0 || caret > text.Length)
        caret = text.Length;

    text = text.Insert(caret, value);
    activeField.text = text;

    SetCaret(activeField, caret + value.Length);

    activeField.ActivateInputField();
    activeField.Select();
}
    private void SetCaret(TMP_InputField field, int position)
    {
        position = Mathf.Clamp(position, 0, field.text.Length);
        field.stringPosition = position;
        field.caretPosition = position;
        field.selectionAnchorPosition = position;
        field.selectionFocusPosition = position;
        field.ForceLabelUpdate();
    }
    
}