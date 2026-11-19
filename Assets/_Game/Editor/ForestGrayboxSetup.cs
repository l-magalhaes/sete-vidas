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
        static Dictionary<string, DialogueDefinition> s_lore;

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
            s_lore = null;

            var reports = ForestReach.CheckAll();
            string text = ForestReach.Format(reports, false);
            bool ok = true;
            foreach (var r in reports) if (r.Problems.Count > 0) ok = false;
            if (ok) Debug.Log("[Sete Vidas] Floresta F2: alcance conferido.\n" + text);
            else Debug.LogWarning("[Sete Vidas] Floresta F2: o alcance tem problemas (a cena é gerada assim mesmo).\n" + text);

            foreach (var region in ForestLayout.Regions()) BuildRegion(region);
            // a casa da Vulpis (a cabana da Forest_Test_C): interior com a porta de volta para o Refúgio (D3)
            ForestTestSetup.BuildCabin(ForestLayout.Cabin, ForestLayout.D, "W16", "gb_cabana_moeda");
            ForestLore.BuildBook();          // o Diário acha as placas lidas em qualquer cena
            SeteVidasSetup.RegisterScenesInBuild();
            EditorSceneManager.OpenScene($"{Folder}/{ForestLayout.A}.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[Sete Vidas] Floresta: 8 cenas geradas (Forest_Road, Forest_A..F e a casa da Vulpis). Comece pela Forest_A_Roots " +
                      "(desligue \"Play começa na tela inicial\" para dar Play direto nela). Roteiro: Docs/QA/floresta-rev4-plano-de-teste.md");
        }

        /// <summary>
        /// Gera só uma cena da Floresta (18/11/2026: a Estrada, com a ponte nova e a escada do vale), sem mexer nas outras.
        /// </summary>
        public static void BuildOnly(params string[] scenes)
        {
            var regions = ForestLayout.Regions().FindAll(r => System.Array.IndexOf(scenes, r.Scene) >= 0);
            if (regions.Count == 0) { Debug.LogError("[Sete Vidas] Floresta: não há essas cenas no ForestLayout."); return; }
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
            EnvLibrary.ImportAll();
            ForestPainter.ResetCaches();
            s_lore = null;
            foreach (var region in regions) BuildRegion(region);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sete Vidas] Floresta: gerada(s) de novo só " + string.Join(", ", scenes) + ".");
        }

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

            if (s_lore == null) s_lore = ForestLore.Create();   // F5: placas dos Primatas (P-PRIM-xx)
            var dialogues = new Dictionary<string, DialogueDefinition>(s_lore);
            if (reg.Scene == ForestLayout.Road) dialogues[MiaraveuGateSetup.EndDialogue] = MiaraveuGateSetup.CreateDialogue();   // o fim da fase
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
            if (reg.Scene == ForestLayout.E)   // F7: o medidor de alerta, o bote e os galhos frágeis
                Debug.Log($"[Sete Vidas] Ninho (F7): a Isyathy embaixo da terra, {NestSetup.Apply(ctx.Root, reg)} galhos frágeis.");
            if (reg.Scene == ForestLayout.F) { IsyathyBossSetup.Apply(ctx.Root, reg); Debug.Log("[Sete Vidas] F8: a Isyathy na arena F1."); }
            ForestAudioSetup.Apply(ctx.Root, reg);   // F10: música, áreas de música e ambiência de cada sala
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
            // a escada de madeira (18/11/2026, o atalho da ponte para o vale): dois montantes e dois degraus por tile, pregados
            var ladder = Placeholder("escada_atalho", 16, 16, (x, y) =>
            {
                var clear = new Color32(0, 0, 0, 0);
                var ink = new Color32(0x2a, 0x18, 0x10, 0xff);
                bool rail = (x >= 2 && x <= 4) || (x >= 11 && x <= 13);
                bool rung = (y == 3 || y == 4 || y == 11 || y == 12) && x > 4 && x < 11;
                if (rail)
                {
                    bool nail = (y == 4 || y == 12) && (x == 3 || x == 12);
                    if (nail) return new Color32(0xd8, 0xdc, 0xe4, 0xff);
                    if (x == 2 || x == 11) return new Color32(0xb0, 0x7a, 0x48, 0xff);
                    if (x == 4 || x == 13) return new Color32(0x5a, 0x38, 0x20, 0xff);
                    return (y * 3 + x) % 11 == 0 ? new Color32(0x6e, 0x46, 0x28, 0xff) : new Color32(0x8a, 0x5a, 0x34, 0xff);
                }
                if (rung) return y == 4 || y == 12 ? new Color32(0xc0, 0x8a, 0x50, 0xff) : new Color32(0x7a, 0x4e, 0x2a, 0xff);
                if ((x == 1 || x == 14) || ((y == 2 || y == 10) && x > 4 && x < 11)) return x == 1 || x == 14 ? ink : clear;
                return clear;
            });
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
                        bool isLadder = p.Text == "escada";
                        var go = new GameObject(isLadder ? "Escada (aberta)" : "Cipó (aberto)") { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Climbable) };
                        go.transform.SetParent(root.transform, false);
                        go.transform.position = new Vector3(p.X + 0.5f, p.Y + p.H * 0.5f, 0f);
                        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, p.H);
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = isLadder ? ladder : vine; sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(1f, p.H); sr.sortingOrder = 2;
                        if (isLadder) go.AddComponent<SeteVidas.World.LadderTop>().Configure(p.Value >= 0 ? 1 : -1);   // em cima, segurar baixo agarra (Value: o lado do gato)
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
                    // a estaca nunca fica em cima de uma tampa do atalho: ela some ao abrir e a estaca ficava no ar
                    // (19/11/2026); vai para o chão firme do lado oposto ao cipó
                    float stakeX = opener.X;
                    var lidUnder = parts.Find(p => p.Kind == "sc_block" && Mathf.Abs(p.Y + p.H - opener.Y) < 0.3f && opener.X > p.X - 0.01f && opener.X < p.X + p.W + 0.01f);
                    if (lidUnder != null) stakeX = left ? lidUnder.X + lidUnder.W + 0.5f : lidUnder.X - 0.5f;
                    closed.Add(ShortcutProp(root.transform, "Estaca com o nó", "atalho_estaca", stakeX, opener.Y, left, true));
                    open.Add(ShortcutProp(root.transform, "Estaca com o cipó solto", "atalho_estaca_solta", stakeX, opener.Y, left, false));
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
                // F4c: a toca camuflada no chão (um montinho de musgo e terra igual ao topo do chão, fresta no meio)
                var hole = new GameObject("Toca").AddComponent<SpriteRenderer>();
                hole.transform.SetParent(go.transform, false);
                hole.sprite = ForestTestSetup.LoadSprite("serpente_buraco");
                hole.transform.localPosition = new Vector3(0f, hole.sprite != null ? hole.sprite.bounds.extents.y - 0.0625f : 0.1f, 0f);
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
            // F4c: arte definitiva (Tools/EnemySprites/gerar_floresta.ps1), olhando para a direita
            var hidden = ForestTestSetup.LoadSprite("serpente_olhos");
            var crawlSprite = ForestTestSetup.LoadSprite("serpente");
            if (hidden == null || crawlSprite == null) Debug.LogError("[Sete Vidas] Floresta: falta a arte da serpente (rode Tools/EnemySprites/gerar_floresta.ps1).");
            sr.sprite = crawl ? crawlSprite : hidden;
            var e = go.AddComponent<SeteVidas.Enemies.ForestSnake>();
            e.Configure(o.Facing, crawl, crawl ? o.A : o.X, crawl ? o.B : o.X);
            var so = new SerializedObject(e);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("body").objectReferenceValue = body.transform;
            so.FindProperty("hiddenSprite").objectReferenceValue = hidden;
            so.FindProperty("warnSprite").objectReferenceValue = ForestTestSetup.LoadSprite("serpente_aviso");
            so.FindProperty("strikeSprite").objectReferenceValue = ForestTestSetup.LoadSprite("serpente_bote");
            so.FindProperty("crawlSprite").objectReferenceValue = crawlSprite;
            so.FindProperty("segmentSprite").objectReferenceValue = ForestTestSetup.LoadSprite("serpente_gomo");
            // na toca: olhos, olhos, olhos, brilho, olhos, olhos, piscada (2 por segundo)
            ForestTestSetup.SetSprites(so, "hiddenFrames", "serpente_olhos", "serpente_olhos", "serpente_olhos", "serpente_olhos_1",
                "serpente_olhos", "serpente_olhos", "serpente_olhos_2");
            ForestTestSetup.SetSprites(so, "warnFrames", "serpente_aviso", "serpente_aviso", "serpente_aviso_1");
            ForestTestSetup.SetSprites(so, "crawlFrames", "serpente", "serpente_1");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ F4b: Vulpis, Portão do Templo, ruínas

        /// <summary>
        /// A Vulpis (NPC do vínculo, como o Koda na Fortaleza): E fala; a conversa dá a classe Caçador e +1 coração
        /// (<see cref="CompanionBond"/>). Arte: VulpisAnimationSet (Tools/CharacterSprites/VulpisSpriteGen.cs); sem ela, o desenho provisório.
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
            VulpisArtSetup.Dress(go.transform, VulpisArtSetup.Load(), o.Facing < 0);   // a arte nova (animada), se já foi gerada
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
            VulpisRescueSetup.AddDeparture(go);   // com a Chave do Templo, ela vai na frente para o templo (19/11/2026)
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
                Debug.LogWarning("[Sete Vidas] Infectados: falta o ZombieSettings ou a arte do zumbi (rode \"Fortaleza: gerar a cena\" e Tools/EnemySprites/gerar.ps1)");
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

        // ------------------------------------------------------------ F5 parte 2: ruínas dos Primatas e a Isyathy de longe

        /// <summary>Quadros de luz que pisca: aceso, aceso, apagado (o "Verde. Apaga. Verde." da P-PRIM-04), 0,6 s cada.</summary>
        static Sprite[] BlinkFrames(string baseName)
        {
            var on = ForestTestSetup.LoadSprite(baseName + "_0");
            var off = ForestTestSetup.LoadSprite(baseName + "_1");
            if (on == null || off == null) { Debug.LogError($"[Sete Vidas] Floresta: falta {baseName}_0/_1.png (rode Tools/EnvironmentSprites/gerar.ps1)."); return null; }
            return new[] { on, on, off };
        }

        /// <summary>Máquina dos Primatas com UMA luz verde piscando devagar (fora de sincronia com as outras).</summary>
        static void RuinLight(Transform parent, ForestLayout.Obj o)
        {
            var sr = Prop(parent, o.Name, o.Text + "_0", o.X, o.Y, o.Value, false);
            var frames = BlinkFrames(o.Text);
            if (frames != null) sr.gameObject.AddComponent<SpriteLoop>().Configure(frames, 0.6f, (o.X * 0.37f) % 1.8f);
        }

        /// <summary>Placa dos Primatas que se lê (LoreSign com o desenho dela); o nome do objeto é o título no Diário.</summary>
        static void PrimateLore(Transform parent, ForestLayout.Obj o, Dictionary<string, DialogueDefinition> dialogues)
        {
            if (!dialogues.TryGetValue(o.Id, out var text)) { Debug.LogWarning($"[Sete Vidas] Placa sem texto: {o.Id}"); return; }
            bool blink = o.C > 0f;
            var art = ForestTestSetup.LoadSprite(blink ? o.Link + "_0" : o.Link);
            if (art == null) Debug.LogError($"[Sete Vidas] Floresta: falta {o.Link}.png (rode Tools/EnvironmentSprites/gerar.ps1).");
            var go = CitadelDecorSetup.Sign(parent, o.Text, o.X, o.Y, text, art, o.Value);
            if (!blink) return;
            var frames = BlinkFrames(o.Link);
            if (frames != null) go.transform.Find("Placa").gameObject.AddComponent<SpriteLoop>().Configure(frames, 0.6f);
        }

        /// <summary>Ordem da Isyathy de longe: atrás das árvores médias (−80) e das névoas (−70, −55), na frente das copas de longe (−90).</summary>
        public const int SightingOrder = -85;
        /// <summary>Paralaxe da Isyathy de longe: entre as copas de longe (0,04) e as árvores médias (0,12).</summary>
        public const float SightingParallax = 0.08f;

        /// <summary>A Isyathy vista de longe (mapa §6): só desenho no fundo, sem colisão, com a área que a dispara.</summary>
        static void Sighting(Transform parent, ForestLayout.Obj o)
        {
            var kind = (IsyathySighting.Kind)System.Enum.Parse(typeof(IsyathySighting.Kind), o.Text);
            var eyes = SightingFrames("isy_olhos_", 3);       // só os olhos (melhorias 14/11/2026: pescoço e corpo saíram)
            var go = new GameObject(o.Name);
            go.transform.SetParent(parent, false);
            var artGo = new GameObject("Desenho");
            artGo.transform.SetParent(go.transform, false);
            artGo.transform.position = new Vector3(o.X, o.Y, 0f);
            var sr = artGo.AddComponent<SpriteRenderer>();
            sr.sprite = eyes.Length > 0 ? eyes[0] : null;
            // LÁ NO FUNDO, muitas vezes (pedidos do usuário, 14/11/2026): entre as copas de longe (−90) e as árvores médias
            // (−80), atrás das duas névoas, com o paralaxe do fundo e PEQUENA (está longe). Começa invisível: o componente
            // escolhe o lugar a partir da câmera e a animação em cada aparição.
            sr.sortingOrder = SightingOrder;
            sr.enabled = false;
            float scale = kind == IsyathySighting.Kind.Coiled ? 0.85f : 0.6f;   // na D4, mais perto
            go.AddComponent<IsyathySighting>().Configure(kind, sr, eyes, new Rect(o.TX, o.TY, o.TW, o.TH), scale, 0.9f,
                SightingParallax, o.Link, string.IsNullOrEmpty(o.Id) ? new string[0] : o.Id.Split('|'));
        }

        /// <summary>
        /// O espírito da Isyathy pela floresta (pedido do usuário, 19/11/2026): NPC no primeiro plano, só com a flag da morte
        /// dela; E abre a próxima conversa do rodízio (D-ISYATHY-ESPIRITO-02a..e).
        /// </summary>
        static void SpiritNpc(Transform parent, ForestLayout.Obj o)
        {
            var go = new GameObject(o.Name) { layer = LayerMask.NameToLayer(SeteVidas.Core.GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(o.X, o.Y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(3.5f, 3f);
            box.offset = new Vector2(0f, 1.5f);
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 4.9f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: falar", new Color32(0xc8, 0xf0, 0xff, 0xff), 30);
            promptGo.SetActive(false);
            var (talks, bridge) = IsyathyBossSetup.CreateSpiritDialogues();
            go.AddComponent<IsyathySpiritNpc>().Configure(talks, bridge, new Rect(o.TX, o.TY, o.TW, o.TH), prompt);
        }

        /// <summary>Quadros <paramref name="prefix"/>0..n−1 (n = 0: só <paramref name="prefix"/>), de Forest/Placeholder.</summary>
        static Sprite[] SightingFrames(string prefix, int n)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < Mathf.Max(1, n); i++)
            {
                string name = n == 0 ? prefix : prefix + i;
                var s = ForestTestSetup.LoadSprite(name);
                if (s == null) Debug.LogError($"[Sete Vidas] Floresta: falta {name}.png (rode Tools/EnvironmentSprites/gerar.ps1).");
                else list.Add(s);
            }
            return list.ToArray();
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

        /// <summary>Ordem das tochas e fogueiras: na frente do posto abandonado e dos outros enfeites (ordem 1).</summary>
        public const int FireOrder = 2;

        /// <summary>Tocha de poste ou fogueira (chama em 3 quadros, fora de sincronia) com o seu halo: a luz tem dono.</summary>
        static void Fire(Transform parent, string name, float x, float feetY, bool campfire)
        {
            string baseName = campfire ? "fogueira_" : "tocha_poste_";
            // ordem 2 (melhorias 14/11/2026): na ordem 1, a mesma do posto abandonado da R1, as duas se sobrepunham e o
            // Unity trocava quem ficava na frente conforme a câmera andava: a tocha "piscava"
            var sr = Prop(parent, name, baseName + "0", x, feetY, FireOrder, false);
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
                    if (o.W <= 3f && o.Value != 1)   // (Value 1: o gatilho no meio da escada, sem estaca)
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
                case "sprout": return new Vector2(0f, 3f);   // F4c: os olhos nos nós da casca, a 3 tiles do chão
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
                case "tobi": TobiSetup.Npc(t, o); break;                 // F9
                case "bridge_wood": TobiSetup.Bridge(t, o); break;      // F9: a ponte nova (escondida até a flag)
                case "dawn": MiaraveuGateSetup.Dawn(t, o); break;          // o amanhecer da cabeceira leste (R5)
                case "miaraveu_gate": MiaraveuGateSetup.Gate(t, o); break; // o portão em ruínas de Miaravéu (R5)
                case "waterfall": WaterfallSetup.Build(t, o); break;       // as cachoeiras no fundo, caindo da montanha (R5)
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
                case "ruinprop":
                {
                    var sr = Prop(t, o.Name, o.Text, o.X, o.Y, o.Value, false);
                    if (o.C > 0f && sr.sprite != null) sr.transform.position -= new Vector3(0f, sr.sprite.bounds.size.y, 0f); // pendurado: o topo em Y
                    break;
                }
                case "ruinlight": RuinLight(t, o); break;
                case "prilore": PrimateLore(t, o, dialogues); break;
                case "sighting": Sighting(t, o); break;
                case "spirit": SpiritNpc(t, o); break;                 // depois da luta: o espírito da Isyathy (D-ISYATHY-ESPIRITO-02)
                case "rescue": VulpisRescueSetup.Build(t, o); break;    // F6: o resgate da D2 (D-VULPIS-01)
                case "arrowprop":                                       // F6: a trilha de flechas da Vulpis
                {
                    var go = new GameObject(o.Name);
                    go.transform.SetParent(t, false);
                    go.transform.position = new Vector3(o.X, o.Y, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -3;   // atrás do gato, na frente do cenário
                    go.AddComponent<ArrowProp>().Configure(o.Value == 1 ? ArrowProp.Kind.DeadWasp : ArrowProp.Kind.Arrow, o.A);
                    break;
                }
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
                case "boss_isyathy": break;   // F8: montado pelo IsyathyBossSetup
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
