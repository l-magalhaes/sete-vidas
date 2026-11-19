using System;
using System.Collections.Generic;
using SeteVidas.Player;
using UnityEngine;

namespace SeteVidas.Enemies.Zombie
{
    /// <summary>
    /// Infectados postos numa cena (Floresta F3, Estrada de Soluki): o mesmo Templário Zumbi da Fortaleza, com ou sem
    /// escudo (<see cref="ZombieAI.GiveShield"/>). Nascem no Start, ou só quando pedido (a horda da ponte,
    /// <see cref="SpawnAll"/>). Feito em tempo de jogo, como o <see cref="ZombieOutbreak"/>.
    /// </summary>
    public class ZombieSquad : MonoBehaviour
    {
        [Serializable]
        public struct Post
        {
            public string name;
            public Vector2 at;
            public int facing;
            [Tooltip("Faixa da patrulha (iguais = vaga até a parede ou a beira)")]
            public float minX, maxX;
            [Tooltip("Infectado de escudo: bloqueia golpes comuns de frente")]
            public bool shield;
        }

        [SerializeField] ZombieSettings settings;
        [SerializeField] CharacterAnimationSet animations;
        [SerializeField] PhysicsMaterial2D bodyMaterial;
        [SerializeField] Sprite shieldSprite;
        [SerializeField] Post[] posts = new Post[0];
        [Tooltip("Desligado = só nascem quando alguém chamar SpawnAll (a horda da ponte)")]
        [SerializeField] bool spawnOnStart = true;

        readonly List<ZombieAI> _zombies = new List<ZombieAI>();

        public IReadOnlyList<ZombieAI> Zombies => _zombies;
        public bool Spawned { get; private set; }

        public void Configure(ZombieSettings zombieSettings, CharacterAnimationSet set, PhysicsMaterial2D material, Sprite shield, Post[] list, bool onStart)
        {
            settings = zombieSettings;
            animations = set;
            bodyMaterial = material;
            shieldSprite = shield;
            posts = list ?? new Post[0];
            spawnOnStart = onStart;
        }

        void Start()
        {
            if (spawnOnStart) SpawnAll(false);
        }

        /// <summary>Faz nascer todos (uma vez). <paramref name="alerted"/>: já correm atrás do gato.</summary>
        public void SpawnAll(bool alerted) => SpawnAll(alerted, null);

        /// <summary>
        /// Como acima; com <paramref name="place"/>, cada um nasce em outro x (recebe o x do posto e a posição na fila, do
        /// mais à direita para trás). A horda da ponte usa isso para nascer fora da tela (pedido do usuário, 19/11/2026).
        /// </summary>
        public void SpawnAll(bool alerted, Func<float, int, float> place)
        {
            if (Spawned) return;
            Spawned = true;
            var order = new List<Post>(posts);
            order.Sort((a, b) => b.at.x.CompareTo(a.at.x));   // o da frente primeiro
            for (int i = 0; i < order.Count; i++)
            {
                var p = order[i];
                if (place != null)
                {
                    float x = place(p.at.x, i);
                    p.at = new Vector2(x, p.at.y);
                    p.minX = p.maxX = x;
                }
                var z = ZombieAI.Spawn(transform, p.name, p.at, p.facing, p.minX, p.maxX, settings, animations, bodyMaterial);
                if (p.shield && shieldSprite != null) z.GiveShield(shieldSprite);
                _zombies.Add(z);
            }
            if (alerted) StartCoroutine(AlertSoon());
        }

        // o Start de cada zumbi (Respawn) zera o estado: avisa depois dele
        System.Collections.IEnumerator AlertSoon()
        {
            yield return null;
            yield return null;
            var cat = PlayerHealth.Active;
            if (cat == null) yield break;
            foreach (var z in _zombies) if (z != null) z.Alert(cat.transform.position.x);
        }
    }
}
