using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomLetterImageAudio : MonoBehaviour
{
    public Image letterImage;
    public AudioSource audioSource;

    private List<char> remainingLetters = new List<char>();

    void Start()
    {
        ResetLetters();
    }

    void ResetLetters()
    {
        remainingLetters.Clear();

        for (char c = 'A'; c <= 'Z'; c++)
        {
            remainingLetters.Add(c);
        }
    }

    public void GenerateRandomLetter()
    {
        if (remainingLetters.Count == 0)
        {
            ResetLetters();
        }

        int index = Random.Range(0, remainingLetters.Count);
        char selectedLetter = remainingLetters[index];
        remainingLetters.RemoveAt(index);

        // IMAGE
        Sprite sprite = Resources.Load<Sprite>("IMAGES" + selectedLetter);
        if (sprite != null)
            letterImage.sprite = sprite;
        else
            Debug.LogWarning("Missing Image: Letter" + selectedLetter);

        // AUDIO
        AudioClip clip = Resources.Load<AudioClip>("Sounds" + selectedLetter);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
            Debug.LogWarning("Missing Audio: " + selectedLetter);
    }
}