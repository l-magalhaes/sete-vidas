using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Ponte do Último Marco (Floresta F3, plano §1.3 "cutscene 1"). Pedido do usuário, 17/11/2026: a ponte com o dobro
    /// do tamanho; ao começar a cena, <b>o gato foge sozinho</b> até a ponte cair; e ela <b>só treme quando vários
    /// zumbis estão em cima</b>: é o peso da horda e do herói que derruba a pedra velha.
    /// <list type="number">
    /// <item>O gato pisa na ponte (<see cref="startX"/>): a horda aparece na cabeceira, atrás dele. O controle sai e ele
    /// corre sozinho, olhando para trás, até o meio (<see cref="collapseX"/>), onde para e vira para eles.</item>
    /// <item>Com <see cref="BridgeRules.ZombiesToShake"/> ou mais zumbis em cima da ponte, a pedra treme, mais forte com
    /// mais peso.</item>
    /// <item>Depois de <see cref="BridgeRules.ShakeBeforeCollapse"/> s tremendo, com o gato no meio, o centro desaba (do
    /// meio para as pontas) e cai todo mundo. O gatilho de baixo (flag <see cref="FallenFlag"/>) leva à A1.</item>
    /// </list>
    /// Com a flag no save, a ponte já está caída.
    /// </summary>
    public class BridgeCollapse : MonoBehaviour
    {
        public const string FallenFlag = "ponte_caiu";

        public enum Phase { Waiting, Fleeing, Collapsing, Fallen }

        [Tooltip("Blocos de pedra do trecho que desaba (Rigidbody2D + colisor no Ground)")]
        [SerializeField] Rigidbody2D[] segments = new Rigidbody2D[0];
        [Tooltip("x onde a fuga começa (o gato pisou na ponte)")]
        [SerializeField] float startX;
        [Tooltip("x onde o gato para (o meio, onde a ponte desaba)")]
        [SerializeField] float collapseX;
        [SerializeField] ZombieSquad horde;

        Phase _phase = Phase.Waiting;
        float _time, _shake;
        Vector2[] _rest;
        float _minX, _maxX;
        PlayerInputReader _input;
        PlayerController _player;
        PlayerHealth _health;
        bool _stopped;
        float _stoppedFor;
        readonly List<ZombieAI> _pack = new List<ZombieAI>();

        public Phase Current => _phase;
        /// <summary>Quantos zumbis estão em cima da ponte agora (testes).</summary>
        public int ZombiesOnBridge { get; private set; }

        public void Configure(Rigidbody2D[] blocks, float start, float collapse, ZombieSquad hordeSquad)
        {
            segments = blocks ?? new Rigidbody2D[0];
            startX = start;
            collapseX = collapse;
            horde = hordeSquad;
        }

        void Start()
        {
            _rest = new Vector2[segments.Length];
            _minX = float.MaxValue; _maxX = float.MinValue;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;
                _rest[i] = segments[i].position;
                _minX = Mathf.Min(_minX, _rest[i].x - 1f);
                _maxX = Mathf.Max(_maxX, _rest[i].x + 1f);
            }
            if (SaveManager.Data != null && SaveManager.Data.HasFlag(FallenFlag))
            {
                foreach (var s in segments) if (s != null) s.gameObject.SetActive(false);
                _phase = Phase.Fallen;
            }
        }

        void OnDisable() => Release();

        void Release()
        {
            if (_input != null) { _input.ClearScriptedMove(); _input.SetLock(this, false); }
            _input = null;
            if (_health != null) _health.Shielded = false;
            _health = null;
        }

        void Update()
        {
            var cat = PlayerHealth.Active;
            if (cat == null || cat.IsDead) { if (_phase == Phase.Fleeing) Release(); return; }
            float x = cat.transform.position.x;
            switch (_phase)
            {
                case Phase.Waiting:
                    if (x >= startX) BeginFlight(cat);
                    break;
                case Phase.Fleeing:
                    Flee(cat, x);
                    break;
            }
        }

        void BeginFlight(PlayerHealth cat)
        {
            _phase = Phase.Fleeing;
            _time = 0f; _shake = 0f; _stopped = false;
            if (horde != null) horde.SpawnAll(alerted: true);
            _player = cat.GetComponent<PlayerController>();
            _health = cat;
            cat.Shielded = true;   // a horda colada atrás não fere: o controle está travado
            _stoppedFor = 0f;
            _input = cat.GetComponent<PlayerInputReader>();
            if (_input != null) { _input.SetLock(this, true); _input.SetScriptedMove(BridgeRules.FleeInput); }
            SpeechBubble.Show(cat.transform, "Ah, não. Eles me seguiram.", 2f);
            SeteVidas.Audio.Sfx.Play("zumbi_sente", (Vector2)cat.transform.position + Vector2.left * 6f, 1f, 0.8f);
            CameraShake.Shake(0.6f, 0.4f);
        }

        void Flee(PlayerHealth cat, float x)
        {
            _time += Time.deltaTime;
            // o gato corre até o meio e para, virado para a horda
            if (!_stopped && x >= collapseX)
            {
                _stopped = true;
                if (_input != null) _input.ClearScriptedMove();
                if (_player != null) _player.Face(-1);
                SpeechBubble.Show(cat.transform, "...Será que a ponte aguenta tanto peso?", BridgeRules.LineTime + 0.4f);
            }
            if (_stopped) _stoppedFor += Time.deltaTime;
            Pace(x);
            // o peso: zumbis em cima da ponte (e o gato)
            int count = 0;
            if (horde != null)
                foreach (var z in horde.Zombies)
                    if (z != null && !z.IsDead && OnBridge(z.transform.position)) count++;
            ZombiesOnBridge = count;
            bool heroOn = OnBridge(cat.transform.position);
            float weight = BridgeRules.Weight(count, heroOn);
            if (weight > 0f)
            {
                _shake += Time.deltaTime;
                Rumble(weight);
            }
            else Settle();
            if ((BridgeRules.Collapses(_shake, _stoppedFor) || _time >= BridgeRules.MaxFlight) && _phase == Phase.Fleeing)
                StartCoroutine(Collapse(cat));
        }

        /// <summary>
        /// A horda acelera e freia para chegar amontoada atrás do gato: o mais perto a <see cref="BridgeRules.PackGap"/>(0)
        /// dele, os outros em fila. Assim, quando ele para no meio, a ponte já está cheia de zumbis perto dele.
        /// </summary>
        void Pace(float catX)
        {
            if (horde == null) return;
            _pack.Clear();
            foreach (var z in horde.Zombies) if (z != null && !z.IsDead) _pack.Add(z);
            _pack.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));   // o da frente primeiro
            float catSpeed = _stopped || _player == null ? 0f : Mathf.Max(0f, _player.Body.linearVelocity.x);
            for (int i = 0; i < _pack.Count; i++)
            {
                float gap = catX - _pack[i].transform.position.x;
                _pack[i].ChaseSpeedMultiplier = BridgeRules.PaceMultiplier(gap, BridgeRules.PackGap(i), catSpeed, _pack[i].ChaseSpeed);
            }
        }

        bool OnBridge(Vector2 p) => p.x >= _minX && p.x <= _maxX && p.y > _rest[0].y - 1f && p.y < _rest[0].y + 3f;

        // a pedra treme com o peso: mais zumbis, mais forte (aviso de que vai ceder)
        void Rumble(float weight)
        {
            float j = 0.015f + 0.07f * weight;
            for (int i = 0; i < segments.Length; i++)
            {
                var s = segments[i];
                if (s == null) continue;
                s.position = _rest[i] + new Vector2(0f, Mathf.Sin(Time.time * 40f + i * 1.7f) * j);
            }
            if (Random.value < Time.deltaTime * (1f + 4f * weight))
                SeteVidas.Audio.Sfx.Play("parede_racha", segments.Length > 0 && segments[0] != null ? segments[0].position : (Vector2)transform.position, 0.6f, 0.8f);
            if (Random.value < Time.deltaTime * 3f * weight) CameraShake.Shake(0.5f + weight, 0.2f);
            if (Random.value < Time.deltaTime * 6f * weight && segments.Length > 0)
            {
                var s = segments[Random.Range(0, segments.Length)];   // pedrinhas soltando
                if (s != null) HitSparks.One(s.position + new Vector2(Random.Range(-1f, 1f), -0.4f), new Vector2(0f, -1f), new Color32(0x6e, 0x68, 0x84, 0xff), 0.6f, 30f);
            }
        }

        void Settle()
        {
            for (int i = 0; i < segments.Length; i++) if (segments[i] != null) segments[i].position = _rest[i];
        }

        IEnumerator Collapse(PlayerHealth cat)
        {
            _phase = Phase.Collapsing;
            var data = SaveManager.Data;
            if (data != null) { data.SetFlag(FallenFlag); SaveManager.Save(); } // libera o gatilho da queda
            SpeechBubble.Show(cat.transform, "Não, não, não—", 1.6f);
            CameraShake.Shake(3f, 1.2f);
            SeteVidas.Audio.Sfx.Play("plataforma_cai", cat.transform.position, 1f, 0.6f);
            // do meio (onde está o gato) para as pontas, um bloco de cada vez
            var order = new List<int>();
            for (int i = 0; i < segments.Length; i++) order.Add(i);
            float catX = cat.transform.position.x;
            order.Sort((a, b) => Mathf.Abs(_rest[a].x - catX).CompareTo(Mathf.Abs(_rest[b].x - catX)));
            int n = 0;
            foreach (int i in order)
            {
                var s = segments[i];
                if (s == null) continue;
                s.position = _rest[i];
                s.bodyType = RigidbodyType2D.Dynamic;
                s.gravityScale = 3f;
                s.angularVelocity = Random.Range(-120f, 120f);
                var col = s.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;   // cai todo mundo junto
                if (++n == 3) Release();                 // o gato já está caindo: devolve o controle
                yield return new WaitForSeconds(0.05f);
            }
            Release();
            _phase = Phase.Fallen;
            yield return new WaitForSeconds(4f);
            foreach (var s in segments) if (s != null) s.gameObject.SetActive(false);
        }
    }

    /// <summary>Regras da queda da ponte (testadas em EditMode).</summary>
    public static class BridgeRules
    {
        /// <summary>O gato foge a 60% da corrida (olhando para trás): a horda alcança a ponte antes de ele chegar ao meio.</summary>
        public const float FleeInput = 0.6f;
        /// <summary>A ponte só treme com tantos zumbis em cima (o peso).</summary>
        public const int ZombiesToShake = 3;
        /// <summary>Tremendo este tempo (s), com o gato no meio, o centro desaba.</summary>
        public const float ShakeBeforeCollapse = 1.6f;
        /// <summary>Segurança: desaba de qualquer jeito depois deste tempo de fuga (zumbi preso).</summary>
        public const float MaxFlight = 14f;

        /// <summary>O peso que faz tremer (0 = parada; 1 = muito): só com <see cref="ZombiesToShake"/> zumbis ou mais.</summary>
        public static float Weight(int zombies, bool hero)
        {
            if (zombies < ZombiesToShake) return 0f;
            return Mathf.Clamp01((zombies - ZombiesToShake + 1 + (hero ? 1 : 0)) / 4f);
        }

        /// <summary>O gato parado no meio há <see cref="LineTime"/> s (a fala) e a ponte tremendo: desaba.</summary>
        public static bool Collapses(float shakeTime, float stoppedFor) => stoppedFor >= LineTime && shakeTime >= ShakeBeforeCollapse;

        /// <summary>O tempo da fala no meio da ponte, com a horda colada nele, antes de desabar.</summary>
        public const float LineTime = 1.6f;

        /// <summary>A distância (tiles) que o <paramref name="rank"/>º zumbi da fila fica atrás do gato.</summary>
        public static float PackGap(int rank) => 1.7f + rank * 1.05f;

        /// <summary>
        /// Quanto o zumbi corre (multiplicador da corrida dele): acompanha o gato e fecha a distância até a vaga dele na
        /// fila; perto demais, para. Até 2,6× (alcança quem corre a 60%).
        /// </summary>
        public static float PaceMultiplier(float gap, float desired, float catSpeed, float zombieSpeed)
        {
            float v = catSpeed + 1.8f * (gap - desired);
            return Mathf.Clamp(v / Mathf.Max(0.1f, zombieSpeed), 0f, 2.6f);
        }
    }
}
