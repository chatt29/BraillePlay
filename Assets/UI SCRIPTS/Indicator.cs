using UnityEngine;

public class Indicator : MonoBehaviour
{
    public RectTransform indicator;
    public float padding = 20f; // space between button and arrow

    public void MoveTo(RectTransform target)
    {
        float x = target.anchoredPosition.x + (target.rect.width / 2f) + (indicator.rect.width / 2f) + padding;
        float y = target.anchoredPosition.y;

        indicator.anchoredPosition = new Vector2(x, y);
    }
}