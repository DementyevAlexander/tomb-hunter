using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Экран гробницы: поле плит слева, справа кнопки, подсказка и рука; все эффекты действий.
    public partial class Game
    {
        RectTransform dScreen, boardFrame, boardInner, handRoot, trayActs;
        Text hintText, handCount;
        Button btnPlay, btnSwap, btnCancel, btnLeave, btnResult, btnEvac, btnReveal;
        readonly List<TileView> tileViews = new List<TileView>();
        readonly Dictionary<int, RectTransform> handViews = new Dictionary<int, RectTransform>();
        readonly HashSet<int> flipAnim = new HashSet<int>();
        object boardBuiltFor;
        float tileSize;
        string hint = "";
        Vector3? flyFrom;
        Sprite sFog, sSlab, sFloor, sDark, sSlabBevel, sFogBevel;

        void BuildDungeon()
        {
            dScreen = NewScreen("dungeon");
            boardInner = Frame(dScreen, 18, Top, 608, 608);
            boardFrame = (RectTransform)boardInner.parent.parent;

            const float X = 648, W = 614;
            var tools = Kit.HRow(dScreen, 8, TextAnchor.MiddleRight); tools.Place(X, Top, W, 38);
            btnReveal = Kit.Btn(tools, "Показать всё: нет", Kit.BtnStyle.Dark, () => { debugReveal = !debugReveal; RenderDungeon(); }, 12); btnReveal.Size(170, 36);
            Kit.Btn(tools, "Справка", Kit.BtnStyle.Parch, () => OpenMeta("help"), 12).Size(100, 36);
            Kit.Btn(tools, "Журнал", Kit.BtnStyle.Parch, () => OpenMeta("log"), 12).Size(100, 36);
            btnEvac = Kit.Btn(tools, "Эвакуация", Kit.BtnStyle.Brick, () => { if (run != null && !run.ended && string.IsNullOrEmpty(run.modal)) { run.modal = "evac"; RenderDungeon(); } }, 12); btnEvac.Size(120, 36);

            var hb = Kit.Node("Hint", dScreen).Place(X, Top + 50, W, 70);
            var hsh = Kit.Img(hb, Kit.ShadowSprite, Kit.Ink, "Shadow"); hsh.rectTransform.Fill(0, 5, 0, -5);
            var hbg = Kit.Img(hb, Kit.PlateSprite, Color.white, "Bg"); hbg.rectTransform.Fill();
            hintText = Kit.Txt(hb, "", 15, Kit.Cream, TextAnchor.MiddleLeft); hintText.rectTransform.Fill(16, 6, 16, 6);

            var tray = Kit.Node("Tray", dScreen).Place(X, Top + 150, W, 456);
            var tsh = Kit.Img(tray, Kit.ShadowSprite, Kit.Ink, "Shadow"); tsh.rectTransform.Fill(0, 6, 0, -6);
            var tbg = Kit.Img(tray, Pixel.Box(Kit.PlateHi, Kit.Ink, 3, 6), Color.white, "Bg"); tbg.rectTransform.Fill();
            handCount = Kit.Banner(tray, "Рука", 12, -16);
            handRoot = Kit.Node("Hand", tray).Fill();
            trayActs = Kit.HRow(tray, 8, TextAnchor.MiddleRight);
            trayActs.anchorMin = trayActs.anchorMax = new Vector2(1, 1); trayActs.pivot = new Vector2(1, .5f); trayActs.anchoredPosition = new Vector2(-12, 0); trayActs.sizeDelta = new Vector2(560, 40);
            btnPlay = Kit.Btn(trayActs, "Сыграть", Kit.BtnStyle.Gold, () => { var c = run?.Selected; if (c != null && c.type == "map" && !busy && string.IsNullOrEmpty(run.modal) && !run.ended) DoMap(c); }, 13);
            btnSwap = Kit.Btn(trayActs, "Сменить карту", Kit.BtnStyle.Parch, DoSwap, 13);
            btnCancel = Kit.Btn(trayActs, "Отмена", Kit.BtnStyle.Parch, () => { run.selected = -1; RenderDungeon(); }, 13);
            btnLeave = Kit.Btn(trayActs, "Покинуть гробницу", Kit.BtnStyle.Gold, () => { if (!run.ended && run.exitFound) FinishRun("escaped"); }, 13);
            btnResult = Kit.Btn(trayActs, "Итоги", Kit.BtnStyle.Parch, () => { run.modal = "summary"; RenderDungeon(); }, 13);
            btnPlay.Size(110, 38); btnSwap.Size(150, 38); btnCancel.Size(100, 38); btnLeave.Size(190, 38); btnResult.Size(90, 38);
        }

        void BuildBoard()
        {
            Kit.Clear(boardInner);
            tileViews.Clear(); flipAnim.Clear();
            var tomb = meta.TombById(run.tombId);
            var theme = Data.Cult(tomb.cult).Theme;
            boardFrame.Find("Brick").GetComponent<Image>().sprite = Pixel.Brick(theme.Brick);
            sFog = Pixel.Noise(theme.Fog, 11); sSlab = Pixel.Noise(theme.Slab, 12); sFloor = Pixel.Noise(theme.Floor, 13);
            sDark = Pixel.Noise(Pixel.Hex("#3a2766"), 14);
            sSlabBevel = Pixel.BevelBox(Pixel.Shade(theme.Slab, .45f), Pixel.Shade(theme.Slab, -.35f));
            sFogBevel = Pixel.BevelBox(new Color(.08f, .04f, .12f, .28f), Color.clear, true);
            float inner = boardInner.rect.width > 10 ? boardInner.rect.width : 564;
            const float pad = 8, gap = 5;
            int n = run.size;
            tileSize = (inner - pad * 2 - gap * (n - 1)) / n;
            for (int i = 0; i < run.tiles.Count; i++)
            {
                var t = run.tiles[i];
                var v = new TileView(boardInner, i, tileSize);
                v.root.Place(pad + t.c * (tileSize + gap), pad + t.r * (tileSize + gap), tileSize, tileSize);
                int idx = i;
                v.button.onClick.AddListener(() => ClickTile(idx));
                var hv = v.root.gameObject.AddComponent<Hover>();
                var view = v;
                hv.enter = () => { view.hovered = true; if (run != null) hintText.text = run.Describe(run.tiles[idx], debugReveal || run.ended); };
                hv.exit = () => { view.hovered = false; if (run != null) hintText.text = HintText(); };
                v.shownOpen = t.open;
                tileViews.Add(v);
            }
            boardBuiltFor = run;
        }

        string HintText()
        {
            if (run.ended) return "Экспедиция окончена. Гробница раскрыта целиком.";
            var sel = run.Selected;
            if (sel != null)
            {
                if (sel.type == "map") return "Карта играется без цели: нажмите «Сыграть».";
                int n = run.tiles.Count(t => run.CanApply(sel.type, t));
                var def = Data.Card(sel.type);
                return n > 0 ? $"{def.Name}: {def.Text.ToLower()}. Подходящих плит: {n}." : $"{def.Name} сейчас некуда применить. Её можно сменить на карту из сумки.";
            }
            if (!string.IsNullOrEmpty(hint)) return hint;
            bool free = run.tiles.Any(t => run.IsAvail(t) && t.obs.Count == 0);
            return free ? "Переверните сияющую плиту или выберите карту в руке." : "Свободных плит нет. Выберите карту и примените её к препятствию.";
        }

        void RenderDungeon()
        {
            if (run == null) return;
            if (!ReferenceEquals(boardBuiltFor, run) || tileViews.Count != run.tiles.Count) BuildBoard();
            bool reveal = debugReveal || run.ended;
            var sel = run.Selected;
            for (int i = 0; i < run.tiles.Count; i++)
            {
                var t = run.tiles[i]; var v = tileViews[i];
                bool av = run.IsAvail(t), known = av || t.scouted || reveal;
                bool target = sel != null && av && run.CanApply(sel.type, t);
                bool dark = !t.open && known && t.Top == "dark";
                // рубашка
                v.back.sprite = dark ? sDark : av ? sSlab : sFog;
                v.backBevel.sprite = av ? sSlabBevel : sFogBevel;
                var list = known ? (reveal || t.Top != "dark" ? t.obs : new List<string> { "dark" }) : new List<string>();
                string key = string.Join(",", list) + (t.scouted && !av ? "s" : "");
                if (v.backKey != key) { v.SetObstacles(list, tileSize); v.backKey = key; }
                foreach (Transform o in v.obsRoot) o.GetComponent<Image>().color = !av && t.scouted && !reveal ? new Color(1, 1, 1, .75f) : Color.white;
                bool showExit = !t.open && t.content == "exit" && (run.exitMarked || reveal);
                v.badge.gameObject.SetActive(showExit);
                bool peek = reveal && !t.open && t.content != "empty" && t.content != "exit";
                v.peek.gameObject.SetActive(peek);
                if (peek) v.peek.sprite = Pixel.Get(ContentSprite(t));
                // лицо
                v.front.sprite = sFloor;
                bool icon = t.destroyed || t.content != "empty";
                v.frontIcon.gameObject.SetActive(icon);
                if (icon)
                {
                    v.frontIcon.sprite = Pixel.Get(t.destroyed ? "rubble" : ContentSprite(t));
                    v.frontIcon.color = new Color(1, 1, 1, t.destroyed ? .45f : t.used ? .4f : 1);
                }
                // состояние
                v.group.alpha = sel != null && av && !target ? .45f : 1;
                v.outlineMode = t.open ? 0 : target ? 2 : av && t.obs.Count == 0 && sel == null && !run.ended ? 1 : (!t.open && t.content == "exit" && run.exitMarked) ? 3 : 0;
                if (t.open && !v.shownOpen)
                {
                    v.shownOpen = true;
                    if (flipAnim.Remove(i)) AnimateFlip(v); else SetFaces(v, true);
                }
                else if (!flipAnim.Contains(i)) SetFaces(v, v.shownOpen);
            }

            Kit.Clear(handRoot); handViews.Clear();
            for (int i = 0; i < run.hand.Count; i++)
            {
                var c = run.hand[i]; int id = c.id;
                var cv = CardView.Build(handRoot, c.type, true, () => ClickCard(id));
                float x = 52 + (i % 4) * 130, y = 30 + (i / 4) * 210;
                cv.Place(x, y - (c.id == run.selected ? 14 : 0), CardView.W, CardView.H);
                if (c.id == run.selected)
                {
                    cv.localRotation = Quaternion.Euler(0, 0, 1.5f);
                    var glow = Kit.Img(cv, Pixel.Box(Color.clear, Kit.Gold, 3, 8), Color.white, "Selected"); glow.rectTransform.Fill(-4, -4, -4, -4);
                }
                handViews[id] = cv;
            }
            if (run.hand.Count == 0) { var e = Kit.Txt(handRoot, "Рука пуста.", 15, Kit.Cream, TextAnchor.MiddleCenter); e.rectTransform.Fill(); }
            handCount.text = $"РУКА {run.hand.Count}/{Data.HandMax}";

            hintText.text = HintText();
            bool live = !run.ended;
            btnPlay.gameObject.SetActive(live && sel != null && sel.type == "map");
            btnSwap.gameObject.SetActive(live && sel != null);
            btnSwap.interactable = run.deck.Count > 0;
            btnSwap.SetLabel(run.deck.Count > 0 ? "Сменить карту" : "Сумка пуста");
            btnCancel.gameObject.SetActive(live && sel != null);
            btnLeave.gameObject.SetActive(live && run.exitFound);
            btnResult.gameObject.SetActive(run.ended && string.IsNullOrEmpty(run.modal));
            btnEvac.gameObject.SetActive(live);
            btnReveal.SetLabel(debugReveal ? "Показать всё: да" : "Показать всё: нет");
            RenderTop();
            RenderModal();
            RunCardFlights();
            Save();
        }

        static string ContentSprite(Tile t) => t.content == "artifact" ? (t.hasArt && t.art != null ? t.art.sprite : "chest") : t.content;

        void SetFaces(TileView v, bool open)
        {
            v.backRoot.gameObject.SetActive(!open);
            v.frontRoot.gameObject.SetActive(open);
            v.flip.localScale = Vector3.one;
        }

        void AnimateFlip(TileView v)
        {
            SetFaces(v, false);
            fx.Tween(.24f, k => v.flip.localScale = new Vector3(1 - k, 1 + .16f * k, 1), () =>
            {
                SetFaces(v, true);
                fx.Tween(.34f, k => v.flip.localScale = new Vector3(Fx.OutBack(k), 1.16f - .16f * k, 1), () => v.flip.localScale = Vector3.one);
            });
        }

        // Пульс золотой рамки, покачивание целей, сияние свободных плит.
        void TickDungeonFx()
        {
            if (screen != "dungeon" || run == null) return;
            float t = Time.unscaledTime;
            foreach (var v in tileViews)
            {
                float a = 0;
                switch (v.outlineMode)
                {
                    case 1: a = .65f + .35f * Mathf.Sin(t * 4); break;
                    case 2: a = .8f + .2f * Mathf.Sin(t * 8); break;
                    case 3: a = Mathf.Repeat(t, 1.2f) < .6f ? 1 : .25f; break;
                }
                v.outline.color = new Color(1, 1, 1, a);
                v.glow.color = new Color(1, 1, 1, v.outlineMode == 1 || v.outlineMode == 2 ? a * .5f : 0);
                v.flip.anchoredPosition = new Vector2(0, v.outlineMode == 2 ? Mathf.Abs(Mathf.Sin(t * 3.5f)) * 3 : v.hovered && v.outlineMode == 1 ? 4 : 0);
            }
        }

        // ---------- ввод ----------
        public void ClickCard(int id)
        {
            if (run == null || busy || run.ended || !string.IsNullOrEmpty(run.modal)) return;
            run.selected = run.selected == id ? -1 : id;
            hint = "";
            RenderDungeon();
        }

        public void ClickTile(int i)
        {
            if (run == null || busy || run.ended || !string.IsNullOrEmpty(run.modal)) return;
            var t = run.tiles[i]; var sel = run.Selected;
            if (sel != null)
            {
                if (sel.type == "map") { hint = "Карта играется без цели: нажмите «Сыграть»."; RenderDungeon(); return; }
                if (run.CanApply(sel.type, t)) { DoApply(sel, i); return; }
                run.selected = -1;
                hint = $"{Data.Card(sel.type).Name} сюда не подходит. {run.Describe(t, debugReveal)}";
                RenderDungeon(); return;
            }
            if (run.IsAvail(t) && t.obs.Count == 0) { DoFlip(i); return; }
            hint = run.Describe(t, debugReveal); RenderDungeon();
        }

        HashSet<int> AvailSet() => new HashSet<int>(Enumerable.Range(0, run.tiles.Count).Where(i => run.IsAvail(run.tiles[i])));

        Vector3 TileCenter(int i) => Fx.WorldCenter(tileViews[i].root);

        void LockFor(float s) { busy = true; fx.After(s, () => busy = false); }

        void DoFlip(int i)
        {
            var t = run.tiles[i];
            var prev = AvailSet();
            hint = "";
            run.ClearEvents();
            flyFrom = TileCenter(i);
            flipAnim.Add(i);
            LockFor(.42f);
            run.Flip(t);
            ProcessLost();
            AfterAction();
            RevealNew(prev);
            FxFlip(i, t);
        }

        void DoApply(Card card, int i)
        {
            var t = run.tiles[i];
            var prev = AvailSet();
            bool killsArt = card.type == "bomb" && t.content == "artifact";
            busy = true;
            if (handViews.TryGetValue(card.id, out var cv))
            {
                var cg = cv.GetComponent<CanvasGroup>(); if (cg == null) cg = cv.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0;
                fx.FlyClone(cv, TileCenter(i), Go2);
            }
            else Go2();

            void Go2()
            {
                busy = false; hint = "";
                run.ClearEvents();
                var removed = run.Apply(card, t);
                AfterAction();
                RevealNew(prev);
                FxApply(card.type, i, removed, killsArt);
            }
        }

        void DoMap(Card card)
        {
            int exitIdx = run.tiles.FindIndex(t => t.content == "exit");
            busy = true;
            void Go2()
            {
                busy = false; hint = "";
                run.ClearEvents();
                flyFrom = null;
                run.PlayMap(card);
                AfterAction();
                var c = TileCenter(exitIdx);
                fx.Burst(c, "map"); fx.Ring(c, Kit.Gold, 50); LitTile(exitIdx, 0);
            }
            if (handViews.TryGetValue(card.id, out var cv)) { var cg = cv.GetComponent<CanvasGroup>(); if (cg == null) cg = cv.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0; fx.FlyClone(cv, TileCenter(exitIdx), Go2); }
            else Go2();
        }

        public void ClickPlayMap() { var c = run?.Selected; if (c != null && c.type == "map" && !busy && string.IsNullOrEmpty(run.modal) && !run.ended) DoMap(c); }
        public void ClickTakeAltar(int k) { if (run == null || run.modal != "altar") return; run.ClearEvents(); run.TakeAltar(run.altarOptions[k]); AfterAction(); }
        public void ClickModal(string act)
        {
            if (run == null) return;
            if (act == "leave") { run.modal = ""; FinishRun("escaped"); }
            else if (act == "evac") { run.modal = ""; FinishRun("evac"); }
            else if (act == "close") { run.modal = ""; AfterAction(); }
            else if (act == "home") FlyHome();
        }
        public void ClickSwap() => DoSwap();

        void DoSwap()
        {
            var c = run?.Selected;
            if (c == null || run.deck.Count == 0 || busy || run.ended || !string.IsNullOrEmpty(run.modal)) return;
            hint = "";
            run.ClearEvents();
            flyFrom = null;
            LockFor(.3f);
            run.Swap(c);
            ProcessLost();
            AfterAction();
        }

        void AfterAction()
        {
            var res = run.After();
            if (res != null) FinishRun(res); else RenderDungeon();
        }

        void FinishRun(string res)
        {
            run.Finish(meta, res);
            RenderDungeon();
            RenderTop();
            Save();
        }

        // Потерянные карты падают из руки до перерисовки, пока их виды ещё на месте.
        void ProcessLost()
        {
            foreach (var (id, _, trap) in run.lost)
                if (handViews.TryGetValue(id, out var cv) && cv != null) fx.Drop(cv, trap);
        }

        void RunCardFlights()
        {
            if (run.fresh == null || run.fresh.Count == 0) { flyFrom = null; return; }
            var from = flyFrom ?? Fx.WorldCenter(statDeck);
            int k = 0;
            foreach (var id in run.fresh)
            {
                if (!handViews.TryGetValue(id, out var cv)) continue;
                var c = run.hand.Find(x => x.id == id);
                fx.FlyMini(c.type, from, cv, .12f + k * .14f, true);
                k++;
            }
            run.fresh.Clear();
            flyFrom = null;
        }

        void RevealNew(HashSet<int> prev)
        {
            int k = 0;
            for (int i = 0; i < run.tiles.Count; i++)
            {
                if (!run.IsAvail(run.tiles[i]) || prev.Contains(i)) continue;
                var v = tileViews[i];
                fx.Tween(.55f, t =>
                {
                    float s = t < .65f ? Mathf.Lerp(.6f, 1.07f, t / .65f) : Mathf.Lerp(1.07f, 1, (t - .65f) / .35f);
                    v.backRoot.localScale = Vector3.one * s;
                    v.flash.color = new Color(1, 1, 1, .75f * (1 - t));
                }, () => { v.backRoot.localScale = Vector3.one; v.flash.color = new Color(1, 1, 1, 0); }, .26f + k * .07f);
                k++;
            }
        }

        void LitTile(int i, float delay)
        {
            var v = tileViews[i];
            fx.Tween(.8f, t => v.flash.color = new Color(1, .95f, .6f, .8f * (1 - t)), () => v.flash.color = new Color(1, 1, 1, 0), delay);
        }

        void Quake(int i, float delay = 0) => fx.After(delay, () => fx.Shake(tileViews[i].root, 3));

        void Hit(int i) => fx.Tween(.3f, t => tileViews[i].root.localScale = Vector3.one * (t < .35f ? Mathf.Lerp(1, .88f, t / .35f) : t < .7f ? Mathf.Lerp(.88f, 1.06f, (t - .35f) / .35f) : Mathf.Lerp(1.06f, 1, (t - .7f) / .3f)), () => tileViews[i].root.localScale = Vector3.one);

        void FxApply(string type, int i, List<string> removed, bool killsArt)
        {
            var c = TileCenter(i);
            Hit(i);
            for (int k = 0; k < removed.Count; k++) fx.PopSprite(c, removed[k], k * .09f);
            switch (type)
            {
                case "torch": fx.Burst(c, removed.Count > 0 && removed[0] == "dark" ? "smoke" : "torch"); fx.Burst(c, "torch", 12); break;
                case "ice":
                    fx.Burst(c, "ice");
                    var v = tileViews[i]; fx.Tween(.6f, t => v.flash.color = new Color(.6f, .92f, 1f, .8f * (1 - t)), () => v.flash.color = new Color(1, 1, 1, 0));
                    break;
                case "bomb":
                    fx.Flash(c, 200); fx.Burst(c, "bomb"); fx.Shake(boardFrame);
                    foreach (var n in run.Nbrs(run.tiles[i])) Quake(run.Index(n));
                    fx.FloatText(c, killsArt ? "Артефакт разбит!" : "БУМ!", Pixel.Hex("#ff8a7a"));
                    break;
                case "lantern":
                    fx.Ring(c, Pixel.Hex("#fff2a8"), 70); fx.Burst(c, "lantern");
                    foreach (var j in run.lit) { var tt = run.tiles[j]; LitTile(j, (Mathf.Abs(tt.r - run.tiles[i].r) + Mathf.Abs(tt.c - run.tiles[i].c)) * .11f); }
                    break;
                case "scroll": fx.Burst(c, "scroll"); fx.Ring(c, Pixel.Hex("#b47cff"), 50); break;
                default: fx.Burst(c, type); break;
            }
        }

        void FxFlip(int i, Tile t)
        {
            var c = TileCenter(i);
            var quakes = new List<int>(run.quake);
            fx.After(.26f, () =>
            {
                fx.Burst(c, "dust");
                switch (t.content)
                {
                    case "chest": fx.Burst(c, "gold", 12); fx.FloatText(c, "+1 карта", Kit.Gold); break;
                    case "cache": fx.Burst(c, "gold", 16); fx.FloatText(c, "+2 карты", Kit.Gold); break;
                    case "gold": fx.Burst(c, "gold", -1, 1, 3); fx.FloatText(c, $"+{t.gold}", Kit.Gold); break;
                    case "artifact":
                        fx.Rays(c); fx.Burst(c, "relic"); fx.FloatText(c, t.art.name, Pixel.Hex("#9ff0a0"));
                        fx.FlyMini(t.art.sprite, c, statArt, .45f, false, () => { fx.Burst(Fx.WorldCenter(statArt), "gold", 10, .5f); fx.Bump(statArt); });
                        break;
                    case "altar": fx.Burst(c, "altar"); fx.FloatText(c, "Дар", Kit.Cream); break;
                    case "trap": fx.Burst(c, "trap"); Quake(i); fx.FloatText(c, "−1 карта", Pixel.Hex("#ff8a7a")); break;
                    case "collapse":
                        Quake(i); fx.Shake(boardFrame); fx.FloatText(c, "Обвал!", Pixel.Hex("#ff8a7a"));
                        for (int k = 0; k < quakes.Count; k++)
                        {
                            int j = quakes[k];
                            fx.After(.12f + k * .12f, () =>
                            {
                                var r = tileViews[j].root;
                                fx.Burst(r.TransformPoint(new Vector3(r.rect.center.x, r.rect.yMax + 4, 0)), "rocks");
                                Quake(j);
                            });
                        }
                        break;
                    case "exit": fx.Rays(c); fx.Burst(c, "exit"); fx.Ring(c, Kit.Gold, 70); fx.FloatText(c, "Выход!", Pixel.Hex("#9ff0a0")); break;
                }
            });
        }
    }
}
