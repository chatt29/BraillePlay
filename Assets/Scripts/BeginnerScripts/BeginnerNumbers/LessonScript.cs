using UnityEngine;
using TMPro;

public class BrailleLessonInputHandler : MonoBehaviour
{
    [Header("References")]
    public BrailleLessonSceneController lessonScript;
    public TMP_Text livePatternText;
    public AudioSource voiceAudioSource;
    public WirelessHaptics wirelessHaptics;

    [Header("Options")]
    public bool showHeldDotsPattern = true;

    [Header("Audio Speed Settings")]
    public float speedLevel1 = 0.75f;
    public float speedLevel2 = 0.9f;
    public float speedLevel3 = 1.0f;
    public float speedLevel4 = 1.25f;
    public float speedLevel5 = 1.5f;

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleSubmitted;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleSubmitted;
    }

    private void Update()
    {
        HandleAudioSpeedControl();

        if (!showHeldDotsPattern || livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }

    private void HandleAudioSpeedControl()
    {
        if (voiceAudioSource == null)
            return;

        // ESP32 sends these keys:
        // 7 = slowest
        // 8 = slow
        // 9 = normal
        // 0 = fast
        // - = fastest

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SetAudioSpeed(speedLevel1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetAudioSpeed(speedLevel2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SetAudioSpeed(speedLevel3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetAudioSpeed(speedLevel4);
        }
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            SetAudioSpeed(speedLevel5);
        }
    }

    private void SetAudioSpeed(float speed)
    {
        voiceAudioSource.pitch = Mathf.Clamp(speed, 0.5f, 2f);

        Debug.Log("Audio speed set to: " + voiceAudioSource.pitch);
    }

    private void HandleBrailleSubmitted(string submittedPattern)
    {
        if (lessonScript == null || wirelessHaptics == null)
            return;

        BrailleLessonSceneController.BrailleLesson lesson = lessonScript.GetCurrentLesson();

        if (lesson == null)
            return;

        bool isCorrect = false;

        if (lesson.lessonKind == BrailleLessonSceneController.LessonKind.SymbolOnly)
        {
            string expectedPattern = BrailleLessonSceneController.PatternFromDots(lesson.dots);
            isCorrect = submittedPattern == expectedPattern;
        }
        else
        {
            int currentStep = lessonScript.GetCurrentSequenceStep();

            if (lesson.expectedSequencePatterns != null &&
                currentStep >= 0 &&
                currentStep < lesson.expectedSequencePatterns.Count)
            {
                isCorrect = submittedPattern == lesson.expectedSequencePatterns[currentStep];
            }
        }

        if (!isCorrect)
        {
            wirelessHaptics.TriggerHaptic();
        }
    }
}