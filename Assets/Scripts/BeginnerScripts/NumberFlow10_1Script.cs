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

    [Header("Audio Source")]
    public AudioSource voiceSource;

    [Header("Button Click Sound")]
    public AudioSource clickSource;
    public AudioClip clickClip;

    [Header("ENTER SOUND")]
    public AudioClip enterClip;

    [Header("Warning Audio")]
    public AudioClip pressBrailleDotsClip;

    [Header("Hashtag Audio")]
    public AudioClip hashtagWrongClip;     // ❗ NEW
    public AudioClip hashtagCorrectClip;   // ❗ NEW

    [Header("Result Audio")]
    public AudioClip correctClip;
    public AudioClip wrongInputClip;
    public AudioClip doneClip;
    public AudioClip finishedClip;

    [Header("Speech Settings")]
    public float typeSpeed = 0.045f;
    public RectTransform speechBox;

    private Coroutine speechCoroutine;

    private List<string> numbers;
    private Dictionary<string, string> brailleMap;

    private int currentIndex = 0;
    private string currentNumber;

    private int correctScore = 0;
    private int wrongScore = 0;
    private int totalScore = 0;

    private bool waitingForHashtag = true;
    private bool canInput = false;
    private bool gameFinished = false;
    private bool brailleKeysPressed = false;

    void Start()
    {
        InitMaps();
        StartGame();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (enterClip != null)
                voiceSource.PlayOneShot(enterClip);
        }

        if (
            Input.GetKeyDown(KeyCode.F) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.J) ||
            Input.GetKeyDown(KeyCode.K) ||
            Input.GetKeyDown(KeyCode.L)
        )
        {
            brailleKeysPressed = true;
            PlayClickSound();
        }
    }

    void InitMaps()
    {
        numbers = new List<string>()
        {
            "1","2","3","4","5",
            "6","7","8","9","10"
        };

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
    }

    void StartGame()
    {
        currentIndex = 0;
        correctScore = 0;
        wrongScore = 0;
        totalScore = 0;

        gameFinished = false;

        UpdateScoreUI();
        ShowNumber();

        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        float oldSpeed = typeSpeed;
        typeSpeed = 0.08f;

        PlaySpeech("Congratulations! You have successfully completed the Letter to Braille quiz flow.");
        yield return new WaitForSeconds(6f);

        PlaySpeech("Now, you will move on to the Number Flow. This activity helps you practice Braille using number sequences.");
        yield return new WaitForSeconds(11f);

        PlaySpeech("making learning both engaging and meaningful as you explore and master each step of the flow.");
        yield return new WaitForSeconds(6f);

        PlaySpeech("Now start by typing the number 1.");
        yield return new WaitForSeconds(5f);

        typeSpeed = oldSpeed;
    }

    void PlaySpeech(string message)
    {
        if (speechCoroutine != null)
            StopCoroutine(speechCoroutine);

        speechCoroutine = StartCoroutine(TypeSpeech(message));
    }

    IEnumerator TypeSpeech(string message)
    {
        speechBubbleText.text = "";

        foreach (char c in message)
        {
            speechBubbleText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    void ShowNumber()
    {
        currentNumber = numbers[currentIndex];
        waitingForHashtag = true;
        canInput = true;

        numberText.text = currentNumber;
        resultText.text = "";
        resultIcon.gameObject.SetActive(false);

        brailleKeysPressed = false;

        PlaySpeech("Type the number " + currentNumber);
    }

    void PlayClickSound()
    {
        if (clickSource != null && clickClip != null)
            clickSource.PlayOneShot(clickClip);
    }

    public void CheckAnswer(string inputPattern)
    {
        if (!canInput || gameFinished)
            return;

        if (!brailleKeysPressed)
        {
            PlaySpeech("Press Braille dots first before Enter");

            if (pressBrailleDotsClip != null)
                voiceSource.PlayOneShot(pressBrailleDotsClip);

            return;
        }

        string correctPattern = brailleMap[currentNumber];

        // ================= HASHTAG CHECK =================
        if (waitingForHashtag)
        {
            if (inputPattern != "001111")
            {
                PlaySpeech("You must type the hashtag first before entering the number " + currentNumber);

                if (hashtagWrongClip != null)
                    voiceSource.PlayOneShot(hashtagWrongClip);

                StartCoroutine(ContinueToNumberPrompt());
                return;
            }

            waitingForHashtag = false;
            brailleKeysPressed = false;

            PlaySpeech("Hashtag correct. Now type number " + currentNumber);

            if (hashtagCorrectClip != null)
                voiceSource.PlayOneShot(hashtagCorrectClip);

            return;
        }

        // ================= NUMBER CHECK =================
        totalScore++;

        if (inputPattern == correctPattern)
        {
            correctScore++;
            resultText.text = "CORRECT";

            if (correctClip != null)
                voiceSource.PlayOneShot(correctClip);

            PlaySpeech("Good Job");

            resultIcon.gameObject.SetActive(true);
            resultIcon.sprite = correctSprite;
        }
        else
        {
            wrongScore++;
            resultText.text = "WRONG";

            if (wrongInputClip != null)
                voiceSource.PlayOneShot(wrongInputClip);

            PlaySpeech("Wrong Input");

            resultIcon.gameObject.SetActive(true);
            resultIcon.sprite = wrongSprite;
        }

        brailleKeysPressed = false;

        UpdateScoreUI();
        StartCoroutine(NextNumber());
    }

    IEnumerator ContinueToNumberPrompt()
    {
        yield return new WaitForSeconds(2f);
        PlaySpeech("Now type the hashtag first ");
    }

    IEnumerator NextNumber()
    {
        yield return new WaitForSeconds(1.2f);

        currentIndex++;

        if (currentIndex >= numbers.Count)
        {
            gameFinished = true;
            canInput = false;

            resultText.text = "DONE";

            PlaySpeech("You finished 1 to 10!");

            yield return new WaitForSeconds(1.5f);

            if (finishedClip != null)
                voiceSource.PlayOneShot(finishedClip);

            yield break;
        }

        ShowNumber();
    }

    void UpdateScoreUI()
    {
        correctScoreText.text = "CORRECT: " + correctScore;
        wrongScoreText.text = "WRONG: " + wrongScore;
        totalScoreText.text = "TOTAL: " + totalScore;
    }
}