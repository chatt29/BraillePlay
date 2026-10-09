using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Quiz portion of the Rhyme experience, including scoring.
/// Fully audio-driven (no visual UI).
///
/// Question format: the learner hears a target word, then three spoken choices
/// (A, B, C), and answers by entering the Braille letter of the rhyming word.
///   A = 100000, B = 110000, C = 100100
///
/// Started by RhymeLessonManager calling BeginQuizPhase().
/// </summary>
public class RhymeQuizManager : MonoBehaviour
{
    public enum Choice { A, B, C }

    [Serializable]
    public class RhymeQuizQuestion
    {
        [Header("Reference (design-time only - not displayed)")]
        public string targetWord;
        public string choiceAWord;
        public string choiceBWord;
        public string choiceCWord;

        [Header("Audio Layout")]
        public AudioClip introAudio;        // e.g. "Listen carefully to the word."
        public AudioClip instructionAudio;  // optional extra instruction
        public AudioClip successAudio;      // optional, falls back to generic
        public AudioClip supportAudio;      // played after repeated mistakes

        [Header("Question Audio")]
        [Tooltip("The target word, e.g. 'cat'")]
        public AudioClip targetWordAudio;

        [Tooltip("e.g. 'Which word rhymes with cat?'")]
        public AudioClip questionPromptAudio;

        public AudioClip choiceAAudio; // e.g. "A, dog"
        public AudioClip choiceBAudio; // e.g. "B, hat"
        public AudioClip choiceCAudio; // e.g. "C, sun"

        [Tooltip("e.g. 'Write the letter of your answer.'")]
        public AudioClip answerInstructionAudio;

        [Header("Answer")]
        public Choice correctChoice;
    }

    // -------------------------------------------------------------------------
    // Result Reporting
    // -------------------------------------------------------------------------

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio Source")]
    [Tooltip("Can be the same AudioSource assigned to RhymeLessonManager.")]
    public AudioSource voiceAudioSource;

    [Header("Quiz Intro Audio")]
    public AudioClip letsQuizAudio;

    [Header("Generic Feedback Audio")]
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Content
    // -------------------------------------------------------------------------

    [Header("Quiz Questions")]
    public List<RhymeQuizQuestion> quizQuestions = new List<RhymeQuizQuestion>();

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleRhymeHighScore";

    [Header("Flow Delays")]
    public float delayAfterVoice = 0.35f;
    public float delayBetweenChoices = 0.25f;
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private const string PatternA = "100000";
    private const string PatternB = "110000";
    private const string PatternC = "100100";

    private int currentQuizIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool quizActive = false;
    private bool quizFinished = false;
    private bool waitingForQuestionInput = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
    }

    /// <summary>Entry point called by RhymeLessonManager when the lesson ends.</summary>
    public void BeginQuizPhase()
    {
        if (logDebug)
            Debug.Log("RhymeQuizManager: beginning quiz phase.");

        RunFlow(TransitionToQuizPhase());
    }

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------

    private void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    private void AddMistake()
    {
        totalWrongCount++;

        int deductions = totalWrongCount / 3;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        PlayAnswerFeedback(false);
    }

    private void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }

    private void PlayAnswerFeedback(bool isCorrect)
    {
        if (BrailleMapping.Instance == null) return;

        if (isCorrect) BrailleMapping.Instance.PlayCorrectSfx();
        else BrailleMapping.Instance.PlayWrongSfx();
    }

    // -------------------------------------------------------------------------
    // Quiz Flow
    // -------------------------------------------------------------------------

    private IEnumerator TransitionToQuizPhase()
    {
        yield return PlayClipAndWait(letsQuizAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        ResetQuizScore();
        StartQuizQuestion(0);
    }

    private void StartQuizQuestion(int index)
    {
        if (index < 0 || index >= quizQuestions.Count)
        {
            RunFlow(FinalizeQuizCompletion());
            return;
        }

        currentQuizIndex = index;
        currentMistakeCount = 0;
        quizActive = true;
        quizFinished = false;
        waitingForQuestionInput = false;

        RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
    }

    private IEnumerator PlayQuizFromBeginning(RhymeQuizQuestion question)
    {
        yield return PlayClipsSequentially(noAudioFallbackDelay, question.introAudio, question.instructionAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskQuizQuestionInput(question);
    }

    private IEnumerator AskQuizQuestionInput(RhymeQuizQuestion question)
    {
        waitingForQuestionInput = true;

        // Target word, then the question, then the three choices.
        yield return PlayClipAndWait(question.targetWordAudio, 0f);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayClipAndWait(question.questionPromptAudio, 0f);
        yield return new WaitForSeconds(delayBetweenChoices);

        yield return PlayClipAndWait(question.choiceAAudio, 0f);
        yield return new WaitForSeconds(delayBetweenChoices);

        yield return PlayClipAndWait(question.choiceBAudio, 0f);
        yield return new WaitForSeconds(delayBetweenChoices);

        yield return PlayClipAndWait(question.choiceCAudio, 0f);
        yield return new WaitForSeconds(delayBetweenChoices);

        yield return PlayClipAndWait(question.answerInstructionAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private string GetCorrectPattern(Choice choice)
    {
        switch (choice)
        {
            case Choice.A: return PatternA;
            case Choice.B: return PatternB;
            default:       return PatternC;
        }
    }

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizActive || quizFinished || !waitingForQuestionInput)
            return;

        RhymeQuizQuestion question = quizQuestions[currentQuizIndex];
        waitingForQuestionInput = false;

        if (submittedPattern.Trim() == GetCorrectPattern(question.correctChoice))
        {
            currentMistakeCount = 0;
            quizActive = false;
            PlayAnswerFeedback(true);
            RunFlow(HandleCorrectAnswer(question));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(question));
            else
                RunFlow(HandleWrongAnswer(question));
        }
    }

    private IEnumerator HandleCorrectAnswer(RhymeQuizQuestion question)
    {
        SaveHighScoreIfNeeded();
        AudioClip clip = question.successAudio != null ? question.successAudio : genericCorrectAudio;

        yield return PlayClipAndWait(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartQuizQuestion(currentQuizIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(RhymeQuizQuestion question)
    {
        yield return PlayClipAndWait(genericTryAgainAudio, noAudioFallbackDelay);
        yield return AskQuizQuestionInput(question);
    }

    private IEnumerator HandleSupportThenRetry(RhymeQuizQuestion question)
    {
        yield return PlayClipAndWait(question.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuizQuestionInput(question);
    }

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        // "R" mid-question replays the current question from the start.
        if (quizActive && currentQuizIndex >= 0 && currentQuizIndex < quizQuestions.Count)
            RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
    }

    // -------------------------------------------------------------------------
    // Termination
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuizCompletion()
    {
        quizActive = false;
        quizFinished = true;
        SaveHighScoreIfNeeded();

        yield return PlayClipAndWait(genericCompletedAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[RhymeQuizManager] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        yield return PlayClipAndWait(yourScoreIsAudio, 0f);
        yield return PlayClipAndWait(GetNumberAudio(totalScore), 0f);
        yield return PlayClipAndWait(whileYourHighestScoreIsAudio, 0f);
        yield return PlayClipAndWait(GetNumberAudio(highScore), 0f);
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0 || number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Audio Helpers
    // -------------------------------------------------------------------------

    private IEnumerator PlayClipAndWait(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else if (fallbackWait > 0f)
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    private IEnumerator PlayClipsSequentially(float fallbackWait, params AudioClip[] clips)
    {
        bool playedAny = false;

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;
                playedAny = true;

                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        if (!playedAny)
            yield return new WaitForSeconds(fallbackWait);
    }
}
