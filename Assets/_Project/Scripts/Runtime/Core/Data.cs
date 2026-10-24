using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TombHunter
{
    // Справочники игры: препятствия, карты, находки, культуры, коллекционеры, стартовые гробницы и карта мира.
    public static class Data
    {
        public const int DrawEvery = 2, HandMax = 7, StartHand = 4, BagCap = 14, FuelPrice = 15, FullBonus = 20;

        public class ObstDef { public string Id, Name, Gone; }
        public class CardDef { public string Id, Name, Text; public string[] Clears; public int Price; public Color Art; }
        public class ContentDef { public string Id, Name, Desc; }
        public class Theme { public Color Brick, Fog, Slab, Floor; }
        public class CultDef { public string Id, Name; public Theme Theme; public (string name, string sprite, bool legendary)[] Items; }
        public class RarityDef { public string Id, Name; public int Base; public Color Color; }
        public class Collector { public string Id, Name, Sprite; public Color Color; public string[] Likes; }

        public static readonly ObstDef[] Obstacles =
        {
            new ObstDef { Id = "dark", Name = "Тьма", Gone = "тьма рассеяна" },
            new ObstDef { Id = "rubble", Name = "Завал", Gone = "завал разобран" },
            new ObstDef { Id = "lock", Name = "Замок", Gone = "замок открыт" },
            new ObstDef { Id = "web", Name = "Паутина", Gone = "паутина сожжена" },
            new ObstDef { Id = "water", Name = "Затопление", Gone = "вода замёрзла" },
            new ObstDef { Id = "seal", Name = "Печать", Gone = "печать снята" },
        };
        public static ObstDef Obst(string id) => Obstacles.First(o => o.Id == id);

        public static readonly CardDef[] Cards =
        {
            new CardDef { Id = "pickaxe", Name = "Кирка", Text = "Разбирает завал", Clears = new[] { "rubble" }, Price = 5, Art = Pixel.Hex("#ead2b2") },
            new CardDef { Id = "key", Name = "Ключ", Text = "Открывает замок", Clears = new[] { "lock" }, Price = 6, Art = Pixel.Hex("#ffe6a0") },
            new CardDef { Id = "torch", Name = "Факел", Text = "Рассеивает тьму или сжигает паутину", Clears = new[] { "dark", "web" }, Price = 4, Art = Pixel.Hex("#ffd09c") },
            new CardDef { Id = "ice", Name = "Лёд", Text = "Замораживает воду, по льду можно пройти", Clears = new[] { "water" }, Price = 6, Art = Pixel.Hex("#c4eefa") },
            new CardDef { Id = "scroll", Name = "Свиток", Text = "Снимает печать", Clears = new[] { "seal" }, Price = 10, Art = Pixel.Hex("#e5d3ff") },
            new CardDef { Id = "bomb", Name = "Бомба", Text = "Сносит завал, замок и паутину. Находка гибнет", Clears = new[] { "rubble", "lock", "web" }, Price = 12, Art = Pixel.Hex("#ffc6bc") },
            new CardDef { Id = "lantern", Name = "Фонарь", Text = "Снимает тьму и освещает плиты в радиусе 2", Clears = new[] { "dark" }, Price = 8, Art = Pixel.Hex("#fff0b0") },
            new CardDef { Id = "map", Name = "Карта", Text = "Отмечает выход и даёт 1 карту. Без цели", Clears = new string[0], Price = 9, Art = Pixel.Hex("#efdfbf") },
        };
        public static readonly string[] Types = Cards.Select(c => c.Id).ToArray();
        public static CardDef Card(string id) => Cards.First(c => c.Id == id);
        public static int TypeIndex(string id) => System.Array.IndexOf(Types, id);

        public static readonly Dictionary<string, string> MainTool = new Dictionary<string, string>
        {
            { "rubble", "pickaxe" }, { "lock", "key" }, { "web", "torch" }, { "dark", "torch" }, { "water", "ice" }, { "seal", "scroll" },
        };
        public static readonly HashSet<string> Physical = new HashSet<string> { "rubble", "lock", "web" };

        public static string ToolsFor(string obst) => string.Join(" или ", Cards.Where(c => c.Clears.Contains(obst)).Select(c => c.Name));

        public static readonly ContentDef[] Contents =
        {
            new ContentDef { Id = "entrance", Name = "Вход", Desc = "Здесь сел вертолёт" },
            new ContentDef { Id = "empty", Name = "Пустой коридор", Desc = "Ничего интересного" },
            new ContentDef { Id = "chest", Name = "Сундук", Desc = "Дал 1 карту" },
            new ContentDef { Id = "cache", Name = "Склад снаряжения", Desc = "Дал 2 карты" },
            new ContentDef { Id = "altar", Name = "Алтарь", Desc = "Дал карту на выбор из трёх" },
            new ContentDef { Id = "gold", Name = "Монеты", Desc = "Монеты" },
            new ContentDef { Id = "artifact", Name = "Тайник", Desc = "Здесь лежал артефакт" },
            new ContentDef { Id = "trap", Name = "Ловушка", Desc = "Выбила случайную карту из руки" },
            new ContentDef { Id = "collapse", Name = "Обвал", Desc = "Засыпал соседние закрытые плиты" },
            new ContentDef { Id = "exit", Name = "Выход", Desc = "Лестница наверх" },
        };
        public static ContentDef Content(string id) => Contents.First(c => c.Id == id);

        static Theme T(string brick, string fog, string slab, string floor) => new Theme { Brick = Pixel.Hex(brick), Fog = Pixel.Hex(fog), Slab = Pixel.Hex(slab), Floor = Pixel.Hex(floor) };

        public static readonly CultDef[] Cultures =
        {
            new CultDef { Id = "egypt", Name = "Египет", Theme = T("#c8693a", "#5c4a70", "#a9b8cc", "#e2b06a"),
                Items = new[] { ("Маска шакала", "mask", false), ("Скарабей из лазурита", "amulet", false), ("Анкх фараона", "amulet", false), ("Канопа", "vase", false), ("Золотая маска фараона", "mask", true) } },
            new CultDef { Id = "maya", Name = "Майя", Theme = T("#5f8f4a", "#3f4f4a", "#a2b89c", "#b9c97c"),
                Items = new[] { ("Нефритовый идол", "idol", false), ("Календарный диск", "coin", false), ("Обсидиановый нож", "idol", false), ("Хрустальный череп", "skull", true) } },
            new CultDef { Id = "tibet", Name = "Тибет", Theme = T("#8a4f9a", "#4f5a78", "#b8c8e0", "#e8eef4"),
                Items = new[] { ("Поющая чаша", "vase", false), ("Бирюзовая мандала", "amulet", false), ("Молитвенный барабан", "idol", false), ("Корона ламы", "crown", true) } },
            new CultDef { Id = "crete", Name = "Крит", Theme = T("#3f7fb0", "#4a4f74", "#a9c4d8", "#e8e2d0"),
                Items = new[] { ("Бычий ритон", "vase", false), ("Двойной топор", "idol", false), ("Фреска с дельфинами", "scroll", false), ("Маска Миноса", "mask", true) } },
            new CultDef { Id = "china", Name = "Китай", Theme = T("#a8322a", "#5a3f55", "#b0b8c4", "#e6b88a"),
                Items = new[] { ("Нефритовый дракон", "idol", false), ("Бронзовое зеркало", "coin", false), ("Терракотовый воин", "idol", false), ("Печать императора", "crown", true) } },
            new CultDef { Id = "rome", Name = "Рим", Theme = T("#9a8f84", "#544a66", "#b3bfcc", "#d8d0c0"),
                Items = new[] { ("Монета Цезаря", "coin", false), ("Лавровый венок", "crown", false), ("Амфора", "vase", false), ("Орёл легиона", "idol", true) } },
        };
        public static CultDef Cult(string id) => Cultures.First(c => c.Id == id);

        public static readonly RarityDef[] Rarities =
        {
            new RarityDef { Id = "common", Name = "обычный", Base = 30, Color = Pixel.Hex("#e6cfa2") },
            new RarityDef { Id = "rare", Name = "редкий", Base = 70, Color = Pixel.Hex("#a9d9f5") },
            new RarityDef { Id = "legendary", Name = "легендарный", Base = 180, Color = Pixel.Hex("#ffd76a") },
        };
        public static RarityDef Rarity(string id) => Rarities.First(r => r.Id == id);

        public static readonly Collector[] Collectors =
        {
            new Collector { Id = "baron", Name = "Барон фон Штайн", Sprite = "baron", Color = Pixel.Hex("#8e3f22"), Likes = new[] { "egypt", "rome" } },
            new Collector { Id = "iris", Name = "Мадам Ирис", Sprite = "iris", Color = Pixel.Hex("#6a3fb8"), Likes = new[] { "maya", "china" } },
            new Collector { Id = "prof", Name = "Профессор Олдридж", Sprite = "prof", Color = Pixel.Hex("#2f8a3e"), Likes = new[] { "crete", "tibet" } },
            new Collector { Id = "ho", Name = "Мистер Хо", Sprite = "ho", Color = Pixel.Hex("#b9822a"), Likes = new[] { "china", "tibet" } },
            new Collector { Id = "valenti", Name = "Синьора Валенти", Sprite = "valenti", Color = Pixel.Hex("#3f7fb0"), Likes = new[] { "rome", "crete" } },
        };
        public static Collector CollectorById(string id) => Collectors.First(c => c.Id == id);

        public const int HomeCol = 23, HomeRow = 5;
        public const string HomeName = "Эшфорд";

        public static List<Tomb> StartTombs() => new List<Tomb>
        {
            new Tomb("t1", "Катакомбы Рима", "rome", 25, 6, 6, 1, 1, 2, false, 1.6f, 1101, ("rubble", 28), ("web", 26), ("lock", 16), ("water", 8), ("seal", 4)),
            new Tomb("t2", "Затопленный дворец Кносса", "crete", 27, 7, 7, 1, 1, 2, false, 0.8f, 2202, ("water", 34), ("rubble", 18), ("lock", 16), ("web", 10), ("seal", 5)),
            new Tomb("t3", "Долина царей", "egypt", 28, 8, 7, 2, 2, 3, false, 1f, 3303, ("rubble", 34), ("lock", 18), ("seal", 12), ("web", 12), ("water", 4)),
            new Tomb("t4", "Храм Ягуара", "maya", 11, 9, 8, 3, 2, 3, false, 1.1f, 4404, ("web", 30), ("water", 26), ("rubble", 18), ("lock", 12), ("seal", 6)),
            new Tomb("t5", "Монастырь в облаках", "tibet", 35, 8, 8, 2, 2, 3, false, 2f, 5505, ("lock", 30), ("rubble", 16), ("water", 12), ("web", 10), ("seal", 8)),
            new Tomb("t6", "Гробница императора", "china", 38, 7, 9, 3, 3, 4, true, 1.5f, 6606, ("lock", 28), ("rubble", 22), ("seal", 14), ("web", 12), ("water", 8)),
        };

        public static readonly string[] NameA = { "Святилище", "Склеп", "Храм", "Чертог", "Лабиринт", "Усыпальница" };
        public static readonly string[] NameB = { "Змея", "Луны", "Солнца", "Шакала", "Дракона", "Ворона", "Короля", "Жрицы", "Бури" };

        public static readonly string[] World = Pad(new[]
        {
            "................................................",
            "................LLLL..........LLLLLLLLLL........",
            "....LLLLLLLL....LLLL......LLLLLLLLLLLLLLLLLLL...",
            "..LLLLLLLLLLLLL..LL.....LLLLLLLLLLLLLLLLLLLLLL..",
            "...LLLLLLLLLLLLL.......LLLLLLLLLLLLLLLLLLLLLL...",
            "....LLLLLLLLLLL.......LLLLLLLLLLLLLLLLLLLLLL....",
            ".....LLLLLLLLL........L.LLLLLLLLLLLLLLLLLLLL....",
            "......LLLLLLL........LLLLLL.LLLLLLLLLLLLLLLL....",
            ".......LLLLL.........LLLLLLLL.LLLLLLLLLLLL.L....",
            "........LLLL.........LLLLLLLLL..LLL.LLLLL.......",
            ".........LLL..........LLLLLLLL...LL...LLL.L.....",
            "............LLLL.......LLLLLL.........LL.L......",
            "............LLLLLL.......LLLL...........LL.L....",
            ".............LLLLLL......LLLL.L.......LLLLL.....",
            "..............LLLL.......LLL.L.......LLLLLLL....",
            "..............LLL........LL...........LLLLLL....",
            "..............LL.......................LLLL..L..",
            "..............LL..............................L.",
            "...............L................................",
            "...............L................................",
            "................................................",
            "................................................",
            "....LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL....",
            "LLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLLL",
        });
        public const int WorldCols = 48, WorldRows = 24;
        static string[] Pad(string[] rows) => rows.Select(r => (r + new string('.', 48)).Substring(0, 48)).ToArray();
        public static bool IsLand(int c, int r) => r >= 0 && r < WorldRows && c >= 0 && c < WorldCols && World[r][c] == 'L';

        public static readonly string[] Rules =
        {
            "Плита входа открыта. Перевернуть можно только закрытую плиту, соседнюю с открытой по стороне.",
            "На закрытой стороне видны препятствия. Карта снимает верхнее препятствие, после этого плиту можно перевернуть.",
            "Тьма прячет, что лежит под ней.",
            "Каждые два переворота вы тянете карту из сумки экспедиции. Сундуки, склады и алтари дают новые карты.",
            "Артефакты отправляются в рюкзак. Продать их можно на аукционе в городе.",
            "Выход запечатан. Покиньте гробницу через выход, и она будет считаться разграбленной.",
            "Эвакуация вытаскивает вас в любой момент, но пилот забирает половину найденных монет, а гробница остаётся неразграбленной.",
            "Неиспользованные карты возвращаются в коллекцию.",
        };
    }
}
