using UnityEngine;

public class UITextFloat : MonoBehaviour
{
    public float speed = 1.5f;
    public float height = 4f;
    public bool isActive = true;

    private RectTransform rt;
    private Vector2 startPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    void Update()
    {
        if (!isActive) return;

        float yOffset = Mathf.Sin(Time.unscaledTime * speed) * height;
        rt.anchoredPosition = startPos + new Vector2(0, yOffset);
    }

    public void StopFloat()
    {
        isActive = false;
        rt.anchoredPosition = startPos;
    }
}