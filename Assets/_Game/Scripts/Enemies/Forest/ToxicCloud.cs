using SeteVidas.Combat;
using SeteVidas.Core;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Nuvem tóxica da praga (F4): o Galho Seco infectado solta uma ao morrer (2 × 2 tiles, 2 s) e a Planta infectada
    /// solta esporos ao ser atingida (1,5 tile, 1,5 s). Quem entra é envenenado uma vez por nuvem
    /// (<see cref="ForestEnemyRules.CloudPoisons"/>). Feita na hora, sem prefab; some ao reiniciar o mundo.
    /// </summary>
    public class ToxicCloud : MonoBehaviour
    {
        Rect _area;
        float _left, _total, _nextPuff;
        bool _poisoned;

        /// <summary>Solta uma nuvem com o centro em <paramref name="center"/>.</summary>
        public static ToxicCloud Spawn(Vector2 center, Vector2 size, float seconds)
        {
            var go = new GameObject("Nuvem tóxica");
            go.transform.position = center;
            var c = go.AddComponent<ToxicCloud>();
            c._area = new Rect(center - size * 0.5f, size);
            c._left = c._total = Mathf.Max(0.1f, seconds);
            // a nuvem aparece de uma vez (antes de o gato poder sair)
            HitSparks.Burst(center, 14, Infected.Acid, Infected.Purple, 0.6f, 2.2f, 0.6f, -0.5f, 0.2f);
            SeteVidas.Audio.Sfx.Play("veneno_tique", center, 0.5f, 0.7f); // provisório: o chiado dos esporos
            return c;
        }

        public bool IsAlive => _left > 0f;
        public Rect Area => _area;

        void OnEnable() => GameEvents.WorldReset += Vanish;
        void OnDisable() => GameEvents.WorldReset -= Vanish;
        void Vanish() { if (this != null) Destroy(gameObject); }

        void Update()
        {
            _left -= Time.deltaTime;
            if (_left <= 0f) { Destroy(gameObject); return; }

            // fumacinha: pontinhos que sobem devagar dentro da área, rareando no fim
            _nextPuff -= Time.deltaTime;
            if (_nextPuff <= 0f)
            {
                _nextPuff = Mathf.Lerp(0.12f, 0.04f, _left / _total);
                var p = new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));
                HitSparks.Rise(p, Random.value < 0.6f ? Infected.Acid : Infected.Purple, 0.6f, 0.6f);
            }

            var h = PlayerHealth.Active;
            if (h == null || h.IsDead) return;
            Vector2 cat = (Vector2)h.transform.position + Vector2.up * 0.7f;
            if (!ForestEnemyRules.CloudPoisons(_poisoned, _area, cat)) return;
            _poisoned = true;
            h.Poison();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.56f, 0.82f, 0.31f, 0.5f);
            Gizmos.DrawWireCube(_area.center, _area.size);
        }
#endif
    }
}
