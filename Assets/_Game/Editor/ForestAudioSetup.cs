using System.Collections.Generic;
using SeteVidas.Audio;
using UnityEditor;
using UnityEngine;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F10, o som da Floresta numa cena (pedido do usuário, 19/11/2026; Docs/Audio/floresta-audio.md): o diretor de
    /// música com as faixas da Floresta, as áreas de música (ruínas, trecho corrompido, ninho, a Vulpis, a Tobi) e a
    /// ambiência de cada sala. Roda junto do "Floresta: gerar as cenas" (ForestGrayboxSetup) e sozinho pelo menu
    /// "Floresta: som F10 (sem recriar as cenas)".
    /// <para>As músicas são compostas pelo usuário (como as do Treblo na Fortaleza): o diretor procura os arquivos abaixo
    /// em Assets/_Game/Audio/Music e, enquanto um não existe, aquele momento segue com a faixa que já toca (ou com a
    /// ambiência sozinha, que é o pilar da fase: "o silêncio cresce").</para>
    /// </summary>
    public static class ForestAudioSetup
    {
        const string MusicDir = "Assets/_Game/Audio/Music/";
        public const string RootName = "Som da Floresta";

        /// <summary>
        /// Os arquivos de cada momento (os briefings MUS-F01..F14 estão em floresta-audio.md). A faixa da fase de cada cena
        /// vem de <see cref="MainTrack"/>; os outros momentos só tocam quando o arquivo deles existir. O volume de todos é
        /// <see cref="AmbientLevel"/>.
        /// </summary>
        public static readonly (MusicCue cue, string file, float volume, bool resume)[] Tracks =
        {
            (MusicCue.Road, "mus_floresta_estrada", 0.2f, true),           // MUS-F01
            (MusicCue.Explore, "mus_floresta_exploracao", 0.2f, true),     // MUS-F03
            (MusicCue.Ruins, "mus_floresta_ruinas", 0.2f, true),           // MUS-F04
            (MusicCue.Blight, "mus_floresta_corrompida", 0.2f, true),      // MUS-F05
            (MusicCue.Nest, "mus_floresta_ninho", 0.14f, true),            // MUS-F08 (quase silêncio)
            (MusicCue.Boss, "mus_isyathy", 0.16f, false),                  // MUS-F09
            (MusicCue.Vulpis, "mus_vulpis", 0.2f, false),                  // MUS-F10
            (MusicCue.Tobi, "mus_tobi", 0.2f, true),                       // MUS-F11
            (MusicCue.Sanctuary, "mus_floresta_santuario", 0.2f, false),   // MUS-F13 (sem ela, a da Fortaleza)
            (MusicCue.Spirit, "mus_isyathy_espirito", 0.18f, true),        // MUS-F14
        };

        static readonly string[] RuinRooms = { "A5", "B4", "C3", "C4", "C5", "G2" };   // as ruínas dos Primatas (+ a caverna do lago)
        static readonly string[] BlightRooms = { "C7", "F1" };                         // corrompida (o Ninho tem a dele)

        public static void Apply(Transform root, ForestLayout.Region reg)
        {
            var old = root.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(RootName);
            go.transform.SetParent(root, false);
            var missing = new List<string>();
            Music(go.transform, reg, missing);
            int zones = 0;
            foreach (var r in reg.Rooms) zones += Room(go.transform, reg, r) ? 1 : 0;
            Debug.Log($"[Sete Vidas] {reg.Label}: som da Floresta ({zones} zonas de ambiência)" +
                      (missing.Count > 0 ? $"; músicas que faltam (o momento segue a faixa que já toca): {string.Join(", ", missing)}" : "."));
        }

        // ------------------------------------------------------------ música

        static void Music(Transform parent, ForestLayout.Region reg, List<string> missing)
        {
            var go = new GameObject("Música");
            go.transform.SetParent(parent, false);
            var director = go.AddComponent<MusicDirector>();
            var list = new List<MusicDirector.Track>();
            var have = new HashSet<MusicCue>();
            // a faixa da fase em cada cena (pedido do usuário, 19/11/2026)
            string main = MainTrack(reg.Scene);
            var mainClip = Load(main);
            if (mainClip != null) { list.Add(new MusicDirector.Track { Cue = MusicCue.Explore, Clip = mainClip, Volume = AmbientLevel, Resume = true }); have.Add(MusicCue.Explore); }
            else missing.Add(main);
            // a luta com a Isyathy usa a mesma do Ninho: sem faixa de chefe, o diretor segue a que já toca (sem cortar);
            // na morte dela, o silêncio de sempre e depois a faixa volta de onde parou
            if (reg.Scene == ForestLayout.E || reg.Scene == ForestLayout.F) have.Add(MusicCue.Boss);
            // os momentos com faixa própria só entram quando o arquivo existir; sem ele, segue a faixa da fase (a área nem é criada)
            foreach (var t in Tracks)
            {
                if (t.cue == MusicCue.Road || t.cue == MusicCue.Explore || t.cue == MusicCue.Nest || have.Contains(t.cue)) continue;
                if (t.cue == MusicCue.Sanctuary) continue;   // da queda até o Ninho, só a Floresta Silenciosa (sem a do santuário da Fortaleza)
                var clip = Load(t.file);
                if (clip == null) { missing.Add(t.file); continue; }
                list.Add(new MusicDirector.Track { Cue = t.cue, Clip = clip, Volume = AmbientLevel, Resume = t.resume });
                have.Add(t.cue);
            }
            director.Configure(list.ToArray(), Rect.zero);
            EditorUtility.SetDirty(director);

            // as áreas de música (só as que têm faixa): ruínas, corrompida; a Vulpis no posto dela e a Tobi na beira do abismo
            foreach (var r in reg.Rooms)
            {
                MusicCue cue = reg.Scene == ForestLayout.E || reg.Scene == ForestLayout.F ? MusicCue.None
                             : System.Array.IndexOf(RuinRooms, r.Id) >= 0 ? MusicCue.Ruins
                             : System.Array.IndexOf(BlightRooms, r.Id) >= 0 ? MusicCue.Blight : MusicCue.None;
                if (cue != MusicCue.None && have.Contains(cue)) Area(parent, r.Id + ": música", cue, RoomRect(r));
            }
            foreach (var o in reg.Objects)
            {
                if (o.Kind == "vulpis" && have.Contains(MusicCue.Vulpis)) Area(parent, "Vulpis: música", MusicCue.Vulpis, new Rect(o.X - 10f, o.Y - 3f, 20f, 12f));
                if (o.Kind == "tobi" && have.Contains(MusicCue.Tobi)) Area(parent, "Tobi: música", MusicCue.Tobi, new Rect(o.X - 14f, o.Y - 4f, 28f, 14f));
            }
        }

        /// <summary>
        /// O volume das faixas da Floresta: na altura da ambiência (pedido do usuário: "em som ambiente"). É o mesmo nível
        /// de fundo aprovado na Fortaleza (0,2 × o ganho geral 0,5 da trilha), um pouco abaixo.
        /// </summary>
        public const float AmbientLevel = 0.1f;   // 19/11/2026: era 0,18 (pedido do usuário: "abaixe mais o som da música da floresta")

        /// <summary>
        /// A faixa da fase por cena (pedido do usuário, 19/11/2026): a Estrada de Soluki com "Guardian of the Forbidden
        /// Tome"; da queda da ponte até o Ninho (A a D) "Silent Forest"; o Ninho, o despertar da serpente e a luta com ela
        /// (E e F) "Ancient Grove of Shadows".
        /// </summary>
        public static string MainTrack(string scene)
        {
            if (scene == ForestLayout.Road) return "mus_floresta_estrada";     // Guardian of the Forbidden Tome
            if (scene == ForestLayout.E || scene == ForestLayout.F) return "mus_floresta_ninho";   // Ancient Grove of Shadows
            return "mus_floresta_exploracao";                                   // Silent Forest
        }

        static AudioClip Load(string file)
        {
            string path = MusicDir + file + ".wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { path = MusicDir + file + ".ogg"; clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path); }
            if (clip != null) FortressSetup.SetMusicImport(path);
            return clip;
        }

        static void Area(Transform parent, string name, MusicCue cue, Rect rect)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = rect.center;
            go.AddComponent<MusicArea>().Configure(cue, rect.size);
        }

        static Rect RoomRect(ForestLayout.Room r) => new Rect(r.InX0, r.InY0, r.InX1 - r.InX0 + 1, r.InY1 - r.InY0 + 1);

        // ------------------------------------------------------------ ambiência

        /// <summary>O loop, o volume e os sons pontuais de cada sala. Regra pura (testada no EditMode).</summary>
        public static (string loop, float volume, string[] spots) AmbienceFor(string scene, string roomId, int nestIndex, int nestCount)
        {
            // a cabeceira leste (R5, as cachoeiras e o portão de Miaravéu): o chiado do vento soava como a água alta demais
            // (pedido do usuário, 19/11/2026: "o barulho da cachoeira está muito alto"), então ali ela fica bem mais baixa
            if (scene == ForestLayout.Road) return ("amb_estrada", roomId == "R5" ? 0.35f : 1f, new[] { "amb_galho_cai" });
            if (scene == ForestLayout.E)
            {
                // o silêncio cresce: cada sala do Ninho mais quieta que a anterior (de 0,9 a 0,35)
                float k = nestCount <= 1 ? 0f : nestIndex / (float)(nestCount - 1);
                return ("amb_ninho", Mathf.Lerp(0.9f, 0.35f, k), new string[0]);
            }
            if (System.Array.IndexOf(BlightRooms, roomId) >= 0) return ("amb_floresta_corrompida", 0.9f, new string[0]);
            if (roomId == "C4") return ("amb_floresta_ruinas", 0.8f, new[] { "amb_sapo" });   // a caverna do lago
            if (System.Array.IndexOf(RuinRooms, roomId) >= 0) return ("amb_floresta_ruinas", 0.9f, new[] { "amb_maquina_bip" });
            // perto do Ninho (C6 e a região D), a floresta já perde os bichos: só a coruja de vez em quando
            bool nearNest = roomId.StartsWith("D") || roomId == "C6";
            return ("amb_floresta_viva", nearNest ? 0.75f : 1f, nearNest ? new[] { "amb_galho_cai" } : new[] { "amb_coruja", "amb_galho_cai" });
        }

        static bool Room(Transform parent, ForestLayout.Region reg, ForestLayout.Room r)
        {
            int nestIndex = reg.Rooms.IndexOf(r);
            var (loop, volume, spots) = AmbienceFor(reg.Scene, r.Id, nestIndex, reg.Rooms.Count);
            if (string.IsNullOrEmpty(loop)) return false;
            var go = new GameObject(r.Id + ": ambiência");
            go.transform.SetParent(parent, false);
            // a área inclui as paredes: passando de uma sala para outra, a troca acontece na porta
            go.AddComponent<AmbienceZone>().Configure(loop, volume, new Rect(r.X, r.Y, r.W, r.H), spots);
            return true;
        }

        // ------------------------------------------------------------ menu

        /// <summary>Menu: põe/atualiza o som em todas as cenas da Floresta, sem recriar nada (abre, aplica e salva cada uma).</summary>
        public static void PatchScenes()
        {
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var reg in ForestLayout.Regions())
            {
                string path = $"{ForestTestSetup.Folder}/{reg.Scene}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
                Transform root = null;
                foreach (var g in scene.GetRootGameObjects())
                    if (g.transform.Find(reg.Rooms.Count > 0 ? reg.Rooms[0].Id : "") != null) { root = g.transform; break; }
                if (root == null) { Debug.LogWarning($"[Sete Vidas] Som: não achei a raiz de {reg.Scene}."); continue; }
                Apply(root, reg);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
