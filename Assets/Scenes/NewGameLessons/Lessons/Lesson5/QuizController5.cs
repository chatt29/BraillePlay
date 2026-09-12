using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Quiz portion of the Braille Onset-Rime experience, including
/// scoring, mistake tracking, support hints, and final-score reporting.
///
/// This script is fully audio-driven — it has no dependency on any visual UI
/// (no TMP_Text, no Image state sprites, no live pattern display). Text fields
/// such as "promptMessage" / "successMessage" are kept purely as design-time
/// reference for lesson/quiz authors; they are never displayed on screen.
///
/// This script does not start itself — LessonManager calls the public
/// BeginQuizPhase() method once the lesson portion has finished. That method
/// call is the only point of communication between the two scripts.
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Serializable]
    public class OnsetRimeQuizQuestion
    {
        [Header("Identity (design-time reference only — not displayed)")]
        public string displayLabel;
        public string categoryLabel = "QUIZ MODE";

        [Header("Messages (design-time reference only — not displayed)")]
        [TextArea(2, 4)] public string promptMessage;
        [TextArea(2, 4)] public string successMessage;
        [TextArea(2, 4)] public string wrongMessage;
        [TextArea(2, 4)] public string onsetAudioPromptMessage;
        [TextArea(2, 4)] public string supportMessage;

        [Header("Audio Layout")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip successAudio;
        public AudioClip supportAudio;

        [Header("Auditory Onset Identification")]
        [Tooltip("Audio clip of the full target word (e.g., 'Cat')")]
        public AudioClip targetWordAudio;

        [Tooltip("Audio clip explicitly asking for the target onset sound (e.g., 'What is the starting sound you hear in cat? Write the letter that makes that sound.')")]
        public AudioClip onsetAudioPromptClip;

        [Header("Answer Configuration")]
        [Tooltip("The 6-dot Braille pattern string required for the identified onset sound (e.g., '100100' for /k/ sound mapped to 'c')")]
        public string correctOnsetBraillePattern;
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
    [Tooltip("Can be the same AudioSource assigned to LessonManager.")]
    public AudioSource voiceAudioSource;

    [Header("Quiz Intro Audio")]
    public AudioClip letsQuizAudio;

    [TextArea(2, 5)]
    public string letsQuizInstructionMessage = "Great job learning! Now let's test your skills with a quiz.";

    [Header("Generic Feedback Audio")]
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio; // Reserved — preserved from original script (see HandleNext note below)

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the onset and rime quiz.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the quiz. Do you want to repeat it? Press R to restart or Space to finish.";

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Content Setup
    // -------------------------------------------------------------------------

    [Header("Quiz Questions")]
    public List<OnsetRimeQuizQuestion> quizQuestions = new List<OnsetRimeQuizQuestion>();

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleOnsetRimeHighScore";

    [Header("Flow Delays")]
    public float delayAfterVoice = 0.35f;
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

    private int currentQuizIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool quizActive = false;
    private bool quizFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForQuestionInput = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events & Braille Mapping Subscriptions
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
    }

    /// <summary>
    /// Entry point called by LessonManager once the lesson portion has finished.
    /// This is the sole hand-off between the two scripts.
    /// </summary>
    public void BeginQuizPhase()
    {
        if (logDebug)
            Debug.Log("QuizManager: beginning quiz phase.");

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

    // -------------------------------------------------------------------------
    // Answer Feedback (audio-only — replaces the old answerStateImage)
    // -------------------------------------------------------------------------

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
        waitingForRepeatChoice = false;
        waitingForQuestionInput = false;

        RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
    }

    private IEnumerator PlayQuizFromBeginning(OnsetRimeQuizQuestion question)
    {
        yield return PlayClipsSequentially(noAudioFallbackDelay, question.introAudio, question.instructionAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return AskQuizQuestionInput(question);
    }

    private IEnumerator AskQuizQuestionInput(OnsetRimeQuizQuestion question)
    {
        waitingForQuestionInput = true;

        // Play the full target word (e.g. "Cat") — no fallback wait if missing,
        // matching original behavior.
        yield return PlayClipAndWait(question.targetWordAudio, 0f);
        yield return new WaitForSeconds(delayAfterVoice);

        // Deliver the audio focus cue asking for the onset sound identification.
        yield return PlayClipAndWait(question.onsetAudioPromptClip, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizActive || quizFinished || waitingForRepeatChoice || !waitingForQuestionInput)
            return;

        OnsetRimeQuizQuestion question = quizQuestions[currentQuizIndex];
        waitingForQuestionInput = false;

        // Directly check if the chord matches the correct configuration for the target onset sound.
        if (submittedPattern.Trim() == question.correctOnsetBraillePattern.Trim())
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

    private IEnumerator HandleCorrectAnswer(OnsetRimeQuizQuestion question)
    {
        SaveHighScoreIfNeeded();
        AudioClip clip = question.successAudio != null ? question.successAudio : genericCorrectAudio;

        yield return PlayClipAndWait(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartQuizQuestion(currentQuizIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(OnsetRimeQuizQuestion question)
    {
        yield return PlayClipAndWait(genericTryAgainAudio, noAudioFallbackDelay);
        yield return AskQuizQuestionInput(question);
    }

    private IEnumerator HandleSupportThenRetry(OnsetRimeQuizQuestion question)
    {
        yield return PlayClipAndWait(question.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskQuizQuestionInput(question);
    }

    // -------------------------------------------------------------------------
    // Navigation Action Triggers
    // -------------------------------------------------------------------------

    private void HandleRepeat()
    {
        // waitingForRepeatChoice is preserved from the original script's
        // structure (via repeatQuestionMessage) but, exactly as in the
        // original, nothing currently sets it to true — this branch is kept
        // for structural/behavioral parity and future use.
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartQuizQuestion(0);
            return;
        }

        // "R" pressed mid-question — replay the current question from the start.
        if (quizActive && currentQuizIndex >= 0 && currentQuizIndex < quizQuestions.Count)
        {
            RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
        }
    }

    private void HandleNext()
    {
        // No behavior here in the original combined script either — "Next"
        // during the quiz phase was only ever handled for the lesson's
        // end-of-lesson prompt, which now lives entirely in LessonManager.
    }

    // -------------------------------------------------------------------------
    // Termination Flow
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
            Debug.LogWarning("[QuizManager] No QuizResultReporter assigned — score won't be saved or returned to GameMenu.");
    }

    // -------------------------------------------------------------------------
    // Final Score Audio Sequence
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        yield return PlayClipAndWait(yourScoreIsAudio, 0f);
        yield return PlayClipAndWait(finalScoreClip, 0f);
        yield return PlayClipAndWait(whileYourHighestScoreIsAudio, 0f);
        yield return PlayClipAndWait(highScoreClip, 0f);
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