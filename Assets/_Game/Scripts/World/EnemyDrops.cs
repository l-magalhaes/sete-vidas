using SeteVidas.Core;
using SeteVidas.Classes;
using SeteVidas.Progression;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// O que todo inimigo solta ao morrer (18/11/2026, pedido do usuário: "todos os inimigos dropam corações igual os cães
    /// templários, e 10% de chance do potinho de mana"):
    /// <list type="bullet">
    /// <item>Meio coração com <see cref="HeartChance"/> (a mesma chance dos templários, SentinelSettings.HeartDropChance).</item>
    /// <item>O potinho de mana (<see cref="ManaPotion"/>, recarrega a Cura do Paladino) com <see cref="ManaChance"/>, só
    /// depois de ganhar o Paladino (antes, na Fortaleza, não serve: não cai).</item>
    /// </list>
    /// Os templários (EnemyAI) já soltavam o coração pelo prefab deles; aqui eles só ganham o potinho. Os zumbis e os
    /// inimigos da Floresta usam os dois.
    /// </summary>
    public static class EnemyDrops
    {
        public const float HeartChance = 0.15f, ManaChance = 0.10f;
        static Sprite s_heart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_heart = null; }   // Play sem recarregar o domínio

        /// <summary>O que cai com estas sortes (0..1): o coração e/ou o potinho.</summary>
        public static bool DropsHeart(float roll) => HealthRules.RollDrop(HeartChance, roll);
        /// <summary>
        /// O potinho só cai depois de ganhar o Paladino (que ensina a Cura): antes, na primeira fase, não serve para nada
        /// (pedido do usuário, 18/11/2026).
        /// </summary>
        public static bool DropsMana(float roll, bool paladinUnlocked) => paladinUnlocked && HealthRules.RollDrop(ManaChance, roll);

        static bool PaladinUnlocked() { var d = SaveManager.Data; return d != null && d.IsUnlocked(CharacterClass.Paladin); }

        /// <summary>Um inimigo morreu em <paramref name="at"/> (o peito dele): sorteia o coração e o potinho.</summary>
        public static void OnDeath(Vector2 at, bool heart = true)
        {
            if (heart && DropsHeart(Random.value)) SpawnHeart(at);
            if (DropsMana(Random.value, PaladinUnlocked())) ManaPotion.Spawn(at + new Vector2(heart ? 0.5f : 0f, 0f));
        }

        /// <summary>O meio coração (o mesmo do templário): salta, pousa e cura meio coração ao encostar.</summary>
        public static HeartPickup SpawnHeart(Vector2 at)
        {
            var go = new GameObject("Meio coração") { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.position = at;
            var art = new GameObject("Desenho");
            art.transform.SetParent(go.transform, false);
            var sr = art.AddComponent<SpriteRenderer>();
            sr.sprite = Heart();
            sr.sortingOrder = 6;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            col.offset = new Vector2(0f, 0.3f);
            var pickup = go.AddComponent<HeartPickup>();
            pickup.Configure(sr, 1);
            return pickup;
        }

        /// <summary>O coração (9 × 8 px, o pivô no fundo): vermelho, com o brilho e o contorno do jogo.</summary>
        static Sprite Heart()
        {
            if (s_heart != null) return s_heart;
            const int w = 9, h = 8;
            string[] rows =   // de cima para baixo
            {
                ".oo...oo.",
                "oRRo.oRRo",
                "oRxRoRRRo",
                "oRRRRRRRo",
                ".oRRRRRo.",
                "..oRRRo..",
                "...oRo...",
                "....o....",
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = rows[h - 1 - y][x];
                    if (c == 'o') px[y * w + x] = new Color32(0x3a, 0x0f, 0x18, 0xff);
                    else if (c == 'R') px[y * w + x] = new Color32(0xd6, 0x30, 0x4a, 0xff);
                    else if (c == 'x') px[y * w + x] = new Color32(0xff, 0x8a, 0x9a, 0xff);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "coracao_drop", hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(false);
            s_heart = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
            s_heart.hideFlags = HideFlags.DontSave;
            return s_heart;
        }
    }
}
