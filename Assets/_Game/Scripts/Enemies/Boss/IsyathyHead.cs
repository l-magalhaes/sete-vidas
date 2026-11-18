using SeteVidas.Combat;
using UnityEngine;

namespace SeteVidas.Enemies.Boss
{
    /// <summary>
    /// Um ponto de golpe da Isyathy: a cabeça (o ponto fraco), a ponta do rabo no Arco (pedido do usuário, 17/11/2026:
    /// "para que possamos atacar dos dois lados") ou um pedaço do corpo fora da terra (escamas grossas: na espera, meio
    /// dano; fora dela, resvala, mas o golpe nunca atravessa sem resposta). Um gatilho na layer Enemy que acompanha o
    /// desenho; passa o golpe para o <see cref="IsyathyBoss"/>. Aceita pogo enquanto está fora da terra.
    /// </summary>
    public class IsyathyHead : MonoBehaviour, IDamageable, IPogoTarget
    {
        public enum Part { Head, Tail, Body }

        IsyathyBoss _boss;
        Part _part;

        public Part Kind => _part;

        public void Bind(IsyathyBoss boss, Part part = Part.Head) { _boss = boss; _part = part; }

        public bool AcceptsPogo => _boss != null && (_part == Part.Tail ? _boss.TailOut : _boss.HeadOut || _part == Part.Body);

        public void TakeDamage(in DamageInfo info)
        {
            if (_boss == null) return;
            switch (_part)
            {
                case Part.Tail: _boss.HitTail(info); break;
                case Part.Body: _boss.HitBody(info, transform.position); break;
                default: _boss.HitHead(info); break;
            }
        }
    }
}
