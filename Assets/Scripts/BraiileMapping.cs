using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BrailleMapping : MonoBehaviour
{
    public static BrailleMapping Instance;

    public static event Action OnDot1;
    public static event Action OnDot2;
    public static event Action OnDot3;
    public static event Action OnDot4;
    public static event Action OnDot5;
    public static event Action OnDot6;

    public static event Action OnRepeat;
    public static event Action OnSubmit;
    public static event Action OnDeleteOrNo;
    public static event Action OnYesOrNext;
    public static event Action OnLogin;

    [Header("Braille Dots")]
    public KeyCode dot1Key = KeyCode.F;
    public KeyCode dot2Key = KeyCode.D;
    public KeyCode dot3Key = KeyCode.S;
    public KeyCode dot4Key = KeyCode.J;
    public KeyCode dot5Key = KeyCode.K;
    public KeyCode dot6Key = KeyCode.L;

    [Header("Actions")]
    public KeyCode repeatKey = KeyCode.R;
    public KeyCode submitKey = KeyCode.Space;
    public KeyCode deleteOrNoKey = KeyCode.Backspace;
    public KeyCode yesOrNextKey = KeyCode.Y;
    public KeyCode loginKey = KeyCode.Return;

    [Header("Options")]
    public bool dontDestroyOnLoad = true;
    public bool logInputs = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        CheckDotInputs();
        CheckActionInputs();
    }

    private void CheckDotInputs()
    {
        if (Input.GetKeyDown(dot1Key))
        {
            if (logInputs) Debug.Log("Braille Dot 1");
            OnDot1?.Invoke();
        }

        if (Input.GetKeyDown(dot2Key))
        {
            if (logInputs) Debug.Log("Braille Dot 2");
            OnDot2?.Invoke();
        }

        if (Input.GetKeyDown(dot3Key))
        {
            if (logInputs) Debug.Log("Braille Dot 3");
            OnDot3?.Invoke();
        }

        if (Input.GetKeyDown(dot4Key))
        {
            if (logInputs) Debug.Log("Braille Dot 4");
            OnDot4?.Invoke();
        }

        if (Input.GetKeyDown(dot5Key))
        {
            if (logInputs) Debug.Log("Braille Dot 5");
            OnDot5?.Invoke();
        }

        if (Input.GetKeyDown(dot6Key))
        {
            if (logInputs) Debug.Log("Braille Dot 6");
            OnDot6?.Invoke();
        }
    }

    private void CheckActionInputs()
    {
        if (Input.GetKeyDown(repeatKey))
        {
            if (logInputs) Debug.Log("Repeat");
            OnRepeat?.Invoke();
        }

        if (Input.GetKeyDown(submitKey))
        {
            if (logInputs) Debug.Log("Submit");
            OnSubmit?.Invoke();
        }

        if (Input.GetKeyDown(deleteOrNoKey))
        {
            if (logInputs) Debug.Log("Delete / No");
            OnDeleteOrNo?.Invoke();
        }

        if (Input.GetKeyDown(yesOrNextKey))
        {
            if (logInputs) Debug.Log("Yes / Next");
            OnYesOrNext?.Invoke();
        }

        if (Input.GetKeyDown(loginKey))
        {
            if (logInputs) Debug.Log("Login");
            OnLogin?.Invoke();
        }
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