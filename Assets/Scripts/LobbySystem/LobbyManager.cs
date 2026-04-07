using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField]
    Button m_StartHostButton;
    
    [SerializeField]
    Button m_StartClientButton;

    [SerializeField] 
    Button m_LeaveGameButton;

    void Awake()
    {
        if (!FindAnyObjectByType<EventSystem>())
        {
            var inputType = typeof(StandaloneInputModule);
#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
                inputType = typeof(InputSystemUIInputModule);                
#endif
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), inputType);
            eventSystem.transform.SetParent(transform);
        }
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        DeactivateButtons();
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        DeactivateButtons();
    }

    void DeactivateButtons()
    {
        m_StartHostButton.interactable = false;
        m_StartClientButton.interactable = false;
        m_LeaveGameButton.interactable = true;
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        m_StartHostButton.interactable = true;
        m_StartClientButton.interactable = true;    
        m_LeaveGameButton.interactable = false;
    }
}
