using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameMenuManager : MonoBehaviour
{
    public Button[] buttons;
    private int currentIndex = 0;

    void Start()
    {
        if (buttons != null && buttons.Length > 0)
        {
            SelectButton(0);
        }
    }

    void Update()
    {
        if (buttons == null || buttons.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Y))
        {
            NextButton();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            PreviousButton();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            buttons[currentIndex].onClick.Invoke();
        }
    }

    public void NextButton()
    {
        currentIndex++;
        if (currentIndex >= buttons.Length)
            currentIndex = 0;

        SelectButton(currentIndex);
    }

    public void PreviousButton()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = buttons.Length - 1;

        SelectButton(currentIndex);
    }

    void SelectButton(int index)
    {
        currentIndex = index;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        }
    }
}