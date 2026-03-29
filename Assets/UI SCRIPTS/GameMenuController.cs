using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class GameMenuController : MonoBehaviour
{
    [System.Serializable]
    public class MenuOption
    {
        public Button button;

        [TextArea(2, 5)]
        public string dialogueText;

        public AudioClip optionAudio;
    }

    [Header("Menu Options")]
    [SerializeField] private MenuOption[] options;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text dialogueTextUI;
    [SerializeField] private TypingAnimation dialogueTyper;
    [SerializeField] private string welcomeMessage = "Welcome to BraillePlay!";
    [SerializeField] private AudioClip welcomeAudio;

    [Header("Indicator")]
    [SerializeField] private RectTransform indicator;
    [SerializeField] private float indicatorPadding = 20f;
    [SerializeField] private bool placeIndicatorOnLeft = false;

    [Header("Navigation Keys")]
    [SerializeField] private KeyCode nextKey = KeyCode.Y;
    [SerializeField] private KeyCode previousKey = KeyCode.Backspace;
    [SerializeField] private KeyCode confirmKey = KeyCode.Return;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cursorMoveAudio;

    [Header("Start Settings")]
    [SerializeField] private int startIndex = 0;

    private int currentIndex = 0;
    private bool hasStartedBrowsing = false;
    private bool canBrowse = false;
    private RectTransform currentTarget;

    public bool HasStartedBrowsing()
    {
        return hasStartedBrowsing;
    }

    public bool IsCurrentButton(Button button)
    {
        if (options == null || options.Length == 0 || button == null)
            return false;

        return options[currentIndex].button == button;
    }

    private void Start()
    {
        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("GameMenuController: No options assigned.");
            return;
        }

        currentIndex = Mathf.Clamp(startIndex, 0, options.Length - 1);

        UIHover[] hoverScripts = FindObjectsByType<UIHover>(FindObjectsSortMode.None);
        foreach (UIHover hover in hoverScripts)
        {
            hover.ResetVisual();
        }

        if (indicator != null)
            indicator.gameObject.SetActive(false);

        SetDialogue(welcomeMessage);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        StartCoroutine(WelcomeSequence());
    }

    private IEnumerator WelcomeSequence()
    {
        if (audioSource != null && welcomeAudio != null)
        {
            audioSource.PlayOneShot(welcomeAudio);
            yield return new WaitForSeconds(welcomeAudio.length);
        }

        canBrowse = true;
    }

    private void Update()
    {
        if (options == null || options.Length == 0 || !canBrowse)
            return;

        if (Input.GetKeyDown(nextKey))
        {
            if (!hasStartedBrowsing)
            {
                hasStartedBrowsing = true;
                ActivateMenuFromWelcome();
            }
            else
            {
                Move(1);
            }
        }
        else if (Input.GetKeyDown(previousKey))
        {
            if (hasStartedBrowsing)
            {
                Move(-1);
            }
        }
        else if (Input.GetKeyDown(confirmKey))
        {
            if (dialogueTyper != null && dialogueTyper.IsTyping)
            {
                dialogueTyper.SkipTyping();
                return;
            }

            if (hasStartedBrowsing && options[currentIndex].button != null)
            {
                options[currentIndex].button.onClick.Invoke();
            }
        }

        if (hasStartedBrowsing && currentTarget != null)
        {
            MoveIndicatorTo(currentTarget);
        }
    }

    private void ActivateMenuFromWelcome()
    {
        if (indicator != null)
            indicator.gameObject.SetActive(true);

        ApplyCurrentOption(playMoveSound: true, playOptionAudio: true);
    }

    private void Move(int direction)
    {
        currentIndex += direction;

        if (currentIndex >= options.Length)
            currentIndex = 0;
        else if (currentIndex < 0)
            currentIndex = options.Length - 1;

        ApplyCurrentOption(playMoveSound: true, playOptionAudio: true);
    }

    private void ApplyCurrentOption(bool playMoveSound, bool playOptionAudio)
    {
        Button currentButton = options[currentIndex].button;
        if (currentButton == null)
            return;

        currentTarget = currentButton.GetComponent<RectTransform>();

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != currentButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(currentButton.gameObject);
        }

        MoveIndicatorTo(currentTarget);
        SetDialogue(options[currentIndex].dialogueText);

        if (audioSource != null)
        {
            if (playMoveSound && cursorMoveAudio != null)
                audioSource.PlayOneShot(cursorMoveAudio);

            if (playOptionAudio && options[currentIndex].optionAudio != null)
                audioSource.PlayOneShot(options[currentIndex].optionAudio);
        }
    }

    public void SyncFromHoveredButton(Button hoveredButton)
    {
        if (!canBrowse || !hasStartedBrowsing || hoveredButton == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i].button == hoveredButton)
            {
                if (currentIndex != i)
                {
                    currentIndex = i;
                    ApplyCurrentOption(playMoveSound: false, playOptionAudio: false);
                }
                else
                {
                    currentTarget = hoveredButton.GetComponent<RectTransform>();
                    MoveIndicatorTo(currentTarget);
                    SetDialogue(options[currentIndex].dialogueText);
                }

                return;
            }
        }
    }

    private void SetDialogue(string message)
    {
        if (dialogueTyper != null)
        {
            dialogueTyper.PlayText(message);
        }
        else if (dialogueTextUI != null)
        {
            dialogueTextUI.text = message;
        }
    }

    private void MoveIndicatorTo(RectTransform target)
    {
        if (indicator == null || target == null)
            return;

        RectTransform indicatorParent = indicator.parent as RectTransform;
        RectTransform targetParent = target.parent as RectTransform;

        if (indicatorParent == null)
            return;

        if (targetParent == null || indicatorParent != targetParent)
        {
            Vector3 worldPos = target.TransformPoint(target.rect.center);
            Vector3 localPos = indicatorParent.InverseTransformPoint(worldPos);

            float targetWidth = target.rect.width * target.lossyScale.x;
            float indicatorWidth = indicator.rect.width * indicator.lossyScale.x;

            float xOffset = (targetWidth * 0.5f) + (indicatorWidth * 0.5f) + indicatorPadding;
            float x = placeIndicatorOnLeft ? localPos.x - xOffset : localPos.x + xOffset;

            indicator.localPosition = new Vector3(x, localPos.y, indicator.localPosition.z);
            return;
        }

        float targetWidthLocal = target.rect.width * target.localScale.x;
        float xOffsetLocal = (targetWidthLocal * 0.5f) + (indicator.rect.width * 0.5f) + indicatorPadding;

        float xPos = placeIndicatorOnLeft
            ? target.anchoredPosition.x - xOffsetLocal
            : target.anchoredPosition.x + xOffsetLocal;

        indicator.anchoredPosition = new Vector2(xPos, target.anchoredPosition.y);
    }
}