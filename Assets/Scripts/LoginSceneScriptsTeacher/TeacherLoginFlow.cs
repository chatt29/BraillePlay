using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeacherLoginFlow : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private string loginURL = "http://localhost/brailleplay/teacher_login.php";

    [Header("UI")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text messageText;

    [Header("Scenes")]
    [SerializeField] private string teacherDashboardScene = "StudentScoreboard";

    public static string LoggedInTeacher;

    public void TriggerLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        StartCoroutine(LoginTeacher(username, password));
    }

    private IEnumerator LoginTeacher(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + www.error);
                ShowMessage("Connection error");
            }
            else
            {
                string response = www.downloadHandler.text.Trim();
                Debug.Log("Teacher Login Response: " + response);

                if (response.StartsWith("SUCCESS"))
                {
                    string[] parts = response.Split('|');

                    int teacherId = -1;
                    string firstName = "";
                    string lastName = "";

                    if (parts.Length > 1)
                        int.TryParse(parts[1], out teacherId);

                    if (parts.Length > 2)
                        firstName = parts[2];

                    if (parts.Length > 3)
                        lastName = parts[3];

                    // SAVE DATA
                    PlayerPrefs.SetInt("TeacherID", teacherId);
                    PlayerPrefs.SetString("TeacherName", firstName + " " + lastName);
                    PlayerPrefs.SetInt("isTeacherLoggedIn", 1);
                    PlayerPrefs.Save();

                    LoggedInTeacher = username;

                    Debug.Log("TeacherID: " + teacherId);

                    ShowMessage("Login successful");

                    yield return new WaitForSeconds(1f);

                    SceneManager.LoadScene(teacherDashboardScene);
                }
                else if (response == "WRONG_PASSWORD")
                {
                    ShowMessage("Wrong password");
                }
                else if (response == "NO_USER")
                {
                    ShowMessage("Teacher not found");
                }
                else
                {
                    ShowMessage("Unexpected response");
                }
            }
        }
    }

    private void ShowMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }
}