using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Beginning_Letters : MonoBehaviour
{
    [System.Serializable]
    public class WordData
    {
        public string word;
        public AudioClip wordAudio;
        public AudioClip inputAudio;   // Sound for the correct first letter
    }

    [Header("Quiz Data")]
    public List<WordData> words = new List<WordData>();

    [Header("Audio Sources")]
    public AudioSource wordAudioSource;
    public AudioSource inputAudioSource;

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
    private int currentIndex = 0;

    private void Start()
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

        feedbackText.text = "Listen carefully.";

        PlayWordAudio();
    }

    void PlayWordAudio()
    {
        if (words[currentIndex].wordAudio != null)
        {
            wordAudioSource.Stop();
            wordAudioSource.clip = words[currentIndex].wordAudio;
            wordAudioSource.Play();
        }
    }

    void PlayInputAudio()
    {
        if (words[currentIndex].inputAudio != null)
        {
            inputAudioSource.Stop();
            inputAudioSource.clip = words[currentIndex].inputAudio;
            inputAudioSource.Play();
        }
    }

    public void CheckAnswer(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Play the user's letter sound
        PlayInputAudio();

        char expectedLetter = char.ToLower(words[currentIndex].word[0]);
        char enteredLetter = char.ToLower(input.Trim()[0]);

        if (enteredLetter == expectedLetter)
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

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        wrongText.text = "Wrong: " + wrongCount;
    }

    public void ReplayWordAudio()
    {
        PlayWordAudio();
    }

    public void ReplayInputAudio()
    {
        PlayInputAudio();
    }
}