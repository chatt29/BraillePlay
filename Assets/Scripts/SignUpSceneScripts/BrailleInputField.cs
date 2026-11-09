using System;
using System.Collections;
using TMPro;
using UnityEngine;

public enum BrailleInputMode
{
    Letters,
    Numbers,
    Any
}

public class BrailleInputField : MonoBehaviour, IAccessibleFormElement, IVoiceFormElement
{
    [Header("Visual (for sighted assistants only - not required to play)")]
    public TMP_InputField inputField;

    [Header("Options")]
    public bool allowSpaces = false;
    public bool isPassword = false;
    public void StopVoice() => StopFocusRoutine();
    /// <summary>Legacy (TTS/string) validator. Used by sign-up scenes.</summary>
    public Func<string, string> Validator;

    /// <summary>Recorded-voice validator. Returns null when valid.</summary>
    public Func<string, VoiceKey?> VoiceValidator;

    public string ElementLabel { get; private set; }

    private string entryPrompt;
    private BrailleInputMode mode;
    private bool hasFocus;

    // Recorded-voice mode
    private bool useVoice;
    private VoiceKey labelKey;
    private VoiceKey hintKey;
    private Coroutine focusRoutine;

    public string Value => inputField != null ? inputField.text : string.Empty;

    /// <summary>Legacy configure (TTS). Unchanged behaviour.</summary>
    public void Configure(string label, string prompt, BrailleInputMode inputMode, bool spacesAllowed = false, bool passwordField = false)
    {
        useVoice = false;
        ElementLabel = label;
        entryPrompt = prompt;
        ApplyCommon(inputMode, spacesAllowed, passwordField);
    }

    /// <summary>Recorded-voice configure.</summary>
    public void Configure(VoiceKey label, VoiceKey hint, BrailleInputMode inputMode, bool spacesAllowed = false, bool passwordField = false)
    {
        useVoice = true;
        labelKey = label;
        hintKey = hint;
        ElementLabel = label.ToString(); // debug only, never spoken
        entryPrompt = string.Empty;
        ApplyCommon(inputMode, spacesAllowed, passwordField);
    }

    private void ApplyCommon(BrailleInputMode inputMode, bool spacesAllowed, bool passwordField)
    {
        mode = inputMode;
        allowSpaces = spacesAllowed;
        isPassword = passwordField;

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.contentType = passwordField ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            inputField.readOnly = true;
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
            StopFocusRoutine();
            if (useVoice)
                VoiceManager.Instance.Play(mode == BrailleInputMode.Numbers
                    ? VoiceKey.WarnOnlyNumbers
                    : VoiceKey.WarnOnlyLetters);
            else
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

        StopFocusRoutine();
        inputField.text += c;

        if (useVoice)
        {
            if (isPassword) VoiceManager.Instance.Play(VoiceKey.FbDot);
            else if (char.IsDigit(c)) VoiceManager.Instance.PlayDigit(c);
            else VoiceManager.Instance.PlayLetter(c);
        }
        else
        {
            AccessibilityManager.Instance.Announce(isPassword ? "dot" : c.ToString());
        }
    }

    private void HandleBackspace()
    {
        if (!hasFocus || inputField == null) return;
        StopFocusRoutine();

        if (inputField.text.Length == 0)
        {
            if (useVoice) VoiceManager.Instance.Play(VoiceKey.WarnNothingToDelete);
            else AccessibilityManager.Instance.Announce(ElementLabel + " is already empty.");
            return;
        }

        inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);

        if (useVoice) VoiceManager.Instance.Play(VoiceKey.FbDeleted);
        else AccessibilityManager.Instance.Announce("Deleted.");
    }

    private void HandleSpace()
    {
        if (!hasFocus || inputField == null) return;
        StopFocusRoutine();

        if (!allowSpaces)
        {
            if (useVoice) VoiceManager.Instance.Play(VoiceKey.WarnNoSpaces);
            else AccessibilityManager.Instance.Announce(ElementLabel + " does not allow spaces.");
            return;
        }

        inputField.text += " ";

        if (useVoice) VoiceManager.Instance.Play(VoiceKey.FbSpace);
        else AccessibilityManager.Instance.Announce("Space.");
    }

    // ---------------- Recorded-voice focus announcement ----------------

    /// <summary>
    /// Recorded-voice version of GetFocusAnnouncement. The navigator should
    /// call this instead of announcing a string when IsVoiceMode is true.
    /// </summary>
    public bool IsVoiceMode => useVoice;

    public void AnnounceFocusVoice(bool firstVisit)
    {
        StopFocusRoutine();
        focusRoutine = StartCoroutine(FocusVoiceRoutine(firstVisit));
    }

    private IEnumerator FocusVoiceRoutine(bool firstVisit)
    {
        yield return VoiceManager.Instance.PlayAndWait(labelKey);

        if (firstVisit)
            yield return VoiceManager.Instance.PlayAndWait(hintKey);

        if (string.IsNullOrEmpty(Value))
        {
            yield return VoiceManager.Instance.PlayAndWait(VoiceKey.FbEmpty);
            yield break;
        }

        if (isPassword) yield break;

        yield return VoiceManager.Instance.PlayAndWait(VoiceKey.FbContains);
        foreach (char c in Value)
            yield return VoiceManager.Instance.SpeakCharAndWait(c);
    }

    private void StopFocusRoutine()
    {
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }
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

    /// <summary>Legacy string validation. In voice mode returns the key name if invalid (never spoken via TTS).</summary>
    public string Validate()
    {
        if (useVoice)
        {
            VoiceKey? v = ValidateVoice();
            return v.HasValue ? v.Value.ToString() : null;
        }
        return Validator != null ? Validator(Value) : null;
    }

    public VoiceKey? ValidateVoice() => VoiceValidator != null ? VoiceValidator(Value) : null;

    public void ActivateSubmit()
    {
        if (useVoice)
        {
            VoiceManager.Instance.Play(ValidateVoice() ?? VoiceKey.WarnFieldOk);
            return;
        }

        string error = Validate();
        AccessibilityManager.Instance.Announce(error ?? (ElementLabel + " looks good."));
    }

    public void SetFocused(bool focused)
    {
        hasFocus = focused;
        if (!focused) StopFocusRoutine();
    }
}