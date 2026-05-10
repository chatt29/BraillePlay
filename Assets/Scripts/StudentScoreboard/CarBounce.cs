using UnityEngine;

public class CarBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceHeight = 3f;
    public float bounceSpeed = 4f;
    public bool randomOffset = true;

    private RectTransform rectTransform;
    private Vector2 startPos;
    private float offset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;

        if (randomOffset)
            offset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        float y = Mathf.Sin((Time.time + offset) * bounceSpeed) * bounceHeight;

        rectTransform.anchoredPosition = new Vector2(
            startPos.x,
            startPos.y + y
        );
    }
}