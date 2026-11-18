using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// O espírito da Isyathy vivo no ar: respira (estica de leve) e solta fiapos de luz azul-clara do corpo e do rabo.
    /// No tempo real: continua durante as conversas com retrato, que pausam o jogo.
    /// </summary>
    public class SpiritFloat : MonoBehaviour
    {
        static readonly Color Mote = new Color(0.75f, 0.95f, 1f), MoteLight = new Color(0.92f, 1f, 1f);
        SpriteRenderer _sr;
        float _t;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            transform.localScale = new Vector3(1f, 1f + 0.025f * Mathf.Sin(_t * 2.2f), 1f);
            if (_sr == null || _sr.color.a < 0.2f || Random.value > dt * 10f) return;
            // do corpo (de 0,5 a 6 tiles acima do pé) e, mais, do rabo que se desfaz
            float h = Random.value < 0.4f ? Random.Range(0f, 1.2f) : Random.Range(0.5f, 6f);
            Vector2 at = (Vector2)transform.position + new Vector2(Random.Range(-1.4f, 1.4f), h);
            SeteVidas.Enemies.Boss.ForestMagic.One(at, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.4f)),
                Random.value < 0.5f ? Mote : MoteLight, Random.Range(0.6f, 1.1f));
        }
    }
}
