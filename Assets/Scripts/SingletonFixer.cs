using Unity.Netcode;
using UnityEngine;

public class SingletonFixer : MonoBehaviour
{
    void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != gameObject)
        {
            Destroy(gameObject);
            return;
        }
    }
}
