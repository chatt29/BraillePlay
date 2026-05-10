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

    [Header("Audio Source")]
    public AudioSource voiceSource;

    [Header("Button Click Sound")]
    public AudioSource clickSource;
    public AudioClip clickClip;

    [Header("Number Audio (10–1)")]
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

    private List<string> numbers;
    private Dictionary<string, string> brailleMap;

    private int currentIndex = 0;
    private string currentNumber;

    private int correctScore = 0;
    private int wrongScore = 0;
    private int totalScore = 0;

    private bool waitingForHashtag = true;

    private string lastPlayedKey = "";
    private bool canInput = false;
    private bool gameFinished = false;

    void Start()
    {
        InitMaps();
        StartGame();
    }

    void InitMaps()
    {
        // 🔥 CHANGED: 10 → 1 order
        numbers = new List<string>()
        {
            "10","9","8","7","6",
            "5","4","3","2","1"
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
    }

    void PlayClickSound()
    {
        if (clickSource != null && clickClip != null)
            clickSource.PlayOneShot(clickClip);
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
            canInput = true;
    }

    void ShowNumber()
    {
        currentNumber = numbers[currentIndex];
        waitingForHashtag = true;

        int i = currentIndex;

        numberText.text = currentNumber;
        resultText.text = "";
        resultIcon.gameObject.SetActive(false);

        // 🔥 UPDATED START MESSAGE
        if (currentIndex == 0)
            speechBubbleText.text = "Welcome to Number Flow. Now Type number 10";
        else
            speechBubbleText.text = "Type Number " + currentNumber;

        PlayClip(speechBubbleClips[i], "speech_" + currentNumber);
    }

    public void CheckAnswer(string inputPattern)
    {
        PlayClickSound();

        if (!canInput || gameFinished)
            return;

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

            waitingForHashtag = false;
            return;
        }

        // =========================
        // NUMBER PHASE
        // =========================
        totalScore++;

        if (inputPattern == correctPattern)
        {
            correctScore++;

            resultText.text = "CORRECT";
            speechBubbleText.text = "Good Now Type Number " + currentNumber;

            PlayClip(correctClip, "correct_" + currentNumber);
            ShowCorrect();
        }
        else
        {
            wrongScore++;

            resultText.text = "Wrong Input";
            speechBubbleText.text = "Wrong Input";

            PlayClip(wrongInputClip, "wrong_" + currentNumber);
            ShowWrong();
        }

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
            speechBubbleText.text = "You finished 10 to 1!";

            PlayClip(doneClip, "done");

            yield return new WaitForSeconds(1.5f);

            PlayClip(finishedClip, "finished");
            yield break;
        }

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