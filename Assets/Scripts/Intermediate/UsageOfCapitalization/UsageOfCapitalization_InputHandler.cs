using UnityEngine;

public class UsageOfCapitalization_InputHandler : MonoBehaviour
{
    [Header("Reference")]
    public UsageOfCapitalization_Script capitalizationLesson;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += HandleNextOrYes;
        BrailleMapping.OnBack += HandleBack;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnDeleteOrNo += HandleNoOrEnd;
        BrailleMapping.OnBrailleChordSubmitted += HandleBrailleInput;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= HandleNextOrYes;
        BrailleMapping.OnBack -= HandleBack;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnDeleteOrNo -= HandleNoOrEnd;
        BrailleMapping.OnBrailleChordSubmitted -= HandleBrailleInput;
    }

    private void Start()
    {
        if (capitalizationLesson == null)
            capitalizationLesson = FindObjectOfType<UsageOfCapitalization_Script>();
    }

    private void HandleNextOrYes()
    {
        Debug.Log("INPUT HANDLER: YES/NEXT event fired");
        if (capitalizationLesson == null) return;
        capitalizationLesson.NextOrConfirmYes();
    }

    private void HandleBack()
    {
        Debug.Log("INPUT HANDLER: BACK event fired");
        if (capitalizationLesson == null) return;
        capitalizationLesson.GoBackOrRestartPrompt();
    }

    private void HandleRepeat()
    {
        Debug.Log("INPUT HANDLER: REPEAT event fired");
        if (capitalizationLesson == null) return;
        capitalizationLesson.RepeatCurrent();
    }

    private void HandleNoOrEnd()
    {
        Debug.Log("INPUT HANDLER: NO/DELETE event fired");
        if (capitalizationLesson == null) return;
        capitalizationLesson.NoOrEndLesson();
    }

    private void HandleBrailleInput(string pattern)
    {
        Debug.Log("INPUT HANDLER: BRAILLE pattern = " + pattern);
        if (capitalizationLesson == null) return;
        capitalizationLesson.HandleBrailleInput(pattern);
    }
}