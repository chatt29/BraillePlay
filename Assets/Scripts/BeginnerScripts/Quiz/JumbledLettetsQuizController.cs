using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
public class JumbledLettersController : MonoBehaviour
{
    [Header("Quiz Info")]
public string quizType = "Beginner"; // or set dynamically
    [Header("Scene")]
public string quizSelectionScene = "BeginnerQuizSelection";
    [Header("UI")]
    public TextMeshProUGUI letterDisplay;
    public TextMeshProUGUI scoreDisplay;

    private List<char> remainingLetters = new List<char>();
    private char currentLetter;

    private int score = 0;
    private bool hasAnswered = false;
    public LetterAudioLoader audioLoader;
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
        for (int i = 0; i < remainingLetters.Count; i++)
        {
            int randIndex = Random.Range(i, remainingLetters.Count);
            char temp = remainingLetters[i];
            remainingLetters[i] = remainingLetters[randIndex];
            remainingLetters[randIndex] = temp;
        }
    }

  void PickNextLetter()
{
    if (remainingLetters == null || remainingLetters.Count <= 0)
{
    SendScoreToDatabase(1, score);

    StartCoroutine(FinishQuiz());
    return;
}

    currentLetter = remainingLetters[0];
    remainingLetters.RemoveAt(0);

    letterDisplay.text = currentLetter.ToString();

    if (audioLoader != null)
    {
        audioLoader.PlayLetter(currentLetter);
    }
}
IEnumerator FinishQuiz()
{
    yield return StartCoroutine(PostScore(1, score));
    SceneManager.LoadScene(quizSelectionScene);
}
   

   void HandleBrailleInput(string pattern)
{
    char inputLetter = PatternToLetter(pattern);

    if (inputLetter == currentLetter)
    {
        score++;
    }

    UpdateScoreUI();
    PickNextLetter(); // 🔥 no Enter needed
}
void OnEnable()
{
    BrailleMapping.OnBrailleChordSubmitted += HandleBrailleInput;
    BrailleMapping.OnSubmit += NextLetter;
}

void OnDisable()
{
    BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleInput;
    BrailleMapping.OnSubmit -= NextLetter;
}

void NextLetter()
{
    PickNextLetter();
}
    void UpdateScoreUI()
    {
        scoreDisplay.text = "Score: " + score;
    }

    public void SendScoreToDatabase(int userId, int finalScore)
    {
        StartCoroutine(PostScore(userId, finalScore));
    }

    IEnumerator PostScore(int userId, int finalScore)
{
    WWWForm form = new WWWForm();
    form.AddField("user_id", userId);
    form.AddField("score", finalScore);
    form.AddField("quiz_type", quizType); // ✅ NEW

    UnityWebRequest www = UnityWebRequest.Post(
        "http://localhost/brailleplay/save_score.php", form);

    yield return www.SendWebRequest();

    Debug.Log("SERVER RESPONSE: " + www.downloadHandler.text);

    if (www.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("Score saved successfully!");
    }
    else
    {
        Debug.LogError("Error: " + www.error);
    }
}
}