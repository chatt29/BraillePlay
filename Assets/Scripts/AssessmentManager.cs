using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class AssessmentManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI questionText;
    public GameObject yesButton;
    public GameObject noButton;
    public TextMeshProUGUI resultText;

    [Header("Effects")]
    [SerializeField] private TypingAnimation typingAnimation;

    private AssessmentNode currentNode;
    private AssessmentNode startNode;

    private bool isWaitingForNext = false;
    private string finalLevel = "";

    private void OnEnable()
    {
        BrailleMapping.OnYesOrNext += OnYesPressed;
        BrailleMapping.OnDeleteOrNo += OnNoPressed;
    }

    private void OnDisable()
    {
        BrailleMapping.OnYesOrNext -= OnYesPressed;
        BrailleMapping.OnDeleteOrNo -= OnNoPressed;
    }

    private void Start()
    {
        BuildAssessmentTree();
        currentNode = startNode;
        DisplayCurrentNode();
    }

    public void OnYesPressed()
    {
        if (isWaitingForNext)
        {
            LoadSceneByLevel(finalLevel);
            return;
        }

        if (currentNode == null) return;
        if (currentNode.isResultNode) return;
        if (currentNode.yesNode == null)
        {
            Debug.LogError("Yes node is missing for question: " + currentNode.questionText);
            return;
        }

        currentNode = currentNode.yesNode;
        DisplayCurrentNode();
    }

    public void OnNoPressed()
    {
        if (isWaitingForNext) return;

        if (currentNode == null) return;
        if (currentNode.isResultNode) return;
        if (currentNode.noNode == null)
        {
            Debug.LogError("No node is missing for question: " + currentNode.questionText);
            return;
        }

        currentNode = currentNode.noNode;
        DisplayCurrentNode();
    }

    private void DisplayCurrentNode()
    {
        if (currentNode == null)
        {
            ShowQuestionText("Assessment tree error: node is missing.");
            if (resultText != null) resultText.text = "";
            return;
        }

        isWaitingForNext = false;
        finalLevel = "";

        if (currentNode.isResultNode)
        {
            ShowResult(currentNode.resultLevel);
            return;
        }

        if (yesButton != null) yesButton.SetActive(true);
        if (noButton != null) noButton.SetActive(true);

        ShowQuestionText(currentNode.questionText);

        if (resultText != null)
            resultText.text = "";
    }

    private void ShowResult(string level)
    {
        Debug.Log("Final Level: " + level);

        finalLevel = level;
        isWaitingForNext = true;

        PlayerPrefs.SetString("AssessmentLevel", level);
        PlayerPrefs.Save();

        if (yesButton != null) yesButton.SetActive(false);
        if (noButton != null) noButton.SetActive(false);

        ShowQuestionText("Assessment Level: " + level + "\n\nPress Y to continue");

        if (resultText != null)
            resultText.text = "";
    }

    private void ShowQuestionText(string message)
    {
        if (typingAnimation != null)
        {
            typingAnimation.PlayText(message);
        }
        else if (questionText != null)
        {
            questionText.text = message;
        }
    }

    private void LoadSceneByLevel(string level)
    {
        switch (level)
        {
            case "Beginner":
                Debug.Log("Loading BeginnerScene");
                SceneManager.LoadScene("BeginnerScene");
                break;

            case "Intermediate":
                Debug.Log("Loading IntermediateScene");
                SceneManager.LoadScene("IntermediateScene");
                break;

            case "Advance":
                Debug.Log("Loading AdvanceScene");
                SceneManager.LoadScene("AdvanceScene");
                break;

            default:
                Debug.LogError("Unknown level: " + level);
                break;
        }
    }

    private void BuildAssessmentTree()
    {
        AssessmentNode beginner1 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Beginner"
        };

        AssessmentNode beginner2 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Beginner"
        };

        AssessmentNode beginner3 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Beginner"
        };

        AssessmentNode intermediate1 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Intermediate"
        };

        AssessmentNode intermediate2 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Intermediate"
        };

        AssessmentNode advance1 = new AssessmentNode
        {
            isResultNode = true,
            resultLevel = "Advance"
        };

        AssessmentNode q1 = new AssessmentNode
        {
            questionText = "Can the student identify dot positions 1 to 6 in a single cell without confusion?"
        };

        AssessmentNode q2 = new AssessmentNode
        {
            questionText = "Can the student identify all 26 letters of the alphabet when they are uncontracted or stand-alone?"
        };

        AssessmentNode q3 = new AssessmentNode
        {
            questionText = "Does the student recognize the number sign that tells them the next character is a number and not a letter?"
        };

        AssessmentNode q4 = new AssessmentNode
        {
            questionText = "Can the student identify one-cell contractions?"
        };

        AssessmentNode q5 = new AssessmentNode
        {
            questionText = "Can the student distinguish between a period, a comma, and a capital letter indicator?"
        };

        AssessmentNode q6 = new AssessmentNode
        {
            questionText = "Can the student distinguish between a period, a comma, and a capital letter indicator consistently in reading?"
        };

        q1.noNode = beginner1;
        q1.yesNode = q2;

        q2.noNode = beginner2;
        q2.yesNode = q3;

        q3.noNode = beginner3;
        q3.yesNode = q4;

        q4.noNode = intermediate1;
        q4.yesNode = q5;

        q5.noNode = intermediate2;
        q5.yesNode = q6;

        q6.noNode = intermediate2;
        q6.yesNode = advance1;

        startNode = q1;
    }
}