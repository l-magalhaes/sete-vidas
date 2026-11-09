using System.IO;
using SeteVidas.Core;
using SeteVidas.World.Traps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Armadilhas: sprites, prefabs (Prefabs/Traps) e o circuito K da Playground, à esquerda do spawn.
    /// Spec: Docs/Design/armadilhas.md.
    /// </summary>
    public static class TrapsSetup
    {
        const string Art = "Assets/_Game/Art/Environments/Traps";
        const string Prefabs = "Assets/_Game/Prefabs/Traps";

        [MenuItem("Tools/Sete Vidas/11. Armadilhas (circuito à esquerda do início)", priority = 47)]
        public static void RunTrapsSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PlayerSetup.CreatePlayerAssets(); // gato com o "último chão seguro"
            CreatePrefabs();
            SeteVidasSetup.CreateScenes();
            Debug.Log("[Sete Vidas] Armadilhas prontas: do início da Playground, ande para a ESQUERDA (x -8 a -74).");
        }

        // ------------------------------------------------------------- cena

        /// <summary>Circuito K (x -74..-8). Os tiles (pilar e plataformas) ficam em SeteVidasSetup.CreatePlayground.</summary>
        public static void PlaceInPlayground(Scene scene)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/SpikeStrip.prefab") == null) CreatePrefabs();
            var parent = new GameObject("TrapCourse");
            SceneManager.MoveGameObjectToScene(parent, scene);
            var t = parent.transform;

            Sign(t, -7f, 1.6f, "← Circuito de armadilhas");

            // A) Espinhos: pular por cima; tocar volta ao último chão seguro
            Spikes(t, -11f, 3);
            Sign(t, -11f, 2.4f, "espinhos: pule");

            // B) Fio de alarme na altura do peito + flechas + passagem baixa: agachar resolve tudo
            var launcherB = Instance<ArrowLauncher>(t, "ArrowLauncher", "Lançador (fio)", new Vector2(-20.6f, 1.2f));
            launcherB.Configure(+1, 0f);
            Rec(launcherB);
            var wire = Instance<TrapTrigger>(t, "Tripwire", "Fio de alarme", new Vector2(-17f, 0f));
            wire.Configure(TrapTrigger.Kind.Tripwire, 1.6f, new TrapEffect[] { launcherB });
            Rec(wire);
            Sign(t, -17f, 2.6f, "fio: passe agachado");

            // C) Placa de pressão + lanças do chão: andar agachado não dispara
            var spears = Instance<SpearTrap>(t, "SpearTrap", "Lanças", new Vector2(-27f, 0f));
            var plate = Instance<TrapTrigger>(t, "PressurePlate", "Placa de pressão", new Vector2(-26f, 0f));
            plate.Configure(TrapTrigger.Kind.PressurePlate, 2f, new TrapEffect[] { spears });
            Rec(plate);
            Sign(t, -26.5f, 2.6f, "placa: passos leves (agachado)");

            // D) Chão de fogo em ondas: atravessar pelas plataformas (topo em 3) ou no tempo certo
            for (int i = 0; i < 8; i++)
            {
                var jet = Instance<FlameJet>(t, "FlameJet", $"Jato de chama {i + 1}", new Vector2(-30.5f - 2f * i, 0f));
                jet.Configure(i * 0.35f, 2f);
                Rec(jet);
            }
            Sign(t, -38f, 4.6f, "fogo: use as plataformas");

            // E) Serra no trilho: pular por cima ou esperar na plataforma
            var saw = Instance<SawBlade>(t, "SawBlade", "Serra", new Vector2(-49f, 0.55f));
            saw.Configure(-58f, -49f);
            Rec(saw);

            // F) Plataformas que desmoronam sobre um poço de espinhos
            //    (começa em x -62,5: entre a serra e o poço sobra chão seguro para voltar depois de um espinho)
            Spikes(t, -67f, 9);
            Instance<CrumblingPlatform>(t, "CrumblingPlatform", "Plataforma frágil 1", new Vector2(-64f, 2.75f));
            Instance<CrumblingPlatform>(t, "CrumblingPlatform", "Plataforma frágil 2", new Vector2(-68f, 2.75f));
            Sign(t, -66f, 4.4f, "frágeis: não pare");
            Sign(t, -73.5f, 1.6f, "Fim do circuito");
        }

        internal static void Rec(Component c) => PrefabUtility.RecordPrefabInstancePropertyModifications(c);

        internal static void Spikes(Transform parent, float x, int width)
        {
            var s = Instance<SpikeStrip>(parent, "SpikeStrip", $"Espinhos ({width})", new Vector2(x, 0f));
            s.SetWidth(width);
            var sr = s.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            PrefabUtility.RecordPrefabInstancePropertyModifications(s);
        }

        internal static T Instance<T>(Transform parent, string prefabName, string name, Vector2 position) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{prefabName}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = position;
            return go.GetComponent<T>();
        }

        internal static void Sign(Transform parent, float x, float y, string text)
        {
            var go = new GameObject("Placa: " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            SeteVidas.Combat.DamagePopup.CreateText(go, text, new Color32(0xff, 0xe9, 0xa8, 0xc0), 22);
        }

        // ------------------------------------------------------------ prefabs

        public static void CreatePrefabs()
        {
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();

            // Espinhos (desenho repetido na largura)
            var spikes = NewRoot("SpikeStrip", GameConstants.Layers.Hazard);
            var ssr = Child(spikes, "Visual", new Vector3(0f, 0.25f, 0f)).AddComponent<SpriteRenderer>();
            ssr.sprite = SpikeSprite();
            ssr.drawMode = SpriteDrawMode.Tiled;
            ssr.size = new Vector2(3f, 0.5f);
            ssr.sortingOrder = 1;
            var strip = spikes.AddComponent<SpikeStrip>();
            Set(strip, "spriteRenderer", ssr);
            SetBool(strip, "returnToSafeGround", true);
            Save(spikes, "SpikeStrip");

            // Flecha e lançador
            var arrow = NewRoot("TrapArrow", null);
            Child(arrow, "Visual", Vector3.zero).AddComponent<SpriteRenderer>().sprite = ArrowSprite();
            arrow.GetComponentInChildren<SpriteRenderer>().sortingOrder = 6;
            arrow.AddComponent<TrapArrow>();
            var arrowPrefab = Save(arrow, "TrapArrow").GetComponent<TrapArrow>();

            var launcher = NewRoot("ArrowLauncher", GameConstants.Layers.Hazard);
            var face = Child(launcher, "Visual", Vector3.zero).AddComponent<SpriteRenderer>();
            face.sprite = LauncherSprite();
            face.sortingOrder = 2;
            var glint = Child(launcher, "Aviso", new Vector3(0.25f, 0f, 0f)).AddComponent<SpriteRenderer>();
            glint.sprite = GlintSprite();
            glint.sortingOrder = 3;
            glint.enabled = false;
            var al = launcher.AddComponent<ArrowLauncher>();
            Set(al, "arrowPrefab", arrowPrefab);
            Set(al, "warning", glint);
            Save(launcher, "ArrowLauncher");

            // Fio de alarme: dois postes, fio esticado (armado) e pontas soltas (disparado)
            var wire = NewRoot("Tripwire", null);
            var post = PostSprite();
            Child(wire, "Poste E", new Vector3(-0.85f, 0.6f, 0f)).AddComponent<SpriteRenderer>().sprite = post;
            Child(wire, "Poste D", new Vector3(0.85f, 0.6f, 0f)).AddComponent<SpriteRenderer>().sprite = post;
            var line = Child(wire, "Fio", new Vector3(0f, 1.1f, 0f)).AddComponent<SpriteRenderer>();
            line.sprite = WireSprite();
            line.drawMode = SpriteDrawMode.Tiled;
            line.size = new Vector2(1.7f, 1f / 16f);
            line.sortingOrder = 6; // na frente do gato: dá para ver onde ele esbarra
            var broken = Child(wire, "Fio solto", new Vector3(0f, 0.9f, 0f)).AddComponent<SpriteRenderer>();
            broken.sprite = BrokenWireSprite();
            broken.sortingOrder = 6;
            var tw = wire.AddComponent<TrapTrigger>();
            Set(tw, "armedVisual", line);
            Set(tw, "firedVisual", broken);
            Save(wire, "Tripwire");

            // Placa de pressão (2 tiles)
            var plate = NewRoot("PressurePlate", null);
            var up = Child(plate, "Solta", new Vector3(0f, 2f / 16f, 0f)).AddComponent<SpriteRenderer>();
            up.sprite = PlateSprite(pressed: false);
            up.drawMode = SpriteDrawMode.Tiled;
            up.size = new Vector2(2f, 4f / 16f);
            up.sortingOrder = 1;
            var down = Child(plate, "Afundada", new Vector3(0f, 1f / 16f, 0f)).AddComponent<SpriteRenderer>();
            down.sprite = PlateSprite(pressed: true);
            down.drawMode = SpriteDrawMode.Tiled;
            down.size = new Vector2(2f, 2f / 16f);
            down.sortingOrder = 1;
            var pt = plate.AddComponent<TrapTrigger>();
            SetEnum(pt, "kind", (int)TrapTrigger.Kind.PressurePlate);
            Set(pt, "armedVisual", up);
            Set(pt, "firedVisual", down);
            Save(plate, "PressurePlate");

            // Lanças do chão (4 tiles)
            var spear = NewRoot("SpearTrap", GameConstants.Layers.Hazard);
            var sp = Child(spear, "Lanças", Vector3.zero).AddComponent<SpriteRenderer>();
            sp.sprite = SpearSprite();
            sp.drawMode = SpriteDrawMode.Tiled;
            sp.size = new Vector2(4f, 1.6f);
            sp.sortingOrder = -1; // atrás do chão: saem de dentro dele
            sp.enabled = false;
            var st = spear.AddComponent<SpearTrap>();
            Set(st, "spears", sp);
            SetInt(st, "width", 4);
            Save(spear, "SpearTrap");

            // Jato de chama (bocal sempre visível; chama e fagulhas ligam no ciclo)
            var jet = NewRoot("FlameJet", GameConstants.Layers.Hazard);
            var nozzle = Child(jet, "Bocal", new Vector3(0f, 2f / 16f, 0f)).AddComponent<SpriteRenderer>();
            nozzle.sprite = NozzleSprite();
            nozzle.drawMode = SpriteDrawMode.Tiled;
            nozzle.size = new Vector2(2f, 4f / 16f);
            nozzle.sortingOrder = 1;
            var flame = Child(jet, "Chama", Vector3.zero).AddComponent<SpriteRenderer>();
            flame.sprite = FlameSprite();
            flame.sortingOrder = 7;
            flame.enabled = false;
            var sparks = Child(jet, "Fagulhas", new Vector3(0f, 0.3f, 0f)).AddComponent<SpriteRenderer>();
            sparks.sprite = SparksSprite();
            sparks.sortingOrder = 7;
            sparks.enabled = false;
            var fj = jet.AddComponent<FlameJet>();
            Set(fj, "flame", flame);
            Set(fj, "sparks", sparks);
            SetFloat(fj, "flameWidth", 1.9f);
            Save(jet, "FlameJet");

            // Serra (girando) — o trilho é desenhado pela própria serra no editor de cena
            var saw = NewRoot("SawBlade", GameConstants.Layers.Hazard);
            var blade = Child(saw, "Lâmina", Vector3.zero);
            var bsr = blade.AddComponent<SpriteRenderer>();
            bsr.sprite = SawSprite();
            bsr.sortingOrder = 6;
            var sb = saw.AddComponent<SawBlade>();
            Set(sb, "blade", blade.transform);
            Save(saw, "SawBlade");

            // Plataforma que desmorona (2 × 0,5 tiles)
            var crumble = NewRoot("CrumblingPlatform", GameConstants.Layers.Ground);
            var box = crumble.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 0.5f);
            var csr = Child(crumble, "Visual", Vector3.zero).AddComponent<SpriteRenderer>();
            var intact = CrumbleSprite(cracked: false);
            csr.sprite = intact;
            csr.sortingOrder = 1;
            var cp = crumble.AddComponent<CrumblingPlatform>();
            Set(cp, "spriteRenderer", csr);
            Set(cp, "intact", intact);
            Set(cp, "cracked", CrumbleSprite(cracked: true));
            Save(crumble, "CrumblingPlatform");
        }

        static GameObject NewRoot(string name, string layer)
        {
            var go = new GameObject(name);
            if (layer != null) go.layer = LayerMask.NameToLayer(layer);
            return go;
        }

        static GameObject Child(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name) { layer = parent.layer };
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static GameObject Save(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Prefabs}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ sprites

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
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); // PixelArtImportPostprocessor (pivot central)
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static readonly Color32 Steel = new Color32(0xc0, 0xc6, 0xd2, 0xff);
        static readonly Color32 SteelDark = new Color32(0x5c, 0x62, 0x70, 0xff);
        static readonly Color32 Ink = new Color32(0x1c, 0x1a, 0x24, 0xff);
        static readonly Color32 Stone = new Color32(0x5a, 0x55, 0x6e, 0xff);
        static readonly Color32 StoneEdge = new Color32(0x3a, 0x36, 0x4a, 0xff);
        static readonly Color32 Danger = new Color32(0xc8, 0x3a, 0x3a, 0xff);

        /// <summary>Espinhos 16×8: dois triângulos de aço com ponta clara e base escura (pontas vermelhas = perigo).</summary>
        static Sprite SpikeSprite() => Draw("trap_spikes", 16, 8, set =>
        {
            foreach (int cx in new[] { 4, 12 })
                for (int y = 0; y < 8; y++)
                {
                    int half = (7 - y) / 2;
                    for (int x = cx - half; x <= cx + half; x++) set(x, y, x == cx - half || x == cx + half ? SteelDark : Steel);
                }
            set(4, 7, Danger); set(12, 7, Danger);
            for (int x = 0; x < 16; x++) set(x, 0, Ink);
        });

        static Sprite ArrowSprite() => Draw("trap_arrow", 10, 3, set =>
        {
            for (int x = 0; x < 8; x++) set(x, 1, new Color32(0x8a, 0x6a, 0x4a, 0xff));
            set(8, 1, Steel); set(9, 1, Steel); set(8, 0, Steel); set(8, 2, Steel); // ponta (para a direita)
            set(0, 0, Danger); set(0, 2, Danger); set(1, 0, Danger); set(1, 2, Danger); // penas
        });

        static Sprite LauncherSprite() => Draw("trap_launcher", 8, 8, set =>
        {
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) set(x, y, x == 0 || y == 0 || x == 7 || y == 7 ? StoneEdge : Stone);
            for (int y = 3; y <= 4; y++) for (int x = 4; x <= 7; x++) set(x, y, Ink); // boca
        });

        static Sprite GlintSprite() => Draw("trap_launcher_glint", 4, 4, set =>
        {
            set(1, 1, Danger); set(2, 2, Danger); set(1, 2, new Color32(0xff, 0x9a, 0x6a, 0xff)); set(2, 1, Danger);
        });

        static Sprite PostSprite() => Draw("trap_wire_post", 3, 20, set =>
        {
            for (int y = 0; y < 20; y++) { set(0, y, Ink); set(1, y, new Color32(0x6b, 0x4a, 0x2b, 0xff)); set(2, y, Ink); }
        });

        static Sprite WireSprite() => Draw("trap_wire", 16, 1, set =>
        {
            for (int x = 0; x < 16; x++) set(x, 0, x % 4 == 0 ? Danger : new Color32(0xd8, 0xd0, 0xb8, 0xff));
        });

        static Sprite BrokenWireSprite() => Draw("trap_wire_broken", 28, 8, set =>
        {
            var c = new Color32(0xd8, 0xd0, 0xb8, 0xff);
            for (int i = 0; i < 6; i++) { set(1 + i / 2, 7 - i, c); set(26 - i / 2, 7 - i, c); }
        });

        static Sprite PlateSprite(bool pressed) => Draw(pressed ? "trap_plate_pressed" : "trap_plate", 16, pressed ? 2 : 4, set =>
        {
            int h = pressed ? 2 : 4;
            for (int y = 0; y < h; y++) for (int x = 0; x < 16; x++)
                set(x, y, y == h - 1 || x == 0 || x == 15 ? StoneEdge : new Color32(0x7a, 0x74, 0x8e, 0xff));
            if (!pressed) { set(7, 1, Danger); set(8, 1, Danger); } // marquinha: "não pise"
        });

        static Sprite SpearSprite() => Draw("trap_spears", 16, 26, set =>
        {
            foreach (int cx in new[] { 3, 8, 13 })
            {
                for (int y = 0; y < 20; y++) set(cx, y, new Color32(0x6b, 0x4a, 0x2b, 0xff));
                for (int y = 20; y < 26; y++) { int half = (25 - y) / 3; for (int x = cx - half; x <= cx + half; x++) set(x, y, Steel); }
                set(cx, 25, Danger);
            }
        });

        static Sprite NozzleSprite() => Draw("trap_flame_nozzle", 16, 4, set =>
        {
            for (int y = 0; y < 4; y++) for (int x = 0; x < 16; x++) set(x, y, (x % 3 == 0 || y == 3) ? Ink : SteelDark);
        });

        /// <summary>Chama 16×32 (1 × 2 tiles, esticada até a altura do jato): branco-amarelo no centro, laranja e vermelho nas bordas.</summary>
        static Sprite FlameSprite() => Draw("trap_flame", 16, 32, set =>
        {
            for (int y = 0; y < 32; y++)
            {
                float k = y / 31f;
                int half = Mathf.RoundToInt(Mathf.Lerp(7f, 1f, k * k));
                for (int x = 8 - half; x < 8 + half; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - 8f) / Mathf.Max(1, half);
                    Color32 c = d < 0.35f ? new Color32(0xff, 0xf2, 0xb0, 0xff) : d < 0.7f ? new Color32(0xff, 0xa8, 0x3a, 0xff) : new Color32(0xd8, 0x40, 0x2a, 0xe0);
                    set(x, y, c);
                }
            }
        });

        static Sprite SparksSprite() => Draw("trap_flame_sparks", 16, 6, set =>
        {
            foreach (var (x, y) in new[] { (3, 1), (7, 3), (11, 0), (13, 4), (5, 5), (9, 1) }) set(x, y, new Color32(0xff, 0xc8, 0x4a, 0xff));
        });

        /// <summary>Serra 16×16: disco de aço com dentes e miolo escuro.</summary>
        static Sprite SawSprite() => Draw("trap_saw", 16, 16, set =>
        {
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                bool tooth = d <= 7.8f && d > 6f && Mathf.Repeat(a * 8f / Mathf.PI, 2f) < 1f;
                if (d <= 6f) set(x, y, d < 2f ? Ink : d < 5f ? Steel : SteelDark);
                else if (tooth) set(x, y, Steel);
            }
        });

        static Sprite CrumbleSprite(bool cracked) => Draw(cracked ? "trap_crumble_cracked" : "trap_crumble", 32, 8, set =>
        {
            for (int y = 0; y < 8; y++) for (int x = 0; x < 32; x++)
                set(x, y, x == 0 || x == 31 || y == 0 || y == 7 || x == 15 ? StoneEdge : new Color32(0x6e, 0x66, 0x5a, 0xff));
            int[] crack = cracked ? new[] { 5, 9, 20, 26 } : new[] { 22 };
            foreach (int cx in crack) for (int y = 1; y < 7; y++) set(cx + (y % 2), y, Ink); // rachaduras
        });
    }
}
