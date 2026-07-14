using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Drives keyboard/braille-key navigation over the Student Scoreboard
/// table: Up/Down selects a row, Enter opens StudentEditPanel to edit that
/// student's number/first/last name, R asks to delete the row (with a
/// Space=yes/Backspace=no confirm, matching the rest of the app's
/// convention). Announces every step via AccessibilityManager so a
/// screen-reader-only teacher can drive the whole table without sighted
/// help.
///
/// R is used for delete because BrailleMapping already maps its repeatKey
/// to R and fires OnRepeat - no new key binding needed.
///
/// This is a new, purpose-built navigator (not QuizBackHandler or
/// AccessibleFormNavigator) - this scene needs row-list navigation plus a
/// delete-confirm and an edit hand-off, which don't match either existing
/// navigator's shape.
/// </summary>
public class StudentTableNavigator : MonoBehaviour
{
    [Header("Collaborators")]
    [SerializeField] private StudentProfilesManager profilesManager;
    [SerializeField] private Transform contentParent;
    [SerializeField] private StudentEditPanel editPanel;

    [Header("Delete confirm")]
    [SerializeField] private GameObject deleteConfirmOverlay;
    [SerializeField] private TMP_Text deleteConfirmText;
    [SerializeField] private string deleteConfirmMessage = "Do you want to delete this record? Press Space for yes. Press Backspace for no.";

    private FirestoreStudentService studentService;

    private enum Mode { Rows, ConfirmingDelete, Editing }
    private Mode mode = Mode.Rows;

    private int selectedIndex = -1;
    private readonly List<StudentRowView> rows = new List<StudentRowView>();

    private void Awake()
    {
        // Constructed here, not as a field initializer - Firestore's
        // constructor checks Application.isPlaying internally, which Unity
        // only allows from a lifecycle method.
        studentService = new FirestoreStudentService();

        if (deleteConfirmOverlay != null)
            deleteConfirmOverlay.SetActive(false);
    }

    private void Start()
    {
        StartCoroutine(OpeningAnnouncementRoutine());
    }

    private IEnumerator OpeningAnnouncementRoutine()
    {
        yield return AccessibilityManager.Instance.AnnounceAndWait("You are now on student scoreboard.");
        yield return AccessibilityManager.Instance.AnnounceAndWait(
            "Use up and down to move between students. Press Enter to edit a student's number, first name, or last name. " +
            "Press R to delete a student, which will ask you to confirm.");

        RefreshRowList();
        SelectRow(0);
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnSubmit += HandleSubmit;
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnSubmit -= HandleSubmit;
        BrailleMapping.OnRepeat -= HandleRepeat;

        StopListeningForDeleteConfirm();
    }

    /// <summary>Re-syncs this navigator's row list against contentParent's current children. Call after any StudentProfilesManager.RefreshAsync() (edit, delete) that changes the rows.</summary>
    public void RefreshRowList()
    {
        rows.Clear();
        foreach (Transform child in contentParent)
        {
            StudentRowView row = child.GetComponent<StudentRowView>();
            if (row != null)
                rows.Add(row);
        }
    }

    private void HandleUp()
    {
        if (mode != Mode.Rows) return;
        SelectRow(selectedIndex - 1);
    }

    private void HandleDown()
    {
        if (mode != Mode.Rows) return;
        SelectRow(selectedIndex + 1);
    }

    private void SelectRow(int index)
    {
        if (rows.Count == 0)
        {
            selectedIndex = -1;
            AccessibilityManager.Instance.Announce("No students found.");
            return;
        }

        index = Mathf.Clamp(index, 0, rows.Count - 1);
        if (index == selectedIndex) return;

        selectedIndex = index;
        StudentRowView row = rows[selectedIndex];

        // Reuses the row's existing Button (Transition: Color Tint) as the
        // visual selection indicator for sighted teachers, via Unity's
        // normal UI focus highlight - no extra visuals needed.
        Button button = row.GetComponent<Button>();
        if (button != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);

        AccessibilityManager.Instance.Announce(
            $"{row.FirstName} {row.LastName}, student number {row.StudentNumber}. Row {selectedIndex + 1} of {rows.Count}.");
    }

    private void HandleSubmit()
    {
        if (mode != Mode.Rows || selectedIndex < 0 || selectedIndex >= rows.Count) return;

        mode = Mode.Editing;
        StudentRowView row = rows[selectedIndex];

        editPanel.Show(
            row.StudentNumber,
            row.FirstName,
            row.LastName,
            onSave: HandleEditSaved,
            onCancel: HandleEditCancelled);
    }

    private void HandleEditSaved(string newStudentNumber, string newFirstName, string newLastName)
    {
        StartCoroutine(SaveEditRoutine(rows[selectedIndex].StudentNumber, newStudentNumber, newFirstName, newLastName));
    }

    private IEnumerator SaveEditRoutine(string oldStudentNumber, string newStudentNumber, string newFirstName, string newLastName)
    {
        AccessibilityManager.Instance.Announce("Saving changes.");

        // TotalScore/HighestScore/etc are intentionally left at their
        // default 0 here - per StudentProfilesManager's own comments,
        // those fields are dead (scores are computed live from the
        // Progress subcollection), so this can't clobber real data.
        StudentData updated = new StudentData { FirstName = newFirstName, LastName = newLastName };

        Task saveTask = studentService.RenameStudentAsync(oldStudentNumber, newStudentNumber, updated);
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.Exception != null)
        {
            Debug.LogException(saveTask.Exception);
            AccessibilityManager.Instance.Announce("Something went wrong saving that student. Please try again.");
            mode = Mode.Rows;
            yield break;
        }

        yield return AccessibilityManager.Instance.AnnounceAndWait("Saved.");

        Task refreshTask = profilesManager.RefreshAsync();
        yield return new WaitUntil(() => refreshTask.IsCompleted);

        RefreshRowList();
        mode = Mode.Rows;

        int restoredIndex = rows.FindIndex(r => r.StudentNumber == newStudentNumber);
        selectedIndex = -1;
        SelectRow(restoredIndex >= 0 ? restoredIndex : 0);
    }

    private void HandleEditCancelled()
    {
        mode = Mode.Rows;
        AccessibilityManager.Instance.Announce("Edit cancelled.");
    }

    private void HandleRepeat()
    {
        if (mode != Mode.Rows || selectedIndex < 0 || selectedIndex >= rows.Count) return;

        mode = Mode.ConfirmingDelete;

        if (deleteConfirmText != null) deleteConfirmText.text = deleteConfirmMessage;
        if (deleteConfirmOverlay != null) deleteConfirmOverlay.SetActive(true);

        AccessibilityManager.Instance.Announce(deleteConfirmMessage);

        BrailleMapping.OnYesOrNext += HandleDeleteConfirmed;
        BrailleMapping.OnDeleteOrNo += HandleDeleteCancelled;
    }

    private void HandleDeleteConfirmed()
    {
        StopListeningForDeleteConfirm();
        if (deleteConfirmOverlay != null) deleteConfirmOverlay.SetActive(false);

        StartCoroutine(DeleteRoutine(rows[selectedIndex].StudentNumber));
    }

    private IEnumerator DeleteRoutine(string studentNumber)
    {
        AccessibilityManager.Instance.Announce("Deleting student.");

        Task deleteTask = studentService.DeleteStudentAsync(studentNumber);
        yield return new WaitUntil(() => deleteTask.IsCompleted);

        if (deleteTask.Exception != null)
        {
            Debug.LogException(deleteTask.Exception);
            AccessibilityManager.Instance.Announce("Something went wrong deleting that student. Please try again.");
            mode = Mode.Rows;
            yield break;
        }

        yield return AccessibilityManager.Instance.AnnounceAndWait("Deleted.");

        Task refreshTask = profilesManager.RefreshAsync();
        yield return new WaitUntil(() => refreshTask.IsCompleted);

        RefreshRowList();
        mode = Mode.Rows;
        selectedIndex = -1;

        if (rows.Count > 0)
            SelectRow(0);
        else
            AccessibilityManager.Instance.Announce("No students remaining.");
    }

    private void HandleDeleteCancelled()
    {
        StopListeningForDeleteConfirm();
        if (deleteConfirmOverlay != null) deleteConfirmOverlay.SetActive(false);

        mode = Mode.Rows;
        AccessibilityManager.Instance.Announce("Delete cancelled.");
    }

    private void StopListeningForDeleteConfirm()
    {
        BrailleMapping.OnYesOrNext -= HandleDeleteConfirmed;
        BrailleMapping.OnDeleteOrNo -= HandleDeleteCancelled;
    }
}