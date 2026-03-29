using System.Collections;
using TMPro;
using UnityEngine;

public class TypingAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text targetText;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private bool playOnStart = false;
    [TextArea(2, 5)]
    [SerializeField] private string defaultMessage = "";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private int soundFrequency = 2;
    [SerializeField] private bool ignoreSpacesForSound = true;

    private Coroutine typingCoroutine;
    private string fullText = "";
    private bool isTyping = false;

    public bool IsTyping => isTyping;
    public string FullText => fullText;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (targetText != null)
            targetText.text = "";

        if (playOnStart && !string.IsNullOrEmpty(defaultMessage))
        {
            PlayText(defaultMessage);
        }
    }

    public void PlayText(string newText)
    {
        if (targetText == null)
        {
            Debug.LogWarning("TypewriterText: No TMP_Text assigned.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        fullText = newText ?? "";
        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    public void SetTextInstant(string newText)
    {
        if (targetText == null)
        {
            Debug.LogWarning("TypewriterText: No TMP_Text assigned.");
            return;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        fullText = newText ?? "";
        targetText.text = fullText;
        isTyping = false;
        typingCoroutine = null;
    }

    public void SkipTyping()
    {
        if (targetText == null || !isTyping)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        targetText.text = fullText;
        isTyping = false;
        typingCoroutine = null;
    }

    private IEnumerator TypeTextRoutine()
    {
        isTyping = true;
        targetText.text = "";

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            typingCoroutine = null;
            yield break;
        }

        float delay = 1f / Mathf.Max(1f, charactersPerSecond);
        int visibleCount = 0;
        int soundCounter = 0;

        while (visibleCount < fullText.Length)
        {
            visibleCount++;
            targetText.text = fullText.Substring(0, visibleCount);

            char currentChar = fullText[visibleCount - 1];
            bool countForSound = !(ignoreSpacesForSound && char.IsWhiteSpace(currentChar));

            if (countForSound)
            {
                soundCounter++;

                if (typingSound != null &&
                    audioSource != null &&
                    soundFrequency > 0 &&
                    soundCounter % soundFrequency == 0)
                {
                    audioSource.PlayOneShot(typingSound);
                }
            }

            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
        typingCoroutine = null;
    }
}