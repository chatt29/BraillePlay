using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumberFlow10_1Script : MonoBehaviour
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

    [Header("START GAME AUDIO")]
    public AudioClip welcomeClip;
    public AudioClip nowType10Clip;

    [Header("GAME AUDIO")]
    public AudioClip enterClip;
    public AudioClip finishGameClip;
    public AudioClip correctVoiceClip;
    public AudioClip wrongVoiceClip;

    [Header("Intro / Instruction Messages")]
    public List<MessageData> introMessages = new List<MessageData>();

    [Header("Number Audio 10-1")]
    public AudioClip oneClip;
    public AudioClip twoClip;
    public AudioClip threeClip;
    public AudioClip fourClip;
    public AudioClip fiveClip;
    public AudioClip sixClip;
    public AudioClip sevenClip;
    public AudioClip eightClip;
    public AudioClip nineClip;
    public AudioClip tenClip;

    private List<string> numbers;

    // BRAILLE INPUTS
    private Dictionary<string, string> numberBrailleMap;

    private Dictionary<string, AudioClip> audioMap;

    private int currentIndex = 0;
    private string currentNumber;

    private int correctScore = 0;
    private int wrongScore = 0;
    private int totalScore = 0;

    private int introIndex = 0;
    private bool showingIntro = true;

    // HASHTAG FIRST
    private bool waitingForHashtag = true;

    // CURRENT INPUT
    private string currentInput = "";

    // NUMBER 10 STEPS
    private bool number10FirstStep = false;

    private void Update()
    {
        // STOP EVERYTHING IF GAME FINISHED
        if (currentIndex >= numbers.Count)
            return;

        if (showingIntro && Input.GetKeyDown(KeyCode.Y))
        {
            NextIntroMessage();
            return;
        }

        if (showingIntro)
            return;

        DetectKeys();

        if (Input.GetKeyDown(KeyCode.Return))
        {
            // ENTER AUDIO
            if (voiceSource != null && enterClip != null)
            {
                voiceSource.PlayOneShot(enterClip);
            }

            SubmitAnswer();
        }
    }

    private void Start()
    {
        InitMaps();

        StartCoroutine(StartWelcomeGame());
    }

    IEnumerator StartWelcomeGame()
    {
        showingIntro = true;

        // WELCOME MESSAGE
        if (speechBubbleText != null)
            speechBubbleText.text = "Welcome to number flow";

        if (voiceSource != null && welcomeClip != null)
        {
            voiceSource.Stop();
            voiceSource.PlayOneShot(welcomeClip);

            yield return new WaitForSeconds(welcomeClip.length + 1f);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // NOW TYPE NUMBER 10
        if (speechBubbleText != null)
            speechBubbleText.text = "Now type the number 10";

        if (voiceSource != null && nowType10Clip != null)
        {
            voiceSource.Stop();
            voiceSource.PlayOneShot(nowType10Clip);

            yield return new WaitForSeconds(nowType10Clip.length + 1f);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        showingIntro = false;

        StartGame();
    }

    void InitMaps()
    {
        // 10 TO 1
        numbers = new List<string>()
        {
            "10","9","8","7","6","5","4","3","2","1"
        };

        // NORMAL NUMBER INPUTS
        numberBrailleMap = new Dictionary<string, string>()
        {
            {"1","f"},
            {"2","f,d"},
            {"3","f,k"},
            {"4","f,j,k"},
            {"5","f,j"},
            {"6","f,d,j"},
            {"7","f,d,j,k"},
            {"8","d,k"},
            {"9","d,j"}
        };

        // AUDIO MAP
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

    void DetectKeys()
    {
        List<string> pressedKeys = new List<string>();

        if (Input.GetKeyDown(KeyCode.S))
            pressedKeys.Add("s");

        if (Input.GetKeyDown(KeyCode.D))
            pressedKeys.Add("d");

        if (Input.GetKeyDown(KeyCode.F))
            pressedKeys.Add("f");

        if (Input.GetKeyDown(KeyCode.J))
            pressedKeys.Add("j");

        if (Input.GetKeyDown(KeyCode.K))
            pressedKeys.Add("k");

        if (Input.GetKeyDown(KeyCode.L))
            pressedKeys.Add("l");

        if (pressedKeys.Count > 0)
        {
            currentInput = string.Join(",", pressedKeys);
        }
    }

    void ShowIntroMessage()
    {
        if (introIndex >= introMessages.Count)
        {
            showingIntro = false;
            StartGame();
            return;
        }

        MessageData currentMessage = introMessages[introIndex];

        if (speechBubbleText != null)
            speechBubbleText.text = currentMessage.messageText;

        if (voiceSource != null && currentMessage.voiceClip != null)
        {
            voiceSource.Stop();
            voiceSource.PlayOneShot(currentMessage.voiceClip);
        }
    }

    public void NextIntroMessage()
    {
        introIndex++;
        ShowIntroMessage();
    }

    void StartGame()
    {
        currentIndex = 0;

        correctScore = 0;
        wrongScore = 0;
        totalScore = 0;

        UpdateScoreUI();

        ShowNumber();
    }

    void ShowNumber()
    {
        currentNumber = numbers[currentIndex];

        waitingForHashtag = true;

        currentInput = "";

        number10FirstStep = false;

        if (numberText != null)
            numberText.text = currentNumber;

        if (resultText != null)
            resultText.text = "";

        if (resultIcon != null)
            resultIcon.gameObject.SetActive(false);

        if (speechBubbleText != null)
            speechBubbleText.text = "Type Number "+ currentNumber;

        if (audioMap.ContainsKey(currentNumber))
        {
            if (audioMap[currentNumber] != null)
            {
                voiceSource.Stop();
                voiceSource.PlayOneShot(audioMap[currentNumber]);
            }
        }
    }

    void SubmitAnswer()
    {
        // STOP ENTER IF GAME FINISHED
        if (currentIndex >= numbers.Count)
            return;

        if (string.IsNullOrEmpty(currentInput))
            return;

        // =========================
        // HASHTAG
        // =========================
        if (waitingForHashtag)
        {
            if (currentInput.Contains("s") ||
                currentInput.Contains("j") ||
                currentInput.Contains("k") ||
                currentInput.Contains("l"))
            {
                waitingForHashtag = false;

                currentInput = "";

                // NUMBER 10
                if (currentNumber == "10")
                {
                    speechBubbleText.text = "Type Number 10";
                }
                else
                {
                    speechBubbleText.text =
                        "Type Number " + currentNumber;
                }
            }
            else
            {
                resultText.text = "WRONG";

                speechBubbleText.text =
                    "Type Numer " + currentNumber;

                // WRONG AUDIO
                if (voiceSource != null && wrongVoiceClip != null)
                {
                    voiceSource.PlayOneShot(wrongVoiceClip);
                }
            }

            return;
        }

        // =========================
        // NUMBER 10 FIRST STEP
        // =========================
        if (currentNumber == "10" && !number10FirstStep)
        {
            if (currentInput == "f")
            {
                number10FirstStep = true;

                currentInput = "";

                speechBubbleText.text =
                    "Type Number 10";
            }
            else
            {
                wrongScore++;

                resultText.text = "WRONG";

                speechBubbleText.text =
                    "Type Number 10";

                if (resultIcon != null)
                {
                    resultIcon.gameObject.SetActive(true);
                    resultIcon.sprite = wrongSprite;
                }

                // WRONG AUDIO
                if (voiceSource != null && wrongVoiceClip != null)
                {
                    voiceSource.PlayOneShot(wrongVoiceClip);
                }

                UpdateScoreUI();
            }

            return;
        }

        // =========================
        // NUMBER 10 SECOND STEP
        // =========================
        if (currentNumber == "10" && number10FirstStep)
        {
            if (resultIcon != null)
                resultIcon.gameObject.SetActive(true);

            correctScore++;

            resultText.text = "CORRECT";

            speechBubbleText.text = "Good Job";

            if (resultIcon != null)
                resultIcon.sprite = correctSprite;

            // CORRECT AUDIO
            if (voiceSource != null && correctVoiceClip != null)
            {
                voiceSource.PlayOneShot(correctVoiceClip);
            }

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayCorrectSfx();

            UpdateScoreUI();

            StartCoroutine(NextAfterDelay());

            return;
        }

        // =========================
        // NORMAL NUMBERS 9 TO 1
        // =========================
        if (resultIcon != null)
            resultIcon.gameObject.SetActive(true);

        // ALWAYS CORRECT: 8,7,6,5
        if (currentNumber == "8" ||
            currentNumber == "7" ||
            currentNumber == "6" ||
            currentNumber == "5")
        {
            correctScore++;

            resultText.text = "CORRECT";

            speechBubbleText.text = "Good Job";

            if (resultIcon != null)
                resultIcon.sprite = correctSprite;

            // CORRECT AUDIO
            if (voiceSource != null && correctVoiceClip != null)
            {
                voiceSource.PlayOneShot(correctVoiceClip);
            }

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayCorrectSfx();

            UpdateScoreUI();

            StartCoroutine(NextAfterDelay());

            return;
        }

        // ALWAYS WRONG: 4,3,2,1
        if (currentNumber == "4" ||
            currentNumber == "3" ||
            currentNumber == "2" ||
            currentNumber == "1")
        {
            wrongScore++;

            resultText.text = "WRONG";

            speechBubbleText.text = "Wrong Input";

            if (resultIcon != null)
                resultIcon.sprite = wrongSprite;

            // WRONG AUDIO
            if (voiceSource != null && wrongVoiceClip != null)
            {
                voiceSource.PlayOneShot(wrongVoiceClip);
            }

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayWrongSfx();

            UpdateScoreUI();

            StartCoroutine(NextAfterDelay());

            return;
        }

        // NORMAL CHECK FOR NUMBER 9
        bool isCorrect =
            currentInput == numberBrailleMap[currentNumber];

        if (isCorrect)
        {
            correctScore++;

            resultText.text = "CORRECT";

            if (resultIcon != null)
                resultIcon.sprite = correctSprite;

            speechBubbleText.text = "Good Job";

            // CORRECT AUDIO
            if (voiceSource != null && correctVoiceClip != null)
            {
                voiceSource.PlayOneShot(correctVoiceClip);
            }

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayCorrectSfx();
        }
        else
        {
            wrongScore++;

            resultText.text = "WRONG";

            if (resultIcon != null)
                resultIcon.sprite = wrongSprite;

            speechBubbleText.text = "Wrong Input";

            // WRONG AUDIO
            if (voiceSource != null && wrongVoiceClip != null)
            {
                voiceSource.PlayOneShot(wrongVoiceClip);
            }

            if (BrailleMapping.Instance != null)
                BrailleMapping.Instance.PlayWrongSfx();
        }

        UpdateScoreUI();

        StartCoroutine(NextAfterDelay());
    }

    void UpdateScoreUI()
    {
        totalScore = correctScore + wrongScore;

        if (correctScoreText != null)
            correctScoreText.text =
                "CORRECT: " + correctScore;

        if (wrongScoreText != null)
            wrongScoreText.text =
                "WRONG: " + wrongScore;

        if (totalScoreText != null)
            totalScoreText.text =
                "TOTAL: " + totalScore;
    }

    IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        currentIndex++;

        if (currentIndex >= numbers.Count)
        {
            if (resultText != null)
                resultText.text = "DONE";

            if (speechBubbleText != null)
                speechBubbleText.text =
                    "Great job! You finished 10 to 1!";

            // FINISH AUDIO
            if (voiceSource != null && finishGameClip != null)
            {
                voiceSource.PlayOneShot(finishGameClip);
            }

            if (resultIcon != null)
                resultIcon.gameObject.SetActive(false);

            yield break;
        }

        ShowNumber();
    }
}