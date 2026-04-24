using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;

public class ColorPuzzleDisplay : MonoBehaviour
{
	[SerializeField] private ColorGameController gameController;
	[SerializeField] private Image[] colorSlots;
	[SerializeField] private Color[] colorPalette;

	private void Start()
	{
		//gameController.solution.OnListChanged += UpdateDisplay;
		//RefreshUI();
		StopAllCoroutines();
		StartCoroutine(InitializeDisplay());
	}

	//private void UpdateDisplay(NetworkListEvent<int> changeEvent)
	//{
	//	RefreshUI();
	//}

	//private void RefreshUI()
	//{
	//	for (int i = 0; i < gameController.solution.Count; i++)
	//	{
	//		if (i < colorSlots.Length)
	//		{
	//			int colorIndex = gameController.solution[i];
	//			colorSlots[i].color = colorPalette[colorIndex];
	//		}
	//	}
	//}

	private IEnumerator InitializeDisplay()
	{
		// Wait until the controller and its solution list are valid
		while (gameController == null || gameController.solution == null)
		{
			yield return null;
		}

		// Subscribe to future changes
		gameController.solution.OnListChanged += UpdateDisplay;

		// Force a refresh for the initial data
		RefreshUI();
	}

	private void UpdateDisplay(NetworkListEvent<int> changeEvent)
	{
		RefreshUI();
	}

	public void RefreshUI()
	{
		// Only run if the list actually has data
		if (gameController.solution.Count == 0) return;

		for (int i = 0; i < gameController.solution.Count; i++)
		{
			if (i < colorSlots.Length)
			{
				int colorIndex = gameController.solution[i];
				// Apply the color from your palette
				colorSlots[i].color = colorPalette[colorIndex];

				// Ensure the image is visible (Alpha = 1)
				var c = colorSlots[i].color;
				c.a = 1f;
				colorSlots[i].color = c;
			}
		}
	}
}
