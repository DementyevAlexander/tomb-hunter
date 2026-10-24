using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Аукционный дом: живые торги коллекционеров, игрок только выставляет лот и смотрит.
    public partial class Game
    {
        RectTransform aucStage, aucBag, aucBar;
        Text aucAmount;
        int aucSeen = -1;
        string aucPhase = "";

        void BuildAuction()
        {
            var s = NewScreen("auction");
            var p = Kit.Panel(s, "Аукционный дом Крейна", 18, Top + 16, 874, 590);
            aucStage = Kit.Node("Stage", p).Fill(18, 26, 18, 16);

            var bp = Kit.Panel(s, "Рюкзак", 916, Top + 16, 346, 330);
            var area = Kit.Node("Area", bp).Fill(10, 24, 10, 10);
            aucBag = Kit.Scroll(area, 8);

            var cp = Kit.Panel(s, "Коллекционеры", 916, Top + 372, 346, 236);
            var col = Kit.VCol(cp, 4); col.Fill(12, 22, 12, 8);
            foreach (var c in Data.Collectors)
            {
                var row = Kit.HRow(col, 8); row.Size(-1, 38);
                Kit.Avatar(row, c.Sprite, Pixel.Shade(c.Color, .72f), 36);
                var t = Kit.Txt(row, $"<b>{c.Name}</b>\n<size=11>собирает: {string.Join(", ", c.Likes.Select(l => Data.Cult(l).Name))}</size>", 13, Kit.Ink, TextAnchor.MiddleLeft);
                t.Size(270, 38);
            }
        }

        void RenderAuction(bool freshBid)
        {
            bool busyAuc = meta.hasAuction && !meta.auction.done;
            Kit.Clear(aucBag);
            if (meta.backpack.Count == 0) Kit.Txt(aucBag, "Рюкзак пуст. Артефакты добываются в гробницах.", 13, Kit.ParchInk).Size(-1, 40);
            foreach (var a in meta.backpack.ToList()) { var id = a.id; ArtRow(aucBag, a, "На торги", () => StartAuction(id), !busyAuc); }

            Kit.Clear(aucStage); aucBar = null; aucAmount = null;
            if (!meta.hasAuction)
            {
                var t = Kit.Txt(aucStage, "Выберите артефакт в рюкзаке и выставьте его на торги. Коллекционеры сами поднимают ставки, а молоток опускается, когда желающих перебить цену больше нет.\n\nКаждый коллекционер платит больше за свою культуру. Чем точнее артефакт попадёт в их интересы, тем жарче торги.", 16, Kit.ParchInk);
                t.rectTransform.Fill();
                return;
            }
            var a2 = meta.auction;
            var seen = a2.events.Where(e => e.t <= a2.elapsed).ToList();
            var cur = seen.LastOrDefault();
            var phase = a2.Phase;
            var lead = cur != null ? Data.CollectorById(cur.who) : null;

            var big = Kit.Img(aucStage, Pixel.Box(Data.Rarity(a2.art.rarity).Color, Kit.Ink, 3, 8), Color.white, "Lot"); big.rectTransform.Place(0, 0, 104, 104);
            Kit.Icon(big.rectTransform, a2.art.sprite, 76).rectTransform.Center(52, 52, 76, 76);
            var nm = Kit.Txt(aucStage, a2.art.name, 20, Kit.Ink, TextAnchor.UpperLeft, true); nm.rectTransform.Place(122, 8, 700, 28);
            var sub = Kit.Txt(aucStage, $"<b>{Data.Rarity(a2.art.rarity).Name.ToUpper()}</b> · {Data.Cult(a2.art.culture).Name} · из «{a2.art.from}»\nОценка экспертов: {a2.art.Estimate} монет", 14, Kit.ParchInk);
            sub.rectTransform.Place(122, 40, 700, 50);

            var bid = Kit.Node("Bid", aucStage).Place(0, 120, 838, 70);
            Kit.Img(bid, Kit.PlateSprite, Color.white, "Bg").rectTransform.Fill();
            aucAmount = Kit.Title(bid, (cur?.price ?? a2.start).ToString(), 34, Kit.Gold); aucAmount.rectTransform.Place(18, 8, 160, 54);
            if (freshBid) fx.Bump(aucAmount.rectTransform, .25f);
            if (lead != null) { var av = Kit.Avatar(bid, lead.Sprite, Pixel.Shade(lead.Color, .72f), 40); av.Place(180, 15, 40, 40); }
            var who = Kit.Txt(bid, lead != null ? "ставка: " + lead.Name : "стартовая цена", 16, Kit.Cream, TextAnchor.MiddleLeft); who.rectTransform.Place(lead != null ? 230 : 180, 10, 400, 50);
            var gavel = Kit.Txt(bid, phase == "sold" ? "Продано!" : phase == "twice" ? "Два!" : phase == "once" ? "Раз!" : "Кто больше?", 18, phase == "once" || phase == "twice" ? Kit.Gold : Kit.Cream, TextAnchor.MiddleRight, true);
            gavel.rectTransform.Place(560, 10, 260, 50);

            var bar = Kit.Img(aucStage, Pixel.Box(Kit.ParchLo, Kit.Ink, 2, 5), Color.white, "Bar"); bar.rectTransform.Place(0, 202, 838, 14);
            aucBar = Kit.Img(bar.rectTransform, Pixel.Box(Kit.Gold, Kit.Gold, 0, 3), Color.white, "Fill").rectTransform;
            aucBar.anchorMin = new Vector2(0, 0); aucBar.anchorMax = new Vector2(Mathf.Clamp01(a2.elapsed / a2.end), 1); aucBar.offsetMin = new Vector2(2, 2); aucBar.offsetMax = new Vector2(-2, -2);

            for (int i = 0; i < Data.Collectors.Length; i++)
            {
                var c = Data.Collectors[i];
                var mine = seen.Where(e => e.who == c.Id).ToList();
                bool isLead = lead != null && lead.Id == c.Id;
                var card = Kit.Node("Bidder", aucStage).Place(i * 168, 230, 158, 96);
                Kit.Img(card, Pixel.Box(isLead ? Kit.Gold : Kit.ParchLo, Kit.Ink, 2, 6), Color.white, "Bg").rectTransform.Fill();
                Kit.Avatar(card, c.Sprite, Pixel.Shade(c.Color, .72f), 44).Place(8, 8, 44, 44);
                var n = Kit.Txt(card, c.Name, 13, Kit.Ink, TextAnchor.UpperLeft, true); n.rectTransform.Place(58, 8, 96, 44);
                var st = Kit.Txt(card, mine.Count > 0 ? "ставка " + mine[mine.Count - 1].price : c.Likes.Contains(a2.art.culture) ? "присматривается" : "молчит", 12, Kit.ParchInk); st.rectTransform.Place(8, 62, 146, 28);
                if (freshBid && cur != null && cur.who == c.Id) fx.Bump(card, .1f);
            }

            var feedArea = Kit.Node("Feed", aucStage).Place(0, 340, 838, a2.done ? 110 : 160);
            var feed = Kit.Scroll(feedArea, 4);
            if (seen.Count == 0) Kit.Txt(feed, "Аукционист объявляет лот…", 14, Kit.ParchInk).Size(-1, 22);
            foreach (var e in Enumerable.Reverse(seen))
            {
                var c = Data.CollectorById(e.who);
                var row = Kit.HRow(feed, 10); row.Size(-1, 26);
                Kit.Txt(row, $"0:{(int)(e.t / 1000):00}", 13, Kit.ParchInk, TextAnchor.MiddleLeft).Size(44, 24);
                Kit.Avatar(row, c.Sprite, Pixel.Shade(c.Color, .72f), 24);
                Kit.Txt(row, c.Name, 14, Kit.Ink, TextAnchor.MiddleLeft).Size(560, 24);
                Kit.Txt(row, e.price.ToString(), 14, Kit.Ink, TextAnchor.MiddleRight, true).Size(160, 24);
            }

            if (a2.done)
            {
                var sold = Kit.Node("Sold", aucStage).Place(0, 462, 838, 96);
                Kit.Img(sold, Pixel.Box(Pixel.Hex("#ffd76a"), Kit.Ink, 3, 8), Color.white, "Bg").rectTransform.Fill();
                var w = Data.CollectorById(a2.winner);
                Kit.Avatar(sold, w.Sprite, Pixel.Shade(w.Color, .72f), 64).Place(14, 16, 64, 64);
                var h = Kit.Txt(sold, $"Продано за {a2.final} монет", 18, Kit.Ink, TextAnchor.UpperLeft, true); h.rectTransform.Place(92, 14, 500, 26);
                var d = Kit.Txt(sold, $"Покупатель: {w.Name}. Выручка уже в кошельке.", 14, Kit.ParchInk); d.rectTransform.Place(92, 44, 500, 40);
                var ok = Kit.Btn(sold, "Готово", Kit.BtnStyle.Gold, () => { meta.hasAuction = false; meta.auction = null; RenderAuction(false); Save(); });
                ((RectTransform)ok.transform).Place(700, 26, 120, 44);
            }
            else
            {
                var fast = Kit.Btn(aucStage, a2.speed > 1 ? "Обычная скорость" : "Быстрее ×3", Kit.BtnStyle.Parch, () => { a2.speed = a2.speed > 1 ? 1 : 3; RenderAuction(false); });
                ((RectTransform)fast.transform).Place(0, 516, 200, 42);
            }
        }

        void StartAuction(string id)
        {
            if (meta.hasAuction && !meta.auction.done) return;
            var art = meta.backpack.Find(a => a.id == id);
            if (art == null) return;
            meta.backpack.Remove(art);
            meta.auction = MetaLogic.CreateAuction(art); meta.hasAuction = true;
            aucSeen = 0; aucPhase = "bid";
            meta.Journal($"«{art.name}» выставлен на торги.");
            RenderTop(); RenderAuction(false); Save();
        }

        void TickAuction()
        {
            if (screen != "auction" || !meta.hasAuction || meta.auction.done) return;
            var a = meta.auction;
            a.elapsed += Mathf.Min(1.1f, Time.unscaledDeltaTime) * 1000 * a.speed;
            int seen = a.events.Count(e => e.t <= a.elapsed);
            var phase = a.Phase;
            if (seen != aucSeen || phase != aucPhase)
            {
                bool fresh = seen > aucSeen && aucSeen >= 0;
                aucSeen = seen; aucPhase = phase;
                if (phase == "sold")
                {
                    a.done = true; meta.coins += a.final;
                    meta.Journal($"{Data.CollectorById(a.winner).Name} купил «{a.art.name}» за {a.final} монет.", "good");
                }
                RenderAuction(fresh); RenderTop(); Save();
                if (aucAmount != null)
                {
                    var c = Fx.WorldCenter(aucAmount.rectTransform);
                    if (phase == "sold") { fx.Burst(c, "gold", 40, 1.2f); fx.Rays(c); fx.Ring(c, Kit.Gold); fx.Bump(chipCoins); }
                    else if (fresh) fx.Burst(c, "gold", 10, .6f);
                }
                return;
            }
            if (aucBar != null) aucBar.anchorMax = new Vector2(Mathf.Clamp01(a.elapsed / a.end), 1);
        }
    }
}
