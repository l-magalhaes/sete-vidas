using System.Collections.Generic;
using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Enemies
{
    /// <summary>
    /// Variante <b>infectada</b> de um inimigo da Floresta (F4, Docs/Design/floresta-bestiario.md): o componente vai no
    /// mesmo objeto da IA (<see cref="ForestEnemy"/> ou <see cref="SeteVidas.World.FloatingCactus"/>), que lê
    /// <see cref="On"/> no Awake e troca os números (<see cref="ForestEnemyRules"/>).
    /// <para>Aqui fica só o visual: troca cada sprite pelo par infectado (paleta da praga, roxo #5b2a6e e verde-ácido
    /// #8fd14f, gerados pelo setup do editor), tinge de roxo o que não tem par (gomos feitos na hora) e solta, de vez em
    /// quando, um pontinho verde dos olhos.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class Infected : MonoBehaviour
    {
        public static readonly Color32 Purple = new Color32(0x5b, 0x2a, 0x6e, 0xff);
        public static readonly Color32 Acid = new Color32(0x8f, 0xd1, 0x4f, 0xff);
        /// <summary>Tinta dos desenhos sem par infectado (multiplica: puxa para o roxo).</summary>
        public static readonly Color Tint = new Color(0.78f, 0.62f, 0.9f, 1f);

        [Tooltip("Sprites normais (na mesma ordem dos infectados)")]
        [SerializeField] Sprite[] normal = new Sprite[0];
        [SerializeField] Sprite[] infected = new Sprite[0];
        [Tooltip("De onde saem os pontinhos verdes (a partir do objeto, em tiles)")]
        [SerializeField] Vector2 eyes = new Vector2(0f, 0.6f);

        readonly Dictionary<Sprite, Sprite> _map = new Dictionary<Sprite, Sprite>();
        readonly HashSet<Sprite> _done = new HashSet<Sprite>();
        SpriteRenderer[] _renderers = new SpriteRenderer[0];
        float _nextGlow;

        /// <summary>Este objeto é a variante infectada?</summary>
        public static bool On(Component c) => c != null && c.GetComponent<Infected>() != null;

        /// <summary>Para o setup do editor.</summary>
        public void Configure(Sprite[] from, Sprite[] to, Vector2 eyeOffset)
        {
            normal = from ?? new Sprite[0];
            infected = to ?? new Sprite[0];
            eyes = eyeOffset;
        }

        void Awake()
        {
            int n = Mathf.Min(normal.Length, infected.Length);
            for (int i = 0; i < n; i++)
            {
                if (normal[i] == null || infected[i] == null) continue;
                _map[normal[i]] = infected[i];
                _done.Add(infected[i]);
            }
            _nextGlow = Random.Range(0.5f, 2f);
            _enemy = GetComponent<ForestEnemy>();
        }

        ForestEnemy _enemy;

        // os gomos da Planta e da Serpente nascem no Awake da IA: procura os desenhos depois
        void Start() => Refresh();

        /// <summary>Procura de novo os desenhos (filhos criados depois).</summary>
        public void Refresh() => _renderers = GetComponentsInChildren<SpriteRenderer>(true);

        void LateUpdate()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null || r.sprite == null) continue;
                if (_map.TryGetValue(r.sprite, out var swap)) { r.sprite = swap; r.color = Color.white; }
                else if (!_done.Contains(r.sprite)) r.color = Tint;
            }

            _nextGlow -= Time.deltaTime;
            if (_nextGlow > 0f) return;
            _nextGlow = Random.Range(1.2f, 2.6f);
            if (_enemy != null && _enemy.IsDead) return;
            Vector2 at = (Vector2)transform.TransformPoint(eyes);
            HitSparks.Rise(at, Acid, 0.8f, 0.7f);
        }
    }
}
