using UnityEngine;

public class SignUpSelection : MonoBehaviour
{
    public void GoToSignUpS()
    {
        SceneTransition.Instance.LoadSceneWithFade("SignUpS");
    }

    public void GoToSignUpT()
    {
        SceneTransition.Instance.LoadSceneWithFade("SignUpT");
    }

}
