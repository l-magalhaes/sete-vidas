using System.Collections.Generic;
using System.IO;
using SeteVidas.Dialogue;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F2 da Floresta Silenciosa (Docs/Floresta/plano-implementacao.md): monta o graybox das 7 cenas (estrada + regiões
    /// A–F, 39 salas) a partir dos dados de <see cref="ForestLayout"/>. Inimigos são placeholders (Sentinela no lugar dos
    /// infectados e das serpentes); a arte é a provisória da F1. Antes de montar, confere o alcance com
    /// <see cref="ForestReach"/> (baús, saídas e softlock) e mostra o relatório no console.
    /// Mudou o mapa? Edite o ForestLayout (não a cena: ela é recriada) e rode este menu de novo.
    /// </summary>
    public static class ForestGrayboxSetup
    {
        const string Folder = ForestTestSetup.Folder;

        [MenuItem("Tools/Sete Vidas/21. Floresta: graybox (todas as cenas)", priority = 71)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SeteVidasSetup.ConfigureLayers();
            PlayerSetup.CreatePlayerAssets();
            ProgressionSetup.CreateHudPrefab();
            ProgressionSetup.CreateSanctuaryPrefab();
            ClassSetup.CreateWallPrefab();
            EnemySetup.CreateSentinelPrefab();
            EnemySetup.CreateCrossbowSentinelPrefab();
            EnemySetup.CreateBushPrefab();
            TrapsSetup.CreatePrefabs();
            Directory.CreateDirectory(Folder);
            ForestTestSetup.EnsureSprites();

            var reports = ForestReach.CheckAll();
            string text = ForestReach.Format(reports, false);
            bool ok = true;
            foreach (var r in reports) if (r.Problems.Count > 0) ok = false;
            if (ok) Debug.Log("[Sete Vidas] Floresta F2: alcance conferido.\n" + text);
            else Debug.LogWarning("[Sete Vidas] Floresta F2: o alcance tem problemas (a cena é gerada assim mesmo).\n" + text);

            foreach (var region in ForestLayout.Regions()) BuildRegion(region);
            // a casa da Vulpis (a cabana da Forest_Test_C): interior com a porta de volta para o Refúgio (D3)
            ForestTestSetup.BuildCabin(ForestLayout.Cabin, ForestLayout.D, "W16", "gb_cabana_moeda");
            SeteVidasSetup.RegisterScenesInBuild();
            EditorSceneManager.OpenScene($"{Folder}/{ForestLayout.A}.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Sete Vidas] Floresta: 8 cenas geradas (Forest_Road, Forest_A..F e a casa da Vulpis). Comece pela Forest_A_Roots " +
                      "(desligue \"Play começa na tela inicial\" para dar Play direto nela). Roteiro: Docs/QA/floresta-rev4-plano-de-teste.md");
        }

        [MenuItem("Tools/Sete Vidas/21b. Floresta F2: conferir alcance (sem gerar)", priority = 72)]
        public static void CheckOnly()
        {
            Debug.Log("[Sete Vidas] Floresta F2: alcance do graybox\n" + ForestReach.Format(ForestReach.CheckAll(), true));
        }

        static void BuildRegion(ForestLayout.Region reg)
        {
            uint c = reg.Sky;
            var sky = new Color32((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), 0xff);
            Tint = reg.Scene == ForestLayout.Road ? PassageLight.Gold : PassageLight.Forest;
            var ctx = ForestTestSetup.Begin(reg.Scene, new Vector2(reg.SpawnX, reg.SpawnY), sky);
            ctx.Root.name = reg.Label + " (graybox F2)";

            foreach (var t in reg.Tiles())
            {
                Tilemap map = t.Map == ForestLayout.Map.Climbable ? ctx.Vines : t.Map == ForestLayout.Map.Background ? ctx.Back : ctx.Ground;
                if (t.Clear) SeteVidasSetup.Clear(map, t.X0, t.Y0, t.X1, t.Y1);
                else ForestTestSetup.Fill(map, t.X0, t.Y0, t.X1, t.Y1);
            }

            var rooms = new GameObject("Salas (mapa)").transform;
            rooms.SetParent(ctx.Root, false);
            foreach (var r in reg.Rooms)
                ForestTestSetup.Room(rooms, r.Id, r.Name, new Vector2(r.X + r.W * 0.5f, r.Y + r.H * 0.5f), new Vector2(r.W, r.H), Vector2.zero, reg.MapRegion);

            var dialogues = new Dictionary<string, DialogueDefinition>();
            var created = new Dictionary<string, Component>();
            var groups = new Dictionary<string, Transform>();
            var infected = new List<SeteVidas.Enemies.Zombie.ZombieSquad.Post>();
            var horde = new List<SeteVidas.Enemies.Zombie.ZombieSquad.Post>();
            var bridges = new List<ForestLayout.Obj>();
            var shortcuts = new Dictionary<string, List<ForestLayout.Obj>>();
            var arenas = new List<ForestLayout.Obj>();
            var arenaGates = new Dictionary<string, List<ArenaGate>>();
            var waves = new Dictionary<string, List<GameObject>>();
            foreach (var o in reg.Objects)
            {
                string room = o.Name.Contains(":") ? o.Name.Substring(0, o.Name.IndexOf(':')) : "geral";
                if (!groups.TryGetValue(room, out var parent))
                {
                    parent = new GameObject(room).transform;
                    parent.SetParent(ctx.Root, false);
                    groups[room] = parent;
                }
                switch (o.Kind)
                {
                    case "infected":
                        infected.Add(new SeteVidas.Enemies.Zombie.ZombieSquad.Post { name = o.Name, at = new Vector2(o.X, o.Y + 0.05f), facing = o.Facing, minX = o.A, maxX = o.B, shield = o.Value > 0 });
                        break;
                    case "horde":
                        horde.Add(new SeteVidas.Enemies.Zombie.ZombieSquad.Post { name = o.Name, at = new Vector2(o.X, o.Y + 0.05f), facing = 1, minX = o.X, maxX = o.X });
                        break;
                    case "bridge": bridges.Add(o); break;
                    case "sc_lever": case "sc_vine": case "sc_block": case "sc_rolled":
                        if (!shortcuts.TryGetValue(o.Flag, out var list)) shortcuts[o.Flag] = list = new List<ForestLayout.Obj>();
                        list.Add(o);
                        break;
                    case "arena": arenas.Add(o); break;
                    case "arena_gate":
                    {
                        var gate = ForestTestSetup.Gate(parent, o.Name, o.X, o.Y);
                        if (!arenaGates.TryGetValue(o.Link, out var gl)) arenaGates[o.Link] = gl = new List<ArenaGate>();
                        gl.Add(gate);
                        break;
                    }
                    default:
                    {
                        int before = parent.childCount;
                        Place(parent, o, dialogues, created);
                        // segunda leva da arena: o objeto que acabou de ser criado fica desligado até a hora
                        if (!string.IsNullOrEmpty(o.Link) && o.Link.StartsWith("onda:") && parent.childCount > before)
                        {
                            string id = o.Link.Substring(5);
                            if (!waves.TryGetValue(id, out var wl)) waves[id] = wl = new List<GameObject>();
                            for (int i = before; i < parent.childCount; i++)
                                if (!parent.GetChild(i).name.Contains("(morcego modelo)")) wl.Add(parent.GetChild(i).gameObject);
                        }
                        break;
                    }
                }
            }
            SeteVidas.Enemies.Zombie.ZombieSquad hordeSquad = null;
            if (infected.Count > 0) Squad(ctx.Root, reg.Scene == ForestLayout.Road ? "Infectados" : "Templários zumbis", infected, true);
            if (horde.Count > 0) hordeSquad = Squad(ctx.Root, "Horda da ponte", horde, false);
            foreach (var b in bridges) Bridge(ctx.Root, b, hordeSquad);
            foreach (var kv in shortcuts) Shortcut(ctx.Root, kv.Key, kv.Value);
            foreach (var a in arenas)
            {
                var go = new GameObject("Arena (" + a.Id + ")");
                go.transform.SetParent(ctx.Root, false);
                go.transform.position = new Vector3(a.X, a.Y, 0f);
                var arena = go.AddComponent<ArenaRoom>();
                arenaGates.TryGetValue(a.Id, out var gates);
                arena.Configure(a.Id, new Vector2(a.W, a.H), gates != null ? gates.ToArray() : new ArenaGate[0], null);
                if (waves.TryGetValue(a.Id, out var w2)) arena.SetWaves(w2.ToArray());
                EditorUtility.SetDirty(arena);
            }
            ForestTestSetup.End(ctx);
        }

        // ------------------------------------------------------------ atalhos de verdade (cipó, alçapão, portão)

        /// <summary>
        /// Um atalho numa cena (pedido do usuário, 13/11/2026: "tipo Dark Souls"): a tampa/portão sólido enquanto fechado,
        /// o cipó escalável (layer Climbable) quando aberto e o cipó enrolado como pista. O <see cref="ShortcutLink"/> fica
        /// onde se abre (ou, do lado fechado, onde o gato comenta). Mesma flag nos dois lados, às vezes em cenas diferentes.
        /// </summary>
        static void Shortcut(Transform parent, string flag, List<ForestLayout.Obj> parts)
        {
            var opener = parts.Find(p => p.Kind == "sc_lever" && p.Opens);
            var marker = parts.Find(p => p.Kind == "sc_lever" && !p.Opens);
            var root = new GameObject("Atalho " + flag);
            root.transform.SetParent(parent, false);
            var at = opener ?? marker ?? parts[0];
            root.transform.position = new Vector3(at.X, at.Y, 0f);

            var closed = new List<GameObject>();
            var open = new List<GameObject>();
            var lid = Placeholder("tampa_raizes", 16, 16, (x, y) => y == 15 || y == 0 ? new Color32(0x34, 0x26, 0x1e, 0xff)
                : (x + y * 2) % 7 == 0 ? new Color32(0x82, 0x3e, 0x2e, 0xff) : new Color32(0x52, 0x36, 0x24, 0xff));
            var vine = Placeholder("cipo_atalho", 16, 16, (x, y) => x >= 5 && x <= 10 ? ((x + y) % 4 == 0 ? new Color32(0x6c, 0x86, 0x60, 0xff) : new Color32(0x4a, 0x68, 0x46, 0xff))
                : (x == 4 || x == 11) && y % 5 == 2 ? new Color32(0x2e, 0x48, 0x34, 0xff) : new Color32(0, 0, 0, 0));
            var rolled = Placeholder("cipo_enrolado", 16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                return d > 7f ? new Color32(0, 0, 0, 0) : ((int)d % 3 == 0 ? new Color32(0x2e, 0x48, 0x34, 0xff) : new Color32(0x4a, 0x68, 0x46, 0xff));
            });
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case "sc_block":
                    {
                        var go = new GameObject(p.Text ?? "tampa") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Ground) };
                        go.transform.SetParent(root.transform, false);
                        go.transform.position = new Vector3(p.X + p.W * 0.5f, p.Y + p.H * 0.5f, 0f);
                        go.AddComponent<BoxCollider2D>().size = new Vector2(p.W, p.H);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = lid; sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(p.W, p.H); sr.sortingOrder = 2;
                        closed.Add(go);
                        break;
                    }
                    case "sc_vine":
                    {
                        var go = new GameObject("Cipó (aberto)") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Climbable) };
                        go.transform.SetParent(root.transform, false);
                        go.transform.position = new Vector3(p.X + 0.5f, p.Y + p.H * 0.5f, 0f);
                        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, p.H);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = vine; sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(1f, p.H); sr.sortingOrder = 2;
                        go.SetActive(false);
                        open.Add(go);
                        break;
                    }
                    case "sc_rolled":
                    {
                        var go = new GameObject("Cipó enrolado");
                        go.transform.SetParent(root.transform, false);
                        go.transform.position = new Vector3(p.X, p.Y, 0f);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = rolled; sr.sortingOrder = 2;
                        closed.Add(go);
                        break;
                    }
                }
            }

            TextMesh prompt = null;
            if (opener != null)
            {
                var promptGo = new GameObject("Prompt");
                promptGo.transform.SetParent(root.transform, false);
                promptGo.transform.localPosition = new Vector3(0f, 2.4f, 0f);
                prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: " + opener.Text, new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
                promptGo.SetActive(false);
                PassageLight.Glow(root.transform, "Clarão do atalho", opener.X, opener.Y - 1f, Tint);
            }
            var link = root.AddComponent<ShortcutLink>();
            link.Configure(flag, opener != null, opener != null ? new Vector2(opener.A, opener.B) : Vector2.up, closed.ToArray(), open.ToArray(),
                opener != null ? opener.Text : "", marker != null ? marker.Text : "Preso do outro lado.");
            var so = new SerializedObject(link);
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();

            // o lado fechado na mesma cena (cipó que desce dentro da mesma sala): um segundo ponto, só para o comentário
            if (opener != null && marker != null)
            {
                var m = new GameObject("Atalho " + flag + " (lado fechado)");
                m.transform.SetParent(parent, false);
                m.transform.position = new Vector3(marker.X, marker.Y, 0f);
                m.AddComponent<ShortcutLink>().Configure(flag, false, Vector2.up, new GameObject[0], new GameObject[0], "", marker.Text);
            }
        }

        // ------------------------------------------------------------ Serpente (F4, placeholder)

        static void Snake(Transform parent, ForestLayout.Obj o)
        {
            bool crawl = o.Value > 0;
            var go = new GameObject(o.Name) { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            if (!crawl)
            {
                var hole = new GameObject("Buraco").AddComponent<SpriteRenderer>();
                hole.transform.SetParent(go.transform, false);
                hole.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                hole.sprite = Placeholder("serpente_buraco", 24, 6, (x, y) =>
                {
                    float dx = (x - 11.5f) / 12f, dy = (y - 0f) / 6f;
                    return dx * dx + dy * dy < 1f ? new Color32(0x0e, 0x0c, 0x10, 0xff) : new Color32(0, 0, 0, 0);
                });
                hole.sortingOrder = 1;
            }
            var body = new GameObject("Corpo") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Enemy) };
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0.4f * o.Facing, 0.3f, 0f);
            var col = body.AddComponent<BoxCollider2D>();
            col.isTrigger = true;               // os golpes do gato acham o inimigo pelo gatilho (layer Enemy)
            col.size = new Vector2(1.4f, 0.8f);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            Color32 green = new Color32(0x4a, 0x68, 0x46, 0xff), light = new Color32(0x9a, 0xc0, 0x5a, 0xff), dark = new Color32(0x2e, 0x48, 0x34, 0xff);
            Color32 eye = new Color32(0xff, 0xd3, 0x4d, 0xff), clear = new Color32(0, 0, 0, 0);
            var hidden = Placeholder("serpente_olhos", 32, 16, (x, y) => y == 4 && (x == 13 || x == 18) ? eye : clear);
            var warn = Placeholder("serpente_aviso", 32, 16, (x, y) =>
                y >= 2 && y <= 8 && x >= 10 && x <= 21 ? ((x + y) % 3 == 0 ? dark : green) : y == 9 && (x == 13 || x == 18) ? eye : clear);
            var strike = Placeholder("serpente_bote", 32, 16, (x, y) =>
            {
                int mid = 6 + (int)(2f * Mathf.Sin(x * 0.5f));
                if (Mathf.Abs(y - mid) <= 1) return x > 26 && y == mid ? eye : (x % 4 == 0 ? light : green);
                return clear;
            });
            var crawlSprite = Placeholder("serpente", 32, 16, (x, y) =>
            {
                int mid = 3 + (int)(1.5f * Mathf.Sin(x * 0.45f));
                if (Mathf.Abs(y - mid) <= 1 && x >= 2 && x <= 29) return x > 26 && y == mid + 1 ? eye : (x % 5 == 0 ? dark : green);
                return clear;
            });
            sr.sprite = crawl ? crawlSprite : hidden;
            var e = go.AddComponent<SeteVidas.Enemies.ForestSnake>();
            e.Configure(o.Facing, crawl, crawl ? o.A : o.X, crawl ? o.B : o.X);
            var so = new SerializedObject(e);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("body").objectReferenceValue = body.transform;
            so.FindProperty("hiddenSprite").objectReferenceValue = hidden;
            so.FindProperty("warnSprite").objectReferenceValue = warn;
            so.FindProperty("strikeSprite").objectReferenceValue = strike;
            so.FindProperty("crawlSprite").objectReferenceValue = crawlSprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Armadilhas de um grupo (o botão do grupo desliga para sempre).</summary>
        static void Group(Transform t, string group)
        {
            if (string.IsNullOrEmpty(group)) return;
            t.GetChild(t.childCount - 1).gameObject.AddComponent<SeteVidas.World.Traps.TrapGroupMember>().Configure(group);
        }

        // ------------------------------------------------------------ estrada (F3): infectados e a ponte

        static SeteVidas.Enemies.Zombie.ZombieSquad Squad(Transform parent, string name, List<SeteVidas.Enemies.Zombie.ZombieSquad.Post> posts, bool onStart)
        {
            var settings = AssetDatabase.LoadAssetAtPath<SeteVidas.Enemies.Zombie.ZombieSettings>(FortressSetup.ZombieSettingsPath);
            var animations = AssetDatabase.LoadAssetAtPath<SeteVidas.Player.CharacterAnimationSet>(FortressSetup.ZombieAnimationsPath);
            if (settings == null || animations == null)
                Debug.LogWarning("[Sete Vidas] Infectados: falta o ZombieSettings ou a arte do zumbi (rode o menu 13 da Fortaleza e Tools/EnemySprites/gerar.ps1)");
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var squad = go.AddComponent<SeteVidas.Enemies.Zombie.ZombieSquad>();
            squad.Configure(settings, animations, AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FortressSetup.NoFrictionPath),
                Placeholder("escudo", 8, 14, (x, y) => x == 0 || x == 7 || y == 0 || y == 13 ? new Color32(0x3a, 0x36, 0x4a, 0xff)
                    : (x == 3 || x == 4) && y > 3 && y < 10 ? new Color32(0xe0, 0xb0, 0x4a, 0xff) : new Color32(0x9a, 0x9c, 0xa8, 0xff)),
                posts.ToArray(), onStart);
            return squad;
        }

        /// <summary>Trecho da ponte que desaba: blocos de pedra (2 × 1) no lugar do chão, mais o BridgeCollapse.</summary>
        static void Bridge(Transform parent, ForestLayout.Obj o, SeteVidas.Enemies.Zombie.ZombieSquad hordeSquad)
        {
            var root = new GameObject("Ponte de pedra (desaba)");
            root.transform.SetParent(parent, false);
            var stone = Placeholder("ponte_pedra", 32, 16, (x, y) => y == 15 || x % 16 == 0 ? new Color32(0x3a, 0x36, 0x4a, 0xff)
                : (x + y * 3) % 11 == 0 ? new Color32(0x46, 0x42, 0x58, 0xff) : new Color32(0x6e, 0x68, 0x84, 0xff));
            var blocks = new List<Rigidbody2D>();
            int n = Mathf.Max(1, Mathf.RoundToInt(o.W / 2f));
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"Bloco {i + 1}") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Ground) };
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(o.X + i * 2f + 1f, o.Y - 0.5f, 0f);
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                go.AddComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = stone;
                sr.sortingOrder = 1;
                blocks.Add(body);
            }
            root.AddComponent<BridgeCollapse>().Configure(blocks.ToArray(), o.A, o.B, hordeSquad);
        }

        static Sprite Placeholder(string name, int w, int h, System.Func<int, int, Color32> paint)
        {
            string path = $"Assets/_Game/Art/Environments/Forest/Placeholder/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = paint(x, y);
                tex.SetPixels32(px);
                tex.Apply(false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Tom da luz de passagem da cena sendo montada: dourado na estrada, verde-claro na Floresta.</summary>
        static Color Tint = PassageLight.Forest;

        /// <summary>
        /// Luz de passagem (pedido do usuário, 13/11/2026) em cada saída para outra cena: clara do lado de fora, o gato
        /// entra nela ao sair. Portas laterais: 5 tiles de gradiente até a parede. Buracos no teto/chão: na vertical.
        /// Gatilhos no meio da sala (corda da ponte nova): um clarão. A queda da ponte (cutscene) fica sem.
        /// </summary>
        static void ExitLight(Transform t, ForestLayout.Obj o)
        {
            string name = o.Name + " (luz)";
            switch (o.Facing)
            {
                case 1:  PassageLight.Horizontal(t, name, o.X - 5.5f, o.X - 0.5f, o.Y, o.H + 2f, Tint, brightRight: true); break;
                case -1: PassageLight.Horizontal(t, name, o.X + 0.5f, o.X + 5.5f, o.Y, o.H + 2f, Tint, brightRight: false); break;
                case 2:  PassageLight.Vertical(t, name, o.X, o.W + 1f, o.Y - 3.5f, o.Y + 1.5f, Tint, brightUp: true); break;
                case -2: PassageLight.Vertical(t, name, o.X, o.W + 1f, o.Y + 1f, o.Y + 5f, Tint, brightUp: false); break;
                default:
                    if (o.W <= 3f) PassageLight.Glow(t, name, o.X, o.Y - o.H * 0.5f, Tint);
                    break;
            }
        }

        static void Place(Transform t, ForestLayout.Obj o, Dictionary<string, DialogueDefinition> dialogues, Dictionary<string, Component> created)
        {
            int before = t.childCount;
            PlaceOne(t, o, dialogues, created);
            if (!o.Infected) return;
            // F4: variante infectada. Vale para tudo o que o objeto criou (o Broto cria também o morcego modelo)
            for (int i = before; i < t.childCount; i++)
                ForestInfectedArt.Attach(t.GetChild(i).gameObject, InfectedEyes(o.Kind));
        }

        /// <summary>De onde saem os pontinhos verdes de cada inimigo infectado (a partir do objeto).</summary>
        static Vector2 InfectedEyes(string kind)
        {
            switch (kind)
            {
                case "dry":
                case "sprout": return new Vector2(0f, SeteVidas.Enemies.ForestEnemyRules.BranchHeight - 1.6f);
                case "plant": return new Vector2(0f, 0.9f);
                case "snake": return new Vector2(0f, 0.4f);
                default: return Vector2.zero; // morcego e cacto: do centro
            }
        }

        static void PlaceOne(Transform t, ForestLayout.Obj o, Dictionary<string, DialogueDefinition> dialogues, Dictionary<string, Component> created)
        {
            switch (o.Kind)
            {
                case "branch": ForestTestSetup.Branch(t, o.Name, o.X, o.Y, o.W); break;
                case "sway": ForestTestSetup.Swaying(t, o.Name, o.X, o.Y, o.W); break;
                case "leaf": ForestTestSetup.Leaf(t, o.Name, o.X, o.Y); break;
                case "rotten": ForestTestSetup.RottenBranch(t, o.Name, o.X, o.Y); break;
                case "mushroom": ForestTestSetup.Mushroom(t, o.Name, o.X, o.Y); break;
                case "cactus": ForestTestSetup.Cactus(t, o.Name, o.X, o.Y, new Vector2(o.A, o.B), o.C > 0f ? o.C : 3f); break;
                case "trunk": ForestTestSetup.Trunk(t, o.Name, o.X, o.Y, o.Id); break;
                case "chest": ForestTestSetup.Chest(t, o.Name, o.X, o.Y, o.Id, wood: true, coins: o.Value); break;
                case "bat":
                    if (o.A > 0f) ForestTestSetup.Bat(t, o.Name, o.X, o.Y, wakeRange: o.A);
                    else ForestTestSetup.Bat(t, o.Name, o.X, o.Y);
                    break;
                case "dry": ForestTestSetup.DryBranchEnemy(t, o.Name, o.X, o.Y, o.Facing); break;
                case "sprout": ForestTestSetup.DryBranchEnemy(t, o.Name, o.X, o.Y, o.Facing, sprout: true); break;
                case "plant": ForestTestSetup.Plant(t, o.Name, o.X, o.Y, o.A); break;
                case "snake": Snake(t, o); break;
                case "blade": ForestTestSetup.Blade(t, o.Name, o.X, o.Y, o.H, o.A, group: o.Text); break;
                case "gspikes":
                {
                    var s = TrapsSetup.Instance<SeteVidas.World.Traps.SpikeStrip>(t, "SpikeStrip", o.Name, new Vector2(o.X, o.Y));
                    s.SetWidth((int)o.W);
                    TrapsSetup.Rec(s);
                    TrapsSetup.Rec(s.GetComponentInChildren<SpriteRenderer>());
                    Group(t, o.Text);
                    break;
                }
                case "gsaw":
                {
                    var s = TrapsSetup.Instance<SeteVidas.World.Traps.SawBlade>(t, "SawBlade", o.Name, new Vector2(o.X, o.Y));
                    s.Configure(o.A, o.B);
                    TrapsSetup.Rec(s);
                    Group(t, o.Text);
                    TrapsSetup.SawRail(t, o.Name + " (trilho)", o.A, o.B, o.Y);
                    break;
                }
                case "vsaw": ForestTestSetup.Saw(t, o.Name, o.A, o.B, o.X, vertical: true, group: o.Text); break;
                case "switch": ForestTestSetup.Switch(t, o.Name, o.X, o.Y, o.Text); break;
                case "key": ForestTestSetup.KeyItem(t, o.Name, o.X, o.Y, o.Id); break;
                case "residence": ForestTestSetup.Residence(t, o.Name, o.X, o.Y, o.Text, "entrada", o.Link, o.Flag, o.Id); break;
                case "hook": ForestTestSetup.Hook(t, o.Name, o.X, o.Y); break;
                case "torch": PassageLight.Glow(t, o.Name, o.X, o.Y, new Color32(0xff, 0xa8, 0x48, 0xff)); break;
                case "bark":
                {
                    var go = new GameObject(o.Name);
                    go.transform.SetParent(t, false);
                    go.AddComponent<SceneBark>().Configure(o.Id, o.A, (o.Text ?? "").Split('|'));
                    break;
                }
                case "slope": SlopeSetup.Create(t, o.Name, Mathf.RoundToInt(o.X), Mathf.RoundToInt(o.Y), Mathf.RoundToInt(o.W), Mathf.RoundToInt(o.H), o.Facing > 0); break;
                case "entrance": ForestTestSetup.Entrance(t, o.Name, new Vector2(o.X, o.Y + 0.05f), o.Id, o.Facing); break;
                case "exit":
                    ForestTestSetup.Exit(t, o.Name, new Vector2(o.X, o.Y), new Vector2(o.W, o.H), o.Text, o.Id, o.Link ?? "");
                    ExitLight(t, o);
                    break;
                default:
                    // o resto é igual ao da Fortaleza: sentinela, atirador, folhagem, sombra, santuário, moeda, espinhos,
                    // serra, fio de alarme, lanças, fogo, lançador, placa, água
                    FortressSetup.Place(t, new FortressLayout.Obj
                    {
                        Kind = o.Kind, Name = o.Name, X = o.X, Y = o.Y, W = o.W, H = o.H, A = o.A, B = o.B,
                        Facing = o.Facing, Value = o.Value, Id = o.Id, Text = o.Text, Link = o.Link,
                    }, dialogues, created);
                    break;
            }
        }
    }
}
