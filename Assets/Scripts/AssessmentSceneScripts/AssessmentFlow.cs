using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking; 
using UnityEngine.UI;

public class AssessmentFlow : MonoBehaviour
{
    public enum AssessmentLevel
    {
        Beginner,
        Intermediate,
        Advance
    }

    private enum AssessmentNode
    {
        Q1_DotPositions,
        Q2_Alphabet,
        Q3_NumberSign,
        Q4_OneCellContractions,
        Q5_PunctuationAndCapital,
        Q6_FinalPunctuationAndCapital,
        Finished
    }

    [Header("Database")]
    public string updateAssessmentURL = "http://localhost/brailleplay/update_assessment.php";
    private string currentUsername => AccessibleLoginFlow.LoggedInUsername;
    [Header("UI")]
    public TMP_Text assessmentQuestionText;
    public TMP_Text resultText;

    [Header("Audio Sources")]
    public AudioSource voiceSource;
    public AudioSource sfxSource;

    [Header("General SFX")]
    public AudioClip typewriterTickSfx;
    public AudioClip validButtonPressSfx;

    [Header("Question Audio Slots")]
    public AudioClip q1Audio;
    public AudioClip q2Audio;
    public AudioClip q3Audio;
    public AudioClip q4Audio;
    public AudioClip q5Audio;
    public AudioClip q6Audio;

    [Header("Result Audio")]
    public AudioClip beginnerAudio;
    public AudioClip intermediateAudio;
    public AudioClip advanceAudio;


    [Header("Scene Loading")]
    public bool loadSceneAfterResult = true;
    public float sceneLoadDelay = 2f;

    [Header("Typewriter")]
    public float characterDelay = 0.03f;
    public bool playTickPerCharacter = false;

    [Header("Options")]
    public bool autoStartOnEnable = true;
    public bool logFlow = true;

    private AssessmentNode currentNode;
    private Coroutine typewriterRoutine;
    private bool isTyping;
    private string currentFullText = "";
    private bool isFinished;

    private const string Q1_TEXT = "Can the student identify dot position 1 to 6 in a single cell without confusion?";
    private const string Q2_TEXT = "Can the student identify all 26 letters of the alphabet when they are uncontracted, stand-alone?";
    private const string Q3_TEXT = "Does the student recognize the number sign, numeric indicator, that tells them the next character is a number, not a letter?";
    private const string Q4_TEXT = "Can the student identify one-cell contractions?";
    private const string Q5_TEXT = "Can the student distinguish between a period, a comma, and a capital letter indicator?";
    private const string Q6_TEXT = "Can the student distinguish between a period, a comma, and a capital letter indicator?";

    private void OnEnable()
    {
        AssessmentInputHandler.OnYes += HandleYes;
        AssessmentInputHandler.OnNo += HandleNo;
        AssessmentInputHandler.OnRepeat += HandleRepeat;
    }

    private void OnDisable()
    {
        AssessmentInputHandler.OnYes -= HandleYes;
        AssessmentInputHandler.OnNo -= HandleNo;
        AssessmentInputHandler.OnRepeat -= HandleRepeat;
    }

    private void Start()
    {
        if (autoStartOnEnable)
        {
            StartAssessment();
        }
    }

    public void StartAssessment()
    {
        isFinished = false;
        resultText.text = "";
        LoadNode(AssessmentNode.Q1_DotPositions);
    }

    private void LoadNode(AssessmentNode node)
    {
        currentNode = node;

        if (logFlow)
            Debug.Log("AssessmentFlow: Loading node -> " + node);

        switch (node)
        {
            case AssessmentNode.Q1_DotPositions:
                ShowQuestion(Q1_TEXT);
                PlayQuestionAudio(q1Audio);
                break;

            case AssessmentNode.Q2_Alphabet:
                ShowQuestion(Q2_TEXT);
                PlayQuestionAudio(q2Audio);
                break;

            case AssessmentNode.Q3_NumberSign:
                ShowQuestion(Q3_TEXT);
                PlayQuestionAudio(q3Audio);
                break;

            case AssessmentNode.Q4_OneCellContractions:
                ShowQuestion(Q4_TEXT);
                PlayQuestionAudio(q4Audio);
                break;

            case AssessmentNode.Q5_PunctuationAndCapital:
                ShowQuestion(Q5_TEXT);
                PlayQuestionAudio(q5Audio);
                break;

            case AssessmentNode.Q6_FinalPunctuationAndCapital:
                ShowQuestion(Q6_TEXT);
                PlayQuestionAudio(q6Audio);
                break;
        }
    }

    private void ShowQuestion(string question)
    {
        currentFullText = question;

        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        typewriterRoutine = StartCoroutine(TypeText(question));
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        assessmentQuestionText.text = "";

        foreach (char c in fullText)
        {
            assessmentQuestionText.text += c;

            if (playTickPerCharacter && sfxSource != null && typewriterTickSfx != null && !char.IsWhiteSpace(c))
            {
                sfxSource.PlayOneShot(typewriterTickSfx);
            }

            yield return new WaitForSeconds(characterDelay);
        }

        isTyping = false;
    }

    private void FinishTypewriterImmediately()
    {
        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        assessmentQuestionText.text = currentFullText;
        isTyping = false;
    }

    private void PlayQuestionAudio(AudioClip clip)
    {
        if (voiceSource == null)
            return;

        voiceSource.Stop();

        if (clip != null)
        {
            voiceSource.clip = clip;
            voiceSource.Play();
        }
    }

    private void PlayValidPressSfx()
    {
        if (sfxSource != null && validButtonPressSfx != null)
        {
            sfxSource.PlayOneShot(validButtonPressSfx);
        }
    }

    private void HandleRepeat()
    {
        if (isFinished)
            return;

        if (logFlow)
            Debug.Log("AssessmentFlow: Repeat question");

        LoadNode(currentNode);
    }

    private void HandleYes()
    {
        if (isFinished)
            return;

        PlayValidPressSfx();

        if (isTyping)
            FinishTypewriterImmediately();

        switch (currentNode)
        {
            case AssessmentNode.Q1_DotPositions:
                LoadNode(AssessmentNode.Q2_Alphabet);
                break;

            case AssessmentNode.Q2_Alphabet:
                LoadNode(AssessmentNode.Q3_NumberSign);
                break;

            case AssessmentNode.Q3_NumberSign:
                LoadNode(AssessmentNode.Q4_OneCellContractions);
                break;

            case AssessmentNode.Q4_OneCellContractions:
                LoadNode(AssessmentNode.Q5_PunctuationAndCapital);
                break;

            case AssessmentNode.Q5_PunctuationAndCapital:
                LoadNode(AssessmentNode.Q6_FinalPunctuationAndCapital);
                break;

            case AssessmentNode.Q6_FinalPunctuationAndCapital:
                EndAssessment(AssessmentLevel.Advance);
                break;
        }
    }

    private void HandleNo()
    {
        if (isFinished)
            return;

        PlayValidPressSfx();

        if (isTyping)
            FinishTypewriterImmediately();

        switch (currentNode)
        {
            case AssessmentNode.Q1_DotPositions:
                EndAssessment(AssessmentLevel.Beginner);
                break;

            case AssessmentNode.Q2_Alphabet:
                EndAssessment(AssessmentLevel.Beginner);
                break;

            case AssessmentNode.Q3_NumberSign:
                EndAssessment(AssessmentLevel.Beginner);
                break;

            case AssessmentNode.Q4_OneCellContractions:
                EndAssessment(AssessmentLevel.Intermediate);
                break;

            case AssessmentNode.Q5_PunctuationAndCapital:
                EndAssessment(AssessmentLevel.Intermediate);
                break;

            case AssessmentNode.Q6_FinalPunctuationAndCapital:
                EndAssessment(AssessmentLevel.Intermediate);
                break;
        }
    }
  private IEnumerator UpdateAssessment(AssessmentLevel level)
{
    if (string.IsNullOrEmpty(currentUsername))
    {
        Debug.LogError("Username is NULL or EMPTY. Skipping DB update.");
        yield break; // stop safely
    }

    WWWForm form = new WWWForm();
    form.AddField("username", currentUsername);
    form.AddField("assessment", level.ToString());

    using (UnityWebRequest www = UnityWebRequest.Post(updateAssessmentURL, form))
    {
        yield return www.SendWebRequest();
    }
}

    private void EndAssessment(AssessmentLevel level)
    {
        isFinished = true;
        currentNode = AssessmentNode.Finished;

        string finalText = "Assessment Result: " + level;
        currentFullText = finalText;

        if (typewriterRoutine != null)
            StopCoroutine(typewriterRoutine);

        assessmentQuestionText.text = finalText;
        resultText.text = finalText;

        if (voiceSource != null)
        {
            voiceSource.Stop();

            AudioClip resultClip = null;

            switch (level)
            {
                case AssessmentLevel.Beginner:
                    resultClip = beginnerAudio;
                    break;
                case AssessmentLevel.Intermediate:
                    resultClip = intermediateAudio;
                    break;
                case AssessmentLevel.Advance:
                    resultClip = advanceAudio;
                    break;
            }

            if (resultClip != null)
            {
                voiceSource.clip = resultClip;
                voiceSource.Play();
            }
        }

        if (logFlow)
            Debug.Log("AssessmentFlow: Finished with result -> " + level);

        if (loadSceneAfterResult)
        {
            StartCoroutine(UpdateAssessment(level));
            StartCoroutine(LoadSceneAfterDelay(level));
        }
    }

    private IEnumerator LoadSceneAfterDelay(AssessmentLevel level)
    {
        yield return new WaitForSeconds(sceneLoadDelay);

        string sceneToLoad = "";

        switch (level)
        {
            case AssessmentLevel.Beginner:
                SceneManager.LoadScene("BeginnerScene");
                break;
            case AssessmentLevel.Intermediate:
                SceneManager.LoadScene("IntermediateScene");
                break;
            case AssessmentLevel.Advance:
                SceneManager.LoadScene("AdvanceScene");
                break;
        }
    }

    public void PressYesFromUI()
    {
        HandleYes();
    }

    public void PressNoFromUI()
    {
        HandleNo();
    }

    public void PressRepeatFromUI()
    {
        HandleRepeat();
    }
}