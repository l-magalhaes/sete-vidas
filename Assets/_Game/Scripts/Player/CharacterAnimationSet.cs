using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>Um frame de animação com as âncoras que o código usa (em pixels, relativas ao pivot nos pés).</summary>
    [Serializable]
    public class SpriteFrame
    {
        public Sprite sprite;
        [Tooltip("Camada da arma da classe neste frame (ex.: marreta no golpe). Vazio = nada")]
        public Sprite overlay;
        [Tooltip("Onde a cauda nasce (px)")]
        public Vector2 tail;
        [Tooltip("Peito: centelha da última vida (px)")]
        public Vector2 chest;
        [Tooltip("Centro da faixa da capa na cor da classe (px)")]
        public Vector2 trim;
        [Tooltip("Deslocamento do tronco em relação à pose em pé (px): a marreta nas costas acompanha")]
        public Vector2Int body;
        public bool hasTail = true;
        public bool hasTrim;
    }

    [Serializable]
    public class SpriteClip
    {
        public string name;
        [Min(0.01f)] public float frameTime = 0.1f;
        public bool loop = true;
        public SpriteFrame[] frames;

        public int Count => frames == null ? 0 : frames.Length;
        public float Duration => frameTime * Count;

        /// <summary>Frame pelo tempo desde o início (com loop ou segurando o último).</summary>
        public SpriteFrame At(float time)
        {
            if (Count == 0) return null;
            int i = Mathf.Max(0, (int)(time / frameTime));
            i = loop ? i % Count : Mathf.Min(i, Count - 1);
            return frames[i];
        }

        /// <summary>Frame por progresso 0..1 (fases de ataque, subida de borda).</summary>
        public SpriteFrame AtNormalized(float t)
        {
            if (Count == 0) return null;
            return frames[Mathf.Clamp((int)(Mathf.Clamp01(t) * Count), 0, Count - 1)];
        }

        public int IndexAt(float time)
        {
            if (Count == 0) return 0;
            int i = Mathf.Max(0, (int)(time / frameTime));
            return loop ? i % Count : Mathf.Min(i, Count - 1);
        }
    }

    /// <summary>
    /// Todas as animações e efeitos de um personagem desenhado em pixel art (ver SeteVidasDocs/Lion_Arte_e_Animacoes.md).
    /// O asset do Lion é gerado por Tools/LionSprites (não edite os frames à mão: rode o gerador de novo).
    /// </summary>
    [CreateAssetMenu(menuName = "Sete Vidas/Character Animation Set", fileName = "CharacterAnimationSet")]
    public class CharacterAnimationSet : ScriptableObject
    {
        public SpriteClip[] clips;

        [Header("Efeitos")]
        public Sprite[] dustJump;
        public Sprite[] dustLand;
        public Sprite[] dustStep;
        public Sprite[] dustWall;
        public Sprite[] backstab;
        [Tooltip("Centelha branca, tingida pela cor da classe (morte e renascimento)")]
        public Sprite[] spark;
        [Tooltip("Anel branco, tingido pela cor da nova classe")]
        public Sprite[] classSwitch;

        [Header("Estado no personagem")]
        public Sprite trimPixel;
        public Sprite chestSpark;

        Dictionary<string, SpriteClip> _map;

        void OnEnable() => _map = null;
        void OnValidate() => _map = null;

        public SpriteClip Get(string clipName)
        {
            if (_map == null)
            {
                _map = new Dictionary<string, SpriteClip>();
                if (clips != null)
                    foreach (var c in clips)
                        if (c != null && !string.IsNullOrEmpty(c.name) && c.Count > 0) _map[c.name] = c;
            }
            return clipName != null && _map.TryGetValue(clipName, out var clip) ? clip : null;
        }

        public bool Has(string clipName) => Get(clipName) != null;
    }
}
