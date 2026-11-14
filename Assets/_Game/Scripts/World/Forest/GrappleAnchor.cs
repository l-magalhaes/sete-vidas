using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Gancho (pedido do usuário, 13/11/2026): ponto de ancoragem da <b>flecha com corda</b> do Caçador (F4b,
    /// <see cref="SeteVidas.Abilities.GrappleAbility"/>). A corda prende aqui e puxa o gato até ele. Antes do vínculo
    /// com a Vulpis é só um marcador: o gato vê o gancho e o que está atrás dele, mas não chega.
    /// </summary>
    [SelectionBase]
    public class GrappleAnchor : MonoBehaviour
    {
        static readonly List<GrappleAnchor> s_all = new List<GrappleAnchor>();
        /// <summary>Ganchos ativos na cena (a mira automática procura aqui).</summary>
        public static IReadOnlyList<GrappleAnchor> All => s_all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_all.Clear(); // Play sem recarregar o domínio

        [Tooltip("Classe que usa o gancho (documentação)")]
        [SerializeField] string requiredClass = "Caçador";
        [SerializeField] Transform visual;

        public string RequiredClass => requiredClass;
        /// <summary>Onde a corda prende.</summary>
        public Vector2 Point => transform.position;

        float _flash;

        void OnEnable() { if (!s_all.Contains(this)) s_all.Add(this); }
        void OnDisable() => s_all.Remove(this);

        /// <summary>A corda prendeu aqui: o gancho dá um brilho.</summary>
        public void OnHooked() => _flash = 0.25f;

        void Update()
        {
            // brilho leve para o jogador notar que é "algo" (e voltar quando tiver a habilidade)
            if (visual == null) return;
            _flash -= Time.deltaTime;
            float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.04f + Mathf.Max(0f, _flash) * 0.8f;
            visual.localScale = Vector3.one * pulse;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.8f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
#endif
    }
}
