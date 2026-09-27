using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quiz portion of the game (audio + Braille input only).
/// Stays dormant until LessonController11 calls BeginQuiz().
/// Handles word prompts, Braille typing, scoring, high score, and the
/// "play again?" decision at the end.
/// </summary>
public class QuizController11 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Data - one entry per WORD
    // -------------------------------------------------------------------------
    [Serializable]
    public class BrailleWordLesson
    {
        [Header("Editor Label (never shown to the player)")]
        public string displayLabel;

        [Header("Target Word")]
        [Tooltip("Capitalize only the first letter (e.g. 'Ham', 'Cat', 'Man').")]
        public string word = "Ham";

        [Header("Prompt Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        public AudioClip successAudio;
        public AudioClip wrongAudio;

        [Header("Support After Mistakes")]
        public AudioClip supportAudio;

        public List<string> GetTargetPatterns()
        {
            var patterns = new List<string>();
            if (string.IsNullOrEmpty(word)) return patterns;

            bool first = true;

            foreach (char c in word)
            {
                if (!char.IsLetter(c)) continue;

                if (first)
                {
                    patterns.Add(BrailleCodes11.CapitalIndicatorPattern);
                    first = false;
                }

                char upper = char.ToUpperInvariant(c);
                if (BrailleCodes11.AlphabetPatterns.TryGetValue(upper, out string pattern))
                    patterns.Add(pattern);
            }

            return patterns;
        }
    }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------
    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;    // "Do you want to play again? Press R to repeat or Y to finish."

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    [Header("Quiz Words")]
    public List<BrailleWordLesson> quizWords = new List<BrailleWordLesson>();

    [Header("Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "RimesAmAnAtHighScore";

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    public float delayAfterCorrect = 0.75f;
    [Tooltip("Short pause used when an audio clip is missing, so the flow never stalls.")]
    public float missingAudioDelay = 0.5f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------
    private int currentWordIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool quizRunning = false;            // false until BeginQuiz()
    private bool wordActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForWordAnswer = false;
    private bool waitingForCapitalIndicator = true;

    private readonly List<string> currentTypedPatterns = new List<string>();

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity lifecycle
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
    // Entry point (called by LessonController11)
    // -------------------------------------------------------------------------
    public void BeginQuiz()
    {
        if (logDebug) Debug.Log("QuizController11 started.");

        quizRunning = true;
        wordActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForWordAnswer = false;

        ResetQuizScore();
        RunFlow(StartQuizIntro());
    }

    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    private IEnumerator StartQuizIntro()
    {
        yield return PlayVoice(welcomeAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoice(letsLearnAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        StartWord(0);
    }

    // -------------------------------------------------------------------------
    // Scoring
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

        if (logDebug)
            Debug.Log($"Mistakes: {totalWrongCount} | Score: {totalScore}");
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
    // Word flow
    // -------------------------------------------------------------------------
    private void StartWord(int index)
    {
        if (index < 0 || index >= quizWords.Count)
        {
            RunFlow(CompleteQuiz());
            return;
        }

        currentWordIndex = index;
        currentMistakeCount = 0;
        wordActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForWordAnswer = false;
        currentTypedPatterns.Clear();

        if (logDebug)
            Debug.Log($"Starting word {currentWordIndex}: {quizWords[currentWordIndex].word}");

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private IEnumerator PlayWordFromBeginning(BrailleWordLesson lesson)
    {
        currentTypedPatterns.Clear();

        yield return PlayPrompt(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    private IEnumerator PlayPrompt(BrailleWordLesson lesson)
    {
        yield return PlayVoiceSequence(lesson.introAudio, lesson.instructionAudio);
    }

    private void AskForWordInput()
    {
        currentTypedPatterns.Clear();
        waitingForCapitalIndicator = true;
        waitingForWordAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Braille input
    // -------------------------------------------------------------------------
    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizRunning || !wordActive || sceneFinished || waitingForRepeatChoice)
            return;

        if (waitingForWordAnswer)
            HandleWordLetterInput(submittedPattern);
    }

    private void HandleWordLetterInput(string pattern)
    {
        if (!waitingForWordAnswer) return;

        BrailleWordLesson lesson = quizWords[currentWordIndex];
        List<string> targetPatterns = lesson.GetTargetPatterns();

        if (waitingForCapitalIndicator)
        {
            if (pattern != BrailleCodes11.CapitalIndicatorPattern)
            {
                waitingForWordAnswer = false;
                currentMistakeCount++;
                AddMistake();
                RunFlow(HandleWrongAnswer(lesson));
                return;
            }

            waitingForCapitalIndicator = false;
            currentTypedPatterns.Add(pattern);
            return;
        }

        currentTypedPatterns.Add(pattern);

        if (currentTypedPatterns.Count < targetPatterns.Count)
            return;

        waitingForWordAnswer = false;

        bool isCorrect = currentTypedPatterns.Count == targetPatterns.Count;
        if (isCorrect)
        {
            for (int i = 0; i < targetPatterns.Count; i++)
            {
                if (currentTypedPatterns[i] != targetPatterns[i])
                {
                    isCorrect = false;
                    break;
                }
            }
        }

        if (isCorrect)
        {
            currentMistakeCount = 0;
            wordActive = false;
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

    private IEnumerator HandleCorrectAnswer(BrailleWordLesson lesson)
    {
        SaveHighScoreIfNeeded();

        yield return PlayVoice(lesson.successAudio != null ? lesson.successAudio : genericCorrectAudio);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartWord(currentWordIndex + 1);
    }

    private IEnumerator HandleWrongAnswer(BrailleWordLesson lesson)
    {
        yield return PlayVoice(lesson.wrongAudio != null ? lesson.wrongAudio : genericTryAgainAudio);

        waitingForCapitalIndicator = true;

        yield return PlayPrompt(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    private IEnumerator HandleSupportThenRetry(BrailleWordLesson lesson)
    {
        yield return PlayVoice(lesson.supportAudio);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        yield return PlayPrompt(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        AskForWordInput();
    }

    // -------------------------------------------------------------------------
    // Repeat / Next
    // -------------------------------------------------------------------------
    private void HandleRepeat()
    {
        if (!quizRunning) return;

        // End of quiz: play again
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartWord(0);
            return;
        }

        // Mid-quiz: repeat the current word prompt
        if (!wordActive) return;
        if (sceneFinished || currentWordIndex < 0 || currentWordIndex >= quizWords.Count) return;

        waitingForWordAnswer = false;
        currentMistakeCount = 0;
        currentTypedPatterns.Clear();

        RunFlow(PlayWordFromBeginning(quizWords[currentWordIndex]));
    }

    private void HandleNext()
    {
        if (!quizRunning) return;

        // End of quiz: finish
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeQuiz());
        }
    }

    // -------------------------------------------------------------------------
    // Quiz completion
    // -------------------------------------------------------------------------
    private IEnumerator CompleteQuiz()
    {
        wordActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        // 1. "I'm so proud of you!..."
        yield return PlayVoice(genericCompletedAudio);
        yield return new WaitForSeconds(delayAfterVoice);

        // 2. "Your score is __, while your highest score is __."
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // 3. "Do you want to play again? Press R to repeat or Y to finish."
        //    The learner can answer as soon as this audio starts.
        waitingForRepeatChoice = true;
        yield return PlayVoice(repeatQuestionAudio);
    }

    private IEnumerator FinalizeQuiz()
    {
        sceneFinished = true;
        wordActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (resultReporter != null)
            resultReporter.ReportScoreAndReturn(totalScore);
        else
            Debug.LogWarning("[QuizController11] No QuizResultReporter assigned - score won't be saved or returned to GameMenu.");

        yield break;
    }

    private IEnumerator PlayFinalScoreAudio()
    {
        yield return PlayClipIfAssigned(yourScoreIsAudio);
        yield return PlayClipIfAssigned(GetNumberAudio(totalScore));
        yield return PlayClipIfAssigned(whileYourHighestScoreIsAudio);
        yield return PlayClipIfAssigned(GetNumberAudio(highScore));
    }

    private AudioClip GetNumberAudio(int number)
    {
        if (numberAudios == null || numberAudios.Count == 0) return null;
        if (number < 0 || number >= numberAudios.Count) return null;
        return numberAudios[number];
    }

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------
    private IEnumerator PlayClipIfAssigned(AudioClip clip)
    {
        if (voiceAudioSource == null || clip == null) yield break;

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    private IEnumerator PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null)
        {
            if (logDebug) Debug.LogWarning("[QuizController11] Missing audio clip or AudioSource.");
            yield return new WaitForSeconds(missingAudioDelay);
            yield break;
        }

        voiceAudioSource.Stop();
        voiceAudioSource.clip = clip;
        voiceAudioSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    private IEnumerator PlayVoiceSequence(params AudioClip[] clips)
    {
        if (voiceAudioSource == null || clips == null) yield break;

        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
    }
}