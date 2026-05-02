using UnityEngine;
using UnityEngine.UI;

public class AbcFlowInoutZ_AHandler : MonoBehaviour
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

    private string savedPattern = "000000";

    private void OnEnable()
    {
        BrailleMapping.OnBrailleChordSubmitted += SavePattern;
        BrailleMapping.OnSubmit += SubmitPattern;
    }

    private void OnDisable()
    {
        BrailleMapping.OnBrailleChordSubmitted -= SavePattern;
        BrailleMapping.OnSubmit -= SubmitPattern;
    }

    private void Start()
    {
        UpdateDots(savedPattern);
    }

    void SavePattern(string patternFromMapping)
    {
        savedPattern = patternFromMapping;
        UpdateDots(savedPattern);
    }

    void SubmitPattern()
    {
        AbcFlowZ_AScript abcFlow = FindObjectOfType<AbcFlowZ_AScript>();

        if (abcFlow == null)
            return;

        if (abcFlow.IsShowingIntro())
            return;

        abcFlow.CheckAnswer(savedPattern);

        savedPattern = "000000";
        UpdateDots(savedPattern);
    }

    void UpdateDots(string pattern)
    {
        if (pattern.Length < 6) return;

        dot1.color = pattern[0] == '1' ? activeColor : idleColor;
        dot2.color = pattern[1] == '1' ? activeColor : idleColor;
        dot3.color = pattern[2] == '1' ? activeColor : idleColor;
        dot4.color = pattern[3] == '1' ? activeColor : idleColor;
        dot5.color = pattern[4] == '1' ? activeColor : idleColor;
        dot6.color = pattern[5] == '1' ? activeColor : idleColor;
    }
}