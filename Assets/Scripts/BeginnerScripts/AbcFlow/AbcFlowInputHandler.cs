using UnityEngine;
using UnityEngine.UI;

public class AbcFlowInputHandler : MonoBehaviour
{
    [Header("Dot Visuals")]
    public Image dot1;
    public Image dot2;
    public Image dot3;
    public Image dot4;
    public Image dot5;
    public Image dot6;

    public Color idleColor = Color.white;
    public Color activeColor = Color.black;

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += SubmitPattern;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= SubmitPattern;
    }

    private void Update()
    {
        if (BrailleMapping.Instance == null)
            return;

        dot1.color = BrailleMapping.Instance.GetDot1() ? activeColor : idleColor;
        dot2.color = BrailleMapping.Instance.GetDot2() ? activeColor : idleColor;
        dot3.color = BrailleMapping.Instance.GetDot3() ? activeColor : idleColor;
        dot4.color = BrailleMapping.Instance.GetDot4() ? activeColor : idleColor;
        dot5.color = BrailleMapping.Instance.GetDot5() ? activeColor : idleColor;
        dot6.color = BrailleMapping.Instance.GetDot6() ? activeColor : idleColor;
    }

    void SubmitPattern(string patternFromMapping)
    {
        FindObjectOfType<AbcFlowScript>().CheckAnswer(patternFromMapping);
    }
}