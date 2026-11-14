using SeteVidas.Combat;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.Abilities
{
    /// <summary>
    /// Dash do Caçador (F4b, Docs/Design/classe-cacador.md): avanço horizontal de 6 tiles em 0,2 s, no chão e no ar,
    /// sem gravidade durante o avanço. No ar, um por vez: volta ao tocar o chão, agarrar parede ou borda, chegar num
    /// gancho ou quicar num pogo. Vale em qualquer classe depois do vínculo com a Vulpis (<see cref="HunterRules.MovesUnlocked"/>).
    /// Botão: Dash (Shift / gatilho direito).
    /// </summary>
    public class DashAbility : PlayerAbility
    {
        const float Buffer = 0.12f;

        float _buffer;
        float _since = 99f;
        float _left;
        int _dir;
        bool _airReady = true;
        float _trail;

        public bool IsDashing => Player != null && Player.ActiveAbility == this;
        /// <summary>Ainda tem o dash do ar.</summary>
        public bool AirDashReady => _airReady;

        static readonly Color32 TrailA = new Color32(0xf2, 0xec, 0xd8, 0xff), TrailB = new Color32(0xbd, 0xb5, 0x9c, 0xff);

        /// <summary>O botão do dash foi apertado há pouco (o gancho solta para o dash pegar).</summary>
        public bool WantsDash => _buffer > 0f;

        /// <summary>Devolve o dash do ar (chegou num gancho, quicou num pogo).</summary>
        public void RefillAir() => _airReady = true;

        void OnEnable() { if (Player != null) Player.Bounced += RefillAir; }
        void OnDisable() { if (Player != null) Player.Bounced -= RefillAir; }

        void Update()
        {
            if (Player == null || Player.Input == null) return;
            if (Player.Input.DashPressedThisFrame && HunterRules.MovesUnlocked(SaveManager.Data)) _buffer = Buffer;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _buffer -= dt;
            if (!IsDashing) _since += dt;
            // tocar o chão, agarrar a parede ou a borda devolve o dash do ar
            if (Player.IsGrounded || Player.IsHanging || Player.ActiveAbility is WallClimbAbility) _airReady = true;
        }

        public override bool TryBegin(bool grounded)
        {
            if (_buffer <= 0f || Player.IsCrouching) return false;
            if (!HunterRules.CanDash(grounded, _airReady, _since)) return false;
            _buffer = 0f;
            _since = 0f;
            if (!grounded) _airReady = false;
            _dir = HunterRules.DashDirection(Player.Input.MoveX, Player.FacingDirection);
            Player.Face(_dir);
            Player.Timers.Consume();
            _left = HunterRules.DashTime;
            _trail = 0f;
            Player.Body.linearVelocity = new Vector2(_dir * HunterRules.DashSpeed, 0f);
            SeteVidas.Audio.Sfx.Play("estocada", Player.Body.position, 0.8f, 1.3f); // provisório: o avanço
            SeteVidas.Core.NoiseSystem.Emit(Player.Body.position, 3f, 0.2f, gameObject, "dash");
            return true;
        }

        public override bool Step(float dt)
        {
            _left -= dt;
            var body = Player.Body;
            // parede logo à frente: o dash acaba encostado nela
            if (WallAhead(body.position, dt)) { End(0f); return false; }
            body.linearVelocity = new Vector2(_dir * HunterRules.DashSpeed, 0f);

            // rastro: pontinhos claros que ficam para trás
            _trail -= dt;
            if (_trail <= 0f)
            {
                _trail = 0.02f;
                Vector2 at = body.position + new Vector2(-_dir * 0.3f, Player.CurrentHeight * Random.Range(0.25f, 0.8f));
                HitSparks.One(at, new Vector2(-_dir * Random.Range(0.5f, 1.5f), 0f), Random.value < 0.5f ? TrailA : TrailB, 0.25f, 0f);
            }

            if (_left > 0f) return true;
            End(HunterRules.DashCarry);
            return false;
        }

        void End(float carry)
        {
            var v = Player.Body.linearVelocity;
            Player.Body.linearVelocity = new Vector2(_dir * HunterRules.DashSpeed * carry, Mathf.Min(v.y, 0f));
        }

        public override void OnInterrupted() => _left = 0f;

        static int s_wallMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_wallMask = -1; // Play sem recarregar o domínio

        bool WallAhead(Vector2 feet, float dt)
        {
            if (s_wallMask < 0)
                s_wallMask = LayerMask.GetMask(SeteVidas.Core.GameConstants.Layers.Ground,
                    SeteVidas.Core.GameConstants.Layers.Climbable, SeteVidas.Core.GameConstants.Layers.Breakable);
            float w = Player.Settings.ColliderWidth, h = Player.CurrentHeight;
            var hit = Physics2D.BoxCast(feet + new Vector2(0f, h * 0.5f + 0.1f), new Vector2(w - 0.1f, h - 0.3f), 0f,
                new Vector2(_dir, 0f), HunterRules.DashSpeed * dt + 0.02f, s_wallMask);
            return hit.collider != null && hit.distance <= 0.03f;
        }
    }
}
