using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Карта снаряжения 120×196: одинаковый размер везде — в руке, лавке, сборах и на алтаре.
    public static class CardView
    {
        public const float W = 120, H = 196;

        public static RectTransform Build(Transform parent, string type, bool button, System.Action onClick = null)
        {
            var def = Data.Card(type);
            var root = Kit.Node("Card " + type, parent);
            root.sizeDelta = new Vector2(W, H);
            var sh = Kit.Img(root, Kit.ShadowSprite, Kit.Ink, "Shadow"); sh.rectTransform.Fill(0, 5, 0, -5);
            var bg = Kit.Img(root, Kit.PanelSprite, Color.white, "Bg"); bg.rectTransform.Fill(); bg.raycastTarget = button;
            var art = Kit.Img(root, Pixel.Box(def.Art, Kit.Ink, 2, 5), Color.white, "Art"); art.rectTransform.Place(7, 7, W - 14, 62);
            var ic = Kit.Icon(art.rectTransform, type, 48); ic.rectTransform.Center((W - 14) / 2, 31, 48, 48);
            var nm = Kit.Txt(root, def.Name, 15, Kit.Ink, TextAnchor.UpperLeft, true); nm.rectTransform.Place(9, 74, W - 16, 20);
            var tx = Kit.Txt(root, def.Text, 11, Kit.ParchInk); tx.rectTransform.Place(9, 96, W - 16, 64); tx.verticalOverflow = VerticalWrapMode.Truncate;
            var row = Kit.HRow(root, 3); row.Place(9, H - 30, W - 14, 20);
            var lbl = Kit.Txt(row, type == "map" ? "БЕЗ ЦЕЛИ" : "СНИМАЕТ", 9, Kit.ParchInk, TextAnchor.MiddleLeft, true); lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            var clears = type == "lantern" ? new[] { "dark" } : def.Clears;
            foreach (var o in clears) { var c = Kit.Icon(row, o, 18); c.Size(18, 18); }
            if (button)
            {
                var b = root.gameObject.AddComponent<Button>(); b.targetGraphic = bg; b.transition = Selectable.Transition.None;
                if (onClick != null) b.onClick.AddListener(() => onClick());
            }
            return root;
        }
    }

    // Плита гробницы: рубашка с препятствиями и лицевая сторона с находкой; переворот — сжатие по X.
    public class TileView
    {
        public RectTransform root, flip, backRoot, frontRoot, obsRoot;
        public Image back, backBevel, front, frontIcon, outline, glow, badge, peek;
        public CanvasGroup group;
        public Button button;
        public bool shownOpen, hovered;
        public int outlineMode;
        public Image flash;
        public string backKey, frontKey;
        public int index;

        public static Sprite Bevel(Color hi, Color lo)
        {
            return Pixel.BevelBox(hi, lo);
        }

        public TileView(Transform parent, int index, float size)
        {
            this.index = index;
            root = Kit.Node("Tile " + index, parent);
            root.sizeDelta = new Vector2(size, size);
            group = root.gameObject.AddComponent<CanvasGroup>();
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            button = root.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None; button.targetGraphic = hit;
            glow = Kit.Img(root, Pixel.Box(new Color(1, 1, 1, 0), Kit.Gold, 3, 8), new Color(1, 1, 1, 0), "Glow"); glow.rectTransform.Fill(-4, -4, -4, -4);
            flip = Kit.Node("Flip", root).Fill();
            backRoot = Kit.Node("Back", flip).Fill();
            back = Kit.Img(backRoot, null, Color.white, "Face"); back.rectTransform.Fill(); back.type = Image.Type.Tiled; back.pixelsPerUnitMultiplier = 1 / 3f;
            backBevel = Kit.Img(backRoot, null, Color.white, "Bevel"); backBevel.rectTransform.Fill();
            obsRoot = Kit.Node("Obstacles", backRoot).Fill(4, 4, 4, 4);
            badge = Kit.Icon(backRoot, "exit", size * .38f); badge.rectTransform.Place(size * .04f, size * .04f, size * .38f, size * .38f);
            peek = Kit.Icon(backRoot, "chest", size * .34f); peek.rectTransform.Place(size * .62f, size * .62f, size * .34f, size * .34f);
            flash = Kit.Img(backRoot, Pixel.Box(Color.white, Color.white, 0, 6), new Color(1, 1, 1, 0), "Flash"); flash.rectTransform.Fill();
            frontRoot = Kit.Node("Front", flip).Fill();
            front = Kit.Img(frontRoot, null, Color.white, "Face"); front.rectTransform.Fill(); front.type = Image.Type.Tiled; front.pixelsPerUnitMultiplier = 1 / 3f;
            var fb = Kit.Img(frontRoot, Pixel.BevelBox(new Color(0.24f, 0.16f, 0.08f, 0.22f), new Color(0, 0, 0, 0), true), Color.white, "Bevel"); fb.rectTransform.Fill();
            frontIcon = Kit.Icon(frontRoot, "chest", size * .64f); frontIcon.rectTransform.Center(size / 2, size / 2, size * .64f, size * .64f);
            outline = Kit.Img(root, Pixel.Box(new Color(1, 1, 1, 0), Kit.Gold, 3, 6), new Color(1, 1, 1, 0), "Outline"); outline.rectTransform.Fill();
        }

        public void SetObstacles(List<string> list, float size)
        {
            Kit.Clear(obsRoot);
            int n = list.Count;
            if (n == 0) return;
            float s = n == 1 ? size * .66f : n == 2 ? size * .46f : size * .36f;
            for (int i = 0; i < n; i++)
            {
                var ic = Kit.Icon(obsRoot, list[i], s);
                float inner = size - 8;
                Vector2 pos;
                if (n == 1) pos = new Vector2(inner / 2, inner / 2);
                else if (n == 2) pos = new Vector2(inner / 2 + (i == 0 ? -s / 2 : s / 2), inner / 2);
                else pos = new Vector2(inner / 2 + (i % 2 == 0 ? -s / 2 : s / 2) + (i == 2 ? s / 2 : 0), inner / 2 + (i < 2 ? s / 2 : -s / 2));
                ic.rectTransform.Center(pos.x, pos.y, s, s);
            }
        }
    }
}
