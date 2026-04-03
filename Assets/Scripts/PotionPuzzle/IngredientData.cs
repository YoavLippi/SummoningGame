using UnityEngine;
using UnityEngine.UI;

public class StarButton : MonoBehaviour
{
	public string runicSymbolName; // e.g., "Spiral", "CrossedWands", "Moon"
	public Color liquidColor;      // e.g., Blue, Red, Green
	public int ingredientID;       // The ID the server uses to check the recipe

	// This is what the Apprentice actually sees
	public Image iconImage;
	public Image liquidImage;

	void Start()
	{
		// Set the visuals in the UI so the Apprentice can describe them
		liquidImage.color = liquidColor;
		// (You'd assign the sprite for the symbol here too)
	}
}
