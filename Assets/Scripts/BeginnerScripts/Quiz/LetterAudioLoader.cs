using System.Collections.Generic;
using UnityEngine;

public class LetterAudioLoader : MonoBehaviour
{
    private AudioSource audioSource;
    private Dictionary<char, AudioClip> letterSounds = new Dictionary<char, AudioClip>();

    [Range(0f, 3f)]
    public float volume = 1.5f;

    void Awake()
    {
        // 🔊 AUTO CREATE AUDIO SOURCE
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        LoadAllLetterSounds();
    }

    void LoadAllLetterSounds()
    {
        for (char c = 'A'; c <= 'Z'; c++)
        {
            AudioClip clip = Resources.Load<AudioClip>(
                "SFX/AlphabetLetterToBraille/" + c
            );

            if (clip != null)
            {
                letterSounds[c] = clip;
            }
            else
            {
                Debug.LogWarning("Missing sound for letter: " + c);
            }
        }
    }

    public void PlayLetter(char letter)
    {
        letter = char.ToUpper(letter);

        if (letterSounds.ContainsKey(letter))
        {
            audioSource.PlayOneShot(letterSounds[letter], volume);
        }
    }
}