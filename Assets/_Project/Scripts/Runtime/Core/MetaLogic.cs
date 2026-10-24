using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TombHunter
{
    // Мета-игра: новые гробницы, автосборы сумки, торги аукциона, подсказки героини, сохранение.
    public static class MetaLogic
    {
        const string SaveKey = "tomb-hunter-save-v1";

        public static Tomb SpawnTomb(Meta meta, int tier)
        {
            tier = Mathf.Min(4, tier);
            var cult = Data.Cultures[Random.Range(0, Data.Cultures.Length)].Id;
            (int c, int r)? spot = null;
            for (int i = 0; i < 400 && spot == null; i++)
            {
                int c = Random.Range(0, Data.WorldCols), r = 3 + Random.Range(0, 15);
                if (!Data.IsLand(c, r)) continue;
                if (Mathf.Abs(c - Data.HomeCol) + Mathf.Abs(r - Data.HomeRow) < 3) continue;
                if (meta.tombs.Any(t => Mathf.Abs(t.col - c) + Mathf.Abs(t.row - r) < 3)) continue;
                spot = (c, r);
            }
            if (spot == null) return null;
            string name = "";
            for (int i = 0; i < 20; i++)
            {
                name = Data.NameA[Random.Range(0, Data.NameA.Length)] + " " + Data.NameB[Random.Range(0, Data.NameB.Length)];
                if (!meta.tombs.Any(t => t.name == name)) break;
            }
            var order = new List<string> { "rubble", "lock", "web", "water", "seal" }.OrderBy(_ => Random.value).ToList();
            float[] ws = { 32, 22, 16, 10, 6 };
            var tomb = new Tomb
            {
                id = "t" + (++meta.tombCounter), name = name, cult = cult, col = spot.Value.c, row = spot.Value.r,
                size = Mathf.Min(9, 5 + tier + (Random.value < 0.5f ? 1 : 0)), fuel = Mathf.Min(4, tier), tier = tier, art = 1 + tier,
                legendary = tier >= 3 && Random.value < 0.5f, darkMul = 0.8f + Random.value * 1.2f, seed = Random.Range(0, 1000000),
            };
            for (int i = 0; i < order.Count; i++) tomb.obsW.Add(new Weight(order[i], ws[i]));
            meta.tombs.Add(tomb);
            return tomb;
        }

        public static List<string> Traits(Tomb t)
        {
            var top = t.obsW.OrderByDescending(w => w.w).Take(2).Select(w => w.id).ToList();
            if (t.darkMul >= 1.4f) top.Add("dark");
            top.Add("seal");
            return top.Distinct().ToList();
        }

        public static int[] AutoPack(Meta meta, Tomb t)
        {
            var want = new int[Data.Types.Length];
            var w = new Dictionary<string, float>();
            foreach (var o in t.obsW) { var k = Data.MainTool[o.id]; w[k] = (w.TryGetValue(k, out var v) ? v : 0) + o.w; }
            w["torch"] = (w.TryGetValue("torch", out var tv) ? tv : 0) + 12 * t.darkMul;
            float sum = w.Values.Sum();
            void Add(string k, int n)
            {
                int i = Data.TypeIndex(k);
                int can = Mathf.Min(n, meta.collection[i] - want[i], Data.BagCap - want.Sum());
                if (can > 0) want[i] += can;
            }
            Add("scroll", 1);
            int slots = Data.BagCap - 3;
            foreach (var p in w.OrderByDescending(p => p.Value)) Add(p.Key, Mathf.Max(1, Mathf.RoundToInt(slots * p.Value / sum)));
            Add("bomb", 1); Add("lantern", 1); Add("map", 1);
            foreach (var k in Data.Types) Add(k, 99);
            return want;
        }

        // Торги: коллекционеры с потолками цены по интересам, английский аукцион, затем «Раз, два, продано».
        public static Auction CreateAuction(Artifact art)
        {
            var bidders = Data.Collectors.Select(c =>
            {
                bool like = c.Likes.Contains(art.culture);
                float m = (like ? 1.45f : 0.92f) * (art.rarity == "legendary" ? 1.2f : 1f) * (0.72f + Random.value * 0.56f);
                return (id: c.Id, max: Mathf.RoundToInt(art.value * m));
            }).ToList();
            int p = Mathf.Max(5, Mathf.RoundToInt(art.value * 0.5f));
            int top = bidders.Max(b => b.max);
            if (top < p) p = Mathf.Max(5, Mathf.RoundToInt(top * 0.9f));
            var a = new Auction { art = art };
            float t = 1600; string leader = null;
            for (int guard = 0; guard < 60; guard++)
            {
                int inc = Mathf.Max(2, Mathf.RoundToInt(p * 0.08f));
                int need = leader != null ? p + inc : p;
                var cand = bidders.Where(b => b.id != leader && b.max >= need).ToList();
                if (cand.Count == 0) break;
                float sum = cand.Sum(x => 1f + (x.max - need) / (float)inc), roll = Random.value * sum;
                var pick = cand[cand.Count - 1];
                foreach (var x in cand) { roll -= 1f + (x.max - need) / (float)inc; if (roll < 0) { pick = x; break; } }
                int price = need;
                if (leader != null && Random.value < 0.18f && pick.max >= need + inc * 2) price = need + inc * 2;
                p = price; leader = pick.id;
                a.events.Add(new AuctionEvent { t = t, who = pick.id, price = price });
                t += 1000 + Random.value * 1400;
            }
            float last = a.events[a.events.Count - 1].t;
            a.final = p; a.winner = leader; a.start = a.events[0].price;
            a.once = last + 1500; a.twice = last + 2600; a.end = last + 3700;
            return a;
        }

        public static string HeroTip(Meta meta)
        {
            var active = meta.tombs.Where(t => !t.looted).ToList();
            int minFuel = active.Count > 0 ? active.Min(t => t.fuel) : 1;
            if (meta.hasAuction && !meta.auction.done) return "На аукционе идут торги. Загляни, пока молоток не опустился!";
            if (meta.fuel < minFuel && meta.coins < Data.FuelPrice * (minFuel - meta.fuel) && meta.backpack.Count > 0) return "Бак почти пуст, а денег на топливо нет. Пора продать что-нибудь на аукционе.";
            if (meta.fuel < minFuel && meta.coins >= Data.FuelPrice) return "Нужно заправить вертолёт. Канистры продают прямо на площадке.";
            if (meta.CollectionTotal < 8) return "Снаряжения маловато. Загляни в лавку за кирками и факелами.";
            if (meta.backpack.Count >= 3) return "Рюкзак тяжелеет. Коллекционеры на аукционе заплатят хорошие деньги.";
            return "Вертолёт готов. Выбирай гробницу на карте экспедиций!";
        }

        public static void Save(SaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static SaveData Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d?.meta?.tombs == null || d.meta.tombs.Count == 0) return null;
                if (d.hasRun) d.run?.ClearEvents();
                return d;
            }
            catch { return null; }
        }

        public static void Wipe() { PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save(); }
    }
}
