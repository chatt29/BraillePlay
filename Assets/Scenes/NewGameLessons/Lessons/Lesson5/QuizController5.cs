using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the Quiz portion of the Braille Onset-Rime experience, including
/// scoring, mistake tracking, support hints, and final-score reporting.
///
/// This script is fully audio-driven: it has no dependency on any visual UI.
/// Text fields such as "promptMessage" / "successMessage" are kept purely as
/// design-time reference for authors; they are never displayed on screen.
///
/// This script does not start itself. LessonManager calls the public
/// BeginQuizPhase() method once the lesson portion has finished.
///
/// End of quiz (after the score and high score are announced and the
/// repeat question plays):
///   Repeat -> reset the score and play the quiz again
///   Next   -> report the score and return to the main menu
///   Back   -> same as Next, with an optional confirmation cue first
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

    [Tooltip("Played AFTER the score and high score: 'Press Repeat to play again or Next to finish.'")]
    public AudioClip repeatQuestionAudio;

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the onset and rime quiz.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the quiz. Do you want to repeat it? Press R to restart or Next to finish.";

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

    [Header("Back To Main Menu")]
    [Tooltip("Only used if no QuizResultReporter is assigned. Must be added in File > Build Settings.")]
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Optional cue, e.g. 'Going back to the main menu.' Played only when Back is pressed.")]
    public AudioClip backToMenuAudio;

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
    private bool waitingForRepeatChoice = false;   // end-of-quiz: Repeat, Next or Back
    private bool waitingForQuestionInput = false;
    private bool returningToMenu = false;          // guards against Next/Back/Repeat firing twice

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events & Braille Mapping Subscriptions
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnYesOrNext += HandleNext;
        BrailleMapping.OnBack += HandleBack;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleChordSubmitted;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnYesOrNext -= HandleNext;
        BrailleMapping.OnBack -= HandleBack;
    }

    /// <summary>
    /// Entry point called by LessonManager once the lesson portion has finished.
    /// This is the sole hand-off between the two scripts.
    /// </summary>
    public void BeginQuizPhase()
    {
        if (logDebug)
            Debug.Log("QuizManager: beginning quiz phase.");

        returningToMenu = false;
        waitingForRepeatChoice = false;

        RunFlow(TransitionToQuizPhase());
    }

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    private void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
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
    // Answer Feedback (audio-only)
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
        if (returningToMenu)
            return;

        // End of quiz: play the quiz again from the first question.
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StopVoice();
            ResetQuizScore();
            StartQuizQuestion(0);
            return;
        }

        // "Repeat" pressed mid-question: replay the current question from the start.
        if (quizActive && currentQuizIndex >= 0 && currentQuizIndex < quizQuestions.Count)
        {
            waitingForQuestionInput = false;
            RunFlow(PlayQuizFromBeginning(quizQuestions[currentQuizIndex]));
        }
    }

    private void HandleNext()
    {
        // The only "Next" the quiz cares about is the end-of-quiz choice: finish.
        if (!waitingForRepeatChoice || returningToMenu)
            return;

        returningToMenu = true;
        waitingForRepeatChoice = false;
        StopVoice();

        if (logDebug)
            Debug.Log("QuizManager: learner chose to finish the quiz.");

        ReturnToMenu();
    }

    private void HandleBack()
    {
        // Back only works at the very end of the quiz (after the score is announced).
        if (!waitingForRepeatChoice || returningToMenu)
            return;

        returningToMenu = true;
        waitingForRepeatChoice = false;

        RunFlow(BackToMenuFlow());
    }

    // -------------------------------------------------------------------------
    // Termination Flow
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuizCompletion()
    {
        quizActive = false;
        quizFinished = true;
        waitingForQuestionInput = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // 1. "Great job! You finished the quiz."
        yield return PlayClipAndWait(genericCompletedAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // 3. "Do you want to repeat?" The learner can answer (Repeat / Next / Back)
        //    as soon as this audio starts.
        waitingForRepeatChoice = true;
        yield return PlayClipAndWait(repeatQuestionAudio, noAudioFallbackDelay);

        // Stays here until the learner presses Repeat, Next or Back.
    }

    private IEnumerator BackToMenuFlow()
    {
        // Cut off the repeat-question audio if it is still playing.
        StopVoice();

        yield return PlayClipAndWait(backToMenuAudio, 0f);

        ReturnToMenu();
    }

    /// <summary>Saves the high score, then reports the score and returns to the menu.</summary>
    private void ReturnToMenu()
    {
        SaveHighScoreIfNeeded();

        if (resultReporter != null)
        {
            resultReporter.ReportScoreAndReturn(totalScore);   // saves score + returns to GameMenu
        }
        else
        {
            Debug.LogWarning("[QuizManager] No QuizResultReporter assigned — loading menu directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
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