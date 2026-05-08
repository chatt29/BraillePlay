using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumberFlow1_10Script : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI speechBubbleText;

    [Header("Score UI")]
    public TextMeshProUGUI correctScoreText;
    public TextMeshProUGUI wrongScoreText;
    public TextMeshProUGUI totalScoreText;

    [Header("Result Icon")]
    public Image resultIcon;
    public Sprite correctSprite;
    public Sprite wrongSprite;

    [Header("Audio")]
    public AudioSource voiceSource;

    [Header("Intro / Instruction Messages")]
    public List<MessageData> introMessages = new List<MessageData>();

    [Header("Number Audio 1-10")]
    public AudioClip oneClip, twoClip, threeClip, fourClip, fiveClip;
    public AudioClip sixClip, sevenClip, eightClip, nineClip, tenClip;

    private List<string> numbers;
    private Dictionary<string, string> brailleMap;
    private Dictionary<string, AudioClip> audioMap;

    private int currentIndex = 0;
    private string currentNumber;

    private int correctScore = 0;
    private int wrongScore = 0;
    private int totalScore = 0;

    private int introIndex = 0;
    private bool showingIntro = true;

    private void Update()
    {
        if (showingIntro && Input.GetKeyDown(KeyCode.Y))
        {
            NextIntroMessage();
        }
    }

    private void Start()
    {
        InitMaps();

        if (introMessages.Count > 0)
        {
            showingIntro = true;
            ShowIntroMessage();
        }
        else
        {
            showingIntro = false;
            StartGame();
        }
    }

    void InitMaps()
    {
        numbers = new List<string>()
        {
            "1","2","3","4","5",
            "6","7","8","9","10"
        };

        // Braille patterns for numbers
        // Numbers use the same pattern as letters A-J
        brailleMap = new Dictionary<string, string>()
        {
            {"1","100000"},
            {"2","110000"},
            {"3","100100"},
            {"4","100110"},
            {"5","100010"},
            {"6","110100"},
            {"7","110110"},
            {"8","110010"},
            {"9","010100"},
            {"10","010110"}
        };

        audioMap = new Dictionary<string, AudioClip>()
        {
            {"1", oneClip},
            {"2", twoClip},
            {"3", threeClip},
            {"4", fourClip},
            {"5", fiveClip},
            {"6", sixClip},
            {"7", sevenClip},
            {"8", eightClip},
            {"9", nineClip},
            {"10", tenClip}
        };
    }

    void ShowIntroMessage()
    {
        if (introIndex >= introMessages.Count)
        {
            showingIntro = false;
            StartGame();
            return;
        }

        if (numberText != null) numberText.text = "";
        if (resultText != null) resultText.text = "";

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        MessageData currentMessage = introMessages[introIndex];

        if (speechBubbleText != null)
            speechBubbleText.text = currentMessage.messageText;

        if (voiceSource != null)
        {
            voiceSource.Stop();

            if (currentMessage.voiceClip != null)
                voiceSource.PlayOneShot(currentMessage.voiceClip);
        }
    }

    public void NextIntroMessage()
    {
        if (!showingIntro) return;

        introIndex++;
        ShowIntroMessage();
    }

    public bool IsShowingIntro()
    {
        return showingIntro;
    }

    void StartGame()
    {
        currentIndex = 0;
        correctScore = 0;
        wrongScore = 0;
        totalScore = 0;

        UpdateScoreUI();

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        ShowNumber();
    }

    void ShowNumber()
    {
        currentNumber = numbers[currentIndex];

        numberText.text = currentNumber;
        resultText.text = "";
        speechBubbleText.text = "Number " + currentNumber;

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        if (audioMap[currentNumber] != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.PlayOneShot(audioMap[currentNumber]);
        }
    }

    public void CheckAnswer(string inputPattern)
    {
        if (showingIntro) return;

        string correctPattern = brailleMap[currentNumber];

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(true);

        if (inputPattern == correctPattern)
        {
            correctScore++;
            totalScore++;

            resultText.text = "CORRECT";
            speechBubbleText.text = "You are correct!";

            if (resultIcon != null && correctSprite != null)
                resultIcon.sprite = correctSprite;

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayCorrectSfx();
        }
        else
        {
            wrongScore++;

            resultText.text = "WRONG";
            speechBubbleText.text = "You are wrong!";

            if (resultIcon != null && wrongSprite != null)
                resultIcon.sprite = wrongSprite;

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayWrongSfx();
        }

        UpdateScoreUI();
        StartCoroutine(NextAfterDelay());
    }

    void UpdateScoreUI()
    {
        correctScoreText.text = "CORRECT: " + correctScore;
        wrongScoreText.text = "WRONG: " + wrongScore;
        totalScoreText.text = "TOTAL: " + totalScore;
    }

    IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        currentIndex++;

        if (currentIndex >= numbers.Count)
        {
            resultText.text = "DONE";
            speechBubbleText.text = "Great job! You finished 1 to 10!";

            if (resultIcon != null)
                resultIcon.gameObject.SetActive(false);

            yield break;
        }

        ShowNumber();
    }
}