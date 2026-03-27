using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterDialogue : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string message;
        public AudioClip voiceClip;
    }

    [Header("References")]
    public TextMeshProUGUI dialogueText;
    public AudioSource voiceAudioSource;

    [Header("Dialogue Sequence")]
    public DialogueLine[] dialogueLines;

    [Header("Typing Settings")]
    public float typingSpeed = 0.04f;
    public float delayBeforeNextLine = 1.0f;
    public bool clearBeforeNextLine = true;

    private Coroutine dialogueCoroutine;
    private bool isTyping;
    private bool lineFinished;
    private int currentLineIndex;

    void Start()
    {
        if (dialogueText == null)
        {
            Debug.LogError("TypewriterDialogue: dialogueText is not assigned.");
            return;
        }

        dialogueText.text = "";
        dialogueText.ForceMeshUpdate();

        isTyping = false;
        lineFinished = false;
        currentLineIndex = 0;
    }

    public void StartTyping()
    {
        if (dialogueText == null)
        {
            Debug.LogError("TypewriterDialogue: dialogueText is not assigned.");
            return;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("TypewriterDialogue: no dialogue lines assigned.");
            return;
        }

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        currentLineIndex = 0;
        dialogueCoroutine = StartCoroutine(PlayDialogueSequence());
    }

    private IEnumerator PlayDialogueSequence()
    {
        dialogueText.gameObject.SetActive(true);
        dialogueText.enabled = true;

        for (currentLineIndex = 0; currentLineIndex < dialogueLines.Length; currentLineIndex++)
        {
            DialogueLine currentLine = dialogueLines[currentLineIndex];

            if (string.IsNullOrEmpty(currentLine.message))
                continue;

            if (clearBeforeNextLine)
            {
                dialogueText.text = "";
                dialogueText.ForceMeshUpdate();
            }

            if (voiceAudioSource != null)
            {
                voiceAudioSource.Stop();

                if (currentLine.voiceClip != null)
                {
                    voiceAudioSource.clip = currentLine.voiceClip;
                    voiceAudioSource.Play();
                }
            }

            isTyping = true;
            lineFinished = false;

            for (int i = 0; i < currentLine.message.Length; i++)
            {
                dialogueText.text += currentLine.message[i];
                dialogueText.ForceMeshUpdate();
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            lineFinished = true;

            if (currentLineIndex < dialogueLines.Length - 1)
            {
                yield return new WaitForSeconds(delayBeforeNextLine);

                if (clearBeforeNextLine)
                {
                    dialogueText.text = "";
                    dialogueText.ForceMeshUpdate();
                }
            }
        }

        dialogueCoroutine = null;
    }

    public void FinishInstantly()
    {
        if (dialogueText == null || dialogueLines == null || dialogueLines.Length == 0)
            return;

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLineIndex].message;
            dialogueText.ForceMeshUpdate();
        }

        isTyping = false;
        lineFinished = true;
    }

    public void ClearText()
    {
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.ForceMeshUpdate();
        }

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        isTyping = false;
        lineFinished = false;
        currentLineIndex = 0;
    }

    public bool IsTyping()
    {
        return isTyping;
    }

    public bool IsLineFinished()
    {
        return lineFinished;
    }
}