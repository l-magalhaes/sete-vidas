using System.Collections.Generic;
using UnityEngine;

namespace SeteVidas.Player
{
    /// <summary>
    /// Efeito de sprite de uma vez só (poeira, centelha, estrela do ataque furtivo, rastro).
    /// Usa um pool para não criar objetos a cada passo. Segue o tempo do jogo: congela no hitstop.
    /// </summary>
    public class SpriteFx : MonoBehaviour
    {
        static readonly Stack<SpriteFx> s_pool = new Stack<SpriteFx>();
        static Transform s_root;

        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _frameTime, _time, _life;
        Vector3 _velocity;
        Color _color;
        bool _fade;
        Vector3 _target;
        bool _homing;

        /// <summary>Toca os frames uma vez. <paramref name="life"/> &gt; 0 mantém o último frame até esse tempo.</summary>
        public static SpriteFx Play(Sprite[] frames, float frameTime, Vector3 position, bool flipX, Color color,
                                    int sortingLayerId, int sortingOrder, Vector3 velocity = default, float life = 0f, bool fade = false)
        {
            if (frames == null || frames.Length == 0 || !Application.isPlaying) return null;
            var fx = Get();
            fx._frames = frames;
            fx._frameTime = Mathf.Max(0.01f, frameTime);
            fx._time = 0f;
            fx._life = life > 0f ? life : frames.Length * fx._frameTime;
            fx._velocity = velocity;
            fx._color = color;
            fx._fade = fade;
            fx._homing = false;
            fx.transform.position = position;
            var r = fx._renderer;
            r.sprite = frames[0];
            r.flipX = flipX;
            r.color = color;
            r.sortingLayerID = sortingLayerId;
            r.sortingOrder = sortingOrder;
            fx.gameObject.SetActive(true);
            return fx;
        }

        /// <summary>Faz o efeito viajar até <paramref name="target"/> no tempo de vida (centelhas do renascimento).</summary>
        public SpriteFx Toward(Vector3 target)
        {
            _target = target;
            _homing = true;
            _velocity = (target - transform.position) / Mathf.Max(0.01f, _life);
            return this;
        }

        static SpriteFx Get()
        {
            while (s_pool.Count > 0)
            {
                var pooled = s_pool.Pop();
                if (pooled != null) return pooled;
            }
            if (s_root == null)
            {
                var root = new GameObject("[SpriteFx]");
                s_root = root.transform;
            }
            var go = new GameObject("Fx");
            go.transform.SetParent(s_root, false);
            var fx = go.AddComponent<SpriteFx>();
            fx._renderer = go.AddComponent<SpriteRenderer>();
            return fx;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;
            transform.position += _velocity * dt;
            if (_homing && _time >= _life) transform.position = _target;

            int i = Mathf.Min((int)(_time / _frameTime), _frames.Length - 1);
            _renderer.sprite = _frames[i];
            if (_fade)
            {
                var c = _color;
                c.a *= 1f - Mathf.Clamp01(_time / _life);
                _renderer.color = c;
            }
            if (_time >= _life) Release();
        }

        void Release()
        {
            gameObject.SetActive(false);
            s_pool.Push(this);
        }
        // Ao trocar de cena os efeitos são destruídos junto com o [SpriteFx]: o Get() descarta as referências mortas.
    }
}
