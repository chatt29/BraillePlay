using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float speed = 10f;

    private Vector3 originalScale;
    private bool isPointerOver;
    private bool initialized;

    private Button button;
    private GameMenuController menuController;

    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        if (!initialized)
            Initialize();
    }

    private void Initialize()
    {
        button = GetComponent<Button>();
        menuController = FindFirstObjectByType<GameMenuController>();

        if (transform.localScale == Vector3.zero)
            transform.localScale = Vector3.one;

        originalScale = transform.localScale;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        bool canHover = menuController != null && menuController.HasStartedBrowsing();
        bool isCurrent = canHover && menuController.IsCurrentButton(button);
        bool shouldHover = canHover && (isPointerOver || isCurrent);

        Vector3 targetScale = shouldHover
            ? originalScale * scaleMultiplier
            : originalScale;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        if (menuController != null && menuController.HasStartedBrowsing())
        {
            menuController.SyncFromHoveredButton(button);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }

    public void ResetVisual()
    {
        if (!initialized)
            Initialize();

        isPointerOver = false;
        transform.localScale = originalScale;
    }
}