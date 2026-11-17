using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Added to every UIKit button: plays the "move" sound when the selection reaches it with the arrow keys,
    /// or when the mouse moves onto it. Code selecting a button when a screen opens stays silent.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UISelectSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnSelect(BaseEventData eventData)
        {
            // Arrow-key navigation sends AxisEventData. UIKit.Select (a screen opening) sends a plain
            // BaseEventData, and a mouse click sends PointerEventData (the click sound covers that).
            if (eventData is AxisEventData)
            {
                UISounds.PlayMove();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable.IsInteractable())
            {
                UISounds.PlayMove();
            }
        }
    }
}
