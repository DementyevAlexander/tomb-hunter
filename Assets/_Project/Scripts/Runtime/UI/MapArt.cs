using UnityEngine;

namespace TombHunter
{
    // Пиксельные карты: город Эшфорд 160×90 и мир 192×96; рисуются один раз при запуске.
    public static class MapArt
    {
        static Texture2D city, world, gradient;

        static Rng R(int seed) => new Rng(seed);

        public static Texture2D Gradient()
        {
            if (gradient != null) return gradient;
            gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++) gradient.SetPixel(0, y, Color.Lerp(Kit.BgBot, Kit.BgTop, y / 63f));
            gradient.Apply();
            return gradient;
        }

        public static Texture2D City()
        {
            if (city != null) return city;
            const int W = 160, H = 90;
            var tex = Pixel.NewTex(W, H);
            var grid = new int[W * H]; // 0 трава, 1 вода, 2 дорога, 3 занято
            var rng = R(77);
            void Set(int px, int py, int v, Color c) { if (px < 0 || py < 0 || px >= W || py >= H) return; grid[py * W + px] = v; tex.SetPixel(px, H - 1 - py, c); }
            int G(int px, int py) => px < 0 || py < 0 || px >= W || py >= H ? -1 : grid[py * W + px];
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    bool sea = Mathf.Pow(px - 160, 2) / (62f * 62f) + Mathf.Pow(py - 90, 2) / (36f * 36f) < 1;
                    float v = rng.Next();
                    if (sea) Set(px, py, 1, Pixel.Hex(v < .06f ? "#5aa9e0" : "#3f8fd0"));
                    else Set(px, py, 0, Pixel.Hex(v < .12f ? "#6aae4c" : v < .2f ? "#8ed06a" : "#7cc35e"));
                }
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    if (G(px, py) != 0) continue;
                    if (G(px + 1, py) == 1 || G(px - 1, py) == 1 || G(px, py + 1) == 1 || G(px, py - 1) == 1 || G(px + 2, py) == 1 || G(px, py + 2) == 1)
                        Set(px, py, 0, Pixel.Hex("#f0d890"));
                }
            for (int py = 0; py < H; py++)
            {
                int cx = Mathf.RoundToInt(98 + 7 * Mathf.Sin(py / 9f) + py * .25f);
                for (int w = -2; w <= 2; w++) if (G(cx + w, py) == 0) Set(cx + w, py, 1, Pixel.Hex("#4f9fe0"));
            }
            void Road(int x0, int y0, int x1, int y1)
            {
                int n = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
                for (int i = 0; i <= n; i++)
                {
                    int px = Mathf.RoundToInt(x0 + (x1 - x0) * i / (float)n), py = Mathf.RoundToInt(y0 + (y1 - y0) * i / (float)n);
                    for (int w = -1; w <= 1; w++) for (int h = -1; h <= 1; h++) Set(px + w, py + h, 2, Pixel.Hex(G(px + w, py + h) == 1 ? "#8a5a32" : "#e3c68c"));
                }
            }
            int plx = 74, ply = 46;
            Road(plx, ply, 32, 54); Road(plx, ply, 31, 27); Road(plx, ply, 112, 27); Road(plx, ply, 86, 66);
            for (int a = 0; a < 360; a += 4) for (int rr = 0; rr <= 7; rr++) Set(Mathf.RoundToInt(plx + Mathf.Cos(a * Mathf.Deg2Rad) * rr), Mathf.RoundToInt(ply + Mathf.Sin(a * Mathf.Deg2Rad) * rr), 2, Pixel.Hex("#e3c68c"));
            for (int a = 0; a < 360; a += 8) for (int rr = 0; rr <= 2; rr++) Set(Mathf.RoundToInt(plx + Mathf.Cos(a * Mathf.Deg2Rad) * rr), Mathf.RoundToInt(ply + Mathf.Sin(a * Mathf.Deg2Rad) * rr), 3, Pixel.Hex("#8fe0f6"));
            var hedge = Pixel.Hex("#3f7f3a");
            for (int px = 14; px <= 48; px++) { Set(px, 14, 3, hedge); Set(px, 64, 3, hedge); }
            for (int py = 14; py <= 64; py++) { Set(14, py, 3, hedge); if (py < 50 || py > 58) Set(48, py, 3, hedge); }
            int[,] pois = { { 31, 27 }, { 29, 54 }, { 112, 27 }, { 86, 66 }, { 74, 46 } };
            bool Near(float px, float py, float d) { for (int i = 0; i < pois.GetLength(0); i++) if (Mathf.Abs(pois[i, 0] - px) < d && Mathf.Abs(pois[i, 1] - py) < d) return true; return false; }
            bool Free(int px, int py, int w, int h)
            {
                for (int yy = py - 1; yy <= py + h; yy++) for (int xx = px - 1; xx <= px + w; xx++) if (G(xx, yy) != 0) return false;
                return !Near(px + w / 2f, py + h / 2f, 15) && !(px < 50 && py < 66);
            }
            string[] roofs = { "#c8693a", "#b24a3a", "#4f7fbf", "#8a4f9a" };
            for (int i = 0; i < 220; i++)
            {
                int w = 6 + rng.Range(3), h = 5, px = rng.Range(W - w), py = rng.Range(H - h);
                if (!Free(px, py, w, h)) continue;
                var roof = Pixel.Hex(roofs[rng.Range(roofs.Length)]);
                for (int yy = 0; yy < h; yy++) for (int xx = 0; xx < w; xx++)
                    Set(px + xx, py + yy, 3, yy < 2 ? roof : (xx == w / 2 && yy == h - 1 ? Pixel.Hex("#6e4a2e") : Pixel.Hex("#f6e7c8")));
                for (int xx = 0; xx < w; xx++) Set(px + xx + 1, py + h, 3, Pixel.Hex("#4e8a3a"));
            }
            for (int i = 0; i < 260; i++)
            {
                int px = rng.Range(W), py = rng.Range(H);
                bool garden = px > 15 && px < 47 && py > 15 && py < 63 && G(px, py) == 0 && !Near(px, py, 12);
                if (!Free(px, py, 3, 3) && !garden) continue;
                Set(px + 1, py, 3, Pixel.Hex("#2f6a2a")); Set(px, py + 1, 3, Pixel.Hex("#2f6a2a")); Set(px + 1, py + 1, 3, Pixel.Hex("#3f8a3a"));
                Set(px + 2, py + 1, 3, Pixel.Hex("#2f6a2a")); Set(px + 1, py + 2, 3, Pixel.Hex("#24521f"));
            }
            int[,] boats = { { 138, 74 }, { 148, 82 }, { 128, 84 } };
            for (int b = 0; b < 3; b++) { for (int i = 0; i < 4; i++) Set(boats[b, 0] + i, boats[b, 1], 1, Pixel.Hex("#f6e7c8")); Set(boats[b, 0] + 1, boats[b, 1] - 1, 1, Pixel.Hex("#e8483c")); }
            tex.Apply();
            return city = tex;
        }

        public static Texture2D World()
        {
            if (world != null) return world;
            const int W = 192, H = 96;
            var tex = Pixel.NewTex(W, H);
            var rng = R(4242);
            bool Land(int px, int py) => Data.IsLand(Mathf.FloorToInt(px / 4f), Mathf.FloorToInt(py / 4f));
            bool In(int px, int py) => px >= 0 && px < W && py >= 0 && py < H;
            for (int py = 0; py < H; py++)
                for (int px = 0; px < W; px++)
                {
                    int r = py / 4, c = px / 4; float v = rng.Next();
                    string col;
                    if (Land(px, py))
                    {
                        bool coast = (In(px + 1, py) && !Land(px + 1, py)) || (In(px - 1, py) && !Land(px - 1, py)) || (In(px, py + 1) && !Land(px, py + 1)) || (In(px, py - 1) && !Land(px, py - 1));
                        if (r <= 1 || r >= 21) col = v < .15f ? "#c9d6e6" : "#eef2f8";
                        else if (coast) col = "#f0d890";
                        else if (r >= 7 && r <= 10 && c >= 20 && c <= 34) col = v < .15f ? "#d9a95a" : "#e8c47a";
                        else if (r >= 9 && r <= 13 && ((c >= 9 && c <= 18) || (c >= 36 && c <= 44))) col = v < .2f ? "#3f8a3a" : "#5aa448";
                        else col = v < .14f ? "#5a9a46" : v < .2f ? "#8ed06a" : "#72b856";
                    }
                    else
                    {
                        bool shallow = Land(px + 2, py) || Land(px - 2, py) || Land(px, py + 2) || Land(px, py - 2) || Land(px + 3, py) || Land(px - 3, py);
                        col = shallow ? (v < .1f ? "#7cc4ec" : "#5aaee4") : (v < .05f ? "#4a8fd0" : v < .08f ? "#2f6aa8" : "#3a7dc0");
                    }
                    tex.SetPixel(px, H - 1 - py, Pixel.Hex(col));
                }
            int[,] mounts = { { 35, 8 }, { 36, 8 }, { 34, 7 }, { 10, 5 }, { 12, 6 }, { 15, 15 }, { 26, 4 } };
            for (int i = 0; i < mounts.GetLength(0); i++)
            {
                int px = mounts[i, 0] * 4 + 1, py = mounts[i, 1] * 4;
                for (int k = 0; k < 3; k++) tex.SetPixel(px + k, H - 1 - (py + 2), Pixel.Hex("#8a7f74"));
                tex.SetPixel(px + 1, H - 1 - (py + 1), Pixel.Hex("#8a7f74"));
                tex.SetPixel(px + 1, H - 1 - py, Color.white);
            }
            tex.Apply();
            return world = tex;
        }
    }
}
