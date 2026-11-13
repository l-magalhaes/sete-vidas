using System;
using System.Collections.Generic;
using SeteVidas.Player;
using SeteVidas.Save;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Enemies.Zombie
{
    /// <summary>
    /// A praga toma a Fortaleza (pedido do usuário, 12/11/2026; spec: Docs/Design/zumbi-templario.md): quando o gato
    /// pega o tomo (marca "tomo_pego" no save), cada templário e cada atirador da cena é desligado e no posto dele
    /// nasce um Templário Zumbi (mesmo lugar, mesma faixa de patrulha). Os postos extras (2 na passagem depois do
    /// tomo e 1 lá embaixo, na estrada) só existem depois do tomo. Ao carregar um save que já tem o tomo, a troca
    /// acontece logo no começo. Feito em tempo de jogo: vale também para os inimigos movidos à mão na cena.
    /// </summary>
    public class ZombieOutbreak : MonoBehaviour
    {
        [Serializable]
        public struct Post
        {
            public string name;
            public Vector2 at;
            public int facing;
            [Tooltip("Faixa da patrulha (iguais = vaga até a parede ou a beira)")]
            public float minX, maxX;
        }

        [SerializeField] ZombieSettings settings;
        [SerializeField] CharacterAnimationSet animations;
        [SerializeField] PhysicsMaterial2D bodyMaterial;
        [Tooltip("Zumbis que só aparecem depois do tomo, além dos que tomam o lugar dos templários")]
        [SerializeField] Post[] extraPosts = new Post[0];

        readonly List<ZombieAI> _zombies = new List<ZombieAI>();
        float _nextCheck;

        public bool HasStarted { get; private set; }
        public IReadOnlyList<ZombieAI> Zombies => _zombies;

        static bool TomeTaken => SaveManager.Data != null && SaveManager.Data.HasFlag(CursedTome.TakenFlag);

        /// <summary>Configura (setup do editor).</summary>
        public void Configure(ZombieSettings zombieSettings, CharacterAnimationSet set, PhysicsMaterial2D material, Post[] extra)
        {
            settings = zombieSettings;
            animations = set;
            bodyMaterial = material;
            extraPosts = extra ?? new Post[0];
        }

        void Start()
        {
            if (TomeTaken) Begin();
        }

        void Update()
        {
            if (HasStarted || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            if (TomeTaken) Begin();
        }

        /// <summary>Troca os templários pelos zumbis (também usado pelos testes).</summary>
        public void Begin()
        {
            if (HasStarted) return;
            HasStarted = true;
            var scene = gameObject.scene;
            foreach (var ai in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include))
            {
                if (ai == null || ai.gameObject.scene != scene) continue;
                var range = ai.PatrolRange;
                Vector2 at = ai.SpawnPoint;
                ai.gameObject.SetActive(false);
                Add("Zumbi: " + ai.name, at, ai.StartFacing, range.x, range.y);
            }
            foreach (var p in extraPosts) Add(p.name, p.at, p.facing, p.minX, p.maxX);
        }

        void Add(string name, Vector2 at, int facing, float minX, float maxX)
        {
            var z = ZombieAI.Spawn(transform, name, at, facing, minX, maxX, settings, animations, bodyMaterial);
            _zombies.Add(z);
        }
    }
}
