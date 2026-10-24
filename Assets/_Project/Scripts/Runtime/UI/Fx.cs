using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TombHunter
{
    // Слой эффектов поверх сцены: пиксельные частицы, всплывающие надписи, кольца, вспышки, перелёты карт, твины.
    public class Fx : MonoBehaviour
    {
        class Part { public RectTransform rt; public Image img; public Vector2 p, v, c; public float g, life, max, size, swirl; public bool tw; }
        class Preset { public string[] colors; public int n; public float speed, grav, size, life, lift; public bool twinkle, swirl; public float angle, spread = Mathf.PI * 2; }

        static readonly Dictionary<string, Preset> Presets = new Dictionary<string, Preset>
        {
            {"pickaxe", new Preset { colors = new[]{"#b07f50","#6e4a2e","#d9b98a","#ffffff"}, n = 20, speed = 4.2f, grav = .3f, size = 5, life = 38 }},
            {"key", new Preset { colors = new[]{"#f6c453","#fff2a8","#ffffff"}, n = 22, speed = 2.6f, grav = 0, size = 4, life = 46, twinkle = true }},
            {"torch", new Preset { colors = new[]{"#ff9a2e","#ffd34d","#e8483c","#fff2a8"}, n = 26, speed = 1.8f, grav = -.09f, size = 5, life = 52 }},
            {"smoke", new Preset { colors = new[]{"#7d5bc4","#3b2a5a","#b9a3e6"}, n = 22, speed = 1.6f, grav = -.04f, size = 7, life = 46 }},
            {"ice", new Preset { colors = new[]{"#8fe0f6","#ffffff","#3f8fe0"}, n = 24, speed = 4.6f, grav = .12f, size = 4, life = 36 }},
            {"scroll", new Preset { colors = new[]{"#b47cff","#e5d3ff","#6a3fb8","#fff2a8"}, n = 26, speed = 2.4f, grav = -.02f, size = 4, life = 52, swirl = true }},
            {"bomb", new Preset { colors = new[]{"#ff9a2e","#ffd34d","#6e4a2e","#93a3ba","#2a1b2e"}, n = 52, speed = 6.5f, grav = .26f, size = 6, life = 44 }},
            {"lantern", new Preset { colors = new[]{"#fff2a8","#ffd34d"}, n = 18, speed = 2.2f, grav = -.03f, size = 4, life = 44, twinkle = true }},
            {"map", new Preset { colors = new[]{"#f6e7c8","#d9b98a","#e8483c"}, n = 16, speed = 2.6f, grav = .08f, size = 5, life = 40 }},
            {"dust", new Preset { colors = new[]{"#f0c888","#d9b98a","#fff4dc"}, n = 14, speed = 2, grav = .05f, size = 5, life = 30 }},
            {"gold", new Preset { colors = new[]{"#f6c453","#fff2a8","#b9822a"}, n = 22, speed = 4, grav = .24f, size = 5, life = 44 }},
            {"trap", new Preset { colors = new[]{"#e8483c","#ffffff","#9e2a2a"}, n = 18, speed = 3.8f, grav = .16f, size = 4, life = 34 }},
            {"exit", new Preset { colors = new[]{"#fff2a8","#f6c453","#ffffff"}, n = 46, speed = 4.4f, grav = 0, size = 5, life = 64, twinkle = true }},
            {"altar", new Preset { colors = new[]{"#b47cff","#fff2a8","#e5d3ff"}, n = 20, speed = 2.2f, grav = -.05f, size = 4, life = 50, twinkle = true }},
            {"rocks", new Preset { colors = new[]{"#b07f50","#6e4a2e","#d9b98a"}, n = 12, speed = 1.2f, grav = .35f, size = 5, life = 30, angle = Mathf.PI * .2f, spread = Mathf.PI * .6f }},
            {"relic", new Preset { colors = new[]{"#ffd76a","#ffffff","#a9d9f5","#b47cff"}, n = 40, speed = 3.6f, grav = -.02f, size = 5, life = 60, twinkle = true }},
        };

        readonly List<Part> parts = new List<Part>();
        readonly Stack<Part> pool = new Stack<Part>();
        RectTransform rt;

        void Awake() { rt = (RectTransform)transform; }

        public Vector2 Local(Vector3 world) => rt.InverseTransformPoint(world);
        public static Vector3 WorldCenter(RectTransform r) => r.TransformPoint(r.rect.center);

        public void Burst(Vector3 world, string preset, int n = -1, float speedMul = 1, float lift = 0)
        {
            var p = Presets[preset];
            var at = Local(world);
            int count = n > 0 ? n : p.n;
            for (int i = 0; i < count; i++)
            {
                var part = pool.Count > 0 ? pool.Pop() : NewPart();
                part.rt.gameObject.SetActive(true);
                float a = p.angle + UnityEngine.Random.value * p.spread;
                float v = p.speed * speedMul * (0.35f + UnityEngine.Random.value * 0.8f) * 60f;
                // В UI ось Y смотрит вверх, а пресеты писались для экрана с осью вниз.
                part.p = at + new Vector2((UnityEngine.Random.value - .5f) * 10, (UnityEngine.Random.value - .5f) * 10);
                part.v = new Vector2(Mathf.Cos(a) * v, -(Mathf.Sin(a) * v) + (p.lift + lift) * 60f);
                part.g = -p.grav * 3600f;
                part.max = p.life / 60f; part.life = part.max * (0.7f + UnityEngine.Random.value * 0.5f);
                part.size = Mathf.Max(2, Mathf.Round(p.size * (0.6f + UnityEngine.Random.value * 0.7f)));
                part.img.color = Pixel.Hex(p.colors[i % p.colors.Length]);
                part.tw = p.twinkle; part.swirl = p.swirl ? (UnityEngine.Random.value < .5f ? -1 : 1) * 0.08f * 3600f : 0; part.c = at;
                part.rt.SetAsLastSibling();
                parts.Add(part);
            }
        }

        Part NewPart()
        {
            var r = Kit.Node("P", transform);
            var img = r.gameObject.AddComponent<Image>(); img.sprite = Pixel.Square(); img.raycastTarget = false;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            return new Part { rt = r, img = img };
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                if (p.swirl != 0) { var d = p.p - p.c; p.v += new Vector2(-d.y, d.x) * p.swirl * 0.05f * dt; }
                p.v.y += p.g * dt;
                p.v *= Mathf.Pow(0.985f, dt * 60f);
                p.p += p.v * dt; p.life -= dt;
                if (p.life <= 0) { p.rt.gameObject.SetActive(false); pool.Push(p); parts.RemoveAt(i); continue; }
                float k = p.life / p.max;
                var c = p.img.color; c.a = Mathf.Min(1, k * 1.6f); p.img.color = c;
                float s = p.tw ? Mathf.Max(1, Mathf.Round(p.size * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(p.life * 24f))))) : p.size;
                p.rt.anchoredPosition = new Vector2(Mathf.Round(p.p.x), Mathf.Round(p.p.y));
                p.rt.sizeDelta = new Vector2(s, s);
            }
        }

        // ---------- твины ----------
        public Coroutine Tween(float dur, Action<float> step, Action done = null, float delay = 0) => StartCoroutine(TweenCo(dur, step, done, delay));
        IEnumerator TweenCo(float dur, Action<float> step, Action done, float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            float t = 0;
            while (t < dur) { if (!Safe(() => step(t / dur))) yield break; yield return null; t += Time.unscaledDeltaTime; }
            if (Safe(() => step(1))) done?.Invoke();
        }

        // Интерфейс перерисовывается целиком, и объект твина может исчезнуть посреди анимации — тогда твин просто заканчивается.
        static bool Safe(Action a)
        {
            try { a(); return true; }
            catch (MissingReferenceException) { return false; }
            catch (NullReferenceException) { return false; }
        }
        public void After(float delay, Action a) => StartCoroutine(AfterCo(delay, a));
        IEnumerator AfterCo(float d, Action a) { yield return new WaitForSecondsRealtime(d); a(); }

        // Копия карты может унаследовать CanvasGroup (скрытая карта в руке), второй добавить нельзя.
        static CanvasGroup Group(GameObject g) { var c = g.GetComponent<CanvasGroup>(); if (c == null) c = g.AddComponent<CanvasGroup>(); return c; }

        public static float OutQuad(float t) => 1 - (1 - t) * (1 - t);
        public static float InOut(float t) => t < .5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }

        // ---------- одиночные эффекты ----------
        public void FloatText(Vector3 world, string text, Color color, float delay = 0)
        {
            After(delay, () =>
            {
                var t = Kit.Txt(transform, text, 20, color, TextAnchor.MiddleCenter, true);
                var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Kit.Ink; o.effectDistance = new Vector2(2, -2);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var r = t.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = new Vector2(300, 30);
                var at = Local(world);
                Tween(1.2f, k =>
                {
                    r.anchoredPosition = at + new Vector2(0, k < .2f ? Mathf.Lerp(0, 12, k / .2f) : Mathf.Lerp(12, 70, (k - .2f) / .8f));
                    float s = k < .2f ? Mathf.Lerp(.6f, 1.15f, k / .2f) : Mathf.Lerp(1.15f, 1f, (k - .2f) / .8f);
                    r.localScale = Vector3.one * s;
                    var c = t.color; c.a = k < .2f ? k / .2f : 1 - (k - .2f) / .8f; t.color = c;
                }, () => Destroy(t.gameObject));
            });
        }

        public void PopSprite(Vector3 world, string sprite, float delay = 0)
        {
            After(delay, () =>
            {
                var img = Kit.Icon(transform, sprite, 52);
                var r = img.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
                var at = Local(world); float dx = (UnityEngine.Random.value - .5f) * 60;
                Tween(.65f, k =>
                {
                    Vector2 off = k < .35f ? Vector2.Lerp(Vector2.zero, new Vector2(dx * .5f, 34), k / .35f) : Vector2.Lerp(new Vector2(dx * .5f, 34), new Vector2(dx, -40), (k - .35f) / .65f);
                    r.anchoredPosition = at + off;
                    r.localScale = Vector3.one * (k < .35f ? Mathf.Lerp(1, 1.25f, k / .35f) : Mathf.Lerp(1.25f, .7f, (k - .35f) / .65f));
                    r.localRotation = Quaternion.Euler(0, 0, -dx * .8f * k);
                    var c = img.color; c.a = k < .35f ? 1 : 1 - (k - .35f) / .65f; img.color = c;
                }, () => Destroy(img.gameObject));
            });
        }

        Image Disk(Sprite s, Vector3 world, float size, Color color)
        {
            var img = Kit.Img(transform, s, color, "Disk");
            var r = img.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = new Vector2(size, size); r.anchoredPosition = Local(world);
            return img;
        }

        public void Ring(Vector3 world, Color color, float size = 60)
        {
            var img = Disk(Pixel.Ring(), world, size, color);
            Tween(.75f, k => { img.rectTransform.localScale = Vector3.one * Mathf.Lerp(.2f, 4f, OutQuad(k)); var c = img.color; c.a = 1 - k; img.color = c; }, () => Destroy(img.gameObject));
        }

        public void Flash(Vector3 world, float size)
        {
            var img = Disk(Pixel.Glow(), world, size, Color.white);
            Tween(.42f, k => { img.rectTransform.localScale = Vector3.one * Mathf.Lerp(.3f, 1.6f, OutQuad(k)); var c = img.color; c.a = 1 - k; img.color = c; }, () => Destroy(img.gameObject));
        }

        public void Rays(Vector3 world, float size = 260)
        {
            var img = Disk(Pixel.Rays(), world, size, Color.white);
            Tween(1.5f, k =>
            {
                img.rectTransform.localScale = Vector3.one * (k < .3f ? Mathf.Lerp(.2f, 1, k / .3f) : Mathf.Lerp(1, 1.3f, (k - .3f) / .7f));
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, -120 * k);
                var c = img.color; c.a = k < .3f ? k / .3f : 1 - (k - .3f) / .7f; img.color = c;
            }, () => Destroy(img.gameObject));
        }

        public void Shake(RectTransform target, float power = 7)
        {
            var basePos = target.anchoredPosition;
            Tween(.42f, k =>
            {
                float a = power * (1 - k);
                target.anchoredPosition = k >= 1 ? basePos : basePos + new Vector2(UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a));
            });
        }

        public void Bump(RectTransform target, float amount = .18f, float dur = .45f)
        {
            Tween(dur, k => target.localScale = Vector3.one * (1 + amount * Mathf.Sin(Mathf.PI * Mathf.Min(1, k * 1.2f))), () => target.localScale = Vector3.one);
        }

        // Копия карты летит по дуге в цель и уменьшается.
        public void FlyClone(RectTransform source, Vector3 toWorld, Action done)
        {
            var clone = Instantiate(source.gameObject, transform);
            foreach (var g in clone.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            var r = (RectTransform)clone.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = source.rect.size;
            var cg = Group(clone); cg.alpha = 1;
            Vector2 a = Local(WorldCenter(source)), b = Local(toWorld);
            r.anchoredPosition = a; r.localScale = Vector3.one;
            bool fired = false;
            void Finish() { if (fired) return; fired = true; Destroy(clone); done?.Invoke(); }
            Tween(.36f, k =>
            {
                float e = k * k;
                var mid = Vector2.Lerp(a, b, .45f) + new Vector2(0, 70);
                var p = e < .45f ? Vector2.Lerp(a, mid, e / .45f) : Vector2.Lerp(mid, b, (e - .45f) / .55f);
                r.anchoredPosition = p;
                r.localScale = Vector3.one * Mathf.Lerp(1, .32f, e);
                r.localRotation = Quaternion.Euler(0, 0, e < .45f ? Mathf.Lerp(0, 10, e / .45f) : Mathf.Lerp(10, -8, (e - .45f) / .55f));
            }, Finish);
        }

        // Мини-карта со спрайтом летит из точки к цели; цель появляется по прилёту.
        public void FlyMini(string sprite, Vector3 fromWorld, RectTransform target, float delay, bool targetIsCard, Action onLand = null)
        {
            CanvasGroup tcg = null;
            if (targetIsCard) { tcg = target.GetComponent<CanvasGroup>(); if (tcg == null) tcg = target.gameObject.AddComponent<CanvasGroup>(); tcg.alpha = 0; }
            After(delay, () =>
            {
                if (target == null) return;
                var mini = Kit.Node("Mini", transform);
                mini.anchorMin = mini.anchorMax = mini.pivot = new Vector2(.5f, .5f); mini.sizeDelta = new Vector2(46, 64);
                var bg = Kit.Img(mini, Pixel.Box(Kit.Parch, Kit.Ink, 3, 6), Color.white, "Bg"); bg.rectTransform.Fill();
                var ic = Kit.Icon(mini, sprite, 30); ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = ic.rectTransform.pivot = new Vector2(.5f, .5f);
                Vector2 a = Local(fromWorld), b = Local(WorldCenter(target));
                float endScale = targetIsCard ? 1.4f : .6f;
                bool fired = false;
                void Land()
                {
                    if (fired) return; fired = true; Destroy(mini.gameObject);
                    if (target == null) return;
                    if (tcg != null) tcg.alpha = 1;
                    if (targetIsCard) Tween(.32f, k => target.localScale = Vector3.one * Mathf.Lerp(1.25f, 1, k), () => target.localScale = Vector3.one);
                    onLand?.Invoke();
                }
                Tween(.56f, k =>
                {
                    float e = OutQuad(k);
                    var mid = Vector2.Lerp(a, b, .4f) + new Vector2(0, 90);
                    mini.anchoredPosition = e < .4f ? Vector2.Lerp(a, mid, e / .4f) : Vector2.Lerp(mid, b, (e - .4f) / .6f);
                    mini.localScale = Vector3.one * (e < .4f ? Mathf.Lerp(.4f, 1.15f, e / .4f) : Mathf.Lerp(1.15f, endScale, (e - .4f) / .6f));
                    mini.localRotation = Quaternion.Euler(0, 0, e < .4f ? Mathf.Lerp(0, 12, e / .4f) : Mathf.Lerp(12, 0, (e - .4f) / .6f));
                }, Land);
            });
        }

        // Потерянная карта краснеет (ловушка) или сереет (сброс) и падает.
        public void Drop(RectTransform source, bool red)
        {
            var clone = Instantiate(source.gameObject, transform);
            foreach (var g in clone.GetComponentsInChildren<Graphic>()) { g.raycastTarget = false; if (red) g.color *= new Color(1f, .55f, .5f); else g.color *= new Color(.7f, .7f, .7f); }
            var r = (RectTransform)clone.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = source.rect.size;
            var a = Local(WorldCenter(source));
            var cg = Group(clone); cg.alpha = 1;
            if (red) Burst(WorldCenter(source), "trap");
            Tween(.7f, k =>
            {
                r.anchoredPosition = a + (k < .25f ? new Vector2(0, 16 * k / .25f) : Vector2.Lerp(new Vector2(0, 16), new Vector2(red ? 40 : -20, -120), (k - .25f) / .75f));
                r.localRotation = Quaternion.Euler(0, 0, (red ? -28 : 20) * k);
                cg.alpha = k < .25f ? 1 : 1 - (k - .25f) / .75f;
            }, () => Destroy(clone));
        }
    }
}
