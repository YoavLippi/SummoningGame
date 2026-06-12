using UnityEngine;

public class PersistentUI : MonoBehaviour
{
	private static PersistentUI instance;

	private void Awake()
	{
		// If a copy of this UI already exists in the world, destroy this duplicate immediately
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
		// THE MAGIC LINE: This keeps your UI alive when switching to the Candle scene!
		DontDestroyOnLoad(gameObject);
	}
}
