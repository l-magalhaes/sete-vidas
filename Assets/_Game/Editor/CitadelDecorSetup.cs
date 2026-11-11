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
    /// placas de lore (E para ler). As posições vêm do FortressLayout; o FortressSetup chama estas peças. Spec de arte: Docs/Arte/decisoes.md.
    /// </summary>
    public static class CitadelDecorSetup
    {
        const string Art = "Assets/_Game/Art/Environments/Citadel";
        const int Behind = -5;   // atrás de personagens e armadilhas, na frente do fundo (-10)


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

        /// <summary>Pasta da arte do cenário; garante que existe (os sprites são desenhados na primeira vez).</summary>
        internal static void EnsureFolder()
        {
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Desenha os sprites de enfeite que ainda faltam (só na primeira vez). A colocação na cena agora é feita
        /// pela biblioteca de cenário: Art/Environments/Citadel/manifest.json + FortressDressing + DressingBuilder.
        /// </summary>
        internal static void EnsureSprites()
        {
            TargetSprite(); RackSprite(); BannerSprite();
            WindowSprite(); ShieldSprite(); PlaqueSprite();
        }

        internal static void Dummy(Transform parent, string name, Vector2 position, TrainingDummy.Side side)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatSetup.DummyPrefabPath);
            if (prefab == null) prefab = CombatSetup.CreateDummyPrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            var dummy = go.GetComponent<TrainingDummy>();
            dummy.SetFacing(side);
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.GetComponentInChildren<SpriteRenderer>());
        }

        /// <summary>Placa de lore no chão (E para ler).</summary>
        internal static void Sign(Transform parent, string name, float x, float floorY, DialogueDefinition text)
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

        /// <summary>
        /// As placas de lore da Fortaleza, pela chave (ScriptId). Textos de referência: Docs/Narrativa/dialogos-mvp.md.
        /// Base: SeteVidasDocs/Sete-Vidas_Lore_Oficial_v1.md e Personagens.md §2 (Koda).
        /// </summary>
        internal static System.Collections.Generic.Dictionary<string, DialogueDefinition> CreateLore()
        {
            var (cat, _) = DialogueSetup.Speakers();
            var lore = new System.Collections.Generic.Dictionary<string, DialogueDefinition>
            {
                ["D-PLACA-01"] = DialogueSetup.Conversation("D-PLACA-01", false, 1,
                    (null, "", "Pátio dos Escudeiros. Aqui o filhote vira templário, e o templário, paladino."),
                    (null, "", "Quem não aguenta o boneco de palha não aguenta a praga.")),
                ["D-PLACA-02"] = DialogueSetup.Conversation("D-PLACA-02", false, 2,
                    (null, "", "Alojamento dos Templários. Silêncio depois do toque do sino."),
                    (null, "", "Arranhado embaixo, com letra torta: \"o Koda ronca\"."),
                    (cat, "ironico", "Bom saber.")),
                ["D-PLACA-03"] = DialogueSetup.Conversation("D-PLACA-03", false, 1,
                    (null, "", "Juramento da Ordem: guardar o selo. Servir o reino. Nunca abandonar um ferido."),
                    (cat, "ironico", "Bonito. Pena que eu não sou do reino.")),
                ["D-PLACA-04"] = DialogueSetup.Conversation("D-PLACA-04", false, 2,
                    (null, "", "Câmara do Selo. Há cinco séculos, Golden, o Dourado, prendeu aqui um tomo que bebe vidas."),
                    (null, "", "Que ninguém o desperte. Que nenhum guardião o abandone."),
                    (cat, "", "...Ninguém falou nada de beber vidas.")),
                ["D-PLACA-05"] = DialogueSetup.Conversation("D-PLACA-05", false, 1,
                    (null, "", "Estábulo da Ordem. Aviso de Soluki: só cães passam deste portão."),
                    (cat, "ironico", "Ótimo. Eu nem sou de passar por portões.")),
                ["D-PLACA-06"] = DialogueSetup.Conversation("D-PLACA-06", false, 1,
                    (null, "", "Diário do Guardião do Selo: o tomo voltou a sussurrar à noite. Pedi reforços."),
                    (null, "", "A resposta de Soluki chegou hoje. Uma palavra: \"Superstição.\""),
                    (cat, "", "...O cão de lata avisou. Ninguém ouviu.")),
                ["D-PLACA-07"] = DialogueSetup.Conversation("D-PLACA-07", false, 1,
                    (null, "", "Mural da Marcha Dourada: quarenta dias subindo os salões de Aetherion."),
                    (null, "", "À frente, Golden, o Dourado. No fim, entre os escombros, um livro que sussurrava.")),
                ["D-PLACA-08"] = DialogueSetup.Conversation("D-PLACA-08", false, 1,
                    (null, "", "Pedra antiga dos votos: \"Guardar o selo. Servir os vivos. Nunca abandonar um ferido.\""),
                    (null, "", "Alguém raspou \"os vivos\" e gravou \"o reino\" por cima."),
                    (cat, "ironico", "Engraçado. Os cães também têm letra miúda.")),
                ["D-PLACA-09"] = DialogueSetup.Conversation("D-PLACA-09", false, 1,
                    (null, "", "Guardiões do Selo: Golden, o Dourado. Depois dele, cinco séculos de Dourados."),
                    (null, "", "O último nome da lista ainda tem a tinta fresca: Koda.")),
                ["D-PLACA-10"] = DialogueSetup.Conversation("D-PLACA-10", false, 1,
                    (null, "", "Marco da estrada: Cidadela do Sol Eterno, adiante. Miaravéu, três dias para trás."),
                    (cat, "", "Três dias de volta. Com o livro, dois.")),
                // Santuários de Sethra (melhorias da Fortaleza, item 0): os altares felinos são mais antigos que a Cidadela
                ["D-PLACA-11"] = DialogueSetup.Conversation("D-PLACA-11", false, 1,
                    (null, "", "Altar mais velho que estas muralhas. Na pedra: \"Sethra guarda as sete luzes.\""),
                    (null, "", "Um bilhete na cesta: \"Frutas frescas toda lua. Não contem ao capitão. — B.\""),
                    (cat, "ironico", "Um cão rezando para uma gata. Agora eu já vi de tudo.")),
                ["D-PLACA-12"] = DialogueSetup.Conversation("D-PLACA-12", false, 1,
                    (null, "", "A torre foi erguida sobre este altar. A pedra é de antes da Ordem, de antes dos cães."),
                    (null, "", "Na borda: \"Quem dorme aos pés de Sethra acorda com a centelha acesa.\""),
                    (cat, "", "...Então é por isso que eu continuo voltando.")),
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

        // A tocha de parede saiu daqui: agora é gerada por Tools/EnvironmentSprites (PropSpriteGen), com 4 quadros
        // de chama, em Art/Environments/Common (id env_com_obj_wall_torch).

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
