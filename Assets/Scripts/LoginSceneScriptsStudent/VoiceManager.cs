using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VoiceKey
{
    // Login scene
    LoginWelcome, LoginInstructions,
    FieldLastName, HintLastName, FieldStudentNumber, HintStudentNumber,
    ButtonSubmit, HintSubmit, WelcomeStudent, GoingBack,

    // Errors
    ErrLastNameEmpty, ErrLastNameShort, ErrLastNameLetters,
    ErrNumberEmpty, ErrNumberDigits, ErrBothEmpty,
    ErrNoInternet, ErrGeneric, ErrNoMatch,

    // Warnings
    WarnFieldFull, WarnInvalidKey, WarnNothingToDelete, WarnFieldOk,

    // Common feedback
    FbEmpty, FbDeleted, FbCleared, FbCorrect, FbIncorrect,
    WarnOnlyLetters, WarnOnlyNumbers, WarnNoSpaces,
    FbSpace, FbDot, FbContains, WarnFirstField, WarnLastField
}

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [Header("Login scene")]
    public AudioClip loginWelcome;
    public AudioClip loginInstructions;
    public AudioClip fieldLastName;
    public AudioClip hintLastName;
    public AudioClip fieldStudentNumber;
    public AudioClip hintStudentNumber;
    public AudioClip buttonSubmit;
    public AudioClip hintSubmit;
    public AudioClip welcomeStudent;   // "Welcome,"  (TTS then says the name)
    public AudioClip goingBack;

    [Header("Errors")]
    public AudioClip errLastNameEmpty;
    public AudioClip errLastNameShort;
    public AudioClip errLastNameLetters;
    public AudioClip errNumberEmpty;
    public AudioClip errNumberDigits;
    public AudioClip errBothEmpty;
    public AudioClip errNoInternet;
    public AudioClip errGeneric;
    public AudioClip errNoMatch;

    [Header("Input field feedback")]
    public AudioClip warnOnlyLetters;   // "Only letters are allowed here."
    public AudioClip warnOnlyNumbers;   // "Only numbers are allowed here."
    public AudioClip warnNoSpaces;      // "This field does not allow spaces."
    public AudioClip fbSpace;           // "Space."
    public AudioClip fbDot;             // "Dot."
    public AudioClip fbContains;        // "Currently contains"

    [Header("Warnings")]
    public AudioClip warnFieldFull;
    public AudioClip warnInvalidKey;
    public AudioClip warnNothingToDelete;
    public AudioClip warnFieldOk;
    public AudioClip warnFirstField;   // "This is the first field."
    public AudioClip warnLastField;    // "This is the last field."

    [Header("Common feedback")]
    public AudioClip fbEmpty;
    public AudioClip fbDeleted;
    public AudioClip fbCleared;
    public AudioClip fbCorrect;
    public AudioClip fbIncorrect;

    [Header("Letters A-Z (index 0 = A)")]
    public AudioClip[] letters = new AudioClip[26];

    [Header("Digits 0-9 (index 0 = Zero)")]
    public AudioClip[] digits = new AudioClip[10];

    private AudioSource source;
    private Dictionary<VoiceKey, AudioClip> map;

    private void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();

        map = new Dictionary<VoiceKey, AudioClip>
        {
            { VoiceKey.LoginWelcome, loginWelcome },
            { VoiceKey.LoginInstructions, loginInstructions },
            { VoiceKey.FieldLastName, fieldLastName },
            { VoiceKey.HintLastName, hintLastName },
            { VoiceKey.FieldStudentNumber, fieldStudentNumber },
            { VoiceKey.HintStudentNumber, hintStudentNumber },
            { VoiceKey.ButtonSubmit, buttonSubmit },
            { VoiceKey.HintSubmit, hintSubmit },
            { VoiceKey.WelcomeStudent, welcomeStudent },
            { VoiceKey.GoingBack, goingBack },

            { VoiceKey.ErrLastNameEmpty, errLastNameEmpty },
            { VoiceKey.ErrLastNameShort, errLastNameShort },
            { VoiceKey.ErrLastNameLetters, errLastNameLetters },
            { VoiceKey.ErrNumberEmpty, errNumberEmpty },
            { VoiceKey.ErrNumberDigits, errNumberDigits },
            { VoiceKey.ErrBothEmpty, errBothEmpty },
            { VoiceKey.ErrNoInternet, errNoInternet },
            { VoiceKey.ErrGeneric, errGeneric },
            { VoiceKey.ErrNoMatch, errNoMatch },

            { VoiceKey.WarnFieldFull, warnFieldFull },
            { VoiceKey.WarnInvalidKey, warnInvalidKey },
            { VoiceKey.WarnNothingToDelete, warnNothingToDelete },
            { VoiceKey.WarnFieldOk, warnFieldOk },
            { VoiceKey.WarnFirstField, warnFirstField },
            { VoiceKey.WarnLastField, warnLastField },

            { VoiceKey.FbEmpty, fbEmpty },
            { VoiceKey.FbDeleted, fbDeleted },
            { VoiceKey.FbCleared, fbCleared },
            { VoiceKey.FbCorrect, fbCorrect },
            { VoiceKey.FbIncorrect, fbIncorrect },
            { VoiceKey.WarnOnlyLetters, warnOnlyLetters },
            { VoiceKey.WarnOnlyNumbers, warnOnlyNumbers },
            { VoiceKey.WarnNoSpaces, warnNoSpaces },
            { VoiceKey.FbSpace, fbSpace },
            { VoiceKey.FbDot, fbDot },
            { VoiceKey.FbContains, fbContains },
        };
    }


    private Coroutine seqRoutine;

    public void Play(VoiceKey key)
    {
        StopSequence();
        PlayInternal(key);
    }

    public void Play(VoiceKey? key)
    {
        if (key.HasValue) Play(key.Value);
    }

    private void PlayInternal(VoiceKey key)
    {
        if (!map.TryGetValue(key, out var clip) || clip == null)
        {
            Debug.LogWarning("No voice clip assigned for: " + key);
            return;
        }
        PlayClip(clip);
    }

    public IEnumerator PlayAndWait(VoiceKey key)
    {
        PlayInternal(key);
        yield return null;
        yield return new WaitWhile(() => source.isPlaying);
    }

    /// <summary>Stops anything playing, then plays these clips in order. A new Speak/Play cancels it.</summary>
    public void Speak(params VoiceKey[] keys)
    {
        Stop();
        seqRoutine = StartCoroutine(PlaySequence(keys));
    }

    public void Stop()
    {
        StopSequence();
        source.Stop();
    }

    private void StopSequence()
    {
        if (seqRoutine != null)
        {
            StopCoroutine(seqRoutine);
            seqRoutine = null;
        }
    }



    public void PlayLetter(char c)
    {
        int i = char.ToUpperInvariant(c) - 'A';
        if (i >= 0 && i < letters.Length && letters[i] != null) PlayClip(letters[i]);
    }

    public void PlayDigit(char c)
    {
        int i = c - '0';
        if (i >= 0 && i < digits.Length && digits[i] != null) PlayClip(digits[i]);
    }


    public IEnumerator SpeakCharAndWait(char c)
    {
        AudioClip clip = null;
        if (char.IsLetter(c))
        {
            int i = char.ToUpperInvariant(c) - 'A';
            if (i >= 0 && i < letters.Length) clip = letters[i];
        }
        else if (c >= '0' && c <= '9') clip = digits[c - '0'];
        else if (c == ' ') clip = fbSpace;

        if (clip == null) yield break;
        PlayClip(clip);
        yield return null;
        yield return new WaitWhile(() => source.isPlaying);
    }

    public IEnumerator PlaySequence(params VoiceKey[] keys)
    {
        foreach (var k in keys) yield return PlayAndWait(k);
    }


    private void PlayClip(AudioClip clip)
    {
        source.Stop();
        source.clip = clip;
        source.Play();
    }


}