using System;
using UnityEngine;
using UnityEngine.UI;

public class AbcFlowInput : MonoBehaviour
{
    public event Action<string> OnAnswerSubmitted;

    [Header("Dot UI Images")]
    public Image dot1Image;
    public Image dot2Image;
    public Image dot3Image;
    public Image dot4Image;
    public Image dot5Image;
    public Image dot6Image;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color pressedColor = Color.black;

    [Header("Keys")]
    public KeyCode dot1Key = KeyCode.J;
    public KeyCode dot2Key = KeyCode.K;
    public KeyCode dot3Key = KeyCode.L;
    public KeyCode dot4Key = KeyCode.F;
    public KeyCode dot5Key = KeyCode.D;
    public KeyCode dot6Key = KeyCode.S;

    private bool inputEnabled;

    private bool chordStarted;
    private bool dot1;
    private bool dot2;
    private bool dot3;
    private bool dot4;
    private bool dot5;
    private bool dot6;

    private void Start()
    {
        SetInputEnabled(false);
    }

    private void Update()
    {
        if (!inputEnabled)
        {
            ResetDotVisuals();
            return;
        }

        UpdateDotVisuals();
        ReadDotInput();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            ResetChord();
            ResetDotVisuals();
        }
    }

    private void ReadDotInput()
    {
        if (Input.GetKeyDown(dot1Key))
        {
            chordStarted = true;
            dot1 = true;
        }

        if (Input.GetKeyDown(dot2Key))
        {
            chordStarted = true;
            dot2 = true;
        }

        if (Input.GetKeyDown(dot3Key))
        {
            chordStarted = true;
            dot3 = true;
        }

        if (Input.GetKeyDown(dot4Key))
        {
            chordStarted = true;
            dot4 = true;
        }

        if (Input.GetKeyDown(dot5Key))
        {
            chordStarted = true;
            dot5 = true;
        }

        if (Input.GetKeyDown(dot6Key))
        {
            chordStarted = true;
            dot6 = true;
        }

        bool anyReleased =
            Input.GetKeyUp(dot1Key) ||
            Input.GetKeyUp(dot2Key) ||
            Input.GetKeyUp(dot3Key) ||
            Input.GetKeyUp(dot4Key) ||
            Input.GetKeyUp(dot5Key) ||
            Input.GetKeyUp(dot6Key);

        bool anyStillHeld =
            Input.GetKey(dot1Key) ||
            Input.GetKey(dot2Key) ||
            Input.GetKey(dot3Key) ||
            Input.GetKey(dot4Key) ||
            Input.GetKey(dot5Key) ||
            Input.GetKey(dot6Key);

        if (chordStarted && anyReleased && !anyStillHeld)
        {
            SubmitAnswer();
        }
    }

    private void SubmitAnswer()
    {
        string pattern =
            (dot1 ? "1" : "0") +
            (dot2 ? "1" : "0") +
            (dot3 ? "1" : "0") +
            (dot4 ? "1" : "0") +
            (dot5 ? "1" : "0") +
            (dot6 ? "1" : "0");

        OnAnswerSubmitted?.Invoke(pattern);

        ResetChord();
        ResetDotVisuals();
    }

    private void ResetChord()
    {
        chordStarted = false;

        dot1 = false;
        dot2 = false;
        dot3 = false;
        dot4 = false;
        dot5 = false;
        dot6 = false;
    }

    private void UpdateDotVisuals()
    {
        SetDotColor(dot1Image, Input.GetKey(dot1Key));
        SetDotColor(dot2Image, Input.GetKey(dot2Key));
        SetDotColor(dot3Image, Input.GetKey(dot3Key));
        SetDotColor(dot4Image, Input.GetKey(dot4Key));
        SetDotColor(dot5Image, Input.GetKey(dot5Key));
        SetDotColor(dot6Image, Input.GetKey(dot6Key));
    }

    private void ResetDotVisuals()
    {
        SetDotColor(dot1Image, false);
        SetDotColor(dot2Image, false);
        SetDotColor(dot3Image, false);
        SetDotColor(dot4Image, false);
        SetDotColor(dot5Image, false);
        SetDotColor(dot6Image, false);
    }

    private void SetDotColor(Image image, bool pressed)
    {
        if (image == null)
            return;

        image.color = pressed ? pressedColor : normalColor;
    }
}