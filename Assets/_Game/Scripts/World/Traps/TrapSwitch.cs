using System;
using SeteVidas.Player;
using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>
    /// Botão no chão (desenho do usuário, 13/11/2026): pisar desliga para sempre as armadilhas do grupo dele
    /// (flag "armadilhas_desligadas_&lt;grupo&gt;", salva na hora). As armadilhas do grupo têm <see cref="TrapGroupMember"/>.
    /// Pensado para o caminho de volta: passa-se pelas armadilhas uma vez, e o botão do outro lado as desliga.
    /// </summary>
    [SelectionBase]
    public class TrapSwitch : MonoBehaviour
    {
        public const string FlagPrefix = "armadilhas_desligadas_";
        public static event Action<string> Pressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Pressed = null; // Play sem recarregar o domínio

        [SerializeField] string group = "grupo";
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite up, down;
        [Tooltip("Largura do botão (tiles)")]
        [SerializeField, Min(0.5f)] float width = 1.5f;

        public static string Flag(string group) => FlagPrefix + group;
        public static bool IsOff(SaveData data, string group) => data != null && data.HasFlag(Flag(group));

        public void Configure(string trapGroup) => group = trapGroup;

        void Start() => Refresh();

        void Refresh()
        {
            if (spriteRenderer == null) return;
            var s = IsOff(SaveManager.Data, group) ? down : up;
            if (s != null) spriteRenderer.sprite = s;
        }

        void FixedUpdate()
        {
            var data = SaveManager.Data;
            if (data == null || IsOff(data, group)) return;
            var h = PlayerHealth.Active;
            if (h == null || h.IsDead) return;
            var pc = h.GetComponent<PlayerController>();
            if (pc == null || !pc.IsGrounded) return;
            Vector2 feet = h.transform.position;
            if (Mathf.Abs(feet.x - transform.position.x) > width * 0.5f || Mathf.Abs(feet.y - transform.position.y) > 0.4f) return;

            data.SetFlag(Flag(group));
            SaveManager.Save();
            Refresh();
            SeteVidas.Audio.Sfx.Play("grade_abre", transform.position, 0.7f, 1.4f); // provisório: clique e mecanismo
            SeteVidas.UI.SpeechBubble.Show(h.transform, "Clique. Melhor assim.", 2f);
            Pressed?.Invoke(group);
        }
    }

}
