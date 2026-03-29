using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public void LoadFromSavedAssessment()
    {
        string level = PlayerPrefs.GetString("AssessmentLevel", "Beginner");

        switch (level)
        {
            case "Beginner":
                SceneManager.LoadScene("BeginnerScene");
                break;

            case "Intermediate":
                SceneManager.LoadScene("IntermediateScene");
                break;

            case "Advance":
                SceneManager.LoadScene("AdvanceScene");
                break;

            default:
                Debug.LogWarning("Unknown saved level: " + level);
                break;
        }
    }
}