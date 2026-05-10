using System;
using UnityEngine;

public class BrailleMapping : MonoBehaviour
{
    public static BrailleMapping Instance;

    [Serializable]
    public class DefaultPatternSound
    {
        public string pattern;
        public AudioClip sound;
    }

    public static event Action OnDot1;
    public static event Action OnDot2;
    public static event Action OnDot3;
    public static event Action OnDot4;
    public static event Action OnDot5;
    public static event Action OnDot6;

    public static event Action<string> OnBrailleChordSubmitted;

    public static event Action OnRepeat;
    public static event Action OnSubmit;
    public static event Action OnDeleteOrNo;
    public static event Action OnYesOrNext;
    public static event Action OnLogin;
    public static event Action OnPause;
    public static event Action OnBack;
    public static event Action OnSpace;

    public static event Action OnCorrect;
    public static event Action OnWrong;

    [Header("Braille Dots")]
    public KeyCode dot1Key = KeyCode.F;
    public KeyCode dot2Key = KeyCode.D;
    public KeyCode dot3Key = KeyCode.S;
    public KeyCode dot4Key = KeyCode.J;
    public KeyCode dot5Key = KeyCode.K;
    public KeyCode dot6Key = KeyCode.L;

    [Header("Extra Controls")]
    public KeyCode pauseKey = KeyCode.P;
    public KeyCode backKey = KeyCode.Escape;

    [Header("Actions")]
    public KeyCode repeatKey = KeyCode.R;
    public KeyCode submitKey = KeyCode.Return;
    public KeyCode deleteOrNoKey = KeyCode.Backspace;
    public KeyCode yesOrNextKey = KeyCode.Y;
    public KeyCode loginKey = KeyCode.Return;
    public KeyCode spaceKey = KeyCode.Space;

    [Header("Feedback Keys")]
    public KeyCode correctKey = KeyCode.Alpha1;
    public KeyCode wrongKey = KeyCode.Alpha2;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip dot1Sfx;
    public AudioClip dot2Sfx;
    public AudioClip dot3Sfx;
    public AudioClip dot4Sfx;
    public AudioClip dot5Sfx;
    public AudioClip dot6Sfx;

    public AudioClip repeatSfx;
    public AudioClip submitSfx;
    public AudioClip deleteOrNoSfx;
    public AudioClip yesOrNextSfx;
    public AudioClip loginSfx;
    public AudioClip spaceSfx;

    public AudioClip correctSfx;
    public AudioClip wrongSfx;

    [Header("Default Braille Letter Sounds")]
    public AudioClip aSound;
    public AudioClip bSound;
    public AudioClip cSound;
    public AudioClip dSound;
    public AudioClip eSound;
    public AudioClip fSound;
    public AudioClip gSound;
    public AudioClip hSound;
    public AudioClip iSound;
    public AudioClip jSound;
    public AudioClip kSound;
    public AudioClip lSound;
    public AudioClip mSound;
    public AudioClip nSound;
    public AudioClip oSound;
    public AudioClip pSound;
    public AudioClip qSound;
    public AudioClip rSound;
    public AudioClip sSound;
    public AudioClip tSound;
    public AudioClip uSound;
    public AudioClip vSound;
    public AudioClip wSound;
    public AudioClip xSound;
    public AudioClip ySound;
    public AudioClip zSound;

    [Range(0f, 3f)] public float letterSoundVolume = 1.5f;

    [Header("Other Default Pattern Sounds")]
    public DefaultPatternSound[] otherDefaultPatternSounds;
    [Range(0f, 3f)] public float otherPatternSoundVolume = 1.5f;

    [Header("Stereo Pan")]
    [Range(-1f, 1f)] public float leftEarPan = -1f;
    [Range(-1f, 1f)] public float rightEarPan = 1f;

    [Header("Volume")]
    [Range(0f, 3f)] public float dotVolume = 2.0f;
    [Range(0f, 3f)] public float actionVolume = 1.5f;
    [Range(0f, 3f)] public float feedbackVolume = 2.0f;

    [Header("Options")]
    public bool logInputs = false;
    public bool playLetterSoundOnChord = true;
    public bool playOtherPatternSoundOnChord = true;

    private bool chordStarted;
    private bool chordDot1;
    private bool chordDot2;
    private bool chordDot3;
    private bool chordDot4;
    private bool chordDot5;
    private bool chordDot6;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        CheckDotChordInputs();
        CheckActionInputs();
        CheckFeedbackInputs();
    }

    private void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volumeMultiplier);
    }

    private bool PlayOtherDefaultPatternSound(string pattern)
    {
        if (!playOtherPatternSoundOnChord)
            return false;

        if (otherDefaultPatternSounds == null)
            return false;

        foreach (DefaultPatternSound patternSound in otherDefaultPatternSounds)
        {
            if (patternSound == null)
                continue;

            if (patternSound.pattern == pattern)
            {
                PlaySfx(patternSound.sound, otherPatternSoundVolume);

                if (logInputs)
                    Debug.Log("Played other default pattern sound for: " + pattern);

                return true;
            }
        }

        return false;
    }

    private void PlayDefaultLetterSound(string pattern)
    {
        if (!playLetterSoundOnChord)
            return;

        AudioClip clip = null;

        switch (pattern)
        {
            case "100000": clip = aSound; break; // A
            case "110000": clip = bSound; break; // B
            case "100100": clip = cSound; break; // C
            case "100110": clip = dSound; break; // D
            case "100010": clip = eSound; break; // E
            case "110100": clip = fSound; break; // F
            case "110110": clip = gSound; break; // G
            case "110010": clip = hSound; break; // H
            case "010100": clip = iSound; break; // I
            case "010110": clip = jSound; break; // J
            case "101000": clip = kSound; break; // K
            case "111000": clip = lSound; break; // L
            case "101100": clip = mSound; break; // M
            case "101110": clip = nSound; break; // N
            case "101010": clip = oSound; break; // O
            case "111100": clip = pSound; break; // P
            case "111110": clip = qSound; break; // Q
            case "111010": clip = rSound; break; // R
            case "011100": clip = sSound; break; // S
            case "011110": clip = tSound; break; // T
            case "101001": clip = uSound; break; // U
            case "111001": clip = vSound; break; // V
            case "010111": clip = wSound; break; // W
            case "101101": clip = xSound; break; // X
            case "101111": clip = ySound; break; // Y
            case "101011": clip = zSound; break; // Z
        }

        if (clip != null)
        {
            PlaySfx(clip, letterSoundVolume);

            if (logInputs)
                Debug.Log("Played letter sound for pattern: " + pattern);
        }
    }

    private void PlayPannedSfx(AudioClip clip, float pan, float volumeMultiplier = 1f)
    {
        if (audioSource == null || clip == null) return;

        GameObject tempAudio = new GameObject("TempPannedAudio");
        tempAudio.transform.SetParent(transform);

        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        tempSource.volume = Mathf.Max(0f, audioSource.volume * volumeMultiplier);
        tempSource.pitch = audioSource.pitch;
        tempSource.spatialBlend = 0f;
        tempSource.panStereo = pan;
        tempSource.clip = clip;
        tempSource.Play();

        Destroy(tempAudio, clip.length + 0.1f);
    }

    private void CheckDotChordInputs()
    {
        if (Input.GetKeyDown(dot1Key))
        {
            chordStarted = true;
            chordDot1 = true;
            PlayPannedSfx(dot1Sfx, rightEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 1 pressed");
        }

        if (Input.GetKeyDown(dot2Key))
        {
            chordStarted = true;
            chordDot2 = true;
            PlayPannedSfx(dot2Sfx, rightEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 2 pressed");
        }

        if (Input.GetKeyDown(dot3Key))
        {
            chordStarted = true;
            chordDot3 = true;
            PlayPannedSfx(dot3Sfx, rightEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 3 pressed");
        }

        if (Input.GetKeyDown(dot4Key))
        {
            chordStarted = true;
            chordDot4 = true;
            PlayPannedSfx(dot4Sfx, leftEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 4 pressed");
        }

        if (Input.GetKeyDown(dot5Key))
        {
            chordStarted = true;
            chordDot5 = true;
            PlayPannedSfx(dot5Sfx, leftEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 5 pressed");
        }

        if (Input.GetKeyDown(dot6Key))
        {
            chordStarted = true;
            chordDot6 = true;
            PlayPannedSfx(dot6Sfx, leftEarPan, dotVolume);
            if (logInputs) Debug.Log("Braille Dot 6 pressed");
        }

        bool anyDotReleased =
            Input.GetKeyUp(dot1Key) ||
            Input.GetKeyUp(dot2Key) ||
            Input.GetKeyUp(dot3Key) ||
            Input.GetKeyUp(dot4Key) ||
            Input.GetKeyUp(dot5Key) ||
            Input.GetKeyUp(dot6Key);

        bool anyDotStillHeld =
            Input.GetKey(dot1Key) ||
            Input.GetKey(dot2Key) ||
            Input.GetKey(dot3Key) ||
            Input.GetKey(dot4Key) ||
            Input.GetKey(dot5Key) ||
            Input.GetKey(dot6Key);

        if (chordStarted && anyDotReleased && !anyDotStillHeld)
        {
            SubmitChord();
        }
    }

    private void SubmitChord()
    {
        if (chordDot1) OnDot1?.Invoke();
        if (chordDot2) OnDot2?.Invoke();
        if (chordDot3) OnDot3?.Invoke();
        if (chordDot4) OnDot4?.Invoke();
        if (chordDot5) OnDot5?.Invoke();
        if (chordDot6) OnDot6?.Invoke();

        string pattern =
            (chordDot1 ? "1" : "0") +
            (chordDot2 ? "1" : "0") +
            (chordDot3 ? "1" : "0") +
            (chordDot4 ? "1" : "0") +
            (chordDot5 ? "1" : "0") +
            (chordDot6 ? "1" : "0");

        if (logInputs) Debug.Log("Braille chord submitted: " + pattern);

        if (!PlayOtherDefaultPatternSound(pattern))
        {
            PlayDefaultLetterSound(pattern);
        }

        OnBrailleChordSubmitted?.Invoke(pattern);

        chordStarted = false;
        chordDot1 = false;
        chordDot2 = false;
        chordDot3 = false;
        chordDot4 = false;
        chordDot5 = false;
        chordDot6 = false;
    }

    private void CheckActionInputs()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (logInputs) Debug.Log("Pause");
            OnPause?.Invoke();
        }

        if (Input.GetKeyDown(backKey))
        {
            if (logInputs) Debug.Log("Back");
            OnBack?.Invoke();
        }

        if (Input.GetKeyDown(repeatKey))
        {
            if (logInputs) Debug.Log("Repeat");
            PlaySfx(repeatSfx, actionVolume);
            OnRepeat?.Invoke();
        }

        if (Input.GetKeyDown(submitKey))
        {
            if (logInputs) Debug.Log("Submit");
            PlaySfx(submitSfx, actionVolume);
            OnSubmit?.Invoke();
        }

        if (Input.GetKeyDown(deleteOrNoKey))
        {
            if (logInputs) Debug.Log("Delete / No");
            PlaySfx(deleteOrNoSfx, actionVolume);
            OnDeleteOrNo?.Invoke();
        }

        if (Input.GetKeyDown(yesOrNextKey))
        {
            if (logInputs) Debug.Log("Yes / Next");
            PlaySfx(yesOrNextSfx, actionVolume);
            OnYesOrNext?.Invoke();
        }

        if (Input.GetKeyDown(loginKey))
        {
            if (logInputs) Debug.Log("Login");
            PlaySfx(loginSfx, actionVolume);
            OnLogin?.Invoke();
        }

        if (Input.GetKeyDown(spaceKey))
        {
            if (logInputs) Debug.Log("Space");
            PlaySfx(spaceSfx);
            OnSpace?.Invoke();
        }
    }

    private void CheckFeedbackInputs()
    {
        if (Input.GetKeyDown(correctKey))
        {
            if (logInputs) Debug.Log("Correct");
            PlaySfx(correctSfx, feedbackVolume);
            OnCorrect?.Invoke();
        }

        if (Input.GetKeyDown(wrongKey))
        {
            if (logInputs) Debug.Log("Wrong");
            PlaySfx(wrongSfx, feedbackVolume);
            OnWrong?.Invoke();
        }
    }

    public void PlayCorrectSfx()
    {
        PlaySfx(correctSfx, feedbackVolume);
        OnCorrect?.Invoke();
    }

    public void PlayWrongSfx()
    {
        PlaySfx(wrongSfx, feedbackVolume);
        OnWrong?.Invoke();
    }

    public bool GetDot1() => Input.GetKey(dot1Key);
    public bool GetDot2() => Input.GetKey(dot2Key);
    public bool GetDot3() => Input.GetKey(dot3Key);
    public bool GetDot4() => Input.GetKey(dot4Key);
    public bool GetDot5() => Input.GetKey(dot5Key);
    public bool GetDot6() => Input.GetKey(dot6Key);

    public string GetCurrentBraillePattern()
    {
        return
            (GetDot1() ? "1" : "0") +
            (GetDot2() ? "1" : "0") +
            (GetDot3() ? "1" : "0") +
            (GetDot4() ? "1" : "0") +
            (GetDot5() ? "1" : "0") +
            (GetDot6() ? "1" : "0");
    }
}