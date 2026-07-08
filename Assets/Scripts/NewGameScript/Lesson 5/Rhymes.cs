using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RhymingQuiz : MonoBehaviour
{
    [System.Serializable]
    public class WordData
    {
        [Header("Question")]
        public string word;
        public AudioClip wordAudio;

        [Header("Accepted Rhyming Answers")]
        public List<string> rhymingAnswers = new List<string>();
    }

    [Header("Quiz Data")]
    public List<WordData> words = new List<WordData>();

    [Header("Audio")]
    public AudioSource wordAudioSource;

    [Header("UI")]
    public TMP_InputField inputField;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI wrongText;
    public TextMeshProUGUI feedbackText;

    [Header("Scoring")]
    public int startingScore = 100;
    public int deductionPerWrong = 1;

    private int score;
    private int wrongCount;
    private int currentIndex;

    void Start()
    {
        score = startingScore;
        wrongCount = 0;

        UpdateUI();
        LoadCurrentWord();

        inputField.onSubmit.AddListener(CheckAnswer);
    }

    void LoadCurrentWord()
    {
        if (currentIndex >= words.Count)
        {
            feedbackText.text = "Quiz Complete!\nFinal Score: " + score;
            inputField.interactable = false;
            return;
        }

        inputField.text = "";
        inputField.ActivateInputField();

        feedbackText.text = "Listen carefully and type a rhyming word.";

        if (words[currentIndex].wordAudio != null)
        {
            wordAudioSource.clip = words[currentIndex].wordAudio;
            wordAudioSource.Play();
        }
    }

    public void CheckAnswer(string input)
{
    if (string.IsNullOrWhiteSpace(input))
        return;

    string answer = input.Trim().ToLower();
    string questionWord = words[currentIndex].word.ToLower();

    if (DoWordsRhyme(questionWord, answer))
    {
        feedbackText.text = "Correct!";

        currentIndex++;

        Invoke(nameof(LoadCurrentWord), 1f);
    }
    else
    {
        wrongCount++;
        score -= deductionPerWrong;

        if (score < 0)
            score = 0;

        feedbackText.text = "Wrong! Try Again.";

        UpdateUI();

        inputField.text = "";
        inputField.ActivateInputField();
    }

    UpdateUI();
}
bool DoWordsRhyme(string word1, string word2)
{
    word1 = word1.ToLower().Trim();
    word2 = word2.ToLower().Trim();

    int compareLength = 3;

    if (word1.Length < 3 || word2.Length < 3)
        compareLength = 2;

    string end1 = word1.Substring(word1.Length - compareLength);
    string end2 = word2.Substring(word2.Length - compareLength);

    return end1 == end2;
}
    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        wrongText.text = "Wrong: " + wrongCount;
    }

    public void ReplayWord()
{
    if (words[currentIndex].wordAudio != null)
    {
        wordAudioSource.Stop();
        wordAudioSource.clip = words[currentIndex].wordAudio;
        wordAudioSource.Play();
    }
}
}