using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Put on any UI element to give it a hover tooltip. When the mouse enters it, it announces its text;
    /// the screen that owns it shows that text in its details panel.
    /// </summary>
    public class UITooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Raised with the tooltip text on hover, and with null when the mouse leaves.</summary>
        public static event Action<string> HoverChanged;

        [SerializeField, TextArea] private string _text;

        /// <summary>The text shown while hovered.</summary>
        public string Text
        {
            get => _text;
            set => _text = value;
        }

        /// <summary>Adds a tooltip to an element (or updates its text).</summary>
        public static UITooltipTarget Attach(Component element, string text)
        {
            UITooltipTarget target = element.GetComponent<UITooltipTarget>();
            if (target == null)
            {
                target = element.gameObject.AddComponent<UITooltipTarget>();
            }

            target.Text = text;
            return target;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            HoverChanged?.Invoke(_text);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverChanged?.Invoke(null);
        }

        // Static events outlive scenes (domain reload is off in this project), so clear old listeners on Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            HoverChanged = null;
        }
    }
}
