using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbcFlowA : MonoBehaviour
{
    [System.Serializable]
    public class InstructionLine
    {
        [TextArea]
        public string message;
        public AudioClip audioClip;
    }

    [System.Serializable]
    public class LetterAudio
    {
        public string letter;
        public AudioClip clip;
    }

    [Header("Input")]
    public AbcFlowInput input;

    [Header("UI")]
    public TMP_Text speechBubbleText;
    public TMP_Text letterBoxText;
    public TMP_Text remainingPointsText;
    public TMP_Text deductionsText;
    public TMP_Text totalText;
    public TMP_Text resultText;

    [Header("Feedback Image")]
    public Image feedbackImagePlaceholder;
    public Sprite correctImage;
    public Sprite wrongImage;
    public float feedbackFlashSeconds = 1.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public InstructionLine[] instructions;

    [Header("Ending Messages")]
    public InstructionLine endingMessage1;
    public InstructionLine endingMessage2;
    public InstructionLine endingMessage3;
    public InstructionLine restartPromptMessage;

    [Header("Letter Audio A-Z")]
    public LetterAudio[] letterAudios = new LetterAudio[26];

    [Header("Feedback Audio")]
    public AudioClip correctClip;
    public AudioClip wrongClip;

    [Header("Score")]
    public int startingScore = 100;
    public int mistakesBeforeDeduction = 3;

    [Header("Wireless Haptics")]
    public GameObject wirelessHapticsObject;

    private int currentLetterIndex;
    private int score;
    private int mistakes;
    private int deductions;

    private bool acceptingInput;
    private bool isProcessingAnswer;

    private readonly string[] letters =
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
    };

    private readonly string[] brailleAnswers =
    {
        "100000","110000","100100","100110","100010",
        "110100","110110","110010","010100","010110",
        "101000","111000","101100","101110","101010",
        "111100","111110","111010","011100","011110",
        "101001","111001","010111","101101","101111","101011"
    };

    private void Start()
    {
        currentLetterIndex = 0;
        score = startingScore;
        mistakes = 0;
        deductions = 0;

        HideFeedbackImage();

        if (input != null)
        {
            input.SetInputEnabled(false);
            input.OnAnswerSubmitted += CheckAnswer;
        }

        UpdateScoreUI();
        StartCoroutine(StartSceneFlow());
    }

    private void Update()
    {
        HandleSpeedKeys();
    }

    private IEnumerator StartSceneFlow()
    {
        acceptingInput = false;
        isProcessingAnswer = false;

        if (introClip != null)
            yield return PlayMessage("ABC Flow Quiz", introClip);

        foreach (InstructionLine line in instructions)
        {
            if (line != null)
                yield return PlayMessage(line.message, line.audioClip);
        }

        ShowCurrentLetter();
    }

    private IEnumerator PlayMessage(string message, AudioClip clip)
    {
        if (speechBubbleText != null)
            speechBubbleText.text = message;

        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }
    }

    private void ShowCurrentLetter()
    {
        acceptingInput = true;
        isProcessingAnswer = false;

        if (input != null)
            input.SetInputEnabled(true);

        HideFeedbackImage();

        string currentLetter = letters[currentLetterIndex];

        if (letterBoxText != null)
            letterBoxText.text = currentLetter;

        if (speechBubbleText != null)
            speechBubbleText.text = "Letter " + currentLetter;

        if (resultText != null)
            resultText.text = "";

        PlayLetterAudio(currentLetter);
    }

    private void CheckAnswer(string pattern)
    {
        if (!acceptingInput || isProcessingAnswer)
            return;

        acceptingInput = false;
        isProcessingAnswer = true;

        if (input != null)
            input.SetInputEnabled(false);

        if (pattern == brailleAnswers[currentLetterIndex])
            StartCoroutine(CorrectFlow());
        else
            StartCoroutine(WrongFlow());
    }

    private IEnumerator CorrectFlow()
    {
        if (speechBubbleText != null)
            speechBubbleText.text = "That is correct!";

        if (resultText != null)
            resultText.text = "CORRECT";

        ShowFeedbackImage(correctImage);

        if (audioSource != null && correctClip != null)
        {
            audioSource.clip = correctClip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(feedbackFlashSeconds);
        }

        currentLetterIndex++;

        if (currentLetterIndex >= letters.Length)
        {
            FinishQuiz();
        }
        else
        {
            ShowCurrentLetter();
        }
    }

    private IEnumerator WrongFlow()
    {
        mistakes++;

        if (mistakes % mistakesBeforeDeduction == 0)
        {
            deductions++;
            score = Mathf.Max(0, score - 1);
        }

        UpdateScoreUI();
        TriggerHaptic();

        if (speechBubbleText != null)
            speechBubbleText.text = "That is wrong.";

        if (resultText != null)
            resultText.text = "WRONG";

        ShowFeedbackImage(wrongImage);

        if (audioSource != null && wrongClip != null)
        {
            audioSource.clip = wrongClip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(feedbackFlashSeconds);
        }

        currentLetterIndex++;

        if (currentLetterIndex >= letters.Length)
        {
            FinishQuiz();
        }
        else
        {
            ShowCurrentLetter();
        }
    }

    private void FinishQuiz()
    {
        acceptingInput = false;

        if (input != null)
            input.SetInputEnabled(false);

        StartCoroutine(FinishQuizFlow());
    }

    private IEnumerator FinishQuizFlow()
    {
        if (letterBoxText != null)
            letterBoxText.text = "";

        if (resultText != null)
            resultText.text = "DONE";

        HideFeedbackImage();

        if (endingMessage1 != null)
            yield return PlayMessage(endingMessage1.message, endingMessage1.audioClip);

        if (endingMessage2 != null)
            yield return PlayMessage(endingMessage2.message, endingMessage2.audioClip);

        if (endingMessage3 != null)
            yield return PlayMessage(endingMessage3.message, endingMessage3.audioClip);

        if (restartPromptMessage != null)
            yield return PlayMessage(restartPromptMessage.message, restartPromptMessage.audioClip);
    }

    private void UpdateScoreUI()
    {
        if (remainingPointsText != null)
            remainingPointsText.text = score.ToString();

        if (deductionsText != null)
            deductionsText.text = deductions.ToString();

        if (totalText != null)
            totalText.text = score.ToString();
    }

    private void PlayLetterAudio(string letter)
    {
        if (audioSource == null)
            return;

        foreach (LetterAudio item in letterAudios)
        {
            if (item != null &&
                item.letter.ToUpper() == letter &&
                item.clip != null)
            {
                audioSource.clip = item.clip;
                audioSource.Play();
                return;
            }
        }
    }

    private void HandleSpeedKeys()
    {
        if (audioSource == null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha7)) audioSource.pitch = 1.0f;
        if (Input.GetKeyDown(KeyCode.Alpha8)) audioSource.pitch = 1.25f;
        if (Input.GetKeyDown(KeyCode.Alpha9)) audioSource.pitch = 1.5f;
        if (Input.GetKeyDown(KeyCode.Alpha0)) audioSource.pitch = 1.75f;
        if (Input.GetKeyDown(KeyCode.Minus)) audioSource.pitch = 2.0f;
    }

    private void ShowFeedbackImage(Sprite sprite)
    {
        if (feedbackImagePlaceholder != null)
        {
            feedbackImagePlaceholder.sprite = sprite;
            feedbackImagePlaceholder.enabled = sprite != null;
        }
    }

    private void HideFeedbackImage()
    {
        if (feedbackImagePlaceholder != null)
        {
            feedbackImagePlaceholder.sprite = null;
            feedbackImagePlaceholder.enabled = false;
        }
    }

    private void TriggerHaptic()
    {
        if (wirelessHapticsObject != null)
        {
            wirelessHapticsObject.SendMessage(
                "TriggerHaptic",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void OnDestroy()
    {
        if (input != null)
            input.OnAnswerSubmitted -= CheckAnswer;
    }
}