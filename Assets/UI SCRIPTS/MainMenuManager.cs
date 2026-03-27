using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public enum MenuPhase
    {
        Idle,
        StartPressed,
        ChoiceAppearing,
        DialogueAppearing,
        CloudsAppearing,
        WaitingForChoice,
        Transitioning
    }

    [Header("References")]
    public RectTransform cat;
    public RectTransform building;
    public RectTransform titleText;
    public RectTransform startButton;

    public RectTransform yesButton;
    public RectTransform noButton;

    public RectTransform prince;
    public RectTransform speechBubble;

    [Header("Top Clouds")]
    public RectTransform loginCloud;
    public RectTransform signupCloud;
    public float cloudMoveSpeed = 400f;
    public Vector2 loginTargetPos;
    public Vector2 signupTargetPos;

    [Header("Idle Animation Scripts")]
    public UIButtonIdleBounce buttonBounce;
    public UITextFloat titleFloat;

    [Header("Start Transition")]
    public float catMoveSpeed = 300f;
    public float buildingMoveSpeed = 300f;
    public float buttonPopSpeed = 10f;
    public float buttonTargetScale = 1.8f;

    [Header("Title Exit")]
    public float titleExitY = 1800f;
    public float titleMoveSpeed = 400f;

    [Header("Choice Buttons Float Up")]
    public float choiceMoveSpeed = 500f;
    public Vector2 yesTargetPos;
    public Vector2 noTargetPos;

    [Header("Dialogue Slide In")]
    public float dialogueMoveSpeed = 500f;
    public Vector2 princeTargetPos;
    public Vector2 bubbleTargetPos;

    [Header("Scene Names")]
    public string signupSceneName = "SignUp";
    public string loginSceneName = "Login";

    [Header("Scene Transition")]
    public float loadSceneDelay = 0.2f;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip startSFX;
    public AudioClip yesSFX;
    public AudioClip noSFX;
    public AudioClip loginSFX;

    [Header("Dialogue")]
    public TypewriterDialogue typewriterDialogue;

    private MenuPhase currentPhase = MenuPhase.Idle;

    private Vector3 buttonStartScale;
    private bool buttonFinished = false;
    private float timer = 0f;
    private bool dialogueStarted = false;
    private bool inputLocked = false;

    void Start()
    {
        if (startButton != null)
            buttonStartScale = startButton.localScale;

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();

        if (typewriterDialogue != null)
            typewriterDialogue.ClearText();
    }

    void Update()
    {
        HandleInput();

        if (currentPhase == MenuPhase.StartPressed)
        {
            AnimateCat();
            AnimateBuilding();
            AnimateTitle();
            AnimateButtonPop();

            timer += Time.deltaTime;

            if (timer >= 1.0f)
            {
                currentPhase = MenuPhase.ChoiceAppearing;
            }
        }
        else if (currentPhase == MenuPhase.ChoiceAppearing)
        {
            bool yesDone = MoveTowardsUI(yesButton, yesTargetPos, choiceMoveSpeed);
            bool noDone = MoveTowardsUI(noButton, noTargetPos, choiceMoveSpeed);

            if (yesDone && noDone)
            {
                currentPhase = MenuPhase.DialogueAppearing;
            }
        }
        else if (currentPhase == MenuPhase.DialogueAppearing)
        {
            bool princeDone = MoveTowardsUI(prince, princeTargetPos, dialogueMoveSpeed);
            bool bubbleDone = MoveTowardsUI(speechBubble, bubbleTargetPos, dialogueMoveSpeed);

            if (princeDone && bubbleDone)
            {
                currentPhase = MenuPhase.CloudsAppearing;
            }
        }
        else if (currentPhase == MenuPhase.CloudsAppearing)
        {
            bool loginDone = MoveTowardsUI(loginCloud, loginTargetPos, cloudMoveSpeed);
            bool signupDone = MoveTowardsUI(signupCloud, signupTargetPos, cloudMoveSpeed);

            if (loginDone && signupDone)
            {
                if (!dialogueStarted && typewriterDialogue != null)
                {
                    dialogueStarted = true;
                    typewriterDialogue.StartTyping();
                }

                currentPhase = MenuPhase.WaitingForChoice;
            }
        }
    }

    void HandleInput()
    {
        if (inputLocked)
            return;

        if (currentPhase == MenuPhase.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartMenuTransition();

                if (sfxSource != null && startSFX != null)
                    sfxSource.PlayOneShot(startSFX);
            }

            return;
        }

        if (currentPhase == MenuPhase.WaitingForChoice)
        {
            // YES / NEXT -> SignUp
            if (Input.GetKeyDown(KeyCode.Y))
            {
                if (sfxSource != null && yesSFX != null)
                    sfxSource.PlayOneShot(yesSFX);

                LoadSceneAfterDelay(signupSceneName);
                return;
            }

            // NO / DELETE -> Backspace
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                if (sfxSource != null && noSFX != null)
                    sfxSource.PlayOneShot(noSFX);

                // Right now this returns to Idle-style start state by reloading current scene.
                // If you want a different "No" behavior later, change this.
                LoadSceneAfterDelay(SceneManager.GetActiveScene().name);
                return;
            }

            // LOGIN -> Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (sfxSource != null && loginSFX != null)
                    sfxSource.PlayOneShot(loginSFX);

                LoadSceneAfterDelay(loginSceneName);
                return;
            }
        }
    }

    void StartMenuTransition()
    {
        currentPhase = MenuPhase.StartPressed;
        timer = 0f;
        dialogueStarted = false;
        buttonFinished = false;

        if (buttonBounce != null)
            buttonBounce.StopBounce();

        if (titleFloat != null)
            titleFloat.StopFloat();

        if (typewriterDialogue != null)
            typewriterDialogue.ClearText();
    }

    void AnimateCat()
    {
        if (cat == null) return;

        Vector2 pos = cat.anchoredPosition;
        pos.x -= catMoveSpeed * Time.deltaTime;
        cat.anchoredPosition = pos;
    }

    void AnimateBuilding()
    {
        if (building == null) return;

        Vector2 pos = building.anchoredPosition;
        pos.x += buildingMoveSpeed * Time.deltaTime;
        building.anchoredPosition = pos;
    }

    void AnimateTitle()
    {
        if (titleText == null) return;

        Vector2 pos = titleText.anchoredPosition;
        pos.y = Mathf.MoveTowards(pos.y, titleExitY, titleMoveSpeed * Time.deltaTime);
        titleText.anchoredPosition = pos;

        if (Mathf.Abs(pos.y - titleExitY) < 2f)
        {
            titleText.anchoredPosition = new Vector2(pos.x, titleExitY);
            titleText.gameObject.SetActive(false);
        }
    }

    void AnimateButtonPop()
    {
        if (startButton == null || buttonFinished) return;

        Vector3 targetScale = buttonStartScale * buttonTargetScale;
        startButton.localScale = Vector3.Lerp(
            startButton.localScale,
            targetScale,
            buttonPopSpeed * Time.deltaTime
        );

        if (Vector3.Distance(startButton.localScale, targetScale) < 0.05f)
        {
            startButton.localScale = targetScale;
            buttonFinished = true;
            startButton.gameObject.SetActive(false);
        }
    }

    bool MoveTowardsUI(RectTransform target, Vector2 targetPos, float speed)
    {
        if (target == null) return true;

        target.anchoredPosition = Vector2.MoveTowards(
            target.anchoredPosition,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(target.anchoredPosition, targetPos) < 2f)
        {
            target.anchoredPosition = targetPos;
            return true;
        }

        return false;
    }

    void LoadSceneAfterDelay(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene name is empty.");
            return;
        }

        inputLocked = true;
        currentPhase = MenuPhase.Transitioning;
        Invoke(nameof(LoadQueuedScene), loadSceneDelay);
        queuedSceneName = sceneName;
    }

    private string queuedSceneName = "";

    void LoadQueuedScene()
    {
        if (string.IsNullOrWhiteSpace(queuedSceneName))
        {
            Debug.LogWarning("Queued scene name is empty.");
            return;
        }

        SceneManager.LoadScene(queuedSceneName);
    }
}