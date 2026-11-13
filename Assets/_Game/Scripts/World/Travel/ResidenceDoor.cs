using SeteVidas.Dialogue;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Porta de residência (pedido do usuário, 13/11/2026): com E, o gato entra numa cena pequena, o interior (a cabana da
    /// Vulpis, e no jogo todo as lojas e outros estabelecimentos). A mesma porta, do lado de dentro, leva de volta para fora.
    /// Pode exigir uma chave na primeira vez (<see cref="KeyRules"/>); destrancada, fica destrancada para sempre.
    /// Convenção: o interior tem uma <see cref="RoomEntrance"/> "entrada" logo depois da porta, e o exterior tem uma
    /// entrada com o id da porta (ex.: "cabana_vulpis") na frente dela.
    /// </summary>
    [SelectionBase]
    public class ResidenceDoor : MonoBehaviour
    {
        [Tooltip("Cena de destino (o interior, ou o exterior se esta é a porta de saída)")]
        [SerializeField] string targetScene = "";
        [Tooltip("Id da RoomEntrance de chegada na cena de destino")]
        [SerializeField] string targetEntrance = "entrada";
        [Tooltip("Nome mostrado no aviso (\"E: entrar\" / \"E: sair\")")]
        [SerializeField] string verb = "entrar";
        [Header("Tranca (opcional)")]
        [SerializeField] string doorId = "";
        [Tooltip("Vazio = sem tranca")]
        [SerializeField] string keyId = "";
        [Header("Visual")]
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite unlockedSprite;
        [SerializeField] TextMesh prompt;
        [Tooltip("Distância (tiles) em que o gato pode usar a porta")]
        [SerializeField, Min(0.5f)] float reach = 1.2f;

        float _barkAt = -10f;

        public string TargetScene => targetScene;
        public bool Locked => !string.IsNullOrEmpty(keyId) && !KeyRules.DoorOpen(SaveManager.Data, doorId);

        public void Configure(string scene, string entrance, string verbText, string door, string key)
        {
            targetScene = scene;
            targetEntrance = entrance;
            verb = verbText;
            doorId = door;
            keyId = key;
        }

        void Start()
        {
            if (prompt != null) prompt.text = "E: " + verb;
            WorldPrompt.Upgrade(prompt);
            RefreshSprite();
        }

        void RefreshSprite()
        {
            if (spriteRenderer == null) return;
            var s = Locked ? lockedSprite : unlockedSprite;
            if (s != null) spriteRenderer.sprite = s;
        }

        void Update()
        {
            var h = PlayerHealth.Active;
            var runner = DialogueRunner.Active;
            Vector2 d = h != null ? (Vector2)(h.transform.position - transform.position) : Vector2.one * 99f;
            bool near = h != null && !h.IsDead && !SceneTravel.IsTraveling && (runner == null || !runner.IsOpen) &&
                        Mathf.Abs(d.x) <= reach && d.y >= -0.5f && d.y <= 2f;
            if (prompt != null) prompt.gameObject.SetActive(near);
            if (!near) return;
            var input = h.GetComponent<PlayerInputReader>();
            if (input == null || !input.isActiveAndEnabled || !input.InteractPressedThisFrame) return;

            var data = SaveManager.Data;
            if (Locked)
            {
                if (!KeyRules.HasKey(data, keyId))
                {
                    if (Time.time - _barkAt > 2f) { SpeechBubble.Show(h.transform, "Trancada. Deve ter uma chave por aí.", 2.2f); _barkAt = Time.time; }
                    SeteVidas.Audio.Sfx.Play("adaga_tink", transform.position, 0.6f, 0.7f); // provisório: maçaneta presa
                    return;
                }
                data.SetFlag(KeyRules.DoorFlag(doorId)); // destrancou: a chave fica na porta
                SaveManager.Save();
                RefreshSprite();
                SeteVidas.Audio.Sfx.Play("grade_abre", transform.position, 0.8f, 1.2f); // provisório
            }
            SeteVidas.Audio.Sfx.Play("santuario_descansa", transform.position, 0.5f, 1.3f); // provisório: a porta rangendo
            SceneTravel.ToEntrance(targetScene, targetEntrance, h);
        }
    }
}
