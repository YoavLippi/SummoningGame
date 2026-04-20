using System;
using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    public InteractionHandler.Color associatedColour;
    public Color slotColor;
    private Image slotImage;

    private void Start()
    {
        slotImage = GetComponent<Image>();
        slotImage.color = slotColor;
    }
}
