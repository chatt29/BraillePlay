using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegisterStudent : MonoBehaviour
{
    public InputField firstName, middleName, lastName, age, username, password;
    public Text messageText;

    string url = "http://localhost/register_student.php"; // Update if using IP

    public void Register()
    {
        StartCoroutine(RegisterCoroutine());
    }

    IEnumerator RegisterCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("first_name", firstName.text);
        form.AddField("middle_name", middleName.text);
        form.AddField("last_name", lastName.text);
        form.AddField("age", age.text);
        form.AddField("username", username.text);
        form.AddField("password", password.text);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            messageText.text = www.downloadHandler.text;
        }
        else
        {
            messageText.text = "Error: " + www.error;
        }
    }
}