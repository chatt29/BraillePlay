using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class ScoreSceneController : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    public AudioSource audioSource;

    // assign clips like "You scored", numbers, etc.
    public AudioClip introClip;
    public AudioClip[] numberClips; // index = number

    private int finalScore;

    void Start()
    {
        finalScore = PlayerPrefs.GetInt("FinalScore", 0);

        scoreText.text = "Your Score: " + finalScore;
    }

    public void OnSubmit()
    {
        StartCoroutine(PlayScoreThenSave());
    }

    IEnumerator PlayScoreThenSave()
    {
        // 1. Play intro sound
        if (audioSource != null && introClip != null)
        {
            audioSource.PlayOneShot(introClip);
            yield return new WaitForSeconds(introClip.length);
        }

        // 2. Play score number audio (optional system)
        yield return StartCoroutine(PlayNumber(finalScore));

        // 3. Save score
        int userId = AccessibleLoginFlow.LoggedInUserId;
        string quizName = PlayerPrefs.GetString("PreviousScene", "UnknownQuiz");

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId);
        form.AddField("score", finalScore);
        form.AddField("quiz_name", quizName);

        UnityWebRequest www = UnityWebRequest.Post(
            "http://localhost/brailleplay/save_score.php",
            form);

        yield return www.SendWebRequest();

        Debug.Log(www.downloadHandler.text);

        // 4. Go back to game
        SceneManager.LoadScene("BeginnerScene");
    }

    IEnumerator PlayNumber(int score)
    {
        if (numberClips == null || numberClips.Length == 0)
            yield break;

        if (score >= 0 && score < numberClips.Length)
        {
            audioSource.PlayOneShot(numberClips[score]);
            yield return new WaitForSeconds(numberClips[score].length);
        }
    }
}