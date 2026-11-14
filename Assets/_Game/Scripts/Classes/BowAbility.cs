using System.Collections.Generic;
using SeteVidas.Abilities;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Zombie;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Classes
{
    /// <summary>
    /// Arco do Caçador (F4b): com a classe ativa, o ataque atira uma flecha, no chão e no ar. A estocada para baixo
    /// (pogo) continua a das adagas. Barulho baixo: é a arma silenciosa.
    /// Melhorias 3 (14/11/2026): a mira anda de 15 em 15° com o direcional ou o analógico (de 45° para baixo até reto
    /// para cima, como a besta dos atiradores), com uma linha pontilhada mostrando para onde vai; atira mais rápido
    /// (até 3 flechas no ar) e tem mira assistida: puxa a flecha para o inimigo mais perto na frente do gato, sem travar.
    /// O <see cref="CombatController"/> chama <see cref="TryFire"/> quando a arma é à distância.
    /// </summary>
    public class BowAbility : ClassAbility, IRangedAttack
    {
        [SerializeField] CombatController combat;
        [SerializeField] ClassManager classes;
        [SerializeField] PlayerInputReader input;

        float _since = 99f;
        float _damageCarry;
        PlayerController _player;

        /// <summary>Acabou de atirar (para a pose do arco).</summary>
        public bool JustShot => _since < 0.18f;

        /// <summary>Direção da última flecha (para a pose e os testes).</summary>
        public Vector2 LastShot { get; private set; } = Vector2.right;

        void Awake()
        {
            if (combat == null) combat = GetComponent<CombatController>();
            if (classes == null) classes = GetComponent<ClassManager>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            _player = GetComponent<PlayerController>();
        }

        void Update()
        {
            _since += Time.deltaTime;
            UpdateAimGuide();
        }

        void OnDisable() => ShowGuide(false);

        public override void OnClassActivated()
        {
            if (combat == null || classes == null) return;
            var def = classes.Definition(ForClass);
            if (def != null && def.Weapon != null) combat.Equip(def.Weapon);
        }

        Vector2 Origin(Vector2 dir)
        {
            float h = _player.CurrentHeight;
            // sai do peito, um pouco para o lado da mira
            return _player.Body.position + new Vector2(dir.x * 0.5f, h * 0.6f + Mathf.Max(0f, dir.y) * h * 0.4f);
        }

        public bool TryFire(Vector2 stick)
        {
            if (!enabled || _player == null) return false;
            if (!HunterRules.CanShoot(_since, PlayerArrow.InFlight)) return false;
            _since = 0f;
            int facing = _player.FacingDirection;
            Vector2 aim = HunterRules.BowAim(stick, facing);
            int side = aim.x > 0.01f ? 1 : aim.x < -0.01f ? -1 : (facing >= 0 ? 1 : -1);
            bool aimed = stick.magnitude >= HunterRules.AimDeadZone;
            Vector2 from = Origin(aim);
            Vector2 dir = HunterRules.AssistAim(from, aim, side, aimed, VisibleTargets(from));
            LastShot = dir;

            // metade de um golpe de adaga, com o bônus dos corações (pedido de 14/11/2026)
            var daggers = classes != null ? classes.Definition(CharacterClass.Rogue) : null;
            int daggerBase = daggers != null && daggers.Weapon != null ? daggers.Weapon.BaseDamage : 2;
            int daggerHit = combat != null ? HeartDamageRules.Apply(daggerBase, combat.DamageMultiplier) : daggerBase;
            int damage = HunterRules.ArrowHit(daggerHit, ref _damageCarry);
            PlayerArrow.Fire(from, dir, HunterRules.ArrowSpeed, HunterRules.ArrowRange, damage, gameObject);
            SeteVidas.Audio.Sfx.Play("flecha", from);
            NoiseSystem.Emit(from, 2.5f, HunterRules.ArrowNoise, gameObject, "arco");
            return true;
        }

        // ---------------------------------------------------------------- alvos da mira assistida

        static readonly List<Vector2> s_targets = new List<Vector2>();
        static readonly Collider2D[] s_cols = new Collider2D[24];
        static int s_enemyMask = -1, s_wallMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_targets.Clear(); s_enemyMask = s_wallMask = -1; s_guideSprite = null; } // Play sem recarregar o domínio

        /// <summary>Centros dos inimigos vivos no alcance, sem parede no meio.</summary>
        List<Vector2> VisibleTargets(Vector2 from)
        {
            if (s_enemyMask < 0)
            {
                s_enemyMask = LayerMask.GetMask(GameConstants.Layers.Enemy);
                s_wallMask = LayerMask.GetMask(GameConstants.Layers.Ground, GameConstants.Layers.Breakable);
            }
            s_targets.Clear();
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = s_enemyMask, useTriggers = true };
            int n = Physics2D.OverlapCircle(from, HunterRules.ArrowRange, filter, s_cols);
            for (int i = 0; i < n; i++)
            {
                var col = s_cols[i];
                if (col == null || col.GetComponentInParent<IDamageable>() == null || IsDead(col)) continue;
                Vector2 c = col.bounds.center;
                if (Physics2D.Linecast(from, c, s_wallMask).collider != null) continue;  // atrás da parede
                s_targets.Add(c);
            }
            return s_targets;
        }

        static bool IsDead(Component c)
        {
            var ai = c.GetComponentInParent<EnemyAI>();
            if (ai != null) return ai.IsDead;
            var forest = c.GetComponentInParent<ForestEnemy>();
            if (forest != null) return forest.IsDead;
            var zombie = c.GetComponentInParent<ZombieAI>();
            return zombie != null && zombie.IsDead;
        }

        // ---------------------------------------------------------------- linha de mira

        const int GuideDots = 5;
        const float GuideStep = 0.55f;
        SpriteRenderer[] _guide;
        static Sprite s_guideSprite;

        /// <summary>Apontando para cima ou na diagonal, uma linha pontilhada curta mostra para onde a flecha vai.</summary>
        void UpdateAimGuide()
        {
            bool show = input != null && _player != null && input.isActiveAndEnabled && !input.GameplayLocked;
            Vector2 aim = Vector2.zero;
            if (show)
            {
                Vector2 stick = input.RawMove;
                aim = HunterRules.BowAim(stick, _player.FacingDirection);
                show = stick.magnitude >= HunterRules.AimDeadZone && Mathf.Abs(aim.y) > 0.1f;
            }
            ShowGuide(show);
            if (!show) return;
            Vector2 from = Origin(aim);
            for (int i = 0; i < GuideDots; i++)
            {
                var sr = _guide[i];
                sr.transform.position = from + aim * (0.8f + i * GuideStep);
                var c = sr.color;
                c.a = 0.75f - i * 0.12f;
                sr.color = c;
            }
        }

        void ShowGuide(bool show)
        {
            if (_guide == null)
            {
                if (!show) return;
                _guide = new SpriteRenderer[GuideDots];
                for (int i = 0; i < GuideDots; i++)
                {
                    var go = new GameObject("Mira do arco " + (i + 1));
                    go.transform.SetParent(transform, false);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = GuideSprite();
                    sr.sortingOrder = 7;
                    sr.color = new Color(1f, 0.93f, 0.7f, 0.7f);
                    _guide[i] = sr;
                }
            }
            foreach (var sr in _guide) if (sr != null && sr.enabled != show) sr.enabled = show;
        }

        static Sprite GuideSprite()
        {
            if (s_guideSprite != null) return s_guideSprite;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "mira_arco", hideFlags = HideFlags.DontSave
            };
            var px = new Color32[4];
            for (int i = 0; i < 4; i++) px[i] = new Color32(0xff, 0xff, 0xff, 0xff);
            tex.SetPixels32(px);
            tex.Apply(false);
            s_guideSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
            s_guideSprite.hideFlags = HideFlags.DontSave;
            return s_guideSprite;
        }
    }
}

namespace SeteVidas.Combat
{
    /// <summary>Arma à distância da classe (o arco): o combate chama no lugar do golpe corpo a corpo.</summary>
    public interface IRangedAttack
    {
        /// <summary>Atira na direção do direcional/analógico (<paramref name="stick"/>); false se não pôde agora.</summary>
        bool TryFire(Vector2 stick);
    }
}
