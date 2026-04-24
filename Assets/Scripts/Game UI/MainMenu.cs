using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameManager _gameManager;

    [SerializeField] private GameObject[] panels;

    private void Start()
    {
        //Just cleaning up sessions after booting back to menu
        NetworkManager.Singleton.Shutdown();
        LeaveSession("default-session");
        ShowPanel(0); // this shows the first panel (Main Menu) by default
    }
    
    //really ugly to have it here, but it should work
    public async Task LeaveSession(string sessionType)
    {
        var leaveTask = MultiplayerService.Instance?.Sessions[sessionType]?.LeaveAsync();
        if (leaveTask != null)
            await leaveTask;
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
