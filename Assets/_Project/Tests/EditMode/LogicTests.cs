using System.Linq;
using NUnit.Framework;

namespace TombHunter.Tests
{
    // Логика без интерфейса: генерация гробниц, автоигра простым ботом, итоги экспедиции и торги.
    public class LogicTests
    {
        [Test]
        public void GenerationIsDeterministicAndValid()
        {
            var meta = Meta.Fresh();
            foreach (var tomb in meta.tombs)
            {
                var load = MetaLogic.AutoPack(meta, tomb);
                var a = Run.Create(tomb, load);
                var b = Run.Create(tomb, load);
                Assert.AreEqual(tomb.size * tomb.size, a.tiles.Count);
                Assert.AreEqual(1, a.tiles.Count(t => t.open), "открыт только вход");
                Assert.AreEqual(1, a.tiles.Count(t => t.content == "exit"));
                Assert.IsTrue(a.tiles.First(t => t.content == "exit").obs.Contains("seal"), "выход под печатью");
                Assert.AreEqual(tomb.art, a.tiles.Count(t => t.content == "artifact"));
                Assert.AreEqual(string.Join("|", a.tiles.Select(t => t.content + string.Join(",", t.obs))), string.Join("|", b.tiles.Select(t => t.content + string.Join(",", t.obs))));
                Assert.LessOrEqual(a.hand.Count, Data.StartHand);
                Assert.AreEqual(load.Sum(), a.hand.Count + a.deck.Count);
            }
        }

        // Бот как в браузерной проверке: свободная плита, иначе первая подходящая карта, иначе смена карты.
        static string Bot(Run run, Meta meta)
        {
            for (int step = 0; step < 2000; step++)
            {
                if (run.modal == "altar") { run.TakeAltar(run.altarOptions[0]); var r0 = run.After(); if (r0 != null) return r0; continue; }
                if (run.modal == "exit") { run.modal = ""; return "escaped"; }
                if (run.modal == "stuck") return "evac";
                Assert.LessOrEqual(run.hand.Count, Data.HandMax);
                var free = run.tiles.FirstOrDefault(t => run.IsAvail(t) && t.obs.Count == 0);
                if (free != null) { run.Flip(free); var r = run.After(); if (r != null) return r; continue; }
                bool acted = false;
                foreach (var c in run.hand.ToList())
                {
                    if (c.type == "map") { run.PlayMap(c); acted = true; break; }
                    var tgt = run.tiles.FirstOrDefault(t => run.CanApply(c.type, t));
                    if (tgt != null) { run.Apply(c, tgt); acted = true; break; }
                }
                if (!acted && run.hand.Count > 0 && run.deck.Count > 0) { run.Swap(run.hand[0]); acted = true; }
                var res = run.After();
                if (res != null) return res;
                if (!acted && string.IsNullOrEmpty(run.modal)) return "evac";
            }
            Assert.Fail("бот застрял");
            return null;
        }

        [Test]
        public void BotPlaysManyRunsAndCardsComeBack()
        {
            int escaped = 0, total = 0;
            for (int seed = 0; seed < 60; seed++)
            {
                var meta = Meta.Fresh();
                var tomb = meta.tombs[seed % meta.tombs.Count];
                tomb.seed += seed * 31;
                var load = MetaLogic.AutoPack(meta, tomb);
                for (int i = 0; i < load.Length; i++) meta.collection[i] -= load[i];
                int before = meta.CollectionTotal;
                var run = Run.Create(tomb, load);
                var res = Bot(run, meta);
                run.Finish(meta, res);
                total++; if (res != "evac") escaped++;
                Assert.AreEqual(before + run.summary.back, meta.CollectionTotal, "неиспользованные карты вернулись");
                Assert.AreEqual(res != "evac", tomb.looted);
                Assert.AreEqual(run.artifacts.Count, meta.backpack.Count);
            }
            UnityEngine.Debug.Log($"Бот выбрался из {escaped} гробниц из {total}");
            Assert.Greater(escaped, 0);
        }

        [Test]
        public void AuctionBidsGrowAndWinnerPaysLastBid()
        {
            var meta = Meta.Fresh();
            foreach (var rar in new[] { "common", "rare", "legendary" })
                foreach (var cult in Data.Cultures)
                {
                    var art = new Artifact { id = "x", name = "Тест", sprite = "mask", culture = cult.Id, rarity = rar, value = Data.Rarity(rar).Base, from = "Тест" };
                    var a = MetaLogic.CreateAuction(art);
                    Assert.Greater(a.events.Count, 0);
                    for (int i = 1; i < a.events.Count; i++)
                    {
                        Assert.Greater(a.events[i].price, a.events[i - 1].price);
                        Assert.Greater(a.events[i].t, a.events[i - 1].t);
                        Assert.AreNotEqual(a.events[i].who, a.events[i - 1].who);
                    }
                    Assert.AreEqual(a.events.Last().price, a.final);
                    Assert.AreEqual(a.events.Last().who, a.winner);
                    Assert.Greater(a.end, a.events.Last().t);
                }
        }

        [Test]
        public void LootingSpawnsNewTombOnLand()
        {
            var meta = Meta.Fresh();
            int n = meta.tombs.Count;
            var t = MetaLogic.SpawnTomb(meta, 3);
            Assert.IsNotNull(t);
            Assert.AreEqual(n + 1, meta.tombs.Count);
            Assert.IsTrue(Data.IsLand(t.col, t.row));
        }

        [Test]
        public void SaveRoundTrip()
        {
            var meta = Meta.Fresh();
            var run = Run.Create(meta.tombs[0], MetaLogic.AutoPack(meta, meta.tombs[0]));
            var json = UnityEngine.JsonUtility.ToJson(new SaveData { meta = meta, run = run, hasRun = true, screen = "dungeon" });
            var back = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(meta.tombs.Count, back.meta.tombs.Count);
            Assert.AreEqual(run.tiles.Count, back.run.tiles.Count);
            Assert.AreEqual(run.hand.Count, back.run.hand.Count);
            Assert.AreEqual(run.rng.s, back.run.rng.s);
        }
    }
}
