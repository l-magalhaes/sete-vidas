using System.Collections;
using SeteVidas.Dialogue;
using SeteVidas.Enemies.Boss;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A Vulpis sai do posto (pedido do usuário, 19/11/2026: "após pegarmos a chave, faça a Vulpis falar que nos
    /// encontrará no templo e sair de cena"). Com a Chave do Templo no bolso, ao chegar perto dela na D5, a conversa
    /// D-VULPIS-CHAVE abre sozinha; no fim ela agacha, pula para as copas e some. Depois disso (flag
    /// <see cref="GoneFlag"/>) o posto fica vazio: ela espera no templo (e chega na luta com a Isyathy).
    /// </summary>
    public class VulpisDeparture : MonoBehaviour
    {
        public const string GoneFlag = "vulpis_foi_templo";

        [SerializeField] DialogueDefinition talk;
        [Tooltip("A conversa abre com o gato a esta distância (tiles)")]
        [SerializeField] float radius = 6f;

        bool _running;

        public void Configure(DialogueDefinition dialogue) { talk = dialogue; }

        /// <summary>Ela vai embora agora? Regra pura: vínculo feito, com a chave, ainda no posto e o gato perto.</summary>
        public static bool ShouldLeave(bool bonded, bool hasKey, bool gone, bool catNear) => bonded && hasKey && !gone && catNear;

        void Start()
        {
            var data = SaveManager.Data;
            if (data != null && data.HasFlag(GoneFlag)) gameObject.SetActive(false);   // já está no templo
        }

        void Update()
        {
            if (_running) return;
            var data = SaveManager.Data;
            var cat = PlayerHealth.Active;
            if (data == null || cat == null || cat.IsDead) return;
            bool near = Vector2.Distance(cat.transform.position, transform.position) <= radius;
            if (!ShouldLeave(SeteVidas.Abilities.HunterRules.MovesUnlocked(data), KeyRules.HasKey(data, KeyRules.TempleKey), data.HasFlag(GoneFlag), near)) return;
            var runner = DialogueRunner.Active;
            if (runner == null || runner.IsOpen) return;
            StartCoroutine(Leave(cat));
        }

        IEnumerator Leave(PlayerHealth cat)
        {
            _running = true;
            var data = SaveManager.Data;
            var input = cat.GetComponent<PlayerInputReader>();
            var look = GetComponentInChildren<VulpisVisual>();
            var art = look != null ? look.Renderer : GetComponentInChildren<SpriteRenderer>();
            if (look != null) look.FaceLeft = cat.transform.position.x < transform.position.x;
            // a conversa (o jogo pausa nela)
            if (talk != null && DialogueRunner.Active != null)
            {
                bool done = false;
                if (DialogueRunner.Active.Play(talk, input, () => done = true))
                    while (!done) yield return null;
            }
            data.SetFlag(GoneFlag);
            SaveManager.Save();
            foreach (var label in GetComponentsInChildren<TextMesh>(true)) label.gameObject.SetActive(false);   // o "E: falar"
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            // agacha, pula para o alto (para longe do gato) e some entre as copas, soltando a magia verde dela
            if (look != null) look.Play("crouch");
            yield return new WaitForSeconds(0.18f);
            if (look != null) look.Play("leap");
            SeteVidas.Audio.Sfx.Play("folhas", transform.position);
            Transform body = art != null ? art.transform : transform;
            Vector3 from = body.position;
            float side = cat.transform.position.x < transform.position.x ? 1f : -1f;
            Vector3 to = from + new Vector3(side * 2.5f, 8f, 0f);
            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float k = t / 0.55f;
                body.position = Vector3.Lerp(from, to, 1f - (1f - k) * (1f - k));
                if (art != null) art.color = new Color(1f, 1f, 1f, 1f - k * k);
                if (Random.value < Time.deltaTime * 25f)
                    ForestMagic.One((Vector2)body.position + Random.insideUnitCircle * 0.5f + Vector2.up, Vector2.down * 1.2f, ForestMagic.Mid, 0.45f);
                yield return null;
            }
            SeteVidas.Audio.Sfx.Play("folhas", to);
            gameObject.SetActive(false);
        }
    }
}
