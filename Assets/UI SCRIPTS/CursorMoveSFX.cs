using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CursorMoveSFX : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button[] buttons;

    [Header("Navigation Keys")]
    [SerializeField] private KeyCode nextKey = KeyCode.Y;
    [SerializeField] private KeyCode previousKey = KeyCode.Backspace;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveSound;

    [Header("Start Settings")]
    [SerializeField] private int startIndex = 0;

    private int currentIndex;

    private void Start()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("StandaloneMenuNavigator: No buttons assigned.");
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, buttons.Length - 1);
        SelectCurrentButton();
    }

    private void Update()
    {
        if (buttons == null || buttons.Length == 0)
            return;

        if (Input.GetKeyDown(nextKey))
        {
            MoveNext();
        }
        else if (Input.GetKeyDown(previousKey))
        {
            MovePrevious();
        }
    }

    private void MoveNext()
    {
        currentIndex++;

        if (currentIndex >= buttons.Length)
            currentIndex = 0;

        SelectCurrentButton();
        PlayMoveSound();
    }

    private void MovePrevious()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = buttons.Length - 1;

        SelectCurrentButton();
        PlayMoveSound();
    }

    private void SelectCurrentButton()
    {
        if (buttons[currentIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(buttons[currentIndex].gameObject);
        }
    }

    private void PlayMoveSound()
    {
        if (audioSource != null && moveSound != null)
        {
            audioSource.PlayOneShot(moveSound);
        }
    }
}