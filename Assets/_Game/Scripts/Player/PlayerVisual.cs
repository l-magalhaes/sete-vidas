using SeteVidas.Abilities;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Visual placeholder: troca o sprite por estado e vira para o lado do movimento.
    /// Mostra o corte da adaga durante a fase ativa do golpe.
    /// Será substituído por um Animator quando a arte final do gato chegar.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] WallClimbAbility wallClimb;
        [SerializeField] CombatController combat;

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

        [Header("Corte da adaga")]
        [SerializeField] SpriteRenderer slashRenderer;
        [Tooltip("Um sprite por golpe do combo")]
        [SerializeField] Sprite[] slash;

        int _climbFrame;
        float _climbAnimTime;

        void Reset()
        {
            controller = GetComponentInParent<PlayerController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            wallClimb = GetComponentInParent<WallClimbAbility>();
            combat = GetComponentInParent<CombatController>();
        }

        void LateUpdate()
        {
            if (controller == null || spriteRenderer == null) return;

            bool attacking = combat != null && combat.IsAttacking;
            int facing = attacking ? combat.AttackDirection : controller.FacingDirection;
            spriteRenderer.flipX = facing < 0;
            spriteRenderer.sprite = PickSprite(attacking);
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

        void UpdateSlash()
        {
            if (slashRenderer == null) return;
            bool show = combat != null && combat.IsHitActive && slash != null && slash.Length > 0;
            slashRenderer.enabled = show;
            if (!show) return;

            combat.GetHitbox(out Vector2 center, out _);
            slashRenderer.sprite = slash[Mathf.Clamp(combat.ComboStep, 0, slash.Length - 1)];
            slashRenderer.flipX = combat.AttackDirection < 0;
            // O sprite do corte tem pivot embaixo (pasta Characters): desce meio tile para centralizar
            slashRenderer.transform.position = new Vector3(center.x, center.y - 0.5f, 0f);
        }
    }
}
