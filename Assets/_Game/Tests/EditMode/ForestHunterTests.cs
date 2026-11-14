using System.Collections.Generic;
using NUnit.Framework;
using SeteVidas.Abilities;
using SeteVidas.Classes;
using SeteVidas.Save;
using SeteVidas.UI;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F4b da Floresta: Vulpis, Caçador e a Chave do Templo (Docs/Floresta/plano-f4b.md). Regras puras do dash, da
    /// flecha com corda e do arco; o coração do Caçador na ordem de ganho; a chave do templo.
    /// </summary>
    public class ForestHunterTests
    {
        [Test]
        public void Moves_UnlockWithTheBond_OrTheClass()
        {
            var data = SaveData.NewGame();
            Assert.IsFalse(HunterRules.MovesUnlocked(data), "Antes da Vulpis: sem dash nem gancho");
            data.SetFlag(HunterRules.BondFlag);
            Assert.IsTrue(HunterRules.MovesUnlocked(data));
            var other = SaveData.NewGame();
            other.unlockedClasses.Add(nameof(CharacterClass.Hunter));
            Assert.IsTrue(HunterRules.MovesUnlocked(other), "Save com o Caçador (testes, saves antigos)");
            Assert.IsFalse(HunterRules.MovesUnlocked(null));
        }

        [Test]
        public void Dash_IsGenerous_OneInTheAir()
        {
            Assert.AreEqual(6f, HunterRules.DashSpeed * HunterRules.DashTime, 1e-4f, "6 tiles por dash");
            Assert.Greater(HunterRules.DashDistance, 4f, "Vence o vão que o pulo (4) não vence");
            Assert.IsTrue(HunterRules.CanDash(grounded: true, airDashReady: false, sinceLastDash: 1f), "No chão, sempre");
            Assert.IsFalse(HunterRules.CanDash(true, true, 0.1f), "Recarga no chão");
            Assert.IsTrue(HunterRules.CanDash(false, true, 1f), "No ar, se tem o dash do ar");
            Assert.IsFalse(HunterRules.CanDash(false, false, 1f), "Já usou o do ar");
        }

        [Test]
        public void Dash_GoesWherePressed_OrWhereFacing()
        {
            Assert.AreEqual(1, HunterRules.DashDirection(0.8f, -1));
            Assert.AreEqual(-1, HunterRules.DashDirection(-1f, 1));
            Assert.AreEqual(-1, HunterRules.DashDirection(0f, -1), "Sem direção: para onde olha");
        }

        [Test]
        public void Grapple_Aim_EightDirections_NeverIntoTheFloor()
        {
            Assert.AreEqual(Vector2.right, HunterRules.AimDirection(0f, 0f, 1, true), "Parado: para a frente");
            Assert.AreEqual(Vector2.up, HunterRules.AimDirection(0f, 1f, 1, false));
            Assert.AreEqual(new Vector2(-1f, 1f).normalized, HunterRules.AimDirection(-1f, 1f, 1, false));
            Assert.AreEqual(Vector2.right, HunterRules.AimDirection(1f, -1f, 1, true), "No chão, para baixo vira para a frente");
            Assert.AreEqual(new Vector2(1f, -1f).normalized, HunterRules.AimDirection(1f, -1f, 1, false), "No ar, pode mirar para baixo");
        }

        [Test]
        public void Grapple_AutoAim_PicksAlignedAnchor_InRangeAndCone()
        {
            Vector2 from = Vector2.zero, aim = Vector2.right;
            Assert.Less(HunterRules.AnchorScore(from, aim, new Vector2(5f, 0f)), 0f + 1f, "Reto à frente: vale");
            Assert.Less(HunterRules.AnchorScore(from, aim, new Vector2(10f, 0f)), 0f, "Longe demais (9)");
            Assert.Less(HunterRules.AnchorScore(from, aim, new Vector2(0f, 5f)), 0f, "Fora do cone de 30°");
            float straight = HunterRules.AnchorScore(from, aim, new Vector2(6f, 0f));
            float tilted = HunterRules.AnchorScore(from, aim, new Vector2(5f, 2f));
            Assert.Less(straight, tilted, "O mais alinhado ganha");
        }

        [Test]
        public void Grapple_Pull_StopsAtTheAnchor()
        {
            var v = HunterRules.PullVelocity(Vector2.zero, new Vector2(5f, 0f));
            Assert.AreEqual(HunterRules.GrapplePull, v.magnitude, 1e-3f);
            Assert.AreEqual(Vector2.zero, HunterRules.PullVelocity(Vector2.zero, new Vector2(0.3f, 0f)), "Chegou");
        }

        [Test]
        public void Bow_FireRate_UpToTwoArrows()
        {
            Assert.IsTrue(HunterRules.CanShoot(1f, 0));
            Assert.IsTrue(HunterRules.CanShoot(0.35f, 1), "Melhorias 5: 0,35 s e até 2 no ar");
            Assert.IsFalse(HunterRules.CanShoot(0.2f, 0), "Intervalo");
            Assert.IsFalse(HunterRules.CanShoot(1f, 2), "Já tem 2 flechas no ar");
            Assert.AreEqual(Vector2.up, HunterRules.ArrowDirection(true, -1));
            Assert.AreEqual(Vector2.left, HunterRules.ArrowDirection(false, -1));
        }

        [Test]
        public void Bow_ArrowIs40PercentOfADaggerHit_CarryingTheRest()
        {
            float carry = 0f;
            Assert.AreEqual(1, HunterRules.ArrowHit(2, ref carry), "Adaga 2: flecha 1 (o mínimo)");
            Assert.AreEqual(0f, carry, 1e-4f, "Abaixo de 1 não acumula: o mínimo já cobriu");
            int sum = 0;
            for (int i = 0; i < 5; i++) sum += HunterRules.ArrowHit(3, ref carry);
            Assert.AreEqual(6, sum, "Adaga 3 (com o bônus dos corações): 5 flechas = 2 golpes de adaga (1, 1, 1, 1, 2)");
            carry = 0f;
            Assert.AreEqual(1, HunterRules.ArrowHit(4, ref carry), "Adaga 4: 1,6 → 1, sobra 0,6");
            Assert.AreEqual(2, HunterRules.ArrowHit(4, ref carry), "e a próxima: 2,2 → 2");
        }

        [Test]
        public void Bow_AimSnapsTo15Degrees_From45DownToStraightUp()
        {
            int side;
            Assert.AreEqual(0f, HunterRules.AimAngle(Vector2.zero, -1, out side), "Sem direção: reto");
            Assert.AreEqual(-1, side, "Para onde olha");
            Assert.AreEqual(45f, HunterRules.AimAngle(new Vector2(1f, 1f), -1, out side));
            Assert.AreEqual(1, side, "O direcional manda no lado");
            Assert.AreEqual(90f, HunterRules.AimAngle(Vector2.up, 1, out side));
            Assert.AreEqual(-45f, HunterRules.AimAngle(Vector2.down, 1, out side), "Para baixo: no máximo 45°");
            Assert.AreEqual(30f, HunterRules.AimAngle(new Vector2(0.9f, 0.5f), 1, out side), "Analógico: 29° vira 30°");
            var up = HunterRules.BowAim(Vector2.up, 1);
            Assert.AreEqual(0f, up.x, 1e-4f);
            Assert.AreEqual(1f, up.y, 1e-4f);
        }

        [Test]
        public void Bow_AimAssist_NearestInFront_NoLockBehind()
        {
            var from = Vector2.zero;
            var targets = new List<Vector2> { new Vector2(8f, 2f), new Vector2(4f, -1.5f), new Vector2(-2f, 0f), new Vector2(30f, 0f) };
            var dir = HunterRules.AssistAim(from, Vector2.right, 1, false, targets);
            Assert.AreEqual(new Vector2(4f, -1.5f).normalized.x, dir.x, 1e-4f, "Sem apontar: o mais perto na frente");
            Assert.AreEqual(Vector2.left, HunterRules.AssistAim(from, Vector2.left, -1, false, new List<Vector2> { new Vector2(3f, 0f) }),
                "O inimigo atrás não puxa a flecha");
            var up = HunterRules.BowAim(Vector2.up, 1);
            Assert.AreEqual(up, HunterRules.AssistAim(from, up, 1, true, targets), "Apontando para cima: nada no cone, vale a mira");
            var diag = HunterRules.AssistAim(from, new Vector2(1f, 0.3f).normalized, 1, true, new List<Vector2> { new Vector2(8f, 2f) });
            Assert.AreEqual(new Vector2(8f, 2f).normalized.y, diag.y, 1e-4f, "Apontando perto: corrige até o alvo");
        }

        [Test]
        public void HunterHeart_IsTheThird_InUnlockOrder()
        {
            var order = new List<string> { "Rogue", "Paladin", "Hunter" };
            Assert.AreEqual((int)CharacterClass.Rogue, HeartsHud.HeartClass(0, true, order));
            Assert.AreEqual((int)CharacterClass.Paladin, HeartsHud.HeartClass(1, true, order));
            Assert.AreEqual((int)CharacterClass.Hunter, HeartsHud.HeartClass(2, true, order), "O 3º coração é o do Caçador (flecha)");
            Assert.AreEqual((int)CharacterClass.Mage, HeartsHud.HeartClass(3, true, order), "Depois, as que faltam na ordem");
            Assert.AreEqual(0, HeartsHud.HeartClass(2, false, order), "Antes da maldição: todos do Ladino");
            Assert.AreEqual(2, HeartsHud.HeartClass(2, true, null), "Sem lista: a ordem antiga");
        }

        [Test]
        public void TempleKey_OpensOnlyTheTempleGate()
        {
            var data = SaveData.NewGame();
            Assert.IsFalse(KeyRules.CanOpen(data, KeyRules.TempleGate, KeyRules.TempleKey));
            data.SetFlag(KeyRules.KeyFlag("cabana"));
            Assert.IsFalse(KeyRules.CanOpen(data, KeyRules.TempleGate, KeyRules.TempleKey), "A chave da cabana não serve");
            data.SetFlag(KeyRules.KeyFlag(KeyRules.TempleKey));
            Assert.IsTrue(KeyRules.CanOpen(data, KeyRules.TempleGate, KeyRules.TempleKey));
            Assert.AreNotEqual(KeyRules.Name("cabana"), KeyRules.Name(KeyRules.TempleKey), "Nomes diferentes no inventário");
            Assert.IsTrue(KeyRules.IsTempleKey("templo"));
            Assert.IsFalse(KeyRules.IsTempleKey("cabana"));
            data.SetFlag(KeyRules.DoorFlag(KeyRules.TempleGate));
            Assert.IsFalse(KeyRules.KeysInPocket(data, KeyRules.DoorOfKey).Contains(KeyRules.TempleKey), "Usada: some do bolso");
        }
    }
}
