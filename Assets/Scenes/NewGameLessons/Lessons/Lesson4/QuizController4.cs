using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quiz for the alphabet lesson (A-Z), audio + Braille input only.
/// Started by LessonController4 through StartQuiz().
///
/// End of quiz (after the score, high score and repeat question):
///   Repeat -> reset the score and play the quiz again from A
///   Next   -> report the score and return to the main menu
///   Back   -> same as Next, with an optional confirmation cue first
/// </summary>
public class QuizController4 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Quiz Mode Data (scored) — one entry per letter, A through Z.
    // -------------------------------------------------------------------------
    [Serializable]
    public class AlphabetLetter
    {
        [Header("Identity")]
        [Tooltip("The letter this entry tests (A-Z). The correct Braille pattern is derived from this automatically.")]
        public char letter = 'A';

        [Header("Prompt")]
        [TextArea(2, 4)]
        public string promptMessage;
        public AudioClip introAudio;
        public AudioClip instructionAudio;

        [Header("Result Audio")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;
        public AudioClip wrongAudio;

        [Header("Support After N Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;

        /// <summary>Looks up the correct 6-dot Braille pattern for this letter.</summary>
        public string GetCorrectPattern()
        {
            char upper = char.ToUpperInvariant(letter);
            return BrailleAlphabetPatterns.TryGetValue(upper, out string pattern)
                ? pattern
                : "000000";
        }
    }

    // -------------------------------------------------------------------------
    // Shared Braille Alphabet Data (also used by LessonController4)
    // -------------------------------------------------------------------------

    public static readonly Dictionary<char, string> BrailleAlphabetPatterns = new Dictionary<char, string>
    {
        { 'A', "100000" },
        { 'B', "110000" },
        { 'C', "100100" },
        { 'D', "100110" },
        { 'E', "100010" },
        { 'F', "110100" },
        { 'G', "110110" },
        { 'H', "110010" },
        { 'I', "010100" },
        { 'J', "010110" },
        { 'K', "101000" },
        { 'L', "111000" },
        { 'M', "101100" },
        { 'N', "101110" },
        { 'O', "101010" },
        { 'P', "111100" },
        { 'Q', "111110" },
        { 'R', "111010" },
        { 'S', "011100" },
        { 'T', "011110" },
        { 'U', "101001" },
        { 'V', "111001" },
        { 'W', "010111" },
        { 'X', "101101" },
        { 'Y', "101111" },
        { 'Z', "101011" },
    };

    /// <summary>Turns a pattern like "110000" into a human-readable "Dots 1 and 2".</summary>
    public static string DescribeDots(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return "no dots";

        List<int> dots = new List<int>();
        for (int i = 0; i < pattern.Length && i < 6; i++)
            if (pattern[i] == '1') dots.Add(i + 1);

        if (dots.Count == 0) return "no dots";
        if (dots.Count == 1) return $"Dot {dots[0]}";
        if (dots.Count == 2) return $"Dots {dots[0]} and {dots[1]}";

        string allButLast = string.Join(", ", dots.GetRange(0, dots.Count - 1));
        return $"Dots {allButLast}, and {dots[dots.Count - 1]}";
    }

    // -------------------------------------------------------------------------
    // Quiz Score Settings
    // -------------------------------------------------------------------------

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "IdentifyingAlphabetsHighScore";

    [Header("Quiz Result Reporting")]
    public QuizResultReporter resultReporter;

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Scene Text
    // -------------------------------------------------------------------------

    [Header("Quiz Intro")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";
    public AudioClip welcomeAudio;

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's learn the alphabet in Braille.";
    public AudioClip letsLearnAudio;

    [Header("Repeat-or-Finish Choice (after final score)")]
    [TextArea(2, 5)]
    public string repeatQuestionMessage = "Do you want to repeat the quiz again? Press repeat to try again, or press next to finish.";
    public AudioClip repeatQuestionAudio;

    // -------------------------------------------------------------------------
    // Back To Main Menu
    // -------------------------------------------------------------------------

    [Header("Back To Main Menu")]
    [Tooltip("Only used if no QuizResultReporter is assigned. Must be added in File > Build Settings.")]
    public string mainMenuSceneName = "GameMenu";
    [Tooltip("Optional cue, e.g. 'Going back to the main menu.' Played only when Back is pressed.")]
    public AudioClip backToMenuAudio;

    // -------------------------------------------------------------------------
    // Quiz Flow
    // -------------------------------------------------------------------------

    [Header("Quiz Mode - Scored Assessment A-Z")]
    [Tooltip("One entry per letter. Use the context menu 'Auto-Fill Alphabet A-Z' on this component to generate all 26 entries in order.")]
    public List<AlphabetLetter> alphabetLessons = new List<AlphabetLetter>();

    [Header("Timing")]
    public float delayAfterVoice = 0.35f;
    [Tooltip("How long to wait after a line if no audio clip is assigned for it.")]
    public float noAudioFallbackDelay = 2f;
    public float delayAfterCorrect = 0.75f;

    [Header("Support Settings")]
    public int mistakesBeforeSupport = 3;
    public bool resetMistakesAfterSupport = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Editor Utility
    // -------------------------------------------------------------------------

    [ContextMenu("Auto-Fill Alphabet A-Z")]
    private void AutoFillAlphabet()
    {
        alphabetLessons.Clear();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            alphabetLessons.Add(new AlphabetLetter { letter = c });
        }
    }

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    /// <summary>True from the moment LessonController4 calls StartQuiz() until the learner finishes.</summary>
    private bool quizPhaseActive = false;

    private int currentLessonIndex = -1;
    private int currentMistakeCount = 0;

    /// <summary>True while a specific letter's prompt-and-wait sequence owns the flow (guards against stray Repeat input mid-transition).</summary>
    private bool letterActive = false;
    private bool waitingForChoiceAnswer = false;
    private bool waitingForRepeatChoice = false;   // end-of-quiz: Repeat, Next or Back
    private bool returningToMenu = false;          // guards against Next/Back/Repeat firing twice

    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private Coroutine flowRoutine;

    // -------------------------------------------------------------------------
    // Unity Events
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

    // -------------------------------------------------------------------------
    // Entry Point — called by LessonController4
    // -------------------------------------------------------------------------

    /// <summary>Called by LessonController4 once the learner finishes Lesson Mode and chooses Next.</summary>
    public void StartQuiz()
    {
        if (logDebug)
            Debug.Log("QuizController4 started.");

        quizPhaseActive = true;
        returningToMenu = false;
        waitingForRepeatChoice = false;

        RunFlow(StartQuizAfterLesson());
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

    private void StopVoice()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();
    }

    // -------------------------------------------------------------------------
    // Quiz Flow — Intro -> A-Z assessment -> Final Score -> Repeat/Finish
    // -------------------------------------------------------------------------

    private IEnumerator StartQuizAfterLesson()
    {
        ResetQuizScore();

        yield return PlayVoiceLine(welcomeMessage, welcomeAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayVoiceLine(letsLearnMessage, letsLearnAudio, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= alphabetLessons.Count)
        {
            RunFlow(FinalizeQuizCompletion());
            return;
        }

        currentLessonIndex = index;
        currentMistakeCount = 0;
        letterActive = true;
        waitingForChoiceAnswer = false;
        waitingForRepeatChoice = false;

        if (logDebug)
            Debug.Log($"[Quiz] Starting letter {index}: {alphabetLessons[index].letter}");

        RunFlow(PlayLessonFromBeginning(alphabetLessons[index]));
    }

    // -------------------------------------------------------------------------
    // Quiz Mode Letter Sequence
    //
    // Exact order (per letter, A through Z):
    //   1. Prompt Message (+ intro/instruction audio)
    //   2. Wait for the player to type the matching Braille pattern
    //   3. Success Message  -> handled in HandleCorrectAnswer, then auto-advance
    //   4. Wrong Message    -> handled in HandleWrongAnswer, re-asks same letter
    //   5. Support Message (+ audio) -> only after N consecutive mistakes
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(AlphabetLetter letterLesson)
    {
        yield return PlayPromptSequence(letterLesson);
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForChoiceAnswer = true;
    }

    private IEnumerator PlayPromptSequence(AlphabetLetter letterLesson)
    {
        char upper = char.ToUpperInvariant(letterLesson.letter);

        string prompt = !string.IsNullOrWhiteSpace(letterLesson.promptMessage)
            ? letterLesson.promptMessage
            : $"This is the letter {upper}. Type it in Braille.";

        yield return PlayVoiceLineSequence(prompt, noAudioFallbackDelay, letterLesson.introAudio, letterLesson.instructionAudio);
    }

    private void HandleAlphabetAnswer(string pattern)
    {
        if (!waitingForChoiceAnswer) return;

        AlphabetLetter letterLesson = alphabetLessons[currentLessonIndex];
        waitingForChoiceAnswer = false;

        if (pattern == letterLesson.GetCorrectPattern())
        {
            currentMistakeCount = 0;
            letterActive = false;

            RunFlow(HandleCorrectAnswer(letterLesson));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            if (currentMistakeCount >= mistakesBeforeSupport)
                RunFlow(HandleSupportThenRetry(letterLesson));
            else
                RunFlow(HandleWrongAnswer(letterLesson));
        }
    }

    /// <summary>Step 3 — Success Message, then automatically advance to the next letter.</summary>
    private IEnumerator HandleCorrectAnswer(AlphabetLetter letterLesson)
    {
        SaveHighScoreIfNeeded();

        char upper = char.ToUpperInvariant(letterLesson.letter);

        string message = !string.IsNullOrWhiteSpace(letterLesson.successMessage)
            ? letterLesson.successMessage
            : $"Correct! That is {upper}.";

        AudioClip clip = letterLesson.successAudio != null ? letterLesson.successAudio : genericCorrectAudio;

        yield return PlayVoiceLine(message, clip, noAudioFallbackDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        StartLesson(currentLessonIndex + 1);
    }

    /// <summary>Step 4 — Wrong Message, then re-ask the same letter (no full restart).</summary>
    private IEnumerator HandleWrongAnswer(AlphabetLetter letterLesson)
    {
        string message = !string.IsNullOrWhiteSpace(letterLesson.wrongMessage)
            ? letterLesson.wrongMessage
            : "Try again.";

        AudioClip clip = letterLesson.wrongAudio != null ? letterLesson.wrongAudio : genericTryAgainAudio;

        yield return PlayVoiceLine(message, clip, noAudioFallbackDelay);
        waitingForChoiceAnswer = true;
    }

    /// <summary>
    /// Step 5 — after N consecutive mistakes, play the Support Message + audio
    /// to help the player, reset the mistake streak, then re-ask the same letter.
    /// </summary>
    private IEnumerator HandleSupportThenRetry(AlphabetLetter letterLesson)
    {
        string message = !string.IsNullOrWhiteSpace(letterLesson.supportMessage)
            ? letterLesson.supportMessage
            : "Here is some help. Listen carefully and try typing the letter again.";

        yield return PlayVoiceLine(message, letterLesson.supportAudio, noAudioFallbackDelay);

        if (resetMistakesAfterSupport)
            currentMistakeCount = 0;

        waitingForChoiceAnswer = true;
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!quizPhaseActive) return;
        if (!letterActive) return;
        if (!waitingForChoiceAnswer) return;

        HandleAlphabetAnswer(submittedPattern);
    }

    private void HandleRepeat()
    {
        if (!quizPhaseActive || returningToMenu) return;

        // End of quiz: play the quiz again from A.
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            StopVoice();
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        // Ignore Repeat while a correct-answer transition to the next letter is
        // already in progress (letterActive is false during that window).
        if (!letterActive) return;
        if (currentLessonIndex < 0 || currentLessonIndex >= alphabetLessons.Count) return;

        AlphabetLetter letterLesson = alphabetLessons[currentLessonIndex];

        letterActive = true;
        waitingForChoiceAnswer = false;
        currentMistakeCount = 0;

        RunFlow(PlayLessonFromBeginning(letterLesson));
    }

    private void HandleNext()
    {
        if (!quizPhaseActive) return;
        if (!waitingForRepeatChoice || returningToMenu) return;

        returningToMenu = true;
        waitingForRepeatChoice = false;

        if (logDebug)
            Debug.Log("[QuizController4] Learner chose to finish the quiz.");

        StopVoice();
        ReturnToMenu();
    }

    private void HandleBack()
    {
        // Back only works at the very end of the quiz (after the score is announced).
        if (!quizPhaseActive) return;
        if (!waitingForRepeatChoice || returningToMenu) return;

        returningToMenu = true;
        waitingForRepeatChoice = false;

        RunFlow(BackToMenuFlow());
    }

    // -------------------------------------------------------------------------
    // Final Score -> Repeat/Finish Choice
    // -------------------------------------------------------------------------

    private IEnumerator FinalizeQuizCompletion()
    {
        letterActive = false;
        waitingForChoiceAnswer = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return PlayVoiceLine(finalMessage, genericCompletedAudio, noAudioFallbackDelay);
        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        // The flag is set right before the question so the learner can answer
        // (Repeat / Next / Back) even while the question is still playing.
        waitingForRepeatChoice = true;
        yield return PlayVoiceLine(repeatQuestionMessage, repeatQuestionAudio, noAudioFallbackDelay);

        // Stays here until the learner presses Repeat, Next or Back.
    }

    private IEnumerator BackToMenuFlow()
    {
        // Cut off the repeat-question audio if it is still playing.
        StopVoice();

        if (backToMenuAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.clip = backToMenuAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(backToMenuAudio.length);
        }

        ReturnToMenu();
    }

    /// <summary>Saves the high score, then reports the score and returns to the menu.</summary>
    private void ReturnToMenu()
    {
        quizPhaseActive = false;

        SaveHighScoreIfNeeded();

        if (resultReporter != null)
        {
            resultReporter.ReportScoreAndReturn(totalScore);   // saves score + returns to GameMenu
        }
        else
        {
            Debug.LogWarning("[QuizController4] No QuizResultReporter assigned - loading menu directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
    }

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
    // Voice Line Playback (audio-only — no text UI)
    // -------------------------------------------------------------------------

    private IEnumerator PlayVoiceLine(string debugMessage, AudioClip clip, float fallbackWait)
    {
        if (logDebug && !string.IsNullOrEmpty(debugMessage))
            Debug.Log($"[Quiz VO] {debugMessage}");

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

    private IEnumerator PlayVoiceLineSequence(string debugMessage, float fallbackWait, params AudioClip[] clips)
    {
        if (logDebug && !string.IsNullOrEmpty(debugMessage))
            Debug.Log($"[Quiz VO] {debugMessage}");

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