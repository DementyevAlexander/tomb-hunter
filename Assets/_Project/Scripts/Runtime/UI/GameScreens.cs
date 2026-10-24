using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Вращение винта и покачивание при наведении.
    public class Spin : MonoBehaviour { public float speed = -400; void Update() => transform.Rotate(0, 0, speed * Time.unscaledDeltaTime); }

    public class Bob : MonoBehaviour
    {
        public bool on; public float amp = 3, speed = 7;
        RectTransform rt; Vector2 basePos; bool init;
        void Update()
        {
            if (!init) { rt = (RectTransform)transform; basePos = rt.anchoredPosition; init = true; }
            rt.anchoredPosition = basePos + (on ? new Vector2(0, Mathf.Abs(Mathf.Sin(Time.unscaledTime * speed)) * amp) : Vector2.zero);
        }
    }

    public partial class Game
    {
        // Кирпичная рамка с золотой линией и колодцем; возвращает внутренний прямоугольник.
        RectTransform Frame(Transform parent, float x, float y, float w, float h, Color? brick = null)
        {
            var root = Kit.Node("Frame", parent).Place(x, y, w, h);
            var sh = Kit.Img(root, Pixel.Box(Kit.Ink, Kit.Ink, 3, 8), Kit.Ink, "Shadow"); sh.rectTransform.Fill(0, 7, 0, -7);
            var b = Kit.Img(root, Pixel.Brick(brick ?? Kit.Brick), Color.white, "Brick"); b.rectTransform.Fill(3, 3, 3, 3); b.type = Image.Type.Tiled; b.pixelsPerUnitMultiplier = 1 / 3f; b.name = "Brick";
            var border = Kit.Img(root, Pixel.Box(Color.clear, Kit.Ink, 3, 8), Color.white, "Border"); border.rectTransform.Fill();
            var gold = Kit.Img(root, Pixel.Box(Color.clear, Kit.Gold, 2, 6), new Color(1, 1, 1, .85f), "GoldLine"); gold.rectTransform.Fill(5, 5, 5, 5);
            var well = Kit.Img(root, Pixel.Box(Kit.Well, Kit.Ink, 3, 5), Color.white, "Well"); well.rectTransform.Fill(16, 16, 16, 16);
            var inner = Kit.Node("Inner", well.rectTransform).Fill(3, 3, 3, 3);
            inner.gameObject.AddComponent<RectMask2D>();
            return inner;
        }

        RectTransform Poi(RectTransform map, float px, float py, string sprite, string label, Action onClick, bool heli = false)
        {
            var root = Kit.Node("POI " + label, map);
            root.anchorMin = root.anchorMax = new Vector2(px, 1 - py); root.pivot = new Vector2(.5f, .38f); root.sizeDelta = new Vector2(200, 110);
            var pic = Kit.Node("Pic", root); pic.anchorMin = pic.anchorMax = new Vector2(.5f, 1); pic.pivot = new Vector2(.5f, 1); pic.sizeDelta = new Vector2(78, 78); pic.anchoredPosition = Vector2.zero;
            var bob = pic.gameObject.AddComponent<Bob>();
            var img = Kit.Icon(pic, sprite, 78); img.rectTransform.Fill(); img.raycastTarget = true;
            if (heli)
            {
                var h = Kit.Icon(pic, "heli", 56); h.rectTransform.Fill(11, 11, 11, 11);
                var rotor = Kit.Node("Rotor", pic); rotor.anchorMin = rotor.anchorMax = rotor.pivot = new Vector2(.5f, .5f); rotor.sizeDelta = new Vector2(66, 66); rotor.anchoredPosition = new Vector2(0, 3);
                var b1 = Kit.Img(rotor, Pixel.Box(Pixel.Hex("#e6ecf5"), Kit.Ink, 1, 2), Color.white, "Blade"); b1.rectTransform.Center(33, 33, 66, 5);
                var b2 = Kit.Img(rotor, Pixel.Box(Pixel.Hex("#e6ecf5"), Kit.Ink, 1, 2), Color.white, "Blade"); b2.rectTransform.Center(33, 33, 5, 66);
                rotor.gameObject.AddComponent<Spin>().speed = -220;
            }
            var plate = Kit.PlateLabel(root, label, 12);
            var prt = (RectTransform)plate.transform.parent; prt.anchorMin = prt.anchorMax = new Vector2(.5f, 1); prt.pivot = new Vector2(.5f, 1); prt.anchoredPosition = new Vector2(0, -80);
            var btn = pic.gameObject.AddComponent<Button>(); btn.targetGraphic = img; btn.transition = Selectable.Transition.None; btn.onClick.AddListener(() => onClick());
            var hv = pic.gameObject.AddComponent<Hover>(); hv.enter = () => bob.on = true; hv.exit = () => bob.on = false;
            return root;
        }

        // ---------- город ----------
        Text heroTip;
        RectTransform journalList;

        void BuildHub()
        {
            var s = NewScreen("hub");
            var map = Frame(s, 18, Top, 874, 508);
            var img = Kit.Node("City", map).Fill().gameObject.AddComponent<RawImage>(); img.texture = MapArt.City(); img.raycastTarget = false;
            var title = Kit.PlateLabel(map, "Эшфорд, графство Кент", 13); var trt = (RectTransform)title.transform.parent; trt.Place(10, 10, 10, 10);
            Poi(map, .19f, .28f, "manor", "Особняк", () => OpenMeta("manor"));
            Poi(map, .18f, .62f, "helipad", "Вертолётная площадка", () => Go("world"), true);
            Poi(map, .70f, .30f, "auction", "Аукционный дом", () => Go("auction"));
            Poi(map, .54f, .74f, "shop", "Лавка снаряжения", () => Go("shop"));

            var tip = Kit.Panel(s, "Ада говорит", 916, Top + 16, 346, 112);
            var av = Kit.Avatar(tip, "hero", Kit.ParchLo, 64); av.Place(14, 26, 64, 64);
            heroTip = Kit.Txt(tip, "", 15, Kit.Ink); heroTip.rectTransform.Place(90, 24, 240, 80);

            var jp = Kit.Panel(s, "Дневник", 916, Top + 154, 346, 300);
            var area = Kit.Node("Area", jp).Fill(14, 24, 10, 12);
            journalList = Kit.Scroll(area, 6);

            var dbg = Kit.Panel(s, "Отладка прототипа", 916, Top + 480, 346, 110);
            var row = Kit.HRow(dbg, 8); row.Place(14, 30, 320, 40);
            Kit.Btn(row, "+100 монет", Kit.BtnStyle.Parch, () => { meta.coins += 100; RenderTop(); Save(); fx.Bump(chipCoins); }, 12).Size(100, 38);
            Kit.Btn(row, "+3 топлива", Kit.BtnStyle.Parch, () => { meta.fuel += 3; RenderTop(); Save(); fx.Bump(chipFuel); }, 12).Size(100, 38);
            Button reset = null;
            bool armed = false;
            reset = Kit.Btn(row, "Сброс", Kit.BtnStyle.Brick, () =>
            {
                if (!armed) { armed = true; reset.SetLabel("Точно?"); fx.After(3, () => { armed = false; if (reset != null) reset.SetLabel("Сброс"); }); return; }
                armed = false; MetaLogic.Wipe(); Build(null);
            }, 12);
            reset.Size(100, 38);
        }

        void RenderHub()
        {
            heroTip.text = MetaLogic.HeroTip(meta);
            FillLog(journalList, meta.journal, 13);
        }

        void FillLog(RectTransform list, List<LogEntry> entries, int size)
        {
            Kit.Clear(list);
            foreach (var e in entries)
            {
                var t = Kit.Txt(list, e.text, size, Kit.LogColor(e.kind));
                t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        // ---------- карта мира ----------
        RectTransform worldMap, pinsRoot, tombBody, tombList;
        Text wFuel;
        Button btnFuel;
        string selTomb;

        static Vector2 CellPct(int col, int row) => new Vector2((col + .5f) / Data.WorldCols, 1 - (row + .5f) / Data.WorldRows);

        void BuildWorld()
        {
            var s = NewScreen("world");
            worldMap = Frame(s, 18, Top, 874, 462);
            var img = Kit.Node("World", worldMap).Fill().gameObject.AddComponent<RawImage>(); img.texture = MapArt.World(); img.raycastTarget = false;
            var title = Kit.PlateLabel(worldMap, "Карта экспедиций", 13); ((RectTransform)title.transform.parent).Place(10, 10, 10, 10);
            pinsRoot = Kit.Node("Pins", worldMap).Fill();

            var fp = Kit.Panel(s, "Заправка", 916, Top + 16, 346, 112);
            Kit.Icon(fp, "fuel", 36).rectTransform.Place(14, 24, 36, 36);
            var lbl = Kit.Txt(fp, "В БАКЕ", 11, Kit.ParchInk); lbl.rectTransform.Place(58, 24, 100, 16);
            wFuel = Kit.Txt(fp, "0", 20, Kit.Ink, TextAnchor.UpperLeft, true); wFuel.rectTransform.Place(58, 40, 100, 26);
            btnFuel = Kit.Btn(fp, "Купить 1", Kit.BtnStyle.Gold, BuyFuel); ((RectTransform)btnFuel.transform).Place(206, 26, 124, 40);
            var note = Kit.Txt(fp, $"Канистра стоит {Data.FuelPrice} монет.", 13, Kit.ParchInk); note.rectTransform.Place(14, 76, 320, 20);

            var tp = Kit.Panel(s, "Гробница", 916, Top + 154, 346, 262);
            tombBody = Kit.Node("Body", tp).Fill(14, 22, 14, 12);

            var lp = Kit.Panel(s, "Экспедиции", 916, Top + 442, 346, 166);
            var area = Kit.Node("Area", lp).Fill(10, 24, 10, 10);
            tombList = Kit.Scroll(area, 6);
        }

        void BuyFuel()
        {
            if (meta.coins < Data.FuelPrice) return;
            meta.coins -= Data.FuelPrice; meta.fuel++;
            var c = Fx.WorldCenter((RectTransform)btnFuel.transform);
            fx.Burst(c, "torch", 12); fx.FloatText(c, "+1 топливо", Kit.Gold);
            fx.Bump(chipFuel); RenderTop(); RenderWorld(); Save();
        }

        void RenderWorld()
        {
            var sorted = meta.tombs.OrderBy(t => t.looted).ThenBy(t => t.tier).ToList();
            if (selTomb == null || meta.TombById(selTomb) == null) selTomb = (sorted.FirstOrDefault(t => !t.looted) ?? sorted[0]).id;
            Kit.Clear(pinsRoot);
            Pin(pinsRoot, Data.HomeCol, Data.HomeRow, "pinhome", "Особняк", false, false, null);
            foreach (var t in meta.tombs) { var id = t.id; Pin(pinsRoot, t.col, t.row, "pin", t.name, t.looted, t.id == selTomb, () => { selTomb = id; RenderWorld(); }); }

            Kit.Clear(tombList);
            foreach (var t in sorted)
            {
                var id = t.id;
                var row = Kit.Node("Tomb", tombList); row.Size(-1, 46);
                var bg = Kit.Img(row, Pixel.Box(t.id == selTomb ? Kit.Gold : Kit.ParchLo, Kit.Ink, 2, 6), new Color(1, 1, 1, t.looted ? .6f : 1), "Bg"); bg.rectTransform.Fill(); bg.raycastTarget = true;
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = bg; b.onClick.AddListener(() => { selTomb = id; RenderWorld(); });
                Kit.Icon(row, t.looted ? "exit" : "pin", 24).rectTransform.Place(8, 11, 24, 24);
                var nm = Kit.Txt(row, t.name, 13, Kit.Ink, TextAnchor.UpperLeft, true); nm.rectTransform.Place(40, 5, 220, 18);
                var sub = Kit.Txt(row, $"{Data.Cult(t.cult).Name} · {t.size}×{t.size}" + (t.looted ? " · разграблена" : ""), 11, Kit.ParchInk); sub.rectTransform.Place(40, 24, 220, 16);
                Kit.Icon(row, "fuel", 18).rectTransform.Place(282, 14, 18, 18);
                var f = Kit.Txt(row, t.fuel.ToString(), 14, Kit.Ink, TextAnchor.MiddleLeft, true); f.rectTransform.Place(304, 12, 20, 22);
            }

            wFuel.text = meta.fuel.ToString();
            btnFuel.interactable = meta.coins >= Data.FuelPrice;

            Kit.Clear(tombBody);
            var tomb = meta.TombById(selTomb);
            var title = Kit.Txt(tombBody, tomb.name, 16, Kit.Ink, TextAnchor.UpperLeft, true); title.rectTransform.Place(0, 0, 318, 22);
            var info = Kit.Txt(tombBody, $"Регион: {Data.Cult(tomb.cult).Name}\nРазмер: {tomb.size}×{tomb.size} плит\nТопливо: {tomb.fuel} (в баке {meta.fuel})\nАртефакты: {tomb.art}{(tomb.legendary ? ", ходят слухи о легендарном" : "")}" + (tomb.attempts > 0 ? $"\nПопыток: {tomb.attempts}" : ""), 13, Kit.ParchInk);
            info.rectTransform.Place(0, 26, 318, 90);
            var often = Kit.Txt(tombBody, "Часто встречается:", 12, Kit.ParchInk); often.rectTransform.Place(0, 122, 318, 16);
            var traits = Kit.HRow(tombBody, 6); traits.Place(0, 142, 318, 28);
            foreach (var o in MetaLogic.Traits(tomb)) TraitChip(traits, o, Data.Obst(o).Name);
            if (tomb.looted)
            {
                var p = Kit.PlateLabel(tombBody, "Разграблена", 12); ((RectTransform)p.transform.parent).Place(0, 184, 10, 10);
            }
            else
            {
                var go = Kit.Btn(tombBody, "Собраться в путь", Kit.BtnStyle.Gold, () => { loadout = MetaLogic.AutoPack(meta, meta.TombById(selTomb)); Go("loadout"); });
                ((RectTransform)go.transform).Place(0, 182, 200, 42);
                go.interactable = meta.fuel >= tomb.fuel;
                if (meta.fuel < tomb.fuel) { var w = Kit.Txt(tombBody, "Не хватает топлива", 12, Kit.Danger); w.rectTransform.Place(210, 192, 120, 20); }
            }
        }

        void TraitChip(Transform parent, string obst, string label)
        {
            var chip = Kit.Node("Trait", parent);
            var bg = Kit.Img(chip, Pixel.Box(Kit.ParchLo, Kit.Ink, 2, 5), Color.white, "Bg"); bg.rectTransform.Fill(); bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var lay = chip.gameObject.AddComponent<HorizontalLayoutGroup>(); lay.padding = new RectOffset(4, 8, 3, 3); lay.spacing = 4; lay.childAlignment = TextAnchor.MiddleLeft;
            lay.childControlWidth = lay.childControlHeight = true; lay.childForceExpandWidth = false;
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = chip.Size(-1, 26); le.layoutPriority = 2; le.flexibleHeight = 0;
            Kit.Icon(chip, obst, 20).Size(20, 20);
            var t = Kit.Txt(chip, label, 12, Kit.Ink, TextAnchor.MiddleLeft); t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void Pin(RectTransform root, int col, int row, string sprite, string name, bool looted, bool sel, Action onClick)
        {
            var p = Kit.Node("Pin " + name, root);
            var pct = CellPct(col, row);
            p.anchorMin = p.anchorMax = pct; p.pivot = new Vector2(.5f, .12f); p.sizeDelta = new Vector2(30, 30);
            var img = Kit.Icon(p, sprite, 30); img.rectTransform.Fill(); img.color = looted ? new Color(.6f, .6f, .6f, .75f) : Color.white;
            var plate = Kit.PlateLabel(p, name, 11); var prt = (RectTransform)plate.transform.parent;
            prt.anchorMin = prt.anchorMax = new Vector2(.5f, 0); prt.pivot = new Vector2(.5f, 1); prt.anchoredPosition = new Vector2(0, -2);
            prt.gameObject.SetActive(sel);
            if (sel) p.gameObject.AddComponent<Bob>().on = true;
            img.raycastTarget = true;
            var hv = p.gameObject.AddComponent<Hover>(); hv.enter = () => prt.gameObject.SetActive(true); hv.exit = () => prt.gameObject.SetActive(sel);
            if (onClick != null) { var b = p.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.transition = Selectable.Transition.None; b.onClick.AddListener(() => onClick()); }
        }

        // ---------- сборы ----------
        int[] loadout = new int[Data.Types.Length];
        RectTransform loadGrid, loadInfo;
        Text bagCount, loadTitle;
        Button btnFly;

        Vector2 SlotPos(int i) => new Vector2(18 + (i % 6) * 142, Top + 8 + (i / 6) * 278);

        void BuildLoadout()
        {
            var s = NewScreen("loadout");
            loadGrid = Kit.Node("Grid", s).Fill();
            var p = Kit.Panel(s, "Сумка экспедиции", 916, Top + 16, 346, 590);
            bagCount = Kit.Txt(p, "", 14, Kit.Ink, TextAnchor.UpperRight, true); bagCount.rectTransform.Place(200, 10, 130, 20);
            loadTitle = Kit.Txt(p, "", 16, Kit.Ink, TextAnchor.UpperLeft, true); loadTitle.rectTransform.Place(14, 30, 318, 44);
            loadInfo = Kit.Node("Info", p).Place(14, 80, 318, 330);
            var r1 = Kit.HRow(p, 8); r1.Place(14, 430, 318, 40);
            Kit.Btn(r1, "Собрать сам", Kit.BtnStyle.Parch, () => { loadout = MetaLogic.AutoPack(meta, meta.TombById(selTomb)); RenderLoadout(); }, 13).Size(150, 38);
            Kit.Btn(r1, "Выложить всё", Kit.BtnStyle.Parch, () => { loadout = new int[Data.Types.Length]; RenderLoadout(); }, 13).Size(150, 38);
            var r2 = Kit.HRow(p, 8); r2.Place(14, 484, 318, 46);
            Kit.Btn(r2, "К карте", Kit.BtnStyle.Parch, () => Go("world")).Size(110, 44);
            btnFly = Kit.Btn(r2, "Вылет", Kit.BtnStyle.Gold, Fly); btnFly.Size(200, 44);
        }

        int BagTotal => loadout.Sum();

        void RenderLoadout()
        {
            var tomb = meta.TombById(selTomb);
            if (tomb == null) { Go("world"); return; }
            loadTitle.text = "Сборы: " + tomb.name;
            int n = BagTotal;
            bagCount.text = $"{n}/{Data.BagCap}";
            Kit.Clear(loadGrid);
            for (int i = 0; i < Data.Types.Length; i++)
            {
                int idx = i; var type = Data.Types[i];
                var pos = SlotPos(i);
                var card = CardView.Build(loadGrid, type, false);
                card.Place(pos.x, pos.y, CardView.W, CardView.H);
                if (loadout[i] > 0) { var hl = Kit.Img(card, Pixel.Box(Color.clear, Kit.Gold, 3, 8), Color.white, "Selected"); hl.rectTransform.Fill(-3, -3, -3, -3); }
                int have = meta.collection[i];
                var meta1 = Kit.Txt(loadGrid, $"в коллекции: {have - loadout[i]}", 12, Kit.Cream); meta1.rectTransform.Place(pos.x, pos.y + CardView.H + 10, CardView.W, 18);
                var dec = Kit.Btn(loadGrid, "−", Kit.BtnStyle.Parch, () => { if (loadout[idx] > 0) loadout[idx]--; RenderLoadout(); }, 18);
                ((RectTransform)dec.transform).Place(pos.x, pos.y + CardView.H + 32, 36, 34); dec.interactable = loadout[i] > 0;
                var cnt = Kit.Title(loadGrid, loadout[i].ToString(), 20, Kit.Gold); cnt.alignment = TextAnchor.MiddleCenter; cnt.rectTransform.Place(pos.x + 36, pos.y + CardView.H + 32, 48, 34);
                var inc = Kit.Btn(loadGrid, "+", Kit.BtnStyle.Parch, () => { if (loadout[idx] < meta.collection[idx] && BagTotal < Data.BagCap) loadout[idx]++; RenderLoadout(); }, 18);
                ((RectTransform)inc.transform).Place(pos.x + 84, pos.y + CardView.H + 32, 36, 34); inc.interactable = loadout[i] < have && n < Data.BagCap;
            }
            Kit.Clear(loadInfo);
            var col = Kit.VCol(loadInfo, 6); col.Fill();
            Kit.Txt(col, $"{Data.Cult(tomb.cult).Name}, {tomb.size}×{tomb.size}. Часто встречается:", 13, Kit.ParchInk).Size(-1, 20);
            foreach (var o in MetaLogic.Traits(tomb)) { var r = Kit.HRow(col, 4); r.Size(-1, 28); TraitChip(r, o, $"{Data.Obst(o).Name} → {Data.ToolsFor(o)}"); }
            Kit.Txt(col, $"Топливо: {tomb.fuel} из {meta.fuel}\nВ сумке: {n} из {Data.BagCap}", 13, Kit.Ink).Size(-1, 38);
            if (loadout[Data.TypeIndex("scroll")] == 0) Kit.Txt(col, "Выход всегда под печатью. Без Свитка придётся надеяться на сундуки.", 12, Kit.Danger).Size(-1, 32);
            if (n < Data.StartHand) Kit.Txt(col, $"Возьмите хотя бы {Data.StartHand} карты.", 12, Kit.Danger).Size(-1, 18);
            if (meta.fuel < tomb.fuel) Kit.Txt(col, "Не хватает топлива.", 12, Kit.Danger).Size(-1, 18);
            Kit.Txt(col, "Неиспользованные карты вернутся в коллекцию.", 12, Kit.ParchInk).Size(-1, 18);
            btnFly.interactable = n >= Data.StartHand && meta.fuel >= tomb.fuel;
            btnFly.SetLabel($"Вылет (−{tomb.fuel} топлива)");
        }

        void Fly()
        {
            var t = meta.TombById(selTomb);
            if (t == null || meta.fuel < t.fuel || BagTotal < Data.StartHand) return;
            meta.fuel -= t.fuel;
            for (int i = 0; i < loadout.Length; i++) meta.collection[i] -= loadout[i];
            meta.Journal($"Вылет в «{t.name}». Топливо −{t.fuel}, в сумке {BagTotal} карт.");
            run = Run.Create(t, loadout);
            loadout = new int[Data.Types.Length];
            boardBuiltFor = null;
            Save();
            StartFlight(Data.HomeCol, Data.HomeRow, true, t.col, t.row, false, new[] { "Вылет из Эшфорда", "Курс: " + t.name, $"Топливо −{t.fuel}" }, () => Go("dungeon"));
        }

        // ---------- лавка ----------
        RectTransform shopGrid;

        void BuildShop()
        {
            var s = NewScreen("shop");
            shopGrid = Kit.Node("Grid", s).Fill();
            var p = Kit.Panel(s, "Лавка снаряжения", 916, Top + 16, 346, 150);
            var av = Kit.Avatar(p, "keeper", Kit.ParchLo, 64); av.Place(14, 28, 64, 64);
            var t = Kit.Txt(p, "Старый Гарри: «Всё, что нужно для гробниц. Карты попадают в вашу коллекцию, а перед вылетом вы решаете, что положить в сумку».", 13, Kit.Ink);
            t.rectTransform.Place(90, 24, 240, 120);
        }

        void RenderShop()
        {
            Kit.Clear(shopGrid);
            for (int i = 0; i < Data.Types.Length; i++)
            {
                var type = Data.Types[i]; var def = Data.Card(type);
                var pos = SlotPos(i);
                var card = CardView.Build(shopGrid, type, false); card.Place(pos.x, pos.y, CardView.W, CardView.H);
                var have = Kit.Txt(shopGrid, $"есть: {meta.collection[i]}", 12, Kit.Cream); have.rectTransform.Place(pos.x, pos.y + CardView.H + 10, CardView.W, 18);
                Kit.Icon(shopGrid, "gold", 20).rectTransform.Place(pos.x, pos.y + CardView.H + 38, 20, 20);
                var price = Kit.Txt(shopGrid, def.Price.ToString(), 15, Kit.Cream, TextAnchor.MiddleLeft, true); price.rectTransform.Place(pos.x + 24, pos.y + CardView.H + 36, 30, 24);
                Button buy = null;
                buy = Kit.Btn(shopGrid, "Купить", Kit.BtnStyle.Gold, () =>
                {
                    if (meta.coins < def.Price) return;
                    meta.coins -= def.Price; meta.Add(type, 1);
                    var c = Fx.WorldCenter((RectTransform)buy.transform);
                    fx.Burst(c, "gold", 10, .6f); fx.FloatText(c, "+1 " + def.Name, Kit.Gold);
                    fx.Bump(chipCol); RenderTop(); RenderShop(); Save();
                }, 13);
                ((RectTransform)buy.transform).Place(pos.x + 52, pos.y + CardView.H + 32, 68, 34);
                buy.interactable = meta.coins >= def.Price;
            }
        }
    }
}
