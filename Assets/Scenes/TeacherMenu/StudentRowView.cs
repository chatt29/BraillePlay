using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives on the StudentRow prefab. Knows how to display one student's data
/// and nothing else - no Firestore calls, no list management. Attach this
/// to the Row prefab alongside its 6 existing TMP_Text children.
/// </summary>
public class StudentRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text studentNumberText;
    [SerializeField] private TMP_Text firstNameText;
    [SerializeField] private TMP_Text lastNameText;
    [SerializeField] private TMP_Text highestScoreText;
    [SerializeField] private TMP_Text currentLessonText;
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Selection highlight")]
    [Tooltip("Defaults to this GameObject's own Image component if left empty - the row prefab already has one.")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.35f); // amber highlight, distinct from white cells

    /// <summary>The student number this row is currently showing - StudentProfilesManager/StudentTableNavigator read this to know which student a row represents.</summary>
    public string StudentNumber { get; private set; }

    /// <summary>Exposed for StudentTableNavigator's TTS announcements and for prefilling StudentEditPanel - StudentRowView itself never speaks or edits anything.</summary>
    public string FirstName { get; private set; }

    /// <summary>Exposed for StudentTableNavigator's TTS announcements and for prefilling StudentEditPanel - StudentRowView itself never speaks or edits anything.</summary>
    public string LastName { get; private set; }

    private Action<string> onClicked;

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        SetSelected(false);
    }

    /// <summary>Toggles this row's highlight. Called by StudentTableNavigator as Up/Down moves focus between rows - independent of Unity's built-in Button highlight state, since that alone wasn't visually distinct enough against the white table cells.</summary>
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    public void SetData(string studentNumber, string firstName, string lastName,
        int highestScore, string currentLessonLabel, int totalScore)
    {
        StudentNumber = studentNumber;
        FirstName = firstName;
        LastName = lastName;

        if (studentNumberText != null) studentNumberText.text = studentNumber;
        if (firstNameText != null) firstNameText.text = firstName;
        if (lastNameText != null) lastNameText.text = lastName;
        if (highestScoreText != null) highestScoreText.text = highestScore.ToString();
        if (currentLessonText != null) currentLessonText.text = currentLessonLabel;
        if (totalScoreText != null) totalScoreText.text = totalScore.ToString();
    }

    /// <summary>Wires this row's click (needs a Button component on the Row prefab's root - add one if it isn't there yet) to open this student's detail view.</summary>
    public void SetClickHandler(Action<string> handler)
    {
        onClicked = handler;

        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning("[StudentRowView] No Button component on the Row prefab - rows won't be clickable yet. Add one (it can be fully transparent) to enable opening student detail.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked?.Invoke(StudentNumber));
    }
}