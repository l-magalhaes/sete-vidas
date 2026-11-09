using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Dano no gato, PROVISÓRIO até a Fase 4: conta hits, empurra para longe do atacante e dá
    /// invencibilidade com o sprite piscando. Não existe morte ainda; a Fase 4 troca isto pelo PlayerHealth.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHurtbox : MonoBehaviour, IDamageable
    {
        PlayerController _player;
        float _invulnerableUntil = -1f;

        /// <summary>Hits recebidos desde o início (depuração).</summary>
        public int HitsTaken { get; private set; }
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public event System.Action<DamageInfo> Damaged;

        void Awake() => _player = GetComponent<PlayerController>();

        public void TakeDamage(in DamageInfo info)
        {
            if (IsInvulnerable || info.Amount <= 0) return;
            var s = _player.Settings;

            HitsTaken += info.Amount;
            _invulnerableUntil = Time.time + s.InvulnerabilityTime;

            float dir = info.Direction.x >= 0f ? 1f : -1f; // para longe do atacante
            float vy = s.LaunchVelocityFor(s.KnockbackHeight, Time.fixedDeltaTime);
            _player.ApplyKnockback(new Vector2(dir * s.KnockbackSpeed, vy), s.KnockbackControlLock);

            DamagePopup.Spawn(transform.position + Vector3.up * 1.8f, $"-{info.Amount} hit", new Color32(0xff, 0x6b, 0x6b, 0xff));
            Damaged?.Invoke(info);
        }
    }
}
