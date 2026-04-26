using UnityEngine;

public class AlphabetSounds_InputHandler : MonoBehaviour
{
    [Header("Reference")]
    public AlphabetSounds_Script alphabetSounds;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += HandleNextOrYes;
        BrailleMapping.OnBack += HandleBack;
        BrailleMapping.OnRepeat += HandleRepeat;
        BrailleMapping.OnDeleteOrNo += HandleNoOrEnd;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= HandleNextOrYes;
        BrailleMapping.OnBack -= HandleBack;
        BrailleMapping.OnRepeat -= HandleRepeat;
        BrailleMapping.OnDeleteOrNo -= HandleNoOrEnd;
    }

    private void Start()
    {
        if (alphabetSounds == null)
        {
            alphabetSounds = FindFirstObjectByType<AlphabetSounds_Script>();
        }

        if (alphabetSounds == null)
        {
            Debug.LogWarning("AlphabetSounds_InputHandler could not find AlphabetSounds_Script in the scene.");
        }
    }

    private void HandleNextOrYes()
    {
        if (alphabetSounds == null) return;
        alphabetSounds.NextLetterOrConfirmYes();
    }

    private void HandleBack()
    {
        if (alphabetSounds == null) return;
        alphabetSounds.PreviousLetter();
    }

    private void HandleRepeat()
    {
        if (alphabetSounds == null) return;
        alphabetSounds.RepeatCurrent();
    }

    private void HandleNoOrEnd()
    {
        if (alphabetSounds == null) return;
        alphabetSounds.NoOrEndLesson();
    }
}