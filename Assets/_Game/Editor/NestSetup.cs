using System.Collections.Generic;
using SeteVidas.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F7, o Ninho (pedido do usuário, 17/11/2026, no lugar da fuga): põe a <see cref="IsyathyBurrow"/> na cena do Ninho.
    /// Ela dorme deitada nos patamares da E1 (o trilho do despertar: o corpo do rabo ao pescoço, a cabeça que levanta,
    /// o arco e o mergulho no patamar), e depois ataca de baixo da terra nas salas E1–E3. Os galhos simples da E2 e da E3
    /// viram <see cref="FragileBranch"/> (quebram quando o bote passa e voltam em 8 s).
    /// <para>
    /// Roda junto do "Floresta: gerar as cenas" e sozinho pelo menu "Floresta: Ninho F7 (a Isyathy embaixo da terra, sem
    /// recriar a cena)", que também tira a fuga antiga e o medidor de alerta da cena e os inimigos da E1–E3. Idempotente.
    /// </para>
    /// </summary>
    public static class NestSetup
    {
        public const string RootName = "Isyathy (embaixo da terra)";
        /// <summary>As montagens antigas: o medidor de alerta e a fuga (substituídos em 17/11/2026).</summary>
        public static readonly string[] OldRootNames = { "Isyathy (ninho)", "Isyathy (fuga)" };
        public static readonly string[] AttackRooms = { "E1", "E2", "E3" };
        public const string WakeRoom = "E1";

        /// <summary>
        /// O trilho do despertar na E1 (x e y locais, como no <c>ForestLayout</c>: o chão de dentro é y 0). O corpo deitado
        /// do rabo (no bloco alto, x 36) ao pescoço (na beira do patamar 14..19), passando por cima do vão dos espinhos;
        /// a cabeça descansa na rampa. Depois: levanta e olha para o gato, arqueia e mergulha no patamar (x 16,8), e
        /// continua para o fundo da terra.
        /// </summary>
        static readonly Vector2[] WakePath =
        {
            V(36f, 8.75f), V(33f, 8.8f), V(30.5f, 8.6f), V(28.5f, 7.3f), V(26.5f, 5.3f), V(25f, 4.2f), V(22.8f, 3.5f),
            V(20.5f, 4.2f), V(19f, 4.8f), V(16.5f, 4.75f), V(14.6f, 4.2f),          // [10] o pescoço deitado
            V(13.8f, 5.4f), V(13.2f, 7f), V(11.8f, 7.4f),                             // [13] a cabeça olhando para o gato
            V(12.6f, 9.6f), V(15f, 10.2f), V(16.8f, 8f), V(16.8f, 4f),               // [17] a boca do buraco (o patamar, topo 4)
            V(16.8f, -30f),
        };
        const int RestIndex = 10, LookIndex = 13, HoleIndex = 17;

        /// <summary>Inimigos que saem da E1–E3 (pelos nomes dos objetos): lá, o perigo é ela.</summary>
        public static readonly string[] EnemyWords = { "Templário zumbi", "serpente", "planta carnívora", "morcego" };

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static Vector2 World(ForestLayout.Room r, Vector2 local) => new Vector2(r.X + 1 + local.x, r.Y + 1 + local.y);
        static Rect RoomRect(ForestLayout.Room r) => new Rect(r.X, r.Y, r.W, r.H);

        /// <summary>O galho que vira frágil: o "galho" simples da E2 ou da E3 (não o do buraco do teto, nem o que balança).</summary>
        public static bool IsFragileBranchName(string name) => name == "E2: galho" || name == "E3: galho";

        /// <summary>O trilho do despertar em mundo.</summary>
        public static Vector2[] WakePathWorld(ForestLayout.Region reg)
        {
            var r = reg.FindRoom(WakeRoom);
            var path = new Vector2[WakePath.Length];
            for (int i = 0; i < path.Length; i++) path[i] = World(r, WakePath[i]);
            return path;
        }

        /// <summary>Monta a Isyathy embaixo de <paramref name="root"/> (a raiz da cena gerada). Devolve quantos galhos ficaram frágeis.</summary>
        public static int Apply(Transform root, ForestLayout.Region reg)
        {
            foreach (var name in new List<string>(OldRootNames) { RootName })
            {
                var old = root.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var rooms = new List<Rect>();
            foreach (var id in AttackRooms)
            {
                var r = reg.FindRoom(id);
                if (r == null) { Debug.LogError($"[Sete Vidas] Ninho: falta a sala {id} no ForestLayout."); continue; }
                rooms.Add(RoomRect(r));
            }
            var wake = reg.FindRoom(WakeRoom);
            var path = WakePathWorld(reg);

            var go = new GameObject(RootName);
            go.transform.SetParent(root, false);
            var isy = go.AddComponent<IsyathyBurrow>();
            isy.Configure(rooms.ToArray(), RoomRect(wake), path, RestIndex, LookIndex, HoleIndex, path[HoleIndex]);
            EditorUtility.SetDirty(isy);

            int fragile = 0;
            foreach (var p in root.GetComponentsInChildren<OneWayPlatform>(true))
            {
                var fb = p.GetComponent<FragileBranch>();
                if (IsFragileBranchName(p.name)) { if (fb == null) p.gameObject.AddComponent<FragileBranch>(); fragile++; }
                else if (fb != null) Object.DestroyImmediate(fb);
            }
            return fragile;
        }

        /// <summary>Tira da cena os inimigos da E1–E3. Devolve quantos.</summary>
        static int RemoveRoomEnemies(UnityEngine.SceneManagement.Scene scene)
        {
            var doomed = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    bool inRoom = false;
                    foreach (var id in AttackRooms) if (n.StartsWith(id + ": ")) inRoom = true;
                    if (!inRoom) continue;
                    foreach (var w in EnemyWords) if (n.Contains(w)) { doomed.Add(t.gameObject); break; }
                }
            int count = 0;
            foreach (var d in doomed) if (d != null) { Object.DestroyImmediate(d); count++; }
            // os templários zumbis nascem no Play a partir dos postos do esquadrão: tira os postos da E1–E3
            foreach (var go in scene.GetRootGameObjects())
                foreach (var squad in go.GetComponentsInChildren<SeteVidas.Enemies.Zombie.ZombieSquad>(true))
                {
                    var so = new SerializedObject(squad);
                    var posts = so.FindProperty("posts");
                    for (int i = posts.arraySize - 1; i >= 0; i--)
                    {
                        string pn = posts.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                        foreach (var id in AttackRooms)
                            if (pn.StartsWith(id + ": ")) { posts.DeleteArrayElementAtIndex(i); count++; break; }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            return count;
        }

        /// <summary>Menu: abre a cena do Ninho (se não estiver aberta), põe/atualiza a Isyathy e salva. Não recria nada.</summary>
        public static void PatchScene()
        {
            string path = $"{ForestTestSetup.Folder}/{ForestLayout.E}.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != path)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(path);
            }
            Transform root = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.transform.Find("E2") != null) { root = go.transform; break; }
            if (root == null)
            {
                Debug.LogError("[Sete Vidas] Ninho: não achei a raiz da cena do Ninho (com o grupo \"E2\"). Gere a Floresta primeiro.");
                return;
            }
            int enemies = RemoveRoomEnemies(scene);
            var reg = ForestLayout.Regions().Find(r => r.Scene == ForestLayout.E);
            int n = Apply(root, reg);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Sete Vidas] Ninho (F7): a Isyathy dorme na E1 e ataca de baixo da terra na E1–E3; {n} galhos frágeis, {enemies} inimigos tirados.");
        }
    }
}
