using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Um pedaço da trilha da Vulpis (F6): a flecha dela cravada numa árvore ou no chão, ou uma vespa abatida por ela. Só
    /// desenho (sem colisão), atrás do gato. O sprite é feito na hora (<see cref="VulpisTrailArt"/>).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArrowProp : MonoBehaviour
    {
        public enum Kind { Arrow, DeadWasp }

        [SerializeField] Kind kind;
        [Tooltip("Flecha: para onde a haste aponta (graus; 0 = direita, 90 = para cima). A ponta fica enterrada no pivô")]
        [SerializeField] float angle = 150f;

        static readonly Dictionary<int, Sprite> s_arrows = new Dictionary<int, Sprite>();
        static Sprite s_wasp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_arrows.Clear(); s_wasp = null; }   // Play sem recarregar o domínio

        public void Configure(Kind newKind, float degrees) { kind = newKind; angle = degrees; }

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = kind == Kind.DeadWasp ? Wasp() : ArrowAt(angle);
        }

        static Sprite ArrowAt(float degrees)
        {
            int key = Mathf.RoundToInt(degrees);
            if (s_arrows.TryGetValue(key, out var s) && s != null) return s;
            int n = VulpisTrailArt.ArrowSize;
            s = BeeHive.Make(VulpisTrailArt.Arrow(key), n, n, new Vector2(0.5f, 0.5f), "flecha_cravada_" + key);
            s_arrows[key] = s;
            return s;
        }

        static Sprite Wasp()
        {
            if (s_wasp == null)
                s_wasp = BeeHive.Make(VulpisTrailArt.DeadWasp(), VulpisTrailArt.WaspW, VulpisTrailArt.WaspH, new Vector2(0.5f, 0f), "vespa_abatida");
            return s_wasp;
        }
    }
}
