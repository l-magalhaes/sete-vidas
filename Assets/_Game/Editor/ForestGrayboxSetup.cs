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
            EnvLibrary.ImportAll();          // F5: fundos da floresta (Backgrounds/Flo/manifest.json)
            ForestPainter.ResetCaches();

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
            var debris = new List<ForestLayout.Obj>();
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
                    case "debris": debris.Add(o); break;
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
            if (debris.Count > 0)
            {
                // os destroços da ponte no fundo do vale: atrás do gato, caem juntos na primeira vez (DebrisFall)
                var pile = new GameObject("Destroços da ponte").transform;
                pile.SetParent(ctx.Root, false);
                foreach (var d in debris) Prop(pile, d.Name, d.Text, d.X, d.Y, -4, false);
                pile.gameObject.AddComponent<DebrisFall>();
            }
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
            // F5: cenário da floresta (tiles, massa, rampas, detalhes, fundo, mundo vivo)
            var art = ForestPainter.Paint(ctx, reg);
            int secrets = SecretCovers.Build(ctx.Root, ctx.Ground);   // o que há atrás dos troncos rachados
            Debug.Log($"[Sete Vidas] {reg.Label}: {art.Tiles} tiles, {art.Mass} de massa, {art.Details} detalhes, {art.Emitters} emissores, {art.Shadows} sombras ({art.Trees} com árvore), {secrets} segredo(s) escondido(s).");
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
                // no lugar do clarão (pedido do usuário, 14/11/2026): o objeto da ação, antes e depois
                if (opener.Text != null && opener.Text.Contains("cipó"))
                {
                    var rope = parts.Find(p => p.Kind == "sc_vine");
                    bool left = rope != null && rope.X < opener.X;                       // o cipó desce à esquerda
                    closed.Add(ShortcutProp(root.transform, "Estaca com o nó", "atalho_estaca", opener.X, opener.Y, left, true));
                    open.Add(ShortcutProp(root.transform, "Estaca com o cipó solto", "atalho_estaca_solta", opener.X, opener.Y, left, false));
                }
                else
                {
                    closed.Add(ShortcutProp(root.transform, "Argola do alçapão", "alcapao_argola", opener.X, opener.Y, false, true));
                    var lidPart = parts.Find(p => p.Kind == "sc_block");
                    if (lidPart != null)   // a tampa em pé na dobradiça, do lado esquerdo do buraco
                        open.Add(ShortcutProp(root.transform, "Alçapão aberto", "alcapao_aberto", lidPart.X - 0.5f, lidPart.Y + lidPart.H, false, false));
                }
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

        // ------------------------------------------------------------ F4b: Vulpis, Portão do Templo, ruínas

        /// <summary>
        /// A Vulpis (NPC do vínculo, como o Koda na Fortaleza): E fala; a conversa dá a classe Caçador e +1 coração
        /// (<see cref="CompanionBond"/>). Desenho provisório: raposa de pé com o arco.
        /// </summary>
        static void VulpisNpc(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject(o.Name) { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3f, 2.4f);
            box.offset = new Vector2(0f, 1.2f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = Placeholder("vulpis", 24, 32, PaintVulpis);
            sr.sortingOrder = 3;
            sr.flipX = o.Facing < 0;
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: falar", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);

            var bond = go.AddComponent<CompanionBond>();
            var d = DialogueSetup.CreateVulpisDialogues();
            var so = new SerializedObject(bond);
            so.FindProperty("grantsClass").enumValueIndex = (int)SeteVidas.Classes.CharacterClass.Hunter;
            so.FindProperty("bondFlag").stringValue = SeteVidas.Abilities.HunterRules.BondFlag;
            so.FindProperty("bondDialogue").objectReferenceValue = d.Bond;
            so.FindProperty("bondFollowUp").objectReferenceValue = d.BondFollowUp;
            so.FindProperty("unlockDialogue").objectReferenceValue = d.Unlock;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            var idle = so.FindProperty("idleDialogues");
            idle.arraySize = d.Idle.Length;
            for (int i = 0; i < d.Idle.Length; i++) idle.GetArrayElementAtIndex(i).objectReferenceValue = d.Idle[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Raposa de pé (24 × 32, olhando para a direita): pelo laranja, peito branco, capa verde, arco nas costas.</summary>
        static Color32 PaintVulpis(int x, int y)
        {
            Color32 clear = new Color32(0, 0, 0, 0), fur = new Color32(0xd8, 0x6a, 0x2a, 0xff), white = new Color32(0xf4, 0xee, 0xdc, 0xff),
                    dark = new Color32(0x3a, 0x1e, 0x12, 0xff), cape = new Color32(0x3e, 0x5a, 0x3a, 0xff), wood = new Color32(0x96, 0x6e, 0x40, 0xff),
                    eye = new Color32(0xff, 0xd3, 0x4d, 0xff);
            // cauda (atrás, à esquerda), com a ponta branca
            if (y >= 2 && y <= 8 && x >= 1 && x <= 7) { float dx = (x - 4f) / 3.5f, dy = (y - 5f) / 3.5f; if (dx * dx + dy * dy <= 1f) return x <= 2 ? white : fur; }
            if (x >= 8 && x <= 15 && y <= 1) return dark;                                   // patas
            if (x >= 8 && x <= 15 && y >= 2 && y <= 17) return x >= 12 && y >= 8 && y <= 15 ? white : (x <= 9 ? cape : fur); // corpo, peito, capa
            if (x >= 7 && x <= 8 && y >= 6 && y <= 20) return wood;                        // arco nas costas
            if (y >= 18 && y <= 25 && x >= 9 && x <= 16)                                   // cabeça
            {
                if (y == 22 && x == 14) return eye;
                return fur;
            }
            if (y >= 19 && y <= 21 && x >= 17 && x <= 20) return x == 20 && y == 20 ? dark : white; // focinho
            if ((x == 10 || x == 15) && y >= 26 && y <= 29) return y >= 28 ? dark : fur;   // orelhas
            return clear;
        }

        /// <summary>
        /// Portão do Templo da Serpente (1 × 4, sólido até abrir com a Chave do Templo): pedra verde-musgo com a
        /// serpente entalhada e o olho turquesa. Bem diferente da porta de madeira da cabana.
        /// </summary>
        static void TempleGate(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject(o.Name) { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 2f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            var closed = Placeholder("portao_templo_fechado", 16, 64, (x, y) => PaintTempleGate(x, y, false));
            var open = Placeholder("portao_templo_aberto", 16, 64, (x, y) => PaintTempleGate(x, y, true));
            sr.sprite = closed;
            sr.sortingOrder = 2;
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(-1.2f, 4.6f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: abrir", new Color32(0x9a, 0xf0, 0xe0, 0xff), 30);
            promptGo.SetActive(false);
            var door = go.AddComponent<LockedDoor>();
            door.Configure(o.Id, o.Text);
            var so = new SerializedObject(door);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("closedSprite").objectReferenceValue = closed;
            so.FindProperty("openSprite").objectReferenceValue = open;
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.FindProperty("reach").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Color32 PaintTempleGate(int x, int y, bool open)
        {
            Color32 clear = new Color32(0, 0, 0, 0), stone = new Color32(0x4f, 0x7a, 0x52, 0xff), dark = new Color32(0x2e, 0x4a, 0x34, 0xff),
                    moss = new Color32(0x6c, 0x96, 0x5a, 0xff), eye = new Color32(0x5c, 0xe0, 0xc8, 0xff);
            if (open) return (x <= 1 || x >= 14) ? (y % 8 == 0 ? dark : stone) : clear;   // só os batentes
            if (x == 0 || x == 15 || y == 0 || y == 63) return dark;
            // a serpente entalhada: corpo em S de baixo para cima, cabeça no alto com o olho turquesa
            int sx = 8 + (int)(4f * Mathf.Sin(y * 0.18f));
            if (y < 50 && Mathf.Abs(x - sx) <= 1) return x == sx ? moss : dark;
            if (y >= 50 && y <= 58 && x >= 4 && x <= 11) return (x == 9 && y == 55) || (x == 10 && y == 55) ? eye : moss;
            return (x + y * 3) % 11 == 0 ? dark : stone;
        }

        /// <summary>Parede de pedras de ruínas antigas (1 × 4): a parede rachada da Fortaleza, tingida de musgo. Só a marreta.</summary>
        static void RuinWall(Transform parent, ForestLayout.Obj o)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClassSetup.WallPrefabPath), parent);
            go.name = o.Name;
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var wall = go.GetComponent<BreakableWall>();
            var so = new SerializedObject(wall);
            so.FindProperty("wallId").stringValue = o.Id;
            so.FindProperty("material").enumValueIndex = (int)BreakableMaterial.Stone;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.color = new Color(0.72f, 0.9f, 0.72f, 1f); // musgo das ruínas
                PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            }
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
                // a arte do bloco (ForestPropGen): parapeito, tabuleiro e o pedaço do arco; o topo do tabuleiro = a colisão
                var visual = new GameObject("Visual");
                visual.transform.SetParent(go.transform, false);
                visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = ForestTestSetup.LoadSprite($"ponte_pedra_{n}_{i}") ?? stone;
                sr.sortingOrder = 1;
                blocks.Add(body);
            }
            root.AddComponent<BridgeCollapse>().Configure(blocks.ToArray(), o.A, o.B, hordeSquad);
        }

        static readonly Color32 FireTint = new Color32(0xff, 0xa8, 0x48, 0xff);

        /// <summary>Enfeite com os pés em (x, feetY), na ordem dada.</summary>
        static SpriteRenderer Prop(Transform parent, string name, string sprite, float x, float feetY, int order, bool flip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestTestSetup.LoadSprite(sprite);
            sr.sortingOrder = order;
            sr.flipX = flip;
            if (sr.sprite == null) Debug.LogError($"[Sete Vidas] Floresta: falta {sprite}.png (rode Tools/EnvironmentSprites/gerar.ps1).");
            go.transform.position = new Vector3(x, feetY + (sr.sprite != null ? sr.sprite.bounds.extents.y : 0.5f), 0f);
            return sr;
        }

        /// <summary>Halo liso e quente em volta de uma chama (sem pontilhado), atrás do gato.</summary>
        static void Halo(Transform parent, Vector3 at, float scale, Color32 tint, byte alpha)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestTestSetup.LoadSprite("luz_quente");
            sr.color = new Color32(tint.r, tint.g, tint.b, alpha);
            sr.sortingOrder = -2;
        }

        /// <summary>Tocha de poste ou fogueira (chama em 3 quadros, fora de sincronia) com o seu halo: a luz tem dono.</summary>
        static void Fire(Transform parent, string name, float x, float feetY, bool campfire)
        {
            string baseName = campfire ? "fogueira_" : "tocha_poste_";
            var sr = Prop(parent, name, baseName + "0", x, feetY, 1, false);
            var frames = new Sprite[3];
            for (int i = 0; i < 3; i++) frames[i] = ForestTestSetup.LoadSprite(baseName + i);
            sr.gameObject.AddComponent<SpriteLoop>().Configure(frames, 0.22f, Mathf.Repeat(x * 0.37f, 0.66f));   // chama calma
            Halo(parent, new Vector3(x, feetY + (campfire ? 0.9f : 2.2f), 0f), campfire ? 1.4f : 1f, FireTint, 255);
        }

        /// <summary>Marco da trilha numa saída: poste, placa-seta para fora (<paramref name="flip"/> = para a esquerda) e a lanterna acesa.</summary>
        static void TrailMarker(Transform parent, string name, float x, float feetY, bool flip)
        {
            var sr = Prop(parent, name, "marco_trilha", x, feetY, 1, flip);
            Halo(parent, new Vector3(x + (flip ? -0.4f : 0.4f), feetY + 2.4f, 0f), 0.8f, FireTint, 220);
        }

        /// <summary>Desenho de um atalho (ForestObjectGen) com os pés em (x, feetY); some/aparece pelo ShortcutLink.</summary>
        static GameObject ShortcutProp(Transform parent, string name, string sprite, float x, float feetY, bool flip, bool active)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ForestTestSetup.LoadSprite(sprite);
            sr.flipX = flip;
            sr.sortingOrder = 2;   // por cima do chão e da tampa, atrás do gato
            float half = sr.sprite != null ? sr.sprite.bounds.extents.y : 0.5f;
            if (sr.sprite == null) Debug.LogError($"[Sete Vidas] Floresta: falta {sprite}.png (rode Tools/EnvironmentSprites/gerar.ps1).");
            go.transform.position = new Vector3(x, feetY + half, 0f);
            go.SetActive(active);
            return go;
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
                // saídas laterais (pedido do usuário, 14/11/2026: "luzes sem assets reais"): o marco da trilha, com a
                // lanterna acesa (a luz agora tem dono) e a placa apontando para fora
                case 1:  TrailMarker(t, o.Name + " (marco)", o.X - 3.5f, o.Y - o.H * 0.5f, false); break;
                case -1: TrailMarker(t, o.Name + " (marco)", o.X + 3.5f, o.Y - o.H * 0.5f, true); break;
                // saídas verticais (pedido do usuário, 14/11/2026): a luz fica DENTRO do poço (mais forte lá no fundo, vindo
                // da outra área) e só a ponta fraca entra na sala; o poço tem parede de fundo e vinhas (ForestPainter)
                case 2:  PassageLight.Vertical(t, name, o.X, o.W, o.Y + 0.25f, o.Y + 8f, Tint, brightUp: true); break;
                case -2: PassageLight.Vertical(t, name, o.X, o.W, o.Y - 7f, o.Y + 3f, Tint, brightUp: false); break;
                default:
                    // gatilho pequeno no meio da sala (a corda da ponte nova): a estaca com a corda descendo, sem clarão
                    if (o.W <= 3f)
                    {
                        var rope = Prop(t, o.Name + " (corda)", "atalho_estaca_solta", o.X, o.Y - o.H * 0.5f, 2, false);
                        // só aparece quando a passagem funciona (a corda da ponte nova)
                        if (!string.IsNullOrEmpty(o.Link)) rope.gameObject.AddComponent<FlagVisible>().Configure(o.Link);
                    }
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
                case "trunk": ForestTestSetup.Trunk(t, o.Name, o.X, o.Y, o.Id, o.H > 0f ? o.H : 4f); break;
                case "chest":
                {
                    bool hasKey = !string.IsNullOrEmpty(o.Text);
                    ForestTestSetup.Chest(t, o.Name, o.X, o.Y, o.Id, wood: !hasKey && o.B < 1f, coins: o.Value); // B = 1: só o coração
                    if (hasKey || o.C > 0)
                    {
                        var chest = t.GetChild(t.childCount - 1).GetComponent<WoodChest>();
                        if (chest != null) { if (hasKey) chest.SetKey(o.Text); chest.SetTempHearts((int)o.C); EditorUtility.SetDirty(chest); }
                    }
                    break;
                }
                case "vulpis": VulpisNpc(t, o); break;
                case "gate": TempleGate(t, o); break;
                case "ruinwall": RuinWall(t, o); break;
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
                case "torch": Fire(t, o.Name, o.X, o.Y, o.Name.Contains("fogueira")); break;
                case "templeprop": Prop(t, o.Name, o.Text, o.X, o.Y, o.Value, false); break;
                case "cabinfacade":ForestTestSetup.CabinFacade(t, o.X, o.Y); break;
                case "outpost":Prop(t, o.Name, "posto_abandonado", o.X, o.Y, 1, false); break;
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
