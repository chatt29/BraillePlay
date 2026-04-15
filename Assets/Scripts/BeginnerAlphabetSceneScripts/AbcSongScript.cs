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

    void Start()
    {
        PlayCurrent();
    }

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
        if (songPlaying) return; // prevents skipping during song

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