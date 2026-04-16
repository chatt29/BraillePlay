using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LockableUIButton : MonoBehaviour
{
    [Header("Unlock Key")]
    public string unlockKey;

    [Header("References")]
    public Button targetButton;
    public Image targetImage;
    public TMP_Text targetText;

    [Header("Locked Visuals")]
    public Color lockedButtonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color lockedTextColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Unlocked Visuals")]
    public Color unlockedButtonColor = Color.white;
    public Color unlockedTextColor = Color.black;

    [Header("Optional Lock Icon")]
    public GameObject lockIcon;

    private bool isUnlocked;

    private void Start()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        isUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;

        if (targetButton != null)
            targetButton.interactable = isUnlocked;

        if (targetImage != null)
            targetImage.color = isUnlocked ? unlockedButtonColor : lockedButtonColor;

        if (targetText != null)
            targetText.color = isUnlocked ? unlockedTextColor : lockedTextColor;

        if (lockIcon != null)
            lockIcon.SetActive(!isUnlocked);
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}