using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;
using static SeteVidas.Enemies.ForestEnemyRules;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Planta Carnívora (ajuste do usuário, 13/11/2026): presa ao chão ou à parede, abre a boca (aviso de 0,6 s) e dá um
    /// bote com o pescoço esticado, pegando o gato até 6 tiles de distância. Enquanto o pescoço volta, a cabeça aceita
    /// pogo. A "frente" é o eixo para cima do objeto: no chão, para cima; na parede, gire o objeto (90° = esquerda).
    /// A cabeça é um filho com o colisor (é nela que os golpes acertam); o pescoço são gomos desenhados entre a base e ela.
    /// </summary>
    public class CarnivorousPlant : ForestEnemy
    {
        [Tooltip("Até onde a cabeça vai (tiles)")]
        [SerializeField, Min(1f)] float reach = PlantReach;
        [SerializeField] Transform head;
        [SerializeField] Sprite closed, open, recovering;
        [Tooltip("Gomo do pescoço (desenhado várias vezes entre a base e a cabeça)")]
        [SerializeField] Sprite neckSprite;
        [Tooltip("Onde a cabeça descansa, a partir da base, na direção da frente (tiles)")]
        [SerializeField] float restDistance = 0.8f;

        PlantState _state;
        float _timer;
        Vector2 _aim;
        Vector2 _lungeTarget;
        readonly List<SpriteRenderer> _neck = new List<SpriteRenderer>();

        public PlantState State => _state;
        public override bool AcceptsPogo => PlantVulnerable(_state);

        public void Configure(float bite) { reach = bite; maxHealth = PlantHealth; }

        Vector2 Base => transform.position;
        Vector2 Forward => transform.up;
        Vector2 Rest => Base + Forward * restDistance;

        /// <summary>Um pouco à frente da base, fora do bloco onde a planta está presa (os testes de parede saem daqui).</summary>
        const float MouthOffset = 0.5f;
        /// <summary>Raio da cabeça para não encostar o desenho no bloco (a cabeça tem 1,5 tile).</summary>
        const float HeadRadius = 0.45f;
        Vector2 Mouth => Base + Forward * MouthOffset;

        static int s_wallMask;
        static int WallMask
        {
            get
            {
                if (s_wallMask == 0)
                    s_wallMask = LayerMask.GetMask(SeteVidas.Core.GameConstants.Layers.Ground,
                        SeteVidas.Core.GameConstants.Layers.Climbable, SeteVidas.Core.GameConstants.Layers.Breakable);
                return s_wallMask;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_wallMask = 0; // Play sem recarregar o domínio

        /// <summary>
        /// Até onde (a partir da base) a cabeça vai na direção <paramref name="dir"/> sem entrar num bloco, no máximo
        /// <paramref name="want"/>.
        /// </summary>
        float ClearDistance(Vector2 dir, float want)
        {
            float travel = Mathf.Max(0f, want - MouthOffset);
            if (travel <= 0f) return want;
            var hit = Physics2D.CircleCast(Mouth, HeadRadius, dir, travel, WallMask);
            if (hit.collider == null) return want;
            return MouthOffset + hit.distance;
        }

        protected override void Awake()
        {
            base.Awake();
            _aim = Forward;
            // infectada: o pescoço estica mais (só se o lugar não pediu um alcance próprio)
            if (IsInfected && Mathf.Approximately(reach, PlantReach)) reach = PlantReachInfected;
            if (neckSprite != null)
                for (int i = 0; i < 10; i++)
                {
                    var go = new GameObject("Pescoço " + i);
                    go.transform.SetParent(transform, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = neckSprite;
                    sr.sortingOrder = 3;
                    _neck.Add(sr);
                }
        }

        protected override void Revive()
        {
            base.Revive();
            _state = PlantState.Idle;
            _timer = 0f;
            foreach (var n in _neck) if (n != null) n.enabled = true;
            if (head != null) head.position = Rest;
        }

        protected override void OnDied()
        {
            foreach (var n in _neck) if (n != null) n.enabled = false;
        }

        ToxicCloud _spores;

        /// <summary>Infectada: cada golpe solta esporos em volta da cabeça (uma nuvem de cada vez).</summary>
        protected override void OnHit(in DamageInfo info)
        {
            if (!IsInfected || (_spores != null && _spores.IsAlive)) return;
            Vector2 at = head != null ? (Vector2)head.position : Rest;
            _spores = ToxicCloud.Spawn(at, Vector2.one * PlantSporeSize, PlantSporeTime);
        }

        protected override void Tick(float dt)
        {
            var h = Player;
            Vector2 cat = h != null ? (Vector2)h.transform.position + Vector2.up * 0.7f : Rest;
            Vector2 toCat = cat - Base;
            // só vê o que está à frente (não morde para trás da parede ou do chão)
            bool inRange = h != null && !h.IsDead && toCat.magnitude <= reach + restDistance && Vector2.Dot(toCat, Forward) > 0f
                           && Physics2D.Linecast(Mouth, cat, WallMask).collider == null; // não morde através de bloco
            var before = _state;
            _state = StepPlant(_state, ref _timer, dt, inRange, IsInfected);

            if (_state == PlantState.Windup)
            {
                _aim = PlantAim(Forward, toCat);
                if (before == PlantState.Idle) SeteVidas.Audio.Sfx.Play("zumbi_sente", Base, 0.7f, 1.2f); // provisório
            }
            if (_state == PlantState.Lunge && before == PlantState.Windup)
            {
                // trava o alvo no começo do bote: dá para desviar
                // e para no primeiro bloco do caminho: a cabeça não atravessa chão, parede nem teto
                float d = Mathf.Min(reach, toCat.magnitude);
                _lungeTarget = Base + _aim * Mathf.Max(restDistance, ClearDistance(_aim, d));
                SeteVidas.Audio.Sfx.Play("zumbi_caca", Base, 0.9f, 1.3f); // provisório: o estalo da boca
            }

            float ext = PlantExtension(_state, _timer, IsInfected);
            Vector2 headPos = Vector2.Lerp(Rest, _state == PlantState.Idle || _state == PlantState.Windup ? Rest : _lungeTarget, ext);
            if (_state == PlantState.Windup) headPos += _aim * 0.25f * Mathf.Sin(Time.time * 40f); // tremendo de vontade
            if (head != null) head.position = headPos;

            if (PlantBites(_state)) HurtPlayerIn(new Rect(headPos - Vector2.one * 0.6f, Vector2.one * 1.2f));

            // gomos do pescoço entre a base e a cabeça
            for (int i = 0; i < _neck.Count; i++)
            {
                float t = (i + 1f) / (_neck.Count + 1f);
                _neck[i].transform.position = Vector2.Lerp(Base, headPos, t);
                _neck[i].enabled = ext > 0.02f || i < 2;
            }

            if (spriteRenderer != null)
            {
                Sprite want = _state == PlantState.Windup || PlantBites(_state) ? open : _state == PlantState.Retract ? recovering : closed;
                if (want != null) spriteRenderer.sprite = want;
                // a cabeça olha para onde vai
                Vector2 dir = (_state == PlantState.Idle ? Forward : _aim);
                spriteRenderer.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                spriteRenderer.flipX = false;
            }
        }

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, reach + restDistance);
        }
#endif
    }
}
