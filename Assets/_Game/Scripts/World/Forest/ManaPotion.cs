using SeteVidas.Core;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O potinho de mana (18/11/2026, pedido do usuário): cai da Isyathy sempre que a flecha forte da Vulpis quebra o
    /// escudo da praga. Salta, pousa no chão e, encostando, recarrega todos os usos da Cura do Paladino
    /// (<see cref="HeartPickup.ConfigureMana"/>). Desenho em código: frasco de vidro com rolha, o líquido azul de mana
    /// com um brilho, e faíscas subindo.
    /// </summary>
    public static class ManaPotion
    {
        static Sprite s_sprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_sprite = null; }   // Play sem recarregar o domínio

        public static HeartPickup Spawn(Vector2 at)
        {
            var go = new GameObject("Potinho de mana") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.position = at;
            var art = new GameObject("Desenho");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite();
            sr.sortingOrder = 6;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            col.offset = new Vector2(0f, 0.4f);
            var pickup = go.AddComponent<HeartPickup>();
            pickup.ConfigureMana(sr);
            go.AddComponent<ManaSparkle>();
            return pickup;
        }

        /// <summary>O frasco (10 × 13 px, o pivô no fundo): rolha, gargalo, bojo redondo com o líquido azul e o brilho.</summary>
        public static Sprite Sprite()
        {
            if (s_sprite != null) return s_sprite;
            const int w = 10, h = 13;
            var px = new Color32[w * h];
            Color32 o = new Color32(0x14, 0x10, 0x1a, 0xff), glass = new Color32(0xc8, 0xe8, 0xf0, 0xff), cork = new Color32(0xa0, 0x6a, 0x3a, 0xff), corkD = new Color32(0x6a, 0x42, 0x22, 0xff);
            Color32 mana = new Color32(0x3a, 0x8a, 0xf0, 0xff), manaL = new Color32(0x7a, 0xd8, 0xff, 0xff), manaD = new Color32(0x24, 0x4a, 0xb0, 0xff), shine = new Color32(0xff, 0xff, 0xff, 0xff);
            // as linhas vão de baixo (0) para cima (h - 1)
            string[] rows =
            {
                "..oooooo..",
                ".oDDDDDDo.",
                "oDMMMMMMDo",
                "oMMMMMMMMo",
                "oMLMMMMMMo",
                "oMLMMMMMgo",
                ".oMMMMMgo.",
                ".oggggggo.",
                "..ogxggo..",
                "...oggo...",
                "...occo...",
                "..occcco..",
                "...oooo...",
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = rows[h - 1 - y][x];
                    Color32 col;
                    switch (c)
                    {
                        case 'o': col = o; break;
                        case 'D': col = manaD; break;
                        case 'M': col = mana; break;
                        case 'L': col = manaL; break;
                        case 'g': col = glass; break;
                        case 'x': col = shine; break;
                        case 'c': col = y == h - 2 ? cork : corkD; break;
                        default: continue;
                    }
                    px[y * w + x] = col;
                }
            px[(h - 3) * w + 4] = cork;   // a luz na rolha
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "potinho_mana", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
            s_sprite.hideFlags = HideFlags.DontSave;
            return s_sprite;
        }
    }

    /// <summary>Faíscas azuis subindo do potinho (chama a atenção no meio da luta).</summary>
    public class ManaSparkle : MonoBehaviour
    {
        void Update()
        {
            if (Random.value > Time.deltaTime * 6f) return;
            SeteVidas.Combat.HitSparks.One((Vector2)transform.position + new Vector2(Random.Range(-0.35f, 0.35f), Random.Range(0.3f, 0.9f)),
                new Vector2(0f, Random.Range(0.8f, 1.6f)), Random.value < 0.5f ? new Color32(0x7a, 0xd8, 0xff, 0xff) : new Color32(0xe0, 0xf8, 0xff, 0xff), 0.6f, 0f);
        }
    }
}
