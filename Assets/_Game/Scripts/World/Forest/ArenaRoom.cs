using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Enemies;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Sala que vira arena (pedido do usuário, 13/11/2026): ao entrar, os portões fecham; derrotados todos os inimigos
    /// de dentro, abrem para sempre (flag "arena_vencida_&lt;id&gt;"). Morrer lá dentro reabre a arena e os inimigos
    /// voltam (reinício do mundo). Vencida, os inimigos dela não voltam mais.
    /// Levas: cada <see cref="Wave"/> fica desligada e entra quando a anterior morre (a arena só abre depois da última).
    /// Os morcegos que um Broto solta lá dentro também contam.
    /// </summary>
    [SelectionBase]
    public class ArenaRoom : MonoBehaviour
    {
        [System.Serializable]
        public class Wave
        {
            [Tooltip("Inimigos desta leva (desligados até ela entrar)")]
            public GameObject[] members = new GameObject[0];
        }

        [SerializeField] string arenaId = "arena";
        [Tooltip("Área da arena (tiles), centrada neste objeto: entrar nela começa a luta")]
        [SerializeField] Vector2 size = new Vector2(30f, 12f);
        [SerializeField] ArenaGate[] gates = new ArenaGate[0];
        [Tooltip("Vazio = todos os inimigos dentro da área (fora das levas)")]
        [SerializeField] List<EnemyAI> enemies = new List<EnemyAI>();
        [SerializeField] List<ForestEnemy> forestEnemies = new List<ForestEnemy>();
        [Tooltip("Levas seguintes: entram quando todos os de antes morrem")]
        [SerializeField] Wave[] waves = new Wave[0];
        [Tooltip("Pausa entre o fim de uma leva e a entrada da próxima (s)")]
        [SerializeField, Min(0f)] float waveDelay = 0.9f;
        [Tooltip("Ligado quando a arena é vencida (um baú, uma passagem)")]
        [SerializeField] GameObject reward;

        ArenaRules.State _state;
        int _nextWave;
        float _waveTimer = -1f;
        public ArenaRules.State State => _state;
        public int WavesStarted => _nextWave;
        public Rect Area => new Rect((Vector2)transform.position - size * 0.5f, size);

        public void Configure(string id, Vector2 areaSize, ArenaGate[] arenaGates, GameObject rewardObject)
        {
            arenaId = id;
            size = areaSize;
            gates = arenaGates;
            reward = rewardObject;
        }

        /// <summary>As levas seguintes (cada array é uma leva).</summary>
        public void SetWaves(params GameObject[][] groups)
        {
            waves = new Wave[groups.Length];
            for (int i = 0; i < groups.Length; i++) waves[i] = new Wave { members = groups[i] };
        }

        void OnEnable() => GameEvents.WorldReset += OnWorldReset;
        void OnDisable() => GameEvents.WorldReset -= OnWorldReset;

        void Start()
        {
            HideWaves(); // antes de procurar: as levas não entram na lista da primeira
            if (enemies.Count == 0)
                foreach (var e in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
                    if (e.gameObject.scene == gameObject.scene && Area.Contains(e.transform.position)) enemies.Add(e);
            if (forestEnemies.Count == 0)
                foreach (var e in FindObjectsByType<ForestEnemy>(FindObjectsSortMode.None))
                    if (e.gameObject.scene == gameObject.scene && Area.Contains(e.transform.position)) forestEnemies.Add(e);

            bool cleared = SaveManager.Data != null && SaveManager.Data.HasFlag(ArenaRules.ClearedFlag(arenaId));
            _state = cleared ? ArenaRules.State.Cleared : ArenaRules.State.Waiting;
            foreach (var g in gates) if (g != null) g.SetImmediate(false);
            if (reward != null) reward.SetActive(cleared);
            if (cleared) HideEnemies();
        }

        int Alive()
        {
            int n = 0;
            foreach (var e in enemies) if (e != null && e.gameObject.activeInHierarchy && !e.IsDead) n++;
            // os da lista, os das levas e os morcegos soltos lá dentro (todos os vivos da área)
            var area = Area;
            foreach (var e in ForestEnemy.All)
                if (e != null && !e.IsDead && e.gameObject.activeInHierarchy && e.gameObject.scene == gameObject.scene
                    && (forestEnemies.Contains(e) || area.Contains(e.transform.position))) n++;
            return n;
        }

        void Update()
        {
            if (_state == ArenaRules.State.Cleared) return;
            var h = PlayerHealth.Active;
            bool dead = h == null || h.IsDead;
            // só começa com o gato bem dentro (1 tile além da borda): não fecha o portão em cima dele
            var inner = Area;
            inner.xMin += 1.5f; inner.xMax -= 1.5f;
            bool inside = h != null && inner.Contains(h.transform.position);

            int alive = Alive();
            if (_state == ArenaRules.State.Fighting && !dead && alive == 0 && _nextWave < waves.Length)
            {
                // a leva acabou e ainda tem outra: pausa curta, as faíscas avisam onde vão sair, e entram
                if (_waveTimer < 0f) { _waveTimer = waveDelay; Warn(waves[_nextWave]); }
                _waveTimer -= Time.deltaTime;
                if (_waveTimer <= 0f) { StartWave(waves[_nextWave], h); _nextWave++; _waveTimer = -1f; }
                return; // a arena não abre enquanto houver leva por vir
            }

            var next = ArenaRules.Step(_state, inside, dead, alive);
            if (next == _state) return;
            if (next == ArenaRules.State.Fighting) Begin(h);
            else if (next == ArenaRules.State.Cleared) Clear(h);
            else Reopen();
            _state = next;
        }

        void Warn(Wave w)
        {
            foreach (var m in w.members)
                if (m != null)
                    HitSparks.Burst((Vector2)m.transform.position + Vector2.up * 0.6f, 8, new Color32(0x7a, 0x9a, 0x4a, 0xff),
                        new Color32(0x2e, 0x3a, 0x22, 0xff), 1.5f, 4f, 0.5f, 20f, 0.4f);
        }

        void StartWave(Wave w, PlayerHealth h)
        {
            foreach (var m in w.members)
            {
                if (m == null) continue;
                m.SetActive(true);
                foreach (var e in m.GetComponents<ForestEnemy>()) e.ReviveNow();
            }
            if (h != null && _nextWave == 0) SpeechBubble.Show(h.transform, "Claro. Tinha mais.", 2f);
        }

        void Begin(PlayerHealth h)
        {
            foreach (var g in gates) if (g != null) g.Close();
            if (h != null) SpeechBubble.Show(h.transform, "Ah, ótimo. Uma festa.", 2f);
        }

        void Clear(PlayerHealth h)
        {
            foreach (var g in gates) if (g != null) g.Open();
            var data = SaveManager.Data;
            if (data != null) { data.SetFlag(ArenaRules.ClearedFlag(arenaId)); SaveManager.Save(); }
            if (reward != null) reward.SetActive(true);
            PixelPopup.Spawn(transform.position + Vector3.up * 2f, "Arena vencida", new Color32(0xff, 0xd3, 0x4d, 0xff));
        }

        void HideWaves()
        {
            foreach (var w in waves)
                if (w != null)
                    foreach (var m in w.members) if (m != null) m.SetActive(false);
            _nextWave = 0;
            _waveTimer = -1f;
        }

        void HideEnemies()
        {
            foreach (var e in enemies) if (e != null) e.gameObject.SetActive(false);
            foreach (var e in forestEnemies) if (e != null) e.gameObject.SetActive(false);
            HideWaves();
        }

        void Reopen()
        {
            foreach (var g in gates) if (g != null) g.SetImmediate(false);
            HideWaves(); // morreu: as levas voltam a esperar (a primeira revive com o reinício do mundo)
        }

        void OnWorldReset()
        {
            // vencida: os inimigos dela não voltam (o reinício do mundo reviveria todos)
            if (_state == ArenaRules.State.Cleared) { HideEnemies(); return; }
            if (_state != ArenaRules.State.Fighting) return;
            _state = ArenaRules.State.Waiting;
            Reopen();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
