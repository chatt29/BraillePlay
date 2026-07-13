using UnityEngine;

public class MainMenuSceneLoader : MonoBehaviour
{
    public void GoToLoginS()
    {
        SceneTransition.Instance.LoadSceneWithFade("LoginStudent");
    }

    public void GoToLoginT()
    {
        SceneTransition.Instance.LoadSceneWithFade("LoginTeacher");
    }
    public void GoToCreateAccount()
    {
        SceneTransition.Instance.LoadSceneWithFade("SignUpSelection");
    }

    public void GoToBegginerScene()
    {
        SceneTransition.Instance.LoadSceneWithFade("BeginnerScene");
    }
}