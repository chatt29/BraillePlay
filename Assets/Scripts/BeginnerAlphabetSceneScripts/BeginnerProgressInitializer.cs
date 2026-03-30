using UnityEngine;

public class BeginnerProgressInitializer : MonoBehaviour
{
    private void Awake()
    {
        SetDefaultIfMissing(GameProgressKeys.ABCSongUnlocked, 1);

        SetDefaultIfMissing(GameProgressKeys.ABCSoundsUnlocked, 0);
        SetDefaultIfMissing(GameProgressKeys.LetterToBrailleUnlocked, 0);

        SetDefaultIfMissing(GameProgressKeys.QuizUnlocked, 0);
        SetDefaultIfMissing(GameProgressKeys.JumbleLettersUnlocked, 0);
        SetDefaultIfMissing(GameProgressKeys.BasicWordsUnlocked, 0);

        PlayerPrefs.Save();
    }

    private void SetDefaultIfMissing(string key, int defaultValue)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, defaultValue);
        }
    }
}