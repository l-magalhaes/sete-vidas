using System;
using System.Collections.Generic;
using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>A colmeia e o mel no jogo (sprites feitos dos desenhos de <see cref="BeeArt"/>).</summary>
    public static class BeeHive
    {
        static Sprite s_hive, s_honey;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_hive = null; s_honey = null; }   // Play sem recarregar o domínio

        /// <summary>Um sprite do desenho (linhas de cima para baixo).</summary>
        public static Sprite Make(uint[] px, int w, int h, Vector2 pivot, string name)
        {
            var flipped = new uint[w * h];
            for (int y = 0; y < h; y++) Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
            var tex = new Texture2D(w, h, TextureFormat.BGRA32, false)
                { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name, hideFlags = HideFlags.DontSave };
            tex.SetPixelData(flipped, 0);
            tex.Apply(false);
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, 16f);
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        static Sprite HiveSprite()
        {
            if (s_hive == null) s_hive = Make(BeeArt.Hive(), BeeArt.HiveW, BeeArt.HiveH, new Vector2(0.5f, 1f), "colmeia");
            return s_hive;
        }

        static Sprite HoneySprite()
        {
            if (s_honey == null) s_honey = Make(BeeArt.Honey(), BeeArt.HoneyW, BeeArt.HoneyH, new Vector2(0.5f, 0f), "mel");
            return s_honey;
        }

        /// <summary>
        /// Pendura a colmeia no teto (ou galho) acima de <paramref name="home"/>, onde a vespa dorme: a boca fica na altura
        /// dela. Fica atrás da vespa e continua lá quando ela sai voando ou morre.
        /// </summary>
        public static SpriteRenderer Hang(Transform parent, Vector2 home, int order)
        {
            int mask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Platform);
            var hit = Physics2D.Raycast(home, Vector2.up, 3f, mask);
            float top = hit.collider != null ? hit.point.y + 0.15f : home.y + 1.2f;
            var go = new GameObject("Colmeia");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(home.x - 0.05f, top, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = HiveSprite();
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>O mel que cai da vespa morta: salta, pousa no chão e, encostando, vai para o bolso (<see cref="HoneyRules"/>).</summary>
        public static HeartPickup SpawnHoney(Vector2 at)
        {
            var go = new GameObject("Mel") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.position = at;
            var art = new GameObject("Desenho");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = HoneySprite();
            sr.sortingOrder = 6;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            col.offset = new Vector2(0f, 0.3f);
            var pickup = go.AddComponent<HeartPickup>();
            pickup.ConfigureHoney(sr);
            return pickup;
        }
    }
}
