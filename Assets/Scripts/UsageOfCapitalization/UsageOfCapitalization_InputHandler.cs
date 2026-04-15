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
        if (capitalizationLesson == null) return;
        capitalizationLesson.NextOrConfirmYes();
    }

    private void HandleBack()
    {
        if (capitalizationLesson == null) return;
        capitalizationLesson.GoBackOrRestartPrompt();
    }

    private void HandleRepeat()
    {
        if (capitalizationLesson == null) return;
        capitalizationLesson.RepeatCurrent();
    }

    private void HandleNoOrEnd()
    {
        if (capitalizationLesson == null) return;
        capitalizationLesson.NoOrEndLesson();
    }

    private void HandleBrailleInput(string pattern)
    {
        if (capitalizationLesson == null) return;
        capitalizationLesson.HandleBrailleInput(pattern);
    }
}