using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace SeteVidas.Tests
{
    /// <summary>
    /// F4c, arte definitiva do bestiário da Floresta (Tools/EnemySprites/gerar_floresta.ps1): todos os quadros que os
    /// inimigos carregam existem. A variante infectada é refeita pelo menu 21 a partir destes.
    /// </summary>
    public class ForestBestiaryArtTests
    {
        const string Art = "Assets/_Game/Art/Environments/Forest/Placeholder";

        [Test]
        public void GeneratedArt_HasEveryAnimationFrame()
        {
            var names = new List<string>
            {
                "morcego_dormindo", "morcego_dormindo_1", "morcego_voo_a", "morcego_voo_b", "morcego_voo_c", "morcego_voo_d",
                "planta_cabeca", "planta_cabeca_1", "planta_cabeca_aberta", "planta_cabeca_aberta_1", "planta_cabeca_recuo",
                "planta_pescoco", "planta_base",
                "serpente_buraco", "serpente_olhos", "serpente_olhos_1", "serpente_olhos_2", "serpente_aviso", "serpente_aviso_1",
                "serpente_bote", "serpente", "serpente_1", "serpente_gomo",
            };
            foreach (var k in new[] { "galho_alto", "broto" })
                foreach (var s in new[] { "_disfarce", "_disfarce_1", "_disfarce_2", "", "_1", "_braco", "_braco_1", "_preso" })
                    names.Add(k + s);
            var missing = new List<string>();
            foreach (var n in names) if (!File.Exists($"{Art}/{n}.png")) missing.Add(n);
            Assert.IsEmpty(missing, "Rode Tools/EnemySprites/gerar_floresta.ps1. Faltam: " + string.Join(", ", missing));
        }
    }
}
