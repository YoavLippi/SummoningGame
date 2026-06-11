using Unity.Netcode;
using UnityEngine;

public class PauseButtonHostingHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            gameObject.SetActive(false);
        }
    }
}
