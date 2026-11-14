using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;
using State = SeteVidas.Enemies.Boss.GoldenBoss.State;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// Efeitos dos golpes do Koda, coesos com o resto do jogo (pedido do usuário, 12/11/2026): tudo em pixel inteiro,
    /// no dourado do Paladino, pelo mesmo pool de faíscas e pelo mesmo terremoto da marreta do gato.
    /// <list type="bullet">
    /// <item><b>Marretada:</b> arco dourado de 3 quadros no golpe; ao bater no chão, terremoto dourado curto.</item>
    /// <item><b>Salto:</b> sombra com borda dourada marca onde ele vai cair (fica mais forte na descida);
    /// no pouso, o terremoto dourado grande e o tremor.</item>
    /// <item><b>Cura:</b> centelhas douradas subindo em volta do corpo e anéis de luz abrindo no chão.</item>
    /// <item><b>Relâmpagos:</b> faíscas subindo da marreta erguida (os raios em si ficam no <see cref="LightningStrike"/>).</item>
    /// </list>
    /// Só observa o <see cref="GoldenBoss"/>; ele mesmo o adiciona.
    /// </summary>
    public class BossFx : MonoBehaviour
    {
        static readonly Color32 Gold = GoldenBoss.Gold;
        static readonly Color32 Glow = GoldenBoss.ShieldLight;
        static readonly Color32 Deep = new Color32(0xa8, 0x70, 0x2a, 0xff);
        static readonly Color32 Shadow = new Color32(0x14, 0x0f, 0x18, 0xff);

        static readonly Dictionary<int, Sprite> s_cache = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_cache.Clear(); // Play sem recarregar o domínio

        GoldenBoss _boss;
        SpriteRenderer _body;
        SpriteRenderer _arc, _marker;
        float _arcAge = 99f, _nextMote, _nextRing;
        int _arcFacing = 1;

        void Awake()
        {
            _boss = GetComponent<GoldenBoss>();
            _body = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable() { if (_boss != null) _boss.StateChanged += OnState; }
        void OnDisable() { if (_boss != null) _boss.StateChanged -= OnState; }

        void OnDestroy()
        {
            if (_marker != null) Destroy(_marker.gameObject);
            if (_arc != null) Destroy(_arc.gameObject);
        }

        int Order => _body != null ? _body.sortingOrder : 5;

        void OnState(State previous, State next)
        {
            switch (next)
            {
                case State.SwingActive:
                    _arcAge = 0f;
                    _arcFacing = _boss.FacingDirection;
                    break;
                case State.SwingRecover when previous == State.SwingActive:
                {
                    // a cabeça da marreta bate no chão à frente
                    var p = _boss.SwingPoint;
                    var ground = new Vector2(p.x + _boss.FacingDirection * 0.4f, _boss.GroundY);
                    QuakeFx.Spawn(ground, 1.4f, 0.12f, 0.35f, true);
                    CameraShake.Shake(2f, 0.12f);
                    break;
                }
                case State.LeapRecover when previous == State.LeapAir:
                {
                    var s = _boss.Settings;
                    float half = s != null ? s.LeapImpactHalfWidth : 1.6f;
                    QuakeFx.Spawn(new Vector2(_boss.Feet.x, _boss.GroundY), half * 1.8f, 0.22f, 0.9f, true);
                    CameraShake.Shake(3f, 0.25f);
                    HitSparks.Burst(new Vector2(_boss.Feet.x, _boss.GroundY + 0.3f), 10, Glow, Gold, 3f, 6f, 0.35f, 25f, 0.6f);
                    break;
                }
                case State.Heal:
                    _nextMote = 0f;
                    _nextRing = 0f;
                    break;
                case State.RepelWindup:
                    _nextMote = 0f;
                    break;
                case State.RepelActive:
                {
                    // a luz sobe da cabeça numa coluna: anéis e faíscas por toda a altura onde o gato quicava
                    var area = _boss.RepelArea;
                    var head = new Vector2(area.center.x, area.yMin + 0.2f);
                    PixelRing.Spawn(head, 4, 28, 0.3f, Glow, 2);
                    PixelRing.Spawn(head, 2, 18, 0.25f, Gold, 1, 0.06f);
                    for (int i = 0; i < 14; i++)
                        HitSparks.Rise(new Vector2(area.center.x + Random.Range(-area.width * 0.4f, area.width * 0.4f),
                                area.yMin + Random.Range(0f, area.height * 0.8f)),
                            i % 2 == 0 ? (Color)Glow : Gold, Random.Range(3f, 6f), Random.Range(0.25f, 0.45f));
                    CameraShake.Shake(2f, 0.15f);
                    break;
                }
            }
            if (next != State.LeapAir && _marker != null) _marker.enabled = false;
        }

        void LateUpdate()
        {
            if (_boss == null) return;
            float dt = Time.deltaTime;
            UpdateArc(dt);
            UpdateLeapMarker();

            var state = _boss.Current;
            Vector2 feet = _boss.Feet;
            var size = _boss.Settings != null ? _boss.Settings.BodySize : new Vector2(1.4f, 2f);
            if (state == State.RepelWindup)
            {
                // aviso da repulsa: brilho dourado subindo do alto da cabeça
                _nextMote -= dt;
                if (_nextMote <= 0f)
                {
                    _nextMote = 0.04f;
                    var p = feet + new Vector2(Random.Range(-size.x * 0.6f, size.x * 0.6f), size.y + Random.Range(-0.2f, 0.3f));
                    HitSparks.Rise(p, Random.value < 0.5f ? (Color)Glow : Gold, Random.Range(2f, 3.5f), Random.Range(0.3f, 0.5f));
                }
            }
            if (state == State.Heal)
            {
                _nextMote -= dt;
                if (_nextMote <= 0f)
                {
                    _nextMote = 0.06f;
                    var p = feet + new Vector2(Random.Range(-size.x * 0.7f, size.x * 0.7f), Random.Range(0.1f, size.y * 0.8f));
                    HitSparks.Rise(p, Random.value < 0.5f ? (Color)Glow : Gold, Random.Range(1.2f, 2.4f), Random.Range(0.4f, 0.7f));
                }
                _nextRing -= dt;
                if (_nextRing <= 0f)
                {
                    _nextRing = 0.45f;
                    PixelRing.Spawn(new Vector2(feet.x, _boss.GroundY + 0.1f), 4, 20, 0.4f, Gold, 1, 0f, Order - 1, 0.3f);
                }
            }
            else if (state == State.LightningCast)
            {
                _nextMote -= dt;
                if (_nextMote <= 0f)
                {
                    _nextMote = 0.05f;
                    var p = feet + new Vector2(_boss.FacingDirection * 0.3f + Random.Range(-0.4f, 0.4f), size.y + Random.Range(0.2f, 0.8f));
                    HitSparks.Rise(p, Random.value < 0.5f ? (Color)Glow : Gold, Random.Range(2f, 3.5f), 0.35f);
                }
            }
        }

        // -------------------------------------------------------------- marretada

        void UpdateArc(float dt)
        {
            _arcAge += dt;
            const float frame = 0.05f;
            int i = (int)(_arcAge / frame);
            if (i >= 3)
            {
                if (_arc != null) _arc.enabled = false;
                return;
            }
            if (_arc == null)
            {
                _arc = new GameObject("Arco da marretada (Koda)").AddComponent<SpriteRenderer>();
                _arc.sortingOrder = Order + 1;
            }
            // centro do arco no ombro; o arco desce de cima e de trás até o chão à frente
            Vector2 shoulder = _boss.Feet + new Vector2(_arcFacing * 0.3f, 1.4f);
            _arc.transform.position = PixelFont.Snap(shoulder);
            _arc.flipX = _arcFacing < 0;
            _arc.sprite = Arc(i);
            _arc.enabled = true;
        }

        /// <summary>
        /// Arco da marreta (raio 24 px, de 110° a -40°): borda clara por fora, dourado no meio, sombra dourada por dentro.
        /// Quadro 0 cheio, 1 mais fino, 2 pontilhado.
        /// </summary>
        static Sprite Arc(int frame)
        {
            int key = 10 + frame;
            if (s_cache.TryGetValue(key, out var s) && s != null) return s;
            const int r = 24, size = r * 2 + 3;
            var px = new Color32[size * size];
            float c = size * 0.5f;
            int thick = frame == 0 ? 5 : frame == 1 ? 3 : 3;
            float from = frame == 0 ? -40f : frame == 1 ? -40f : -40f, to = frame == 0 ? 110f : frame == 1 ? 60f : 20f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (a < from || a > to) continue;
                    float depth = r + 0.5f - d; // 0 = borda de fora
                    if (depth < 0f || depth >= thick) continue;
                    // a ponta de trás afina (o rastro vem do alto)
                    float along = (a - from) / (to - from); // 0 = frente/chão, 1 = trás/alto
                    if (depth >= thick * (1f - along * 0.7f)) continue;
                    if (frame == 2 && ((x + y) & 1) == 1) continue;
                    Color32 col = depth < 1f ? Glow : depth < thick - 1.5f ? Gold : Deep;
                    px[y * size + x] = col;
                }
            s = PixelFont.MakeSprite(px, size, size, new Vector2(0.5f, 0.5f), "arco do Koda " + frame);
            s_cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ salto

        void UpdateLeapMarker()
        {
            if (_boss.Current != State.LeapAir)
            {
                if (_marker != null) _marker.enabled = false;
                return;
            }
            if (_marker == null)
            {
                _marker = new GameObject("Sombra do pouso (Koda)").AddComponent<SpriteRenderer>();
                _marker.sortingOrder = 3;
            }
            var s = _boss.Settings;
            float half = s != null ? s.LeapImpactHalfWidth : 1.6f;
            float k = _boss.StateDuration > 0f ? Mathf.Clamp01(_boss.StateTime / _boss.StateDuration) : 1f;
            int level = k < 0.4f ? 0 : k < 0.75f ? 1 : 2; // a sombra escurece conforme ele desce
            int blink = ((int)(Time.time * (level == 2 ? 16f : 8f)) & 1);
            _marker.sprite = Marker(Mathf.RoundToInt(half * PixelFont.Ppu), level, blink);
            _marker.transform.position = PixelFont.Snap(new Vector3(_boss.LeapTargetX, _boss.GroundY, 0f));
            _marker.enabled = true;
        }

        /// <summary>Sombra achatada no chão (pontilhado escuro) com a borda dourada tracejada que pisca.</summary>
        static Sprite Marker(int halfPx, int level, int blink)
        {
            int key = 1000 + (halfPx * 4 + level) * 2 + blink;
            if (s_cache.TryGetValue(key, out var s) && s != null) return s;
            int w = halfPx * 2 + 1, h = 7;
            var px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f - cx) / (w * 0.5f), ny = (y + 0.5f - cy) / (h * 0.5f);
                    float d = nx * nx + ny * ny;
                    if (d > 1f) continue;
                    bool rim = d > 0.72f;
                    if (rim)
                    {
                        if (((x / 2 + blink) & 1) == 0) px[y * w + x] = level == 2 ? Glow : Gold;
                        continue;
                    }
                    bool dot = level == 0 ? (x + y * 2) % 4 == 0 : level == 1 ? ((x + y) & 1) == 0 : true;
                    if (dot) px[y * w + x] = Shadow;
                }
            // pivô no meio da altura: metade da sombra entra no chão, metade fica sobre ele
            s = PixelFont.MakeSprite(px, w, h, new Vector2(0.5f, 0.5f), "sombra do pouso");
            s_cache[key] = s;
            return s;
        }
    }
}
