using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class StudentScoreboardManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject scoreRowPrefab;

    [Header("Parent Container")]
    public Transform scoreContainer;

    [Header("PHP URL")]
    public string scoreboardURL = "http://localhost/brailleplay/get_scoreboard.php";

    [System.Serializable]
    public class ScoreData
    {
        public string username;
        public string quiz_name;
        public string high_score;
    }

    [System.Serializable]
    public class ScoreboardResponse
    {
        public bool success;
        public List<ScoreData> scores;
    }

    void Start()
    {
        StartCoroutine(GetScoreboard());
    }

    IEnumerator GetScoreboard()
    {
        UnityWebRequest www = UnityWebRequest.Get(scoreboardURL);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
        }
        else
        {
            string json = www.downloadHandler.text;

            Debug.Log(json);

            ScoreboardResponse response =
                JsonUtility.FromJson<ScoreboardResponse>(json);

            if (response.success)
            {
                DisplayScores(response.scores);
            }
        }
    }

    void DisplayScores(List<ScoreData> scores)
    {
        foreach (Transform child in scoreContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (ScoreData score in scores)
        {
            GameObject row =
                Instantiate(scoreRowPrefab, scoreContainer);

            ScoreRowUI rowUI =
                row.GetComponent<ScoreRowUI>();

            rowUI.Setup(
                score.username,
                score.quiz_name,
                score.high_score
            );
        }
    }
}