using UnityEngine;

public class AbcSongInputHandler : MonoBehaviour
{
    public AbcSongScript abcSongScript;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += Next;
        BrailleMapping.OnRepeat += Repeat;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= Next;
        BrailleMapping.OnRepeat -= Repeat;
    }

    void Next()
    {
        abcSongScript.Next();
    }

    void Repeat()
    {
        abcSongScript.Repeat();
    }
}