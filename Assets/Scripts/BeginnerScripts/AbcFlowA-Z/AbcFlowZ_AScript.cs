using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbcFlowZ_AScript : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI letterText;
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

    [Header("Letter Audio A-Z")]
    public AudioClip aClip, bClip, cClip, dClip, eClip, fClip, gClip, hClip, iClip, jClip, kClip, lClip, mClip;
    public AudioClip nClip, oClip, pClip, qClip, rClip, sClip, tClip, uClip, vClip, wClip, xClip, yClip, zClip;

    private List<string> letters;
    private Dictionary<string, string> brailleMap;
    private Dictionary<string, AudioClip> audioMap;

    private int currentIndex = 0;
    private string currentLetter;

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
        letters = new List<string>()
        {
            "Z","Y","X","W","V","U","T","S","R","Q",
            "P","O","N","M","L","K","J","I","H","G",
            "F","E","D","C","B","A"
        };

        brailleMap = new Dictionary<string, string>()
        {
            {"A","100000"}, {"B","110000"}, {"C","100100"}, {"D","100110"}, {"E","100010"},
            {"F","110100"}, {"G","110110"}, {"H","110010"}, {"I","010100"}, {"J","010110"},
            {"K","101000"}, {"L","111000"}, {"M","101100"}, {"N","101110"}, {"O","101010"},
            {"P","111100"}, {"Q","111110"}, {"R","111010"}, {"S","011100"}, {"T","011110"},
            {"U","101001"}, {"V","111001"}, {"W","010111"}, {"X","101101"}, {"Y","101111"},
            {"Z","101011"}
        };

        audioMap = new Dictionary<string, AudioClip>()
        {
            {"A", aClip}, {"B", bClip}, {"C", cClip}, {"D", dClip}, {"E", eClip},
            {"F", fClip}, {"G", gClip}, {"H", hClip}, {"I", iClip}, {"J", jClip},
            {"K", kClip}, {"L", lClip}, {"M", mClip}, {"N", nClip}, {"O", oClip},
            {"P", pClip}, {"Q", qClip}, {"R", rClip}, {"S", sClip}, {"T", tClip},
            {"U", uClip}, {"V", vClip}, {"W", wClip}, {"X", xClip}, {"Y", yClip},
            {"Z", zClip}
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

        if (letterText != null) letterText.text = "";
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

        ShowLetter();
    }

    void ShowLetter()
    {
        currentLetter = letters[currentIndex];

        letterText.text = currentLetter;
        resultText.text = "";
        speechBubbleText.text = "Letter " + currentLetter;

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        if (audioMap[currentLetter] != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.PlayOneShot(audioMap[currentLetter]);
        }
    }

    public void CheckAnswer(string inputPattern)
    {
        if (showingIntro) return;

        string correctPattern = brailleMap[currentLetter];

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

        if (currentIndex >= letters.Count)
        {
            resultText.text = "DONE";
            speechBubbleText.text = "Great job! You finished Z to A!";

            if (resultIcon != null)
                resultIcon.gameObject.SetActive(false);

            yield break;
        }

        ShowLetter();
    }
}