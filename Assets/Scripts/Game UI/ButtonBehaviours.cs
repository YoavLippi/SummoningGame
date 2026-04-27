using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonBehaviours : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler  
{
    private Outline outline;
    private bool isHovered = false;

    void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered) return;
        isHovered = true;
        AudioManager.Instance.PlaySound("ButtonHover");
        if (outline != null)
            outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (outline != null)
            outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isHovered = false;
        if (outline != null)
            outline.enabled = false;

        EventSystem.current.SetSelectedGameObject(null);
    }
}
