using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Combat
{
    /// <summary>
    /// Ataques corpo a corpo do gato (spec: Docs/Design/combate-adagas.md).
    /// A hitbox fica à frente do gato durante a fase ativa e acerta cada alvo uma vez por golpe.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class CombatController : MonoBehaviour
    {
        [SerializeField] MeleeAttackSettings weapon;
        [SerializeField] PlayerInputReader input;
        [Tooltip("Layers que a hitbox acerta")]
        [SerializeField] LayerMask targetLayers;

        PlayerController _player;
        readonly AttackSequence _sequence = new AttackSequence();
        readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();
        readonly List<Collider2D> _overlaps = new List<Collider2D>();
        int _lastSwingId;
        ContactFilter2D _filter;

        public bool IsAttacking => _sequence.IsAttacking;
        public bool IsHitActive => _sequence.IsHitActive;
        public int ComboStep => _sequence.Step;
        /// <summary>Lado do golpe atual, travado no início do golpe.</summary>
        public int AttackDirection { get; private set; } = 1;
        public MeleeAttackSettings Weapon => weapon;

        /// <summary>Disparado a cada alvo atingido (para efeitos e testes).</summary>
        public event System.Action<IDamageable, DamageInfo> Hit;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (weapon == null)
            {
                Debug.LogError("[Sete Vidas] CombatController sem MeleeAttackSettings.", this);
                enabled = false;
                return;
            }
            _filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = targetLayers };
        }

        bool CanAttack => !_player.IsHanging && !_player.IsClimbingLedge && _player.ActiveAbility == null;

        void Update()
        {
            if (!CanAttack)
            {
                if (_sequence.IsAttacking) _sequence.Cancel();
                return;
            }

            var timings = weapon.Timings;
            if (input != null && input.AttackPressedThisFrame)
            {
                bool wasIdle = !_sequence.IsAttacking;
                if (_sequence.Press(timings) && wasIdle)
                    AttackDirection = _player.FacingDirection;
            }

            int stepBefore = _sequence.Step;
            _sequence.Tick(Time.deltaTime, timings);
            if (_sequence.Step != stepBefore) AttackDirection = _player.FacingDirection; // 2º golpe pode virar

            if (_sequence.SwingId != _lastSwingId)
            {
                _lastSwingId = _sequence.SwingId;
                _hitThisSwing.Clear();
            }
            if (_sequence.IsHitActive) CheckHits();
        }

        /// <summary>Centro e tamanho da hitbox no mundo, para o lado <see cref="AttackDirection"/>.</summary>
        public void GetHitbox(out Vector2 center, out Vector2 size)
        {
            Vector2 feet = _player != null && _player.Body != null ? _player.Body.position : (Vector2)transform.position;
            float height = _player != null ? _player.CurrentHeight : 1.375f;
            float halfWidth = _player != null && _player.Settings != null ? _player.Settings.ColliderWidth * 0.5f : 0.375f;
            size = weapon.HitboxSize;
            center = new Vector2(feet.x + AttackDirection * (halfWidth + weapon.HitboxForward),
                                 feet.y + height * 0.5f);
        }

        void CheckHits()
        {
            GetHitbox(out var center, out var size);
            _overlaps.Clear();
            Physics2D.OverlapBox(center, size, 0f, _filter, _overlaps);

            float stop = 0f;
            foreach (var col in _overlaps)
            {
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !_hitThisSwing.Add(target)) continue;

                var info = weapon.ResolveHit(_player.Body.position, (Component)target, gameObject);
                target.TakeDamage(info);
                Hit?.Invoke(target, info);
                stop = Mathf.Max(stop, weapon.HitstopFor(info.Angle));
            }
            if (stop > 0f) Hitstop.Trigger(stop);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (weapon == null) return;
            if (_player == null) _player = GetComponent<PlayerController>();
            GetHitbox(out var c, out var s);
            Gizmos.color = Application.isPlaying && IsHitActive ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(c, s);
        }
#endif
    }
}
