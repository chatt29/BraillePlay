using System;
using UnityEngine;

public class AssessmentInputHandler : MonoBehaviour
{
    public static AssessmentInputHandler Instance;

    public static event Action OnDot1;
    public static event Action OnDot2;
    public static event Action OnDot3;
    public static event Action OnDot4;
    public static event Action OnDot5;
    public static event Action OnDot6;

    public static event Action OnRepeat;
    public static event Action OnSubmit;
    public static event Action OnNo;
    public static event Action OnYes;
    public static event Action OnLogin;

    [Header("Braille Dots")]
    public KeyCode dot1Key = KeyCode.F;
    public KeyCode dot2Key = KeyCode.D;
    public KeyCode dot3Key = KeyCode.S;
    public KeyCode dot4Key = KeyCode.J;
    public KeyCode dot5Key = KeyCode.K;
    public KeyCode dot6Key = KeyCode.L;

    [Header("Assessment Actions")]
    public KeyCode repeatKey = KeyCode.R;
    public KeyCode submitKey = KeyCode.Space;
    public KeyCode noKey = KeyCode.Backspace;
    public KeyCode yesKey = KeyCode.Y;
    public KeyCode loginKey = KeyCode.Return;

    [Header("Audio")]
    public AudioSource sfxSource;

    public AudioClip dot1Sfx;
    public AudioClip dot2Sfx;
    public AudioClip dot3Sfx;
    public AudioClip dot4Sfx;
    public AudioClip dot5Sfx;
    public AudioClip dot6Sfx;

    public AudioClip repeatSfx;
    public AudioClip submitSfx;
    public AudioClip noSfx;
    public AudioClip yesSfx;
    public AudioClip loginSfx;
    public AudioClip validPressSfx;

    [Header("Options")]
    public bool logInputs = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        CheckDotInputs();
        CheckActionInputs();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void CheckDotInputs()
    {
        if (Input.GetKeyDown(dot1Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 1");
            PlaySfx(dot1Sfx);
            OnDot1?.Invoke();
        }

        if (Input.GetKeyDown(dot2Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 2");
            PlaySfx(dot2Sfx);
            OnDot2?.Invoke();
        }

        if (Input.GetKeyDown(dot3Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 3");
            PlaySfx(dot3Sfx);
            OnDot3?.Invoke();
        }

        if (Input.GetKeyDown(dot4Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 4");
            PlaySfx(dot4Sfx);
            OnDot4?.Invoke();
        }

        if (Input.GetKeyDown(dot5Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 5");
            PlaySfx(dot5Sfx);
            OnDot5?.Invoke();
        }

        if (Input.GetKeyDown(dot6Key))
        {
            if (logInputs) Debug.Log("Assessment Input: Dot 6");
            PlaySfx(dot6Sfx);
            OnDot6?.Invoke();
        }
    }

    private void CheckActionInputs()
    {
        if (Input.GetKeyDown(repeatKey))
        {
            if (logInputs) Debug.Log("Assessment Input: Repeat");
            PlaySfx(repeatSfx);
            OnRepeat?.Invoke();
        }

        if (Input.GetKeyDown(submitKey))
        {
            if (logInputs) Debug.Log("Assessment Input: Submit");
            PlaySfx(submitSfx);
            OnSubmit?.Invoke();
        }

        if (Input.GetKeyDown(noKey))
        {
            if (logInputs) Debug.Log("Assessment Input: No / Back / Previous / Left");
            PlaySfx(noSfx != null ? noSfx : validPressSfx);
            OnNo?.Invoke();
        }

        if (Input.GetKeyDown(yesKey))
        {
            if (logInputs) Debug.Log("Assessment Input: Yes / Next / Right");
            PlaySfx(yesSfx != null ? yesSfx : validPressSfx);
            OnYes?.Invoke();
        }

        if (Input.GetKeyDown(loginKey))
        {
            if (logInputs) Debug.Log("Assessment Input: Login");
            PlaySfx(loginSfx);
            OnLogin?.Invoke();
        }
    }

    public void PlayValidPress()
    {
        PlaySfx(validPressSfx);
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