using System;
using System.Collections.Generic;
using UnityEngine;

namespace TombHunter
{
    // Сидированный генератор mulberry32: одна гробница с тем же сидом всегда раскладывается одинаково.
    [Serializable]
    public class Rng
    {
        public int s;
        public Rng(int seed) { s = seed; }
        public float Next()
        {
            unchecked
            {
                s += 0x6D2B79F5;
                uint t = (uint)s;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return ((t ^ (t >> 14)) >> 0) / 4294967296f;
            }
        }
        public int Range(int n) => Mathf.Min(n - 1, (int)(Next() * n));
        public T Pick<T>(IList<T> list) => list[Range(list.Count)];
        public string Weighted(IList<Weight> pairs)
        {
            float sum = 0; foreach (var p in pairs) sum += p.w;
            float x = Next() * sum;
            foreach (var p in pairs) { x -= p.w; if (x < 0) return p.id; }
            return pairs[pairs.Count - 1].id;
        }
        public void Shuffle<T>(IList<T> a)
        {
            for (int i = a.Count - 1; i > 0; i--) { int j = Range(i + 1); (a[i], a[j]) = (a[j], a[i]); }
        }
    }

    [Serializable] public class Weight { public string id; public float w; public Weight(string id, float w) { this.id = id; this.w = w; } }

    [Serializable]
    public class Tomb
    {
        public string id, name, cult;
        public int col, row, size, fuel, tier, art, seed, attempts;
        public bool legendary, looted;
        public float darkMul;
        public List<Weight> obsW = new List<Weight>();

        public Tomb() { }
        public Tomb(string id, string name, string cult, int col, int row, int size, int fuel, int tier, int art, bool legendary, float darkMul, int seed, params (string, float)[] w)
        {
            this.id = id; this.name = name; this.cult = cult; this.col = col; this.row = row; this.size = size; this.fuel = fuel;
            this.tier = tier; this.art = art; this.legendary = legendary; this.darkMul = darkMul; this.seed = seed;
            foreach (var (o, v) in w) obsW.Add(new Weight(o, v));
        }
    }

    [Serializable]
    public class Artifact
    {
        public string id, name, sprite, culture, rarity, from;
        public int value;
        public string Estimate => $"{Mathf.RoundToInt(value * 0.75f)}–{Mathf.RoundToInt(value * 1.3f)}";
    }

    [Serializable] public class LogEntry { public string text, kind; public LogEntry(string t, string k) { text = t; kind = k; } }

    [Serializable] public class AuctionEvent { public float t; public string who; public int price; }

    [Serializable]
    public class Auction
    {
        public Artifact art;
        public List<AuctionEvent> events = new List<AuctionEvent>();
        public int final, start;
        public string winner;
        public float elapsed, once, twice, end, speed = 1;
        public bool done;
        public string Phase => elapsed >= end ? "sold" : elapsed >= twice ? "twice" : elapsed >= once ? "once" : "bid";
    }

    [Serializable]
    public class Meta
    {
        public int coins = 60, fuel = 2, nextId = 1, tombCounter = 6, expeditions;
        public int[] collection = { 5, 4, 5, 3, 3, 2, 1, 1 }; // по порядку Data.Types
        public List<Artifact> backpack = new List<Artifact>();
        public List<Tomb> tombs = new List<Tomb>();
        public List<LogEntry> journal = new List<LogEntry>();
        public Auction auction;
        public bool hasAuction;

        public static Meta Fresh()
        {
            var m = new Meta { tombs = Data.StartTombs() };
            m.journal.Add(new LogEntry("Ада вернулась в Эшфорд. На карте отмечены шесть гробниц.", "info"));
            return m;
        }
        public int Count(string type) => collection[Data.TypeIndex(type)];
        public void Add(string type, int n) => collection[Data.TypeIndex(type)] += n;
        public int CollectionTotal { get { int s = 0; foreach (var c in collection) s += c; return s; } }
        public Tomb TombById(string id) => tombs.Find(t => t.id == id);
        public void Journal(string text, string kind = "info") { journal.Insert(0, new LogEntry(text, kind)); if (journal.Count > 40) journal.RemoveAt(journal.Count - 1); }
    }

    [Serializable] public class Card { public int id; public string type; public Card(int id, string type) { this.id = id; this.type = type; } }

    [Serializable]
    public class Tile
    {
        public int r, c, gold;
        public List<string> obs = new List<string>();
        public string content = "empty";
        public bool open, scouted, used, destroyed, hasArt;
        public Artifact art;
        public string Top => obs.Count > 0 ? obs[0] : null;
    }

    [Serializable]
    public class Summary
    {
        public int coins, penalty, bonus, back, used, lost, found;
        public bool looted;
        public string spawned;
        public List<Artifact> arts = new List<Artifact>();
    }

    [Serializable]
    public class SaveData
    {
        public Meta meta;
        public Run run;
        public bool hasRun;
        public string screen = "hub";
    }
}
