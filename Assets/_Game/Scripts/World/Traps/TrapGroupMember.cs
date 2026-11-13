using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World.Traps
{
    /// <summary>Armadilha de um grupo: some quando o botão do grupo é pisado (e continua desligada no save).</summary>
    public class TrapGroupMember : MonoBehaviour
    {
        [SerializeField] string group = "grupo";

        public void Configure(string trapGroup) => group = trapGroup;

        void Awake() => TrapSwitch.Pressed += OnPressed;
        void OnDestroy() => TrapSwitch.Pressed -= OnPressed;

        void Start()
        {
            if (TrapSwitch.IsOff(SaveManager.Data, group)) gameObject.SetActive(false);
        }

        void OnPressed(string g)
        {
            if (g != group || this == null) return;
            var at = (Vector2)transform.position;
            SeteVidas.Combat.HitSparks.Burst(at, 8, new Color32(0xc0, 0xc6, 0xd2, 0xff), new Color32(0x7a, 0x80, 0x8c, 0xff), 1.5f, 4f, 0.4f, 60f, 0.4f);
            gameObject.SetActive(false);
        }
    }
}
