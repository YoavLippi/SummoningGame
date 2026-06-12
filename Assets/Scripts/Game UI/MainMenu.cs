using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public GameManager _gameManager;

    [SerializeField] private GameObject[] panels;

    private async void Start()
    {
        ShowPanel(0); // this shows the first panel (Main Menu) by default
        Cursor.lockState = CursorLockMode.None;
        
        var nm = NetworkManager.Singleton;

        if (nm.IsServer)
        {
            var spawnManager = nm.SpawnManager;

            // Copy list first to avoid modification during iteration
            var spawnedObjects = spawnManager.SpawnedObjectsList.ToArray();

            foreach (var netObj in spawnedObjects)
            {
                if (netObj != null && netObj.IsSpawned && !netObj.IsPlayerObject)
                {
                    netObj.Despawn(false);
                }
            }
        }
        
        await LeaveSession("default-session");
        //Just cleaning up sessions after booting back to menu
        NetworkManager.Singleton.Shutdown();

        await Task.Delay(100);
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
