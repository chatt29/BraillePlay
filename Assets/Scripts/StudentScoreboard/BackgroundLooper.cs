using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 30f;
    public Vector2 direction = Vector2.left;

    [Header("Loop Settings")]
    public float leftLimit = -900f;
    public float rightSpawn = 900f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectTransform.anchoredPosition += direction.normalized * speed * Time.deltaTime;

        if (rectTransform.anchoredPosition.x <= leftLimit)
        {
            rectTransform.anchoredPosition = new Vector2(
                rightSpawn,
                rectTransform.anchoredPosition.y
            );
        }
    }
}