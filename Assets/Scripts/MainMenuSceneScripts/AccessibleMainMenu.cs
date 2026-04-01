using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessibleMenuController : MonoBehaviour
{
    [System.Serializable]
    public class IntroMessageStep
    {
        [TextArea(2, 4)]
        public string message;
        public AudioClip voiceClip;
        public bool useTypewriter = true;
    }

    [System.Serializable]
    public class MenuItem
    {
        public string itemName;
        public Button button;

        [TextArea(2, 4)]
        public string instructionMessage;

        public AudioClip voiceClip;
    }

    [Header("Speech Bubble UI")]
    public TMP_Text speechBubbleText;

    [Header("Audio")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Intro Sequence")]
    public List<IntroMessageStep> introMessages = new List<IntroMessageStep>();

    [Header("Menu Items")]
    public List<MenuItem> menuItems = new List<MenuItem>();

    [Header("Optional Menu SFX")]
    public AudioClip moveSelectionSfx;
    public AudioClip confirmSelectionSfx;

    [Header("Visual Highlight")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.95f, 0.6f);
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);

    [Header("Typewriter Settings")]
    [Range(0.01f, 0.2f)]
    public float typewriterSpeed = 0.04f;
    public float messageHoldTime = 1.5f;
    public bool skipTypewriterWhenRepeatingSelection = false;

    [Header("Timing")]
    public float pauseBetweenMessages = 0.75f;
    public float minimumMessageTime = 3f;

    private int currentIndex = 0;
    private bool menuReady = false;
    private bool introPlaying = false;

    private Coroutine textCoroutine;
    private Coroutine introCoroutine;
    private Coroutine selectionMessageCoroutine;

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += NextOption;
        BrailleMapping.OnDeleteOrNo += PreviousOption;
        BrailleMapping.OnSubmit += SelectOption;
        BrailleMapping.OnRepeat += RepeatCurrentMessage;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= NextOption;
        BrailleMapping.OnDeleteOrNo -= PreviousOption;
        BrailleMapping.OnSubmit -= SelectOption;
        BrailleMapping.OnRepeat -= RepeatCurrentMessage;
    }

    private void Start()
    {
        SetButtonsInteractable(false);
        ResetVisuals();

        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(PlayIntroSequence());
    }

    private IEnumerator PlayIntroSequence()
    {
        introPlaying = true;
        menuReady = false;

        for (int i = 0; i < introMessages.Count; i++)
        {
            IntroMessageStep step = introMessages[i];
            if (step == null) continue;

            // 👉 Delay ONLY before the first message
            if (i == 0)
                yield return new WaitForSeconds(1.2f); // adjust this value

            yield return ShowMessageAndPlayAudio(step.message, step.voiceClip, step.useTypewriter);

            if (i < introMessages.Count - 1 && pauseBetweenMessages > 0f)
                yield return new WaitForSeconds(pauseBetweenMessages);
        }

        introPlaying = false;
        menuReady = true;

        SetButtonsInteractable(true);

        if (menuItems.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, menuItems.Count - 1);
            UpdateSelection(true);
        }
    }

    private IEnumerator ShowMessageAndPlayAudio(string message, AudioClip clip, bool useTypewriter)
    {
        if (selectionMessageCoroutine != null)
            StopCoroutine(selectionMessageCoroutine);

        selectionMessageCoroutine = StartCoroutine(DisplayMessage(message, useTypewriter));

        float audioDuration = 0f;
        if (voiceAudioSource != null && clip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            audioDuration = clip.length;
        }

        float textDuration = useTypewriter ? GetEstimatedReadTime(message) : messageHoldTime;
        float waitTime = Mathf.Max(textDuration, audioDuration, 0.1f);

        yield return new WaitForSeconds(waitTime);
    }
    private IEnumerator DisplayMessage(string message, bool useTypewriter)
    {
        if (speechBubbleText == null)
            yield break;

        if (textCoroutine != null)
            StopCoroutine(textCoroutine);

        if (!useTypewriter)
        {
            speechBubbleText.text = message;
            yield return new WaitForSeconds(messageHoldTime);
            yield break;
        }

        bool finishedTyping = false;
        textCoroutine = StartCoroutine(TypeText(message, () => finishedTyping = true));

        while (!finishedTyping)
            yield return null;

        yield return new WaitForSeconds(messageHoldTime);
    }

    private IEnumerator TypeText(string message, System.Action onComplete = null)
    {
        if (speechBubbleText == null)
            yield break;

        speechBubbleText.text = "";

        foreach (char c in message)
        {
            speechBubbleText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        onComplete?.Invoke();
    }

    private float GetEstimatedReadTime(string message)
    {
        if (string.IsNullOrEmpty(message))
            return minimumMessageTime;

        float typingDuration = message.Length * typewriterSpeed;
        return typingDuration + messageHoldTime;
    }

    private void NextOption()
    {
        if (!menuReady || introPlaying || menuItems.Count == 0)
            return;

        currentIndex = (currentIndex + 1) % menuItems.Count;
        UpdateSelection();
    }

    private void PreviousOption()
    {
        if (!menuReady || introPlaying || menuItems.Count == 0)
            return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = menuItems.Count - 1;

        UpdateSelection();
    }

    private void SelectOption()
    {
        if (!menuReady || introPlaying || menuItems.Count == 0)
            return;

        PlaySfx(confirmSelectionSfx);

        MenuItem currentItem = menuItems[currentIndex];
        if (currentItem != null && currentItem.button != null)
            currentItem.button.onClick.Invoke();
    }

    private void RepeatCurrentMessage()
    {
        if (!menuReady || introPlaying || menuItems.Count == 0)
            return;

        PlayCurrentItemMessage(true);
    }

    private void UpdateSelection(bool firstSelection = false)
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            MenuItem item = menuItems[i];
            if (item == null || item.button == null)
                continue;

            Image image = item.button.GetComponent<Image>();
            if (image != null)
                image.color = (i == currentIndex) ? selectedColor : normalColor;

            item.button.transform.localScale = (i == currentIndex) ? selectedScale : normalScale;
        }

        if (!firstSelection)
            PlaySfx(moveSelectionSfx);

        PlayCurrentItemMessage(false);
    }

    private void PlayCurrentItemMessage(bool isRepeat)
    {
        if (menuItems.Count == 0)
            return;

        MenuItem currentItem = menuItems[currentIndex];
        if (currentItem == null)
            return;

        bool useTypewriter = !(isRepeat && skipTypewriterWhenRepeatingSelection);

        if (selectionMessageCoroutine != null)
            StopCoroutine(selectionMessageCoroutine);

        selectionMessageCoroutine = StartCoroutine(DisplayMessage(currentItem.instructionMessage, useTypewriter));
        PlayVoiceClip(currentItem.voiceClip);
    }

    private void SetButtonsInteractable(bool value)
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            MenuItem item = menuItems[i];
            if (item != null && item.button != null)
                item.button.interactable = value;
        }
    }

    private void ResetVisuals()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            MenuItem item = menuItems[i];
            if (item == null || item.button == null)
                continue;

            Image image = item.button.GetComponent<Image>();
            if (image != null)
                image.color = normalColor;

            item.button.transform.localScale = normalScale;
        }
    }

    private void PlayVoiceClip(AudioClip clip)
    {
        if (voiceAudioSource != null && clip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
            sfxAudioSource.PlayOneShot(clip);
    }
}