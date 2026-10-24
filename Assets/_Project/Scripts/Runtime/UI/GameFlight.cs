using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Перелёт: камера следует за вертолётом над пиксельной картой мира (увеличение ×3), облака, след и посадка.
    public partial class Game
    {
        RectTransform flightView, heli, heliShadow, rotor, flightCaption, trailRoot, cloudRoot, pinFrom, pinTo;
        RawImage flightMap;
        Image flightFade;

        class FlightState
        {
            public Vector2 a, ctrl, b; public float dur, t; public bool ended; public Action done; public float lastTrail;
            public List<(RectTransform rt, Vector2 pos, float v)> clouds = new List<(RectTransform, Vector2, float)>();
            public List<(RectTransform rt, Vector2 pos)> trail = new List<(RectTransform, Vector2)>();
        }
        FlightState flight;

        const float WorldScale = 3, CamW = 320, CamH = 180, MapPxW = 192 * WorldScale, MapPxH = 96 * WorldScale;

        void BuildFlight()
        {
            var s = NewScreen("flight");
            flightView = Frame(s, 126, Top, 1028, 600);
            flightMap = Kit.Node("Map", flightView).Fill().gameObject.AddComponent<RawImage>(); flightMap.texture = MapArt.World(); flightMap.raycastTarget = false;
            pinFrom = Kit.Icon(flightView, "pinhome", 30).rectTransform;
            pinTo = Kit.Icon(flightView, "pin", 30).rectTransform;
            trailRoot = Kit.Node("Trail", flightView).Fill();
            heliShadow = Kit.Icon(flightView, "heli", 40).rectTransform; heliShadow.GetComponent<Image>().color = new Color(.08f, .04f, .12f, .35f);
            heli = Kit.Icon(flightView, "heli", 40).rectTransform;
            rotor = Kit.Node("Rotor", heli); rotor.anchorMin = rotor.anchorMax = rotor.pivot = new Vector2(.5f, .5f); rotor.anchoredPosition = Vector2.zero;
            var b1 = Kit.Img(rotor, Pixel.Square(), new Color(.9f, .93f, .96f, .85f), "Blade"); b1.rectTransform.anchorMin = b1.rectTransform.anchorMax = new Vector2(.5f, .5f); b1.rectTransform.sizeDelta = new Vector2(1, .08f);
            var b2 = Kit.Img(rotor, Pixel.Square(), new Color(.9f, .93f, .96f, .85f), "Blade"); b2.rectTransform.anchorMin = b2.rectTransform.anchorMax = new Vector2(.5f, .5f); b2.rectTransform.sizeDelta = new Vector2(.08f, 1);
            cloudRoot = Kit.Node("Clouds", flightView).Fill();
            flightCaption = Kit.Node("Caption", flightView).Place(14, 14, 400, 120);
            var skip = Kit.Btn(flightView, "Пропустить", Kit.BtnStyle.Parch, () => { if (flight != null) flight.t = flight.dur; });
            var srt = (RectTransform)skip.transform; srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1, 0); srt.anchoredPosition = new Vector2(-12, 14); srt.sizeDelta = new Vector2(150, 42);
            flightFade = Kit.Img(flightView, Pixel.Square(), Kit.Parch, "Fade"); flightFade.rectTransform.Fill(); flightFade.color = new Color(Kit.Parch.r, Kit.Parch.g, Kit.Parch.b, 0);
        }

        static Vector2 CellPx(int col, int row) => new Vector2((col + .5f) * 4 * WorldScale, (row + .5f) * 4 * WorldScale);

        void StartFlight(int fc, int fr, bool fromHome, int tc, int tr, bool toHome, string[] caption, Action done)
        {
            Go("flight");
            var a = CellPx(fc, fr); var b = CellPx(tc, tr);
            flight = new FlightState
            {
                a = a, b = b, done = done,
                ctrl = new Vector2((a.x + b.x) / 2, Mathf.Min(a.y, b.y) - Mathf.Max(30, Mathf.Abs(a.x - b.x) * .18f)),
                dur = Mathf.Min(4.6f, 2.2f + Vector2.Distance(a, b) * .004f),
            };
            pinFrom.GetComponent<Image>().sprite = Pixel.Get(fromHome ? "pinhome" : "pin");
            pinTo.GetComponent<Image>().sprite = Pixel.Get(toHome ? "pinhome" : "pin");
            Kit.Clear(flightCaption);
            for (int i = 0; i < caption.Length; i++) ((RectTransform)Kit.PlateLabel(flightCaption, caption[i], 14).transform.parent).Place(0, i * 36, 10, 10);
            Kit.Clear(trailRoot); Kit.Clear(cloudRoot);
            for (int i = 0; i < 18; i++)
            {
                var cl = Kit.Node("Cloud", cloudRoot); cl.anchorMin = cl.anchorMax = new Vector2(0, 1); cl.pivot = new Vector2(0, 1);
                float sz = 6 + UnityEngine.Random.value * 10;
                void Blob(float x, float y, float w, float h) { var im = Kit.Img(cl, Pixel.Square(), new Color(1, 1, 1, .82f), "B"); im.rectTransform.Place(x, y, w, h); }
                Blob(0, 0, sz * 2, sz * .6f); Blob(sz * .4f, -sz * .4f, sz * 1.1f, sz * .6f); Blob(-sz * .3f, sz * .3f, sz * 1.5f, sz * .4f);
                flight.clouds.Add((cl, new Vector2(UnityEngine.Random.value * MapPxW, UnityEngine.Random.value * MapPxH), .02f + UnityEngine.Random.value * .03f));
            }
            flightFade.color = new Color(Kit.Parch.r, Kit.Parch.g, Kit.Parch.b, 0);
            TickFlight();
        }

        static Vector2 Bez(Vector2 p0, Vector2 p1, Vector2 p2, float t) => (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;

        void TickFlight()
        {
            if (flight == null || screen != "flight" || flight.ended) return;
            flight.t = Mathf.Min(flight.dur, flight.t + Time.unscaledDeltaTime);
            float k = flight.t / flight.dur, e = Fx.InOut(k);
            var pos = Bez(flight.a, flight.ctrl, flight.b, e);
            var nxt = Bez(flight.a, flight.ctrl, flight.b, Mathf.Min(1, e + .01f));
            var size = flightView.rect.size;
            float zoom = size.x / CamW; // пикселей вида на пиксель карты ×3
            float cx = Mathf.Clamp(pos.x - CamW / 2, 0, MapPxW - CamW), cy = Mathf.Clamp(pos.y - CamH / 2, 0, MapPxH - CamH);
            flightMap.uvRect = new Rect(cx / MapPxW, 1 - (cy + CamH) / MapPxH, CamW / MapPxW, CamH / MapPxH);
            Vector2 View(Vector2 p) => new Vector2((p.x - cx) * zoom, -(p.y - cy) * zoom);
            void PlaceAt(RectTransform r, Vector2 p, float s) { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = View(p); r.sizeDelta = new Vector2(s, s); }
            PlaceAt(pinFrom, flight.a - new Vector2(0, 3), 36); pinFrom.pivot = new Vector2(.5f, 0);
            PlaceAt(pinTo, flight.b - new Vector2(0, 3), 36); pinTo.pivot = new Vector2(.5f, 0);

            if (Time.unscaledTime - flight.lastTrail > .07f)
            {
                flight.lastTrail = Time.unscaledTime;
                var dot = Kit.Img(trailRoot, Pixel.Square(), new Color(1, 1, 1, .7f), "Dot").rectTransform;
                flight.trail.Add((dot, pos));
                if (flight.trail.Count > 60) { Kit.Kill(flight.trail[0].rt.gameObject); flight.trail.RemoveAt(0); }
            }
            foreach (var (rt, p) in flight.trail) PlaceAt(rt, p, 5);

            float alt = Mathf.Sin(Mathf.PI * Mathf.Min(1, e * 1.15f));
            float hs = 14 * (2 + 1.3f * alt) * zoom * .6f;
            float ang = Mathf.Atan2(nxt.y - pos.y, nxt.x - pos.x) * Mathf.Rad2Deg + 90;
            PlaceAt(heli, pos, hs); heli.localRotation = Quaternion.Euler(0, 0, -ang);
            float off = (4 + 16 * alt) * zoom * .6f;
            PlaceAt(heliShadow, pos, hs); heliShadow.anchoredPosition += new Vector2(off, -off); heliShadow.localRotation = heli.localRotation;
            rotor.sizeDelta = new Vector2(hs * .9f, hs * .9f);
            foreach (Transform bl in rotor) ((RectTransform)bl).sizeDelta = bl.GetSiblingIndex() == 0 ? new Vector2(hs * .9f, 3) : new Vector2(3, hs * .9f);
            rotor.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 1500);

            for (int i = 0; i < flight.clouds.Count; i++)
            {
                var c = flight.clouds[i];
                c.pos.x += c.v * 16 * 60 * Time.unscaledDeltaTime; if (c.pos.x > MapPxW + 40) c.pos.x = -40;
                flight.clouds[i] = c;
                c.rt.anchoredPosition = new Vector2((c.pos.x - cx * 1.15f) * zoom, -(c.pos.y - cy * 1.15f) * zoom);
                c.rt.localScale = Vector3.one * zoom;
            }

            if (k >= 1)
            {
                flight.ended = true;
                fx.Burst(Fx.WorldCenter(flightView), "dust", 26, 1.5f);
                var done = flight.done;
                fx.Tween(.45f, t => flightFade.color = new Color(Kit.Parch.r, Kit.Parch.g, Kit.Parch.b, t), () =>
                {
                    done?.Invoke();
                    flightFade.color = new Color(Kit.Parch.r, Kit.Parch.g, Kit.Parch.b, 0);
                });
            }
        }
    }
}
