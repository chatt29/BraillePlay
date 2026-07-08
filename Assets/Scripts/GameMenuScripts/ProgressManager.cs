using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Owns the current student's <see cref="StudentProgress"/> for this
    /// session: loads it once, caches it so the menu can query it
    /// synchronously every frame without re-awaiting Firestore, and is the
    /// only place that writes progress back.
    ///
    /// Persists across scenes (DontDestroyOnLoad) because quiz scenes need
    /// to reach it too - it's created once in GameMenu and survives into
    /// whatever quiz scene loads and back again.
    /// </summary>
    public class ProgressManager : MonoBehaviour
    {
        public static ProgressManager Instance { get; private set; }

        public event Action OnProgressLoaded;

        public bool IsLoaded { get; private set; }

        [Tooltip("On by default so quiz scenes can still reach this to record results.")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private StudentProgress progress;
        private FirestoreProgressService progressService;
        private string studentNumber;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            progressService = new FirestoreProgressService();

            if (dontDestroyOnLoad)
            {
                if (transform.parent != null)
                {
                    Debug.LogWarning("[ProgressManager] Had a parent, which breaks DontDestroyOnLoad. Detaching to scene root.");
                    transform.SetParent(null);
                }

                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            studentNumber = UserSession.StudentNumber;

            if (string.IsNullOrEmpty(studentNumber))
            {
                Debug.LogWarning("[ProgressManager] No logged-in student number found - starting with default progress.");
                progress = StudentProgress.CreateDefault();
                IsLoaded = true;
                OnProgressLoaded?.Invoke();
                yield break;
            }

            Task<StudentProgress> loadTask = progressService.LoadProgressAsync(studentNumber);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            if (loadTask.Exception != null)
            {
                Debug.LogException(loadTask.Exception);
                progress = StudentProgress.CreateDefault();
            }
            else
            {
                progress = loadTask.Result;
            }

            IsLoaded = true;
            OnProgressLoaded?.Invoke();
        }

        public QuizProgress GetQuizProgress(int lessonNumber, int quizNumber)
        {
            return progress.GetOrCreate(lessonNumber).GetOrCreate(quizNumber);
        }

        public bool IsQuizUnlocked(int lessonNumber, int quizNumber)
        {
            return GetQuizProgress(lessonNumber, quizNumber).Unlocked;
        }

        /// <summary>
        /// Simple version for callers (like a quiz scene) that don't have
        /// LessonData/LessonDatabase on hand. Unlocks the next quiz number
        /// in the same lesson, and quiz 1 of the next lesson, unconditionally -
        /// harmless if either doesn't actually exist in your content yet.
        /// </summary>
        public void RecordQuizResult(int lessonNumber, int quizNumber, int scorePercent)
        {
            GetQuizProgress(lessonNumber, quizNumber).RecordAttempt(scorePercent);
            GetQuizProgress(lessonNumber, quizNumber + 1).Unlocked = true;
            GetQuizProgress(lessonNumber + 1, 1).Unlocked = true;

            StartCoroutine(SaveRoutine());
        }

        /// <summary>Original version - use when you do have LessonData/LessonDatabase on hand (e.g. from LessonPopupManager) and want lesson-boundary-aware unlocking.</summary>
        public void RecordQuizResult(int lessonNumber, int quizNumber, int scorePercent, LessonData lessonData, LessonDatabase lessonDatabase)
        {
            QuizProgress current = GetQuizProgress(lessonNumber, quizNumber);
            current.RecordAttempt(scorePercent);

            int nextQuizNumber = quizNumber + 1;
            bool wasLastQuizInLesson = lessonData == null || nextQuizNumber > lessonData.QuizCount;

            if (!wasLastQuizInLesson)
            {
                GetQuizProgress(lessonNumber, nextQuizNumber).Unlocked = true;
            }
            else if (lessonDatabase != null)
            {
                for (int i = 0; i < lessonDatabase.Count; i++)
                {
                    LessonData next = lessonDatabase.Get(i);
                    if (next != null && next.LessonNumber == lessonNumber + 1)
                    {
                        GetQuizProgress(lessonNumber + 1, 1).Unlocked = true;
                        break;
                    }
                }
            }

            StartCoroutine(SaveRoutine());
        }

        private IEnumerator SaveRoutine()
        {
            if (string.IsNullOrEmpty(studentNumber))
                yield break;

            Task saveTask = progressService.SaveProgressAsync(studentNumber, progress);
            yield return new WaitUntil(() => saveTask.IsCompleted);

            if (saveTask.Exception != null)
                Debug.LogException(saveTask.Exception);
        }
    }
}