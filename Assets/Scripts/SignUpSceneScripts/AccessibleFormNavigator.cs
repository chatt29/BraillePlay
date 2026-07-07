using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One item in an accessible form's navigation order - a text field or an
/// action like a submit button. AccessibleFormNavigator moves focus between
/// a list of these using only BrailleMapping's Up/Down/Submit/Repeat events.
/// </summary>
public interface IAccessibleFormElement
{
    /// <summary>Short spoken name, e.g. "First name" or "Submit button".</summary>
    string ElementLabel { get; }

    /// <summary>
    /// Full sentence to speak when this element receives focus.
    /// <paramref name="firstVisit"/> is true only the first time this
    /// element is focused, so a longer instruction can be given once.
    /// </summary>
    string GetFocusAnnouncement(bool firstVisit);

    /// <summary>Returns null if this element is currently valid / ok to leave, or a spoken error message describing what's wrong.</summary>
    string Validate();

    /// <summary>Called when the Submit control is pressed while this element has focus.</summary>
    void ActivateSubmit();

    /// <summary>Called by the navigator whenever focus enters or leaves this element.</summary>
    void SetFocused(bool focused);
}

/// <summary>
/// A non-text element for the end of a form, e.g. "Submit button".
/// Validating it re-validates the whole form (defense in depth - the
/// navigator already refuses to leave an invalid field, so by the time this
/// is reachable every field should already be valid); activating it runs the
/// form's final action (Firestore create, etc), provided by the manager.
/// </summary>
public class SubmitButtonElement : IAccessibleFormElement
{
    private readonly string label;
    private readonly string prompt;
    private readonly Func<string> validateAllFields;
    private readonly Action onActivate;

    public SubmitButtonElement(string label, string prompt, Func<string> validateAllFields, Action onActivate)
    {
        this.label = label;
        this.prompt = prompt;
        this.validateAllFields = validateAllFields;
        this.onActivate = onActivate;
    }

    public string ElementLabel => label;

    public string GetFocusAnnouncement(bool firstVisit)
    {
        return firstVisit ? label + ". " + prompt : label + ".";
    }

    public string Validate() => validateAllFields?.Invoke();

    public void ActivateSubmit() => onActivate?.Invoke();

    public void SetFocused(bool focused) { /* no visual focus state needed for a button */ }
}

/// <summary>
/// Moves focus through a list of <see cref="IAccessibleFormElement"/>s using
/// only BrailleMapping's Up/Down/Submit/Repeat/Back events, speaking every
/// focus change and every validation error through AccessibilityManager.
/// Never lets focus leave a field that's currently invalid.
/// Attach one of these per accessible form/scene.
/// </summary>
public class AccessibleFormNavigator : MonoBehaviour
{
    /// <summary>Raised when the Back control is pressed. The owning manager decides what that means (e.g. load the previous scene).</summary>
    public event Action OnBackRequested;

    private readonly List<IAccessibleFormElement> elements = new List<IAccessibleFormElement>();
    private readonly HashSet<int> visitedIndices = new HashSet<int>();
    private int currentIndex;
    private string lastAnnouncement;
    private bool active;

    /// <summary>Provide the ordered list of fields/buttons for this form. Call once before BeginNavigation().</summary>
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

    /// <summary>Starts the form at the first element and announces it. Call after Setup().</summary>
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

    private void Move(int direction)
    {
        if (!active) return;

        string error = elements[currentIndex].Validate();
        if (error != null)
        {
            Speak(error);
            return;
        }

        int newIndex = Mathf.Clamp(currentIndex + direction, 0, elements.Count - 1);

        if (newIndex == currentIndex)
        {
            Speak(direction < 0 ? "This is the first field." : "This is the last field.");
            return;
        }

        elements[currentIndex].SetFocused(false);
        currentIndex = newIndex;
        FocusCurrent();
    }

    private void FocusCurrent()
    {
        bool firstVisit = !visitedIndices.Contains(currentIndex);
        visitedIndices.Add(currentIndex);

        elements[currentIndex].SetFocused(true);
        Speak(elements[currentIndex].GetFocusAnnouncement(firstVisit));
    }

    private void HandleSubmit()
    {
        if (!active) return;
        elements[currentIndex].ActivateSubmit();
    }

    private void HandleRepeat()
    {
        if (!active || lastAnnouncement == null) return;
        AccessibilityManager.Instance.Announce(lastAnnouncement);
    }

    private void HandleBack()
    {
        OnBackRequested?.Invoke();
    }

    private void Speak(string message)
    {
        lastAnnouncement = message;
        AccessibilityManager.Instance.Announce(message);
    }
}