using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TombHunter
{
    // Логика одной экспедиции в гробницу: генерация, карты, перевороты, находки и итоги. UI читает поля и списки событий.
    [Serializable]
    public class Run
    {
        public string tombId;
        public int size, nextId = 1, flipsToDraw = Data.DrawEvery, coins, opened = 1, discards, used, lostCards, found, packed;
        public Rng rng;
        public List<Tile> tiles = new List<Tile>();
        public List<Card> deck = new List<Card>();
        public List<Card> hand = new List<Card>();
        public List<Artifact> artifacts = new List<Artifact>();
        public List<LogEntry> log = new List<LogEntry>();
        public bool exitFound, exitMarked, ended;
        public int selected = -1;
        public string result;
        public string modal = "";          // altar, exit, evac, stuck, summary
        public List<string> altarOptions = new List<string>();
        public Summary summary;

        // События последнего действия для эффектов; не сохраняются.
        [NonSerialized] public List<int> fresh = new List<int>();
        [NonSerialized] public List<(int id, string type, bool trap)> lost = new List<(int, string, bool)>();
        [NonSerialized] public List<int> quake = new List<int>();
        [NonSerialized] public List<int> lit = new List<int>();

        public void ClearEvents() { fresh ??= new List<int>(); lost ??= new List<(int, string, bool)>(); quake ??= new List<int>(); lit ??= new List<int>(); fresh.Clear(); lost.Clear(); quake.Clear(); lit.Clear(); }

        public Tile At(int r, int c) => r >= 0 && c >= 0 && r < size && c < size ? tiles[r * size + c] : null;
        public int Index(Tile t) => t.r * size + t.c;
        public IEnumerable<Tile> Nbrs(Tile t)
        {
            var a = At(t.r - 1, t.c); if (a != null) yield return a;
            a = At(t.r + 1, t.c); if (a != null) yield return a;
            a = At(t.r, t.c - 1); if (a != null) yield return a;
            a = At(t.r, t.c + 1); if (a != null) yield return a;
        }
        public bool IsAvail(Tile t) => !t.open && Nbrs(t).Any(n => n.open);
        public Card Selected => hand.Find(c => c.id == selected);
        public void Log(string text, string kind = "info") { log.Insert(0, new LogEntry(text, kind)); if (log.Count > 60) log.RemoveAt(log.Count - 1); }

        // ---------- генерация ----------
        public static Run Create(Tomb tomb, int[] loadout)
        {
            var run = new Run { tombId = tomb.id, size = tomb.size, rng = new Rng(tomb.seed + tomb.attempts * 7919) };
            run.ClearEvents();
            run.Generate(tomb, loadout);
            return run;
        }

        void Generate(Tomb tomb, int[] loadout)
        {
            int n = size;
            for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) tiles.Add(new Tile { r = r, c = c });
            int side = rng.Range(4), k = rng.Range(n);
            int sr = side == 0 ? n - 1 : side == 1 ? 0 : k, sc = side == 0 || side == 1 ? k : side == 2 ? 0 : n - 1;
            var start = At(sr, sc);
            start.open = true; start.content = "entrance";
            int Dist(Tile t) => Mathf.Abs(t.r - sr) + Mathf.Abs(t.c - sc);
            int maxd = tiles.Max(Dist);
            var far = tiles.Where(t => Dist(t) >= Mathf.CeilToInt(maxd * 0.7f)).ToList();
            var exit = rng.Pick(far);
            exit.content = "exit";
            var cw = new List<Weight> { new Weight("empty", 36), new Weight("chest", 18), new Weight("gold", 14), new Weight("altar", 5), new Weight("cache", 5),
                new Weight("trap", 8 + tomb.tier * 2), new Weight("collapse", 7 + tomb.tier * 2) };
            foreach (var t in tiles)
            {
                if (t == start || t == exit) continue;
                float f = Dist(t) / (float)maxd;
                if (rng.Next() < 0.3f + 0.5f * f) t.obs.Add(rng.Weighted(tomb.obsW));
                if (t.obs.Count > 0 && rng.Next() < 0.3f * f) { var o = rng.Weighted(tomb.obsW); if (!t.obs.Contains(o)) t.obs.Add(o); }
                if (rng.Next() < (0.06f + 0.16f * f) * tomb.darkMul) t.obs.Insert(0, "dark");
                t.content = rng.Weighted(cw);
                if (t.content == "gold") t.gold = 2 + rng.Range(4) + tomb.tier;
            }
            exit.obs = rng.Next() < 0.5f ? new List<string> { "dark", "seal" } : new List<string> { "seal" };
            var around = Nbrs(start).ToList();
            foreach (var t in around) { t.obs.Clear(); if (t.content == "trap" || t.content == "collapse") t.content = "empty"; }
            if (around.Count >= 3) rng.Pick(around).obs = new List<string> { "rubble" };

            var spots = tiles.Where(t => t != start && t != exit && !around.Contains(t) && Dist(t) >= maxd * 0.4f).ToList();
            rng.Shuffle(spots);
            for (int i = 0; i < Mathf.Min(tomb.art, spots.Count); i++)
            {
                var t = spots[i];
                var rar = i == 0 && tomb.legendary ? "legendary" : RollRarity(tomb.tier);
                t.content = "artifact"; t.art = MakeArtifact(tomb, rar); t.hasArt = true;
                if (!t.obs.Any(o => o != "dark")) t.obs.Add(rng.Weighted(tomb.obsW));
                if (rar == "legendary" && !t.obs.Contains("seal")) t.obs.Add("seal");
            }

            var types = new List<string>();
            for (int i = 0; i < Data.Types.Length; i++) for (int j = 0; j < loadout[i]; j++) types.Add(Data.Types[i]);
            packed = types.Count;
            rng.Shuffle(types);
            foreach (var type in types) deck.Add(new Card(nextId++, type));
            for (int i = 0; i < Data.StartHand && deck.Count > 0; i++) { var c = Pop(); hand.Add(c); fresh.Add(c.id); }
            Log($"Вертолёт сел у входа в «{tomb.name}». В сумке {packed} карт.");
        }

        string RollRarity(int tier)
        {
            switch (Mathf.Min(4, tier))
            {
                case 1: return rng.Weighted(new[] { new Weight("common", 75), new Weight("rare", 25) });
                case 2: return rng.Weighted(new[] { new Weight("common", 55), new Weight("rare", 38), new Weight("legendary", 7) });
                case 3: return rng.Weighted(new[] { new Weight("common", 40), new Weight("rare", 45), new Weight("legendary", 15) });
                default: return rng.Weighted(new[] { new Weight("common", 30), new Weight("rare", 48), new Weight("legendary", 22) });
            }
        }

        Artifact MakeArtifact(Tomb tomb, string rarity)
        {
            var pool = Data.Cult(tomb.cult).Items.Where(i => i.legendary == (rarity == "legendary")).ToList();
            var item = rng.Pick(pool);
            int value = Mathf.RoundToInt(Data.Rarity(rarity).Base * (1 + 0.15f * (tomb.tier - 1)) * (0.9f + rng.Next() * 0.2f));
            return new Artifact { name = item.name, sprite = item.sprite, culture = tomb.cult, rarity = rarity, value = value, from = tomb.name };
        }

        Card Pop() { var c = deck[deck.Count - 1]; deck.RemoveAt(deck.Count - 1); return c; }

        // ---------- карты ----------
        public bool CanApply(string type, Tile t)
        {
            if (!IsAvail(t)) return false;
            var top = t.Top;
            switch (type)
            {
                case "bomb": return top != null && Data.Physical.Contains(top);
                case "lantern": return true;
                case "map": return false;
                default: return top != null && Data.Card(type).Clears.Contains(top);
            }
        }

        List<Weight> ToolWeights(ICollection<string> exclude)
        {
            var w = new Dictionary<string, float> { { "pickaxe", 1 }, { "key", 1 }, { "torch", 1 }, { "ice", 1 }, { "scroll", 0.6f }, { "bomb", 1.5f }, { "lantern", 1 }, { "map", exitMarked ? 0 : 0.6f } };
            foreach (var t in tiles) if (!t.open) foreach (var o in t.obs) w[Data.MainTool[o]] += IsAvail(t) || t.scouted ? 1.5f : 0.4f;
            if (exclude != null) foreach (var e in exclude) w[e] = 0;
            return w.Where(p => p.Value > 0).Select(p => new Weight(p.Key, p.Value)).ToList();
        }
        string PickTool() => rng.Weighted(ToolWeights(null));
        List<string> PickDistinct(int n)
        {
            var outList = new List<string>();
            while (outList.Count < n) { var ws = ToolWeights(outList); if (ws.Count == 0) break; outList.Add(rng.Weighted(ws)); }
            return outList;
        }

        void AddToHand(Card card, string src)
        {
            var nm = Data.Card(card.type).Name;
            if (hand.Count >= Data.HandMax) { Log($"{src}: «{nm}» не помещается в руку и потеряна.", "bad"); lostCards++; return; }
            hand.Add(card); fresh.Add(card.id);
            Log($"{src}: +«{nm}».", "find");
        }
        public void Gain(string type, string src) { found++; AddToHand(new Card(nextId++, type), src); }
        void Draw(int n, string src)
        {
            for (int i = 0; i < n; i++)
            {
                if (deck.Count == 0) { Log("Сумка пуста.", "bad"); return; }
                AddToHand(Pop(), src);
            }
        }
        void Take(Card card, bool record, bool trap)
        {
            if (record) lost.Add((card.id, card.type, trap));
            hand.Remove(card);
            if (selected == card.id) selected = -1;
        }

        // ---------- действия ----------
        public void Flip(Tile t)
        {
            t.open = true; opened++;
            Resolve(t);
            if (--flipsToDraw <= 0) { flipsToDraw = Data.DrawEvery; Draw(1, "По пути"); }
        }

        void Resolve(Tile t)
        {
            switch (t.content)
            {
                case "chest": t.used = true; Gain(PickTool(), "Сундук"); break;
                case "cache": t.used = true; Gain(PickTool(), "Склад"); Gain(PickTool(), "Склад"); break;
                case "gold": t.used = true; coins += t.gold; Log($"Монеты: +{t.gold}.", "find"); break;
                case "artifact": t.used = true; artifacts.Add(t.art); Log($"Артефакт: «{t.art.name}» ({Data.Rarity(t.art.rarity).Name}) — в рюкзак.", "good"); break;
                case "altar": t.used = true; modal = "altar"; altarOptions = PickDistinct(3); Log("Алтарь предлагает дар.", "find"); break;
                case "trap":
                    t.used = true;
                    if (hand.Count > 0) { var c = rng.Pick(hand); Take(c, true, true); lostCards++; Log($"Ловушка выбила из рук «{Data.Card(c.type).Name}».", "bad"); }
                    else Log("Ловушка сработала, но руки были пусты.", "bad");
                    break;
                case "collapse":
                {
                    t.used = true;
                    var cand = Nbrs(t).Where(n => !n.open && !n.obs.Contains("rubble")).ToList();
                    rng.Shuffle(cand);
                    cand = cand.Take(2).ToList();
                    foreach (var n in cand) { n.obs.Insert(n.Top == "dark" ? 1 : 0, "rubble"); quake.Add(Index(n)); }
                    Log(cand.Count > 0 ? $"Обвал! Завалено соседних плит: {cand.Count}." : "Обвал, но вокруг уже всё открыто.", "bad");
                    break;
                }
                case "exit": exitFound = true; exitMarked = true; modal = "exit"; Log("Вы нашли выход!", "good"); break;
                default: Log(t.destroyed ? "Обломки: находка погибла при взрыве." : "Пустой коридор.", "dim"); break;
            }
        }

        // Возвращает снятые препятствия (для эффектов).
        public List<string> Apply(Card card, Tile t)
        {
            var removed = new List<string>();
            Take(card, false, false); used++;
            if (card.type == "bomb")
            {
                removed = t.obs.Where(o => Data.Physical.Contains(o)).ToList();
                t.obs = t.obs.Where(o => !Data.Physical.Contains(o)).ToList();
                var msg = "Бомба снесла: " + string.Join(", ", removed.Select(o => Data.Obst(o).Name.ToLower())) + ".";
                if (t.content != "empty" && t.content != "exit")
                {
                    msg += t.content == "artifact" ? $" Артефакт «{t.art.name}» разбит!" : " Находка под плитой уничтожена.";
                    t.content = "empty"; t.destroyed = true; t.hasArt = false; t.art = null;
                }
                Log(msg, t.destroyed ? "bad" : "info");
            }
            else if (card.type == "lantern")
            {
                if (t.Top == "dark") { t.obs.RemoveAt(0); removed.Add("dark"); }
                int n = 0;
                foreach (var u in tiles)
                {
                    if (u.open || Mathf.Abs(u.r - t.r) + Mathf.Abs(u.c - t.c) > 2) continue;
                    if (!u.scouted && !IsAvail(u)) { n++; lit.Add(Index(u)); }
                    u.scouted = true;
                }
                Log($"Фонарь: освещено новых плит — {n}.");
            }
            else
            {
                var o = t.Top; t.obs.RemoveAt(0); removed.Add(o);
                var msg = $"{Data.Card(card.type).Name}: {Data.Obst(o).Gone}.";
                if (o == "dark") msg += t.obs.Count > 0 ? " Под тьмой: " + string.Join(", ", t.obs.Select(x => Data.Obst(x).Name.ToLower())) + "." : " Под ней путь свободен.";
                Log(msg);
            }
            return removed;
        }

        public void PlayMap(Card card)
        {
            Take(card, false, false); used++;
            var exit = tiles.Find(t => t.content == "exit");
            if (!exitMarked) { exitMarked = true; exit.scouted = true; Log("Карта: выход отмечен на плане."); }
            else Log("Карта: выход уже известен.", "dim");
            Draw(1, "Карта");
        }

        public void Swap(Card card)
        {
            Take(card, true, false); discards++;
            Log($"Сброшена «{Data.Card(card.type).Name}».", "dim");
            Draw(1, "Смена");
        }

        public void TakeAltar(string type) { modal = ""; Gain(type, "Алтарь"); }

        public bool HasMove()
        {
            var av = tiles.Where(IsAvail).ToList();
            if (av.Any(t => t.obs.Count == 0)) return true;
            foreach (var c in hand) { if (c.type == "map") return true; if (av.Any(t => CanApply(c.type, t))) return true; }
            return hand.Count > 0 && deck.Count > 0;
        }

        // Проверка после любого действия: полное исследование, передышка, тупик. Возвращает итог, если экспедиция закончилась сама.
        public string After()
        {
            if (tiles.All(t => t.open)) return "full";
            if (string.IsNullOrEmpty(modal))
            {
                for (int g = 0; g < 5 && !HasMove() && hand.Count == 0 && deck.Count > 0; g++) Draw(1, "Передышка");
                if (!HasMove()) { if (exitFound) return "escaped"; modal = "stuck"; }
            }
            return null;
        }

        public void Finish(Meta meta, string res)
        {
            var tomb = meta.TombById(tombId);
            int got = coins, penalty = 0, bonus = 0;
            if (res == "evac") { penalty = got / 2; got -= penalty; }
            if (res == "full") bonus = Data.FullBonus;
            got += bonus;
            int back = 0;
            foreach (var c in hand.Concat(deck)) { meta.Add(c.type, 1); back++; }
            var arts = artifacts.Select(a => new Artifact { id = "a" + meta.nextId++, name = a.name, sprite = a.sprite, culture = a.culture, rarity = a.rarity, value = a.value, from = tomb.name }).ToList();
            meta.backpack.AddRange(arts);
            meta.coins += got; meta.expeditions++;
            bool looted = res != "evac";
            Tomb spawned = null;
            if (looted) { tomb.looted = true; spawned = MetaLogic.SpawnTomb(meta, tomb.tier + 1); }
            else tomb.attempts++;
            meta.Journal(looted ? $"«{tomb.name}» разграблена: +{got} монет, артефактов: {arts.Count}." : $"Эвакуация из «{tomb.name}»: +{got} монет (пилот взял {penalty}), артефактов: {arts.Count}.", looted ? "good" : "bad");
            if (spawned != null) meta.Journal($"Новые слухи: «{spawned.name}» ({Data.Cult(spawned.cult).Name}).", "find");
            ended = true; result = res; selected = -1;
            summary = new Summary { coins = got, penalty = penalty, bonus = bonus, arts = arts, back = back, used = used, lost = lostCards + discards, found = found, looted = looted, spawned = spawned?.name };
            modal = "summary";
            Log(looted ? "Гробница разграблена." : "Вертолёт забрал вас из гробницы.", looted ? "good" : "bad");
        }

        public string Describe(Tile t, bool reveal)
        {
            if (t.open)
            {
                if (t.destroyed) return "Обломки. Находку уничтожил взрыв.";
                if (t.content == "gold") return $"Монеты: +{t.gold}.";
                if (t.content == "artifact") return $"Тайник. Здесь лежал артефакт «{t.art.name}».";
                var c = Data.Content(t.content); return $"{c.Name}. {c.Desc}.";
            }
            bool av = IsAvail(t);
            if (!av && !t.scouted && !reveal) return "Неисследованная плита. Откройте соседнюю, чтобы до неё добраться.";
            var head = av ? "Закрытая плита" : "Плита пока недоступна";
            string s;
            if (t.obs.Count == 0) s = $"{head}: путь свободен." + (av ? " Нажмите, чтобы перевернуть." : "");
            else if (t.Top == "dark" && !reveal) s = $"{head}: тьма, под ней ничего не видно. Нужен Факел или Фонарь.";
            else s = $"{head}: " + string.Join(", под ним ", t.obs.Select(o => $"{Data.Obst(o).Name} ({Data.ToolsFor(o)})")) + ".";
            if (reveal) s += " Внутри: " + (t.destroyed ? "обломки" : t.content == "artifact" ? $"артефакт «{t.art.name}»" : Data.Content(t.content).Name.ToLower()) + ".";
            else if (t.content == "exit" && exitMarked) s += " Здесь выход.";
            return s;
        }
    }
}
