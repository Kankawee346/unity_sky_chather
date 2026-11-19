using UnityEngine;
using UnityEngine.EventSystems;

// Makes a Pause Menu text option respond to mouse hover/click.
// The text itself is the raycast target, so no Button component is required.
public class PauseMenuOptionClick : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public PauseMenuController controller;
    public int index;

    void Awake()
    {
        if (controller == null)
            controller = FindAnyObjectByType<PauseMenuController>(FindObjectsInactive.Include);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.HoverPauseOption(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (controller != null) controller.ClickPauseOption(index);
    }
}
