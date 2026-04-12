using UnityEngine;
using TMPro;

public class Letter_to_Braille_InputHandler : MonoBehaviour
{
    [Header("References")]
    public Letter_to_Braille_Script lessonScript;
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