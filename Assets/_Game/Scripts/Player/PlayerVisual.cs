using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Visual placeholder da Fase 1: troca o sprite por estado e vira para o lado do movimento.
    /// Será substituído por um Animator quando a arte final do gato chegar.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Header("Sprites placeholder")]
        [SerializeField] Sprite idle;
        [SerializeField] Sprite[] run;
        [SerializeField] Sprite jump;
        [SerializeField] Sprite fall;
        [SerializeField] Sprite crouch;
        [SerializeField] Sprite hang;
        [Tooltip("Quadros por segundo da corrida")]
        [SerializeField] float runFps = 10f;

        void Reset()
        {
            controller = GetComponentInParent<PlayerController>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (controller == null || spriteRenderer == null) return;

            spriteRenderer.flipX = controller.FacingDirection < 0;
            spriteRenderer.sprite = PickSprite();
        }

        Sprite PickSprite()
        {
            if (controller.IsHanging) return hang;
            if (controller.IsClimbingLedge) return crouch;
            if (controller.IsCrouching) return crouch;
            Vector2 v = controller.Velocity;
            if (!controller.IsGrounded) return v.y > 0f ? jump : fall;
            if (Mathf.Abs(v.x) > 0.1f && run != null && run.Length > 0)
                return run[(int)(Time.time * runFps) % run.Length];
            return idle;
        }
    }
}
