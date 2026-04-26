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

    [Header("Messages After Song")]
    public AbcMessage message4;
    public AbcMessage message5;
    public AbcMessage message6;

    private int step = 0;
    private bool songPlaying = false;

    void OnEnable()
    {
        BrailleMapping.OnSubmit += HandleFastForward;      // +10s
        BrailleMapping.OnDeleteOrNo += HandleRewind;       // -10s
    }

    void OnDisable()
    {
        BrailleMapping.OnSubmit -= HandleFastForward;
        BrailleMapping.OnDeleteOrNo -= HandleRewind;
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

    // ---------- MESSAGE FLOW ----------
    AbcMessage GetMessage(int index)
    {
        switch (index)
        {
            case 0: return message1;
            case 1: return message2;
            case 2: return message3;
            case 4: return message4;
            case 5: return message5;
            case 6: return message6;
        }
        return null;
    }

    public void PlayCurrent()
    {
        if (step == 3)
        {
            bubbleText.text = "Let's sing!";
            StartCoroutine(PlaySong());
            return;
        }

        AbcMessage msg = GetMessage(step);

        if (msg != null)
        {
            bubbleText.text = msg.messageText;
            PlayAudio(msg.messageAudio);
        }
    }

    IEnumerator PlaySong()
    {
        songPlaying = true;

        audioSource.Stop();
        audioSource.clip = abcSong;
        audioSource.Play();

        yield return new WaitForSeconds(abcSong.length);

        songPlaying = false;
        step++;
        PlayCurrent();
    }

    void PlayAudio(AudioClip clip)
    {
        if (clip == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void Next()
    {
        if (songPlaying) return;

        if (step < 6)
        {
            step++;
            PlayCurrent();
        }
    }

    public void Repeat()
    {
        if (songPlaying) return;

        PlayCurrent();
    }
}