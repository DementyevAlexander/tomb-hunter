using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Содержимое модальных окон: алтарь, выход, эвакуация, итоги; рюкзак, коллекция, особняк, справка, журнал.
    public partial class Game
    {
        void BuildRunModal(string kind)
        {
            switch (kind)
            {
                case "altar":
                {
                    ModalTitle("Алтарь");
                    ModalText("Возьмите одну карту.");
                    var row = Kit.HRow(modalBox, 14, TextAnchor.MiddleCenter); row.Size(-1, CardView.H + 8);
                    foreach (var type in run.altarOptions)
                    {
                        var tp = type;
                        RectTransform cv = null;
                        cv = CardView.Build(row, type, true, () =>
                        {
                            flyFrom = Fx.WorldCenter(cv);
                            run.ClearEvents();
                            run.TakeAltar(tp);
                            AfterAction();
                        });
                        cv.Size(CardView.W, CardView.H);
                    }
                    break;
                }
                case "exit":
                {
                    ModalTitle("Выход найден");
                    ModalText("Можно подняться наверх сейчас или пошарить в остальных залах. Уйти можно в любой момент кнопкой «Покинуть гробницу».");
                    var row = ModalRow();
                    ModalBtn(row, "Подняться наверх", Kit.BtnStyle.Gold, () => { run.modal = ""; FinishRun("escaped"); });
                    ModalBtn(row, "Исследовать дальше", Kit.BtnStyle.Parch, () => { run.modal = ""; AfterAction(); });
                    break;
                }
                case "evac":
                case "stuck":
                {
                    ModalTitle(kind == "stuck" ? "Тупик" : "Эвакуация");
                    int pen = run.coins / 2;
                    ModalText((kind == "stuck" ? "Сумка пуста, и ни одну плиту больше не открыть. " : "") + $"Вертолёт заберёт вас, но пилот возьмёт половину найденных монет (−{pen}). Артефакты остаются у вас, а гробница не будет считаться разграбленной.");
                    var row = ModalRow();
                    ModalBtn(row, "Вызвать вертолёт", Kit.BtnStyle.Brick, () => { run.modal = ""; FinishRun("evac"); });
                    if (kind == "evac") ModalBtn(row, "Остаться", Kit.BtnStyle.Parch, () => { run.modal = ""; AfterAction(); });
                    break;
                }
                case "summary":
                {
                    var s = run.summary;
                    ModalTitle(s.looted ? "Гробница разграблена" : "Эвакуация");
                    var col = Kit.VCol(modalBox, 2);
                    ScoreLine(col, "Открыто плит", $"{run.opened} из {run.size * run.size}");
                    ScoreLine(col, "Монеты", $"+{s.coins}" + (s.penalty > 0 ? $" (пилот взял {s.penalty})" : "") + (s.bonus > 0 ? $" (бонус {s.bonus})" : ""));
                    ScoreLine(col, "Использовано карт", s.used.ToString());
                    ScoreLine(col, "Потеряно и сброшено", s.lost.ToString());
                    ScoreLine(col, "Найдено карт", s.found.ToString());
                    ScoreLine(col, "В коллекцию вернулось", s.back.ToString(), true);
                    if (s.arts.Count > 0) { var list = Kit.VCol(modalBox, 6); foreach (var a in s.arts) ArtRow(list, a); }
                    else ModalText("Артефактов в этот раз нет.");
                    if (!string.IsNullOrEmpty(s.spawned)) ModalText($"Новые слухи: на карте появилась «{s.spawned}».");
                    var row = ModalRow();
                    ModalBtn(row, "Лететь домой", Kit.BtnStyle.Gold, FlyHome);
                    ModalBtn(row, "Осмотреть гробницу", Kit.BtnStyle.Parch, () => { run.modal = ""; RenderDungeon(); });
                    break;
                }
            }
        }

        void FlyHome()
        {
            var tomb = meta.TombById(run.tombId);
            run = null; boardBuiltFor = null;
            RenderModal();
            StartFlight(tomb.col, tomb.row, false, Data.HomeCol, Data.HomeRow, true, new[] { "Возвращение домой", "Курс: Эшфорд" }, () => Go("hub"));
        }

        void BuildMetaModal(string kind)
        {
            switch (kind)
            {
                case "backpack":
                {
                    ModalTitle("Рюкзак");
                    if (meta.backpack.Count == 0) ModalText("Рюкзак пуст. Артефакты находятся в гробницах.");
                    else
                    {
                        var list = Kit.VCol(modalBox, 6);
                        foreach (var a in meta.backpack) ArtRow(list, a);
                        ModalText("Продать артефакты можно в аукционном доме.");
                    }
                    break;
                }
                case "collection":
                {
                    ModalTitle("Коллекция карт");
                    var grid = Kit.Node("Grid", modalBox);
                    var g = grid.gameObject.AddComponent<GridLayoutGroup>(); g.cellSize = new Vector2(240, 40); g.spacing = new Vector2(8, 8);
                    grid.Size(-1, 4 * 48);
                    foreach (var t in Data.Types)
                    {
                        var item = Kit.Node("Item", grid);
                        Kit.Img(item, Pixel.Box(Kit.ParchLo, Kit.Ink, 2, 5), Color.white, "Bg").rectTransform.Fill();
                        Kit.Icon(item, t, 30).rectTransform.Place(6, 5, 30, 30);
                        Kit.Txt(item, Data.Card(t).Name, 14, Kit.Ink, TextAnchor.MiddleLeft).rectTransform.Place(44, 0, 140, 40);
                        Kit.Txt(item, meta.Count(t).ToString(), 16, Kit.Ink, TextAnchor.MiddleRight, true).rectTransform.Place(180, 0, 50, 40);
                    }
                    ModalText("Новые карты продаются в лавке снаряжения. Перед вылетом часть коллекции кладётся в сумку экспедиции.");
                    break;
                }
                case "manor":
                {
                    ModalTitle("Особняк Рейвенкрофтов");
                    ModalText("Трофейный зал и карта на стене. Здесь Ада готовится к экспедициям.");
                    var col = Kit.VCol(modalBox, 2);
                    ScoreLine(col, "Экспедиций", meta.expeditions.ToString());
                    ScoreLine(col, "Гробниц разграблено", $"{meta.tombs.Count(t => t.looted)} из {meta.tombs.Count}");
                    ScoreLine(col, "Артефактов в рюкзаке", meta.backpack.Count.ToString());
                    ScoreLine(col, "Карт в коллекции", meta.CollectionTotal.ToString());
                    var row = ModalRow();
                    ModalBtn(row, "Рюкзак", Kit.BtnStyle.Gold, () => { metaModal = "backpack"; RenderModal(); });
                    ModalBtn(row, "Коллекция", Kit.BtnStyle.Parch, () => { metaModal = "collection"; RenderModal(); });
                    break;
                }
                case "help":
                {
                    ModalTitle("Справка");
                    var col = Kit.VCol(modalBox, 4);
                    foreach (var o in Data.Obstacles)
                    {
                        var r = Kit.HRow(col, 8); r.Size(-1, 28);
                        Kit.Icon(r, o.Id, 26).Size(26, 26);
                        Kit.Txt(r, $"<b>{o.Name}</b> → {Data.ToolsFor(o.Id)}", 14, Kit.Ink, TextAnchor.MiddleLeft).Size(440, 26);
                    }
                    var rules = string.Join("\n", Data.Rules.Select((r, i) => $"{i + 1}. {r}"));
                    ModalText(rules, Kit.Ink, 13);
                    break;
                }
                case "log":
                {
                    ModalTitle("Журнал экспедиции");
                    var area = Kit.Node("Area", modalBox); area.Size(-1, 380);
                    var list = Kit.Scroll(area, 5);
                    if (run != null) FillLog(list, run.log, 14);
                    break;
                }
            }
            var close = ModalRow();
            ModalBtn(close, "Закрыть", Kit.BtnStyle.Parch, () => { metaModal = ""; RenderModal(); });
        }
    }
}
