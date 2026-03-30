using UnityEngine;

public class MainMenuSceneLoader : MonoBehaviour
{
    public void GoToLogin()
    {
        SceneTransition.Instance.LoadSceneWithFade("Login");
    }

    public void GoToCreateAccount()
    {
        SceneTransition.Instance.LoadSceneWithFade("SignUp");
    }

    public void GoToBegginerScene()
    {
        SceneTransition.Instance.LoadSceneWithFade("BeginnerScene");
    }
}