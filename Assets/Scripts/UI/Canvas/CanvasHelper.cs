using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Canvas
{
    public static class CanvasHelper
    {
        public static readonly Color Background = new Color(0.04f, 0.05f, 0.08f, 0.98f);
        public static readonly Color Border = new Color(0.12f, 0.45f, 0.75f, 0.6f);
        public static readonly Color Accent = new Color(0.05f, 0.6f, 1f, 1f);
        public static readonly Color MainText = Color.white;
        public static readonly Color Subtitle = new Color(0.6f, 0.65f, 0.7f, 1f);
        public static readonly Color Value = new Color(0f, 1f, 0.6f, 1f);
        public static readonly Color Good = new Color(0.1f, 0.9f, 0.4f);
        public static readonly Color Warning = new Color(0.95f, 0.7f, 0.1f);
        public static readonly Color Bad = new Color(1f, 0.2f, 0.2f);

        public static GameObject AddPanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            var image = panelObject.AddComponent<Image>();
            image.color = Background;
            image.raycastTarget = true;
            Outline outline = panelObject.AddComponent<Outline>();
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1, 1);
            return panelObject;
        }

        public static void AddTitle(Transform parent, string title, ref float yOffset)
        {
            float panelWidth = parent.GetComponent<RectTransform>().rect.height;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null) panelWidth = parentRect.rect.width;
            
            AddText(parent, title, Accent, 13, FontStyles.Bold, new Vector2(15, yOffset), new Vector2(panelWidth - 30, 20), "Title");
            yOffset -= 26;
            GameObject separator = new GameObject("Sep");
            separator.transform.SetParent(parent, false);
            RectTransform separatorRect = separator.AddComponent<RectTransform>();
            separatorRect.anchorMin = separatorRect.anchorMax = separatorRect.pivot = new Vector2(0.5f, 1);
            separatorRect.anchoredPosition = new Vector2(0, yOffset + 2);
            separatorRect.sizeDelta = new Vector2(panelWidth - 30, 1f);
            separator.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            yOffset -= 12;
        }

        public static void AddRow(Transform parent, string label, string id, ref float yOffset, Color? valueColor = null)
        {
            float panelWidth = 280;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null) panelWidth = parentRect.rect.width;

            GameObject rowObject = new GameObject("Row_" + id);
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.AddComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.anchoredPosition = new Vector2(0, yOffset);
            rowRect.sizeDelta = new Vector2(panelWidth, 18);

            AddText(rowObject.transform, label, Subtitle, 10f, FontStyles.Normal, new Vector2(15, 0), new Vector2(panelWidth * 0.5f, 16), "L");
            AddText(rowObject.transform, "—", valueColor ?? Value, 10f, FontStyles.Bold, new Vector2(panelWidth * 0.5f, 0), new Vector2(panelWidth * 0.45f - 15, 16), "Val", TextAlignmentOptions.Right);
            yOffset -= 22;
        }

        public static void AddBar(Transform parent, string label, string id, ref float yOffset)
        {
            float panelWidth = 280;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null) panelWidth = parentRect.rect.width;

            GameObject barObject = new GameObject("Bar_" + id);
            barObject.transform.SetParent(parent, false);
            RectTransform barRect = barObject.AddComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = new Vector2(0, 1);
            barRect.pivot = new Vector2(0, 1);
            barRect.anchoredPosition = new Vector2(0, yOffset);
            barRect.sizeDelta = new Vector2(panelWidth, 15);

            AddText(barObject.transform, label, Subtitle, 9f, FontStyles.Normal, new Vector2(15, -2), new Vector2(80, 14), "L");

            GameObject barBackground = new GameObject("BG");
            barBackground.transform.SetParent(barObject.transform, false);
            RectTransform bgRect = barBackground.AddComponent<RectTransform>();
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(90, -7);
            bgRect.sizeDelta = new Vector2(110, 8);
            barBackground.AddComponent<Image>().color = new Color(1, 1, 1, 0.05f);

            GameObject fillBar = new GameObject("Fill");
            fillBar.transform.SetParent(barBackground.transform, false);
            RectTransform fillRect = fillBar.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0.5f); fillRect.anchorMax = new Vector2(0, 0.5f);
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0, 8);
            fillBar.AddComponent<Image>().color = Value;

            AddText(barObject.transform, "0%", Value, 9f, FontStyles.Bold, new Vector2(205, -2), new Vector2(40, 14), "V", TextAlignmentOptions.Right);
            yOffset -= 20;
        }

        public static TextMeshProUGUI AddText(Transform parent, string text, Color color, float fontSize, FontStyles fontStyle, Vector2 position, Vector2 sizeDelta, string objectName = "Text", TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = sizeDelta;
            TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
            tmpText.text = text; tmpText.color = color; tmpText.fontSize = fontSize; tmpText.fontStyle = fontStyle; tmpText.alignment = alignment; tmpText.raycastTarget = false;
            tmpText.enableWordWrapping = false;
            tmpText.enableAutoSizing = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            return tmpText;
        }

        public static void AddSeparator(Transform parent, ref float yOffset)
        {
            float panelWidth = 280;
            RectTransform parentRect = parent.GetComponent<RectTransform>();
            if (parentRect != null) panelWidth = parentRect.rect.width;

            GameObject separatorObject = new GameObject("Separator");
            separatorObject.transform.SetParent(parent, false);
            RectTransform separatorRect = separatorObject.AddComponent<RectTransform>();
            separatorRect.anchorMin = separatorRect.anchorMax = new Vector2(0.5f, 1);
            separatorRect.pivot = new Vector2(0.5f, 1);
            separatorRect.anchoredPosition = new Vector2(0, yOffset - 2);
            separatorRect.sizeDelta = new Vector2(panelWidth - 40, 1.2f);
            separatorObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.08f);
            yOffset -= 12;
        }

        public static Button AddButton(Transform parent, string name, string text, Vector2 position, Vector2 size, Color? color = null)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = color ?? Accent;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var colorBlock = button.colors;
            colorBlock.normalColor = buttonImage.color;
            colorBlock.highlightedColor = buttonImage.color * 1.2f;
            colorBlock.pressedColor = buttonImage.color * 0.8f;
            button.colors = colorBlock;
            AddText(buttonObject.transform, text, Color.black, 10, FontStyles.Bold, Vector2.zero, size, "L", TextAlignmentOptions.Center);
            return button;
        }

        public static Image AddImage(Transform parent, string name, Vector2 position, Vector2 size, Color? color = null)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            var image = imageObject.AddComponent<Image>();
            image.color = color ?? Color.white;
            return image;
        }

        public static TextMeshProUGUI GetText(Transform root, string path) => root.Find(path)?.GetComponent<TextMeshProUGUI>();
        public static RectTransform GetFill(Transform root, string path) => root.Find(path)?.GetComponent<RectTransform>();
    }
}
