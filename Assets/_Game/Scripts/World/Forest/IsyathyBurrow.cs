using System.Collections;
using System.Collections.Generic;
using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World.Traps;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Isyathy embaixo da terra do Ninho (F7; pedido do usuário, 17/11/2026, no lugar da fuga). Um por cena
    /// (<c>Forest_E_Nest</c>), montado pelo <c>NestSetup</c>.
    /// <list type="bullet">
    /// <item><b>O despertar</b> (a 1ª vez na E1, pulável depois de visto): ela dorme deitada nos patamares da E1, na frente
    /// da tela; abre o olho, levanta a cabeça para o gato, ruge, mergulha no patamar e entra inteira na terra, deixando a
    /// cratera (que fica lá).</item>
    /// <item><b>Os botes</b> (<see cref="BurrowRules"/>): de tempos em tempos, com o gato no chão das salas E1–E3, a tela
    /// treme e o chão racha perto dele; ela sai do buraco, se arma, dá o bote onde ele estava (a mordida tira um
    /// coração) e volta para a terra. Perto de santuário, não.</item>
    /// </list>
    /// Morta no Coração (<see cref="IsyathySighting.DeadFlag"/>), o Ninho fica quieto; a cratera continua.
    /// </summary>
    public class IsyathyBurrow : MonoBehaviour
    {
        /// <summary>Id da cena do despertar (a flag "cena_vista_ninho_despertar" diz que ela já acordou).</summary>
        public const string WakeId = "ninho_despertar";
        /// <summary>D-NINHO-01 adaptada: ao vê-la dormindo, e depois que ela entra na terra (a dica dos botes).</summary>
        public static readonly string[] WakeLines = { "...Isso não é raiz.", "Ela entrou na terra.", "Se o chão tremer, não fico parado." };

        [Tooltip("As salas onde ela ataca (mundo): E1, E2 e E3")]
        [SerializeField] Rect[] rooms = new Rect[0];
        [Tooltip("A sala do despertar (E1)")]
        [SerializeField] Rect wakeArea;
        [Tooltip("O trilho do despertar (mundo): o corpo deitado (do rabo ao pescoço), a cabeça que levanta, o arco e o mergulho")]
        [SerializeField] Vector2[] wakePath = new Vector2[0];
        [Tooltip("Pontos do trilho: o pescoço deitado, a cabeça olhando para o gato, a boca do buraco")]
        [SerializeField] int restIndex, lookIndex, holeIndex;
        [Tooltip("A boca do buraco do despertar (a superfície, mundo): a cratera que fica")]
        [SerializeField] Vector2 wakeHole;

        enum Phase { Sleeping, Waking, Lurking, Striking, Gone }

        Phase _phase;
        float _timer, _time;
        SerpentRig _rig;
        Crater _wakeCrater, _strikeCrater;
        PlayerHealth _health;
        PlayerController _player;
        readonly List<Vector2> _safe = new List<Vector2>();
        static Sprite s_back, s_lip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_back = null; s_lip = null; }   // Play sem recarregar o domínio

        public void Configure(Rect[] attackRooms, Rect wake, Vector2[] path, int rest, int look, int hole, Vector2 holePoint)
        {
            rooms = attackRooms; wakeArea = wake; wakePath = path; restIndex = rest; lookIndex = look; holeIndex = hole; wakeHole = holePoint;
        }

        // o trilho do despertar alisado (sem quinas) e as distâncias nele
        Vector2[] _wakeSmooth;
        float restS, lookS, holeS;

        static bool Flag(string f) => SaveManager.Data != null && SaveManager.Data.HasFlag(f);
        static bool IsAwake => Flag(Cutscene.SeenPrefix + WakeId);

        void Start()
        {
            var rigGo = new GameObject("Isyathy (corpo)");
            rigGo.transform.SetParent(transform, false);
            _rig = rigGo.AddComponent<SerpentRig>();
            _wakeCrater = new Crater(transform, "Cratera do despertar");
            _strikeCrater = new Crater(transform, "Cratera do bote");
            _wakeSmooth = BurrowRules.Smooth(wakePath);
            restS = BurrowRules.LengthTo(_wakeSmooth, restIndex * BurrowRules.SmoothSteps);
            lookS = BurrowRules.LengthTo(_wakeSmooth, lookIndex * BurrowRules.SmoothSteps);
            holeS = BurrowRules.LengthTo(_wakeSmooth, holeIndex * BurrowRules.SmoothSteps);
            foreach (var s in FindObjectsByType<Sanctuary>(FindObjectsSortMode.None)) _safe.Add(s.transform.position);

            if (IsAwake)
            {
                _wakeCrater.Show(wakeHole, 1f, true);
                _phase = Flag(IsyathySighting.DeadFlag) ? Phase.Gone : Phase.Lurking;
                _timer = BurrowRules.FirstDelay;
            }
            else if (Flag(IsyathySighting.DeadFlag)) _phase = Phase.Gone;
            else
            {
                _phase = Phase.Sleeping;
                _rig.SetPath(_wakeSmooth);
                _rig.SetClip(wakeHole);
                _rig.S = restS;
                _rig.Frame = IsyathyArt.FrameSleep;
                _rig.Visible = true;
                _rig.Render();
            }
        }

        bool FindPlayer()
        {
            var h = PlayerHealth.Active;
            if (h != _health) { _health = h; _player = h != null ? h.GetComponent<PlayerController>() : null; }
            return _health != null && _player != null && !_health.IsDead;
        }

        Vector2 CatCenter => _player.Body.position + Vector2.up * (_player.CurrentHeight * 0.5f);

        bool InRooms(Vector2 p)
        {
            foreach (var r in rooms) if (r.Contains(p)) return true;
            return false;
        }

        Rect RoomOf(Vector2 p)
        {
            foreach (var r in rooms) if (r.Contains(p)) return r;
            return default;
        }

        void Update()
        {
            _time += Time.deltaTime;
            if (_phase == Phase.Gone || _phase == Phase.Waking || _phase == Phase.Striking) return;
            if (Flag(IsyathySighting.DeadFlag)) { _rig.Visible = false; _phase = Phase.Gone; return; }

            if (_phase == Phase.Sleeping)
            {
                // ela respira: uma onda bem lenta e pequena pelo corpo
                _rig.Amplitude = 0.08f + 0.05f * Mathf.Sin(_time * 1.1f);
                _rig.Phase = _time * 0.5f;
                bool near = FindPlayer() && Vector2.Distance(CatCenter, wakePath[wakePath.Length - 1]) < 40f;
                if (near) _rig.Render();
                if (near && wakeArea.Contains(CatCenter) && CatCenter.x > wakeArea.xMin + 4f && !CutsceneRunner.IsPlaying) Wake();
                return;
            }

            // à espreita
            if (!FindPlayer() || CutsceneRunner.IsPlaying) return;
            Vector2 cat = CatCenter;
            if (!InRooms(cat)) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            if (BurrowRules.InSafeZone(cat, _safe.ToArray())) { _timer = 0.4f; return; }
            if (TryPickSpot(cat, out var hole)) StartCoroutine(Strike(hole));
            else _timer = 0.3f;
        }

        // ------------------------------------------------------------ o lugar do bote

        bool TryPickSpot(Vector2 cat, out Vector2 hole)
        {
            hole = default;
            var room = RoomOf(cat);
            // no chão, os pés; pendurado, subindo ou no ar, o chão embaixo dele (ela alcança até lá em cima)
            float feet = _player.Body.position.y;
            int ground = LayerMask.GetMask(GameConstants.Layers.Ground);
            if (!_player.IsGrounded)
            {
                var below = Physics2D.Raycast(_player.Body.position + Vector2.up * 0.2f, Vector2.down, BurrowRules.MaxBelow, ground);
                if (below.collider == null) return false;
                feet = below.point.y;
            }
            int dir = Mathf.Abs(_player.Body.linearVelocity.x) > 0.5f ? (int)Mathf.Sign(_player.Body.linearVelocity.x) : _player.FacingDirection;
            foreach (float off in BurrowRules.Offsets(dir))
            {
                float x = cat.x + off;
                if (x < room.xMin + 2.5f || x > room.xMax - 2.5f) continue;
                if (!GroundAt(x, feet, ground, out float y)) continue;
                // a boca inteira no chão reto
                if (!GroundAt(x - 0.9f, feet, ground, out float yl) || !GroundAt(x + 0.9f, feet, ground, out float yr)) continue;
                if (Mathf.Abs(yl - y) > 0.3f || Mathf.Abs(yr - y) > 0.3f) continue;
                if (HazardNear(new Vector2(x, y))) continue;
                var p = new Vector2(x, y);
                if (BurrowRules.InSafeZone(p, _safe.ToArray())) continue;
                hole = p;
                return true;
            }
            return false;
        }

        static bool GroundAt(float x, float feet, int mask, out float y)
        {
            y = 0f;
            var hit = Physics2D.Raycast(new Vector2(x, feet + 1.5f), Vector2.down, 4f, mask);
            if (hit.collider == null || hit.normal.y < 0.9f || Mathf.Abs(hit.point.y - feet) > 1.6f) return false;
            y = hit.point.y;
            return true;
        }

        static bool HazardNear(Vector2 p)
        {
            foreach (var c in Physics2D.OverlapBoxAll(p + Vector2.up * 0.5f, new Vector2(2.6f, 1.2f), 0f))
                if (c.GetComponentInParent<SpikeStrip>() != null) return true;
            return false;
        }

        // ------------------------------------------------------------ o bote

        IEnumerator Strike(Vector2 hole)
        {
            _phase = Phase.Striking;
            int side = CatCenter.x >= hole.x ? 1 : -1;

            // o aviso: a terra treme e o chão racha
            CameraShake.Shake(1.5f, BurrowRules.WarnTime);
            float t = 0f;
            while (t < BurrowRules.WarnTime)
            {
                t += Time.deltaTime;
                float k = t / BurrowRules.WarnTime;
                _strikeCrater.Show(hole, Mathf.Lerp(0.2f, 0.65f, k), false);
                if (Random.value < Time.deltaTime * 14f)
                    HitSparks.One(hole + new Vector2(Random.Range(-1.2f, 1.2f), 0.1f), new Vector2(Random.Range(-1f, 1f), Random.Range(2f, 5f)), DirtColor(), 0.5f, 25f);
                yield return null;
            }

            // sai do buraco em S (passa um pouco da altura e assenta)
            var path = BurrowRules.Smooth(BurrowRules.RisePath(hole, side));
            _rig.SetPath(path);
            _rig.SetClip(hole);
            _rig.Frame = IsyathyArt.FrameAwake;
            _rig.Visible = true;
            _strikeCrater.Show(hole, 1f, true);
            CameraShake.Shake(2.5f, 0.35f);
            HitSparks.Burst(hole + Vector2.up * 0.3f, 18, DirtColor(), new Color(0.35f, 0.25f, 0.33f), 3f, 8f, 0.7f, 25f, 1.2f);
            yield return Move(BurrowRules.Depth - 2f, _rig.PathLength, BurrowRules.RiseTime, Ease.OutBack, 0.12f);

            // mira: a cabeça acompanha o gato e respira; recua (arma o bote) e a boca abre
            Vector2 aimOffset = Vector2.zero;
            t = 0f;
            while (t < BurrowRules.AimTime)
            {
                t += Time.deltaTime;
                float k = t / BurrowRules.AimTime;
                Vector2 toCat = FindPlayer() ? CatCenter - path[path.Length - 1] : Vector2.zero;
                Vector2 track = Vector2.ClampMagnitude(toCat * 0.06f, 0.35f);   // vira de leve para ele
                Vector2 coil = new Vector2(-side, 0.25f) * 0.55f * BurrowRules.EaseInOut(k);   // recua para dar o bote
                Vector2 breath = new Vector2(0f, Mathf.Sin(t * 14f) * 0.05f);
                aimOffset = Vector2.Lerp(aimOffset, track + coil + breath, 1f - Mathf.Exp(-Time.deltaTime * 12f));
                path = BurrowRules.Smooth(BurrowRules.RisePath(hole, side, aimOffset));
                _rig.SetPath(path);
                _rig.S = _rig.PathLength;
                _rig.Frame = k > 0.45f ? IsyathyArt.FrameBite : IsyathyArt.FrameAwake;
                _rig.Glow = Mathf.Repeat(t, 0.16f) < 0.08f;
                Animate(0.08f, 1.5f);
                yield return null;
            }
            _rig.Glow = true;

            // o bote: em arco até onde o gato estava, passa do ponto e volta um pouco; a boca fecha na mordida
            Vector2 rear = path[path.Length - 1];
            Vector2 target = FindPlayer() ? BurrowRules.LungeTarget(rear, CatCenter, hole.y) : rear + new Vector2(side * 4f, 0f);
            var arc = BurrowRules.LungeArc(rear, target);
            var lunge = new Vector2[path.Length + arc.Length];
            path.CopyTo(lunge, 0);
            arc.CopyTo(lunge, path.Length);
            _rig.SetPath(lunge);
            bool bit = false;
            float from = _rig.S, to = _rig.PathLength;
            CameraShake.Shake(1.2f, 0.15f);
            t = 0f;
            while (t < BurrowRules.LungeTime + BurrowRules.BiteHold)
            {
                t += Time.deltaTime;
                float prev = _rig.S;
                if (t < BurrowRules.LungeTime) _rig.S = Mathf.Lerp(from, to, BurrowRules.EaseOutBack(t / BurrowRules.LungeTime));
                else _rig.S = to - 0.45f * BurrowRules.EaseInOut((t - BurrowRules.LungeTime) / BurrowRules.BiteHold);   // recua do ponto
                // a mordida: fecha, abre e fecha (sacode a cabeça)
                float after = t - BurrowRules.LungeTime;
                _rig.Frame = after < 0.04f ? IsyathyArt.FrameBite : after < 0.12f ? IsyathyArt.FrameAwake : after < 0.18f ? IsyathyArt.FrameBite : IsyathyArt.FrameAwake;
                Animate(0.1f, (_rig.S - prev) / Mathf.Max(Time.deltaTime, 1e-4f));
                if (!bit && FindPlayer() && BurrowRules.Bites(_rig.Snout, CatCenter))
                {
                    bit = true;
                    float dir = CatCenter.x >= _rig.Neck.x ? 1f : -1f;
                    _health.TakeDamage(new DamageInfo(BurrowRules.Damage, new Vector2(dir, 0.3f), DamageType.Normal, AttackAngle.Front, gameObject));
                }
                FragileBranch.BreakInside(new Rect(_rig.Snout.x - 1f, _rig.Snout.y - 1f, 2f, 2f));
                yield return null;
            }

            // volta para a terra: puxa a cabeça de volta ao buraco e escorrega para dentro se contorcendo
            yield return Move(_rig.S, Mathf.Max(0f, BurrowRules.LengthTo(lunge, path.Length - 1) - 0.3f), 0.22f, Ease.InOut, 0.12f);
            yield return Move(_rig.S, 0f, BurrowRules.RetractTime, Ease.In, 0.18f);
            _rig.Visible = false;
            _rig.ClearClip();
            _timer = Random.Range(BurrowRules.CooldownMin, BurrowRules.CooldownMax);   // o próximo já conta (a cratera some enquanto isso)
            _phase = Phase.Lurking;
            t = 0f;
            while (t < BurrowRules.CraterFade && _phase == Phase.Lurking)
            {
                t += Time.deltaTime;
                _strikeCrater.Fade(1f - t / BurrowRules.CraterFade);
                yield return null;
            }
            if (_phase == Phase.Lurking) _strikeCrater.Hide();
        }

        enum Ease { In, Out, InOut, OutBack }

        static float Apply(Ease e, float k)
        {
            switch (e)
            {
                case Ease.In: return BurrowRules.EaseIn(k);
                case Ease.Out: return BurrowRules.EaseOut(k);
                case Ease.OutBack: return BurrowRules.EaseOutBack(k);
                default: return BurrowRules.EaseInOut(k);
            }
        }

        /// <summary>Leva a cabeça de <paramref name="from"/> a <paramref name="to"/> no trilho; a onda corre com a velocidade.</summary>
        IEnumerator Move(float from, float to, float seconds, Ease ease, float wave)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float prev = _rig.S;
                _rig.S = Mathf.LerpUnclamped(from, to, Apply(ease, t / seconds));
                Animate(wave, (_rig.S - prev) / Mathf.Max(Time.deltaTime, 1e-4f));
                yield return null;
            }
            _rig.S = to;
            _rig.Render();
        }

        /// <summary>A onda pelo corpo: corre e cresce com a velocidade da cabeça (rastejar de verdade, nunca dura).</summary>
        void Animate(float wave, float headSpeed)
        {
            _rig.Amplitude = BurrowRules.WaveAmplitude(wave, headSpeed);
            _rig.Phase += Time.deltaTime * BurrowRules.WaveSpeed(headSpeed) * (headSpeed >= 0f ? 1f : -1f);
            _rig.Render();
        }

        static Color DirtColor() => Random.value < 0.5f ? new Color(0.21f, 0.16f, 0.2f) : new Color(0.31f, 0.24f, 0.3f);

        // ------------------------------------------------------------ o despertar

        void Wake()
        {
            _phase = Phase.Waking;
            new Cutscene(WakeId).AllowSkipAfterFirstTime()
                .Run(WakeRoutine)
                .OnSkip(() => { _rig.Visible = false; _wakeCrater.Show(wakeHole, 1f, true); })
                .Play(() =>
                {
                    CameraLook.Lead = Vector2.zero;
                    _rig.Visible = false;
                    _wakeCrater.Show(wakeHole, 1f, true);
                    _timer = BurrowRules.FirstDelay;
                    if (_phase == Phase.Waking) _phase = Phase.Lurking;
                });
        }

        IEnumerator WakeRoutine()
        {
            if (_player != null) _player.Body.linearVelocity = new Vector2(0f, _player.Body.linearVelocity.y);
            CameraLook.Lead = new Vector2(6f, 1.5f);
            // ela respira; o gato percebe
            CameraShake.Shake(0.8f, 1f);
            if (_player != null) SpeechBubble.Show(_player.transform, WakeLines[0], 1.8f);
            yield return Breathe(1.8f);
            // o olho abre
            _rig.Frame = IsyathyArt.FrameAwake;
            CameraShake.Shake(1.2f, 0.4f);
            yield return Breathe(0.7f);
            // a cabeça levanta e olha para o gato (o corpo inteiro escorrega atrás)
            yield return Move(restS, lookS, 1.1f, Ease.OutBack, 0.12f);
            // o rugido
            _rig.Frame = IsyathyArt.FrameBite;
            CameraShake.Shake(3f, 1f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime;
                _rig.Glow = Mathf.Repeat(t, 0.14f) < 0.07f;
                _rig.S = lookS + 0.08f * Mathf.Sin(t * 40f);
                Animate(0.1f, 0f);
                yield return null;
            }
            _rig.Glow = true;
            _rig.Frame = IsyathyArt.FrameAwake;
            yield return Breathe(0.3f);
            // o mergulho: arqueia e entra no patamar
            yield return Move(lookS, holeS - 0.3f, 0.6f, Ease.In, 0.18f);
            _wakeCrater.Show(wakeHole, 1f, true);
            CameraShake.Shake(3f, 0.6f);
            HitSparks.Burst(wakeHole + Vector2.up * 0.3f, 26, DirtColor(), new Color(0.35f, 0.25f, 0.33f), 3f, 9f, 0.8f, 25f, 1.2f);
            // o corpo inteiro escorrega para dentro da terra
            float end = holeS + BurrowRules.BodyLength + 1f, speed = 5f;
            while (_rig.S < end)
            {
                speed = Mathf.MoveTowards(speed, 15f, Time.deltaTime * 10f);   // começa devagar e vai embora rápido
                _rig.S = Mathf.Min(end, _rig.S + speed * Time.deltaTime);
                if (Random.value < Time.deltaTime * 10f)
                    HitSparks.One(wakeHole + new Vector2(Random.Range(-1.3f, 1.3f), 0.2f), new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(2f, 5f)), DirtColor(), 0.5f, 25f);
                if (Random.value < Time.deltaTime * 3f) CameraShake.Shake(1f, 0.25f);
                Animate(0.15f, speed);
                yield return null;
            }
            _rig.Visible = false;
            yield return new WaitForSeconds(0.5f);
            if (_player != null) SpeechBubble.Show(_player.transform, WakeLines[1], 1.8f);
            yield return new WaitForSeconds(2f);
            if (_player != null) SpeechBubble.Show(_player.transform, WakeLines[2], 2.2f);
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Breathe(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                _rig.Amplitude = 0.08f + 0.05f * Mathf.Sin((_time + t) * 1.1f);
                _rig.Phase += Time.deltaTime * 0.5f;
                _rig.Render();
                yield return null;
            }
        }

        // ------------------------------------------------------------ a cratera

        /// <summary>O buraco (atrás do corpo) e a borda de terra da frente (na frente dele).</summary>
        internal sealed class Crater
        {
            readonly SpriteRenderer _back, _lip;


            public Crater(Transform parent, string name)
            {
                var root = new GameObject(name).transform;
                root.SetParent(parent, false);
                _back = Part(root, "Buraco", BackSprite(), SerpentRig.Order - 1);
                _lip = Part(root, "Borda da frente", LipSprite(), SerpentRig.Order + 1);
                Hide();
            }

            static SpriteRenderer Part(Transform root, string name, Sprite sprite, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root, false);
                sr.sprite = sprite;
                sr.sortingOrder = order;
                return sr;
            }

            /// <summary>
            /// Abre a cratera na superfície <paramref name="surface"/>; <paramref name="angle"/> gira a boca (graus: 0 = no
            /// chão, para cima; 90 = na parede da direita, para a esquerda; 180 = no teto; −90 = na parede da esquerda).
            /// </summary>
            public void Show(Vector2 surface, float scale, bool lip, float angle = 0f)
            {
                var rot = Quaternion.Euler(0f, 0f, angle);
                _back.transform.rotation = rot; _lip.transform.rotation = rot;
                _back.enabled = true;
                _back.transform.position = surface;
                _back.transform.localScale = new Vector3(scale, Mathf.Lerp(0.5f, 1f, scale), 1f);
                _lip.enabled = lip;
                _lip.transform.position = surface;
                Fade(1f);
            }

            public void Fade(float a)
            {
                var c = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                _back.color = c; _lip.color = c;
            }

            public void Hide() { _back.enabled = false; _lip.enabled = false; }

            static Sprite BackSprite()
            {
                if (s_back == null) s_back = Make(IsyathyArt.CraterBack(), IsyathyArt.CraterW, IsyathyArt.CraterBackH, IsyathyArt.CraterBackSurface, "isyathy_cratera");
                return s_back;
            }

            static Sprite LipSprite()
            {
                if (s_lip == null) s_lip = Make(IsyathyArt.CraterLip(), IsyathyArt.CraterW, IsyathyArt.CraterLipH, IsyathyArt.CraterLipSurface, "isyathy_cratera_borda");
                return s_lip;
            }

            /// <summary>Um sprite do desenho (linhas de cima para baixo), com a linha <paramref name="surfaceRow"/> no chão.</summary>
            static Sprite Make(uint[] px, int w, int h, int surfaceRow, string name)
            {
                var flipped = new uint[w * h];
                for (int y = 0; y < h; y++) System.Array.Copy(px, y * w, flipped, (h - 1 - y) * w, w);
                var tex = new Texture2D(w, h, TextureFormat.BGRA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name, hideFlags = HideFlags.DontSave };
                tex.SetPixelData(flipped, 0);
                tex.Apply(false);
                var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, (h - surfaceRow) / (float)h), 16f);
                s.hideFlags = HideFlags.DontSave;
                return s;
            }
        }

        void OnDisable() { CameraLook.Lead = Vector2.zero; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.3f, 0.6f, 0.8f);
            for (int i = 1; i < wakePath.Length; i++) Gizmos.DrawLine(wakePath[i - 1], wakePath[i]);
            Gizmos.DrawWireSphere(wakeHole, 0.5f);
            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.4f);
            foreach (var r in rooms) Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
