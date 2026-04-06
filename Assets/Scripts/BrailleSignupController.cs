using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking; 
using System.Collections;
using UnityEngine.UI;







public class BrailleSignupController : MonoBehaviour
{
    [Header("Fields")]
    public TMP_InputField nameField;
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    [Header("Warnings (optional)")]
    public TMP_Text nameWarning;
    public TMP_Text usernameWarning;
    public TMP_Text passwordWarning;
    
    public Button submitBtn;

    private TMP_InputField[] fields;
    private TMP_Text[] warnings;
    private int currentFieldIndex = 0;

    // Braille chord tracking
    private bool chordStarted = false;
    private int currentChordMask = 0;
    private string lastBrailleCharacter = "";

    // Prevent control keys from also being treated like braille text
    private bool controlKeyPressedThisFrame = false;
    private IEnumerator RegisterToDatabase(string first_name, string last_name, string username, string password)
{
    WWWForm form = new WWWForm();
    form.AddField("first_name", first_name);
    form.AddField("last_name", last_name);
    form.AddField("username", username);
    form.AddField("password", password);
    

    using (UnityWebRequest www = UnityWebRequest.Post("http://localhost/brailleplay/register.php", form))       
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error: " + www.error);
        }
        else
        {
            Debug.Log("Server Response: " + www.downloadHandler.text);
        }
    }
    
}

    // Dot mapping:
    // 1 = F
    // 2 = D
    // 3 = S
    // 4 = J
    // 5 = K
    // 6 = L
    private Dictionary<int, string> brailleMap = new Dictionary<int, string>();

    void Start()
    {
        fields = new TMP_InputField[] { nameField, usernameField, passwordField };
        warnings = new TMP_Text[] { nameWarning, usernameWarning, passwordWarning };

        BuildBrailleMap();
        ClearWarnings();

        foreach (var field in fields)
        {
            if (field == null) continue;

            field.readOnly = true;
            field.shouldHideMobileInput = true;
            field.text = "";
            field.caretPosition = 0;
            field.selectionStringAnchorPosition = 0;
            field.selectionStringFocusPosition = 0;
        }

        FocusField(0);
    }

    void Update()
    {
        controlKeyPressedThisFrame = false;

        HandleControlKeys();
        HandleBrailleChordInput();
    }

    void HandleControlKeys()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            controlKeyPressedThisFrame = true;
            DeleteLastCharacter();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            controlKeyPressedThisFrame = true;
            RepeatLastCharacter();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            controlKeyPressedThisFrame = true;
            GoToNextField();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
    controlKeyPressedThisFrame = true;
    HandleNextOrSubmit();
    return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
    controlKeyPressedThisFrame = true;
    HandleNextOrSubmit();
    return;
        }
    }

    void HandleBrailleChordInput()
    {
        // Ignore braille capture on frames where a control key was just pressed
        if (controlKeyPressedThisFrame)
            return;

        bool brailleKeyHeld =
            Input.GetKey(KeyCode.F) ||
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.J) ||
            Input.GetKey(KeyCode.K) ||
            Input.GetKey(KeyCode.L);

        if (!chordStarted && brailleKeyHeld)
        {
            chordStarted = true;
            currentChordMask = 0;
        }

        if (chordStarted)
        {
            if (Input.GetKey(KeyCode.F)) currentChordMask |= 1 << 0; // dot 1
            if (Input.GetKey(KeyCode.D)) currentChordMask |= 1 << 1; // dot 2
            if (Input.GetKey(KeyCode.S)) currentChordMask |= 1 << 2; // dot 3
            if (Input.GetKey(KeyCode.J)) currentChordMask |= 1 << 3; // dot 4
            if (Input.GetKey(KeyCode.K)) currentChordMask |= 1 << 4; // dot 5
            if (Input.GetKey(KeyCode.L)) currentChordMask |= 1 << 5; // dot 6
        }

        // Commit only when all braille keys are released
        if (chordStarted && !brailleKeyHeld)
        {
            CommitBrailleChord();
            chordStarted = false;
            currentChordMask = 0;
        }
    }

    void CommitBrailleChord()
    {
        if (currentChordMask == 0) return;

        if (brailleMap.TryGetValue(currentChordMask, out string letter))
        {
            AppendCharacter(letter);
            lastBrailleCharacter = letter;
        }
        else
        {
            Debug.Log("Unknown braille chord: " + currentChordMask);
        }
    }
    void HandleNextOrSubmit()
{
    // If current field is NOT valid → stay here
    if (!ValidateField(currentFieldIndex))
        return;

    // If NOT last field → go next
    if (currentFieldIndex < fields.Length - 1)
    {
        currentFieldIndex++;
        FocusField(currentFieldIndex);
    }
    else
    {
        // Last field → check all then submit
        SubmitForm();
    }
}

    void AppendCharacter(string character)
    {
        TMP_InputField field = fields[currentFieldIndex];
        if (field == null) return;

        field.text += character;
        ClearWarning(currentFieldIndex);
        RefreshFocus(field);
    }

    void DeleteLastCharacter()
    {
        TMP_InputField field = fields[currentFieldIndex];
        if (field == null) return;

        if (!string.IsNullOrEmpty(field.text))
        {
            field.text = field.text.Substring(0, field.text.Length - 1);
        }

        ClearWarning(currentFieldIndex);
        RefreshFocus(field);
    }

    void RepeatLastCharacter()
    {
        if (!string.IsNullOrEmpty(lastBrailleCharacter))
        {
            AppendCharacter(lastBrailleCharacter);
        }
    }

    void GoToNextField()
    {
        if (!ValidateField(currentFieldIndex))
            return;

        if (currentFieldIndex < fields.Length - 1)
        {
            currentFieldIndex++;
            FocusField(currentFieldIndex);
        }
    }

    void SubmitForm()
{
    ClearWarnings();

    bool valid = true;
    int firstInvalidIndex = -1;

    // Validate all fields
    for (int i = 0; i < fields.Length; i++)
    {
        if (!ValidateField(i))
        {
            valid = false;

            if (firstInvalidIndex == -1)
                firstInvalidIndex = i;
        }
    }

    // If invalid, focus first error
    if (!valid)
    {
        FocusField(firstInvalidIndex);
        return;
    }

    // Clean inputs
    string fullName = nameField.text.Trim();
    string username = usernameField.text.Trim();
    string password = passwordField.text.Trim();

    // 🔥 Split name safely
    string[] nameParts = fullName.Split(' ');

    string firstName = "";
    string lastName = "";

    if (nameParts.Length == 1)
    {
        firstName = nameParts[0];
        lastName = ""; // optional
    }
    else if (nameParts.Length >= 2)
    {
        firstName = nameParts[0];
        lastName = nameParts[nameParts.Length - 1]; // last word as last name
    }

    // Debug logs
    Debug.Log("Signup submitted");
    Debug.Log("First Name: " + firstName);
    Debug.Log("Last Name: " + lastName);
    Debug.Log("Username: " + username);
    Debug.Log("Password: " + password);

    // Send to database
    StartCoroutine(RegisterToDatabase(firstName, lastName, username, password));


}

    bool ValidateField(int index)
    {
        if (index < 0 || index >= fields.Length || fields[index] == null)
            return false;

        string value = fields[index].text.Trim();

        switch (index)
        {
            case 0: // Name
                if (value.Length == 0)
                {
                    ShowWarning(index, "Please enter your name");
                    return false;
                }

                if (value.Length < 2)
                {
                    ShowWarning(index, "Name is too short");
                    return false;
                }
                break;

            case 1: // Username
                if (value.Length == 0)
                {
                    ShowWarning(index, "Username is required");
                    return false;
                }

                if (value.Length < 4)
                {
                    ShowWarning(index, "Username must be at least 4 characters");
                    return false;
                }
                break;

            case 2: // Password
                if (value.Length == 0)
                {
                    ShowWarning(index, "Password is required");
                    return false;
                }

                if (value.Length < 4)
                {
                    ShowWarning(index, "Password must be at least 4 characters");
                    return false;
                }
                break;
        }

        ClearWarning(index);
        return true;
    }

    void ShowWarning(int index, string message)
    {
        if (index >= 0 && index < warnings.Length && warnings[index] != null)
        {
            warnings[index].text = message;
            warnings[index].gameObject.SetActive(true);
        }
    }

    void ClearWarnings()
    {
        for (int i = 0; i < warnings.Length; i++)
        {
            if (warnings[i] != null)
            {
                warnings[i].text = "";
                warnings[i].gameObject.SetActive(false);
            }
        }
    }

    void ClearWarning(int index)
    {
        if (index >= 0 && index < warnings.Length && warnings[index] != null)
        {
            warnings[index].text = "";
            warnings[index].gameObject.SetActive(false);
        }
    }

    void FocusField(int index)
    {
        if (index < 0 || index >= fields.Length || fields[index] == null)
            return;

        currentFieldIndex = index;
        RefreshFocus(fields[index]);
    }

    void RefreshFocus(TMP_InputField field)
    {
        if (field == null) return;

        field.readOnly = true;
        field.Select();
        field.ActivateInputField();
        field.caretPosition = field.text.Length;
        field.selectionStringAnchorPosition = field.text.Length;
        field.selectionStringFocusPosition = field.text.Length;
    }

    void BuildBrailleMap()
    {
        brailleMap.Clear();

        brailleMap.Add(Dots(1), "a");
        brailleMap.Add(Dots(1, 2), "b");
        brailleMap.Add(Dots(1, 4), "c");
        brailleMap.Add(Dots(1, 4, 5), "d");
        brailleMap.Add(Dots(1, 5), "e");
        brailleMap.Add(Dots(1, 2, 4), "f");
        brailleMap.Add(Dots(1, 2, 4, 5), "g");
        brailleMap.Add(Dots(1, 2, 5), "h");
        brailleMap.Add(Dots(2, 4), "i");
        brailleMap.Add(Dots(2, 4, 5), "j");

        brailleMap.Add(Dots(1, 3), "k");
        brailleMap.Add(Dots(1, 2, 3), "l");
        brailleMap.Add(Dots(1, 3, 4), "m");
        brailleMap.Add(Dots(1, 3, 4, 5), "n");
        brailleMap.Add(Dots(1, 3, 5), "o");
        brailleMap.Add(Dots(1, 2, 3, 4), "p");
        brailleMap.Add(Dots(1, 2, 3, 4, 5), "q");
        brailleMap.Add(Dots(1, 2, 3, 5), "r");
        brailleMap.Add(Dots(2, 3, 4), "s");
        brailleMap.Add(Dots(2, 3, 4, 5), "t");

        brailleMap.Add(Dots(1, 3, 6), "u");
        brailleMap.Add(Dots(1, 2, 3, 6), "v");
        brailleMap.Add(Dots(2, 4, 5, 6), "w");
        brailleMap.Add(Dots(1, 3, 4, 6), "x");
        brailleMap.Add(Dots(1, 3, 4, 5, 6), "y");
        brailleMap.Add(Dots(1, 3, 5, 6), "z");
        brailleMap.Add(Dots(6), " "); 
    }

    int Dots(params int[] dots)
    {
        int mask = 0;

        foreach (int dot in dots)
        {
            mask |= 1 << (dot - 1);
        }

        return mask;
    }
}