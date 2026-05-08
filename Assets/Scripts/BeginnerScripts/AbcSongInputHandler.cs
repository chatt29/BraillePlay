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

    private void Update()
    {
        if (abcSongScript == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha7))
            abcSongScript.SetAudioSpeed(1.0f);

        if (Input.GetKeyDown(KeyCode.Alpha8))
            abcSongScript.SetAudioSpeed(1.25f);

        if (Input.GetKeyDown(KeyCode.Alpha9))
            abcSongScript.SetAudioSpeed(1.5f);

        if (Input.GetKeyDown(KeyCode.Alpha0))
            abcSongScript.SetAudioSpeed(1.75f);

        if (Input.GetKeyDown(KeyCode.Minus))
            abcSongScript.SetAudioSpeed(2.0f);
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