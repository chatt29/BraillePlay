using UnityEngine;

public class SceneryScript : MonoBehaviour
{
    public float speed = 500f;
    public float width = 200f; // how far to jump back

    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        rt.anchoredPosition += Vector2.right * speed * Time.deltaTime;

        // when it goes too far right, move it back by width
        if (rt.anchoredPosition.x > width)
        {
            rt.anchoredPosition -= new Vector2(width * 2, 0);
        }
    }
}