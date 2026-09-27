using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the quiz portion of the scene: asking each sound-identification
/// question, judging Braille chord answers, tracking score, and reporting
/// the result back to the game menu. This script is audio-only - there is
/// no visual UI, since the app targets blind/visually impaired learners.
///
/// LessonController1 calls StartQuiz(0) once the lesson pages are done;
/// from that point this script owns the rest of the flow.
///
/// Flow (unchanged from the original combined script, plus the new
/// repeat-quiz prompt at the end):
///   1. For each lesson: play the prompt, then (if configured) the sound
///      identification question.
///   2. Judge the Braille chord answer (Dot 1 = true/yes, Dot 2 = false/no).
///   3. Correct -> success message, advance to the next lesson.
///      Wrong -> wrong message, re-ask the same question.
///      3 wrong in a row -> support message, re-ask the same question.
///   4. After the last lesson: announce score + high score, THEN play the
///      repeat-question audio and wait for the player to choose whether to
///      play the quiz again (R) or finish (Y).
/// </summary>
public class QuizController1 : MonoBehaviour
{
    [Serializable]
    public class BrailleLesson
    {
        [Header("Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip successAudio;
        public AudioClip repeatAudio;

        [Header("Sound Identification")]
        public bool useSoundQuestion = false;
        public AudioClip soundEffectAudio;
        public AudioClip soundQuestionAudio;
        public bool correctAnswerIsTrue = true;

        [Header("Support After Mistakes")]
        public AudioClip supportAudio;
    }

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForSoundQuestion = false;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
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

    // -------------------------------------------------------------------------
    // Entry Point (called by LessonController1)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by LessonController1 once the lesson pages are finished.
    /// Resets the score and begins the quiz at the given lesson index.
    /// </summary>
    public void StartQuiz(int startIndex)
    {
        if (logDebug)
            Debug.Log("QuizController1 starting quiz.");

        ResetQuizScore();
        StartLesson(startIndex);
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
    // Coroutine Helper
    // -------------------------------------------------------------------------

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Lesson (Quiz Item) Sequence
    //
    // Exact order, unchanged from the original script:
    //   1. Prompt Message audio (intro + instruction)
    //   2. Sound Identification Question
    //   3. Success / Wrong handling
    //   4. Support Message audio -> only after 3 consecutive mistakes
    // -------------------------------------------------------------------------

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(FinalizeQuizCompletion());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForSoundQuestion = false;

        if (logDebug)
            Debug.Log($"Starting quiz item {currentLessonIndex}.");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        if (lesson.useSoundQuestion)
            yield return AskSoundQuestion(lesson);
    }

    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        yield return PlayVoiceClipSequence(noAudioFallbackDelay, lesson.introAudio, lesson.instructionAudio);
    }

    /// <summary>
    /// Plays the sound effect (if any) then asks the identification question.
    /// Used both for the first ask and every re-ask after a wrong answer or
    /// support message.
    /// </summary>
    private IEnumerator AskSoundQuestion(BrailleLesson lesson)
    {
        waitingForSoundQuestion = true;

        if (lesson.soundEffectAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = lesson.soundEffectAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(lesson.soundEffectAudio.length);
        }

        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoiceClip(lesson.soundQuestionAudio, noAudioFallbackDelay);
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForSoundQuestion)
        {
            HandleSoundQuestion(submittedPattern);
        }
    }

    private void HandleSoundQuestion(string pattern)
    {
        if (!waitingForSoundQuestion) return;

        // Dot 1 = Yes / True   |   Dot 2 = No / False
        bool userAnswer;

        if (pattern == "100000") userAnswer = true;
        else if (pattern == "010000") userAnswer = false;
        else return;

        BrailleLesson lesson = lessons[currentLessonIndex];
        waitingForSoundQuestion = false;

        if (userAnswer == lesson.correctAnswerIsTrue)
        {
            currentMistakeCount = 0;
            lessonActive = false;

            RunFlow(HandleCorrectAnswer(lesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(lesson));
            else
                RunFlow(HandleWrongAnswer(lesson));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson)
    {
        SaveHighScoreIfNeeded();

        AudioClip clip = lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio;

        yield return PlayVoiceClip(clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLesson(currentLessonIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(BrailleLesson lesson)
    {
        yield return PlayVoiceClip(genericTryAgainAudio, noAudioFallbackDelay);
        yield return AskSoundQuestion(lesson);
    }

    /// <summary>
    /// After 3 consecutive mistakes, play the support audio to help the
    /// player, reset the mistake streak, then re-ask the question.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(BrailleLesson lesson)
    {
        yield return PlayVoiceClip(lesson.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return AskSoundQuestion(lesson);
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Repeats ONLY the current quiz item from the very beginning (prompt +
    /// sound identification question), unless the player is at the final
    /// "play again?" prompt, in which case R restarts the whole quiz.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StartQuiz(0);
            return;
        }

        if (!lessonActive)
            return;

        if (sceneFinished || currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        lessonActive = true;
        waitingForSoundQuestion = false;
        currentMistakeCount = 0;

        RunFlow(PlayLessonFromBeginning(lesson));
    }

    private void HandleNext()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(ReportAndReturnToMenu());
        }
    }

    // -------------------------------------------------------------------------
    // Quiz Completion
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuizCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // Announce the score and high score.
        yield return PlayVoiceClip(genericCompletedAudio, noAudioFallbackDelay);
        yield return PlayFinalScoreAudio();

        // Then ask whether the player wants to play the quiz again.
        waitingForRepeatChoice = true;
        yield return PlayVoiceClip(repeatQuestionAudio, noAudioFallbackDelay);

        while (waitingForRepeatChoice)
            yield return null;
    }

    private IEnumerator ReportAndReturnToMenu()
    {
        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController1] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");

        yield break;
    }

    // -------------------------------------------------------------------------
    // Final Score Audio
    // -------------------------------------------------------------------------

    private IEnumerator PlayFinalScoreAudio()
    {
        if (voiceAudioSource == null) yield break;

        AudioClip finalScoreClip = GetNumberAudio(totalScore);
        AudioClip highScoreClip = GetNumberAudio(highScore);

        if (yourScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = yourScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(yourScoreIsAudio.length);
        }

        if (finalScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = finalScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(finalScoreClip.length);
        }

        if (whileYourHighestScoreIsAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = whileYourHighestScoreIsAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(whileYourHighestScoreIsAudio.length);
        }

        if (highScoreClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = highScoreClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(highScoreClip.length);
        }
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Audio Helpers
    // -------------------------------------------------------------------------

    private IEnumerator PlayVoiceClip(AudioClip clip, float fallbackWait)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(fallbackWait);
        }
    }

    private IEnumerator PlayVoiceClipSequence(float fallbackWait, params AudioClip[] clips)
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