using System.Collections;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.UI;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Comentário do Lion ao chegar num lugar, uma vez por jogo (flag "fala_&lt;key&gt;"): balões curtos em sequência, sem
    /// parar o jogo. Ex.: ao entrar na cabana da Vulpis.
    /// </summary>
    public class SceneBark : MonoBehaviour
    {
        [SerializeField] string key = "lugar";
        [SerializeField] string[] lines = new string[0];
        [Tooltip("Espera antes da primeira fala (s), depois de a tela clarear")]
        [SerializeField, Min(0f)] float delay = 0.8f;
        [SerializeField, Min(0.5f)] float secondsPerLine = 2.6f;

        public void Configure(string flagKey, float wait, params string[] text)
        {
            key = flagKey;
            delay = wait;
            lines = text;
        }

        IEnumerator Start()
        {
            var data = SaveManager.Data;
            string flag = "fala_" + key;
            if (data == null || data.HasFlag(flag) || lines == null || lines.Length == 0) yield break;
            yield return new WaitForSeconds(delay);
            data.SetFlag(flag);
            foreach (var line in lines)
            {
                var h = PlayerHealth.Active;
                if (h == null) yield break;
                SpeechBubble.Show(h.transform, line, secondsPerLine);
                yield return new WaitForSeconds(secondsPerLine);
            }
        }
    }
}
