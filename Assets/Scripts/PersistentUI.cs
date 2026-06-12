using UnityEngine;
using UnityEngine.SceneManagement; // REQUIRED TO MONITOR SCENE CHANGES

public class PersistentUI : MonoBehaviour
{
	private static PersistentUI instance;

	[Header("Cleanup Settings")]
	// TYPE YOUR STARTING/MAIN MENU SCENE NAME HERE EXACTLY
	[SerializeField] private string mainStartingSceneName = "Main Menu";

	private void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
			return;
		}

		instance = this;
		DontDestroyOnLoad(gameObject);

		// Tell Unity to notify this script whenever a new scene finishes loading
		SceneManager.sceneLoaded += OnSceneLoadedHandler;
	}

	private void OnDestroy()
	{
		// Always unhook event listeners when destroyed to prevent memory leaks
		SceneManager.sceneLoaded -= OnSceneLoadedHandler;
	}

	private void OnSceneLoadedHandler(Scene scene, LoadSceneMode mode)
	{
		// THE CLEANUP TRIGGER:
		// If we have traveled back to the main starting scene, destroy this persistent UI canvas!
		if (scene.name == mainStartingSceneName)
		{
			Debug.Log($"[PERSISTENT UI] Returning to start menu scene. Cleansing old gameplay canvas footprint.");
			Destroy(gameObject);
		}
	}
}