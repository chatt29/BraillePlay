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

    [Header("Number Audio (1–10)")]
    public AudioClip oneClip, twoClip, threeClip, fourClip, fiveClip;
    public AudioClip sixClip, sevenClip, eightClip, nineClip, tenClip;

    [Header("Message Audio (10 each)")]
    public AudioClip[] wrongHashtagClips = new AudioClip[10];
    public AudioClip[] typeNumberClips = new AudioClip[10];
    public AudioClip[] speechBubbleClips = new AudioClip[10];
    public AudioClip[] goodNowTypeNumberClips = new AudioClip[10];

    [Header("Result Audio")]
    public AudioClip correctClip;
    public AudioClip wrongInputClip;
    public AudioClip doneClip;
    public AudioClip finishedClip;

    [Header("Warning Audio")]
    public AudioClip pressBrailleDotsClip;

    private List<string> numbers;
    private Dictionary<string, string> brailleMap;
    private Dictionary<string, AudioClip> numberAudioMap;

    private int currentIndex = 0;
    private string currentNumber;

    private int correctScore = 0;
    private int wrongScore = 0;
    private int totalScore = 0;

    private bool waitingForHashtag = true;

    // Prevent repeat audio
    private string lastPlayedKey = "";

    // Prevent input while audio is playing
    private bool canInput = false;

    // Prevent input after finish
    private bool gameFinished = false;

    // Detect if braille keys were pressed
    private bool brailleKeysPressed = false;

    void Start()
    {
        InitMaps();
        StartGame();
    }

    void Update()
    {
        // Detect F D S J K L keys
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

        numberAudioMap = new Dictionary<string, AudioClip>()
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

    void StartGame()
    {
        currentIndex = 0;
        correctScore = 0;
        wrongScore = 0;
        totalScore = 0;

        gameFinished = false;

        UpdateScoreUI();
        ShowNumber();
    }

    // Button click sound
    void PlayClickSound()
    {
        if (clickSource != null && clickClip != null)
        {
            clickSource.PlayOneShot(clickClip);
        }
    }

    void PlayClip(AudioClip clip, string key)
    {
        if (clip == null)
        {
            canInput = true;
            return;
        }

        if (lastPlayedKey == key) return;

        StartCoroutine(PlayRoutine(clip, key));
    }

    IEnumerator PlayRoutine(AudioClip clip, string key)
    {
        canInput = false;

        lastPlayedKey = key;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();

        yield return new WaitForSeconds(clip.length);

        if (!gameFinished)
        {
            canInput = true;
        }
    }

    void ShowNumber()
    {
        currentNumber = numbers[currentIndex];
        waitingForHashtag = true;

        int i = currentIndex;

        numberText.text = currentNumber;
        resultText.text = "";
        resultIcon.gameObject.SetActive(false);

        // Reset braille key detection
        brailleKeysPressed = false;

        if (currentIndex == 0)
        {
            speechBubbleText.text = "Welcome to Number Flow. Now Type number 1";
        }
        else
        {
            speechBubbleText.text = "Type Number " + currentNumber;
        }

        // Speech audio
        PlayClip(speechBubbleClips[i], "speech_" + currentNumber);
    }

    public void CheckAnswer(string inputPattern)
    {
        // Play click sound
        PlayClickSound();

        // Cannot enter while audio is playing
        if (!canInput)
            return;

        // Cannot enter after game finished
        if (gameFinished)
            return;

        // Prevent enter if no braille keys pressed
        if (!brailleKeysPressed)
        {
            resultText.text = "";
            speechBubbleText.text = "Press Braille dots first before Enter";

            // Play warning audio
            PlayClip(pressBrailleDotsClip, "press_braille_first");

            return;
        }

        string hashtagPattern = "001111";
        string correctPattern = brailleMap[currentNumber];

        int i = currentIndex;

        // =========================
        // HASHTAG PHASE
        // =========================
        if (waitingForHashtag)
        {
            if (inputPattern != hashtagPattern)
            {
                speechBubbleText.text =
                    "Don't forget hashtag first (dot 3,4,5,6). Now Type Number " + currentNumber;

                PlayClip(wrongHashtagClips[i], "hashtag_" + currentNumber);
            }
            else
            {
                speechBubbleText.text = "Type Number " + currentNumber;

                PlayClip(goodNowTypeNumberClips[i], "good_" + currentNumber);
            }

            // Reset key detection
            brailleKeysPressed = false;

            waitingForHashtag = false;
            return;
        }

        // =========================
        // NUMBER PHASE ONLY COUNTS
        // =========================
        totalScore++;

        if (inputPattern == correctPattern)
        {
            correctScore++;

            resultText.text = "CORRECT";
            speechBubbleText.text = "Good Job";

            // Correct audio
            PlayClip(correctClip, "correct_" + currentNumber);

            ShowCorrect();
        }
        else
        {
            wrongScore++;

            resultText.text = "Wrong Input";
            speechBubbleText.text = "Wrong Input";

            // Wrong audio
            PlayClip(wrongInputClip, "wrong_" + currentNumber);

            ShowWrong();
        }

        // Reset key detection
        brailleKeysPressed = false;

        UpdateScoreUI();
        NextNumber();
    }

    void NextNumber()
    {
        StartCoroutine(DelayNext());
    }

    IEnumerator DelayNext()
    {
        yield return new WaitForSeconds(1.2f);

        currentIndex++;

        if (currentIndex >= numbers.Count)
        {
            gameFinished = true;

            canInput = false;

            resultText.text = "DONE";
            speechBubbleText.text = "You finished 1 to 10!";

            // Done audio
            PlayClip(doneClip, "done");

            yield return new WaitForSeconds(1.5f);

            // Finished audio
            PlayClip(finishedClip, "finished");

            yield break;
        }

        // Reset key for next number
        lastPlayedKey = "";

        ShowNumber();
    }

    void ShowCorrect()
    {
        resultIcon.gameObject.SetActive(true);
        resultIcon.sprite = correctSprite;
    }

    void ShowWrong()
    {
        resultIcon.gameObject.SetActive(true);
        resultIcon.sprite = wrongSprite;
    }

    void UpdateScoreUI()
    {
        correctScoreText.text = "CORRECT: " + correctScore;
        wrongScoreText.text = "WRONG: " + wrongScore;
        totalScoreText.text = "TOTAL: " + totalScore;
    }
}