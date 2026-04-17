using UnityEngine;
using TMPro;

public class CommonContractionInputHandler : MonoBehaviour
{
    [Header("References")]
    public BrailleLessonSceneController lessonScript;
    public TMP_Text livePatternText;

    [Header("Options")]
    public bool showHeldDotsPattern = true;

    private void Update()
    {
        if (!showHeldDotsPattern || livePatternText == null || BrailleMapping.Instance == null)
            return;

        livePatternText.text = BrailleMapping.Instance.GetCurrentBraillePattern();
    }
}