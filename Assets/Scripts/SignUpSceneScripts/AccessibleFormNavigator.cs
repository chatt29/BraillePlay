using System;
using System.Collections.Generic;
using UnityEngine;

public interface IAccessibleFormElement
{
    string ElementLabel { get; }
    string GetFocusAnnouncement(bool firstVisit);
    string Validate();
    void ActivateSubmit();
    void SetFocused(bool focused);
}

/// <summary>
/// Implemented by elements that can speak with recorded voice instead of TTS.
/// The navigator checks IsVoiceMode and uses these members when true.
/// </summary>
public interface IVoiceFormElement
{
    bool IsVoiceMode { get; }
    void AnnounceFocusVoice(bool firstVisit);
    VoiceKey? ValidateVoice();
    void StopVoice();
}

public class SubmitButtonElement : IAccessibleFormElement, IVoiceFormElement
{
    // Legacy (TTS) data
    private readonly string label;
    private readonly string prompt;
    private readonly Func<string> validateAllFields;

    // Voice data
    private readonly bool useVoice;
    private readonly VoiceKey labelKey;
    private readonly VoiceKey hintKey;
    private readonly Func<VoiceKey?> validateAllVoice;

    private readonly Action onActivate;

    /// <summary>Legacy TTS constructor (sign-up scenes).</summary>
    public SubmitButtonElement(string label, string prompt, Func<string> validateAllFields, Action onActivate)
    {
        this.label = label;
        this.prompt = prompt;
        this.validateAllFields = validateAllFields;
        this.onActivate = onActivate;
        useVoice = false;
    }

    /// <summary>Recorded-voice constructor (login scene).</summary>
    public SubmitButtonElement(VoiceKey labelKey, VoiceKey hintKey, Func<VoiceKey?> validateAllVoice, Action onActivate)
    {
        this.labelKey = labelKey;
        this.hintKey = hintKey;
        this.validateAllVoice = validateAllVoice;
        this.onActivate = onActivate;
        useVoice = true;
        label = labelKey.ToString(); // debug only
    }

    public string ElementLabel => label;

    public string GetFocusAnnouncement(bool firstVisit)
    {
        if (useVoice) return string.Empty; // never used in voice mode
        return firstVisit ? label + ". " + prompt : label + ".";
    }

    public string Validate()
    {
        if (useVoice)
        {
            VoiceKey? v = ValidateVoice();
            return v.HasValue ? v.Value.ToString() : null;
        }
        return validateAllFields?.Invoke();
    }

    public void ActivateSubmit() => onActivate?.Invoke();

    public void SetFocused(bool focused) { }

    // ---- IVoiceFormElement ----
    public bool IsVoiceMode => useVoice;

    public void AnnounceFocusVoice(bool firstVisit)
    {
        if (firstVisit) VoiceManager.Instance.Speak(labelKey, hintKey);
        else VoiceManager.Instance.Speak(labelKey);
    }

    public VoiceKey? ValidateVoice() => validateAllVoice?.Invoke();

    public void StopVoice() { }
}

public class AccessibleFormNavigator : MonoBehaviour
{
    public event Action OnBackRequested;

    private readonly List<IAccessibleFormElement> elements = new List<IAccessibleFormElement>();
    private readonly HashSet<int> visitedIndices = new HashSet<int>();
    private int currentIndex;
    private Action repeatAction;
    private bool active;

    public void Setup(List<IAccessibleFormElement> formElements)
    {
        elements.Clear();
        elements.AddRange(formElements);
        visitedIndices.Clear();
        currentIndex = 0;
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnBack += HandleBack;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnBack -= HandleBack;

        active = false;
    }

    public void BeginNavigation()
    {
        if (elements.Count == 0)
        {
            Debug.LogWarning("[AccessibleFormNavigator] No elements configured - call Setup() first.");
            return;
        }

        active = true;
        currentIndex = 0;
        FocusCurrent();
    }

    private void HandleUp() => Move(-1);
    private void HandleDown() => Move(1);

    /// <summary>Returns the element as a voice element only if it is in voice mode.</summary>
    private IVoiceFormElement VoiceOf(IAccessibleFormElement element)
    {
        var v = element as IVoiceFormElement;
        return (v != null && v.IsVoiceMode) ? v : null;
    }

    private void Move(int direction)
    {
        if (!active) return;

        var current = elements[currentIndex];
        var voice = VoiceOf(current);

        // Validate before leaving
        if (voice != null)
        {
            VoiceKey? voiceError = voice.ValidateVoice();
            if (voiceError != null)
            {
                SpeakVoice(voiceError.Value, voice);
                return;
            }
        }
        else
        {
            string error = current.Validate();
            if (error != null)
            {
                Speak(error);
                return;
            }
        }

        int newIndex = Mathf.Clamp(currentIndex + direction, 0, elements.Count - 1);

        if (newIndex == currentIndex)
        {
            if (voice != null)
                SpeakVoice(direction < 0 ? VoiceKey.WarnFirstField : VoiceKey.WarnLastField, voice);
            else
                Speak(direction < 0 ? "This is the first field." : "This is the last field.");
            return;
        }

        current.SetFocused(false);
        currentIndex = newIndex;
        FocusCurrent();
    }

    private void FocusCurrent()
    {
        bool firstVisit = !visitedIndices.Contains(currentIndex);
        visitedIndices.Add(currentIndex);

        var element = elements[currentIndex];
        element.SetFocused(true);

        var voice = VoiceOf(element);
        if (voice != null)
        {
            VoiceManager.Instance.Stop();
            voice.AnnounceFocusVoice(firstVisit);
            repeatAction = () =>
            {
                VoiceManager.Instance.Stop();
                voice.AnnounceFocusVoice(false);
            };
        }
        else
        {
            Speak(element.GetFocusAnnouncement(firstVisit));
        }
    }

    private void HandleSubmit()
    {
        if (!active) return;

        var element = elements[currentIndex];
        VoiceOf(element)?.StopVoice();
        element.ActivateSubmit();
    }

    private void HandleRepeat()
    {
        if (!active) return;
        repeatAction?.Invoke();
    }

    private void HandleBack()
    {
        OnBackRequested?.Invoke();
    }

    // Legacy TTS
    private void Speak(string message)
    {
        repeatAction = () => AccessibilityManager.Instance.Announce(message);
        AccessibilityManager.Instance.Announce(message);
    }

    // Recorded voice (stops the field's own focus routine first so it can't talk over it)
    private void SpeakVoice(VoiceKey key, IVoiceFormElement voice)
    {
        voice.StopVoice();
        repeatAction = () => VoiceManager.Instance.Play(key);
        VoiceManager.Instance.Play(key);
    }
}