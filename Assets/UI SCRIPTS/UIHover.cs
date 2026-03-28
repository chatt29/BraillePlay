using UnityEngine;
using UnityEngine.EventSystems;

public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float scaleMultiplier = 1.1f;
    public float speed = 10f;
    public Indicator indicatorManager;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private RectTransform rt;
    private bool isSelected = false;
    private bool isPointerOver = false;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        ApplyHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        if (!isSelected)
        {
            RemoveHover();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;

        if (!isPointerOver)
        {
            RemoveHover();
        }
    }

    private void ApplyHover()
    {
        targetScale = originalScale * scaleMultiplier;

        if (indicatorManager != null)
        {
            indicatorManager.MoveTo(rt);
        }
    }

    private void RemoveHover()
    {
        targetScale = originalScale;
    }
}