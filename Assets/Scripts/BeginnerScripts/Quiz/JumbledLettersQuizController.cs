using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class JumbledLettersController : MonoBehaviour
{
    [Header("Quiz Info")]
    public string quiz_name = "JumbledLetters";

    [Header("Scene")]
    public string beginnerScene = "BeginnerScene";

    [Header("UI")]
    public TextMeshProUGUI letterDisplay;
    public TextMeshProUGUI scoreDisplay;

    [Header("Letter Audio")]
    public LetterAudioLoader audioLoader;

    public AudioSource audioSource;

    public AudioClip afterLetterSound;

    public AudioClip finishClip;

    [Header("Score Audio")]
    public AudioSource scoreAudioSource;

    public AudioClip yourScoreIsClip;

    public AudioClip[] scoreClips;

    private List<char> remainingLetters =
        new List<char>();

    private char currentLetter;

    private bool quizFinished = false;

    private int score = 0;

    char PatternToLetter(string pattern)
    {
        switch (pattern)
        {
            case "100000": return 'A';
            case "110000": return 'B';
            case "100100": return 'C';
            case "100110": return 'D';
            case "100010": return 'E';
            case "110100": return 'F';
            case "110110": return 'G';
            case "110010": return 'H';
            case "010100": return 'I';
            case "010110": return 'J';
            case "101000": return 'K';
            case "111000": return 'L';
            case "101100": return 'M';
            case "101110": return 'N';
            case "101010": return 'O';
            case "111100": return 'P';
            case "111110": return 'Q';
            case "111010": return 'R';
            case "011100": return 'S';
            case "011110": return 'T';
            case "101001": return 'U';
            case "111001": return 'V';
            case "010111": return 'W';
            case "101101": return 'X';
            case "101111": return 'Y';
            case "101011": return 'Z';

            default: return '?';
        }
    }

    void Start()
    {
        InitializeLetters();

        PickNextLetter();

        UpdateScoreUI();
    }

    void InitializeLetters()
    {
        remainingLetters.Clear();

        for (char c = 'A'; c <= 'Z'; c++)
        {
            remainingLetters.Add(c);
        }

        ShuffleLetters();
    }

    void ShuffleLetters()
    {
        for (int i = 0;
            i < remainingLetters.Count;
            i++)
        {
            int randIndex =
                Random.Range(
                    i,
                    remainingLetters.Count
                );

            char temp =
                remainingLetters[i];

            remainingLetters[i] =
                remainingLetters[randIndex];

            remainingLetters[randIndex] =
                temp;
        }
    }

    void PickNextLetter()
    {
        if (quizFinished)
            return;

        // QUIZ FINISHED
        if (remainingLetters == null ||
            remainingLetters.Count <= 0)
        {
            quizFinished = true;

            StartCoroutine(
                FinishQuiz()
            );

            return;
        }

        currentLetter =
            remainingLetters[0];

        remainingLetters.RemoveAt(0);

        if (letterDisplay != null)
        {
            letterDisplay.text =
                currentLetter.ToString();
        }

        StartCoroutine(
            PlayLetterSequence()
        );
    }

    IEnumerator PlayLetterSequence()
    {
        // PLAY LETTER AUDIO
        if (audioLoader != null)
        {
            audioLoader.PlayLetter(
                currentLetter
            );
        }

        // WAIT FOR AUDIO
        if (audioSource != null &&
            audioSource.clip != null)
        {
            yield return new WaitForSeconds(
                audioSource.clip.length
            );
        }

        // SMALL PAUSE
        yield return new WaitForSeconds(
            0.5f
        );

        // PLAY AFTER SOUND
        if (audioSource != null &&
            afterLetterSound != null)
        {
            audioSource.PlayOneShot(
                afterLetterSound
            );
        }
    }

    IEnumerator FinishQuiz()
{
    // SAVE SCORE
    PlayerPrefs.SetInt(
        "FinalScore",
        score
    );

    PlayerPrefs.SetString(
        "PreviousScene",
        SceneManager
            .GetActiveScene()
            .name
    );

    PlayerPrefs.Save();

    // GET USER ID
    int userId =
        PlayerPrefs.GetInt(
            "user_id",
            0
        );

    // SAVE TO DATABASE
    // This runs while audio is playing
    

    // PLAY FINISH SOUND
    if (audioSource != null &&
        finishClip != null)
    {
        audioSource.PlayOneShot(
            finishClip
        );

        yield return new WaitForSeconds(
            finishClip.length
        );
    }

    // SAY "YOUR SCORE IS"
    if (scoreAudioSource != null &&
        yourScoreIsClip != null)
    {
        scoreAudioSource.clip =
            yourScoreIsClip;

        scoreAudioSource.Play();

        yield return new WaitForSeconds(
            yourScoreIsClip.length
        );
    }

    // SAY SCORE NUMBER
    if (scoreAudioSource != null &&
        score >= 0 &&
        score < scoreClips.Length &&
        scoreClips[score] != null)
    {
        scoreAudioSource.clip =
            scoreClips[score];

        scoreAudioSource.Play();

        yield return new WaitForSeconds(
            scoreClips[score].length
        );
    }
    if (userId > 0)
    {
        StartCoroutine(
            PostScore(
                userId,
                score
            )
        );
    }

    // SMALL DELAY
    yield return new WaitForSeconds(
        1f
    );

    // GO TO SCENE
    SceneManager.LoadScene(
        beginnerScene
    );
}

    void HandleBrailleInput(
        string pattern
    )
    {
        char inputLetter =
            PatternToLetter(pattern);

        if (inputLetter ==
            currentLetter)
        {
            score++;
        }

        UpdateScoreUI();

        PickNextLetter();
    }

    void OnEnable()
    {
        BrailleMapping
            .OnBrailleChordSubmitted
            += HandleBrailleInput;

        BrailleMapping
            .OnSubmit
            += NextLetter;
    }

    void OnDisable()
    {
        BrailleMapping
            .OnBrailleChordSubmitted
            -= HandleBrailleInput;

        BrailleMapping
            .OnSubmit
            -= NextLetter;
    }

    void NextLetter()
    {
        PickNextLetter();
    }

    void UpdateScoreUI()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.text =
                "Score: " + score;
        }
    }

    IEnumerator PostScore(
    int userId,
    int finalScore
)
{
    WWWForm form =
        new WWWForm();

    form.AddField(
        "user_id",
        userId
    );

    form.AddField(
        "score",
        finalScore
    );

    // USE CURRENT SCENE NAME
    form.AddField(
        "quiz_name",
        quiz_name
    );

    UnityWebRequest www =
        UnityWebRequest.Post(
            "http://localhost/brailleplay/save_score.php",
            form
        );

    yield return www.SendWebRequest();

    Debug.Log(
        "SERVER RESPONSE: "
        + www.downloadHandler.text
    );

    if (www.result ==
        UnityWebRequest.Result.Success)
    {
        Debug.Log(
            "Score saved successfully!"
        );
    }
    else
    {
        Debug.LogError(
            "Error: "
            + www.error
        );
    }
       }
}
