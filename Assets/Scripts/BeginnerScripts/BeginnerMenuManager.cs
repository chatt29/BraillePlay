using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BeginnerMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform selectorArrow;
    public Button[] buttons;
    public TMP_Text dialogueText;

    [Header("INTRO")]
    [TextArea]
    public string introMessage;

    public AudioClip introClip;

    [Header("Dialogue Messages")]
    [TextArea]
    public string[] messages;

    [Header("Dialogue Audio Clips")]
    public AudioClip[] dialogueClips;

    [Header("Navigation Sound")]
    public AudioClip moveSound;

    [Header("Audio Sources")]
    public AudioSource dialogueAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Selector Settings")]
    public float offsetX = -60f;

    private int currentIndex = 0;
    private Vector2 targetPos;

    private bool introFinished = false;

    void Start()
    {
        Canvas.ForceUpdateCanvases();

        RectTransform btnRect = buttons[currentIndex].GetComponent<RectTransform>();
        RectTransform arrowParent = selectorArrow.parent as RectTransform;

        Vector3 worldPos = btnRect.position;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            arrowParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out localPoint
        );

        targetPos = new Vector2(localPoint.x + offsetX, localPoint.y);

        selectorArrow.anchoredPosition = targetPos;

        // Start intro first
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        // Show intro text
        dialogueText.text = introMessage;

        // Play intro audio
        if (introClip != null)
        {
            dialogueAudioSource.clip = introClip;
            dialogueAudioSource.Play();

            // Wait until intro audio finishes
            yield return new WaitForSeconds(introClip.length);
        }

        introFinished = true;

        // After intro, load first menu dialogue
        UpdateSelection();
    }

    void OnEnable()
    {
        BrailleMapping.OnYesOrNext += MoveDown;
        BrailleMapping.OnDeleteOrNo += MoveUp;
        BrailleMapping.OnSubmit += ActivateButton;
        BrailleMapping.OnLogin += ActivateButton;
        BrailleMapping.OnRepeat += RepeatDialogue;
        BrailleMapping.OnBack += GoBack;
    }

    void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= MoveDown;
        BrailleMapping.OnDeleteOrNo -= MoveUp;
        BrailleMapping.OnSubmit -= ActivateButton;
        BrailleMapping.OnLogin -= ActivateButton;
        BrailleMapping.OnRepeat -= RepeatDialogue;
        BrailleMapping.OnBack -= GoBack;
    }

    void Update()
    {
        selectorArrow.anchoredPosition = Vector2.Lerp(
            selectorArrow.anchoredPosition,
            targetPos,
            10f * Time.deltaTime
        );
    }

    // 🔽 Move Down
    void MoveDown()
    {
        // Prevent movement during intro
        if (!introFinished) return;

        currentIndex++;

        if (currentIndex >= buttons.Length)
            currentIndex = 0;

        PlayMoveSound();
        UpdateSelection();
    }

    // 🔼 Move Up
    void MoveUp()
    {
        // Prevent movement during intro
        if (!introFinished) return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = buttons.Length - 1;

        PlayMoveSound();
        UpdateSelection();
    }

    // 🎯 Update Arrow + Dialogue + Audio
    void UpdateSelection()
    {
        RectTransform btnRect = buttons[currentIndex].GetComponent<RectTransform>();
        RectTransform arrowParent = selectorArrow.parent as RectTransform;

        Vector3 worldPos = btnRect.position;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            arrowParent,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null,
            out localPoint
        );

        targetPos = new Vector2(localPoint.x + offsetX, localPoint.y);

        // Update dialogue text
        if (messages.Length > currentIndex)
        {
            dialogueText.text = messages[currentIndex];
        }

        // Play dialogue voice/audio
        PlayDialogueAudio();
    }

    // ▶️ Play dialogue audio
    void PlayDialogueAudio()
    {
        if (dialogueClips.Length > currentIndex &&
            dialogueClips[currentIndex] != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = dialogueClips[currentIndex];
            dialogueAudioSource.Play();
        }
    }

    // 🔊 Play move/toggle sound
    void PlayMoveSound()
    {
        if (moveSound != null)
        {
            sfxAudioSource.PlayOneShot(moveSound);
        }
    }

    // ✅ Activate Button
    void ActivateButton()
    {
        // Prevent activation during intro
        if (!introFinished) return;

        switch (currentIndex)
        {
            case 0:
                SceneManager.LoadScene("BeginnerAlphabetScene");
                break;

            case 1:
                SceneManager.LoadScene("NumbersScene");
                break;
        }
    }

    // 🔁 Repeat dialogue
    void RepeatDialogue()
    {
        if (!introFinished) return;

        if (messages.Length > currentIndex)
        {
            dialogueText.text = messages[currentIndex];
        }

        PlayDialogueAudio();

        Debug.Log("Repeat: " + messages[currentIndex]);
    }

    // 🔙 Back
    void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // 🖱️ Optional UI Button Support
    public void SelectButton(int index)
    {
        if (!introFinished) return;

        currentIndex = index;

        PlayMoveSound();
        UpdateSelection();
        ActivateButton();
    }
}