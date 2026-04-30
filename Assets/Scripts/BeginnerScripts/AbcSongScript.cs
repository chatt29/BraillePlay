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
    public Sprite pauseSprite; // Pause.Image_0
    public Sprite playSprite;  // Play.Image_0

    [Header("Messages Before ABC Song")]
    public AbcMessage message1;
    public AbcMessage message2;
    public AbcMessage message3;

    [Header("ABC Song")]
    public AudioClip abcSong;

    [Header("Messages After Song")]
    public AbcMessage afterMessage1;
    public AbcMessage afterMessage2;

    private int step = 0;
    private bool songPlaying = false;
    private bool isPaused = false;

    void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleFastForward;
        BrailleMapping.OnDeleteOrNo += HandleRewind;
        BrailleMapping.OnRepeat += RepeatSongButton;
        BrailleMapping.OnPause += TogglePausePlay; // P key
    }

    void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleFastForward;
        BrailleMapping.OnDeleteOrNo -= HandleRewind;
        BrailleMapping.OnRepeat -= RepeatSongButton;
        BrailleMapping.OnPause -= TogglePausePlay;
    }

    void Start()
    {
        isPaused = false;
        ShowPauseImage();
        PlayCurrent();
    }

    // ---------- PAUSE / PLAY ----------
    public void TogglePausePlay()
    {
        if (isPaused)
        {
            PlayButton();
        }
        else
        {
            PauseButton();
        }
    }

    public void PauseButton()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }

        isPaused = true;
        ShowPlayImage();
    }

    public void PlayButton()
    {
        if (audioSource != null)
        {
            audioSource.UnPause();
        }

        isPaused = false;
        ShowPauseImage();
    }

    void ShowPauseImage()
    {
        if (pausePlayImage != null && pauseSprite != null)
        {
            pausePlayImage.sprite = pauseSprite;
        }
    }

    void ShowPlayImage()
    {
        if (pausePlayImage != null && playSprite != null)
        {
            pausePlayImage.sprite = playSprite;
        }
    }

    // ---------- FAST FORWARD ----------
    void HandleFastForward()
    {
        FastForward10();
    }

    public void FastForward10()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.time += 10f;

        if (audioSource.time > audioSource.clip.length)
        {
            audioSource.time = audioSource.clip.length - 0.1f;
        }
    }

    // ---------- REWIND ----------
    void HandleRewind()
    {
        Rewind10();
    }

    public void Rewind10()
    {
        if (audioSource == null || audioSource.clip == null) return;

        audioSource.time -= 10f;

        if (audioSource.time < 0f)
        {
            audioSource.time = 0f;
        }
    }

    // ---------- BACK TO START ----------
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
        PlayCurrent();
    }

    // ---------- REPEAT ----------
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

        // Go back to the 3rd message first.
        // Then it will automatically continue to the song.
        step = 2;

        ShowPauseImage();
        PlayCurrent();
    }

    // ---------- MESSAGE FLOW ----------
    AbcMessage GetMessage(int index)
    {
        switch (index)
        {
            case 0:
                return message1;

            case 1:
                return message2;

            case 2:
                return message3;

            // step 3 = ABC song

            case 4:
                return afterMessage1;

            case 5:
                return afterMessage2;
        }

        return null;
    }

    public void PlayCurrent()
    {
        StopAllCoroutines();

        isPaused = false;
        ShowPauseImage();

        // ABC song step
        if (step == 3)
        {
            StartCoroutine(PlaySong());
            return;
        }

        AbcMessage msg = GetMessage(step);

        if (msg != null)
        {
            bubbleText.text = msg.messageText;
            PlayAudio(msg.messageAudio);

            StartCoroutine(AutoNext(msg.messageAudio));
        }
    }

    IEnumerator AutoNext(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying && !isPaused);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        if (!songPlaying && step < 5)
        {
            step++;
            PlayCurrent();
        }
    }

    IEnumerator PlaySong()
    {
        songPlaying = true;

        if (audioSource != null && abcSong != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.clip = abcSong;
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

    void PlayAudio(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.clip = clip;
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