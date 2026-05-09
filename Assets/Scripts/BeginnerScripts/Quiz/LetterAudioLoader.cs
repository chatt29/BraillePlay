using UnityEngine;
using System.Collections.Generic;

public class LetterAudioLoader : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Letter Clips")]
    public AudioClip aClip;
    public AudioClip bClip;
    public AudioClip cClip;
    public AudioClip dClip;
    public AudioClip eClip;
    public AudioClip fClip;
    public AudioClip gClip;
    public AudioClip hClip;
    public AudioClip iClip;
    public AudioClip jClip;
    public AudioClip kClip;
    public AudioClip lClip;
    public AudioClip mClip;
    public AudioClip nClip;
    public AudioClip oClip;
    public AudioClip pClip;
    public AudioClip qClip;
    public AudioClip rClip;
    public AudioClip sClip;
    public AudioClip tClip;
    public AudioClip uClip;
    public AudioClip vClip;
    public AudioClip wClip;
    public AudioClip xClip;
    public AudioClip yClip;
    public AudioClip zClip;

    private Dictionary<char, AudioClip> letterMap;

    void Awake()
    {
        letterMap = new Dictionary<char, AudioClip>()
        {
            {'A', aClip},
            {'B', bClip},
            {'C', cClip},
            {'D', dClip},
            {'E', eClip},
            {'F', fClip},
            {'G', gClip},
            {'H', hClip},
            {'I', iClip},
            {'J', jClip},
            {'K', kClip},
            {'L', lClip},
            {'M', mClip},
            {'N', nClip},
            {'O', oClip},
            {'P', pClip},
            {'Q', qClip},
            {'R', rClip},
            {'S', sClip},
            {'T', tClip},
            {'U', uClip},
            {'V', vClip},
            {'W', wClip},
            {'X', xClip},
            {'Y', yClip},
            {'Z', zClip}
        };
    }

    public void PlayLetter(char letter)
    {
        letter = char.ToUpper(letter);

        if (letterMap.ContainsKey(letter))
        {
            audioSource.clip = letterMap[letter];
            audioSource.Play();
        }
    }
}