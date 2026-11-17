using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Small helpers that build uGUI + TextMeshPro screens from code: canvas, panels, text, buttons and layout groups.
    /// Every screen uses these, so they all look the same and none needs hand-made scene UI.
    /// Placeholder look for Alpha: flat dark panels with the player's teal as the accent.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.75f);
        public static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.22f, 0.26f, 1f);
        public static readonly Color Accent = new Color(0.2f, 0.75f, 0.7f, 1f);
        public static readonly Color TextColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        public static readonly Color MutedText = new Color(0.65f, 0.65f, 0.7f, 1f);
        public static readonly Color WarningText = new Color(0.95f, 0.55f, 0.4f, 1f);

        /// <summary>A screen-space canvas that scales from a 1920x1080 layout. Higher sortingOrder draws on top.</summary>
        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        /// <summary>
        /// Buttons need one EventSystem in the scene. Makes one (with the new Input System's UI module) if missing.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        /// <summary>A plain colored rectangle (an Image). Position it with Stretch, Center or a layout group.</summary>
        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return (RectTransform)panelObject.transform;
        }

        /// <summary>An empty rectangle for grouping, with no image.</summary>
        public static RectTransform CreateGroup(Transform parent, string name)
        {
            GameObject groupObject = new GameObject(name, typeof(RectTransform));
            groupObject.transform.SetParent(parent, false);
            return (RectTransform)groupObject.transform;
        }

        /// <summary>Makes a rectangle fill its parent.</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Puts a rectangle of a fixed size in the middle of its parent.</summary>
        public static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Stacks children top to bottom, each as wide as the group.</summary>
        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, float spacing, int padding)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>Lines children up left to right, sharing the width.</summary>
        public static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rect, float spacing, int padding)
        {
            HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        /// <summary>A TextMeshPro label. In a layout group it takes the height its text needs.</summary>
        public static TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>A button with a centered label. onClick runs when it's pressed. Every button plays the menu sounds.</summary>
        public static Button CreateButton(Transform parent, string label, UnityAction onClick, float height)
        {
            GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UISelectSound));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = Color.white; // tinted by the button's ColorBlock below

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = Color.Lerp(ButtonColor, Accent, 0.35f);
            colors.selectedColor = Color.Lerp(ButtonColor, Accent, 0.2f);
            colors.pressedColor = Accent;
            colors.disabledColor = new Color(0.16f, 0.16f, 0.18f, 1f);
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            // Added after onClick, so a volume button's click already plays at the new volume (the preview).
            button.onClick.AddListener(UISounds.PlayClick);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;

            TextMeshProUGUI text = CreateText(buttonObject.transform, label, 28f, TextAlignmentOptions.Center, TextColor);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Changes a button's label.</summary>
        public static void SetButtonLabel(Button button, string label)
        {
            button.GetComponentInChildren<TextMeshProUGUI>().text = label;
        }

        /// <summary>Gives an element a fixed height (and optionally width) inside a layout group.</summary>
        public static LayoutElement SetSize(Component element, float height, float width = -1f)
        {
            LayoutElement layout = element.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = element.gameObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
            if (width > 0f)
            {
                layout.minWidth = width;
                layout.preferredWidth = width;
            }

            return layout;
        }

        /// <summary>
        /// Highlights a button so the arrow keys and Enter work right away (keyboard menu navigation).
        /// Call it whenever a screen is shown.
        /// </summary>
        public static void Select(Selectable selectable)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && selectable != null)
            {
                eventSystem.SetSelectedGameObject(selectable.gameObject);
            }
        }

        /// <summary>Removes every child of a container, e.g. before rebuilding a list.</summary>
        public static void ClearChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
