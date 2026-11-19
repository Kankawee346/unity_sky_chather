using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public sealed class CatchLabButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private Image panel;
    private TextMeshProUGUI label;
    private Color normal, textNormal, accent;
    private bool hover, selected, pressed;
    public void Configure(Image image, TextMeshProUGUI text, Color highlight)
    {
        panel=image; label=text; normal=panel.color; textNormal=label.color; accent=highlight;
        GetComponent<Button>().transition=Selectable.Transition.None;
    }
    private void Paint()
    {
        if(panel==null) return;
        bool active=hover||selected;
        panel.color=pressed?accent:active?Color.Lerp(normal,accent,.48f):normal;
        label.color=pressed?new Color(.06f,.04f,.08f):active?Color.white:textNormal;
        transform.localScale=Vector3.one*(pressed?.97f:active?1.025f:1);
        label.rectTransform.anchoredPosition=pressed?new Vector2(0,-3):Vector2.zero;
    }
    public void OnPointerEnter(PointerEventData e) { hover=true; Paint(); }
    public void OnPointerExit(PointerEventData e) { hover=false; pressed=false; Paint(); }
    public void OnPointerDown(PointerEventData e) { pressed=true; Paint(); }
    public void OnPointerUp(PointerEventData e) { pressed=false; Paint(); }
    public void OnSelect(BaseEventData e) { selected=true; Paint(); }
    public void OnDeselect(BaseEventData e) { selected=false; Paint(); }
    private void OnDisable() { hover=selected=pressed=false; Paint(); }
}
