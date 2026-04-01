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

    private void OnEnable()
    {
        BrailleMapping.OnDot1 += HandleBrailleInput;
        BrailleMapping.OnDot2 += HandleBrailleInput;
        BrailleMapping.OnDot3 += HandleBrailleInput;
        BrailleMapping.OnDot4 += HandleBrailleInput;
        BrailleMapping.OnDot5 += HandleBrailleInput;
        BrailleMapping.OnDot6 += HandleBrailleInput;

        BrailleMapping.OnDeleteOrNo += HandleDelete;
    }

    private void OnDisable()
    {
        BrailleMapping.OnDot1 -= HandleBrailleInput;
        BrailleMapping.OnDot2 -= HandleBrailleInput;
        BrailleMapping.OnDot3 -= HandleBrailleInput;
        BrailleMapping.OnDot4 -= HandleBrailleInput;
        BrailleMapping.OnDot5 -= HandleBrailleInput;
        BrailleMapping.OnDot6 -= HandleBrailleInput;

        BrailleMapping.OnDeleteOrNo -= HandleDelete;
    }

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

    private void HandleBrailleInput()
    {
        if (activeField == null)
            return;

        if (BrailleMapping.Instance == null)
            return;

        string pattern = BrailleMapping.Instance.GetCurrentBraillePattern();
        string letter = TranslateBraille(pattern);

        if (string.IsNullOrEmpty(letter))
            return;

        if (!CanInsertIntoActiveField(letter))
            return;

        if (logBrailleLetters)
            Debug.Log("Braille Pattern: " + pattern + " -> " + letter);

        InsertText(letter);
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

            return;
        }

        string text = activeField.text;

        if (string.IsNullOrEmpty(text))
        {
            if (signupFlow != null)
                signupFlow.GoToPreviousField();

            return;
        }

        int caret = activeField.stringPosition;
        if (caret <= 0 || caret > text.Length)
            caret = text.Length;

        text = text.Remove(caret - 1, 1);
        activeField.text = text;

        SetCaret(activeField, caret - 1);
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

    private string TranslateBraille(string pattern)
    {
        switch (pattern)
        {
            case "100000": return "a";
            case "110000": return "b";
            case "100100": return "c";
            case "100110": return "d";
            case "100010": return "e";
            case "110100": return "f";
            case "110110": return "g";
            case "110010": return "h";
            case "010100": return "i";
            case "010110": return "j";
            case "101000": return "k";
            case "111000": return "l";
            case "101100": return "m";
            case "101110": return "n";
            case "101010": return "o";
            case "111100": return "p";
            case "111110": return "q";
            case "111010": return "r";
            case "011100": return "s";
            case "011110": return "t";
            case "101001": return "u";
            case "111001": return "v";
            case "010111": return "w";
            case "101101": return "x";
            case "101111": return "y";
            case "101011": return "z";

            case "000000": return "";
            default: return "";
        }
    }
}