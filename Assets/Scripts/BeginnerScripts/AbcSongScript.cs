using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

[System.Serializable]
public class AbcMessage
{
    [TextArea(2, 4)]
    public string messageText;
    public AudioClip messageAudio;
}

public class AbcSongScript : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI bubbleText;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Wireless Haptics")]
    public GameObject wirelessHapticsObject;

    [Header("Pause / Play Image")]
    public Image pausePlayImage;
    public Sprite pauseSprite;
    public Sprite playSprite;

    [Header("Messages Before ABC Song")]
    public AbcMessage message1;
    public AbcMessage message2;
    public AbcMessage message3;

    [Header("ABC Song")]
    public AudioClip abcSong;

    [Header("Messages After Song")]
    public AbcMessage afterMessage1;
    public AbcMessage afterMessage2;

    [Header("Settings")]
    public float skipSeconds = 10f;

    private int step = 0;
    private bool songPlaying = false;
    private bool isPaused = false;

    private void OnEnable()
    {
        BrailleMapping.OnSpace += TogglePausePlay;
        BrailleMapping.OnLeft += Rewind10;
        BrailleMapping.OnRight += FastForward10;
        BrailleMapping.OnRepeat += RepeatSongButton;
        BrailleMapping.OnBack += BackToMainMenu;
    }

    private void OnDisable()
    {
        BrailleMapping.OnSpace -= TogglePausePlay;
        BrailleMapping.OnLeft -= Rewind10;
        BrailleMapping.OnRight -= FastForward10;
        BrailleMapping.OnRepeat -= RepeatSongButton;
        BrailleMapping.OnBack -= BackToMainMenu;
    }

    private void Start()
    {
        isPaused = false;
        ShowPauseImage();
        PlayCurrent();
    }

    public void TogglePausePlay()
    {
        if (isPaused)
            PlayButton();
        else
            PauseButton();
    }

    public void PauseButton()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();

        isPaused = true;
        ShowPlayImage();
        TriggerHaptic();
    }

    public void PlayButton()
    {
        if (audioSource != null)
            audioSource.UnPause();

        isPaused = false;
        ShowPauseImage();
        TriggerHaptic();
    }

    public void FastForward10()
    {
        if (!songPlaying) return;
        if (audioSource == null) return;
        if (audioSource.clip == null) return;
        if (audioSource.clip.length <= 0f) return;

        float currentTime = audioSource.time;
        float maxTime = Mathf.Max(0f, audioSource.clip.length - 0.25f);
        float newTime = Mathf.Clamp(currentTime + skipSeconds, 0f, maxTime);

        audioSource.time = newTime;

        TriggerHaptic();
    }

    public void Rewind10()
    {
        if (!songPlaying) return;
        if (audioSource == null) return;
        if (audioSource.clip == null) return;
        if (audioSource.clip.length <= 0f) return;

        float currentTime = audioSource.time;
        float maxTime = Mathf.Max(0f, audioSource.clip.length - 0.25f);
        float newTime = Mathf.Clamp(currentTime - skipSeconds, 0f, maxTime);

        audioSource.time = newTime;

        TriggerHaptic();
    }

    public void Back()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.clip = null;
        }

        songPlaying = false;
        isPaused = false;
        step = 0;

        ShowPauseImage();
        TriggerHaptic();
        PlayCurrent();
    }

    public void BackToMainMenu()
    {
        TriggerHaptic();

        if (audioSource != null)
            audioSource.Stop();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RepeatSongButton()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        songPlaying = false;
        isPaused = false;
        step = 2;

        ShowPauseImage();
        TriggerHaptic();
        PlayCurrent();
    }

    public void Next()
    {
        if (songPlaying || isPaused)
            return;

        if (step < 5)
        {
            step++;
            PlayCurrent();
        }
    }

    public void Repeat()
    {
        RepeatSongButton();
    }

    private AbcMessage GetMessage(int index)
    {
        switch (index)
        {
            case 0: return message1;
            case 1: return message2;
            case 2: return message3;
            case 4: return afterMessage1;
            case 5: return afterMessage2;
        }

        return null;
    }

    public void PlayCurrent()
    {
        StopAllCoroutines();

        isPaused = false;
        ShowPauseImage();

        if (step == 3)
        {
            StartCoroutine(PlaySong());
            return;
        }

        AbcMessage msg = GetMessage(step);

        if (msg != null)
        {
            if (bubbleText != null)
                bubbleText.text = msg.messageText;

            PlayAudio(msg.messageAudio);
            StartCoroutine(AutoNext(msg.messageAudio));
        }
    }

    private IEnumerator AutoNext(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            yield return new WaitUntil(() => !audioSource.isPlaying && !isPaused);
        else
            yield return new WaitForSeconds(2f);

        if (!songPlaying && step < 5)
        {
            step++;
            PlayCurrent();
        }
    }

    private IEnumerator PlaySong()
    {
        songPlaying = true;

        if (audioSource != null && abcSong != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.clip = abcSong;
            audioSource.pitch = currentAudioSpeed;
            audioSource.Play();

            yield return new WaitUntil(() => !audioSource.isPlaying && !isPaused);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        songPlaying = false;
        step++;
        PlayCurrent();
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.clip = clip;
        audioSource.pitch = currentAudioSpeed;
        audioSource.Play();
    }

    private void ShowPauseImage()
    {
        if (pausePlayImage != null && pauseSprite != null)
            pausePlayImage.sprite = pauseSprite;
    }

    private void ShowPlayImage()
    {
        if (pausePlayImage != null && playSprite != null)
            pausePlayImage.sprite = playSprite;
    }

    private float currentAudioSpeed = 1.0f;

    private void Update()
    {
        HandleSpeedKeys();
    }

    private void HandleSpeedKeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
            SetAudioSpeed(1.0f);

        if (Input.GetKeyDown(KeyCode.Alpha8))
            SetAudioSpeed(1.25f);

        if (Input.GetKeyDown(KeyCode.Alpha9))
            SetAudioSpeed(1.5f);

        if (Input.GetKeyDown(KeyCode.Alpha0))
            SetAudioSpeed(1.75f);

        if (Input.GetKeyDown(KeyCode.Minus))
            SetAudioSpeed(2.0f);
    }

    private void SetAudioSpeed(float speed)
    {
        currentAudioSpeed = speed;

        if (audioSource != null)
            audioSource.pitch = currentAudioSpeed;

        TriggerHaptic();
    }

    private void TriggerHaptic()
    {
        if (wirelessHapticsObject != null)
        {
            wirelessHapticsObject.SendMessage(
                "TriggerHaptic",
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}