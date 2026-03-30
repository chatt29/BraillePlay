using System;

[Serializable]
public class AssessmentNode
{
    public string questionText;

    public AssessmentNode yesNode;
    public AssessmentNode noNode;

    public bool isResultNode;
    public string resultLevel;
}