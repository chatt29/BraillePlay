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

    public static event Action OnUp;
    public static event Action OnDown;
    public static event Action OnLeft;
    public static event Action OnRight;

    public static event Action OnCorrect;
    public static event Action OnWrong;

    [Header("Braille Dots")]
    public KeyCode dot1Key = KeyCode.J;
    public KeyCode dot2Key = KeyCode.K;
    public KeyCode dot3Key = KeyCode.L;
    public KeyCode dot4Key = KeyCode.F;
    public KeyCode dot5Key = KeyCode.D;
    public KeyCode dot6Key = KeyCode.S;

    [Header("Directional Controls")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;

    [Header("Extra Controls")]
    public KeyCode pauseKey = KeyCode.P;
    public KeyCode backKey = KeyCode.Escape;

    [Header("Actions")]
    public KeyCode repeatKey = KeyCode.R;
    public KeyCode submitKey = KeyCode.Return;
    public KeyCode deleteOrNoKey = KeyCode.Backspace;
    public KeyCode yesOrNextKey = KeyCode.Space;
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

    // ---------------------------------------------------------------------
    // Sequential dot entry state
    //
    // Dots are no longer chorded by holding multiple keys down at once.
    // Instead, pressing a dot key TOGGLES that dot on/off in the buffer
    // below (press dot1, then dot2, etc., one at a time). Pressing the
    // Submit key (Enter) finalizes whatever dots are currently toggled on
    // into a single letter pattern and fires OnBrailleChordSubmitted, then
    // clears the buffer for the next letter.
    // ---------------------------------------------------------------------
    private bool chordStarted;
    private bool chordDot1;
    private bool chordDot2;
    private bool chordDot3;
    private bool chordDot4;
    private bool chordDot5;
    private bool chordDot6;

    private void Awake()
    {
        Instance = this;
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
            case "100000": clip = aSound; break;
            case "110000": clip = bSound; break;
            case "100100": clip = cSound; break;
            case "100110": clip = dSound; break;
            case "100010": clip = eSound; break;
            case "110100": clip = fSound; break;
            case "110110": clip = gSound; break;
            case "110010": clip = hSound; break;
            case "010100": clip = iSound; break;
            case "010110": clip = jSound; break;
            case "101000": clip = kSound; break;
            case "111000": clip = lSound; break;
            case "101100": clip = mSound; break;
            case "101110": clip = nSound; break;
            case "101010": clip = oSound; break;
            case "111100": clip = pSound; break;
            case "111110": clip = qSound; break;
            case "111010": clip = rSound; break;
            case "011100": clip = sSound; break;
            case "011110": clip = tSound; break;
            case "101001": clip = uSound; break;
            case "111001": clip = vSound; break;
            case "010111": clip = wSound; break;
            case "101101": clip = xSound; break;
            case "101111": clip = ySound; break;
            case "101011": clip = zSound; break;
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

    /// <summary>
    /// Toggles a single dot on/off in the current letter buffer. Pressing an
    /// already-active dot again removes it (e.g. for correcting a mistake
    /// before submitting). Plays the dot's panned sound on every press,
    /// regardless of whether it turned the dot on or off.
    /// </summary>
    private void ToggleDot(ref bool dotState, AudioClip sfx, float pan, string dotLabel)
    {
        dotState = !dotState;
        PlayPannedSfx(sfx, pan, dotVolume);

        if (logInputs)
            Debug.Log($"Braille Dot {dotLabel} {(dotState ? "added" : "removed")}");
    }

    private void CheckDotChordInputs()
    {
        if (Input.GetKeyDown(dot1Key))
        {
            ToggleDot(ref chordDot1, dot1Sfx, rightEarPan, "1");
            OnDot1?.Invoke();
        }

        if (Input.GetKeyDown(dot2Key))
        {
            ToggleDot(ref chordDot2, dot2Sfx, rightEarPan, "2");
            OnDot2?.Invoke();
        }

        if (Input.GetKeyDown(dot3Key))
        {
            ToggleDot(ref chordDot3, dot3Sfx, rightEarPan, "3");
            OnDot3?.Invoke();
        }

        if (Input.GetKeyDown(dot4Key))
        {
            ToggleDot(ref chordDot4, dot4Sfx, leftEarPan, "4");
            OnDot4?.Invoke();
        }

        if (Input.GetKeyDown(dot5Key))
        {
            ToggleDot(ref chordDot5, dot5Sfx, leftEarPan, "5");
            OnDot5?.Invoke();
        }

        if (Input.GetKeyDown(dot6Key))
        {
            ToggleDot(ref chordDot6, dot6Sfx, leftEarPan, "6");
            OnDot6?.Invoke();
        }

        chordStarted = chordDot1 || chordDot2 || chordDot3 || chordDot4 || chordDot5 || chordDot6;
    }

    /// <summary>
    /// Finalizes whatever dots are currently toggled on into a single
    /// pattern string, fires OnBrailleChordSubmitted, plays the matching
    /// letter/pattern sound, then clears the buffer so the next letter can
    /// be entered from scratch.
    /// </summary>
    private void SubmitChord()
    {
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
        if (Input.GetKeyDown(upKey))
        {
            if (logInputs) Debug.Log("Up");
            OnUp?.Invoke();
        }

        if (Input.GetKeyDown(downKey))
        {
            if (logInputs) Debug.Log("Down");
            OnDown?.Invoke();
        }

        if (Input.GetKeyDown(leftKey))
        {
            if (logInputs) Debug.Log("Left");
            OnLeft?.Invoke();
        }

        if (Input.GetKeyDown(rightKey))
        {
            if (logInputs) Debug.Log("Right");
            OnRight?.Invoke();
        }

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
            // If any dots have been entered for the current letter, Submit
            // finalizes that letter instead of firing the generic OnSubmit
            // event.
            if (chordStarted)
            {
                if (logInputs) Debug.Log("Submit — finalizing letter");
                PlaySfx(submitSfx, actionVolume);
                SubmitChord();
            }
            else
            {
                if (logInputs) Debug.Log("Submit");
                PlaySfx(submitSfx, actionVolume);
                OnSubmit?.Invoke();
            }
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

    public bool GetDot1() => chordDot1;
    public bool GetDot2() => chordDot2;
    public bool GetDot3() => chordDot3;
    public bool GetDot4() => chordDot4;
    public bool GetDot5() => chordDot5;
    public bool GetDot6() => chordDot6;

    /// <summary>
    /// Returns the pattern currently being built (dots toggled on so far),
    /// not just the instantaneous held-key state — since dots are no longer
    /// held simultaneously, this reflects the in-progress letter buffer.
    /// </summary>
    public string GetCurrentBraillePattern()
    {
        return
            (chordDot1 ? "1" : "0") +
            (chordDot2 ? "1" : "0") +
            (chordDot3 ? "1" : "0") +
            (chordDot4 ? "1" : "0") +
            (chordDot5 ? "1" : "0") +
            (chordDot6 ? "1" : "0");
    }
}