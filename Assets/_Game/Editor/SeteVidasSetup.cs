using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SeteVidas.Core;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Automação da Fase 0 (README, seção Fase 0). Menu: Tools > Sete Vidas.
    /// Todas as etapas podem ser executadas de novo sem estragar nada (idempotentes).
    /// </summary>
    public static class SeteVidasSetup
    {
        const string Menu = "Tools/Sete Vidas/";
        const string PlaceholderFolder = "Assets/_Game/Art/Environments/Placeholder";

        static readonly string[] Folders =
        {
            "Art/Characters", "Art/Environments/Placeholder", "Art/Enemies", "Art/UI",
            "Audio/Music", "Audio/SFX", "Animations",
            "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Interactables",
            "Scenes/MainMenu", "Scenes/Prologue", "Scenes/World", "Scenes/Dev",
            "Scripts/Core", "Scripts/Player", "Scripts/Classes", "Scripts/Combat", "Scripts/Abilities",
            "Scripts/Enemies", "Scripts/World", "Scripts/Progression", "Scripts/Save", "Scripts/UI",
            "ScriptableObjects", "Settings/Input", "Tests/EditMode", "Tests/PlayMode", "Editor"
        };

        // ------------------------------------------------------------------ menu

        [MenuItem(Menu + "Executar Fase 0 completa", priority = 0)]
        public static void RunAll()
        {
            ConfigureProject();
            CreateScenes();
            PlayerSetup.CreatePlayerAndCamera(); // Fases 1 e 2 (prefab com escalada e adagas)
            InstallPackages(); // por último: a instalação recompila o projeto
        }

        [MenuItem(Menu + "1. Configurar projeto (pastas, layers, colisões, input)", priority = 20)]
        public static void ConfigureProject()
        {
            CreateFolders();
            ConfigureLayers();
            ConfigureCollisionMatrix();
            ConfigureEditorAndInput();
            AssetDatabase.SaveAssets();
            Debug.Log("[Sete Vidas] Projeto configurado: pastas, layers 8–14, matriz de colisão e serialização.");
        }

        [MenuItem(Menu + "2. Criar cenas (Playground + Fortress_MVP)", priority = 21)]
        public static void CreateScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateFolders();
            if (LayerMask.NameToLayer(GameConstants.Layers.Ground) < 0) ConfigureProject();

            CreatePlaceholderTiles(); // garante os assets; a Playground os recarrega depois de abrir a cena
            CreatePlayground();
            PlayerSetup.PlaceInPlaygroundIfReady(); // a Playground é recriada; recoloca jogador e câmera
            CreateFortressScene();
            RegisterScenesInBuild();
            EditorSceneManager.OpenScene(GameConstants.PlaygroundScenePath);
            Debug.Log("[Sete Vidas] Cenas criadas. Playground aberta.");
        }

        [MenuItem(Menu + "3. Instalar pacotes que faltam (Cinemachine)", priority = 22)]
        public static void InstallPackages()
        {
            s_addRequest = Client.AddAndRemove(new[] { "com.unity.cinemachine" }, null);
            EditorApplication.update += WaitForPackages;
            Debug.Log("[Sete Vidas] Instalando Cinemachine… aguarde a recompilação.");
        }

        // ------------------------------------------------------------- pacotes

        static AddAndRemoveRequest s_addRequest;

        static void WaitForPackages()
        {
            if (s_addRequest == null || !s_addRequest.IsCompleted) return;
            EditorApplication.update -= WaitForPackages;
            if (s_addRequest.Status == StatusCode.Success)
                Debug.Log("[Sete Vidas] Pacotes instalados.");
            else
                Debug.LogError("[Sete Vidas] Falha ao instalar pacotes: " + s_addRequest.Error?.message);
            s_addRequest = null;
        }

        // ------------------------------------------------------- configuração

        static void CreateFolders()
        {
            foreach (var f in Folders)
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Game", f));
            AssetDatabase.Refresh();
        }

        static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < GameConstants.Layers.All.Length; i++)
            {
                int index = GameConstants.Layers.FirstIndex + i;
                string wanted = GameConstants.Layers.All[i];
                var slot = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(slot.stringValue) || slot.stringValue == wanted)
                    slot.stringValue = wanted;
                else
                    Debug.LogWarning($"[Sete Vidas] Layer {index} já se chama '{slot.stringValue}'. Esperado '{wanted}'. Ajuste manualmente.");
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureCollisionMatrix()
        {
            int enemy = LayerMask.NameToLayer(GameConstants.Layers.Enemy);
            int interactable = LayerMask.NameToLayer(GameConstants.Layers.Interactable);
            int player = LayerMask.NameToLayer(GameConstants.Layers.Player);
            if (enemy < 0 || interactable < 0 || player < 0) return;

            // Inimigos atravessam uns aos outros (patrulhas não se travam).
            Physics2D.IgnoreLayerCollision(enemy, enemy, true);

            // O gato atravessa inimigos (para chegar às costas deles); só os golpes machucam.
            Physics2D.IgnoreLayerCollision(player, enemy, true);

            // Interativos (santuários, gatilhos, NPCs) só detectam o jogador.
            for (int layer = 0; layer < 32; layer++)
                Physics2D.IgnoreLayerCollision(interactable, layer, layer != player);

            var physics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (physics.Length > 0) EditorUtility.SetDirty(physics[0]);
        }

        static void ConfigureEditorAndInput()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;

            // 0 = Input Manager antigo, 1 = Input System, 2 = ambos.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler != null && handler.intValue == 0)
            {
                handler.intValue = 1;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("[Sete Vidas] Input System ativado. Reinicie o Unity para aplicar.");
            }
        }

        // ------------------------------------------------------------ tiles

        struct PlaceholderTiles { public Tile Ground; public Tile Climbable; public Tile Background; }

        static PlaceholderTiles CreatePlaceholderTiles()
        {
            return new PlaceholderTiles
            {
                Ground = MakeTile("tile_ground_placeholder", new Color32(0x5a, 0x55, 0x6e, 0xff), new Color32(0x3a, 0x36, 0x4a, 0xff), false),
                Climbable = MakeTile("tile_climbable_placeholder", new Color32(0x5a, 0x55, 0x6e, 0xff), new Color32(0x3a, 0x36, 0x4a, 0xff), true),
                Background = MakeTile("tile_background_placeholder", new Color32(0x24, 0x22, 0x33, 0xff), new Color32(0x1c, 0x1a, 0x28, 0xff), false),
            };
        }

        /// <summary>
        /// Gera um PNG 16×16 com borda escura. Parede escalável: a mesma pedra do chão com ranhuras de
        /// apoio e hera seca (linguagem visual do gate do Ladino). O PNG é regravado se o desenho do código
        /// mudar, então não edite estes placeholders à mão.
        /// </summary>
        static Tile MakeTile(string name, Color32 fill, Color32 border, bool climbable)
        {
            string pngPath = $"{PlaceholderFolder}/{name}.png";
            string tilePath = $"{PlaceholderFolder}/{name}.asset";

            int size = GameConstants.TileSize;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                pixels[y * size + x] = edge ? border : fill;
            }
            if (climbable) PaintClimbable(pixels, size, border);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string fullPath = Path.GetFullPath(pngPath);
            bool changed = !File.Exists(fullPath) || !File.ReadAllBytes(fullPath).AsSpan().SequenceEqual(png);
            if (changed || AssetDatabase.LoadAssetAtPath<Sprite>(pngPath) == null)
            {
                File.WriteAllBytes(fullPath, png);
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            }

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        /// <summary>Ranhuras de apoio (sombra + brilho) e dois ramos de hera seca descendo pela pedra.</summary>
        static void PaintClimbable(Color32[] px, int size, Color32 groove)
        {
            var grooveLight = new Color32(0x77, 0x72, 0x8c, 0xff);
            var vine = new Color32(0x4f, 0x5a, 0x2e, 0xff);
            var leaf = new Color32(0x8a, 0x9a, 0x4e, 0xff);
            void Set(int x, int y, Color32 c) { if (x > 0 && x < size - 1 && y > 0 && y < size - 1) px[y * size + x] = c; }

            // Ranhuras: onde as patas apoiam
            for (int x = 4; x <= 8; x++) { Set(x, 4, groove); Set(x, 5, grooveLight); }
            for (int x = 8; x <= 12; x++) { Set(x, 11, groove); Set(x, 12, grooveLight); }

            // Hera seca: ramos que ondulam de cima a baixo, com folhas alternadas
            for (int y = 1; y < size - 1; y++)
            {
                int wobble = (y / 3) % 2;
                int left = 2 + wobble, right = 13 - wobble;
                Set(left, y, vine);
                Set(right, y, vine);
                if (y % 4 == 1) Set(left + 1, y, leaf);
                if (y % 4 == 3) Set(right - 1, y, leaf);
            }
        }

        // ------------------------------------------------------------ cenas

        // Copia a cena-modelo do template Universal 2D (já tem câmera e Global Light 2D configuradas).
        static Scene NewSceneFromTemplate(string path)
        {
            if (File.Exists(Path.GetFullPath(path)))
                AssetDatabase.DeleteAsset(path);

            string[] templates =
            {
                "Assets/Settings/Scenes/URP2DSceneTemplate.unity",
                "Assets/Scenes/SampleScene.unity"
            };
            foreach (var t in templates)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(t) != null && AssetDatabase.CopyAsset(t, path))
                    return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }

            Debug.LogWarning("[Sete Vidas] Cena-modelo não encontrada. Criando cena vazia: adicione uma Global Light 2D manualmente.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            return scene;
        }

        static Camera FindCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var cam = root.GetComponentInChildren<Camera>();
                if (cam != null) return cam;
            }
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.AddComponent<AudioListener>();
            return go.AddComponent<Camera>();
        }

        static void SetupPixelPerfectCamera(Camera cam, Vector2 position)
        {
            cam.orthographic = true;
            cam.orthographicSize = GameConstants.ReferenceHeight / 2f / GameConstants.PixelsPerUnit;
            cam.backgroundColor = new Color32(0x14, 0x12, 0x1e, 0xff);
            cam.transform.position = new Vector3(position.x, position.y, -10f);

            var type = FindType("UnityEngine.Rendering.Universal.PixelPerfectCamera");
            if (type == null)
            {
                Debug.LogWarning("[Sete Vidas] PixelPerfectCamera (URP) não encontrada. Adicione-a manualmente na câmera.");
                return;
            }
            var ppc = cam.GetComponent(type);
            if (ppc == null) ppc = cam.gameObject.AddComponent(type);
            SetMember(ppc, "assetsPPU", "m_AssetsPPU", GameConstants.PixelsPerUnit);
            SetMember(ppc, "refResolutionX", "m_RefResolutionX", GameConstants.ReferenceWidth);
            SetMember(ppc, "refResolutionY", "m_RefResolutionY", GameConstants.ReferenceHeight);
        }

        // O assembly da PixelPerfectCamera muda entre versões do URP; procura pelo nome entre os componentes.
        static Type FindType(string fullName)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                if (type.FullName == fullName) return type;
            return null;
        }

        static void SetMember(Component target, string property, string serializedName, int value)
        {
            var prop = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite) { prop.SetValue(target, value); return; }

            var so = new SerializedObject(target);
            var sp = so.FindProperty(serializedName);
            if (sp != null) { sp.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); return; }

            Debug.LogWarning($"[Sete Vidas] Não consegui definir {property} = {value} na {target.GetType().Name}. Ajuste no Inspector.");
        }

        static Tilemap CreateTilemap(Transform grid, string name, string physicsLayer, int sortingOrder, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid, false);
            if (physicsLayer != null) go.layer = LayerMask.NameToLayer(physicsLayer);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            if (collider) MergeTileColliders(go.AddComponent<TilemapCollider2D>());
            return map;
        }

        /// <summary>
        /// Une os tiles num só colisor: sem emendas onde o gato possa enganchar ao andar.
        /// Retorna true se mudou algo (para cenas antigas que ainda não tinham).
        /// </summary>
        static bool MergeTileColliders(TilemapCollider2D tilemapCollider)
        {
            var go = tilemapCollider.gameObject;
            if (go.GetComponent<CompositeCollider2D>() != null &&
                tilemapCollider.compositeOperation == Collider2D.CompositeOperation.Merge)
                return false;

            var body = go.GetComponent<Rigidbody2D>();
            if (body == null) body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null) composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            return true;
        }

        /// <summary>
        /// Força a montagem das formas de colisão dos tilemaps antes de salvar a cena.
        /// O Unity só gera o CompositeCollider2D um instante depois de pintar os tiles; salvando antes,
        /// a cena guarda um colisor vazio e o chão não colide durante o jogo.
        /// </summary>
        static void BuildTileColliders(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            foreach (var tc in root.GetComponentsInChildren<TilemapCollider2D>(true))
            {
                tc.ProcessTilemapChanges();
                var composite = tc.GetComponent<CompositeCollider2D>();
                if (composite == null) continue;
                composite.GenerateGeometry();
                if (composite.pathCount == 0 && tc.GetComponent<Tilemap>().GetUsedTilesCount() > 0)
                    Debug.LogError($"[Sete Vidas] {scene.name}/{tc.name}: colisor unificado ficou vazio. O chão não vai colidir.");
            }
        }

        static Transform CreateGrid()
        {
            var grid = new GameObject("Grid");
            grid.AddComponent<Grid>().cellSize = Vector3.one; // 1 célula = 1 tile de 16 px
            return grid.transform;
        }

        static void Fill(Tilemap map, TileBase tile, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static void Clear(Tilemap map, int x0, int y0, int x1, int y1) => Fill(map, null, x0, y0, x1, y1);

        /// <summary>
        /// Layout em células (1 célula = 1 tile). O chão fica na linha y = -1 (superfície em y = 0).
        /// Detalhes e o motivo de cada trecho estão em Docs/Level/playground.md.
        /// </summary>
        static void CreatePlayground()
        {
            var scene = NewSceneFromTemplate(GameConstants.PlaygroundScenePath);
            SetupPixelPerfectCamera(FindCamera(scene), new Vector2(8f, 4f));

            // Abrir a cena descarrega assets sem uso da memória, inclusive tiles criados antes.
            // Por isso os tiles são carregados só agora, depois de a cena estar aberta.
            var tiles = CreatePlaceholderTiles();
            if (tiles.Ground == null || tiles.Climbable == null || tiles.Background == null)
            {
                Debug.LogError("[Sete Vidas] Tiles placeholder não carregaram. A Playground ficaria vazia.");
                return;
            }

            var grid = CreateGrid();
            var background = CreateTilemap(grid, "Background", null, -10, false);
            var ground = CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true);
            var climbable = CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 0, true);
            CreateTilemap(grid, "Foreground", null, 10, false);

            // Fundo decorativo (sem colisão)
            Fill(background, tiles.Background, -74, 0, 220, 13);

            // Chão contínuo (2 tiles de espessura) e paredes de limite
            Fill(ground, tiles.Ground, -76, -2, 222, -1);
            Fill(ground, tiles.Ground, -76, 0, -75, 14); // limite esquerdo (depois do circuito de armadilhas)

            // K) Circuito de armadilhas (à esquerda do spawn, x -74..-8). Tiles aqui; armadilhas em TrapsSetup.
            Fill(ground, tiles.Ground, -22, 1, -22, 6);   // pilar do teto: passagem de 1 tile embaixo (agachar)
            Fill(ground, tiles.Ground, -34, 2, -33, 2);   // plataformas acima do fogo (topo em 3)
            Fill(ground, tiles.Ground, -39, 2, -38, 2);
            Fill(ground, tiles.Ground, -44, 2, -43, 2);
            Fill(ground, tiles.Ground, -55, 2, -52, 2);   // plataforma acima da serra
            TrapsSetup.PlaceInPlayground(scene);
            Fill(ground, tiles.Ground, 221, 0, 222, 16); // limite direito

            // A) Spawn: x -4..6 livre.
            // B) Degraus de 1 a 6 tiles de altura, 2 de largura, a cada 4 tiles.
            //    1 e 2: blocos maciços (qualquer pulo vence).
            //    3 a 6: plataformas de 1 tile de espessura, com o topo na altura h. O gato passa por
            //    baixo andando, então não há vão onde ficar preso nem bloqueio no caminho de volta.
            for (int h = 1; h <= 6; h++)
            {
                int x = 4 + 4 * h; // 8, 12, 16, 20, 24, 28
                int bottom = h <= 2 ? 0 : h - 1;
                Fill(ground, tiles.Ground, x, bottom, x + 1, h - 1);
            }

            // C) Buracos de 2, 3, 4 e 5 tiles; fundo raso (2 tiles abaixo do chão) para não prender o jogador
            int[] holeStarts = { 36, 42, 49, 57 };
            int[] holeWidths = { 2, 3, 4, 5 };
            for (int i = 0; i < holeStarts.Length; i++)
            {
                int x0 = holeStarts[i], x1 = x0 + holeWidths[i] - 1;
                Clear(ground, x0, -2, x1, -1);
                Fill(ground, tiles.Ground, x0, -4, x1, -3);
            }

            // D) Passagem baixa (vão de 1 tile): exige agachar. x 68..75, teto em y 1..3
            Fill(ground, tiles.Ground, 68, 1, 75, 3);

            // E) Parede escalável de 10 tiles + plataforma no topo
            Fill(climbable, tiles.Climbable, 80, 0, 81, 9);
            Fill(ground, tiles.Ground, 82, 9, 86, 9);

            // F) Chaminé (Fase 2): duas paredes escaláveis frente a frente, vão de 3 tiles (x 94..96).
            //    Esquerda: x 92..93, do chão até y 6 (topo em 7). Direita: x 97..98, de y 3 a 10 (topo em 11),
            //    começa no ar para treinar o pulo de uma parede para a outra. Plataforma de saída x 99..104 (topo em 11).
            Fill(climbable, tiles.Climbable, 92, 0, 93, 6);
            Fill(climbable, tiles.Climbable, 97, 3, 98, 10);
            Fill(ground, tiles.Ground, 99, 10, 104, 10);

            // G) Bonecos de treino (Fase 2): x 108..118, chão livre. Ficam como prefabs, não tiles.
            CombatSetup.PlaceDummies(scene);

            // H) Furtividade (Fase 3): x 122..151. Sentinela patrulhando x 133–140; sombras em x 122–130 e 144–152.
            //    Rota por cima: parede escalável x 128–129 (dentro da sombra A, topo em 5) e passarela x 130..143
            //    (topo em 5). A passarela bloqueia a visão da Sentinela. Cair dela (5 tiles) faz barulho;
            //    pendurar na ponta e soltar (3,75) não. Embaixo há 4 tiles de altura livre.
            Fill(climbable, tiles.Climbable, 128, 0, 129, 4);
            Fill(ground, tiles.Ground, 130, 4, 143, 4);
            EnemySetup.PlaceStealthSection(scene);

            // Fase 4: HUD de corações e santuários (E para descansar e salvar) em x 3 e x 119,5
            ProgressionSetup.PlaceInPlayground(scene);

            // I) Paladino (Fase 5): x 154..177. Túnel embaixo de uma laje (x 156..177, y 4, topo em 5; 4 tiles de
            //    altura livre; encosta no limite direito, sem fresta). A parede rachada (x 160, y 0..3) fecha o túnel: é vista antes de ter a marreta.
            //    O Golden fica em cima da laje (x 168,5): sobe agarrando a borda de 5 tiles em x 156.
            Fill(ground, tiles.Ground, 156, 4, 220, 4); // laje contínua até a arena (sem fresta para cair no túnel)
            Fill(ground, tiles.Ground, 178, 0, 220, 3); // maciço embaixo da arena: o túnel termina em x 177
            ClassSetup.PlaceInPlayground(scene);

            // J) Chefão (Golden): arena x 180..210 (chão em 5, teto em 14) e sala do tomo x 213..218.
            //    Passagens de 4 tiles (y 5..8) com grades em x 179 (entrada) e x 211 (tomo).
            Fill(ground, tiles.Ground, 178, 9, 179, 14);  // parede da entrada, acima da grade
            Fill(ground, tiles.Ground, 180, 14, 210, 14); // teto da arena
            Fill(ground, tiles.Ground, 211, 9, 212, 14);  // parede do tomo, acima da grade
            Fill(ground, tiles.Ground, 213, 10, 220, 14); // teto da sala do tomo
            Fill(ground, tiles.Ground, 219, 5, 220, 9);   // fundo da sala do tomo
            BossSetup.PlaceInPlayground(scene);

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(0.5f, 1f, 0f);

            BuildTileColliders(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void CreateFortressScene()
        {
            if (File.Exists(Path.GetFullPath(GameConstants.FortressScenePath)))
            {
                // Não sobrescreve trabalho de level design já feito; só atualiza os colisores dos tilemaps.
                var existing = EditorSceneManager.OpenScene(GameConstants.FortressScenePath, OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in existing.GetRootGameObjects())
                foreach (var tc in root.GetComponentsInChildren<TilemapCollider2D>(true))
                    changed |= MergeTileColliders(tc);
                if (changed)
                {
                    BuildTileColliders(existing);
                    EditorSceneManager.MarkSceneDirty(existing);
                    EditorSceneManager.SaveScene(existing);
                    Debug.Log("[Sete Vidas] Fortress_MVP: colisores dos tilemaps unificados (conteúdo preservado).");
                }
                return;
            }

            var scene = NewSceneFromTemplate(GameConstants.FortressScenePath);
            SetupPixelPerfectCamera(FindCamera(scene), Vector2.zero);

            var grid = CreateGrid();
            CreateTilemap(grid, "Background", null, -10, false);
            CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true);
            CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 0, true);
            CreateTilemap(grid, "Hazard", GameConstants.Layers.Hazard, 0, true);
            CreateTilemap(grid, "Foreground", null, 10, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Tela inicial primeiro (a build abre nela), depois as fases. Outras cenas ficam na lista, desligadas.</summary>
        public static void RegisterScenesInBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (File.Exists(Path.GetFullPath(GameConstants.MainMenuScenePath)))
                scenes.Add(new EditorBuildSettingsScene(GameConstants.MainMenuScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(GameConstants.PlaygroundScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(GameConstants.FortressScenePath, true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != GameConstants.PlaygroundScenePath && existing.path != GameConstants.FortressScenePath &&
                    existing.path != GameConstants.MainMenuScenePath)
                    scenes.Add(new EditorBuildSettingsScene(existing.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
