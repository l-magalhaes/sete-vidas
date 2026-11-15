using System.Collections.Generic;
using SeteVidas.World;
using SeteVidas.World.Scenery;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Cenário da Floresta (F5, parte 1) numa cena do graybox: troca os tiles provisórios pelos da floresta
    /// (<see cref="ForestAutotile"/>, material por sala em <see cref="ForestTerrain"/>), pinta a massa da floresta em volta
    /// das salas (tilemap sem colisão), veste as rampas, espalha os detalhes (samambaias, fungos, brotos da praga, raízes),
    /// põe o fundo em paralaxe (<see cref="ForestBackgrounds"/>) e o mundo vivo (vaga-lumes, folhas, poeira da praga).
    /// Só muda o desenho: a colisão continua a do <see cref="ForestLayout"/>.
    /// Arte: Tools/EnvironmentSprites/gerar.ps1 (ForestSpriteGen). Prévia sem o Unity: Tools/LevelPreview/ver_floresta.ps1.
    /// </summary>
    public static class ForestPainter
    {
        public const string TerrainArt = "Assets/_Game/Art/Environments/Forest/Terrain";
        const string TileFolder = "Assets/_Game/ScriptableObjects/Environment/Tiles";
        const string Life = "Assets/_Game/Art/Environments/Common/Life/";
        const string Plague = "Assets/_Game/Art/Environments/Necro/Plague/";

        /// <summary>Massa: atrás da parede do fundo (-10) e na frente do fundo em paralaxe (-60).</summary>
        const int MassOrder = -12;

        static readonly Dictionary<string, Tile> s_tiles = new Dictionary<string, Tile>();
        static readonly Dictionary<string, Sprite> s_sprites = new Dictionary<string, Sprite>();
        static int s_missing;

        public class Result { public int Tiles, Mass, Details, Emitters, Shadows, Trees, Missing; }

        internal static Result Paint(ForestTestSetup.Ctx ctx, ForestLayout.Region reg)
        {
            s_missing = 0;
            var res = new Result();
            var m = ForestTerrain.Build(reg);

            // ---- chão, vinha e parede do fundo (as células que o layout já pôs)
            for (int x = m.X0; x <= m.X1; x++)
                for (int y = m.Y0; y <= m.Y1; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    string mat = m.MaterialAt(x, y);
                    string cabin = m.Cabin(x, y);
                    if (m.Ground(x, y) && Set(ctx.Ground, c, m.Hidden(x, y) ? InvisibleTile : cabin != null ? ForestTerrain.CabinTile(cabin, x, y) : ForestTerrain.GroundTile(mat, m.Visual, x, y))) res.Tiles++;
                    if (m.CabinInside(x, y) && Set(ctx.Back, c, ForestTerrain.CabinBackTile(x, y))) res.Tiles++;
                    if (m.Climb(x, y) && Set(ctx.Vines, c, ForestAutotile.ClimbTileFor(ForestTerrain.DecorMaterial(mat), m.Visual, x, y))) res.Tiles++;
                    if (m.Back(x, y) && Set(ctx.Back, c, ForestAutotile.BackTileFor(ForestTerrain.DecorMaterial(mat), x, y))) res.Tiles++;
                }

            // ---- a janela velha no meio da parede de cada cabaninha (esboço do usuário, 14/11/2026)
            foreach (var box in m.CabinBoxes)
            {
                if (box[2] - box[0] < 2 || box[3] - box[1] < 2) continue;                 // vão pequeno demais
                var s = ForestTestSetup.LoadSprite("cabana_janela_velha");
                if (s == null) { s_missing++; continue; }
                var go = new GameObject("Janela da cabaninha");
                go.transform.SetParent(ctx.Root, false);
                go.transform.position = new Vector3((box[0] + box[2] + 1) * 0.5f, (box[1] + box[3] + 1) * 0.5f + 0.4f, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                sr.sortingOrder = -9;                                                    // na parede (-10), atrás do gato
            }

            // ---- massa da floresta (fora das salas)
            var grid = ctx.Ground.transform.parent;
            var mass = SeteVidasSetup.CreateTilemap(grid, "Massa da floresta", null, MassOrder, false);
            for (int x = m.X0; x <= m.X1; x++)
                for (int y = m.Y0; y <= m.Y1; y++)
                    if (Set(mass, new Vector3Int(x, y, 0), ForestTerrain.MassTile(m, x, y))) res.Mass++;

            DressShafts(ctx, m, grid);
            DressSlopes(ctx.Root, m);
            res.Details = Details(ctx.Root, m);
            Backgrounds(ctx.Root, reg);
            res.Emitters = LifeOf(ctx.Root, m);
            DressShadows(ctx.Root, m, res);
            res.Missing = s_missing;
            if (s_missing > 0)
                Debug.LogError($"[Sete Vidas] Floresta F5: {s_missing} sprite(s) sem arte em {reg.Scene}. Rode Tools/EnvironmentSprites/gerar.ps1 e reimporte.");
            return res;
        }

        // ------------------------------------------------------------ a cabana da Vulpis (interior)

        /// <summary>
        /// Veste o interior da cabana da Vulpis (pedido do usuário, 14/11/2026: "está sem asset nenhum"): os tiles do
        /// chão viram piso de tábuas, paredes de tábuas e as vigas do teto; o fundo, tábuas escuras; e os móveis de uma
        /// caçadora (cama de peles, mesa com vela, arco e aljava na parede, peles secando, janela com a lua). O interior vai
        /// de (x0, y0) a (x1, y1) (o chão fica em y0 − 1).
        /// </summary>
        internal static void DressCabin(ForestTestSetup.Ctx ctx, int x0, int y0, int x1, int y1)
        {
            ResetCaches();
            for (int x = x0 - 3; x <= x1 + 3; x++)
                for (int y = y0 - 4; y <= y1 + 3; y++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (ctx.Ground.HasTile(c))
                    {
                        // a casa da Vulpis é habitada (pedido do usuário, 14/11/2026: "orgânica e detalhada, parecendo que
                        // alguém mora lá"): assoalho de tábuas sobre pedra, toras nas paredes grossas, o telhado da cabaninha
                        string tile = y == y0 - 1 ? "tile_flo_casa_piso_" + TerrainAutotile.Mod(x, 2)
                            : y < y0 ? "tile_flo_casa_alicerce_" + TerrainAutotile.Mod(x, 2)
                            : y > y1 ? ForestTerrain.CabinTile("telhado", x, y)
                            : "tile_flo_cabana_tora_" + TerrainAutotile.Mod(x, 3);
                        Set(ctx.Ground, c, tile);
                    }
                    else if (x >= x0 && x <= x1 && y >= y0 && y <= y1)
                        // por dentro, toras deitadas com barro entre elas; aqui e ali um nó ou um gancho de ferro
                        Set(ctx.Back, c, "tile_flo_casa_parede_" + (TerrainAutotile.Hash(x, y, 152) % 100u < 78u ? 0 : 1 + (int)(TerrainAutotile.Hash(x, y, 153) % 2u)));
                }
            var parent = new GameObject("Móveis da casa").transform;
            parent.SetParent(ctx.Root, false);
            // Tools/EnvironmentSprites/VulpisHomeGen.cs. Da porta (x 0) para a direita: barril de flechas, cama embaixo da
            // janela, pele no bastidor, lenha, lareira acesa com o tapete na frente, o baú (do jogo), o mapa na parede,
            // ervas secando do teto, a mesa com a vela, o banco, a prateleira (a plataforma do jogo) e o arco.
            Furniture(parent, "casa_barril", 2.4f, y0, -3);
            Furniture(parent, "casa_janela", 5f, y0 + 3.2f, -5);
            Furniture(parent, "casa_cama", 5f, y0, -3);
            Furniture(parent, "casa_pele", 7.4f, y0 + 4.2f, -5);
            Furniture(parent, "casa_lenha", 7.6f, y0, -3);
            Furniture(parent, "casa_lareira", 10f, y0, -4);
            var fire = Furniture(parent, "casa_fogo_0", 10f, y0 + 0.25f, -3);
            if (fire != null)
            {
                var frames = new[] { ForestTestSetup.LoadSprite("casa_fogo_0"), ForestTestSetup.LoadSprite("casa_fogo_1"), ForestTestSetup.LoadSprite("casa_fogo_2") };
                fire.gameObject.AddComponent<SpriteLoop>().Configure(frames, 0.12f);
                WarmLight(parent, "Luz da lareira", new Vector3(10f, y0 + 1.1f, 0f), 1.6f, 255);   // toda luz tem quem a faça
            }
            Furniture(parent, "casa_tapete", 10f, y0, -2);
            Furniture(parent, "casa_mapa", 13f, y0 + 4f, -5);
            Furniture(parent, "casa_ervas", 12.5f, y1 + 1f - 26f / 16f, -4);
            Furniture(parent, "casa_mesa", 15f, y0, -3);
            WarmLight(parent, "Luz da vela", new Vector3(15.94f, y0 + 1.8f, 0f), 0.6f, 200);
            Furniture(parent, "casa_banco", 17.2f, y0, -3);
            Furniture(parent, "casa_prateleira", 16f, y0 + 2.25f, -2);   // a tábua fica no topo da plataforma (y 3)
            Furniture(parent, "casa_arco", 18.1f, y0 + 4.6f, -4);
        }

        /// <summary>Halo quente e liso (luz_quente, sem pontilhado) em volta de uma chama, atrás do gato.</summary>
        static void WarmLight(Transform parent, string name, Vector3 at, float scale, byte alpha)
        {
            var s = ForestTestSetup.LoadSprite("luz_quente");
            if (s == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = new Color32(0xff, 0xb0, 0x60, alpha);
            sr.sortingOrder = -2;
        }

        static SpriteRenderer Furniture(Transform parent, string sprite, float x, float feetY, int order)
        {
            var s = ForestTestSetup.LoadSprite(sprite);
            if (s == null) { s_missing++; Debug.LogWarning($"[Sete Vidas] Cabana: falta {sprite}.png (rode gerar.ps1)"); return null; }
            var go = new GameObject(sprite);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, feetY + s.bounds.extents.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        // ------------------------------------------------------------ poços das saídas verticais

        /// <summary>
        /// O poço de uma saída para cima ou para baixo: parede de fundo (não fica transparente) e vinhas nas duas laterais,
        /// num tilemap SÓ de desenho (a colisão do poço continua a do layout). A luz entra pelo ExitLight.
        /// </summary>
        static void DressShafts(ForestTestSetup.Ctx ctx, ForestTerrain.Model m, Transform grid)
        {
            var shafts = ForestTerrain.Shafts(m);
            if (shafts.Count == 0) return;
            var vines = SeteVidasSetup.CreateTilemap(grid, "Vinhas das passagens", null, 1, false);
            foreach (var s in shafts)
                for (int y = s.Y0; y <= s.Y1; y++)
                {
                    string mat = ForestTerrain.DecorMaterial(m.MaterialAt(s.X0, s.Shell));
                    for (int x = s.X0; x <= s.X1; x++)
                        if (!m.Real(x, y)) Set(ctx.Back, new Vector3Int(x, y, 0), ForestAutotile.BackTileFor(mat, x, y));
                    if (y == s.Shell) continue;                                  // na casca, a vinha da sala (se houver)
                    foreach (int x in new[] { s.X0 - 1, s.X1 + 1 })
                        if (m.Visual(x, y) && !m.Climb(x, y)) Set(vines, new Vector3Int(x, y, 0), ForestAutotile.ClimbTileFor(mat, m.Visual, x, y));
                }
        }

        // ------------------------------------------------------------ sombras (esconderijos)

        /// <summary>Tile transparente (ForestPropGen): colisão sem desenho, na casca da estrada.</summary>
        public const string InvisibleTile = "tile_flo_invisivel";

        public const string ShadowArt = "Assets/_Game/Art/Environments/Forest/Shadows";
        /// <summary>Árvore que faz a sombra: atrás do chão (0) e da vinha, na frente da parede do fundo (-10).</summary>
        const int ShadowTreeOrder = -6;

        /// <summary>
        /// Cada zona de sombra ganha quem a faz (regra: toda sombra tem um objeto que a projeta): uma árvore no fundo, com a
        /// copa por cima da zona e o tronco dentro dela, ou o teto logo acima. O escuro troca o retângulo pontilhado pelo
        /// desenho com a forma da copa (ou do teto). A zona de jogo (LightZone) não muda.
        /// </summary>
        static void DressShadows(Transform root, ForestTerrain.Model m, Result res)
        {
            var zones = root.GetComponentsInChildren<LightZone>(true);
            foreach (var s in ForestTerrain.Shadows(m))
            {
                LightZone zone = null;
                foreach (var z in zones)
                    if (Vector2.Distance(z.transform.position, new Vector2(s.Obj.X, s.Obj.Y)) < 0.05f) { zone = z; break; }
                if (zone == null) { Debug.LogWarning($"[Sete Vidas] Floresta F5: zona de sombra não encontrada ({s.Obj.Name})."); continue; }

                // o desenho da sombra fica separado da zona de jogo (que continua o mesmo retângulo): começa colado na copa e
                // desce até o chão, mesmo quando a zona está num degrau (antes ficava "voando")
                var shadeSprite = LoadArt(ShadowArt, s.ShadeName);
                var so = new SerializedObject(zone);
                var old = so.FindProperty("shade").objectReferenceValue as SpriteRenderer;
                if (shadeSprite != null)
                {
                    Color tint = old != null ? old.color : new Color(0.04f, 0.03f, 0.1f, 0.42f);
                    tint.a = Mathf.Lerp(0.6f, 0f, zone.Exposure);   // como o LightZone pinta: mais escuro = mais opaco
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    so.FindProperty("shade").objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var art = new GameObject("Sombra (desenho)");
                    art.transform.SetParent(zone.transform, false);
                    // pivô no centro: o desenho vai do topo da zona até "Below" tiles abaixo dela
                    art.transform.localPosition = new Vector3(0f, -s.Below * 0.5f * (s.Roof ? 0f : 1f), 0f);
                    var shade = art.AddComponent<SpriteRenderer>();
                    shade.sprite = shadeSprite;
                    shade.color = tint;
                    shade.sortingOrder = 8;   // por cima do gato: ele escurece ao entrar na sombra
                    res.Shadows++;
                }
                if (s.Roof) continue;
                var treeSprite = LoadArt(ShadowArt, s.TreeName);
                if (treeSprite == null) continue;
                var tree = new GameObject("Árvore (faz a sombra)");
                tree.transform.SetParent(zone.transform, false);
                // a árvore vai de "Below" tiles abaixo da zona até 3 acima dela; pivô no centro do sprite
                tree.transform.localPosition = new Vector3(0f, (ForestTerrain.ShadowInfo.Crown - s.Below) * 0.5f, 0f);
                var sr = tree.AddComponent<SpriteRenderer>();
                sr.sprite = treeSprite;
                sr.sortingOrder = ShadowTreeOrder;
                res.Trees++;
            }
        }

        static Sprite LoadArt(string folder, string name)
        {
            string key = folder + "/" + name;
            if (s_sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png");
            if (s == null) { s_missing++; Debug.LogWarning($"[Sete Vidas] Floresta F5: falta {folder}/{name}.png"); return null; }
            s_sprites[key] = s;
            return s;
        }

        // ------------------------------------------------------------ rampas

        /// <summary>Troca o triângulo provisório das rampas 1:1 e 1:2 de solo e praga pelos pedaços da floresta.</summary>
        static void DressSlopes(Transform root, ForestTerrain.Model m)
        {
            var colliders = root.GetComponentsInChildren<PolygonCollider2D>(true);
            foreach (var s in m.Slopes)
            {
                var pieces = ForestAutotile.SlopePieces(s.Material, s.X0, s.Y0, s.W, s.H, s.RisesRight);
                if (pieces.Count == 0) continue;
                foreach (var col in colliders)
                {
                    var p = col.transform.position;
                    if (Mathf.Abs(p.x - s.X0) > 0.01f || Mathf.Abs(p.y - s.Y0) > 0.01f) continue;
                    var visual = col.transform.Find("Visual");
                    if (visual == null) continue;
                    var sr = visual.GetComponent<SpriteRenderer>();
                    if (sr != null) Object.DestroyImmediate(sr);
                    visual.localPosition = Vector3.zero;
                    foreach (var piece in pieces)
                    {
                        var go = new GameObject(piece.Sprite);
                        go.transform.SetParent(visual, false);
                        go.transform.position = new Vector3(piece.X + 0.5f, piece.Y + 0.5f, 0f);
                        var psr = go.AddComponent<SpriteRenderer>();
                        psr.sprite = LoadSprite(piece.Sprite);
                        psr.sortingOrder = 0;
                    }
                }
            }
        }

        // ------------------------------------------------------------ detalhes

        static int Details(Transform root, ForestTerrain.Model m)
        {
            var parent = new GameObject("Detalhes do terreno (F5)").transform;
            parent.SetParent(root, false);
            int n = 0;
            for (int x = m.X0; x <= m.X1; x++)
                for (int y = m.Y0; y <= m.Y1; y++)
                {
                    if (!m.Ground(x, y) || m.Climb(x, y)) continue;
                    string mat = m.MaterialAt(x, y);
                    var list = new List<TerrainAutotile.Detail>();
                    if (m.Hidden(x, y) || m.Cabin(x, y) != null) continue;   // na cabaninha, sem samambaia nem raiz
                    if (!m.Visual(x, y + 1)) list.AddRange(ForestTerrain.TopDetailsFor(mat, m.BlightAt(x, y), x, y));
                    if (!m.Visual(x, y - 1)) list.AddRange(ForestTerrain.CeilingDetailsFor(mat, x, y));
                    foreach (var d in list) if (Detail(parent, d)) n++;
                }
            return n;
        }

        static bool Detail(Transform parent, TerrainAutotile.Detail d)
        {
            var sprite = LoadSprite(d.Sprite);
            if (sprite == null) return false;
            var go = new GameObject(d.Sprite);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1; // terreno_detalhe: na frente do chão, atrás do gato
            go.transform.position = new Vector3(d.X, d.Y + sprite.bounds.extents.y, 0f);
            if (ForestAutotile.IsFern(d.Sprite))
            {
                var left = LoadSprite(ForestAutotile.FernBentName(d.Sprite, false));
                var right = LoadSprite(ForestAutotile.FernBentName(d.Sprite, true));
                if (left != null && right != null) go.AddComponent<BendOnPass>().Configure(sr, sprite, left, right);
            }
            else if (ForestAutotile.IsGlowing(d.Sprite))
            {
                // fungos e brotos pulsam devagar, fora de tempo uns dos outros
                float phase = Mathf.Repeat(d.X * 0.173f + d.Y * 0.071f, 1f);
                bool sprout = d.Sprite.StartsWith("env_flo_praga_broto_");
                go.AddComponent<PulseLight>().Configure(sprout ? 1.8f : 3.2f, phase, sprout ? 0.55f : 0.7f, 1f, false);
            }
            return true;
        }

        // ------------------------------------------------------------ fundo

        static void Backgrounds(Transform root, ForestLayout.Region reg)
        {
            var parent = new GameObject("Fundo da floresta (F5)").transform;
            parent.SetParent(root, false);
            float ax, ay;
            ForestBackgrounds.Anchor(reg, out ax, out ay);
            foreach (var l in ForestBackgrounds.Layers(reg.Scene))
            {
                if (EnvLibrary.Get(l.Id) == null) { Debug.LogError($"[Sete Vidas] Floresta F5: falta o fundo {l.Id} na biblioteca (rode gerar.ps1 e Importar biblioteca de cenário)"); s_missing++; continue; }
                var layer = DressingBuilder.Background(parent, l.Id, l.Id, new Vector2(ax, float.IsNaN(l.AnchorY) ? ay : l.AnchorY), new Vector2(l.FactorX, l.FactorY), l.Order);
                if (l.Drift != 0f && layer.TryGetComponent<ParallaxLayer>(out var plx)) plx.SetDrift(l.Drift);
            }
        }

        // ------------------------------------------------------------ mundo vivo

        /// <summary>
        /// Por sala: vaga-lumes rente ao chão onde a mata é saudável, folhas caindo nas regiões A e B, poeira roxa onde a
        /// praga chegou (mais densa no Ninho e no Coração). Os emissores param longe da câmera (teto global de 40).
        /// </summary>
        static int LifeOf(Transform root, ForestTerrain.Model m)
        {
            var parent = new GameObject("Mundo vivo (F5)").transform;
            parent.SetParent(root, false);
            var fireflies = Sprites(Life + "env_com_fx_vagalume.png");
            var leaves = Sprites(Life + "env_com_fx_folha_0.png", Life + "env_com_fx_folha_1.png");
            var plague = Sprites(Plague + "env_nec_fx_poeira.png");
            int n = 0;
            string scene = m.Region.Scene;
            foreach (var r in m.Region.Rooms)
            {
                int blight = ForestTerrain.BlightOf(r.Id);
                float x0 = r.InX0, x1 = r.InX1 + 1, y0 = r.InY0, y1 = r.InY1 + 1;
                if (blight < 2 && scene != ForestLayout.Road)
                    n += Emitter(parent, r.Id + ": vaga-lumes", AmbientEmitter.Kind.Fireflies, Rect.MinMaxRect(x0, y0 + 0.4f, x1, Mathf.Min(y1, y0 + 4f)), 0.4f, 3, fireflies);
                if (blight == 0 && (scene == ForestLayout.A || scene == ForestLayout.B || scene == ForestLayout.Road))
                    n += Emitter(parent, r.Id + ": folhas", AmbientEmitter.Kind.Leaves, Rect.MinMaxRect(x0, Mathf.Max(y0, y1 - 5f), x1, y1), 0.18f, 2, leaves);
                if (blight > 0)
                    n += Emitter(parent, r.Id + ": poeira da praga", AmbientEmitter.Kind.PlagueDust, Rect.MinMaxRect(x0, y0 + 0.5f, x1, y1), blight * 0.2f, 2 + blight * 2, plague);
            }
            return n;
        }

        static int Emitter(Transform parent, string name, AmbientEmitter.Kind kind, Rect area, float rate, int max, Sprite[] sprites)
        {
            if (sprites.Length == 0) { Debug.LogWarning($"[Sete Vidas] {name}: falta a arte (rode Tools/EnvironmentSprites/gerar.ps1)"); return 0; }
            if (area.width <= 0f || area.height <= 0f) return 0;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<AmbientEmitter>().Configure(kind, area, rate, max, sprites, kind == AmbientEmitter.Kind.Leaves ? 9 : 8);
            return 1;
        }

        static Sprite[] Sprites(params string[] paths)
        {
            var list = new List<Sprite>();
            foreach (var p in paths) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(p); if (s != null) list.Add(s); }
            return list.ToArray();
        }

        // ------------------------------------------------------------ tiles e sprites

        static bool Set(Tilemap map, Vector3Int c, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return false;
            var tile = TileFor(spriteName);
            if (tile == null) return false;
            map.SetTile(c, tile);
            return true;
        }

        public static Sprite LoadSprite(string name)
        {
            if (s_sprites.TryGetValue(name, out var cached) && cached != null) return cached;
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{TerrainArt}/{name}.png");
            if (s == null)
            {
                // o chão de fora da Fortaleza na estrada (R1, R2): tiles e tufos da biblioteca de cenário
                foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/_Game/Art/Environments" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;
                    s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null) break;
                }
            }
            if (s == null) { s_missing++; Debug.LogWarning($"[Sete Vidas] Floresta F5: falta {name}.png"); return null; }
            s_sprites[name] = s;
            return s;
        }

        /// <summary>Tile (colisão de quadrado cheio) com o sprite; criado uma vez em ScriptableObjects/Environment/Tiles.</summary>
        static Tile TileFor(string spriteName)
        {
            if (s_tiles.TryGetValue(spriteName, out var cached) && cached != null) return cached;
            var sprite = LoadSprite(spriteName);
            if (sprite == null) return null;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(TileFolder));
            string path = $"{TileFolder}/{spriteName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            if (tile.sprite != sprite || tile.colliderType != Tile.ColliderType.Grid)
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            s_tiles[spriteName] = tile;
            return tile;
        }

        /// <summary>Esquece os caches (o menu pode ser rodado de novo depois de regerar a arte).</summary>
        public static void ResetCaches() { s_tiles.Clear(); s_sprites.Clear(); }
    }
}
