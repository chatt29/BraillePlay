using System;
using TMPro;
using UnityEngine;

/// <summary>What kind of characters a BrailleInputField accepts.</summary>
public enum BrailleInputMode
{
    /// <summary>Only letters (a-z).</summary>
    Letters,

    /// <summary>Only digits. Braille has no separate digit chords, so this
    /// reinterprets the a-j letter chords as 1-9,0 - the standard braille
    /// numeral convention.</summary>
    Numbers,

    /// <summary>Anything BrailleTextInput can resolve (letters + punctuation). Useful for usernames/passwords.</summary>
    Any
}

/// <summary>
/// A single accessible text field driven entirely by braille chords instead
/// of normal keyboard typing. Displays its text in a TMP_InputField for
/// sighted assistants/teachers, but this component is the source of truth -
/// the TMP_InputField is set read-only so it can't be typed into directly.
///
/// Only reacts to chord/backspace/space input while it has focus (set by
/// AccessibleFormNavigator via SetFocused), since BrailleMapping's events
/// are static and broadcast to every field in the scene at once.
/// </summary>
public class BrailleInputField : MonoBehaviour, IAccessibleFormElement
{
    [Header("Visual (for sighted assistants only - not required to play)")]
    public TMP_InputField inputField;

    [Header("Options")]
    public bool allowSpaces = false;
    public bool isPassword = false;

    /// <summary>Assigned by whichever manager configures this field. Returns null if the current value is valid.</summary>
    public Func<string, string> Validator;

    public string ElementLabel { get; private set; }

    private string entryPrompt;
    private BrailleInputMode mode;
    private bool hasFocus;

    public string Value => inputField != null ? inputField.text : string.Empty;

    /// <summary>Sets this field's label, spoken prompt, and input mode. Call once before the form starts navigating.</summary>
    public void Configure(string label, string prompt, BrailleInputMode inputMode, bool spacesAllowed = false, bool passwordField = false)
    {
        ElementLabel = label;
        entryPrompt = prompt;
        mode = inputMode;
        allowSpaces = spacesAllowed;
        isPassword = passwordField;

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.contentType = passwordField ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            inputField.readOnly = true; // typing happens only via braille chords, never the on-screen keyboard
        }
    }

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleChordSubmitted;
        BrailleMapping.OnDeleteOrNo += HandleBackspace;
        BrailleMapping.OnSpace += HandleSpace;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleChordSubmitted;
        BrailleMapping.OnDeleteOrNo -= HandleBackspace;
        BrailleMapping.OnSpace -= HandleSpace;
    }

    private void HandleChordSubmitted(string pattern)
    {
        if (!hasFocus) return;

        if (!BrailleTextInput.TryGetChar(pattern, out char letter))
            return;

        char? resolved = ResolveForMode(letter);
        if (resolved == null)
        {
            AccessibilityManager.Instance.Announce(mode == BrailleInputMode.Numbers
                ? "Only numbers are allowed here."
                : "Only letters are allowed here.");
            return;
        }

        AppendCharacter(resolved.Value);
    }

    private char? ResolveForMode(char letter)
    {
        switch (mode)
        {
            case BrailleInputMode.Any:
                return letter;

            case BrailleInputMode.Letters:
                return char.IsLetter(letter) ? letter : (char?)null;

            case BrailleInputMode.Numbers:
                // Standard braille numerals: chords a-j double as digits 1-9,0.
                if (letter < 'a' || letter > 'j')
                    return null;
                const string digitsAToJ = "1234567890";
                return digitsAToJ[letter - 'a'];

            default:
                return null;
        }
    }

    private void AppendCharacter(char c)
    {
        if (inputField == null) return;

        inputField.text += c;
        AccessibilityManager.Instance.Announce(isPassword ? "dot" : c.ToString());
    }

    private void HandleBackspace()
    {
        if (!hasFocus || inputField == null) return;

        if (inputField.text.Length == 0)
        {
            AccessibilityManager.Instance.Announce(ElementLabel + " is already empty.");
            return;
        }

        inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        AccessibilityManager.Instance.Announce("Deleted.");
    }

    private void HandleSpace()
    {
        if (!hasFocus || inputField == null) return;

        if (!allowSpaces)
        {
            AccessibilityManager.Instance.Announce(ElementLabel + " does not allow spaces.");
            return;
        }

        inputField.text += " ";
        AccessibilityManager.Instance.Announce("Space.");
    }

    // ---------------- IAccessibleFormElement ----------------

    public string GetFocusAnnouncement(bool firstVisit)
    {
        string msg = ElementLabel + " field.";

        if (firstVisit)
            msg += " " + entryPrompt;

        msg += string.IsNullOrEmpty(Value)
            ? " Currently empty."
            : " Currently contains " + (isPassword ? Value.Length + " characters" : Value) + ".";

        return msg;
    }

    public string Validate() => Validator != null ? Validator(Value) : null;

    public void ActivateSubmit()
    {
        string error = Validate();
        AccessibilityManager.Instance.Announce(error ?? (ElementLabel + " looks good."));
    }

    public void SetFocused(bool focused) => hasFocus = focused;
}