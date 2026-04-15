using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameManager _gameManager;

    [SerializeField] private GameObject[] panels;

    private void Start()
    {
        ShowPanel(0); // this shows the first panel (Main Menu) by default
    }

    public void StartButton()
    {
        _gameManager.StartGame();
    }

    public void ShowPanel (int index)
    {
        foreach (var panel in panels)
            panel.SetActive(false);

        panels[index].SetActive(true);
    }

    public void QuitButton()
    {
        _gameManager.QuitGame();
    }
}
