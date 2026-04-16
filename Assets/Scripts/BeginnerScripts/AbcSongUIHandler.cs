using UnityEngine;
using UnityEngine.SceneManagement;

public class AbcSongUIHandler : MonoBehaviour
{
    public AudioSource audioSource;

    private bool isPaused = false;

    private void OnEnable()
    {
        BrailleMapping.OnPause += TogglePause;
        BrailleMapping.OnBack += Back;
    }

    private void OnDisable()
    {
        BrailleMapping.OnPause -= TogglePause;
        BrailleMapping.OnBack -= Back;
    }

    public void Back()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // change if needed
    }

    public void TogglePause()
    {
        if (!isPaused)
        {
            Time.timeScale = 0f;
            audioSource.Pause();
            isPaused = true;
        }
        else
        {
            Time.timeScale = 1f;
            audioSource.UnPause();
            isPaused = false;
        }
    }
}