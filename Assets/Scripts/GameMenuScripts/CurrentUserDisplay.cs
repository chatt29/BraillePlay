using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Reads the currently logged-in student and displays their name.
    /// Nothing else, per the README ("Displays Student Name. Nothing else.").
    ///
    /// ASSUMPTION: UserSession exposes a static StudentNumber getter
    /// alongside the SetStudent(...) it already has (see StudentLoginManager).
    /// Adjust the property name below if yours differs.
    /// </summary>
    public class CurrentUserDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;

        private FirestoreStudentService studentService;

        private void Awake()
        {
            studentService = new FirestoreStudentService();
        }

        private void Start()
        {
            StartCoroutine(LoadAndDisplayName());
        }

        private IEnumerator LoadAndDisplayName()
        {
            string studentNumber = UserSession.StudentNumber;

            if (string.IsNullOrEmpty(studentNumber))
            {
                Debug.LogWarning("[CurrentUserDisplay] No logged-in student number found in UserSession.");
                yield break;
            }

            Task<StudentData> loadTask = studentService.LoadStudentAsync(studentNumber);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            if (loadTask.Exception != null)
            {
                Debug.LogException(loadTask.Exception);
                yield break;
            }

            StudentData student = loadTask.Result;
            if (nameLabel != null && student != null)
                nameLabel.text = student.FirstName;
        }
    }
}