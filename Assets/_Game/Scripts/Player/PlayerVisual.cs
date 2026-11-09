using SeteVidas.Abilities;
using SeteVidas.Classes;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Visual placeholder: troca o sprite por estado e vira para o lado do movimento.
    /// Mostra o efeito do golpe na fase ativa e a camada da classe (ex.: marreta) por cima do gato:
    /// o corpo é sempre o do gato (GDD §14). Será substituído por um Animator com a arte final.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] WallClimbAbility wallClimb;
        [SerializeField] CombatController combat;
        [SerializeField] PlayerHealth health;
        [SerializeField] ClassManager classes;

        [Header("Sprites placeholder")]
        [SerializeField] Sprite idle;
        [SerializeField] Sprite[] run;
        [SerializeField] Sprite jump;
        [SerializeField] Sprite fall;
        [SerializeField] Sprite crouch;
        [SerializeField] Sprite hang;
        [SerializeField] Sprite[] climb;
        [SerializeField] Sprite attack;
        [Tooltip("Quadros por segundo da corrida e da escalada")]
        [SerializeField] float runFps = 10f;

        [Header("Corte da adaga (padrão, se a classe não tiver efeito próprio)")]
        [SerializeField] SpriteRenderer slashRenderer;
        [Tooltip("Um sprite por golpe do combo")]
        [SerializeField] Sprite[] slash;

        [Header("Camada da classe")]
        [Tooltip("Arma da classe desenhada por cima do gato (filho do Visual)")]
        [SerializeField] SpriteRenderer classOverlay;
        [Tooltip("Duração do brilho na cor da classe ao trocar (s)")]
        [SerializeField] float switchFlashTime = 0.25f;

        int _climbFrame;
        float _climbAnimTime;
        float _flashUntil;
        Color _flashColor = Color.white;

        void Reset()
        {
            controller = GetComponentInParent<PlayerController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            wallClimb = GetComponentInParent<WallClimbAbility>();
            combat = GetComponentInParent<CombatController>();
            health = GetComponentInParent<PlayerHealth>();
            classes = GetComponentInParent<ClassManager>();
        }

        void OnEnable() { if (classes != null) classes.ClassChanged += OnClassChanged; }
        void OnDisable() { if (classes != null) classes.ClassChanged -= OnClassChanged; }

        void OnClassChanged(ClassDefinition previous, ClassDefinition next)
        {
            if (next == null) return;
            _flashColor = next.ThemeColor;
            _flashUntil = Time.time + switchFlashTime;
        }

        void LateUpdate()
        {
            if (controller == null || spriteRenderer == null) return;

            bool attacking = combat != null && combat.IsAttacking;
            int facing = attacking ? combat.AttackDirection : controller.FacingDirection;
            spriteRenderer.flipX = facing < 0;
            spriteRenderer.sprite = PickSprite(attacking);

            // Troca de classe: brilho rápido na cor da classe
            float flash = Time.time < _flashUntil ? (_flashUntil - Time.time) / switchFlashTime : 0f;
            spriteRenderer.color = Color.Lerp(Color.white, _flashColor, flash);

            // Morto: deitado de lado. Invencível depois de tomar dano: pisca
            bool dead = health != null && (health.IsDead || health.IsCollapsed);
            spriteRenderer.transform.localRotation = dead ? Quaternion.Euler(0f, 0f, facing * 90f) : Quaternion.identity;
            spriteRenderer.enabled = dead || health == null || !health.IsInvulnerable || health.IsCollapsed || ((int)(Time.time * 20f) & 1) == 0;

            UpdateOverlay(attacking, dead);
            UpdateSlash();
        }

        Sprite PickSprite(bool attacking)
        {
            if (controller.IsHanging) return hang;
            if (controller.IsClimbingLedge) return crouch;
            if (wallClimb != null && wallClimb.IsClimbing && climb != null && climb.Length > 0)
            {
                // Só anima enquanto sobe ou desce
                if (Mathf.Abs(wallClimb.ClimbVelocity) > 0.01f) _climbAnimTime += Time.deltaTime;
                _climbFrame = (int)(_climbAnimTime * runFps * 0.8f) % climb.Length;
                return climb[_climbFrame];
            }
            if (controller.IsCrouching) return crouch;
            if (attacking && attack != null) return attack;
            Vector2 v = controller.Velocity;
            if (!controller.IsGrounded) return v.y > 0f ? jump : fall;
            if (Mathf.Abs(v.x) > 0.1f && run != null && run.Length > 0)
                return run[(int)(Time.time * runFps) % run.Length];
            return idle;
        }

        void UpdateOverlay(bool attacking, bool dead)
        {
            if (classOverlay == null) return;
            var def = classes != null ? classes.Current : null;
            Sprite s = null;
            if (def != null && !dead && !controller.IsHanging && !controller.IsClimbingLedge &&
                !(wallClimb != null && wallClimb.IsClimbing) && !controller.IsCrouching)
            {
                if (attacking) s = combat.IsWindingUp ? def.OverlayWindup : def.OverlayStrike;
                if (s == null) s = def.OverlayIdle;
            }
            classOverlay.sprite = s;
            classOverlay.enabled = s != null && spriteRenderer.enabled;
            classOverlay.flipX = spriteRenderer.flipX;
            classOverlay.color = spriteRenderer.color;
        }

        void UpdateSlash()
        {
            if (slashRenderer == null) return;
            var def = classes != null ? classes.Current : null;
            var sprites = def != null && def.SlashSprites != null && def.SlashSprites.Length > 0 ? def.SlashSprites : slash;
            bool show = combat != null && combat.IsHitActive && sprites != null && sprites.Length > 0;
            slashRenderer.enabled = show;
            if (!show) return;

            combat.GetHitbox(out Vector2 center, out _);
            slashRenderer.sprite = sprites[Mathf.Clamp(combat.ComboStep, 0, sprites.Length - 1)];
            slashRenderer.flipX = combat.AttackDirection < 0;
            // O sprite do efeito tem pivot embaixo (pasta Characters): desce meia altura para centralizar na hitbox
            float half = slashRenderer.sprite != null ? slashRenderer.sprite.bounds.size.y * 0.5f : 0.5f;
            slashRenderer.transform.position = new Vector3(center.x, center.y - half, 0f);
        }
    }
}
