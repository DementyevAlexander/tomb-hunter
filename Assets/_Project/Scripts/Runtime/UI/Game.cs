using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TombHunter
{
    // Корень игры. Весь интерфейс строится из кода на сцене 1280×720, которая масштабируется под окно (CanvasScaler, Expand).
    public partial class Game : MonoBehaviour
    {
        public const float StageW = 1280, StageH = 720, Top = 98;

        public Meta meta;
        public Run run;
        public string screen = "hub";
        public bool debugReveal;

        Canvas canvas;
        RectTransform stage, modalLayer, modalBox, fxLayer;
        Fx fx;
        readonly Dictionary<string, RectTransform> screens = new Dictionary<string, RectTransform>();
        string metaModal = "";
        string modalKey = "";
        bool busy;

        // Нужен тестам и снимкам экрана: построить игру без сохранения.
        public static bool NoSave;

        void Awake() => Build(NoSave ? null : MetaLogic.Load());

        public void Build(SaveData saved)
        {
            Kit.Clear(transform);
            screens.Clear(); tileViews.Clear(); handViews.Clear(); flipAnim.Clear();
            metaModal = ""; modalKey = ""; selTomb = null; boardBuiltFor = null; flight = null; busy = false; aucSeen = -1; aucPhase = "";
            meta = saved?.meta ?? Meta.Fresh();
            run = saved != null && saved.hasRun ? saved.run : null;
            if (run != null) run.ClearEvents();
            if (meta.hasAuction && meta.auction == null) meta.hasAuction = false;
            screen = saved?.screen ?? "hub";
            if (screen == "flight") screen = run != null && !run.ended ? "dungeon" : "hub";
            if (screen == "dungeon" && run == null) screen = "hub";
            if (screen == "loadout") screen = "world";

            BuildCanvas();
            BuildTopbar();
            BuildHub(); BuildWorld(); BuildLoadout(); BuildFlight(); BuildDungeon(); BuildAuction(); BuildShop();
            BuildModal();
            fxLayer = Kit.Node("Fx", stage).Fill();
            fx = fxLayer.gameObject.AddComponent<Fx>();
            Go(screen);
        }

        void BuildCanvas()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var cgo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
                cam = cgo.GetComponent<Camera>();
            }
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Kit.BgBot;
            var cgoCanvas = Kit.Node("Canvas", transform).gameObject;
            canvas = cgoCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10; canvas.pixelPerfect = false;
            var scaler = cgoCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(StageW, StageH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cgoCanvas.AddComponent<GraphicRaycaster>();
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }
            var bg = Kit.Node("Background", canvas.transform).Fill().gameObject.AddComponent<RawImage>();
            bg.texture = MapArt.Gradient(); bg.raycastTarget = false;
            stage = Kit.Node("Stage", canvas.transform);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f, .5f);
            stage.sizeDelta = new Vector2(StageW, StageH); stage.anchoredPosition = Vector2.zero;
        }

        RectTransform NewScreen(string id)
        {
            var r = Kit.Node("Screen " + id, stage).Fill();
            screens[id] = r;
            return r;
        }

        // ---------- верхняя плашка ----------
        RectTransform resMeta, resRun;
        Text vCoins, vFuel, vBag, vCol, rName, rDeck, rNext, rOpen, rCoins, rArt, rExit;
        RectTransform chipCoins, chipFuel, chipBag, chipCol, statDeck, statArt;
        Button btnCity;

        void BuildTopbar()
        {
            var bar = Kit.Node("Topbar", stage).Place(18, 14, StageW - 36, 72);
            var sh = Kit.Img(bar, Kit.ShadowSprite, Kit.Ink, "Shadow"); sh.rectTransform.Fill(0, 6, 0, -6);
            var bg = Kit.Img(bar, Kit.PlateSprite, Color.white, "Bg"); bg.rectTransform.Fill();
            var av = Kit.Avatar(bar, "hero", Kit.ParchLo, 56); av.Place(10, 8, 56, 56);
            var nm = Kit.Txt(bar, "Ада Рейвенкрофт", 19, Kit.Gold, TextAnchor.MiddleLeft, true); nm.rectTransform.Place(78, 12, 300, 26);
            var sub = Kit.Txt(bar, "расхитительница гробниц", 14, Kit.Cream, TextAnchor.MiddleLeft); sub.rectTransform.Place(78, 38, 300, 20);

            resMeta = Kit.HRow(bar, 10, TextAnchor.MiddleRight); RightRow(resMeta);
            chipCoins = Kit.Chip(resMeta, "gold", "0", out vCoins);
            chipFuel = Kit.Chip(resMeta, "fuel", "0", out vFuel);
            chipBag = Kit.Chip(resMeta, "backpack", "0", out vBag, "рюкзак"); MakeClickable(chipBag, () => OpenMeta("backpack"));
            chipCol = Kit.Chip(resMeta, "deck", "0", out vCol, "карты"); MakeClickable(chipCol, () => OpenMeta("collection"));
            btnCity = Kit.Btn(resMeta, "В город", Kit.BtnStyle.Parch, () => Go("hub"));
            btnCity.Size(110, 40);

            resRun = Kit.HRow(bar, 8, TextAnchor.MiddleRight); RightRow(resRun);
            rName = Kit.PlateLabel(resRun, "Гробница", 12);
            statDeck = Kit.Chip(resRun, "deck", "0", out rDeck, "в сумке");
            Kit.Chip(resRun, "hour", "2", out rNext, "до карты");
            Kit.Chip(resRun, "map", "1", out rOpen);
            Kit.Chip(resRun, "gold", "0", out rCoins);
            statArt = Kit.Chip(resRun, "backpack", "0", out rArt);
            rExit = Kit.PlateLabel(resRun, "выход не найден", 11);
        }

        static void RightRow(RectTransform row)
        {
            row.anchorMin = row.anchorMax = new Vector2(1, .5f); row.pivot = new Vector2(1, .5f);
            row.anchoredPosition = new Vector2(-14, 0); row.sizeDelta = new Vector2(900, 48);
        }

        static void MakeClickable(RectTransform chip, Action onClick)
        {
            var img = chip.Find("Bg").GetComponent<Image>(); img.raycastTarget = true;
            var b = chip.gameObject.AddComponent<Button>(); b.targetGraphic = img; b.onClick.AddListener(() => onClick());
        }

        void RenderTop()
        {
            vCoins.text = meta.coins.ToString(); vFuel.text = meta.fuel.ToString(); vBag.text = meta.backpack.Count.ToString(); vCol.text = meta.CollectionTotal.ToString();
            bool inRun = screen == "dungeon" && run != null;
            resMeta.gameObject.SetActive(!inRun); resRun.gameObject.SetActive(inRun);
            btnCity.gameObject.SetActive(screen != "hub" && screen != "dungeon" && screen != "flight");
            if (inRun)
            {
                var tomb = meta.TombById(run.tombId);
                rName.text = $"{tomb.name} · {Data.Cult(tomb.cult).Name}";
                rDeck.text = run.deck.Count.ToString(); rNext.text = run.flipsToDraw.ToString();
                rOpen.text = $"{run.opened}/{run.size * run.size}"; rCoins.text = run.coins.ToString(); rArt.text = run.artifacts.Count.ToString();
                rExit.text = "выход " + (run.exitFound ? "найден" : run.exitMarked ? "на плане" : "не найден");
                rExit.color = run.exitFound ? Pixel.Hex("#9ff0a0") : run.exitMarked ? Kit.Gold : Kit.Cream;
            }
        }

        // ---------- навигация ----------
        public void Go(string id)
        {
            screen = id;
            foreach (var kv in screens) kv.Value.gameObject.SetActive(kv.Key == id);
            RenderTop();
            switch (id)
            {
                case "hub": RenderHub(); break;
                case "world": RenderWorld(); break;
                case "loadout": RenderLoadout(); break;
                case "shop": RenderShop(); break;
                case "auction": RenderAuction(false); break;
                case "dungeon": RenderDungeon(); break;
            }
            RenderModal();
            Save();
        }

        // Для тестов и отладки: сразу высадиться в гробницу с автосборами.
        public void DebugStartRun(string tombId)
        {
            var t = meta.TombById(tombId);
            selTomb = tombId;
            run = Run.Create(t, MetaLogic.AutoPack(meta, t));
            boardBuiltFor = null;
            Go("dungeon");
        }

        public void DebugGo(string id, string tombId = null) { if (tombId != null) selTomb = tombId; if (id == "loadout") loadout = MetaLogic.AutoPack(meta, meta.TombById(selTomb ?? meta.tombs[0].id)); if (id == "loadout" && selTomb == null) selTomb = meta.tombs[0].id; Go(id); }
        public void DebugFlight(float progress)
        {
            var t = meta.tombs[2];
            StartFlight(Data.HomeCol, Data.HomeRow, true, t.col, t.row, false, new[] { "Вылет из Эшфорда", "Курс: " + t.name }, () => Go("hub"));
            flight.t = flight.dur * progress;
        }
        public bool Busy => busy;
        public string ModalKind => modalLayer.gameObject.activeSelf ? modalKey : "";
        public void DebugOpenMeta(string kind) => OpenMeta(kind);

        public void Save()
        {
            if (NoSave) return;
            MetaLogic.Save(new SaveData { meta = meta, run = run, hasRun = run != null, screen = screen });
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) OnEscape();
            TickAuction();
            TickFlight();
            TickDungeonFx();
        }

        void OnEscape()
        {
            bool dungeonModal = screen == "dungeon" && run != null && !string.IsNullOrEmpty(run.modal);
            if (!string.IsNullOrEmpty(metaModal) && !dungeonModal) { metaModal = ""; RenderModal(); return; }
            if (screen == "dungeon" && run != null)
            {
                if (run.modal == "exit" || run.modal == "evac" || run.modal == "summary") { run.modal = ""; AfterAction(); return; }
                if (run.selected >= 0) { run.selected = -1; RenderDungeon(); }
            }
        }

        // ---------- модальные окна ----------
        void BuildModal()
        {
            modalLayer = Kit.Node("Modal", stage).Fill();
            var dim = Kit.Img(modalLayer, Pixel.Square(), new Color(Kit.Ink.r, Kit.Ink.g, Kit.Ink.b, .55f), "Dim"); dim.rectTransform.Fill(); dim.raycastTarget = true;
            modalBox = Kit.Node("Box", modalLayer);
            modalBox.anchorMin = modalBox.anchorMax = modalBox.pivot = new Vector2(.5f, .5f);
            modalBox.sizeDelta = new Vector2(540, 300);
            var frameSh = Kit.Img(modalBox, Pixel.Box(Kit.Ink, Kit.Ink, 3, 8), Kit.Ink, "Shadow"); frameSh.rectTransform.Fill(-9, -9, -9, -17);
            frameSh.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var brick = Kit.Img(modalBox, Pixel.Box(Kit.Brick, Kit.Ink, 3, 8), Color.white, "Brick"); brick.rectTransform.Fill(-9, -9, -9, -9);
            brick.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var bg = Kit.Img(modalBox, Kit.PanelSprite, Color.white, "Bg"); bg.rectTransform.Fill();
            bg.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var lay = modalBox.gameObject.AddComponent<VerticalLayoutGroup>();
            lay.padding = new RectOffset(22, 22, 34, 22); lay.spacing = 12; lay.childControlWidth = lay.childControlHeight = true; lay.childForceExpandHeight = false;
            modalBox.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            modalLayer.gameObject.SetActive(false);
        }

        void OpenMeta(string kind)
        {
            if (screen == "dungeon" && run != null && !string.IsNullOrEmpty(run.modal)) return;
            metaModal = kind; RenderModal();
        }

        RectTransform modalContent;

        // Сначала окно гробницы (алтарь, выход, итоги), иначе окно города (рюкзак, коллекция, справка).
        void RenderModal()
        {
            string kind = screen == "dungeon" && run != null && !string.IsNullOrEmpty(run.modal) ? "run:" + run.modal : !string.IsNullOrEmpty(metaModal) ? "meta:" + metaModal : "";
            if (kind == "") { modalLayer.gameObject.SetActive(false); modalKey = ""; return; }
            string key = kind + "|" + meta.backpack.Count + "|" + meta.CollectionTotal + "|" + (run != null ? run.log.Count : 0);
            if (modalLayer.gameObject.activeSelf && key == modalKey) return;
            modalKey = key;
            modalLayer.gameObject.SetActive(true);
            modalLayer.SetAsLastSibling(); fxLayer.SetAsLastSibling();
            for (int i = modalBox.childCount - 1; i >= 0; i--) { var ch = modalBox.GetChild(i); if (ch.name != "Shadow" && ch.name != "Brick" && ch.name != "Bg") Kit.Kill(ch.gameObject); }
            modalContent = modalBox;
            if (kind.StartsWith("run:")) BuildRunModal(run.modal); else BuildMetaModal(metaModal);
            fx.Tween(.25f, k => modalBox.localScale = Vector3.one * Mathf.Lerp(.88f, 1, Fx.OutBack(k)));
        }

        Text ModalTitle(string title)
        {
            var t = Kit.Banner(modalBox, title, 0, -19);
            var plate = (RectTransform)t.transform.parent;
            plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            plate.anchorMin = plate.anchorMax = new Vector2(.5f, 1); plate.pivot = new Vector2(.5f, 1); plate.anchoredPosition = new Vector2(0, 19);
            return t;
        }

        Text ModalText(string text, Color? color = null, int size = 15)
        {
            var t = Kit.Txt(modalBox, text, size, color ?? Kit.ParchInk);
            return t;
        }

        RectTransform ModalRow()
        {
            var row = Kit.HRow(modalBox, 10);
            row.Size(-1, 44);
            return row;
        }

        Button ModalBtn(RectTransform row, string label, Kit.BtnStyle style, Action a, float w = 0)
        {
            var b = Kit.Btn(row, label, style, a);
            b.Size(w > 0 ? w : Mathf.Max(120, label.Length * 9 + 30), 42);
            return b;
        }

        void ScoreLine(RectTransform parent, string label, string value, bool total = false)
        {
            var row = Kit.HRow(parent, 8);
            var l = Kit.Txt(row, label, total ? 16 : 14, total ? Kit.Brick : Kit.ParchInk, TextAnchor.MiddleLeft, total); l.Size(-1, 22, 1);
            var v = Kit.Txt(row, value, total ? 16 : 14, total ? Kit.Brick : Kit.Ink, TextAnchor.MiddleRight, true); v.Size(220, 22);
        }

        RectTransform ArtRow(Transform parent, Artifact a, string btnLabel = null, Action onBtn = null, bool btnEnabled = true)
        {
            var row = Kit.Node("Art", parent);
            row.Size(-1, 62);
            var bg = Kit.Img(row, Pixel.Box(Kit.ParchLo, Kit.Ink, 2, 6), Color.white, "Bg"); bg.rectTransform.Fill();
            var ico = Kit.Img(row, Pixel.Box(Data.Rarity(a.rarity).Color, Kit.Ink, 2, 5), Color.white, "Ico"); ico.rectTransform.Place(7, 7, 48, 48);
            var s = Kit.Icon(ico.rectTransform, a.sprite, 36); s.rectTransform.Center(24, 24, 36, 36);
            float textW = btnLabel != null ? 170 : 290;
            var nm = Kit.Txt(row, a.name, 14, Kit.Ink, TextAnchor.UpperLeft, true); nm.rectTransform.Place(64, 6, textW, 20);
            var sub = Kit.Txt(row, $"<b>{Data.Rarity(a.rarity).Name.ToUpper()}</b> · {Data.Cult(a.culture).Name} · оценка {a.Estimate}", 11, Kit.ParchInk); sub.rectTransform.Place(64, 26, textW, 34);
            if (btnLabel != null)
            {
                var b = Kit.Btn(row, btnLabel, Kit.BtnStyle.Gold, onBtn, 12);
                ((RectTransform)b.transform).anchorMin = ((RectTransform)b.transform).anchorMax = new Vector2(1, .5f);
                ((RectTransform)b.transform).pivot = new Vector2(1, .5f); ((RectTransform)b.transform).anchoredPosition = new Vector2(-8, 2); ((RectTransform)b.transform).sizeDelta = new Vector2(92, 34);
                b.interactable = btnEnabled;
            }
            return row;
        }
    }
}
