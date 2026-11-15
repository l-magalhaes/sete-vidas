using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SeteVidas.World;
using UnityEngine;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F5 da Floresta, parte 2: as Ruínas dos Primatas (placas e objetos), o catálogo do Diário e a Isyathy vista de
    /// longe. A posição de cada coisa está no ForestLayout (conferida por Tools/LevelPreview/checar_floresta.ps1).
    /// </summary>
    public class ForestRuinsTests
    {
        const string Art = "Assets/_Game/Art/Environments/Forest/Placeholder";

        [Test]
        public void GeneratedArt_HasRuinsAndSightings()
        {
            var missing = new List<string>();
            foreach (var n in new[]
            {
                "pri_placa_estrada", "pri_mapa", "pri_vitrine", "pri_farol_0", "pri_farol_1", "pri_painel", "pri_carcaca", "pri_lapide",
                "pri_cabine_mesa", "pri_poste", "pri_poste_tombado", "pri_maquina_0", "pri_maquina_1", "pri_janela", "pri_escada",
                "pri_relogio", "isy_olhos_0", "isy_olhos_1", "isy_olhos_2", "isy_silhueta",
                "isy_fundo_0", "isy_fundo_1", "isy_fundo_2", "isy_fundo_3", "isy_fundo_4", "isy_fundo_5", "isy_fundo_6", "doc_oracao", "doc_caderno", "doc_carta", "doc_pagina", "portao_templo_fechado",
                "portao_templo_aberto", "portal_templo_pilares", "portal_templo_lintel", "portao_barras",
                "casa_lareira", "casa_fogo_0", "casa_fogo_1", "casa_fogo_2", "casa_cama", "casa_mesa", "casa_banco", "casa_prateleira",
                "casa_arco", "casa_ervas", "casa_pele", "casa_tapete", "casa_lenha", "casa_barril", "casa_janela", "casa_mapa",
            })
                if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            Assert.IsEmpty(missing, "Rode Tools/EnvironmentSprites/gerar.ps1. Faltam: " + string.Join(", ", missing));
        }

        [Test]
        public void Sighting_NeverAfterDeath()
        {
            Assert.IsFalse(IsyathySighting.CanAppear(dead: true));
            Assert.IsTrue(IsyathySighting.CanAppear(dead: false));
        }

        [Test]
        public void Sighting_AppearsAtTheSides_NeverOverTheCat()
        {
            var cam = new Vector2(100f, 50f);
            foreach (int side in new[] { -1, 1 })
                for (float t = 0f; t <= 1f; t += 0.25f)
                {
                    var p = IsyathySighting.SpotFromCamera(cam, 15f, 8.4f, side, t);
                    float dx = (p.x - cam.x) * side;
                    Assert.GreaterOrEqual(dx, 15f * 0.35f - 1e-4f, "longe do meio (o gato)");
                    Assert.LessOrEqual(dx, 15f * 0.75f + 1e-4f, "dentro da tela");
                    Assert.GreaterOrEqual(p.y, cam.y, "do meio da tela para cima (as árvores do fundo)");
                }
        }

        [Test]
        public void Sighting_OnlyEyes_AppearOften()
        {
            // melhorias 14/11/2026: só os olhos, e bastante (intervalo curto entre as aparições)
            Assert.LessOrEqual(IsyathySighting.Gap.y, 5f, "os olhos aparecem bastante");
            Assert.Greater(IsyathySighting.Gap.x, 0.5f, "mas não colados um no outro");
            Assert.LessOrEqual(IsyathySighting.FirstDelay.y, 2f, "a primeira aparição vem logo");
        }

        [Test]
        public void LakeEyes_VanishOnlyWhenCrouchingOnTheGround()
        {
            Assert.IsTrue(IsyathySighting.LakeVanishes(crouching: true, grounded: true));
            Assert.IsFalse(IsyathySighting.LakeVanishes(crouching: true, grounded: false));
            Assert.IsFalse(IsyathySighting.LakeVanishes(crouching: false, grounded: true));
        }

        [Test]
        public void Sighting_Parallax_FollowsCameraByAFraction()
        {
            var b = new Vector2(10f, 5f);
            Assert.AreEqual(b, IsyathySighting.Parallax(b, b, 0.08f));                       // câmera em cima: no lugar
            var p = IsyathySighting.Parallax(b, new Vector2(20f, 5f), 0.08f);
            Assert.AreEqual(10.8f, p.x, 1e-4f);                                               // anda 8% do que a câmera andou
            Assert.AreEqual(5f, p.y, 1e-4f);
        }

        [Test]
        public void LoreBook_PutUpdatesInsteadOfDuplicating()
        {
            var book = ScriptableObject.CreateInstance<LoreBook>();
            try
            {
                book.Put("P-PRIM-01", "Placa da estrada", 2, 100f, null);
                book.Put("P-PRIM-01", "Placa da estrada (nova)", 2, 120f, null);
                Assert.AreEqual(1, book.entries.Count);
                Assert.AreEqual("Placa da estrada (nova)", book.Find("P-PRIM-01").title);
                Assert.AreEqual(120f, book.Find("P-PRIM-01").order);
                Assert.IsNull(book.Find("P-PRIM-99"));
                Assert.IsNull(book.Find(""));
            }
            finally { Object.DestroyImmediate(book); }
        }
    }
}
