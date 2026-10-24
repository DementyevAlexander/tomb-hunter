using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TombHunter
{
    // Кирпичики интерфейса: узлы, текст, пиксельные панели, плашки, кнопки, чипы, прокручиваемые списки.
    public static class Kit
    {
        public static readonly Color Ink = Pixel.Hex("#2a1b2e"), Cream = Pixel.Hex("#fff4dc"), Gold = Pixel.Hex("#f6c453"), GoldLo = Pixel.Hex("#b9822a"),
            Plate = Pixel.Hex("#33264a"), PlateHi = Pixel.Hex("#46355f"), Parch = Pixel.Hex("#f7e9cb"), ParchLo = Pixel.Hex("#e6cfa2"), ParchInk = Pixel.Hex("#6a5444"),
            Brick = Pixel.Hex("#c8693a"), Well = Pixel.Hex("#3d2c45"), Danger = Pixel.Hex("#c8352b"), Good = Pixel.Hex("#2f8a3e"), Find = Pixel.Hex("#a8611a"),
            Muted = Pixel.Hex("#8a7766"), BgTop = Pixel.Hex("#9a7a9c"), BgBot = Pixel.Hex("#6a4f72");

        static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Center(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Img(Transform parent, Sprite sprite, Color color, string name = "Image")
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Image Icon(Transform parent, string name, float size = 32)
        {
            var img = Img(parent, Pixel.Get(name), Color.white, name);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, bool bold = false)
        {
            var rt = Node("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = color; t.alignment = align; t.text = text;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true; t.raycastTarget = false; t.lineSpacing = 1.05f;
            return t;
        }

        public static Text Title(Transform parent, string text, int size, Color color)
        {
            var t = Txt(parent, text, size, color, TextAnchor.MiddleLeft, true);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Ink; o.effectDistance = new Vector2(2, -2);
            var s = t.gameObject.AddComponent<Shadow>(); s.effectColor = Ink; s.effectDistance = new Vector2(0, -4);
            return t;
        }

        public static Sprite PanelSprite => Pixel.Box(Parch, Ink, 3, 6);
        public static Sprite PlateSprite => Pixel.Box(Plate, Ink, 3, 5, GoldLo);
        public static Sprite ShadowSprite => Pixel.Box(Ink, Ink, 3, 6);

        // Пергаментная панель с тенью снизу и тёмной табличкой-заголовком.
        public static RectTransform Panel(Transform parent, string banner, float x, float y, float w, float h, Color? fill = null)
        {
            var root = Node("Panel", parent).Place(x, y, w, h);
            var sh = Img(root, ShadowSprite, Ink, "Shadow"); sh.rectTransform.Fill(0, 6, 0, -6);
            var bg = Img(root, Pixel.Box(fill ?? Parch, Ink, 3, 6), Color.white, "Bg"); bg.rectTransform.Fill();
            if (!string.IsNullOrEmpty(banner)) Banner(root, banner, 12, -16);
            return root;
        }

        public static Text Banner(Transform parent, string text, float x, float y)
        {
            var plate = Node("Banner", parent);
            plate.anchorMin = plate.anchorMax = new Vector2(0, 1); plate.pivot = new Vector2(0, 1);
            plate.anchoredPosition = new Vector2(x, -y);
            var img = plate.gameObject.AddComponent<Image>(); img.sprite = PlateSprite; img.type = Image.Type.Sliced; img.raycastTarget = false;
            var lay = plate.gameObject.AddComponent<HorizontalLayoutGroup>(); lay.padding = new RectOffset(12, 12, 5, 5); lay.childControlWidth = lay.childControlHeight = true;
            var fit = plate.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Txt(plate, text.ToUpper(), 13, Gold, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        // Маленькая табличка с золотым текстом, ширина по содержимому.
        public static Text PlateLabel(Transform parent, string text, int size = 11)
        {
            var plate = Node("Plate", parent);
            var img = plate.gameObject.AddComponent<Image>(); img.sprite = Pixel.Box(Plate, Ink, 2, 4, GoldLo); img.type = Image.Type.Sliced; img.raycastTarget = false;
            var lay = plate.gameObject.AddComponent<HorizontalLayoutGroup>(); lay.padding = new RectOffset(8, 8, 4, 4); lay.childControlWidth = lay.childControlHeight = true;
            var fit = plate.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = Txt(plate, text, size, Gold, TextAnchor.MiddleCenter, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        public enum BtnStyle { Parch, Gold, Brick, Dark }

        public static Button Btn(Transform parent, string label, BtnStyle style, Action onClick, int size = 14)
        {
            var root = Node("Button " + label, parent);
            var sh = Img(root, Pixel.Box(Ink, Ink, 3, 6), Ink, "Shadow"); sh.rectTransform.Fill(0, 4, 0, -4);
            var face = Img(root, Pixel.Box(style == BtnStyle.Gold ? Gold : style == BtnStyle.Brick ? Brick : style == BtnStyle.Dark ? PlateHi : Parch, Ink, 3, 6), Color.white, "Face");
            face.rectTransform.Fill(); face.raycastTarget = true;
            var t = Txt(face.rectTransform, label, size, style == BtnStyle.Brick || style == BtnStyle.Dark ? Cream : Ink, TextAnchor.MiddleCenter, true);
            t.rectTransform.Fill(8, 2, 8, 4); t.name = "Label";
            var b = root.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            var cols = b.colors; cols.highlightedColor = new Color(1.06f, 1.06f, 1.06f); cols.pressedColor = new Color(0.92f, 0.92f, 0.92f); cols.disabledColor = new Color(1, 1, 1, 0.5f); cols.fadeDuration = 0.05f; b.colors = cols;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            root.gameObject.AddComponent<PressDip>().face = face.rectTransform;
            return b;
        }

        public static void SetLabel(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        public static RectTransform Chip(Transform parent, string sprite, string text, out Text value, string small = null)
        {
            var root = Node("Chip", parent);
            var sh = Img(root, Pixel.Box(Ink, Ink, 3, 6), Ink, "Shadow"); sh.rectTransform.Fill(0, 3, 0, -3);
            var bg = Img(root, Pixel.Box(Parch, Ink, 3, 6), Color.white, "Bg"); bg.rectTransform.Fill();
            var lay = root.gameObject.AddComponent<HorizontalLayoutGroup>(); lay.padding = new RectOffset(6, 10, 4, 4); lay.spacing = 6;
            lay.childAlignment = TextAnchor.MiddleLeft; lay.childControlWidth = lay.childControlHeight = true; lay.childForceExpandWidth = false;
            var fit = root.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sh.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var icon = Icon(root, sprite, 28); var le = icon.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 28;
            value = Txt(root, text, 16, Ink, TextAnchor.MiddleLeft, true); value.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (small != null) { var s = Txt(root, small, 12, ParchInk, TextAnchor.MiddleLeft); s.horizontalOverflow = HorizontalWrapMode.Overflow; }
            return root;
        }

        public static RectTransform HRow(Transform parent, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = Node("Row", parent);
            var lay = rt.gameObject.AddComponent<HorizontalLayoutGroup>(); lay.spacing = spacing; lay.childAlignment = align;
            lay.childControlWidth = lay.childControlHeight = true; lay.childForceExpandWidth = lay.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform VCol(Transform parent, float spacing, RectOffset pad = null)
        {
            var rt = Node("Col", parent);
            var lay = rt.gameObject.AddComponent<VerticalLayoutGroup>(); lay.spacing = spacing; lay.padding = pad ?? new RectOffset();
            lay.childControlWidth = lay.childControlHeight = true; lay.childForceExpandWidth = true; lay.childForceExpandHeight = false;
            return rt;
        }

        public static LayoutElement Size(this Component c, float w = -1, float h = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        // Прокручиваемый вертикальный список внутри прямоугольника.
        public static RectTransform Scroll(RectTransform area, float spacing = 6)
        {
            var view = Node("Viewport", area).Fill();
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var content = VCol(view, spacing);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = area.gameObject.AddComponent<ScrollRect>();
            sr.viewport = view; sr.content = content; sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Kill(t.GetChild(i).gameObject);
        }

        // Отцепить и уничтожить: раскладка не должна учитывать объект до конца кадра.
        public static void Kill(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(null, false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go);
        }

        // Квадратный портрет в рамке.
        public static RectTransform Avatar(Transform parent, string sprite, Color bg, float size)
        {
            var root = Node("Avatar", parent);
            root.sizeDelta = new Vector2(size, size);
            var b = Img(root, Pixel.Box(bg, Ink, 2, 5), Color.white, "Bg"); b.rectTransform.Fill();
            var s = Icon(root, sprite, size - 8); s.rectTransform.Fill(4, 4, 4, 4);
            root.Size(size, size);
            return root;
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static Color LogColor(string kind) => kind == "good" ? Good : kind == "bad" ? Danger : kind == "find" ? Find : kind == "dim" ? Muted : Ink;
    }

    // Кнопка проседает на 3 пикселя при нажатии, как в браузерной версии.
    public class PressDip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform face;
        bool down;
        public void OnPointerDown(PointerEventData e) { if (!down && GetComponent<Button>().interactable) { down = true; face.anchoredPosition += new Vector2(0, -3); } }
        public void OnPointerUp(PointerEventData e) => Up();
        public void OnPointerExit(PointerEventData e) => Up();
        void Up() { if (down) { down = false; face.anchoredPosition += new Vector2(0, 3); } }
        void OnDisable() { if (down) Up(); }
    }

    // Наведение мыши: показывает подсказку.
    public class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action enter, exit;
        public void OnPointerEnter(PointerEventData e) => enter?.Invoke();
        public void OnPointerExit(PointerEventData e) => exit?.Invoke();
    }
}
