using System.Collections;
using SeteVidas.Combat;
using SeteVidas.Dialogue;
using SeteVidas.Enemies;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// F6, o resgate na D2 (Docs/Narrativa/floresta-roteiro.md, D-VULPIS-01; pedido do usuário, 19/11/2026: faltavam a
    /// trilha de flechas, a cena da flecha derrubando a criatura e o aviso sobre a Isyathy). O gato entra na D2 e um Galho
    /// Seco infectado larga o disfarce na frente dele; o controle sai, a Vulpis aparece no galho alto, puxa o arco e a
    /// flecha de magia verde derruba o tronco. Ela fala (o primeiro relance dela; o vínculo é na D5) e vai embora pulando.
    /// Uma vez por jogo (<see cref="SeenFlag"/>); depois, o Galho Seco da cena não volta mais.
    /// </summary>
    public class VulpisRescue : MonoBehaviour
    {
        public const string SeenFlag = "cena_vista_vulpis_resgate";

        [Tooltip("A criatura da cena (um Galho Seco infectado, disfarçado)")]
        [SerializeField] ForestEnemy creature;
        [Tooltip("A cena começa quando o gato passa deste x (mundo), andando para a direita")]
        [SerializeField] float triggerX;
        [Tooltip("Os pés da Vulpis no galho alto (mundo)")]
        [SerializeField] Vector2 perch;
        [SerializeField] CharacterAnimationSet vulpisAnimations;
        [SerializeField] DialogueDefinition talk;

        SpriteRenderer _fox;
        VulpisVisual _look;
        bool _running;

        public void Configure(ForestEnemy target, float startX, Vector2 foxFeet, CharacterAnimationSet anims, DialogueDefinition dialogue)
        {
            creature = target; triggerX = startX; perch = foxFeet; vulpisAnimations = anims; talk = dialogue;
        }

        static bool Seen => SaveManager.Data != null && SaveManager.Data.HasFlag(SeenFlag);

        /// <summary>A cena começa? Regra pura: primeira vez, gato vivo e já dentro da sala, além do ponto.</summary>
        public static bool ShouldStart(bool seen, bool catAlive, float catX, float startX) => !seen && catAlive && catX >= startX;

        void Start()
        {
            if (Seen && creature != null) creature.gameObject.SetActive(false);   // a criatura já caiu
        }

        void Update()
        {
            if (_running) return;
            var cat = PlayerHealth.Active;
            if (cat == null || !ShouldStart(Seen, !cat.IsDead, cat.transform.position.x, triggerX)) return;
            if (creature == null || creature.IsDead || !creature.gameObject.activeInHierarchy) return;
            var runner = DialogueRunner.Active;
            if (runner != null && runner.IsOpen) return;
            StartCoroutine(Run(cat));
        }

        void BuildFox()
        {
            if (_fox != null) return;
            var go = new GameObject("Vulpis (resgate)");
            go.transform.SetParent(transform, false);
            go.transform.position = perch;
            _fox = go.AddComponent<SpriteRenderer>();
            _fox.sortingOrder = 4;
            if (vulpisAnimations != null)
            {
                _look = go.AddComponent<VulpisVisual>();
                _look.Configure(vulpisAnimations, true);
            }
        }

        void Fox(string clip, string then = null) { if (_look != null) _look.Play(clip, then); }
        float FoxTime(string clip) => _look != null ? _look.Duration(clip) : 0.4f;

        IEnumerator Run(PlayerHealth cat)
        {
            _running = true;
            var input = cat.GetComponent<PlayerInputReader>();
            if (input != null) input.SetLock(this, true);
            cat.Shielded = true;   // o tronco (e quem mais estiver por perto) não fere o gato durante a cena
            var data = SaveManager.Data;
            if (data != null) { data.SetFlag(SeenFlag); SaveManager.Save(); }
            try
            {
                // 1. o tronco larga o disfarce na frente do gato e se arma
                var branch = creature as DryBranch;
                if (branch != null) branch.OnBumped(cat.transform.position);
                yield return new WaitForSeconds(0.5f);
                SpeechBubble.Show(cat.transform, "...Isso não é um tronco.", 1.6f);
                yield return new WaitForSeconds(1.1f);

                // 2. a Vulpis no galho alto: cai das folhas, pousa e puxa o arco
                BuildFox();
                bool left = cat.transform.position.x < perch.x;
                if (_look != null) _look.FaceLeft = left;
                Vector2 from = perch + Vector2.up * 5f;
                _fox.transform.position = from;
                Fox("fall");
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    float k = t / 0.35f;
                    _fox.transform.position = Vector2.Lerp(from, perch, k * k);
                    _fox.color = new Color(1f, 1f, 1f, Mathf.Min(1f, k * 3f));
                    if (Random.value < Time.deltaTime * 20f) ForestMagic.One((Vector2)_fox.transform.position + Vector2.up, Vector2.up, ForestMagic.Mid, 0.4f);
                    yield return null;
                }
                _fox.transform.position = perch;
                _fox.color = Color.white;
                SeteVidas.Audio.Sfx.Play("folhas", perch);
                SeteVidas.Audio.Sfx.Play("vulpis_motivo");   // MUS-F10
                Fox("land", "idle");
                yield return new WaitForSeconds(FoxTime("land"));
                Fox("shoot", "idle");
                SeteVidas.Audio.Sfx.Play("vulpis_arco_tensiona", perch);
                yield return new WaitForSeconds(0.45f);

                // 3. a flecha: da ponta do arco até o peito do tronco
                Vector2 tip = perch + new Vector2(left ? -0.75f : 0.75f, 0.8f);
                Vector2 hit = creature != null ? (Vector2)creature.transform.position + Vector2.up * 3f : tip + Vector2.down * 3f;
                var arrow = new GameObject("Flecha da Vulpis").AddComponent<SpriteRenderer>();
                arrow.sprite = ForestMagic.Arrow();
                arrow.sortingOrder = 24;
                Vector2 dir = (hit - tip).normalized;
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                SeteVidas.Audio.Sfx.Play("vulpis_flecha_voa", tip);
                t = 0f;
                float flight = Mathf.Max(0.12f, Vector2.Distance(tip, hit) / 45f);
                while (t < flight)
                {
                    t += Time.deltaTime;
                    Vector2 p = Vector2.Lerp(tip, hit, t / flight);
                    arrow.transform.position = p;
                    if (Random.value < Time.deltaTime * 60f) ForestMagic.One(p, -dir * 2f, ForestMagic.Mid, 0.3f);
                    yield return null;
                }
                Destroy(arrow.gameObject);
                SeteVidas.Audio.Sfx.Play("vulpis_flecha_crava", hit);
                ForestMagic.Burst(hit, 14, 2f, 6f, 0.5f);
                CameraShake.Shake(1.5f, 0.3f);
                if (creature != null) creature.KillScripted(gameObject, dir);
                yield return new WaitForSeconds(1.8f);   // o tronco tomba (a animação de morte dele)

                // 4. a conversa (D-VULPIS-01): o primeiro relance dela e o aviso
                if (talk != null && DialogueRunner.Active != null)
                {
                    bool done = false;
                    if (DialogueRunner.Active.Play(talk, input, () => done = true))
                        while (!done) yield return null;
                }

                // 5. ela some pelo alto, entre as folhas
                Fox("crouch");
                yield return new WaitForSeconds(0.16f);
                Fox("leap");
                SeteVidas.Audio.Sfx.Play("folhas", perch);
                Vector2 a = _fox.transform.position, b = a + new Vector2(left ? 2f : -2f, 7f);
                t = 0f;
                while (t < 0.45f)
                {
                    t += Time.deltaTime;
                    float k = t / 0.45f;
                    _fox.transform.position = Vector2.Lerp(a, b, 1f - (1f - k) * (1f - k));
                    _fox.color = new Color(1f, 1f, 1f, 1f - k * k);
                    yield return null;
                }
                _fox.enabled = false;
            }
            finally
            {
                if (input != null) input.SetLock(this, false);
                if (cat != null) cat.Shielded = false;
                if (creature != null && creature.IsDead) creature.gameObject.SetActive(false);   // não volta no reinício do mundo
            }
        }
    }
}
