using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Base dos inimigos da Floresta (F4, versão básica): vida, dano, piscar ao apanhar, morrer e voltar quando o mundo
    /// reinicia (descansar ou morrer). Fica na layer Enemy com um colisor de gatilho (é o que os golpes do gato procuram).
    /// Variante infectada (F4): o componente <see cref="Infected"/> no mesmo objeto liga <see cref="IsInfected"/>; cada
    /// inimigo troca os números pelos de <see cref="ForestEnemyRules"/> e ganha +1 de vida. Sons: os do zumbi por enquanto.
    /// </summary>
    [SelectionBase]
    public abstract class ForestEnemy : MonoBehaviour, IDamageable, IPogoTarget, IFacingTarget
    {
        static readonly List<ForestEnemy> s_all = new List<ForestEnemy>();
        public static IReadOnlyList<ForestEnemy> All => s_all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        [SerializeField, Min(1)] protected int maxHealth = 5;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [Tooltip("1 = olhando para a direita no começo")]
        [SerializeField] protected int startFacing = -1;

        protected int _hp;
        protected Vector3 _home;
        protected int _facing;
        Collider2D[] _colliders;
        float _goneAt = -1f;

        public bool IsDead { get; private set; }
        /// <summary>Variante infectada (tem o componente <see cref="Infected"/>).</summary>
        public bool IsInfected { get; private set; }
        /// <summary>Vida cheia: a variante infectada aguenta um golpe a mais (bestiário).</summary>
        public int MaxHealth => maxHealth + (IsInfected ? 1 : 0);
        public int Health => _hp;
        public abstract bool AcceptsPogo { get; }
        public int FacingDirection => _facing;
        public bool InCombat => true; // nunca é surpreendido (são bichos atentos)

        protected virtual void Awake()
        {
            _home = transform.position;
            IsInfected = Infected.On(this);
            _hp = MaxHealth;
            _facing = startFacing >= 0 ? 1 : -1;
            _colliders = GetComponentsInChildren<Collider2D>(true);
        }

        protected virtual void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); GameEvents.WorldReset += Revive; }
        protected virtual void OnDisable() { s_all.Remove(this); GameEvents.WorldReset -= Revive; }

        /// <summary>Multiplicador do golpe (o Galho Seco preso leva o dobro nas costas).</summary>
        protected virtual float DamageMultiplier(in DamageInfo info) => 1f;

        /// <summary>Não leva dano agora (a Serpente dentro do buraco).</summary>
        protected virtual bool Invulnerable => false;

        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead || info.Amount <= 0) return;
            if (Invulnerable)
            {
                SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position, 0.5f, 1.2f);
                return;
            }
            int dmg = Mathf.Max(1, Mathf.RoundToInt(info.Amount * DamageMultiplier(info)));
            _hp = Mathf.Max(0, _hp - dmg);
            var flash = HitFlash.For(spriteRenderer);
            if (flash != null) { flash.Flash(0.08f); flash.Squash(1, 0.12f); }
            SeteVidas.Audio.Sfx.Play(_hp > 0 ? "zumbi_dano" : "zumbi_morte", transform.position); // provisório (decisão de 13/11/2026)
            OnHit(info);
            if (_hp <= 0) Die();
        }

        protected virtual void OnHit(in DamageInfo info) { }

        void Die()
        {
            IsDead = true;
            foreach (var c in _colliders) if (c != null) c.enabled = false;
            _goneAt = Time.time + 0.4f;
            Vector2 at = (Vector2)transform.position + Vector2.up * 0.8f;
            HitSparks.Burst(at, 10, new Color32(0xc4, 0x98, 0x60, 0xff), new Color32(0x52, 0x26, 0x1e, 0xff), 2f, 6f, 0.5f, 40f, 0.5f);
            OnDied();
        }

        protected virtual void OnDied() { }

        /// <summary>Volta vivo, no lugar de origem (reinício do mundo).</summary>
        protected virtual void Revive()
        {
            if (!gameObject.activeInHierarchy) return;
            IsDead = false;
            _hp = MaxHealth;
            _goneAt = -1f;
            transform.position = _home;
            _facing = startFacing >= 0 ? 1 : -1;
            foreach (var c in _colliders) if (c != null) c.enabled = true;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
        }

        /// <summary>Entra vivo e inteiro, no lugar de origem (uma leva de arena que acabou de ser ligada).</summary>
        public void ReviveNow() => Revive();

        protected virtual void Update()
        {
            if (IsDead)
            {
                if (_goneAt > 0f && Time.time >= _goneAt && spriteRenderer != null) spriteRenderer.enabled = false;
                return;
            }
            Tick(Time.deltaTime);
            if (spriteRenderer != null) spriteRenderer.flipX = ArtFacesRight ? _facing < 0 : _facing > 0;
        }

        /// <summary>O desenho olha para a direita (a Serpente); os outros olham para a esquerda.</summary>
        protected virtual bool ArtFacesRight => false;

        protected abstract void Tick(float dt);

        // ------------------------------------------------------------ ajudas

        protected static PlayerHealth Player => PlayerHealth.Active;

        /// <summary>Fere o gato se ele estiver na caixa (a direção do empurrão é para longe deste inimigo).</summary>
        protected bool HurtPlayerIn(Rect box, int damage = 1)
        {
            var h = Player;
            if (h == null || h.IsDead) return false;
            var col = h.GetComponent<Collider2D>();
            var b = col != null ? col.bounds : new Bounds(h.transform.position + Vector3.up * 0.7f, new Vector3(0.75f, 1.375f, 0f));
            if (!box.Overlaps(new Rect(b.min, b.size))) return false;
            float dir = h.transform.position.x >= transform.position.x ? 1f : -1f;
            h.TakeDamage(new DamageInfo(damage, new Vector2(dir, 0f), DamageType.Normal, AttackAngle.Front, gameObject));
            return true;
        }

        /// <summary>
        /// Como <see cref="HurtPlayerIn"/>, mas envenena se o golpe tirou vida de verdade (fora da invencibilidade):
        /// a mordida da Serpente infectada. Encostar de novo durante a invencibilidade não envenena outra vez.
        /// </summary>
        protected bool HurtAndPoisonIn(Rect box, int damage = 1)
        {
            var h = Player;
            if (h == null || h.IsDead) return false;
            int before = h.CurrentHits;
            if (!HurtPlayerIn(box, damage)) return false;
            if (h.CurrentHits < before && !h.IsDead) h.Poison();
            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected() { }
#endif
    }
}
