using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using BraillePlay.GameMenu;

/// <summary>
/// Loads every student from Firestore and spawns one StudentRowView per
/// student under Content. "Total Score" and "Current Lesson" are computed
/// live from each student's Progress/current document, NOT read from
/// StudentData.TotalScore/CurrentLesson - those two fields are dead (nothing
/// in the quiz pipeline writes to them), so reading them directly would show
/// 0 for every student regardless of what they've actually completed.
/// </summary>
public class StudentProfilesManager : MonoBehaviour
{
    [SerializeField] private StudentRowView rowPrefab;
    [SerializeField] private Transform contentParent;

    private FirestoreStudentService studentService;
    private FirestoreProgressService progressService;

    private void Awake()
    {
        // Constructed here rather than as field initializers - Firestore's
        // constructor checks Application.isPlaying internally, and Unity
        // only allows that check from a lifecycle method (Awake/Start/etc),
        // not from field initializers or a MonoBehaviour's implicit
        // constructor.
        studentService = new FirestoreStudentService();
        progressService = new FirestoreProgressService();
    }

    private void Start()
    {
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        List<(string StudentNumber, StudentData Data)> students;
        try
        {
            students = await studentService.ListAllStudentsAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[StudentProfilesManager] Failed to load student list: " + e);
            return;
        }

        // Sort by student number so the list order is stable/predictable
        // between refreshes instead of whatever order Firestore returns.
        foreach (var (studentNumber, data) in students.OrderBy(s => s.StudentNumber))
        {
            StudentProgress progress;
            try
            {
                progress = await progressService.LoadProgressAsync(studentNumber);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StudentProfilesManager] Couldn't load progress for {studentNumber}, showing zeros: " + e);
                progress = StudentProgress.CreateDefault();
            }

            (int totalScore, int bestScore, int currentLesson) = ComputeSummary(progress);

            StudentRowView row = Instantiate(rowPrefab, contentParent);
            row.SetData(
                studentNumber,
                data.FirstName,
                data.LastName,
                highestScore: bestScore,
                currentLessonLabel: "Lesson " + currentLesson,
                totalScore: totalScore);

            row.SetClickHandler(HandleRowClicked);
        }
    }

    /// <summary>
    /// Total score = sum of HighestScore across every quiz the student has
    /// touched. Best score = the single highest quiz score among those.
    /// Current lesson = the highest lesson number that has an unlocked
    /// quiz (progression unlocks sequentially, so this is always "where
    /// they currently are").
    /// </summary>
    private static (int totalScore, int bestScore, int currentLesson) ComputeSummary(StudentProgress progress)
    {
        int totalScore = 0;
        int bestScore = 0;
        int currentLesson = 1;

        foreach (var lessonEntry in progress.Lessons)
        {
            // Keys are "lesson1", "lesson2", ... - parse the number back out.
            if (!int.TryParse(lessonEntry.Key.Replace("lesson", ""), out int lessonNumber))
                continue;

            bool anyUnlocked = false;
            foreach (QuizProgress quiz in lessonEntry.Value.Quizzes.Values)
            {
                totalScore += quiz.HighestScore;
                if (quiz.HighestScore > bestScore) bestScore = quiz.HighestScore;
                if (quiz.Unlocked) anyUnlocked = true;
            }

            if (anyUnlocked && lessonNumber > currentLesson)
                currentLesson = lessonNumber;
        }

        return (totalScore, bestScore, currentLesson);
    }

    private void HandleRowClicked(string studentNumber)
    {
        // Next step: switch to the Detail state and load this student's
        // full per-lesson breakdown + edit/delete actions.
        Debug.Log("[StudentProfilesManager] Row clicked for student " + studentNumber);
    }
}