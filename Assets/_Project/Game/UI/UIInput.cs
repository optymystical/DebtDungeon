using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Builds a TextMeshPro text box in code. A TMP_InputField needs three parts wired together:
    /// a viewport that clips the text, the text itself, and a grey placeholder shown while it's empty.
    /// </summary>
    public static class UIInput
    {
        public static TMP_InputField CreateTextBox(Transform parent, string placeholderText, int characterLimit, float height)
        {
            GameObject boxObject = new GameObject("Text Box", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            boxObject.transform.SetParent(parent, false);
            boxObject.GetComponent<Image>().color = UIKit.ButtonColor;
            UIKit.SetSize(boxObject.GetComponent<RectTransform>(), height);

            RectTransform viewport = UIKit.CreateGroup(boxObject.transform, "Text Area");
            UIKit.Stretch(viewport);
            viewport.offsetMin = new Vector2(16f, 6f);
            viewport.offsetMax = new Vector2(-16f, -6f);
            viewport.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholder = UIKit.CreateText(viewport, placeholderText, 28f, TextAlignmentOptions.MidlineLeft, UIKit.MutedText);
            UIKit.Stretch(placeholder.rectTransform);

            TextMeshProUGUI text = UIKit.CreateText(viewport, string.Empty, 28f, TextAlignmentOptions.MidlineLeft, UIKit.TextColor);
            UIKit.Stretch(text.rectTransform);

            TMP_InputField input = boxObject.GetComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = characterLimit;
            return input;
        }
    }
}
