using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TombHunter.Tests
{
    // Живая игра: проходит все экраны, играет гробницу ботом через клики и снимает экраны в Screenshots/.
    public class ScreensTests
    {
        Game game;
        Camera cam;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Game.NoSave = true;
            var camGo = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            cam = camGo.GetComponent<Camera>();
            game = new GameObject("Game").AddComponent<Game>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(game.gameObject);
            Object.Destroy(cam.gameObject);
            foreach (var es in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None)) Object.Destroy(es.gameObject);
            Game.NoSave = false;
            yield return null;
        }

        IEnumerator Shot(string name, int w = 1280, int h = 720)
        {
            // WaitForEndOfFrame в пакетном режиме не возвращается, поэтому ждём кадр и рендерим камеру вручную.
            yield return null;
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = prev; RenderTexture.active = null;
            Directory.CreateDirectory("Screenshots");
            File.WriteAllBytes(Path.Combine("Screenshots", name + ".png"), tex.EncodeToPNG());
            Object.Destroy(rt); Object.Destroy(tex);
        }

        static IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

        [UnityTest]
        public IEnumerator AllScreens()
        {
            yield return Shot("01_hub");
            game.DebugOpenMeta("manor"); yield return Wait(.4f); yield return Shot("02_manor");
            game.DebugOpenMeta("");
            game.DebugGo("world", "t3"); yield return Shot("03_world");
            game.DebugGo("loadout", "t3"); yield return Shot("04_loadout");
            game.DebugGo("shop"); yield return Shot("05_shop");
            game.DebugFlight(.5f); yield return Wait(.1f); yield return Shot("06_flight");
            game.DebugStartRun("t3");
            yield return Wait(1.2f); yield return Shot("07_dungeon_start");

            // Бот играет кликами, как игрок.
            var run = game.run;
            int steps = 0; bool midShot = false;
            while (run != null && run.modal != "summary" && steps < 400)
            {
                steps++;
                while (game.Busy) yield return null;
                if (run.modal == "altar") { game.ClickTakeAltar(0); yield return Wait(.3f); continue; }
                if (run.modal == "exit") { yield return Shot("09_exit_found"); game.ClickModal("leave"); yield return Wait(.3f); continue; }
                if (run.modal == "stuck" || run.modal == "evac") { game.ClickModal("evac"); yield return Wait(.3f); continue; }
                var free = run.tiles.FindIndex(t => run.IsAvail(t) && t.obs.Count == 0);
                if (free >= 0) { game.ClickTile(free); yield return Wait(.5f); }
                else
                {
                    bool acted = false;
                    foreach (var c in run.hand.ToList())
                    {
                        if (c.type == "map") { game.ClickCard(c.id); if (!midShot) { midShot = true; yield return Shot("08_card_selected"); } game.ClickPlayMap(); acted = true; break; }
                        var ti = run.tiles.FindIndex(t => run.CanApply(c.type, t));
                        if (ti >= 0)
                        {
                            game.ClickCard(c.id);
                            if (!midShot) { midShot = true; yield return Shot("08_card_selected"); }
                            game.ClickTile(ti); acted = true; break;
                        }
                    }
                    if (!acted && run.hand.Count > 0 && run.deck.Count > 0) { game.ClickCard(run.hand[0].id); game.ClickSwap(); acted = true; }
                    if (!acted && string.IsNullOrEmpty(run.modal)) { run.modal = "evac"; game.ClickModal("evac"); }
                    yield return Wait(.6f);
                }
                if (steps == 12) yield return Shot("08b_dungeon_mid");
            }
            Assert.IsNotNull(run.summary, "экспедиция завершилась итогами");
            yield return Wait(.4f);
            yield return Shot("10_summary");
            game.ClickModal("home");
            yield return Wait(.3f);
            yield return Shot("11_flight_home");
            yield return Wait(6f);
            Assert.AreEqual("hub", game.screen);

            // Аукцион с артефактом из этой экспедиции или подставным.
            if (game.meta.backpack.Count == 0) game.meta.backpack.Add(new Artifact { id = "a99", name = "Маска шакала", sprite = "mask", culture = "egypt", rarity = "rare", value = 90, from = "Долина царей" });
            game.DebugGo("auction");
            yield return Shot("12_auction_idle");
            var btn = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.GetComponentInChildren<UnityEngine.UI.Text>()?.text == "На торги");
            btn.onClick.Invoke();
            game.meta.auction.speed = 3;
            yield return Wait(3f);
            yield return Shot("13_auction_live");
            float guard = 0;
            while (!game.meta.auction.done && guard < 30) { guard += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(game.meta.auction.done, "торги закончились");
            yield return Wait(.5f);
            yield return Shot("14_auction_sold");
            game.DebugGo("hub"); game.DebugOpenMeta("backpack"); yield return Wait(.4f); yield return Shot("15_backpack");
        }
    }
}
