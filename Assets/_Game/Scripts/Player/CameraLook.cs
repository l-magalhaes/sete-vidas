using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>Regras puras do "olhar para baixo" da câmera (Docs/Level/floresta-mapa.md §2, EditMode).</summary>
    public static class CameraLookRules
    {
        /// <summary>Caindo há mais que isto, a câmera antecipa o pouso.</summary>
        public const float FallDelay = 1f;
        public const float FallLook = 3f;
        /// <summary>Agachado e parado há mais que isto, a câmera desce para o gato ver embaixo.</summary>
        public const float CrouchDelay = 0.6f;
        public const float CrouchLook = 4f;

        /// <summary>Quanto o alvo da câmera desce (tiles, positivo = para baixo).</summary>
        public static float LookDown(float fallingFor, float crouchStillFor)
        {
            float look = 0f;
            if (fallingFor > FallDelay) look = FallLook;
            if (crouchStillFor > CrouchDelay) look = Mathf.Max(look, CrouchLook);
            return look;
        }
    }

    /// <summary>
    /// Alvo da câmera filho do gato ("CameraTarget"): a Cinemachine segue este alvo, e ele desce quando o gato cai
    /// há mais de 1 s ou fica agachado e parado (salas verticais da Floresta). Sem ele, a câmera segue o gato.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class CameraLook : MonoBehaviour
    {
        public const string TargetName = "CameraTarget";

        [SerializeField, Min(0.1f)] float moveSpeed = 8f;

        PlayerController _player;
        Transform _target;
        float _fallingFor, _crouchStillFor, _look;

        /// <summary>O transform que a câmera deve seguir (criado se não existir).</summary>
        public Transform Target
        {
            get
            {
                if (_target == null)
                {
                    _target = transform.Find(TargetName);
                    if (_target == null)
                    {
                        _target = new GameObject(TargetName).transform;
                        _target.SetParent(transform, false);
                    }
                }
                return _target;
            }
        }

        void Awake() => _player = GetComponent<PlayerController>();

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var body = _player.Body;
            bool falling = !_player.IsGrounded && body != null && body.linearVelocity.y < -0.5f;
            _fallingFor = falling ? _fallingFor + dt : 0f;
            bool crouchStill = _player.IsGrounded && _player.IsCrouching && body != null && Mathf.Abs(body.linearVelocity.x) < 0.05f;
            _crouchStillFor = crouchStill ? _crouchStillFor + dt : 0f;

            float want = CameraLookRules.LookDown(_fallingFor, _crouchStillFor);
            _look = Mathf.MoveTowards(_look, want, moveSpeed * dt);
            Target.localPosition = new Vector3(0f, -_look, 0f);
        }
    }
}
