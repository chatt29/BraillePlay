using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Lesson10 : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Intro Lesson Page (plays before the Welcome Message, one page at a time)
    // -------------------------------------------------------------------------

    [Serializable]
    public class IntroLessonPage
    {
        [TextArea(2, 4)]
        public string pageText;

        public AudioClip pageAudio;
    }

    // -------------------------------------------------------------------------
    // Story Line
    // -------------------------------------------------------------------------

    [Serializable]
    public class StoryLine
    {
        [TextArea(2, 4)]
        public string storyText;

        public AudioClip storyAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Question — Multiple Choice (3 choices: A, B, C — answered via
    // Braille Dot1/Dot2/Dot3)
    // -------------------------------------------------------------------------

    public enum AnswerChoice { A, B, C }

    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Answer Choices")]
        public string choiceAText = "A";
        public string choiceBText = "B";
        public string choiceCText = "C";

        [Header("Correct Answer (Braille: Dot1=A, Dot2=B, Dot3=C)")]
        public AnswerChoice correctAnswer = AnswerChoice.A;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Support (shown after a wrong answer; falls back to the lesson's Support Message/Audio if left empty)")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // Quiz Question — True or False (2 choices — answered via Braille
    // Dot1=True, Dot2=False)
    // -------------------------------------------------------------------------

    public enum TrueFalseChoice { True, False }

    [Serializable]
    public class TrueFalseQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Optional sound effect played before the question")]
        public AudioClip soundEffectAudio;

        [Header("Choice Labels")]
        public string trueText = "True";
        public string falseText = "False";

        [Header("Correct Answer (Braille: Dot1=True, Dot2=False)")]
        public TrueFalseChoice correctAnswer = TrueFalseChoice.True;

        [Header("Feedback")]
        [TextArea(2, 4)]
        public string successMessage;
        public AudioClip successAudio;

        [TextArea(2, 4)]
        public string wrongMessage;

        [Header("Support (shown after a wrong answer; falls back to the lesson's Support Message/Audio if left empty)")]
        [TextArea(2, 4)]
        public string supportMessage;
        public AudioClip supportAudio;
    }

    [Serializable]
    public class BrailleLesson
    {
        [Header("Identity")]
        public string displayLabel;
        public string categoryLabel = "BRAILLE";

        [Header("Intro Messages")]
        [TextArea(2, 4)]
        public string promptMessage;

        [TextArea(2, 4)]
        public string repeatMessage;

        [Header("Display Image")]
        public Sprite displayImage;

        [Header("Intro Audio")]
        public AudioClip introAudio;
        public AudioClip instructionAudio;
        public AudioClip repeatAudio;

        [Header("1. Story Section (4 lines)")]
        public List<StoryLine> storyLines = new List<StoryLine>();

        // Question Section — the two lists below are asked back-to-back in
        // order: all Multiple Choice questions first, then all True/False
        // questions (see GetActiveQuestionData / GetTotalQuestionCount).
        [Header("2a. Question Section — Multiple Choice")]
        public List<QuizQuestion> questions = new List<QuizQuestion>();

        [Header("2b. Question Section — True or False")]
        public List<TrueFalseQuestion> trueFalseQuestions = new List<TrueFalseQuestion>();

        [Header("Support After Mistakes")]
        [TextArea(2, 4)]
        public string supportMessage;

        public AudioClip supportAudio;
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------

    [Header("UI")]
    public TMP_Text bubbleMessageText;
    public TMP_Text displayLabelText;
    public TMP_Text categoryText;
    public TMP_Text livePatternText;
    public Image displayImageUI;

    [Header("Answer State Image")]
    public Image answerStateImage;
    public Sprite correctStateSprite;
    public Sprite wrongStateSprite;
    public bool hideAnswerStateAtStart = false;

    [Header("Quiz Score UI")]
    public TMP_Text fixScoreText;
    public TMP_Text wrongScoreText;
    public TMP_Text totalScoreText;
    public TMP_Text highScoreText;

    [Header("Quiz Score Settings")]
    public int fixedScore = 100;
    public int deductionPerMistake = 1;
    public string highScoreKey = "BrailleSoundsAroundHighScore";

    // -------------------------------------------------------------------------
    // Audio
    // -------------------------------------------------------------------------

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioClip welcomeAudio;
    public AudioClip letsLearnAudio;
    public AudioClip genericCorrectAudio;
    public AudioClip genericTryAgainAudio;
    public AudioClip genericCompletedAudio;
    public AudioClip repeatQuestionAudio;

    [Header("Final Score Audio")]
    public AudioClip yourScoreIsAudio;
    public AudioClip whileYourHighestScoreIsAudio;

    [Header("Number Audios 0-100")]
    public List<AudioClip> numberAudios = new List<AudioClip>();

    // -------------------------------------------------------------------------
    // Intro Lesson (plays before the Welcome Message)
    // -------------------------------------------------------------------------

    [Header("Intro Lesson (plays BEFORE the Welcome Message)")]
    public List<IntroLessonPage> introLessonPages = new List<IntroLessonPage>();
    public float delayBetweenIntroPages = 0.5f;

    // -------------------------------------------------------------------------
    // Scene Text
    // -------------------------------------------------------------------------

    [Header("Scene Text")]
    [TextArea(2, 5)]
    public string welcomeMessage = "Welcome to Braille Sounds Around!";

    [TextArea(2, 5)]
    public string letsLearnMessage = "Let's identify some sounds.";

    [TextArea(2, 5)]
    public string completedMessage = "Great job! You finished the lesson.";

    [TextArea(2, 5)]
    public string repeatQuestionMessage = "You finished the lesson. Do you want to repeat again? Press R to repeat or Y to finish.";

    [Header("Repeat-Story Confirmation (mid-lesson Repeat button)")]
    [TextArea(2, 5)]
    public string repeatQuestionConfirmMessage = "Do you want to repeat the question? Press Next to continue.";
    public AudioClip repeatQuestionConfirmAudio;

    // -------------------------------------------------------------------------
    // Lesson Flow
    // -------------------------------------------------------------------------

    [Header("Lesson Flow")]
    public List<BrailleLesson> lessons = new List<BrailleLesson>();
    public float delayAfterVoice = 0.35f;
    public float noAudioTextDelay = 2f;
    public float delayAfterCorrect = 0.75f;
    public float delayBetweenStoryLines = 0.5f;

    [Header("Support Settings")]
    public bool resetMistakesAfterSupport = true;

    [Tooltip("Number of consecutive wrong answers on the same question before the Support Message plays. The 'repeat the story?' prompt still shows after every wrong answer, regardless of this count.")]
    public int mistakesBeforeSupport = 3;

    [Header("Repeat-Story Prompt (shown after every wrong answer)")]
    [TextArea(2, 5)]
    public string repeatStoryPromptMessage = "Do you want to hear the story again? Press Repeat to hear it again, or press Next to continue with the question.";
    public AudioClip repeatStoryPromptAudio;

    [Header("Typewriter Sync")]
    public bool useTypewriterEffect = true;
    [Min(0.005f)] public float defaultTypewriterCharacterDelay = 0.03f;
    [Min(0.001f)] public float minSyncedCharacterDelay = 0.01f;
    [Min(0.001f)] public float maxSyncedCharacterDelay = 0.12f;
    public bool waitForFullAudioBeforeContinuing = true;

    [Header("Debug")]
    public bool logDebug = true;

    // -------------------------------------------------------------------------
    // Private State
    // -------------------------------------------------------------------------

    private int currentLessonIndex = -1;
    private int currentQuestionIndex = -1;
    private int currentMistakeCount = 0;
    private int totalWrongCount = 0;
    private int totalScore = 100;
    private int highScore = 0;

    private bool lessonActive = false;
    private bool sceneFinished = false;
    private bool waitingForRepeatChoice = false;
    private bool waitingForQuizAnswer = false;

    // True whenever the player is being asked whether to repeat the Story —
    // either right after a wrong answer (repeatStoryPromptMessage) or after
    // replaying the Story via the mid-lesson Repeat button
    // (repeatQuestionConfirmMessage). Does NOT touch currentQuestionIndex,
    // score, or mistake count — it only gates HandleNext()/HandleRepeat()
    // into re-asking the same question or replaying the story again.
    private bool waitingForRepeatConfirmation = false;

    private Coroutine flowRoutine;
    private Coroutine bubbleTypeRoutine;

    // Read-only snapshot of "whichever question is currently active", built
    // fresh from either a QuizQuestion or a TrueFalseQuestion so the rest of
    // the flow (asking / correct / wrong / support) doesn't need to care
    // which list the question actually lives in.
    private struct ActiveQuestionData
    {
        public bool isTrueFalse;
        public string questionText;
        public AudioClip questionAudio;
        public AudioClip soundEffectAudio;
        public string choicesDisplayText;
        public string successMessage;
        public AudioClip successAudio;
        public string wrongMessage;
        public string supportMessage;
        public AudioClip supportAudio;
    }

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

    private void Update()
    {
        if (livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void Start()
    {
        if (logDebug)
            Debug.Log("BrailleSoundsAround1 started.");

        ResetQuizScore();
        RunFlow(BeginSceneFlow());
    }

    // -------------------------------------------------------------------------
    // Score
    // -------------------------------------------------------------------------

    private void ResetQuizScore()
    {
        totalWrongCount = 0;
        totalScore = fixedScore;
        highScore = PlayerPrefs.GetInt(highScoreKey, 0);

        UpdateScoreUI();
        ResetAnswerState();
    }

    private void AddMistake()
    {
        totalWrongCount++;

        int deductions = totalWrongCount / 3;
        totalScore = Mathf.Max(0, fixedScore - (deductions * deductionPerMistake));

        UpdateScoreUI();
        SetAnswerState(false);
    }

    private void UpdateScoreUI()
    {
        if (fixScoreText != null) fixScoreText.text = fixedScore.ToString();
        if (wrongScoreText != null) wrongScoreText.text = totalWrongCount.ToString();
        if (totalScoreText != null) totalScoreText.text = totalScore.ToString();
        if (highScoreText != null) highScoreText.text = highScore.ToString();
    }

    private void SaveHighScoreIfNeeded()
    {
        if (totalScore > highScore)
        {
            highScore = totalScore;
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }

        UpdateScoreUI();
    }

    // -------------------------------------------------------------------------
    // Answer State Image
    // -------------------------------------------------------------------------

    private void SetAnswerState(bool isCorrect)
    {
        if (answerStateImage == null) return;

        answerStateImage.enabled = true;
        answerStateImage.sprite = isCorrect ? correctStateSprite : wrongStateSprite;
    }

    private void ResetAnswerState()
    {
        if (answerStateImage == null) return;

        if (hideAnswerStateAtStart)
        {
            answerStateImage.enabled = false;
        }
        else
        {
            answerStateImage.enabled = true;
            answerStateImage.sprite = wrongStateSprite;
        }
    }

    // -------------------------------------------------------------------------
    // Coroutine Helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stops whatever flow coroutine is currently running and starts a new one.
    /// Centralizing this avoids repeating the stop/start boilerplate everywhere.
    /// </summary>
    private void RunFlow(IEnumerator routine)
    {
        if (flowRoutine != null)
            StopCoroutine(flowRoutine);

        flowRoutine = StartCoroutine(routine);
    }

    // -------------------------------------------------------------------------
    // Scene Flow
    // -------------------------------------------------------------------------

    private IEnumerator BeginSceneFlow()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        // Intro Lesson — plays first, before the Welcome Message.
        yield return PlayIntroLessonPages();
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(welcomeMessage, welcomeAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(letsLearnMessage, letsLearnAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        StartLesson(0);
    }

    /// <summary>
    /// Plays each configured intro page one at a time: shows the text in the
    /// message bubble, plays its matching audio clip, waits for it to finish,
    /// then moves on to the next page. Empty/unused page slots (no text and
    /// no audio) are skipped. Runs once, before the Welcome Message.
    /// </summary>
    private IEnumerator PlayIntroLessonPages()
    {
        if (introLessonPages == null) yield break;

        foreach (IntroLessonPage page in introLessonPages)
        {
            if (page == null) continue;
            if (string.IsNullOrWhiteSpace(page.pageText) && page.pageAudio == null) continue;

            yield return ShowBubbleMessageSynced(page.pageText, page.pageAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayBetweenIntroPages);
        }
    }

    private void StartLesson(int index)
    {
        if (index < 0 || index >= lessons.Count)
        {
            RunFlow(CompleteScene());
            return;
        }

        currentLessonIndex = index;
        currentQuestionIndex = -1;
        currentMistakeCount = 0;
        lessonActive = true;
        sceneFinished = false;
        waitingForRepeatChoice = false;
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = false;

        if (logDebug)
            Debug.Log($"Starting lesson {currentLessonIndex}: {lessons[currentLessonIndex].displayLabel}");

        RunFlow(PlayLessonFromBeginning(lessons[currentLessonIndex]));
    }

    // -------------------------------------------------------------------------
    // Lesson Sequence
    //
    // Exact order:
    //   1. Display Label / Category Label / Display Image
    //   2. Prompt Message (+ audio)
    //   3. Story Section        -> 4 lines, one text box + audio at a time
    //   4. Question Section     -> Multiple Choice questions, then
    //                              True/False questions (see
    //                              GetTotalQuestionCount / GetActiveQuestionData)
    //        - Success Message on correct answer -> advance to next question
    //        - Wrong Message on every incorrect answer, Support Message only
    //          after N consecutive mistakes, then a "repeat the story?"
    //          prompt on EVERY incorrect answer (see HandleWrongAnswer)
    //   5. After the last question is answered correctly -> next lesson
    //
    // This single coroutine is reused both when a lesson first starts and
    // whenever the player asks to repeat the current lesson, so there is one
    // source of truth for "what the beginning of a lesson looks like".
    // -------------------------------------------------------------------------

    private IEnumerator PlayLessonFromBeginning(BrailleLesson lesson)
    {
        ResetAnswerState();

        // Step 1: Display Label, Category Label, Display Image
        ApplyLessonDisplay(lesson);

        // Step 2: Prompt Message + audio
        yield return ShowPromptMessage(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 3: Story Section
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        // Step 4: Question Section — start with the first question
        currentQuestionIndex = 0;
        yield return AskQuizQuestion(lesson, currentQuestionIndex);
    }

    /// <summary>Display Label, Category Label, Display Image.</summary>
    private void ApplyLessonDisplay(BrailleLesson lesson)
    {
        if (displayLabelText != null)
            displayLabelText.text = lesson.displayLabel;

        if (displayImageUI != null)
        {
            displayImageUI.sprite = lesson.displayImage;
            displayImageUI.enabled = lesson.displayImage != null;
        }

        if (categoryText != null)
            categoryText.text = string.IsNullOrWhiteSpace(lesson.categoryLabel)
                ? "BRAILLE"
                : lesson.categoryLabel;

        // displayLabel is also used as a fallback for the prompt bubble text
        // (see ShowPromptMessage) if promptMessage is left empty.
    }

    /// <summary>Prompt Message together with its intro/instruction audio.</summary>
    private IEnumerator ShowPromptMessage(BrailleLesson lesson)
    {
        string introMessage = !string.IsNullOrWhiteSpace(lesson.promptMessage)
            ? lesson.promptMessage
            : lesson.displayLabel;

        yield return ShowBubbleMessageWithAudioSequence(
            introMessage,
            noAudioTextDelay,
            lesson.introAudio,
            lesson.instructionAudio
        );
    }

    /// <summary>
    /// Step 3 — Story Section. Plays each of the lesson's story lines one at a
    /// time: shows the text box, plays its matching audio clip, waits for it
    /// to finish, then moves on to the next line. Empty/unused story line
    /// slots (no text and no audio) are skipped.
    /// </summary>
    private IEnumerator PlayStory(BrailleLesson lesson)
    {
        if (lesson.storyLines == null) yield break;

        foreach (StoryLine line in lesson.storyLines)
        {
            if (line == null) continue;
            if (string.IsNullOrWhiteSpace(line.storyText) && line.storyAudio == null) continue;

            yield return ShowBubbleMessageSynced(line.storyText, line.storyAudio, noAudioTextDelay);
            yield return new WaitForSeconds(delayBetweenStoryLines);
        }
    }

    // -------------------------------------------------------------------------
    // Combined Multiple Choice / True-False question lookup
    //
    // Multiple Choice questions occupy indices [0, mcCount), and True/False
    // questions occupy [mcCount, mcCount + tfCount) — i.e. all Multiple
    // Choice questions are asked first, then all True/False questions, each
    // list keeping its own Inspector ordering.
    // -------------------------------------------------------------------------

    private int GetTotalQuestionCount(BrailleLesson lesson)
    {
        int mcCount = lesson.questions?.Count ?? 0;
        int tfCount = lesson.trueFalseQuestions?.Count ?? 0;
        return mcCount + tfCount;
    }

    private ActiveQuestionData GetActiveQuestionData(BrailleLesson lesson, int index)
    {
        int mcCount = lesson.questions?.Count ?? 0;

        if (index < mcCount)
        {
            QuizQuestion q = lesson.questions[index];
            return new ActiveQuestionData
            {
                isTrueFalse = false,
                questionText = q.questionText,
                questionAudio = q.questionAudio,
                soundEffectAudio = q.soundEffectAudio,
                choicesDisplayText = $"A) {q.choiceAText}   B) {q.choiceBText}   C) {q.choiceCText}",
                successMessage = q.successMessage,
                successAudio = q.successAudio,
                wrongMessage = q.wrongMessage,
                supportMessage = q.supportMessage,
                supportAudio = q.supportAudio
            };
        }

        TrueFalseQuestion tf = lesson.trueFalseQuestions[index - mcCount];
        return new ActiveQuestionData
        {
            isTrueFalse = true,
            questionText = tf.questionText,
            questionAudio = tf.questionAudio,
            soundEffectAudio = tf.soundEffectAudio,
            choicesDisplayText = $"1) {tf.trueText}   2) {tf.falseText}",
            successMessage = tf.successMessage,
            successAudio = tf.successAudio,
            wrongMessage = tf.wrongMessage,
            supportMessage = tf.supportMessage,
            supportAudio = tf.supportAudio
        };
    }

    /// <summary>
    /// Step 4 — plays the optional sound effect then asks the question at the
    /// given combined index (Multiple Choice or True/False — see
    /// GetActiveQuestionData). Used for the first ask of a question and every
    /// re-ask after a wrong answer or support message, so the sound effect +
    /// formatting logic only lives here.
    /// </summary>
    private IEnumerator AskQuizQuestion(BrailleLesson lesson, int questionIndex)
    {
        int totalQuestions = GetTotalQuestionCount(lesson);

        if (questionIndex < 0 || questionIndex >= totalQuestions)
        {
            // No (more) questions configured — treat lesson as complete.
            StartLesson(currentLessonIndex + 1);
            yield break;
        }

        waitingForQuizAnswer = true;
        ActiveQuestionData data = GetActiveQuestionData(lesson, questionIndex);

        if (data.soundEffectAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = data.soundEffectAudio;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(data.soundEffectAudio.length);
            yield return new WaitForSeconds(delayAfterVoice);
        }

        // Question text goes on the display label, answer choices go on the
        // category text.
        if (categoryText != null)
            categoryText.text = data.choicesDisplayText;

        yield return ShowBubbleMessageSynced(
            data.questionText,
            data.questionAudio,
            noAudioTextDelay,
            displayLabelText
        );
    }

    // -------------------------------------------------------------------------
    // Input Handling
    // -------------------------------------------------------------------------

    private void HandleBrailleChordSubmitted(string submittedPattern)
    {
        if (!lessonActive || sceneFinished || waitingForRepeatChoice || waitingForRepeatConfirmation)
            return;

        if (waitingForQuizAnswer)
        {
            HandleQuizAnswer(submittedPattern);
            return;
        }
    }

    /// <summary>
    /// Validates the submitted Braille pattern against the current question's
    /// correct answer.
    /// Multiple Choice: Dot 1 = A, Dot 2 = B, Dot 3 = C.
    /// True/False:      Dot 1 = True, Dot 2 = False.
    /// </summary>
    private void HandleQuizAnswer(string pattern)
    {
        if (!waitingForQuizAnswer) return;

        BrailleLesson lesson = lessons[currentLessonIndex];
        int mcCount = lesson.questions?.Count ?? 0;
        bool isCorrect;

        if (currentQuestionIndex < mcCount)
        {
            AnswerChoice userAnswer;

            if (pattern == "100000") userAnswer = AnswerChoice.A;
            else if (pattern == "010000") userAnswer = AnswerChoice.B;
            else if (pattern == "001000") userAnswer = AnswerChoice.C;
            else return;

            isCorrect = userAnswer == lesson.questions[currentQuestionIndex].correctAnswer;
        }
        else
        {
            TrueFalseChoice userAnswer;

            if (pattern == "100000") userAnswer = TrueFalseChoice.True;
            else if (pattern == "010000") userAnswer = TrueFalseChoice.False;
            else return;

            isCorrect = userAnswer == lesson.trueFalseQuestions[currentQuestionIndex - mcCount].correctAnswer;
        }

        waitingForQuizAnswer = false;

        if (isCorrect)
        {
            currentMistakeCount = 0;

            SetAnswerState(true);
            RunFlow(HandleCorrectAnswer(lesson, currentQuestionIndex));
        }
        else
        {
            currentMistakeCount++;
            AddMistake();

            RunFlow(HandleWrongAnswer(lesson, currentQuestionIndex));
        }
    }

    // -------------------------------------------------------------------------
    // Correct / Wrong / Support
    // -------------------------------------------------------------------------

    /// <summary>
    /// Success Message for the current question, then advance to the next
    /// question — or, if that was the last question, advance to the next
    /// lesson.
    /// </summary>
    private IEnumerator HandleCorrectAnswer(BrailleLesson lesson, int questionIndex)
    {
        SaveHighScoreIfNeeded();

        ActiveQuestionData data = GetActiveQuestionData(lesson, questionIndex);

        string message = !string.IsNullOrWhiteSpace(data.successMessage)
            ? data.successMessage
            : $"Correct! {lesson.displayLabel}.";

        AudioClip clip = data.successAudio != null
            ? data.successAudio
            : genericCorrectAudio;

        yield return ShowBubbleMessageSynced(message, clip, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterCorrect);

        currentQuestionIndex++;

        if (currentQuestionIndex >= GetTotalQuestionCount(lesson))
        {
            // All questions answered correctly — move on to the next lesson.
            StartLesson(currentLessonIndex + 1);
        }
        else
        {
            RunFlow(AskQuizQuestion(lesson, currentQuestionIndex));
        }
    }

    /// <summary>
    /// Wrong Message for the current question, then — only once the player
    /// has made <see cref="mistakesBeforeSupport"/> consecutive mistakes on
    /// this question — the Support Message. After that, EVERY wrong answer
    /// (regardless of mistake count) asks the player whether they want to
    /// repeat the Story: pressing Repeat replays the Story section (and
    /// re-shows this same prompt), pressing Next continues on and re-asks
    /// the current question.
    /// </summary>
    private IEnumerator HandleWrongAnswer(BrailleLesson lesson, int questionIndex)
    {
        ActiveQuestionData data = GetActiveQuestionData(lesson, questionIndex);

        string wrongMessage = !string.IsNullOrWhiteSpace(data.wrongMessage)
            ? data.wrongMessage
            : "Try again.";

        yield return ShowBubbleMessageSynced(wrongMessage, genericTryAgainAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        if (currentMistakeCount >= mistakesBeforeSupport)
        {
            string supportMessage = !string.IsNullOrWhiteSpace(data.supportMessage)
                ? data.supportMessage
                : (!string.IsNullOrWhiteSpace(lesson.supportMessage)
                    ? lesson.supportMessage
                    : "Here is some help. Listen carefully and try again.");

            AudioClip supportClip = data.supportAudio != null
                ? data.supportAudio
                : lesson.supportAudio;

            yield return ShowBubbleMessageSynced(supportMessage, supportClip, noAudioTextDelay);
            yield return new WaitForSeconds(delayAfterVoice);

            if (resetMistakesAfterSupport)
                currentMistakeCount = 0;
        }

        // Ask whether to repeat the Story — every wrong answer, regardless
        // of mistake count. Pressing Repeat (HandleRepeat) replays the Story
        // and re-shows this same prompt; pressing Next (HandleNext)
        // continues on and re-asks the current question.
        waitingForRepeatConfirmation = true;

        yield return ShowBubbleMessageSynced(
            repeatStoryPromptMessage,
            repeatStoryPromptAudio,
            noAudioTextDelay
        );

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Repeat or Next.
    }

    // -------------------------------------------------------------------------
    // Repeat / Next handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mid-lesson Repeat behavior:
    ///   - Replays the Story section only (Display Label/Image and the
    ///     current question index are left untouched — no full lesson
    ///     restart, no score/mistake reset).
    ///   - Afterwards asks the player whether they want to repeat the
    ///     current question ("Press Next to continue").
    ///   - The current question is only re-asked once HandleNext() fires.
    ///
    /// Separately, if the player is at the end-of-scene repeat prompt
    /// (waitingForRepeatChoice), Repeat still restarts the whole scene from
    /// lesson 0, exactly as before.
    /// </summary>
    private void HandleRepeat()
    {
        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            ResetQuizScore();
            StartLesson(0);
            return;
        }

        if (!lessonActive || sceneFinished)
            return;

        if (currentLessonIndex < 0 || currentLessonIndex >= lessons.Count)
            return;

        BrailleLesson lesson = lessons[currentLessonIndex];

        // Cancel any pending quiz answer wait — we're repeating the story
        // instead of waiting on an answer right now.
        waitingForQuizAnswer = false;
        waitingForRepeatConfirmation = true;

        RunFlow(RepeatStorySection(lesson));
    }

    /// <summary>
    /// Replays just the Story section for the current lesson, then asks the
    /// player if they want to repeat the current question. Does not reset
    /// currentQuestionIndex, score, or mistake count.
    /// </summary>
    private IEnumerator RepeatStorySection(BrailleLesson lesson)
    {
        yield return PlayStory(lesson);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return ShowBubbleMessageSynced(
            repeatQuestionConfirmMessage,
            repeatQuestionConfirmAudio,
            noAudioTextDelay
        );

        // Stays here — waitingForRepeatConfirmation remains true until the
        // player presses Next (see HandleNext()).
    }

    private void HandleNext()
    {
        if (waitingForRepeatConfirmation)
        {
            waitingForRepeatConfirmation = false;
            RunFlow(AskQuizQuestion(lessons[currentLessonIndex], currentQuestionIndex));
            return;
        }

        if (waitingForRepeatChoice)
        {
            waitingForRepeatChoice = false;
            RunFlow(FinalizeSceneCompletion());
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Scene Completion
    // -------------------------------------------------------------------------

    private IEnumerator CompleteScene()
    {
        lessonActive = false;
        sceneFinished = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        yield return ShowBubbleMessageSynced(completedMessage, genericCompletedAudio, noAudioTextDelay);
        yield return new WaitForSeconds(delayAfterVoice);

        yield return PlayFinalScoreAudio();
        yield return new WaitForSeconds(delayAfterVoice);

        waitingForRepeatChoice = true;
        yield return ShowBubbleMessageSynced(repeatQuestionMessage, repeatQuestionAudio, noAudioTextDelay);
    }

    private IEnumerator FinalizeSceneCompletion()
    {
        sceneFinished = true;
        lessonActive = false;
        waitingForRepeatChoice = false;

        SaveHighScoreIfNeeded();

        if (displayImageUI != null)
            displayImageUI.enabled = false;

        if (displayLabelText != null)
            displayLabelText.text = string.Empty;

        ResetAnswerState();

        string finalMessage = $"Your score is {totalScore}, while your highest score is {highScore}.";

        yield return ShowBubbleMessageSynced(finalMessage, genericCompletedAudio, noAudioTextDelay);
        yield return PlayFinalScoreAudio();
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
    // Bubble Text / Typewriter
    // -------------------------------------------------------------------------

    private IEnumerator ShowBubbleMessageSynced(string message, AudioClip clip, float fallbackWait, TMP_Text targetText = null)
    {
        TMP_Text target = targetText != null ? targetText : bubbleMessageText;
        if (target == null) yield break;

        StopBubbleTyping();

        float audioDuration = GetClipDuration(clip);
        float charDelay = GetCharacterDelayForMessage(message, audioDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, target, () => typingFinished = true));

        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        while (!typingFinished)
            yield return null;

        bubbleTypeRoutine = null;

        if (clip != null && voiceAudioSource != null && waitForFullAudioBeforeContinuing)
            yield return new WaitForSeconds(Mathf.Max(0f, clip.length - EstimatedTypingDuration(message, charDelay)));
        else if (clip == null)
            yield return new WaitForSeconds(fallbackWait);
    }

    private IEnumerator ShowBubbleMessageWithAudioSequence(string message, float fallbackWait, params AudioClip[] clips)
    {
        if (bubbleMessageText == null) yield break;

        StopBubbleTyping();

        float totalDuration = GetTotalClipDuration(clips);
        float charDelay = GetCharacterDelayForMessage(message, totalDuration);

        bool typingFinished = false;
        bubbleTypeRoutine = StartCoroutine(TypeBubbleText(message, charDelay, bubbleMessageText, () => typingFinished = true));

        if (voiceAudioSource != null)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip == null) continue;

                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }
        else
        {
            while (!typingFinished)
                yield return null;

            yield return new WaitForSeconds(fallbackWait);
        }

        while (!typingFinished)
            yield return null;

        bubbleTypeRoutine = null;
    }

    private IEnumerator TypeBubbleText(string message, float characterDelay, TMP_Text target, Action onComplete = null)
    {
        if (target == null) yield break;

        if (!useTypewriterEffect)
        {
            target.text = message;
            onComplete?.Invoke();
            yield break;
        }

        target.text = string.Empty;

        if (string.IsNullOrEmpty(message))
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < message.Length; i++)
        {
            target.text += message[i];
            yield return new WaitForSeconds(characterDelay);
        }

        onComplete?.Invoke();
    }

    private void StopBubbleTyping()
    {
        if (bubbleTypeRoutine != null)
        {
            StopCoroutine(bubbleTypeRoutine);
            bubbleTypeRoutine = null;
        }
    }

    private float GetCharacterDelayForMessage(string message, float audioDuration)
    {
        if (!useTypewriterEffect) return 0f;

        int visibleLength = GetVisibleCharacterCount(message);

        if (visibleLength <= 0) return defaultTypewriterCharacterDelay;
        if (audioDuration <= 0f) return defaultTypewriterCharacterDelay;

        float syncedDelay = audioDuration / visibleLength;
        return Mathf.Clamp(syncedDelay, minSyncedCharacterDelay, maxSyncedCharacterDelay);
    }

    private int GetVisibleCharacterCount(string message)
    {
        if (string.IsNullOrEmpty(message)) return 0;

        int count = 0;
        foreach (char c in message)
        {
            if (!char.IsWhiteSpace(c)) count++;
        }

        return Mathf.Max(1, count);
    }

    private float EstimatedTypingDuration(string message, float characterDelay)
        => GetVisibleCharacterCount(message) * characterDelay;

    private float GetClipDuration(AudioClip clip)
        => clip != null ? clip.length : 0f;

    private float GetTotalClipDuration(params AudioClip[] clips)
    {
        float total = 0f;
        if (clips == null) return total;
        foreach (AudioClip c in clips)
            if (c != null) total += c.length;
        return total;
    }
}
