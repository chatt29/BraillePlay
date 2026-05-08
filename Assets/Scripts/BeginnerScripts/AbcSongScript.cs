using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Audio Speed")]
    [Range(1f, 2f)]
    public float audioSpeed = 1f;

    private int step = 0;
    private bool songPlaying = false;
    private bool isPaused = false;

    private void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleFastForward;
        BrailleMapping.OnDeleteOrNo += HandleRewind;
        BrailleMapping.OnRepeat += RepeatSongButton;
        BrailleMapping.OnPause += TogglePausePlay;
    }

    private void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleFastForward;
        BrailleMapping.OnDeleteOrNo -= HandleRewind;
        BrailleMapping.OnRepeat -= RepeatSongButton;
        BrailleMapping.OnPause -= TogglePausePlay;
    }

    private void Start()
    {
        isPaused = false;
        ShowPauseImage();
        PlayCurrent();
    }

    public void SetAudioSpeed(float speed)
    {
        audioSpeed = Mathf.Clamp(speed, 1f, 2f);

        if (audioSource != null && audioSource.clip == abcSong)
        {
            audioSource.pitch = audioSpeed;
        }

        Debug.Log("ABC Song speed set to: " + audioSpeed + "x");
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
    }

    public void PlayButton()
    {
        if (audioSource != null)
            audioSource.UnPause();

        isPaused = false;
        ShowPauseImage();
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

    private void HandleFastForward()
    {
        FastForward10();
    }

    public void FastForward10()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.time += 10f;

        if (audioSource.time > audioSource.clip.length)
            audioSource.time = audioSource.clip.length - 0.1f;
    }

    private void HandleRewind()
    {
        Rewind10();
    }

    public void Rewind10()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.time -= 10f;

        if (audioSource.time < 0f)
            audioSource.time = 0f;
    }

    public void Back()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.clip = null;
            audioSource.pitch = 1f;
        }

        songPlaying = false;
        isPaused = false;
        step = 0;

        ShowPauseImage();
        PlayCurrent();
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
        PlayCurrent();
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
            audioSource.pitch = audioSpeed;
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
        audioSource.pitch = 1f; // messages stay normal speed
        audioSource.Play();
    }

    public void Next()
    {
        if (songPlaying || isPaused) return;

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
}