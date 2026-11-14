using SeteVidas.Save;
using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// Destroços da Ponte do Último Marco no fundo do vale (pedido do usuário, 14/11/2026): na primeira vez que o gato chega
    /// (caindo com a ponte), os pedaços caem do alto junto com ele, um depois do outro, e ficam no chão como cenário. Das
    /// outras vezes já estão lá (flag <see cref="Flag"/>). Cada filho é um pedaço, já na posição de repouso.
    /// </summary>
    public class DebrisFall : MonoBehaviour
    {
        public const string Flag = "ponte_destrocos_no_vale";

        [Tooltip("De quanto mais alto cada pedaço começa (tiles)")]
        [SerializeField] float dropHeight = 22f;
        [SerializeField] float gravity = 40f;
        [Tooltip("Intervalo entre um pedaço e o seguinte (s)")]
        [SerializeField] float stagger = 0.18f;

        Transform[] _pieces;
        Vector3[] _rest;
        float[] _speed;
        float _t;
        bool _falling;

        void Start()
        {
            if (SaveManager.Data == null || SaveManager.Data.HasFlag(Flag)) return;   // já caíram antes
            int n = transform.childCount;
            _pieces = new Transform[n]; _rest = new Vector3[n]; _speed = new float[n];
            for (int i = 0; i < n; i++)
            {
                _pieces[i] = transform.GetChild(i);
                _rest[i] = _pieces[i].position;
                _pieces[i].position = _rest[i] + Vector3.up * (dropHeight + i * 1.5f);
            }
            _falling = true;
        }

        void Update()
        {
            if (!_falling) return;
            _t += Time.deltaTime;
            bool all = true;
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i] == null || _t < i * stagger) { all &= _pieces[i] == null; continue; }
                var p = _pieces[i].position;
                if (p.y <= _rest[i].y) continue;
                _speed[i] += gravity * Time.deltaTime;
                p.y = Mathf.Max(_rest[i].y, p.y - _speed[i] * Time.deltaTime);
                _pieces[i].position = p;
                if (p.y > _rest[i].y) all = false;
                else SeteVidas.Audio.Sfx.Play("koda_impacto", p, 0.45f, 0.8f);   // o baque da pedra (provisório)
            }
            if (!all) return;
            _falling = false;
            SaveManager.Data.SetFlag(Flag);
        }
    }
}
