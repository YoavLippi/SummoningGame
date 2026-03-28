using UnityEngine;
using UnityEngine.UI;

public class StarButton : MonoBehaviour
{
	public int starIndex; // Set this manually (0-15) in the Inspector
	public ConstellationManager manager;
	public Image starImage; // The visual part of the star

	public void OnClick()
	{
		// Tell the manager to toggle this star across the network
		manager.ToggleStarServerRpc(starIndex);
	}

	// This will be called by the Manager when the network state changes
	public void SetVisual(bool isOn)
	{
		starImage.color = isOn ? Color.yellow : Color.gray;
	}
}
