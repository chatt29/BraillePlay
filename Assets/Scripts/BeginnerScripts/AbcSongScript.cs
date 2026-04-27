using UnityEngine;
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

    [Header("Messages Before ABC Song")]
    public AbcMessage message1;
    public AbcMessage message2;
    public AbcMessage message3;

    [Header("ABC Song")]
    public AudioClip abcSong;

    [Header("Message After Song")]
    public AbcMessage afterSongMessage;

    private int step = 0;
    private bool songPlaying = false;
    private bool lastPlayedWasSong = false;

    void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleFastForward;
        BrailleMapping.OnDeleteOrNo += HandleRewind;
        BrailleMapping.OnRepeat += RepeatSongButton;
    }

    void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleFastForward;
        BrailleMapping.OnDeleteOrNo -= HandleRewind;
        BrailleMapping.OnRepeat -= RepeatSongButton;
    }

    void Start()
    {
        PlayCurrent();
    }

    // ---------- FAST FORWARD (+10s) ----------
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

    // ---------- REWIND (-10s) ----------
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

    // ---------- UI BACK BUTTON ----------
    public void BackButton()
    {
        Back();
    }

    // ---------- BACK TO START / HELLO ----------
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
        lastPlayedWasSong = false;
        step = 0;

        PlayCurrent(); // back to first message / "Hello"
    }

    // ---------- REPEAT SONG BUTTON ----------
    // Connect your Repeat Button OnClick() to this function.
    public void RepeatSongButton()
    {
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        songPlaying = false;
        lastPlayedWasSong = true;
        step = 3;

        PlayCurrent(); // repeat ABC song
    }

    // ---------- MESSAGE FLOW ----------
    AbcMessage GetMessage(int index)
    {
        switch (index)
        {
            case 0: return message1;
            case 1: return message2;
            case 2: return message3;

            // Step 3 is the ABC song

            case 4: return afterSongMessage;
        }

        return null;
    }

    public void PlayCurrent()
    {
        StopAllCoroutines();

        if (step == 3)
        {
            lastPlayedWasSong = true;

            if (bubbleText != null)
            {
                bubbleText.text = "Lets sing!";
            }

            StartCoroutine(PlaySong());
            return;
        }

        AbcMessage msg = GetMessage(step);

        if (msg != null)
        {
            lastPlayedWasSong = false;

            if (bubbleText != null)
            {
                bubbleText.text = msg.messageText;
            }

            PlayAudio(msg.messageAudio);
            StartCoroutine(AutoNext(msg.messageAudio));
        }
    }

    IEnumerator AutoNext(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        if (!songPlaying && step < 4)
        {
            step++;
            PlayCurrent();
        }
    }

    IEnumerator PlaySong()
    {
        songPlaying = true;
        lastPlayedWasSong = true;

        if (bubbleText != null)
        {
            bubbleText.text = "Lets sing!";
        }

        if (audioSource != null && abcSong != null)
        {
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.clip = abcSong;
            audioSource.Play();

            yield return new WaitUntil(() => !audioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        songPlaying = false;
        lastPlayedWasSong = true;

        step++;
        PlayCurrent(); // plays "I hope you enjoyed the song."
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
        if (songPlaying) return;

        if (step < 4)
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