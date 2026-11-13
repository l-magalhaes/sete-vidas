using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Gancho na ponta de uma parede alta (pedido do usuário, 13/11/2026): ponto de ancoragem que só a classe
    /// <b>Arqueiro</b> vai usar (flecha com corda, capítulo futuro). Por enquanto é só o marcador: o gato vê o gancho e
    /// o que está atrás dele (baú, moedas), mas não chega. Serve de gate para a volta à Floresta (backtracking).
    /// </summary>
    [SelectionBase]
    public class GrappleAnchor : MonoBehaviour
    {
        [Tooltip("Classe que usa o gancho (documentação; a habilidade ainda não existe)")]
        [SerializeField] string requiredClass = "Arqueiro";
        [SerializeField] Transform visual;

        public string RequiredClass => requiredClass;

        void Update()
        {
            // brilho leve para o jogador notar que é "algo" (e voltar quando tiver a habilidade)
            if (visual != null) visual.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2f) * 0.04f);
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
