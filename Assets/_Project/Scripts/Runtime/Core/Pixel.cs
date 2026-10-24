using System.Collections.Generic;
using UnityEngine;

namespace TombHunter
{
    // Пиксельные спрайты из строк, текстуры-шумы и кирпич; всё генерируется при запуске.
    public static class Pixel
    {
        public static readonly Color Ink = Hex("#2a1b2e");

        static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>
        {
            {'a', Hex("#2a1b2e")}, {'w', Hex("#ffffff")}, {'g', Hex("#f6c453")}, {'G', Hex("#b9822a")}, {'o', Hex("#ff9a2e")},
            {'r', Hex("#e8483c")}, {'R', Hex("#9e2a2a")}, {'b', Hex("#b07f50")}, {'B', Hex("#6e4a2e")}, {'s', Hex("#dbe4ef")},
            {'S', Hex("#93a3ba")}, {'t', Hex("#5d6b82")}, {'c', Hex("#8fe0f6")}, {'C', Hex("#3f8fe0")}, {'p', Hex("#b47cff")},
            {'P', Hex("#6a3fb8")}, {'y', Hex("#fff2a8")}, {'e', Hex("#eef2f8")}, {'n', Hex("#3b2a5a")}, {'m', Hex("#7d5bc4")},
            {'q', Hex("#f6e7c8")}, {'Q', Hex("#d9b98a")}, {'l', Hex("#7ccf5a")}, {'L', Hex("#3f7f3a")}, {'k', Hex("#f0c09a")},
            {'K', Hex("#c98e6a")}, {'h', Hex("#6a4126")}, {'H', Hex("#3b2414")}, {'T', Hex("#3aa59a")}, {'D', Hex("#25766f")},
        };

        static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
        {
            {"rubble", new[]{"............","............",".....bb.....","....bQQb....","...bbQbbB...","..bQbBbbbB..",".bQQbBBbQbB.",".bbbBbbbbbB.","bQbBBbQQbbBB","bbbbBBbbbBBB","BBBBBBBBBBBB","............"}},
            {"lock", new[]{"............","....SSSS....","...S....S...","...S....S...","...S....S...","..gggggggg..","..gyyggggG..","..gggaaggG..","..ggggaggG..","..gggaagGG..","..GGGGGGGG..","............"}},
            {"web", new[]{"e....ee....e",".e...ee...e.","..eeeeeeee..","..ee.ee.ee..","..e.eeee.e..","eeeeeaaeeeee","eeeeeaaeeeee","..e.eeee.e..","..ee.ee.ee..","..eeeeeeee..",".e...ee...e.","e....ee....e"}},
            {"dark", new[]{"............","...mmmmmm...","..mPPPPPPm..",".mPPnnnnPPm.",".mPnnnnnnPm.","mPnyynnyynPm","mPnnnnnnnnPm","mPnnnnnnnnPm",".mPnnnnnnPm.",".mPPnnnnPPm.","..mPmPPmPm..","...m.mm.m..."}},
            {"water", new[]{"............","...cc....cc.","..cCCc..cCCc","ccCCCCccCCCC","CCCCCCCCCCCC","CCwCCCCCwCCC","CCCCCCCCCCCC","CCCccCCCCccC","CCcCCcCCcCCc","CCCCCCCCCCCC","CCCCCCCCCCCC","............"}},
            {"seal", new[]{"...PPPPPP...","..P......P..",".P..pppp..P.","P..p....p..P","P.p..yy..p.P","P.p.yppy.p.P","P.p.yppy.p.P","P.p..yy..p.P","P..p....p..P",".P..pppp..P.","..P......P..","...PPPPPP..."}},
            {"pickaxe", new[]{"..SSSS......",".SssssSS....","Ss...BbsSS..","S....bB.sSS.",".....Bb...sS","....bB.....S","...bB.......","..bB........",".bB.........","bB..........","B...........","............"}},
            {"key", new[]{"............","............","............",".gggg.......","gyyggg......","gg..gggggggg","gg..ggGGGGGG","gGGGGG..G.G.",".GGGG...G.G.","............","............","............"}},
            {"torch", new[]{".....oo.....","....oyyo....","...oyyyyo...","...ryyyyr...","...roooor...","....rRRr....","....bBBb....",".....bB.....",".....bB.....",".....bB.....",".....bB.....",".....BB....."}},
            {"ice", new[]{".....w......",".c...c...c..","..c..c..c...","...c.c.c....","....cwc.....","wcccwwwcccw.","....cwc.....","...c.c.c....","..c..c..c...",".c...c...c..",".....w......","............"}},
            {"scroll", new[]{"............","..BbbbbbbB..","..qqqqqqqq..","..qaaaaaaq..","..qqqqqqqq..","..qaaaaaQq..","..qqqqqqqq..","..qaaaPPqq..","..qqqqPpqq..","..BbbbbbbB..","......Pp....","......P....."}},
            {"bomb", new[]{"........y.y.",".........o..","........B...",".......B....","...aaaaa....","..anwnnna...",".anwnnnnna..",".annnnnnna..",".annnnnnna..",".annnnnnna..","..annnnna...","...aaaaa...."}},
            {"lantern", new[]{"............",".....aa.....","....a..a....","...GGGGGG...","...GyyyyG...","...GyooyG...","...GyooyG...","...GyyyyG...","...GGGGGG...","....GGGG....","............","............"}},
            {"map", new[]{"............","............","qqqqQqqqQqqq","qaaqQqqqQqqq","qqaqQqqqQqqq","qqqaQqqqQqqq","qqqqaaqqQqqq","qqqqQqaaQrqr","qqqqQqqqaqrq","qqqqQqqqQrqr","QQQQQQQQQQQQ","............"}},
            {"chest", new[]{"............","............","..BBBBBBBB..",".BbbbbbbbbB.",".BbbbbbbbbB.",".GGGGggGGGG.",".BbbbgGbbbB.",".BbbbbbbbbB.",".BbbbbbbbbB.",".BbbbbbbbbB.",".GGGGGGGGGG.","............"}},
            {"cache", new[]{"............",".BBBBBBBBBB.",".BBbbbbbbBB.",".BbBbbbbBbB.",".BbbBbbBbbB.",".BbbbBBbbbB.",".BbbbBBbbbB.",".BbbBbbBbbB.",".BbBbbbbBbB.",".BBbbbbbbBB.",".BBBBBBBBBB.","............"}},
            {"altar", new[]{".....o......","....oyo.....","....oyo.....",".....o......",".pppppppppp.",".ssssssssss.",".SSSSSSSSSS.","..sS....Ss..","..sS....Ss..","..sS....Ss..",".ssSS..SSss.","............"}},
            {"gold", new[]{"............","............",".....gg.....","....gyyg....","....gggG....","..gg.GG.gg..",".gyyg..gyyg.",".gggG..gggG.","..GG.gg.GG..","....gyyg....","...ggggGG...","....GGGG...."}},
            {"trap", new[]{"............","............","............",".r...r...r..",".s...s...s..","sSs.sSs.sSs.","sSS.sSS.sSS.","SSStSSStSSSt","tttttttttttt","RRRRRRRRRRRR","............","............"}},
            {"collapse", new[]{"...bB...bB..","...BB....B..","......bB....","......BB....","............","..b.....Bb..","............","...QbB......","..QbbbB.QbB.",".QbbBbbQbbbB","QbbbbbBbbbbB","BBBBBBBBBBBB"}},
            {"exit", new[]{"....aaaa....","...ayyyya...","..ayyyyyya..","..ayyyyyya..","..ayyyyyGa..","..ayyyySSa..","..ayyySSsa..","..ayySSssa..","..aySSssSa..","..aSSssSSa..",".aaaaaaaaaa.","............"}},
            {"entrance", new[]{"....tttt....","...tBBBBt...","..tBbbbbBt..","..tbBbbBbt..","..tbBbbBbt..","..tbBbbBbt..","..tbBbbBbt..","..tbBbbBgt..","..tbBbbBbt..","..tbBbbBbt..",".tttttttttt.","............"}},
            {"deck", new[]{"............","....gggggg..","....gPPPPg..","..ggggggPg..","..gPPPPgPg..","..gPggPgPg..","..gPggPgPg..","..gPPPPgPg..","..gPggPggg..","..gPggPg....","..gPPPPg....","..gggggg...."}},
            {"hour", new[]{"..BBBBBBBB..","...cyyyyc...","...cyyyyc...","....cyyc....",".....cc.....",".....yc.....","....c.yc....","...c..y.c...","...c.yyyc...","...cyyyyc...","..BBBBBBBB..","............"}},
            {"fuel", new[]{"............","...aa.......","..rrrrrrr...",".rrrrrrrrr..",".rrwrrrrrr..",".rrrRrrRrr..",".rrrrRRrrr..",".rrrRrrRrr..",".rrRrrrrRr..",".rrrrrrrrr..",".RRRRRRRRR..","............"}},
            {"backpack", new[]{"....BBBB....","...B....B...","..bbbbbbbb..",".bbbbbbbbbb.",".bBBBBBBBBb.",".bbbbggbbbb.",".bbbbbbbbbb.",".bQQQQQQQQb.",".bQqqqqqqQb.",".bQQQQQQQQb.",".bbbbbbbbbb.","..BBBBBBBB.."}},
            {"pin", new[]{"...rrrrr....","..rrwrrrr...",".rrwrrrrrr..",".rrrrrrrrr..",".rrrryyrrr..",".rrrryyrrr..","..rrrrrrr...","...rrrrr....","....rrr.....",".....r......",".....r......","............"}},
            {"pinhome", new[]{".....gg.....","....gyyg....","...gyyyyg...","..gyyyyyyg..",".gggggggggg.","..qqqqqqqq..","..qcqBBqcq..","..qqqBBqqq..","..qqqBBqqq..","..QQQQQQQQ..","............","............"}},
            {"mask", new[]{"............","..gggggggg..",".gyyyyyyyyg.",".gyaayyaayG.",".gyyyyyyyyG.",".gyyygGyyyG.",".gyyygGyyyG.","..gyyyyyyG..","..gyaaaayG..","...gyyyyG...","....GGGG....","............"}},
            {"idol", new[]{"....LLLL....","...LllllL...","...LalalL...","...LllllL...","....LllL....","..LLllllLL..",".LllLllLllL.",".Ll.LllL.lL.","....LllL....","...LllllL...","..LLLLLLLL..","............"}},
            {"amulet", new[]{"............",".....gg.....","....g..g....","....g..g....","...CccccC...","..CcwcccCC..","..CccccccC..","..CCcccCCC..","...CcccC....","....CCC.....","....g.g.....","............"}},
            {"vase", new[]{"...bbbbbb...","....bBBb....",".B..bbbb..B.",".BBbbbbbbBB.","..baabaabb..","..bbbbbbbb..","..baaaaaab..","..bbbbbbbb..","...bbbbbb...","....bbbb....","...BBBBBB...","............"}},
            {"crown", new[]{"............","............","............",".g...g...g..",".gg.ggg.gg..",".ggggggggg..",".gyrgygCyg..",".ggggggggg..",".GGGGGGGGG..","............","............","............"}},
            {"skull", new[]{"............","...cccccc...","..cwcccccc..",".cwcccccccC.",".ccCCccCCcC.",".ccCCccCCcC.",".cccccccccC.","..cccCcccC..","...cCcCcC...","...cccccc...","....CCCC....","............"}},
            {"coin", new[]{"............","...gggggg...","..gyyyyyyg..",".gyyGGGyyyG.",".gyGyyyGyyG.",".gyGyyyyyyG.",".gyGyyyGyyG.",".gyyGGGyyyG.","..gyyyyyyG..","...GGGGGG...","............","............"}},
            {"heli", new[]{"............",".....cc.....","....cccc....","....rrrr....","...rrrrrr...","...rrwrrr...","...rrrrrr...","....rrrr....",".....rr.....",".....rr.....","....rRRr....",".....rr....."}},
            {"helipad", new[]{"................",".....tttttt.....","...ttSSSSSStt...","..tSSSSSSSSSSt..",".tSSyySSSSyySSt.",".tSSyySSSSyySSt.","tSSSyySSSSyySSSt","tSSSyyyyyyyySSSt","tSSSyyyyyyyySSSt","tSSSyySSSSyySSSt",".tSSyySSSSyySSt.",".tSSyySSSSyySSt.","..tSSSSSSSSSSt..","...ttSSSSSStt...",".....tttttt.....","................"}},
            {"manor", new[]{"................","...tt......tt...","..RRRRRRRRRRRR..",".RrrrrrrrrrrrrR.","RrrrrrrrrrrrrrrR",".qqqqqqqqqqqqqq.",".qccqccqqccqccq.",".qccqccqqccqccq.",".qqqqqqqqqqqqqq.",".qccqccBBqccqcc.",".qqqqqqBBqqqqqq.",".qqqqqqBBqqqqqq.","QQQQQQQQQQQQQQQQ","................","................","................"}},
            {"auction", new[]{"................",".......gg.......",".....gggggg.....","...gggyyyyggg...",".GGGGGGGGGGGGGG.",".qqqqqqqqqqqqqq.","..qQ.qQ..qQ.qQ..","..qQ.qQ..qQ.qQ..","..qQ.qQ..qQ.qQ..","..qQ.qQ..qQ.qQ..","..qQ.qQBBqQ.qQ..","..qQ.qQBBqQ.qQ..",".QQQQQQQQQQQQQQ.","qqqqqqqqqqqqqqqq","QQQQQQQQQQQQQQQQ","................"}},
            {"shop", new[]{"................","................",".BBBBBBBBBBBBBB.","rrwwrrwwrrwwrrww","rrwwrrwwrrwwrrww",".rr.ww.rr.ww.rr.",".bbbbbbbbbbbbbb.",".bqqqqbbbbqqqqb.",".bqccqbBBbqccqb.",".bqccqbBBbqccqb.",".bqqqqbBBbqqqqb.",".bbbbbbBBbbbbbb.",".bbbbbbBgbbbbbb.",".BBBBBBBBBBBBBB.","................","................"}},
            {"hero", new[]{"................",".....hhhhhh.....","....hhhhhhhh....","...hhkkkkkkhh...","...hkkkkkkkkh...","...hkaakkaakh...","...hkkkkkkkkhH..","...hkkkKKkkkhH..","....kkkrrkkk.H..",".....kkkkkk..H..","....TTkkkkTT.H..","...TTTTTTTTTTH..","..TTTTTTTTTTTT..","..DDTTTTTTTTDD..","..BBBBBBBBBBBB..","..BbbbbbbbbbbB.."}},
            {"baron", new[]{"....aaaaaaaa....","....aaaaaaaa....","....aRRRRRRa....","..aaaaaaaaaaaa..","....kkkkkkkk....","...kkakkkgggk...","...kkkkkkgagk...","...kkkkkkgggk...","...kkkkKKkkkk...","...kHHHHHHHHk...","....kkkkkkkk....",".....kkkkkk.....","...tttwwwwttt...","..ttttwRRwtttt..","..tttttRRttttt..","..tttttttttttt.."}},
            {"iris", new[]{"....PPPPPPPP....","...PppppppppP...","..PppppppppppP..","..PpkkkkkkkkpP..","..Ppkkkkkkkkpp..","..Ppkaakkaakpp..","..Ppkkkkkkkkpp..","..PgkkkKKkkkgp..","..Pgkkkrrkkkgp..","..Pp.kkkkkk.pp..","..Pp..kkkk..pp..","...PpPPPPPPpP...","...ppPPkkPPpp...","..pppPPggPPppp..","..pppPPPPPPppp..","..pppppppppppp.."}},
            {"prof", new[]{"................",".....kkkkkk.....","....kkkkkkkk....","...skkkkkkkks...","..sskkkkkkkkss..","..skaaakkaaaks..","..skacakkacaks..","..skaaakkaaaks..","...kkkkKKkkkk...","...kssssssssk...","....sssrrsss....",".....ssssss.....","...LLLqqqqLLL...","..LLLLqRRqLLLL..","..LLLLLqqLLLLL..","..LLLLLLLLLLLL.."}},
            {"ho", new[]{"................","....nnnnnnnn....","...nnnnnnnnnn...","...nnkkkkkknn...","...nkkkkkkkkn...","...kaaakkaaak...","...kacakkacak...","...kaaakkaaak...","...kkkkKKkkkk...","....kkkkkkkk....","....kkkRRkkk....",".....kkkkkk.....","...CCCwwwwCCC...","..CCCCwaawCCCC..","..CCCCCaaCCCCC..","..CCCCCCCCCCCC.."}},
            {"valenti", new[]{"......HHHH......",".....HHHHHH.....","....HHHHHHHH....","...HHkkkkkkHH...","...HkkkkkkkkH...","...HkaakkaakH...","...HkkkkkkkkH...","...gkkkKKkkkg...","....kkkrrkkk....",".....kkkkkk.....","......kkkk......","....rrkkkkrr....","...rrrrkkrrrr...","..rrrrrrrrrrrr..","..rRrrrrrrrrRr..","..rrrrrrrrrrrr.."}},
            {"keeper", new[]{"................","................",".....kkkkkk.....","....kkkkkkkk....","...kkkkkkkkkk...","...kkaakkaakk...","...kkkkkkkkkk...","..KkkkkKKkkkkK..","...oookkkkooo...","...oooorrrooo...","....oooooooo....",".....oooooo.....","...bbbqqqqbbb...","..bbbbqqqqbbbb..","..bbbbqqgqbbbb..","..bbbbqqqqbbbb.."}},
        };

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static Color Shade(Color c, float f)
        {
            var t = f < 0 ? Color.black : Color.white;
            var k = Mathf.Abs(f);
            return new Color(Mathf.Lerp(c.r, t.r, k), Mathf.Lerp(c.g, t.g, k), Mathf.Lerp(c.b, t.b, k), c.a);
        }

        public static bool Has(string name) => Rows.ContainsKey(name);

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out var s)) return s;
            if (!Rows.TryGetValue(name, out var rows)) { Debug.LogWarning("Нет спрайта " + name); rows = Rows["chest"]; }
            int h = rows.Length, w = rows[0].Length;
            var tex = NewTex(w + 2, h + 2);
            var px = new Color[(w + 2) * (h + 2)];
            bool Filled(int r, int c) => r >= 0 && r < h && c >= 0 && c < w && c < rows[r].Length && rows[r][c] != '.';
            for (int r = -1; r <= h; r++)
                for (int c = -1; c <= w; c++)
                {
                    Color col = Color.clear;
                    if (Filled(r, c)) col = Pal.TryGetValue(rows[r][c], out var pc) ? pc : Color.magenta;
                    else if (Filled(r - 1, c) || Filled(r + 1, c) || Filled(r, c - 1) || Filled(r, c + 1)) col = Ink;
                    int x = c + 1, y = (h + 1) - (r + 1);
                    px[y * (w + 2) + x] = col;
                }
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w + 2, h + 2), new Vector2(0.5f, 0.5f), 100f);
            s.name = name;
            Cache[name] = s;
            return s;
        }

        public static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            return t;
        }

        // Шумовая плитка 12×12 для фонов плит и пола.
        public static Sprite Noise(Color baseCol, int seed)
        {
            string key = "noise" + ColorUtility.ToHtmlStringRGB(baseCol) + seed;
            if (Cache.TryGetValue(key, out var s)) return s;
            var rng = new Rng(seed);
            var tex = NewTex(12, 12); tex.wrapMode = TextureWrapMode.Repeat;
            var dark = Shade(baseCol, -0.1f); var light = Shade(baseCol, 0.1f);
            for (int i = 0; i < 144; i++)
            {
                float v = rng.Next();
                var c = v < 0.14f ? dark : v < 0.24f ? light : baseCol;
                tex.SetPixel(i % 12, i / 12, c);
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }

        static readonly string[] BrickRows =
        {
            "mmmmmmmmmmmmmmmm","hhhhhhhmhhhhhhhm","BBBBBBBmBBBBBBBm","BBBBBBBmBBBBBBBm","BBBBBBBmBBBBBBBm","BBBBBBBmBBBBBBBm","BBBBBBBmBBBBBBBm","lllllllmlllllllm",
            "mmmmmmmmmmmmmmmm","hhhmhhhhhhhmhhhh","BBBmBBBBBBBmBBBB","BBBmBBBBBBBmBBBB","BBBmBBBBBBBmBBBB","BBBmBBBBBBBmBBBB","BBBmBBBBBBBmBBBB","lllmlllllllmllll",
        };

        public static Sprite Brick(Color brick)
        {
            string key = "brick" + ColorUtility.ToHtmlStringRGB(brick);
            if (Cache.TryGetValue(key, out var s)) return s;
            var tex = NewTex(16, 16); tex.wrapMode = TextureWrapMode.Repeat;
            for (int r = 0; r < 16; r++)
                for (int c = 0; c < 16; c++)
                {
                    char ch = BrickRows[r][c];
                    var col = ch == 'm' ? Shade(brick, -0.45f) : ch == 'h' ? Shade(brick, 0.18f) : ch == 'l' ? Shade(brick, -0.18f) : brick;
                    tex.SetPixel(c, 15 - r, col);
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }

        // Девятисрезовая рамка: заливка, контур чернилами, по желанию тень снизу и внутренняя золотая линия.
        public static Sprite Box(Color fill, Color border, int bw, int radius, Color? inner = null)
        {
            string key = "box" + ColorUtility.ToHtmlStringRGBA(fill) + ColorUtility.ToHtmlStringRGBA(border) + bw + "_" + radius + (inner.HasValue ? ColorUtility.ToHtmlStringRGBA(inner.Value) : "");
            if (Cache.TryGetValue(key, out var s)) return s;
            int n = 32;
            var tex = NewTex(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (n - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (n - 1 - radius)));
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    float inset = edge;
                    bool outside = false;
                    if (dx > 0 && dy > 0)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        outside = d > radius + 0.5f;
                        inset = radius + 0.5f - d;
                    }
                    Color c;
                    if (outside) c = Color.clear;
                    else if (inset < bw) c = border;
                    else if (inner.HasValue && inset < bw + 2) c = inner.Value;
                    else c = fill;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            int b = Mathf.Max(radius, bw + 2) + 2;
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            Cache[key] = s;
            return s;
        }

        // Скос плиты: светлая полоса сверху, тёмная снизу, контур чернилами.
        public static Sprite BevelBox(Color top, Color bottom, bool inset = false)
        {
            string key = "bevel" + ColorUtility.ToHtmlStringRGBA(top) + ColorUtility.ToHtmlStringRGBA(bottom) + inset;
            if (Cache.TryGetValue(key, out var s)) return s;
            int n = 32, radius = 6, bw = 2;
            var tex = NewTex(n, n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (n - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (n - 1 - radius)));
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(n - 1 - x, n - 1 - y));
                    float inner = edge; bool outside = false;
                    if (dx > 0 && dy > 0) { float d = Mathf.Sqrt(dx * dx + dy * dy); outside = d > radius + 0.5f; inner = radius + 0.5f - d; }
                    int fromTop = n - 1 - y;
                    Color c = Color.clear;
                    if (outside) c = Color.clear;
                    else if (inner < bw) c = Ink;
                    else if (fromTop < bw + (inset ? 5 : 4)) c = top;
                    else if (y < bw + 5) c = bottom;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
            Cache[key] = s;
            return s;
        }

        public static Sprite Square()
        {
            if (Cache.TryGetValue("square", out var s)) return s;
            var tex = NewTex(4, 4);
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
            Cache["square"] = s;
            return s;
        }

        public static Sprite Circle()
        {
            if (Cache.TryGetValue("circle", out var s)) return s;
            int n = 64; var tex = NewTex(n, n); tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                tex.SetPixel(x, y, d <= n / 2f - 1 ? Color.white : Color.clear);
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            Cache["circle"] = s;
            return s;
        }

        public static Sprite Ring()
        {
            if (Cache.TryGetValue("ring", out var s)) return s;
            int n = 64; var tex = NewTex(n, n); tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                tex.SetPixel(x, y, d <= n / 2f - 1 && d >= n / 2f - 5 ? Color.white : Color.clear);
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            Cache["ring"] = s;
            return s;
        }

        // Мягкое пятно света для вспышек и лучей.
        public static Sprite Glow()
        {
            if (Cache.TryGetValue("glow", out var s)) return s;
            int n = 64; var tex = NewTex(n, n); tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1 - d); a *= a;
                tex.SetPixel(x, y, new Color(1, 0.95f, 0.75f, a));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            Cache["glow"] = s;
            return s;
        }

        public static Sprite Rays()
        {
            if (Cache.TryGetValue("rays", out var s)) return s;
            int n = 128; var tex = NewTex(n, n); tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                var v = new Vector2(x + 0.5f - n / 2f, y + 0.5f - n / 2f);
                float d = v.magnitude / (n / 2f);
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 180f;
                bool ray = (ang % 30f) < 10f;
                float a = ray ? Mathf.Clamp01(1 - d) * 0.8f : 0;
                tex.SetPixel(x, y, new Color(1, 0.93f, 0.6f, a));
            }
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            Cache["rays"] = s;
            return s;
        }
    }
}
