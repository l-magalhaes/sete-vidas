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

        // ------------------------------------------------------------ alerta e detecção (pedido de 14/11/2026)

        /// <summary>Olhos, ouvidos e medidor de suspeita ("?" e "!") deste bicho.</summary>
        protected ForestAwareness Senses { get; private set; }
        /// <summary>Como ele percebe o gato (padrão: o da Sentinela).</summary>
        protected virtual ForestSenseProfile SenseProfile => ForestSenseRules.Default;
        /// <summary>De onde ele olha.</summary>
        protected virtual Vector2 EyePosition => (Vector2)transform.position + Vector2.up * 0.8f;
        /// <summary>Para onde ele olha.</summary>
        protected virtual Vector2 LookDirection => new Vector2(_facing, 0f);
        /// <summary>Onde fica o "?"/"!" (local, em cima da cabeça).</summary>
        protected virtual Vector3 IconOffset => new Vector3(0f, 2.1f, 0f);
        /// <summary>Detectou o gato: ataca.</summary>
        public bool Aware => Senses != null && Senses.Aware;
        /// <summary>Estado do medidor (testes e depuração).</summary>
        public AlertState AlertState => Senses != null ? Senses.State : AlertState.Patrolling;

        void OnNoise(NoiseEvent n) { if (!IsDead && Senses != null) Senses.Hear(n, EyePosition); }

        protected virtual void Awake()
        {
            _home = transform.position;
            IsInfected = Infected.On(this);
            _hp = MaxHealth;
            _facing = startFacing >= 0 ? 1 : -1;
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _animPhase = Random.Range(0f, 10f);   // iguais lado a lado não animam em sincronia
            Senses = new ForestAwareness(gameObject, SenseProfile, IconOffset);
        }

        float _animPhase;

        /// <summary>
        /// Quadro da animação em loop (F4c, arte definitiva do bestiário): <paramref name="frames"/> a <paramref name="fps"/>
        /// quadros por segundo, fora de sincronia com os vizinhos. Sem quadros, o desenho único <paramref name="fallback"/>.
        /// </summary>
        protected Sprite Anim(Sprite[] frames, Sprite fallback, float fps)
        {
            if (frames == null || frames.Length == 0) return fallback;
            int i = (int)((Time.time + _animPhase) * fps) % frames.Length;
            return frames[i] != null ? frames[i] : fallback;
        }

        protected virtual void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); GameEvents.WorldReset += Revive; NoiseSystem.Emitted += OnNoise; }
        protected virtual void OnDisable() { s_all.Remove(this); GameEvents.WorldReset -= Revive; NoiseSystem.Emitted -= OnNoise; }

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
            int dmg = AdjustDamage(info, Mathf.Max(1, Mathf.RoundToInt(info.Amount * DamageMultiplier(info))));
            if (dmg <= 0)
            {
                // não passou da casca (a flecha no Galho Seco): só o tranco
                var chip = HitFlash.For(spriteRenderer);
                if (chip != null) chip.Squash(1, 0.08f);
                if (Senses != null) Senses.Damaged(info.Source != null ? (Vector2)info.Source.transform.position : (Vector2)transform.position);
                OnHit(info);
                return;
            }
            _hp = Mathf.Max(0, _hp - dmg);
            var flash = HitFlash.For(spriteRenderer);
            if (flash != null) { flash.Flash(0.08f); flash.Squash(1, 0.12f); }
            SeteVidas.Audio.Sfx.Play(_hp > 0 ? "zumbi_dano" : "zumbi_morte", transform.position); // provisório (decisão de 13/11/2026)
            if (Senses != null) Senses.Damaged(info.Source != null ? (Vector2)info.Source.transform.position : (Vector2)transform.position);
            OnHit(info);
            if (_hp <= 0) Die();
        }

        protected virtual void OnHit(in DamageInfo info) { }

        /// <summary>O dano final deste golpe (0 = não passou; o Galho Seco guarda o que a flecha tirou de pouco em pouco).</summary>
        protected virtual int AdjustDamage(in DamageInfo info, int dmg) => dmg;

        /// <summary>Quanto tempo o desenho fica depois de morrer (a animação de morte).</summary>
        protected virtual float CorpseTime => 0.4f;

        /// <summary>Morre na hora, ignorando a vida e a invulnerabilidade (a serpente entocada no terremoto).</summary>
        protected void KillNow(in DamageInfo info)
        {
            if (IsDead) return;
            _hp = 0;
            var flash = HitFlash.For(spriteRenderer);
            if (flash != null) { flash.Flash(0.08f); flash.Squash(1, 0.12f); }
            SeteVidas.Audio.Sfx.Play("zumbi_morte", transform.position);
            OnHit(info);
            Die();
        }

        void Die()
        {
            IsDead = true;
            foreach (var c in _colliders) if (c != null) c.enabled = false;
            _goneAt = Time.time + CorpseTime;
            if (Senses != null) Senses.Kill();
            Vector2 at = (Vector2)transform.position + Vector2.up * 0.8f;
            HitSparks.Burst(at, 10, new Color32(0xc4, 0x98, 0x60, 0xff), new Color32(0x52, 0x26, 0x1e, 0xff), 2f, 6f, 0.5f, 40f, 0.5f);
            SeteVidas.World.EnemyDrops.OnDeath(at);   // o coração (15%) e o potinho de mana (10%), como todo inimigo
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
            if (Senses != null) Senses.Reset();
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
            if (Senses != null) Senses.Step(EyePosition, LookDirection, Time.deltaTime);
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
            int before = h.CurrentHits + h.TempHits;
            if (!HurtPlayerIn(box, damage)) return false;
            if (h.CurrentHits + h.TempHits < before && !h.IsDead) h.Poison();
            return true;
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected() { }
#endif
    }
}
