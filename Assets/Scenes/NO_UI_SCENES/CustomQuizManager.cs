using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays one Firestore lesson (the id in CustomLessonSession.SelectedLessonId).
///   Up / Down   -> previous / next choice (plays that choice's recording)
///   Enter       -> answer with the current choice
///   R (Repeat)  -> replay the question and all choices
///   Backspace   -> back to the lesson menu
/// </summary>
public class CustomQuizManager : MonoBehaviour
{
    private class QuizChoice { public string text; public AudioClip clip; }
    private class QuizQuestion
    {
        public string text;
        public AudioClip clip;
        public int correctIndex;
        public List<QuizChoice> choices = new List<QuizChoice>();

        private string lessonId, lessonTitle, teacherId;
    }

    [Header("Recorded prompts")]
    [Tooltip("\"Loading lesson. Please wait.\"")] public AudioClip loadingClip;
    [Tooltip("\"Could not load this lesson. Press Backspace to go back.\"")] public AudioClip loadErrorClip;
    [Tooltip("\"This lesson has no questions. Press Backspace to go back.\"")] public AudioClip noQuestionsClip;
    [Tooltip("\"Correct.\"")] public AudioClip correctClip;
    [Tooltip("\"Not correct.\"")] public AudioClip wrongClip;
    [Tooltip("\"Lesson finished.\"")] public AudioClip finishedClip;
    [Tooltip("\"Going back.\"")] public AudioClip goingBackClip;

    [Header("Visuals (optional, for sighted helpers)")]
    public TMP_Text questionText;
    public TMP_Text choiceText;

    [Header("Scene flow")]
    [Tooltip("Exact name of your custom lesson menu scene.")]
    public string previousSceneName = "CustomLessonMenu";

    private readonly List<QuizQuestion> questions = new List<QuizQuestion>();
    private AudioClip instructionClip;
    private AudioSource source;
    private Coroutine seqRoutine;
    private int qIndex, cIndex, score;
    private bool loading = true, busy, answering;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        BrailleMapping.OnUp += HandleUp;
        BrailleMapping.OnDown += HandleDown;
        BrailleMapping.OnSubmit += HandleEnter;
        BrailleMapping.OnDeleteOrNo += HandleBack;
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnUp -= HandleUp;
        BrailleMapping.OnDown -= HandleDown;
        BrailleMapping.OnSubmit -= HandleEnter;
        BrailleMapping.OnDeleteOrNo -= HandleBack;
        BrailleMapping.OnRepeat -= HandleRepeat;
    }

    private void Start()
    {
        if (BrailleMapping.Instance == null)
            Debug.LogError("[CustomQuiz] No BrailleMapping in this scene. Input will not work.");
        StartCoroutine(Run());
    }

    // ---- loading ----

    private static object Get(Dictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out object v) ? v : null;
    }

    private static IEnumerator LoadClip(object b64, Action<AudioClip> done)
    {
        if (b64 is string s && s.Length > 0)
            yield return CustomLessonLoader.Base64ToClip(s, done);
        else
            done(null);
    }

    private IEnumerator Run()
    {
        string id = CustomLessonSession.SelectedLessonId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[CustomQuiz] No lesson id. Open this scene from the lesson menu.");
            FailLoad(loadErrorClip);
            yield break;
        }

        Play(loadingClip);

        Task<DocumentSnapshot> task = FirebaseFirestore.DefaultInstance
            .Collection("CustomLessons").Document(id).GetSnapshotAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.IsCanceled || !task.Result.Exists)
        {
            Debug.LogError("[CustomQuiz] Couldn't load lesson " + id);
            FailLoad(loadErrorClip);
            yield break;
        }

        Dictionary<string, object> data = task.Result.ToDictionary();
                lessonId = id;
        lessonTitle = Get(data, "title") as string ?? "";
        teacherId = Get(data, "teacher_id") as string ?? "";
        yield return LoadClip(Get(data, "instruction_audio"), c => instructionClip = c);

        if (Get(data, "questions") is List<object> qList)
        {
            foreach (object qItem in qList)
            {
                var qd = qItem as Dictionary<string, object>;
                if (qd == null) continue;

                var q = new QuizQuestion();
                q.text = Get(qd, "text") as string ?? "";
                object correct = Get(qd, "correct_index");
                q.correctIndex = correct != null ? Convert.ToInt32(correct) : 0;
                yield return LoadClip(Get(qd, "audio"), c => q.clip = c);

                if (Get(qd, "choices") is List<object> cList)
                {
                    foreach (object cItem in cList)
                    {
                        var cd = cItem as Dictionary<string, object>;
                        if (cd == null) continue;

                        var choice = new QuizChoice();
                        choice.text = Get(cd, "text") as string ?? "";
                        yield return LoadClip(Get(cd, "audio"), c => choice.clip = c);
                        q.choices.Add(choice);
                    }
                }
                questions.Add(q);
            }
        }

        loading = false;
        if (questions.Count == 0) { Play(noQuestionsClip); yield break; }

        qIndex = 0;
        AskQuestion(true);
    }

    private void FailLoad(AudioClip clip)
    {
        loading = false;
        Play(clip);
    }

    // ---- quiz flow ----

    private void AskQuestion(bool withInstructions)
    {
        QuizQuestion q = questions[qIndex];
        cIndex = 0;
        answering = false;

        if (questionText != null) questionText.text = q.text;
        if (choiceText != null) choiceText.text = q.choices.Count > 0 ? q.choices[0].text : "";

        var clips = new List<AudioClip>();
        if (withInstructions) clips.Add(instructionClip);
        clips.Add(q.clip);
        foreach (QuizChoice c in q.choices) clips.Add(c.clip);
        Play(clips.ToArray());
    }

    private IEnumerator AnswerRoutine()
    {
        answering = true;
        bool correct = cIndex == questions[qIndex].correctIndex;
        if (correct) score++;

        StopSequence();
        yield return SequenceRoutine(correct ? correctClip : wrongClip);

        qIndex++;
                if (qIndex >= questions.Count)
        {
            busy = true;
            Debug.Log("[CustomQuiz] Finished. Score " + score + "/" + questions.Count);

            // YOUR GAME'S LOGGED-IN STUDENT NUMBER GOES HERE (see note below)
            string studentId = PlayerPrefs.GetString("StudentID", "");

            Task saveTask = CustomProgressSaver.Save(
                studentId, lessonId, lessonTitle, teacherId, score, questions.Count);

            yield return SequenceRoutine(finishedClip);
            yield return new WaitUntil(() => saveTask.IsCompleted);
            if (saveTask.IsFaulted)
                Debug.LogError("[CustomProgress] Save failed: " + saveTask.Exception);

            SceneManager.LoadScene(previousSceneName);
            yield break;
        }

        AskQuestion(false);
    }

    private IEnumerator GoBackRoutine()
    {
        busy = true;
        StopSequence();
        yield return SequenceRoutine(goingBackClip);
        SceneManager.LoadScene(previousSceneName);
    }

    // ---- input ----

    private void HandleUp() => MoveChoice(-1);
    private void HandleDown() => MoveChoice(1);

    private void MoveChoice(int direction)
    {
        if (busy || loading || answering || questions.Count == 0) return;

        QuizQuestion q = questions[qIndex];
        if (q.choices.Count == 0) return;

        cIndex = Mathf.Clamp(cIndex + direction, 0, q.choices.Count - 1);
        if (choiceText != null) choiceText.text = q.choices[cIndex].text;
        Play(q.choices[cIndex].clip);
    }

    private void HandleEnter()
    {
        if (busy || loading || answering || questions.Count == 0) return;
        if (questions[qIndex].choices.Count == 0) return;
        StartCoroutine(AnswerRoutine());
    }

    private void HandleRepeat()
    {
        if (busy || loading || answering || questions.Count == 0) return;
        AskQuestion(false);
    }

    private void HandleBack()
    {
        if (busy) return;
        StartCoroutine(GoBackRoutine());
    }

    // ---- recorded audio ----

    private void Play(params AudioClip[] clips)
    {
        StopSequence();
        source.Stop();
        seqRoutine = StartCoroutine(SequenceRoutine(clips));
    }

    private IEnumerator SequenceRoutine(params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;
            source.Stop();
            source.clip = clip;
            source.Play();
            yield return null;
            yield return new WaitWhile(() => source.isPlaying);
        }
    }

    private void StopSequence()
    {
        if (seqRoutine != null) { StopCoroutine(seqRoutine); seqRoutine = null; }
    }
}