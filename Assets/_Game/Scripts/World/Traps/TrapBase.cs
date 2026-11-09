using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Base das armadilhas que machucam: registra a área perigosa e acerta o gato que estiver nela.
    /// Espinhos devolvem o gato ao último chão seguro; o resto empurra para longe (como um golpe).
    /// </summary>
    public abstract class TrapBase : MonoBehaviour
    {
        [Tooltip("Hits tirados do gato")]
        [SerializeField, Min(1)] protected int damage = 1;
        [Tooltip("Espinhos: o gato volta ao último chão seguro. Desligado: só empurra")]
        [SerializeField] protected bool returnToSafeGround;

        System.Func<Rect> _area;
        static int s_playerMask = -1;

        protected static int PlayerMask
        {
            get
            {
                if (s_playerMask < 0) s_playerMask = LayerMask.GetMask(GameConstants.Layers.Player);
                return s_playerMask;
            }
        }

        /// <summary>Área perigosa no mundo agora (para o "chão seguro" evitar).</summary>
        protected abstract Rect DangerArea();

        protected virtual void OnEnable()
        {
            _area = DangerArea;
            TrapRules.RegisterDanger(_area);
        }

        protected virtual void OnDisable() => TrapRules.UnregisterDanger(_area);

        /// <summary>Acerta o gato se ele estiver dentro da caixa. Retorna true se acertou.</summary>
        protected bool HitPlayerIn(Vector2 center, Vector2 size)
        {
            var col = Physics2D.OverlapBox(center, size, 0f, PlayerMask);
            if (col == null) return false;
            var health = col.GetComponentInParent<PlayerHealth>();
            if (health == null) return false;
            float dir = col.transform.position.x >= center.x ? 1f : -1f;
            var info = new DamageInfo(damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, gameObject);
            if (returnToSafeGround) health.TakeHazardDamage(info);
            else health.TakeDamage(info);
            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            var r = DangerArea();
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(r.center, r.size);
        }
#endif
    }
}
