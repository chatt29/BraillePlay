using UnityEngine;

public class UIButtonIdleBounce : MonoBehaviour
{
    public float speed = 2f;
    public float amount = 0.05f;
    public bool isActive = true;

    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        if (!isActive) return;

        float scaleOffset = Mathf.Sin(Time.unscaledTime * speed) * amount;
        transform.localScale = startScale + Vector3.one * scaleOffset;
    }

    public void StopBounce()
    {
        isActive = false;
        transform.localScale = startScale;
    }
}