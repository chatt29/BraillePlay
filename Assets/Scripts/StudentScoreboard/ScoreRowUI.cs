using TMPro;
using UnityEngine;

public class ScoreRowUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text gameModeText;
    public TMP_Text scoreText;

    public void Setup(string username, string gameMode, string score)
    {
        nameText.text = username;
        gameModeText.text = gameMode;
        scoreText.text = score;
    }
}