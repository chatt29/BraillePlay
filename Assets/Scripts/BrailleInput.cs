using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BrailleInput : MonoBehaviour
{
    [Header("Braille Dot UI")]
    public Image dot1;
    public Image dot2;
    public Image dot3;
    public Image dot4;
    public Image dot5;
    public Image dot6;

    [Header("Target Letter UI")]
    public Image targetImage;

    [Header("Letter Sprites A-Z")]
    public Sprite letterA;
    public Sprite letterB;
    public Sprite letterC;
    public Sprite letterD;
    public Sprite letterE;
    public Sprite letterF;
    public Sprite letterG;
    public Sprite letterH;
    public Sprite letterI;
    public Sprite letterJ;
    public Sprite letterK;
    public Sprite letterL;
    public Sprite letterM;
    public Sprite letterN;
    public Sprite letterO;
    public Sprite letterP;
    public Sprite letterQ;
    public Sprite letterR;
    public Sprite letterS;
    public Sprite letterT;
    public Sprite letterU;
    public Sprite letterV;
    public Sprite letterW;
    public Sprite letterX;
    public Sprite letterY;
    public Sprite letterZ;

    [Header("Letter Audio A-Z")]
    public AudioClip audioA;
    public AudioClip audioB;
    public AudioClip audioC;
    public AudioClip audioD;
    public AudioClip audioE;
    public AudioClip audioF;
    public AudioClip audioG;
    public AudioClip audioH;
    public AudioClip audioI;
    public AudioClip audioJ;
    public AudioClip audioK;
    public AudioClip audioL;
    public AudioClip audioM;
    public AudioClip audioN;
    public AudioClip audioO;
    public AudioClip audioP;
    public AudioClip audioQ;
    public AudioClip audioR;
    public AudioClip audioS;
    public AudioClip audioT;
    public AudioClip audioU;
    public AudioClip audioV;
    public AudioClip audioW;
    public AudioClip audioX;
    public AudioClip audioY;
    public AudioClip audioZ;

    [Header("Feedback Audio")]
    public AudioClip correctClip;
    public AudioClip wrongClip;

    public AudioSource audioSource;
    private LetterData[] levels;
    private int currentLevel = 0;

    private bool d1, d2, d3, d4, d5, d6;

    void Start()
    {
        levels = new LetterData[]
        {
            new LetterData("A", new bool[] { true,  false, false, false, false, false }, letterA),
            new LetterData("B", new bool[] { true,  true,  false, false, false, false }, letterB),
            new LetterData("C", new bool[] { true,  false, false, true,  false, false }, letterC),
            new LetterData("D", new bool[] { true,  false, false, true,  true,  false }, letterD),
            new LetterData("E", new bool[] { true,  false, false, false, true,  false }, letterE),
            new LetterData("F", new bool[] { true,  true,  false, true,  false, false }, letterF),
            new LetterData("G", new bool[] { true,  true,  false, true,  true,  false }, letterG),
            new LetterData("H", new bool[] { true,  true,  false, false, true,  false }, letterH),
            new LetterData("I", new bool[] { false, true,  false, true,  false, false }, letterI),
            new LetterData("J", new bool[] { false, true,  false, true,  true,  false }, letterJ),
            new LetterData("K", new bool[] { true,  false, true,  false, false, false }, letterK),
            new LetterData("L", new bool[] { true,  true,  true,  false, false, false }, letterL),
            new LetterData("M", new bool[] { true,  false, true,  true,  false, false }, letterM),
            new LetterData("N", new bool[] { true,  false, true,  true,  true,  false }, letterN),
            new LetterData("O", new bool[] { true,  false, true,  false, true,  false }, letterO),
            new LetterData("P", new bool[] { true,  true,  true,  true,  false, false }, letterP),
            new LetterData("Q", new bool[] { true,  true,  true,  true,  true,  false }, letterQ),
            new LetterData("R", new bool[] { true,  true,  true,  false, true,  false }, letterR),
            new LetterData("S", new bool[] { false, true,  true,  true,  false, false }, letterS),
            new LetterData("T", new bool[] { false, true,  true,  true,  true,  false }, letterT),
            new LetterData("U", new bool[] { true,  false, true,  false, false, true  }, letterU),
            new LetterData("V", new bool[] { true,  true,  true,  false, false, true  }, letterV),
            new LetterData("W", new bool[] { false, true,  false, true,  true,  true  }, letterW),
            new LetterData("X", new bool[] { true,  false, true,  true,  false, true  }, letterX),
            new LetterData("Y", new bool[] { true,  false, true,  true,  true,  true  }, letterY),
            new LetterData("Z", new bool[] { true,  false, true,  false, true,  true  }, letterZ),
        };

        LoadLevel();
        ResetInput();
    }

    void Update()
    {
        HandleInput();
        UpdateDotVisuals();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SubmitAnswer();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayLetterAudio(levels[currentLevel].letter);
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F)) d1 = !d1;
        if (Input.GetKeyDown(KeyCode.D)) d2 = !d2;
        if (Input.GetKeyDown(KeyCode.S)) d3 = !d3;
        if (Input.GetKeyDown(KeyCode.J)) d4 = !d4;
        if (Input.GetKeyDown(KeyCode.K)) d5 = !d5;
        if (Input.GetKeyDown(KeyCode.L)) d6 = !d6;
    }

    void UpdateDotVisuals()
    {
        SetDot(dot1, d1);
        SetDot(dot2, d2);
        SetDot(dot3, d3);
        SetDot(dot4, d4);
        SetDot(dot5, d5);
        SetDot(dot6, d6);
    }

    void SetDot(Image dot, bool isOn)
    {
        dot.color = isOn ? Color.black : Color.gray;
    }

    void SubmitAnswer()
    {
        bool[] playerInput = new bool[] { d1, d2, d3, d4, d5, d6 };
        bool[] correctPattern = levels[currentLevel].pattern;

        if (PatternsMatch(playerInput, correctPattern))
        {
            Debug.Log("Correct! " + levels[currentLevel].letter);
            StartCoroutine(HandleCorrectAnswer());
        }
        else
        {
            Debug.Log("Wrong! Try again.");
            PlayFeedback(wrongClip);
            ResetInput();
        }
    }

    IEnumerator HandleCorrectAnswer()
    {
        if (audioSource != null && correctClip != null)
        {
            audioSource.Stop();
            audioSource.clip = correctClip;
            audioSource.Play();

            yield return new WaitForSeconds(correctClip.length);
        }

        currentLevel++;

        if (currentLevel >= levels.Length)
        {
            Debug.Log("You finished all letters!");
            currentLevel = 0; // restart from A
        }

        LoadLevel();
        ResetInput();
    }

    bool PatternsMatch(bool[] input, bool[] target)
    {
        for (int i = 0; i < 6; i++)
        {
            if (input[i] != target[i])
                return false;
        }
        return true;
    }

    void LoadLevel()
    {
        if (targetImage == null)
        {
            Debug.LogError("Target Image is not assigned!");
            return;
        }

        if (levels[currentLevel].sprite == null)
        {
            Debug.LogError("Missing sprite for letter: " + levels[currentLevel].letter);
            return;
        }

        targetImage.sprite = levels[currentLevel].sprite;
        Debug.Log("Level " + (currentLevel + 1) + ": " + levels[currentLevel].letter);

        PlayLetterAudio(levels[currentLevel].letter);
    }

    void PlayLetterAudio(string letter)
    {
        if (audioSource == null) return;

        AudioClip clip = null;

        switch (letter)
        {
            case "A": clip = audioA; break;
            case "B": clip = audioB; break;
            case "C": clip = audioC; break;
            case "D": clip = audioD; break;
            case "E": clip = audioE; break;
            case "F": clip = audioF; break;
            case "G": clip = audioG; break;
            case "H": clip = audioH; break;
            case "I": clip = audioI; break;
            case "J": clip = audioJ; break;
            case "K": clip = audioK; break;
            case "L": clip = audioL; break;
            case "M": clip = audioM; break;
            case "N": clip = audioN; break;
            case "O": clip = audioO; break;
            case "P": clip = audioP; break;
            case "Q": clip = audioQ; break;
            case "R": clip = audioR; break;
            case "S": clip = audioS; break;
            case "T": clip = audioT; break;
            case "U": clip = audioU; break;
            case "V": clip = audioV; break;
            case "W": clip = audioW; break;
            case "X": clip = audioX; break;
            case "Y": clip = audioY; break;
            case "Z": clip = audioZ; break;
        }

        if (clip != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void PlayFeedback(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    void ResetInput()
    {
        d1 = false;
        d2 = false;
        d3 = false;
        d4 = false;
        d5 = false;
        d6 = false;

        UpdateDotVisuals();
    }

    [System.Serializable]
    public class LetterData
    {
        public string letter;
        public bool[] pattern;
        public Sprite sprite;

        public LetterData(string letter, bool[] pattern, Sprite sprite)
        {
            this.letter = letter;
            this.pattern = pattern;
            this.sprite = sprite;
        }
    }
}