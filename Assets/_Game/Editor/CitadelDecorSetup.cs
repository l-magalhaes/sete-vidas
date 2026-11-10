using System.IO;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Dialogue;
using SeteVidas.World;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Cenário da Cidadela do Sol Eterno (Ordem da Presa Dourada), onde os filhotes viram templários e os templários,
    /// paladinos: estandartes, estátuas, pátio de treino (bonecos, alvos, armas), escudos, tochas, vitrais e
    /// placas de lore (E para ler). Chamado pelo FortressSetup. Spec de arte: Docs/Arte/decisoes.md.
    /// </summary>
    public static class CitadelDecorSetup
    {
        const string Art = "Assets/_Game/Art/Environments/Citadel";
        const int Behind = -5;   // atrás de personagens e armadilhas, na frente do fundo (-10)
        const float Upper = 14f;

        // Cores da Ordem: carmesim e dourado (os guardas usam o mesmo carmesim no tabardo)
        static readonly Color32 Crimson = new Color32(0x8f, 0x2a, 0x2a, 0xff);
        static readonly Color32 CrimsonDark = new Color32(0x5a, 0x16, 0x1c, 0xff);
        static readonly Color32 Gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
        static readonly Color32 GoldDark = new Color32(0x8a, 0x62, 0x1e, 0xff);
        static readonly Color32 Stone = new Color32(0x7a, 0x74, 0x8e, 0xff);
        static readonly Color32 StoneDark = new Color32(0x4a, 0x45, 0x5c, 0xff);
        static readonly Color32 Ink = new Color32(0x1c, 0x1a, 0x24, 0xff);
        static readonly Color32 Wood = new Color32(0x6b, 0x4a, 0x2b, 0xff);
        static readonly Color32 WoodLight = new Color32(0x9a, 0x70, 0x44, 0xff);

        // --------------------------------------------------------- montagem

        public static void PlaceInFortress(Transform parent)
        {
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            var root = new GameObject("Cenário da Cidadela").transform;
            root.SetParent(parent, false);
            var lore = CreateLore();

            // 1) Entrada: a muralha anuncia a Ordem
            Prop(root, "Estátua do guardião", StatueSprite(), new Vector2(28.5f, 0f), bottom: true);
            Prop(root, "Estandarte da entrada", BannerSprite(), new Vector2(35.5f, 7.9f), top: true);

            // 2) Pátio de treino dos escudeiros (depois da passagem baixa): bonecos de verdade para treinar
            Prop(root, "Alvo de arco", TargetSprite(), new Vector2(35.5f, 0f), bottom: true);
            Dummy(root, "Boneco de treino (olha para a direita)", 50.5f, TrainingDummy.Side.Right);
            Dummy(root, "Boneco de treino (olha para a esquerda)", 55.5f, TrainingDummy.Side.Left);
            Prop(root, "Suporte de armas de treino", RackSprite(), new Vector2(58.3f, 0f), bottom: true);
            Prop(root, "Estandarte do pátio", BannerSprite(), new Vector2(53f, 7.9f), top: true);
            Sign(root, "Placa do pátio", 48.6f, 0f, lore.Yard);
            TrapsSetup.Sign(root, 53f, 3.4f, "pátio de treino: bata nos bonecos (pelas costas dói mais)");

            // 3) Muralha: estandartes longos e um paladino de pedra vigiando a hera
            Prop(root, "Estandarte da muralha 1", BannerSprite(), new Vector2(62.5f, 23.9f), top: true);
            Prop(root, "Estandarte da muralha 2", BannerSprite(), new Vector2(69.5f, 23.9f), top: true);
            Prop(root, "Estátua da muralha", StatueSprite(), new Vector2(65f, 0f), bottom: true);

            // 4) Corredor (alojamento dos templários): tochas só no meio iluminado, nada nas sombras
            Prop(root, "Tocha 1", TorchSprite(), new Vector2(88f, Upper + 2.6f));
            Prop(root, "Tocha 2", TorchSprite(), new Vector2(97f, Upper + 2.6f));
            Prop(root, "Estandarte do corredor", BannerSprite(), new Vector2(92f, 23.9f), top: true);
            Sign(root, "Placa do alojamento", 110f, Upper, lore.Barracks);

            // 5) Salão de armas: vitrais, escudos da Ordem e estandartes
            Prop(root, "Vitral 1", WindowSprite(), new Vector2(118f, Upper + 6f));
            Prop(root, "Vitral 2", WindowSprite(), new Vector2(134f, Upper + 6f));
            for (int i = 0; i < 4; i++) Prop(root, $"Escudo {i + 1}", ShieldSprite(), new Vector2(113.5f + i * 8f, Upper + 3.3f));
            Prop(root, "Estandarte do salão", BannerSprite(), new Vector2(126.5f, 23.9f), top: true);
            Sign(root, "Placa do juramento", 140f, Upper, lore.Oath);

            // 6) Antes da câmara e a câmara do selo
            Sign(root, "Placa da câmara", 188f, Upper, lore.Seal);
            Prop(root, "Estátua da câmara 1", StatueSprite(), new Vector2(190f, Upper), bottom: true);
            Prop(root, "Estátua da câmara 2", StatueSprite(), new Vector2(215.5f, Upper), bottom: true);
            Prop(root, "Vitral da câmara 1", WindowSprite(), new Vector2(196f, Upper + 5.5f));
            Prop(root, "Vitral da câmara 2", WindowSprite(), new Vector2(209f, Upper + 5.5f));
            Prop(root, "Estandarte do selo", BannerSprite(), new Vector2(202.5f, 22.9f), top: true);
            Prop(root, "Estandarte do tomo", BannerSprite(), new Vector2(224f, 19.9f), top: true);
        }

        static void Prop(Transform parent, string name, Sprite sprite, Vector2 position, bool bottom = false, bool top = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = Behind;
            // Sprites daqui têm pivot central: "bottom" apoia no chão, "top" pendura no teto
            float half = sprite != null ? sprite.bounds.extents.y : 0f;
            go.transform.position = position + (bottom ? Vector2.up * half : top ? Vector2.down * half : Vector2.zero);
        }

        static void Dummy(Transform parent, string name, float x, TrainingDummy.Side side)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatSetup.DummyPrefabPath);
            if (prefab == null) prefab = CombatSetup.CreateDummyPrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = new Vector3(x, 0f, 0f);
            var dummy = go.GetComponent<TrainingDummy>();
            dummy.SetFacing(side);
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponentInChildren<SpriteRenderer>());
        }

        static void Sign(Transform parent, string name, float x, float floorY, DialogueDefinition text)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, floorY, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, 2f);
            box.offset = new Vector2(0f, 1f);
            var plate = new GameObject("Placa");
            plate.transform.SetParent(go.transform, false);
            var sr = plate.AddComponent<SpriteRenderer>();
            sr.sprite = PlaqueSprite();
            sr.sortingOrder = Behind + 1;
            plate.transform.localPosition = new Vector3(0f, sr.sprite.bounds.extents.y, 0f);
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var prompt = DamagePopup.CreateText(promptGo, "E: ler", new Color32(0xff, 0xe9, 0xa8, 0xff), 28);
            promptGo.SetActive(false);
            var sign = go.AddComponent<LoreSign>();
            var so = new SerializedObject(sign);
            so.FindProperty("text").objectReferenceValue = text;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------- lore

        struct Lore { public DialogueDefinition Yard, Barracks, Oath, Seal; }

        static Lore CreateLore()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var lore = new Lore
            {
                Yard = DialogueSetup.Conversation("D-PLACA-01", false, 1,
                    (null, "", "Pátio dos Escudeiros. Aqui o filhote vira templário, e o templário, paladino."),
                    (null, "", "Quem não aguenta o boneco de palha não aguenta a praga.")),
                Barracks = DialogueSetup.Conversation("D-PLACA-02", false, 2,
                    (null, "", "Alojamento dos Templários. Silêncio depois do toque do sino."),
                    (null, "", "Arranhado embaixo, com letra torta: \"o Koda ronca\"."),
                    (cat, "ironico", "Bom saber.")),
                Oath = DialogueSetup.Conversation("D-PLACA-03", false, 1,
                    (null, "", "Juramento da Ordem: guardar o selo. Servir o reino. Nunca abandonar um ferido."),
                    (cat, "ironico", "Bonito. Pena que eu não sou do reino.")),
                Seal = DialogueSetup.Conversation("D-PLACA-04", false, 2,
                    (null, "", "Câmara do Selo. Há cinco séculos, Golden, o Dourado, prendeu aqui um tomo que bebe vidas."),
                    (null, "", "Que ninguém o desperte. Que nenhum guardião o abandone."),
                    (cat, "", "...Ninguém falou nada de beber vidas.")),
            };
            AssetDatabase.SaveAssets();
            return lore;
        }

        // ---------------------------------------------------------- sprites

        delegate void Painter(System.Action<int, int, Color32> set);

        static Sprite Draw(string name, int w, int h, Painter paint)
        {
            string path = $"{Art}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            var px = new Color32[w * h];
            paint((x, y, c) => { if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = c; });
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // pasta Environments: pivot central
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Rect(System.Action<int, int, Color32> set, int x0, int y0, int x1, int y1, Color32 fill, Color32 edge)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                set(x, y, x == x0 || x == x1 || y == y0 || y == y1 ? edge : fill);
        }

        /// <summary>Emblema da Ordem: pata de cão sobre uma marreta (usado em estandartes, escudos e vitrais).</summary>
        static void Emblem(System.Action<int, int, Color32> set, int cx, int cy, Color32 c)
        {
            for (int y = cy - 3; y <= cy + 3; y++) set(cx, y, c);                // cabo da marreta
            for (int x = cx - 2; x <= cx + 2; x++) { set(x, cy + 3, c); set(x, cy + 4, c); } // cabeça
            for (int y = cy - 2; y <= cy; y++) for (int x = cx - 2; x <= cx + 2; x++) if (x != cx) set(x, y, c); // almofada da pata
            set(cx - 3, cy + 1, c); set(cx + 3, cy + 1, c); set(cx - 2, cy + 2, c); set(cx + 2, cy + 2, c); // dedos
        }

        /// <summary>Estandarte 16×40: haste, pano carmesim com barra dourada, emblema e ponta em V.</summary>
        static Sprite BannerSprite() => Draw("decor_banner", 16, 40, set =>
        {
            for (int x = 0; x < 16; x++) { set(x, 39, Wood); set(x, 38, WoodLight); } // haste
            for (int y = 4; y <= 37; y++)
            for (int x = 2; x <= 13; x++)
            {
                if (y < 10 && Mathf.Abs(x - 7.5f) < (10 - y)) continue;          // ponta em V
                set(x, y, x == 2 || x == 13 ? CrimsonDark : x == 3 || x == 12 ? Gold : Crimson);
            }
            Emblem(set, 8, 22, Gold);
        });

        /// <summary>Estátua 24×48 de um cão paladino de pedra, de pé, com a marreta apoiada, sobre um pedestal.</summary>
        static Sprite StatueSprite() => Draw("decor_statue", 24, 48, set =>
        {
            Rect(set, 2, 0, 21, 6, Stone, StoneDark);          // pedestal
            for (int x = 3; x <= 20; x += 4) set(x, 3, StoneDark);
            Rect(set, 7, 6, 16, 28, Stone, StoneDark);          // corpo de armadura
            Rect(set, 9, 10, 14, 24, StoneDark, StoneDark);     // tabardo esculpido
            Emblem(set, 11, 17, Stone);
            Rect(set, 7, 28, 16, 38, Stone, StoneDark);         // cabeça
            Rect(set, 16, 30, 20, 33, Stone, StoneDark);        // focinho
            Rect(set, 4, 27, 6, 36, StoneDark, StoneDark);      // orelha caída
            set(13, 35, Ink);                                    // olho
            for (int y = 6; y <= 30; y++) set(20, y, StoneDark); // cabo da marreta
            Rect(set, 18, 30, 23, 35, Stone, StoneDark);        // cabeça da marreta (para cima)
        });

        /// <summary>Alvo de palha 16×24 num cavalete, com anéis vermelhos e uma flecha cravada.</summary>
        static Sprite TargetSprite() => Draw("decor_archery_target", 16, 24, set =>
        {
            set(3, 0, Wood); set(3, 1, Wood); set(12, 0, Wood); set(12, 1, Wood);
            for (int y = 0; y < 14; y++) { set(4 + y / 7, y, Wood); set(11 - y / 7, y, Wood); } // cavalete
            for (int y = 8; y < 24; y++)
            for (int x = 0; x < 16; x++)
            {
                float d = Mathf.Sqrt((x - 7.5f) * (x - 7.5f) + (y - 15.5f) * (y - 15.5f));
                if (d > 7.6f) continue;
                set(x, y, d > 6.6f ? GoldDark : d > 5f ? new Color32(0xc9, 0xa2, 0x5a, 0xff) : d > 3.5f ? Crimson : d > 2f ? new Color32(0xe9, 0xdc, 0xb8, 0xff) : Crimson);
            }
            for (int i = 0; i < 4; i++) set(10 + i, 17 + i, Wood); // flecha cravada
        });

        /// <summary>Suporte 24×24 com espadas e marretas de madeira de treino.</summary>
        static Sprite RackSprite() => Draw("decor_weapon_rack", 24, 24, set =>
        {
            Rect(set, 1, 0, 2, 18, Wood, Wood);
            Rect(set, 21, 0, 22, 18, Wood, Wood);
            for (int x = 1; x <= 22; x++) { set(x, 16, WoodLight); set(x, 6, WoodLight); } // travessas
            foreach (int x in new[] { 5, 9, 13 })                                     // espadas de madeira
            {
                for (int y = 2; y <= 21; y++) set(x, y, WoodLight);
                set(x - 1, 17, Wood); set(x + 1, 17, Wood);
            }
            for (int y = 2; y <= 18; y++) set(18, y, Wood);                              // marreta de treino
            Rect(set, 16, 18, 20, 22, WoodLight, Wood);
        });

        /// <summary>Escudo 12×14 da Ordem (para pendurar): carmesim, borda dourada, emblema.</summary>
        static Sprite ShieldSprite() => Draw("decor_shield", 12, 14, set =>
        {
            for (int y = 0; y < 14; y++)
            {
                int half = y < 6 ? (int)(y * 0.9f) : 5;
                for (int x = 6 - half - 1; x <= 5 + half + 1; x++)
                    set(x, y, x == 6 - half - 1 || x == 5 + half + 1 || y == 13 ? Gold : Crimson);
            }
            Emblem(set, 6, 7, Gold);
        });

        /// <summary>Tocha de parede 8×16: suporte de ferro e chama.</summary>
        static Sprite TorchSprite() => Draw("decor_torch", 8, 16, set =>
        {
            for (int y = 0; y <= 8; y++) set(3, y, Ink);
            set(2, 8, Ink); set(4, 8, Ink); set(1, 9, Ink); set(5, 9, Ink);
            for (int y = 9; y < 16; y++)
            for (int x = 1; x <= 5; x++)
            {
                float d = Mathf.Abs(x - 3f) + (y - 9) * 0.5f;
                if (d > 3f) continue;
                set(x, y, d < 1.2f ? new Color32(0xff, 0xf2, 0xb0, 0xff) : d < 2.2f ? new Color32(0xff, 0xa8, 0x3a, 0xff) : new Color32(0xd8, 0x40, 0x2a, 0xff));
            }
        });

        /// <summary>Vitral em arco 16×32 com vidros azul e dourado e o emblema da Ordem no centro.</summary>
        static Sprite WindowSprite() => Draw("decor_stained_glass", 16, 32, set =>
        {
            var blue = new Color32(0x2a, 0x4a, 0x8f, 0xff);
            var blueLight = new Color32(0x4a, 0x7a, 0xc8, 0xff);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 16; x++)
            {
                bool inside = y < 24 || (x - 7.5f) * (x - 7.5f) + (y - 24f) * (y - 24f) <= 56f; // arco no topo
                if (!inside) continue;
                bool edge = x == 0 || x == 15 || y == 0 || (y >= 24 && (x - 7.5f) * (x - 7.5f) + (y - 24f) * (y - 24f) > 42f);
                bool lead = x % 5 == 2 || y % 6 == 3;
                set(x, y, edge ? StoneDark : lead ? Ink : ((x / 5 + y / 6) % 2 == 0 ? blue : blueLight));
            }
            Emblem(set, 8, 15, Gold);
        });

        /// <summary>Placa de pedra 16×20 em pé, com linhas gravadas.</summary>
        static Sprite PlaqueSprite() => Draw("decor_lore_plaque", 16, 20, set =>
        {
            Rect(set, 1, 0, 14, 17, Stone, StoneDark);
            for (int x = 3; x <= 12; x++) { set(x, 18, StoneDark); }
            for (int y = 4; y <= 13; y += 3) for (int x = 4; x <= 11; x++) if ((x + y) % 4 != 0) set(x, y, StoneDark);
            Emblem(set, 8, 15, Gold);
        });
    }
}
