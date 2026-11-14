using System.IO;
using SeteVidas.Core;
using SeteVidas.Player;
using SeteVidas.World;
using SeteVidas.World.Traps;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// F1 da Floresta Silenciosa (Docs/Floresta/plano-implementacao.md): duas cenas de teste em blocos para provar a base
    /// técnica antes do graybox. Critério de pronto da F1: andar entre as duas cenas, quebrar um tronco, abrir um baú,
    /// sair e voltar com tudo lembrado, e viajar entre os dois santuários.
    /// <list type="bullet">
    /// <item>Forest_Test_A: santuário, tronco rachado com baú atrás, poço vertical com galhos de mão única, galho que
    /// balança, folha que afunda, cogumelo saltador, galho podre e um baú no topo; porta para a B.</item>
    /// <item>Forest_Test_B: santuário, outro tronco + baú, porta de volta para a A e uma porta interna (mesma cena).</item>
    /// </list>
    /// Arte provisória desenhada aqui (Art/Environments/Forest/Placeholder), na paleta da Floresta.
    /// </summary>
    public static class ForestTestSetup
    {
        public const string Folder = "Assets/_Game/Scenes/World/Forest";
        public const string SceneA = "Forest_Test_A";
        public const string SceneB = "Forest_Test_B";
        public const string SceneC = "Forest_Test_C";
        public const string SceneCabin = "Forest_Test_Cabin"; // interior (residência): a cabana da Vulpis
        const string ArtFolder = "Assets/_Game/Art/Environments/Forest/Placeholder";

        [MenuItem("Tools/Sete Vidas/20. Floresta F1: cenas de teste (A e B)", priority = 70)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SeteVidasSetup.ConfigureLayers(); // garante a layer Platform (galhos de mão única)
            PlayerSetup.CreatePlayerAssets();
            ProgressionSetup.CreateHudPrefab();
            ProgressionSetup.CreateSanctuaryPrefab();
            ClassSetup.CreateWallPrefab();
            EnemySetup.CreateSentinelPrefab(); // inimigos da arena de teste (provisórios: os da Floresta vêm na F4)
            Directory.CreateDirectory(Folder);
            EnsureSprites();

            BuildSceneA();
            BuildSceneB();
            BuildSceneC();
            BuildCabin();
            SeteVidasSetup.RegisterScenesInBuild();
            EditorSceneManager.OpenScene($"{Folder}/{SceneA}.unity");
            AssetDatabase.SaveAssets(); // grava a lista de cenas do Build Settings (as portas carregam as cenas por ela)
            Debug.Log("[Sete Vidas] Floresta F1: cenas Forest_Test_A, Forest_Test_B e Forest_Test_C geradas. Aperte Play na A (a C, a Encruzilhada, fica depois do patamar alto da B). " +
                      "Roteiro de teste: Docs/QA/floresta-f1-plano-de-teste.md");
        }

        // ------------------------------------------------------------ cena A

        static void BuildSceneA()
        {
            var ctx = Begin(SceneA, new Vector2(3f, 0.1f), new Color32(0x16, 0x1a, 0x38, 0xff));
            var g = ctx.Ground;

            // chão e paredes (o chão fica em y 0: tiles de y -3 a -1)
            Fill(g, -8, -3, 64, -1);
            Fill(g, -8, 0, -7, 42);            // parede da esquerda
            // parede da direita: a porta para a B saiu do chão e foi para o alto (corredor em y 18..21, rev. do usuário)
            Fill(g, 63, 0, 64, 17);
            Fill(g, 63, 22, 64, 42);
            Fill(g, -8, 40, 64, 42);           // teto

            // santuário
            Sanctuary(ctx.Root, "S-T1", "Clareira de teste", 1f);

            // tronco rachado (x 10) fechando uma alcova com o baú W01
            Fill(g, 9, 4, 17, 4);              // teto da alcova (topo em 5: dá para agarrar a borda e seguir por cima)
            Fill(g, 5, 0, 6, 1);               // degrau antes do tronco (topo em 2)
            Fill(g, 17, 0, 17, 3);             // fundo da alcova
            Trunk(ctx.Root, "Tronco rachado (A1)", 10.5f, 0f, "teste_A1");
            Chest(ctx.Root, "Baú W01", 14f, 0f, "TESTE_W01", wood: true, coins: 0);

            // poço vertical (x 22..36): galhos de mão única a cada 3 tiles, alternando os lados
            Fill(g, 21, 4, 21, 38);            // parede esquerda do poço (passagem pelo chão, y 0..3)
            // parede direita fechada embaixo; só se passa para o lado dos cactos pelo alto (aberta em y 35..38)
            Fill(g, 37, 0, 37, 34);
            Mushroom(ctx.Root, "Cogumelo saltador", 24f, 0f);
            Cactus(ctx.Root, "Cacto (poço, meio)", 23.5f, 14f, new Vector2(0f, 1f), 3f);
            for (int i = 0; i < 6; i++)
            {
                float y = 3f + i * 3f;
                float x = i % 2 == 0 ? 25f : 30f;
                Branch(ctx.Root, $"Galho de mão única {i + 1}", x, y, 5f);
            }
            Leaf(ctx.Root, "Folha larga", 28.5f, 21f);
            Swaying(ctx.Root, "Galho que balança", 32f, 24f, 4f);
            RottenBranch(ctx.Root, "Galho podre", 26f, 27f);
            // no lugar do galho 7: sobe e desce (27..29), baixo o bastante para pular nele do galho podre (y 27)
            Cactus(ctx.Root, "Cacto (poço, topo)", 31.5f, 29.3f, new Vector2(0f, 1f), 3f);
            Fill(g, 22, 32, 28, 32);           // patamar do topo (em pé em y 33), com o baú W02
            Chest(ctx.Root, "Baú W02 (topo do poço)", 24f, 33f, "TESTE_W02", wood: true, coins: 5);
            Fill(g, 34, 34, 42, 34);           // borda que atravessa a parede para o lado dos cactos (em pé em y 35)

            // lado dos cactos (x 38..62): chão de espinhos, cactos para quicar e plataformas até a porta no alto
            TrapsSetup.Spikes(ctx.Root, 50f, 24);
            // Coluna em zigue-zague, ~6 tiles entre um e outro (o quique sobe 7,2): dos espinhos do chão dá para subir
            // tudo de volta até a borda (y 35). Descendo da borda, o de cima leva à plataforma alta; os de 12 e 18, ao corredor.
            Cactus(ctx.Root, "Cacto 1 (y 30, leva à plataforma)", 45.5f, 30f, new Vector2(1f, 0f), 3f);
            Cactus(ctx.Root, "Cacto 2 (y 24)", 40.2f, 24f, new Vector2(0f, 0.5f), 2.5f);
            Cactus(ctx.Root, "Cacto 3 (y 15)", 51.1f, 15f, Vector2.zero, 3f);
            Cactus(ctx.Root, "Cacto 4 (y 11)", 45.5f, 10.8f, new Vector2(0.75f, 0f), 3f);
            Cactus(ctx.Root, "Cacto 5 (y 5,5, alcança dos espinhos)", 49.3f, 5.5f, new Vector2(0f, 0.3f), 2.5f); // quique nos espinhos: até ~7,7
            // plataforma alta (em pé em y 26) e um galho até o corredor: o caminho para baixo sempre aparece na tela
            Fill(g, 52, 25, 58, 25);
            Branch(ctx.Root, "Galho entre os cactos", 44.3f, 22f, 4f);
            // cogumelos (posições do usuário, 13/11/2026): o caminho de volta para cima
            Mushroom(ctx.Root, "Cogumelo da plataforma alta", 52.7f, 26f);
            Mushroom(ctx.Root, "Cogumelo do corredor", 60f, 18f);
            // e no chão de espinhos: espinhos e cogumelos juntos (cair no cogumelo salva)
            Mushroom(ctx.Root, "Cogumelo nos espinhos 1", 41f, 0f);
            Mushroom(ctx.Root, "Cogumelo nos espinhos 2", 55.5f, 0f);
            Mushroom(ctx.Root, "Cogumelo nos espinhos 3", 61.7f, 0f);
            Fill(g, 54, 17, 62, 17);           // chão do corredor da porta (em pé em y 18)
            TrapsSetup.Sign(ctx.Root, 62.1f, 18.2f, "Saída →");

            // salas do mapa (aba Mapa do inventário)
            Room(ctx.Root, "A1", "Clareira", new Vector2(7f, 20f), new Vector2(28f, 40f), Vector2.zero);
            Room(ctx.Root, "A2", "Poço", new Vector2(29.5f, 20f), new Vector2(17f, 40f), Vector2.zero);
            Room(ctx.Root, "A3", "Cactos", new Vector2(50.5f, 20f), new Vector2(25f, 40f), Vector2.zero);

            // porta para a B (no alto, à direita) e a chegada de volta
            Exit(ctx.Root, "Porta para a B", new Vector2(63.5f, 20f), new Vector2(1f, 4f), SceneB, "B_da_A");
            Entrance(ctx.Root, "Chegada (da B)", new Vector2(56.5f, 18.05f), "A_da_B", -1); // antes do cogumelo do corredor

            End(ctx);
        }

        // ------------------------------------------------------------ cena B

        static void BuildSceneB()
        {
            var ctx = Begin(SceneB, new Vector2(3f, 0.1f), new Color32(0x10, 0x14, 0x2c, 0xff));
            var g = ctx.Ground;

            Fill(g, -8, -3, 70, -1);
            Fill(g, -8, 4, -7, 30);            // parede da esquerda (porta para a A embaixo, y 0..3)
            Fill(g, 69, 0, 70, 14);            // parede da direita: abertura para a C no patamar (y 15..18)
            Fill(g, 69, 19, 70, 30);
            Fill(g, -8, 28, 70, 30);

            Entrance(ctx.Root, "Chegada (da A)", new Vector2(2f, 0.05f), "B_da_A", 1);
            Exit(ctx.Root, "Porta para a A", new Vector2(-7.5f, 2f), new Vector2(1f, 4f), SceneA, "A_da_B");
            Sanctuary(ctx.Root, "S-T2", "Vale de teste", 8f);

            // --- redesenho do usuário (13/11/2026): a alcova do tronco saiu; o baú foi para o abrigo no alto à esquerda

            // abrigo no alto à esquerda: galho grosso (em pé em y 18), telhado e o tronco rachado fechando o baú
            Fill(g, -6, 17, 13, 17);
            Fill(g, -6, 22, 1, 22);            // telhado do abrigo, encostado no topo do tronco (sem vão para pular por cima)
            Trunk(ctx.Root, "Tronco rachado (abrigo)", 1.5f, 18f, "teste_B_abrigo");
            Chest(ctx.Root, "Baú W03 (abrigo)", -3f, 18f, "TESTE_W03", wood: true, coins: 0);
            DryBranchEnemy(ctx.Root, "Galho Seco (abrigo 1)", 4.5f, 18f, 1);
            DryBranchEnemy(ctx.Root, "Galho Seco (abrigo 2)", 10.5f, 18f, 1);

            // pogos (cactos) do chão até o abrigo e até o patamar
            // Duas correntes de pogo medidas (PlatformRules.PogoStepOk): cada cacto no máximo ~6,9 acima do anterior e
            // até 4,5 para o lado; o primeiro a no máximo 3 do chão (o pulo chega a 4).
            // Corrente 1, até o abrigo: chão -> B1 -> B2 -> B3 -> galho grosso do abrigo (em pé em y 18, x <= 13)
            PogoChain(ctx.Root, "Corrente do abrigo", 0f, 18f,
                ("Cacto B1 (do chão)", 17.5f, 2.8f), ("Cacto B2", 21.5f, 8.3f), ("Cacto B3 (leva ao abrigo)", 18f, 14.8f)); // B2 mais baixo (pedido do usuário)
            // Corrente 2, até o patamar: chão -> B4 -> galho (y 6) -> galho (y 9) -> B5 -> patamar (em pé em y 15, x >= 44)
            PogoChain(ctx.Root, "Corrente do patamar", 0f, 6f, ("Cacto B4 (leva aos galhos)", 26f, 2.8f));
            PogoChain(ctx.Root, "Corrente do patamar (fim)", 9f, 15f, ("Cacto B5 (leva ao patamar)", 40.5f, 11.5f)); // posição do usuário
            Branch(ctx.Root, "Galho de mão única (descer)", 32.8f, 9f, 5f);     // 3 acima do outro e 2 tiles por cima dele: sobe pulando reto
            Branch(ctx.Root, "Galho de mão única (descer 2)", 29.8f, 6f, 5f);

            // plantas carnívoras: uma no chão e uma presa na parede da direita, virada para a esquerda
            Plant(ctx.Root, "Planta carnívora (chão)", 48f, 0f, 0f);
            Plant(ctx.Root, "Planta carnívora (parede)", 68.9f, 21f, 90f);

            // canto de baixo à direita: degraus até uma plataforma com moedas
            Branch(ctx.Root, "Degrau 1", 57.5f, 3f, 2f);
            Fill(g, 60, 5, 68, 5);             // plataforma das moedas (em pé em y 6)
            FortressSetup.Coin(ctx.Root, "Moeda B1", new Vector2(61.7f, 6.6f), "teste_b_moeda1", 1);
            FortressSetup.Coin(ctx.Root, "Moeda B2", new Vector2(65.8f, 6.6f), "teste_b_moeda2", 1);
            FortressSetup.Coin(ctx.Root, "Moeda B3", new Vector2(62.6f, 1f), "teste_b_moeda3", 1);
            FortressSetup.Coin(ctx.Root, "Moeda B4", new Vector2(64.3f, 1f), "teste_b_moeda4", 1);
            FortressSetup.Coin(ctx.Root, "Moeda B5", new Vector2(66.6f, 1f), "teste_b_moeda5", 1);

            // salas do mapa: a B fica à direita da A, na altura do corredor da porta (y 18 da A)
            Room(ctx.Root, "B1", "Vale", new Vector2(11.5f, 14f), new Vector2(37f, 28f), MapOffsetB);
            Room(ctx.Root, "B2", "Patamar", new Vector2(49.5f, 14f), new Vector2(39f, 28f), MapOffsetB);

            // patamar alto com dois Galhos Secos; porta para a C no fim; atalho do chão para o patamar (mesma cena)
            Fill(g, 44, 14, 68, 14);
            DryBranchEnemy(ctx.Root, "Galho Seco (patamar 1)", 49f, 15f, -1);
            DryBranchEnemy(ctx.Root, "Broto (patamar 2)", 58f, 15f, -1, sprout: true);   // solta morcegos
            Exit(ctx.Root, "Porta para a C", new Vector2(69.5f, 17f), new Vector2(1f, 4f), SceneC, "C_da_B");
            Entrance(ctx.Root, "Chegada (da C)", new Vector2(66.5f, 15.05f), "B_da_C", -1);
            Exit(ctx.Root, "Porta interna (sobe)", new Vector2(68f, 2f), new Vector2(1f, 4f), "", "B_patamar");
            Entrance(ctx.Root, "Chegada (patamar)", new Vector2(64f, 15.05f), "B_patamar", -1);

            End(ctx);
        }

        // ------------------------------------------------------------ cena C (sala-hub)

        /// <summary>
        /// Sala-hub (redesenho do usuário, 13/11/2026). Do hub, três caminhos:
        /// <list type="bullet">
        /// <item>embaixo, para a direita: o santuário, perto da planta carnívora presa na parede;</item>
        /// <item>no meio, à direita: a arena (fecha ao entrar; as plantas carnívoras são os inimigos; baú W04 depois);</item>
        /// <item>para cima, escalando as vinhas: a cabana da Vulpis (residência trancada: entra numa cena pequena), o alto
        /// da arena com dois Galhos Secos, a chave no canto de cima à direita e a saída por cima.</item>
        /// </list>
        /// </summary>
        static void BuildSceneC()
        {
            var ctx = Begin(SceneC, new Vector2(2f, 0.1f), new Color32(0x12, 0x16, 0x30, 0xff));
            var g = ctx.Ground;
            var vines = ctx.Vines;

            // limites (chão em y 0); teto com o buraco da saída (x 55..59)
            Fill(g, -8, -3, 80, -1);
            Fill(g, -8, 4, -7, 44);            // parede da esquerda (porta para a B embaixo, y 0..3)
            Fill(g, 79, 0, 80, 44);
            Fill(g, -8, 42, 54, 44);
            Fill(g, 60, 42, 80, 44);

            Entrance(ctx.Root, "Chegada (da B)", new Vector2(2f, 0.05f), "C_da_B", 1);
            Exit(ctx.Root, "Porta para a B", new Vector2(-7.5f, 2f), new Vector2(1f, 4f), SceneB, "B_da_C");

            // parede entre o hub e a direita (x 21): passagem embaixo (y 0..4) e para a arena (y 13..16)
            Fill(g, 21, 5, 21, 11);
            Fill(g, 21, 17, 21, 27);

            // --- subida do hub: pilar e parede com vinhas (chaminé de 3 de largura entre x 16 e x 20)
            Fill(vines, 16, 3, 16, 28);         // pilar de vinhas até a altura do galho grosso (sem vão entre o pilar e o galho)
            Fill(vines, 20, 5, 20, 11);         // vinhas na parede, abaixo da entrada da arena
            Fill(vines, 20, 17, 20, 27);        // e acima dela
            Branch(ctx.Root, "Galho da entrada da arena", 17f, 12f, 4f); // de mão única: a chaminé continua por ele
            // --- lado esquerdo do hub (redesenho do usuário, 13/11/2026): três plataformas com ganchos na ponta
            // (classe Arqueiro, no futuro) e morcegos pendurados embaixo. Hoje: só a do meio, pelo pilar de vinhas; a de baixo e
            // a de cima (com o baú W06) só com o Arqueiro.
            Fill(g, -6, 11, 2, 11);            // plataforma de baixo (em pé em y 12)
            Hook(ctx.Root, "Gancho da plataforma de baixo (Arqueiro)", 3f, 11.4f);
            Bat(ctx.Root, "Morcego (plataforma de baixo 1)", -2.2f, 10.4f, wakeRange: 12f); // veem quem entra no hub
            Bat(ctx.Root, "Morcego (plataforma de baixo 2)", 0.7f, 10.4f, wakeRange: 12f);
            Fill(g, 9, 17, 15, 17);            // plataforma do meio (em pé em y 18), encostada no pilar de vinhas
            Hook(ctx.Root, "Gancho da plataforma do meio (Arqueiro)", 8.2f, 16.8f);
            Bat(ctx.Root, "Morcego (plataforma do meio)", 12.4f, 16.4f);
            Fill(g, -6, 23, 3, 23);            // plataforma de cima (em pé em y 24): 6 acima da do meio, sem alcance hoje
            Hook(ctx.Root, "Gancho da plataforma de cima (Arqueiro)", 4f, 23.2f);
            Bat(ctx.Root, "Morcego (plataforma de cima 1)", -5.3f, 22.4f);
            Bat(ctx.Root, "Morcego (plataforma de cima 2)", -1f, 22.4f);
            Chest(ctx.Root, "Baú W06 (só com o Arqueiro)", -2.5f, 24f, "TESTE_W06", wood: true, coins: 5);

            // morcegos pendurados embaixo do galho grosso da cabana
            Bat(ctx.Root, "Morcego do hub 1", -1f, 27.4f);
            Bat(ctx.Root, "Morcego do hub 2", 5f, 27.4f);
            Bat(ctx.Root, "Morcego do hub 3", 10.5f, 27.4f);

            // --- a cabana da Vulpis (residência): a casa desenhada em volta da porta (fundo, sem colisão) e o telhado
            Fill(g, -6, 28, 15, 28);           // galho grosso (em pé em y 29), encostado no pilar de vinhas (sem vão)
            Fill(ctx.Back, -3, 29, 11, 35);    // fachada da casa (atrás do gato)
            Fill(g, -4, 36, 12, 36);           // telhado
            Residence(ctx.Root, "Cabana da Vulpis (porta)", 6f, 29f, SceneCabin, "entrada", "entrar", "cabana_vulpis", "cabana");
            Entrance(ctx.Root, "Chegada (da cabana)", new Vector2(8f, 29.05f), "cabana_vulpis", 1);
            TrapsSetup.Sign(ctx.Root, 3f, 33.6f, "Cabana da Vulpis");

            // --- embaixo, à direita da passagem do hub (redesenho do usuário): mais vida e armadilhas, terreno orgânico.
            // O botão azul do outro lado desliga as armadilhas para sempre (para a volta).
            Plant(ctx.Root, "Planta da entrada de baixo", 23f, 0f, 0f);
            Fill(g, 28, 5, 30, 11);            // massa de terra pendurada da laje da arena
            Fill(g, 31, 9, 41, 11);            // e outra, mais rasa
            Blade(ctx.Root, "Lâmina pendular 1", 29.5f, 5f, 3.5f, 0f);
            Blade(ctx.Root, "Lâmina pendular 2", 39.5f, 9f, 4.6f, 0.5f);
            Fill(g, 36, 0, 42, 2);             // degrau de terra (em pé em y 3)
            Spikes(ctx.Root, 47f, 7);           // fosso de espinhos (x 43,5..50,5): atravessa quicando neles
            Fill(g, 51, 0, 52, 3);             // degrau (em pé em y 4)
            Fill(g, 53, 0, 58, 6);             // morro (em pé em y 7)
            Fill(g, 59, 0, 60, 3);             // degrau do lado direito do morro (sem ele, quem desce não volta)
            Saw(ctx.Root, "Serra do morro", 53.5f, 57.5f, 7.55f);   // corre em cima do morro (era o "gancho": pedido do usuário)
            Saw(ctx.Root, "Serra do fosso (sobe e desce)", 1.2f, 10.2f, 47f, vertical: true); // no meio dos espinhos: quique na hora certa
            Sanctuary(ctx.Root, "S-T3", "Encruzilhada de teste", 63.5f);   // longe do degrau do morro (a cesta encostava)
            Switch(ctx.Root, "Botão das armadilhas", 67.7f, 0f, TrapGroupC);
            Fill(g, 66, 6, 78, 11);            // teto baixo no canto da direita
            Plant(ctx.Root, "Planta pendurada do teto", 71.5f, 6f, 180f);
            Plant(ctx.Root, "Planta carnívora (parede de baixo)", 78.9f, 1.8f, 90f); // na altura de quem anda no chão
            KeyItem(ctx.Root, "Chave da cabana", 74.4f, 0.6f, "cabana");             // a chave veio para cá
            FortressSetup.Coin(ctx.Root, "Moeda C1", new Vector2(71f, 1.4f), "teste_c_moeda1", 1);

            // --- a arena (chão em y 13, x 22..61): as plantas carnívoras são os inimigos
            Fill(g, 21, 12, 78, 12);           // chão da arena e da sala depois dela (teto da parte de baixo)
            Fill(g, 22, 24, 78, 24);           // teto da arena (chão do alto)
            Fill(g, 62, 17, 62, 23);           // parede da saída da arena (portão embaixo, y 13..16)
            var gateIn = Gate(ctx.Root, "Portão da entrada", 22.5f, 13f);
            var gateOut = Gate(ctx.Root, "Portão da saída", 62.5f, 13f);
            Plant(ctx.Root, "Planta da arena 1", 34.6f, 13f, 0f);
            Plant(ctx.Root, "Planta da arena 2", 45.8f, 13f, 0f);
            Plant(ctx.Root, "Planta da arena 3", 54.6f, 13f, 0f);
            var arenaGo = new GameObject("Arena (C2)");
            arenaGo.transform.SetParent(ctx.Root, false);
            arenaGo.transform.position = new Vector3(42f, 18.5f, 0f);
            var arena = arenaGo.AddComponent<ArenaRoom>();
            arena.Configure("teste_C_arena", new Vector2(40f, 11f), new[] { gateIn, gateOut }, null);
            // segunda leva (pedido do usuário): mortas as plantas, saem morcegos e um Broto (que solta mais morcegos)
            var wave2 = new[]
            {
                DryBranchEnemy(ctx.Root, "Broto da arena (leva 2)", 40f, 13f, -1, sprout: true).gameObject,
                Bat(ctx.Root, "Morcego da arena 1 (leva 2)", 28f, 23.4f, wakeRange: 40f).gameObject,
                Bat(ctx.Root, "Morcego da arena 2 (leva 2)", 50f, 23.4f, wakeRange: 40f).gameObject,
                Bat(ctx.Root, "Morcego da arena 3 (leva 2)", 58f, 23.4f, wakeRange: 40f).gameObject,
            };
            arena.SetWaves(wave2);
            Chest(ctx.Root, "Baú W04 (depois da arena)", 72f, 13f, "TESTE_W04", wood: true, coins: 10);

            // --- o alto (em cima da arena, chão em y 25): Galhos Secos, plataformas, a chave e a saída por cima
            DryBranchEnemy(ctx.Root, "Galho Seco (alto 1)", 29.6f, 25f, -1);
            DryBranchEnemy(ctx.Root, "Broto (alto 2)", 48f, 25f, -1, sprout: true);       // solta morcegos
            Fill(g, 35, 28, 41, 28);           // plataforma (em pé em y 29)
            Fill(g, 45, 32, 60, 32);           // plataforma da saída (em pé em y 33)
            Fill(vines, 60, 33, 60, 41);        // parede de vinhas até o buraco do teto
            Fill(g, 74, 27, 78, 27);           // cantinho da chave (em pé em y 28)
            Chest(ctx.Root, "Baú W07 (canto de cima, onde era a chave)", 76.5f, 28f, "TESTE_W07", wood: true, coins: 0);
            FortressSetup.Coin(ctx.Root, "Moeda C2", new Vector2(70f, 26.2f), "teste_c_moeda2", 1);
            FortressSetup.Coin(ctx.Root, "Moeda C3", new Vector2(71f, 27.4f), "teste_c_moeda3", 1);
            FortressSetup.Coin(ctx.Root, "Moeda C4", new Vector2(72f, 28.6f), "teste_c_moeda4", 1);
            Exit(ctx.Root, "Saída por cima", new Vector2(57f, 43f), new Vector2(5f, 1f), SceneA, "A_da_B"); // provisório: volta para a A
            TrapsSetup.Sign(ctx.Root, 57f, 34.2f, "saída ↑");

            // salas do mapa: a C fica à direita da B, na altura do patamar (y 15 da B)
            Room(ctx.Root, "C0", "Encruzilhada", new Vector2(7f, 14f), new Vector2(28f, 28f), MapOffsetC);
            Room(ctx.Root, "C1", "Santuário", new Vector2(50f, 6f), new Vector2(57f, 12f), MapOffsetC);
            Room(ctx.Root, "C2", "Arena", new Vector2(42f, 18.5f), new Vector2(40f, 11f), MapOffsetC);
            Room(ctx.Root, "C3", "Depois da arena", new Vector2(70.5f, 18.5f), new Vector2(17f, 11f), MapOffsetC);
            Room(ctx.Root, "C4", "Cabana", new Vector2(4f, 35f), new Vector2(20f, 14f), MapOffsetC);
            Room(ctx.Root, "C5", "O alto", new Vector2(50f, 33.5f), new Vector2(58f, 17f), MapOffsetC);

            End(ctx);
        }

        // ------------------------------------------------------------ interior: a cabana da Vulpis

        /// <summary>Residência (pedido do usuário): cena pequena, entra e sai pela mesma porta.</summary>
        static void BuildCabin() => BuildCabin(SceneCabin, SceneC, "TESTE_W05", "teste_cabana_moeda");

        /// <summary>A cabana em qualquer cena (a da fase: Forest_D_Cabin, que volta para o Refúgio).</summary>
        internal static void BuildCabin(string scene, string outsideScene, string chestId, string coinId)
        {
            var ctx = Begin(scene, new Vector2(2f, 0.1f), new Color32(0x2a, 0x1c, 0x14, 0xff));
            var g = ctx.Ground;
            Fill(g, -2, -3, 20, -1);           // chão de tábuas
            Fill(g, -2, 0, -1, 10);            // paredes
            Fill(g, 19, 0, 20, 10);
            Fill(g, -2, 9, 20, 10);            // teto (a sala tem 8 de altura)
            Entrance(ctx.Root, "Entrada", new Vector2(1.5f, 0.05f), "entrada", 1);
            Residence(ctx.Root, "Porta (sair)", 0.5f, 0f, outsideScene, "cabana_vulpis", "sair", "", "");
            Chest(ctx.Root, "Baú " + chestId + " (cabana)", 12f, 0f, chestId, wood: true, coins: 0);
            TrapsSetup.Sign(ctx.Root, 8f, 2.2f, "Cabana da Vulpis");
            // o Lion comenta ao entrar (uma vez): a cabana está vazia e é de um caçador (pedido do usuário)
            var bark = new GameObject("Fala ao entrar");
            bark.transform.SetParent(ctx.Root, false);
            bark.AddComponent<SceneBark>().Configure("cabana_vulpis_vazia", 0.8f,
                "Vazia.",
                "Arco na parede, flechas no chão, peles secando...",
                "Algum caçador mora aqui. E não está em casa.");
            Branch(ctx.Root, "Prateleira", 14f, 3f, 4f);          // 3 do chão (degrau confortável)
            FortressSetup.Coin(ctx.Root, "Moeda da prateleira", new Vector2(16f, 3.6f), coinId, 5);
            End(ctx);
        }

        static readonly Vector2 MapOffsetC = new Vector2(151f, 33f);

        static void Guard(Transform parent, string name, Vector2 at, int facing, float patrolA, float patrolB)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemySetup.SentinelPrefabPath);
            if (prefab == null) prefab = EnemySetup.CreateSentinelPrefab();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = at;
            var ai = go.GetComponent<SeteVidas.Enemies.EnemyAI>();
            var so = new SerializedObject(ai);
            var points = so.FindProperty("patrolPoints");
            points.arraySize = 2;
            points.GetArrayElementAtIndex(0).floatValue = patrolA;
            points.GetArrayElementAtIndex(1).floatValue = patrolB;
            so.FindProperty("startFacing").intValue = facing;
            so.FindProperty("respawnDelay").floatValue = 0f; // só voltam ao morrer ou descansar
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Portão de barras (1 × 4 tiles) com os pés em <paramref name="y"/>; começa aberto.</summary>
        internal static ArenaGate Gate(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);
            var bars = Visual(go.transform, "portao_barras", new Vector2(1f, 4f), new Vector3(0f, 2f, 0f), 4);
            var gate = go.AddComponent<ArenaGate>();
            var so = new SerializedObject(gate);
            so.FindProperty("bars").objectReferenceValue = bars.transform;
            so.FindProperty("startsClosed").boolValue = false;
            so.FindProperty("openHeight").floatValue = 4.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return gate;
        }

        internal static void KeyItem(Transform parent, string name, float x, float y, string keyId)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("chave");
            sr.sortingOrder = 5;
            var key = go.AddComponent<KeyPickup>();
            key.Configure(keyId);
            var so = new SerializedObject(key);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Porta trancada (1 × 4 tiles) com os pés em <paramref name="y"/>.</summary>
        static void Door(Transform parent, string name, float x, float y, string doorId, string keyId)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 4f);
            box.offset = new Vector2(0f, 2f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 2f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("porta_fechada");
            sr.sortingOrder = 2;
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(-1.2f, 4.6f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: abrir", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);
            var door = go.AddComponent<LockedDoor>();
            door.Configure(doorId, keyId);
            var so = new SerializedObject(door);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("closedSprite").objectReferenceValue = LoadSprite("porta_fechada");
            so.FindProperty("openSprite").objectReferenceValue = LoadSprite("porta_aberta");
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ montagem da cena

        internal struct Ctx { public Scene Scene; public Transform Root; public Tilemap Ground; public Tilemap Vines; public Tilemap Back; public Tile Tile; }

        internal static Ctx Begin(string name, Vector2 spawn, Color32 sky)
        {
            string path = $"{Folder}/{name}.unity";
            var scene = SeteVidasSetup.NewSceneFromTemplate(path);
            var cam = SeteVidasSetup.FindCamera(scene);
            SeteVidasSetup.SetupPixelPerfectCamera(cam, spawn + Vector2.up * 4f);
            cam.backgroundColor = sky;
            var tiles = SeteVidasSetup.CreatePlaceholderTiles();

            var grid = SeteVidasSetup.CreateGrid();
            SceneManager.MoveGameObjectToScene(grid.gameObject, scene);
            var ground = SeteVidasSetup.CreateTilemap(grid, "Ground", GameConstants.Layers.Ground, 0, true);
            // vinhas: paredes escaláveis (layer Climbable, como a hera da Fortaleza)
            var vines = SeteVidasSetup.CreateTilemap(grid, "Climbable", GameConstants.Layers.Climbable, 1, true);
            s_vineTile = tiles.Climbable;
            // fundo sem colisão (fachadas de casas, paredes ao longe)
            var back = SeteVidasSetup.CreateTilemap(grid, "Background", null, -10, false);
            s_backTile = tiles.Background;

            var root = new GameObject("Floresta (teste)");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ForestTestBootstrap>(); // save de quem saiu da Fortaleza (Paladino, 2 corações)
            var spawnGo = new GameObject("PlayerSpawn");
            SceneManager.MoveGameObjectToScene(spawnGo, scene);
            spawnGo.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.HudPrefabPath), scene);
            hud.name = "HUD";
            return new Ctx { Scene = scene, Root = root.transform, Ground = ground, Vines = vines, Back = back, Tile = tiles.Ground };
        }

        internal static void End(Ctx ctx)
        {
            SeteVidasSetup.BuildTileColliders(ctx.Scene);
            PlayerSetup.PlaceInScene(ctx.Scene); // gato + câmera no PlayerSpawn; salva a cena
            AddForestPlayerParts(ctx.Scene);
            EditorSceneManager.MarkSceneDirty(ctx.Scene);
            EditorSceneManager.SaveScene(ctx.Scene);
        }

        /// <summary>Peças do gato que só a Floresta usa: alvo da câmera que olha para baixo e as falas curtas.</summary>
        internal static void AddForestPlayerParts(Scene scene)
        {
            GameObject player = null;
            CinemachineCamera cm = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (player == null && root.GetComponent<PlayerController>() != null) player = root;
                if (cm == null) cm = root.GetComponentInChildren<CinemachineCamera>();
            }
            if (player == null) { Debug.LogWarning("[Sete Vidas] Gato não encontrado na cena da Floresta"); return; }
            var look = player.GetComponent<CameraLook>() ?? player.AddComponent<CameraLook>();
            if (player.GetComponent<LionBarks>() == null) player.AddComponent<LionBarks>();
            if (cm != null)
            {
                cm.Target.TrackingTarget = look.Target;
                // salas verticais: a câmera acompanha a altura mais de perto
                var composer = cm.GetComponent<CinemachinePositionComposer>();
                if (composer != null)
                {
                    composer.Composition.DeadZone.Size = new Vector2(0.1f, 0.12f);
                    composer.Damping = new Vector3(0.3f, 0.35f, 0f);
                }
            }
        }

        static Tile s_tile, s_vineTile, s_backTile;

        /// <summary>Pinta tiles: no tilemap "Climbable" vai a vinha (escalável); nos outros, o chão provisório.</summary>
        internal static void Fill(Tilemap map, int x0, int y0, int x1, int y1)
        {
            if (s_tile == null || s_vineTile == null || s_backTile == null)
            {
                var t = SeteVidasSetup.CreatePlaceholderTiles();
                if (s_tile == null) s_tile = t.Ground;
                if (s_vineTile == null) s_vineTile = t.Climbable;
                if (s_backTile == null) s_backTile = t.Background;
            }
            string n = map.gameObject.name;
            SeteVidasSetup.Fill(map, n == "Climbable" ? s_vineTile : n == "Background" ? s_backTile : s_tile, x0, y0, x1, y1);
        }

        // ------------------------------------------------------------ inimigos da Floresta e residências

        static SpriteRenderer EnemyRoot(Transform parent, string name, float x, float y, Vector2 box, Vector2 boxOffset, string sprite, out GameObject go)
        {
            go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;            // os golpes do gato acham o inimigo pelo gatilho (layer Enemy)
            col.size = box;
            col.offset = boxOffset;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.sortingOrder = 4;
            return sr;
        }

        /// <summary>
        /// Galho Seco parado e alto (5,5 tiles: não dá para passar por cima) com os pés em <paramref name="y"/>.
        /// <paramref name="sprout"/> = Broto: solta morcegos das folhas (leva um morcego modelo desligado).
        /// </summary>
        internal static SeteVidas.Enemies.DryBranch DryBranchEnemy(Transform parent, string name, float x, float y, int facing, bool sprout = false)
        {
            float h = SeteVidas.Enemies.ForestEnemyRules.BranchHeight;
            string kind = sprout ? "broto" : "galho_alto";
            var sr = EnemyRoot(parent, name, x, y, new Vector2(1.2f, h), new Vector2(0f, h * 0.5f), kind + "_disfarce", out var go);
            sr.transform.localPosition = new Vector3(0f, h * 0.5f, 0f); // sprite 24×88 com o pivô no meio
            var e = go.AddComponent<SeteVidas.Enemies.DryBranch>();
            e.Configure(h, facing, disguise: true, sprout: sprout);
            var so = new SerializedObject(e);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("standing").objectReferenceValue = LoadSprite(kind);
            so.FindProperty("armUp").objectReferenceValue = LoadSprite(kind + "_braco");
            so.FindProperty("armDown").objectReferenceValue = LoadSprite(kind + "_preso");
            so.FindProperty("disguised").objectReferenceValue = LoadSprite(kind + "_disfarce");
            if (sprout)
            {
                var template = Bat(parent, name + " (morcego modelo)", x, y + h, sleeping: false);
                template.gameObject.SetActive(false);
                so.FindProperty("batTemplate").objectReferenceValue = template;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return e;
        }

        /// <summary>Morcego Noturno pendurado em (x, y) (o poleiro). Acorda e persegue.</summary>
        internal static SeteVidas.Enemies.NightBat Bat(Transform parent, string name, float x, float y, bool sleeping = true, float wakeRange = SeteVidas.Enemies.ForestEnemyRules.BatWakeRange)
        {
            var sr = EnemyRoot(parent, name, x, y, new Vector2(0.9f, 0.8f), Vector2.zero, "morcego_dormindo", out var go);
            var e = go.AddComponent<SeteVidas.Enemies.NightBat>();
            e.Configure(wakeRange);
            var so = new SerializedObject(e);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("sleeping").objectReferenceValue = LoadSprite("morcego_dormindo");
            so.FindProperty("flyA").objectReferenceValue = LoadSprite("morcego_voo_a");
            so.FindProperty("flyB").objectReferenceValue = LoadSprite("morcego_voo_b");
            so.ApplyModifiedPropertiesWithoutUndo();
            return e;
        }

        /// <summary>
        /// Planta Carnívora com a base em (x, y). <paramref name="angle"/>: 0 = no chão; 90 = na parede, virada para a
        /// esquerda; -90 = para a direita. A cabeça é um filho com o colisor; o pescoço estica até 6 tiles.
        /// </summary>
        internal static void Plant(Transform parent, string name, float x, float y, float angle)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var baseSr = new GameObject("Base").AddComponent<SpriteRenderer>();
            baseSr.transform.SetParent(go.transform, false);
            baseSr.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            baseSr.sprite = LoadSprite("planta_base");
            baseSr.sortingOrder = 3;

            var head = new GameObject("Cabeça") { layer = LayerMask.NameToLayer(GameConstants.Layers.Enemy) };
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            var col = head.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.2f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(head.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("planta_cabeca");
            sr.sortingOrder = 4;

            var e = go.AddComponent<SeteVidas.Enemies.CarnivorousPlant>();
            e.Configure(SeteVidas.Enemies.ForestEnemyRules.PlantReach);
            var so = new SerializedObject(e);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("head").objectReferenceValue = head.transform;
            so.FindProperty("closed").objectReferenceValue = LoadSprite("planta_cabeca");
            so.FindProperty("open").objectReferenceValue = LoadSprite("planta_cabeca_aberta");
            so.FindProperty("recovering").objectReferenceValue = LoadSprite("planta_cabeca_recuo");
            so.FindProperty("neckSprite").objectReferenceValue = LoadSprite("planta_pescoco");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Grupo das armadilhas de baixo da C (o botão azul desliga todas).</summary>
        internal const string TrapGroupC = "teste_C_baixo";

        /// <summary>Lâmina pendular com o pivô no teto em (x, y); <paramref name="phase"/> põe vizinhas fora de compasso.</summary>
        internal static void Blade(Transform parent, string name, float x, float y, float length, float phase, string group = TrapGroupC)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var arm = new GameObject("Haste");
            arm.transform.SetParent(go.transform, false);
            var rope = new GameObject("Corda").AddComponent<SpriteRenderer>();
            rope.transform.SetParent(arm.transform, false);
            rope.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            rope.sprite = LoadSprite("haste");
            rope.drawMode = SpriteDrawMode.Tiled;
            rope.size = new Vector2(0.25f, length);
            rope.sortingOrder = 3;
            var blade = new GameObject("Lâmina").AddComponent<SpriteRenderer>();
            blade.transform.SetParent(arm.transform, false);
            blade.transform.localPosition = new Vector3(0f, -length, 0f);
            blade.sprite = LoadSprite("lamina");
            blade.sortingOrder = 4;
            var trap = go.AddComponent<SeteVidas.World.Traps.SwingingBlade>();
            trap.Configure(length, 50f, 2.4f, phase);
            var so = new SerializedObject(trap);
            so.FindProperty("arm").objectReferenceValue = arm.transform;
            so.FindProperty("returnToSafeGround").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!string.IsNullOrEmpty(group)) go.AddComponent<SeteVidas.World.Traps.TrapGroupMember>().Configure(group);
        }

        /// <summary>Espinhos no chão (y 0) que o botão do grupo desliga.</summary>
        internal static void Spikes(Transform parent, float x, int width, string group = TrapGroupC)
        {
            TrapsSetup.Spikes(parent, x, width);
            var spikes = parent.GetChild(parent.childCount - 1);
            if (!string.IsNullOrEmpty(group)) spikes.gameObject.AddComponent<SeteVidas.World.Traps.TrapGroupMember>().Configure(group);
        }

        /// <summary>
        /// Serra que corre de <paramref name="a"/> a <paramref name="b"/> na altura <paramref name="y"/> (o botão do grupo
        /// desliga). Com <paramref name="vertical"/>, sobe e desce de a a b (Y) na coluna x = <paramref name="y"/>.
        /// </summary>
        internal static void Saw(Transform parent, string name, float a, float b, float y, bool vertical = false, string group = TrapGroupC)
        {
            var at = vertical ? new Vector2(y, a) : new Vector2(a, y);
            var saw = TrapsSetup.Instance<SeteVidas.World.Traps.SawBlade>(parent, "SawBlade", name, at);
            saw.Configure(a, b, vertical);
            PrefabUtility.RecordPrefabInstancePropertyModifications(saw);
            if (!string.IsNullOrEmpty(group)) saw.gameObject.AddComponent<SeteVidas.World.Traps.TrapGroupMember>().Configure(group);
        }

        /// <summary>Botão azul no chão: desliga para sempre as armadilhas do grupo.</summary>
        internal static void Switch(Transform parent, string name, float x, float y, string group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("botao");
            sr.sortingOrder = 2;
            var sw = go.AddComponent<SeteVidas.World.Traps.TrapSwitch>();
            sw.Configure(group);
            var so = new SerializedObject(sw);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("up").objectReferenceValue = LoadSprite("botao");
            so.FindProperty("down").objectReferenceValue = LoadSprite("botao_apertado");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Gancho (classe Arqueiro, futuro) na ponta de uma parede.</summary>
        internal static void Hook(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("gancho");
            sr.sortingOrder = 3;
            var a = go.AddComponent<GrappleAnchor>();
            var so = new SerializedObject(a);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Porta de residência (2 × 3 tiles) com os pés em <paramref name="y"/>.</summary>
        internal static void Residence(Transform parent, string name, float x, float y, string scene, string entrance, string verb, string doorId, string keyId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            bool locked = !string.IsNullOrEmpty(keyId);
            sr.sprite = LoadSprite(locked ? "porta_casa_trancada" : "porta_casa");
            sr.sortingOrder = -1; // atrás do gato: ele "entra" nela
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: " + verb, new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);
            var door = go.AddComponent<ResidenceDoor>();
            door.Configure(scene, entrance, verb, doorId, keyId);
            var so = new SerializedObject(door);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("lockedSprite").objectReferenceValue = LoadSprite("porta_casa_trancada");
            so.FindProperty("unlockedSprite").objectReferenceValue = LoadSprite("porta_casa");
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------ objetos

        static void Sanctuary(Transform parent, string id, string displayName, float x)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressionSetup.SanctuaryPrefabPath);
            ProgressionSetup.PlaceSanctuary(prefab, parent, id, displayName, x);
        }

        internal static void Trunk(Transform parent, string name, float x, float y, string id)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClassSetup.WallPrefabPath), parent);
            go.name = name;
            go.transform.position = new Vector3(x, y, 0f);
            var wall = go.GetComponent<BreakableWall>();
            var so = new SerializedObject(wall);
            so.FindProperty("wallId").stringValue = id;
            so.FindProperty("material").enumValueIndex = (int)BreakableMaterial.Wood;
            so.FindProperty("maxHealth").intValue = 12;   // 3 marretadas (dano 4)
            BreakableRules.BreakNoise(BreakableMaterial.Wood, out float r, out float l);
            so.FindProperty("breakNoiseRadius").floatValue = r;
            so.FindProperty("breakNoiseLoudness").floatValue = l;
            var intact = LoadSprite("tronco_rachado_intacto");
            so.FindProperty("intactSprite").objectReferenceValue = intact;
            so.FindProperty("crackedSprite").objectReferenceValue = LoadSprite("tronco_rachado_lascado");
            var sr = (SpriteRenderer)so.FindProperty("spriteRenderer").objectReferenceValue;
            if (sr != null) { sr.sprite = intact; PrefabUtility.RecordPrefabInstancePropertyModifications(sr); }
            var rubble = (GameObject)so.FindProperty("rubble").objectReferenceValue;
            var rsr = rubble != null ? rubble.GetComponent<SpriteRenderer>() : null;
            if (rsr != null) { rsr.sprite = LoadSprite("tronco_lascas"); PrefabUtility.RecordPrefabInstancePropertyModifications(rsr); }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void Chest(Transform parent, string name, float x, float y, string id, bool wood, int coins)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(2.5f, 2f);
            box.offset = new Vector2(0f, 1f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("bau_fechado");
            sr.sortingOrder = 2;
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(go.transform, false);
            promptGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var prompt = SeteVidas.Combat.DamagePopup.CreateText(promptGo, "E: abrir", new Color32(0xff, 0xe9, 0xa8, 0xff), 30);
            promptGo.SetActive(false);
            var chest = go.AddComponent<WoodChest>();
            var so = new SerializedObject(chest);
            so.FindProperty("chestId").stringValue = id;
            so.FindProperty("containsWood").boolValue = wood;
            so.FindProperty("coins").intValue = coins;
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("closedSprite").objectReferenceValue = LoadSprite("bau_fechado");
            so.FindProperty("openSprite").objectReferenceValue = LoadSprite("bau_aberto");
            so.FindProperty("prompt").objectReferenceValue = prompt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Galho de mão única: <paramref name="x"/> é a ponta esquerda; o topo fica em <paramref name="y"/>.</summary>
        internal static void Branch(Transform parent, string name, float x, float y, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + width * 0.5f, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "galho", new Vector2(width, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
        }

        internal static void Swaying(Transform parent, string name, float x, float y, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + width * 0.5f, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(width, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "galho", new Vector2(width, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
            go.AddComponent<SwayingBranch>();
        }

        internal static void Leaf(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3f, 0.25f);
            box.offset = new Vector2(0f, -0.125f);
            go.AddComponent<PlatformEffector2D>();
            go.AddComponent<OneWayPlatform>().Configure();
            Visual(go.transform, "folha_larga", new Vector2(3f, 0.375f), new Vector3(0f, -0.125f, 0f), 1);
            go.AddComponent<SinkingLeaf>();
        }

        internal static void Mushroom(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.75f, 0.95f);   // topo do chapéu desenhado (o gato não afunda nele)
            box.offset = new Vector2(0f, 0.475f);
            var cap = Visual(go.transform, "cogumelo", new Vector2(2f, 1f), new Vector3(0f, 0.5f, 0f), 1);
            var mush = go.AddComponent<BounceMushroom>();
            var so = new SerializedObject(mush);
            so.FindProperty("cap").objectReferenceValue = cap.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void RottenBranch(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Ground) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + 1f, y - 0.25f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(2f, 0.5f);
            var sr = Visual(go.transform, "galho_podre", new Vector2(2f, 0.5f), Vector3.zero, 1);
            var cp = go.AddComponent<CrumblingPlatform>();
            var so = new SerializedObject(cp);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("intact").objectReferenceValue = LoadSprite("galho_podre");
            so.FindProperty("cracked").objectReferenceValue = LoadSprite("galho_podre_rachando");
            so.FindProperty("isBranch").boolValue = true;
            so.FindProperty("respawnTime").floatValue = 5f;
            so.FindProperty("shakeTime").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        const string MapRegion = "Floresta de teste";
        static readonly Vector2 MapOffsetB = new Vector2(73f, 18f);

        internal static void Room(Transform parent, string id, string label, Vector2 center, Vector2 size, Vector2 offset, string region = MapRegion)
        {
            var go = new GameObject("Sala " + id + " (" + label + ")");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<MapRoom>().Configure(id, region, size, offset);
        }

        /// <summary>
        /// Corrente de cactos parados, conferida com as regras de alcance do pogo (<see cref="PlatformRules"/>): sai de um
        /// chão em pé em <paramref name="startFeetY"/> e termina num chão em pé em <paramref name="endTopY"/>. Avisa no
        /// console se algum passo for impossível (alto ou longe demais).
        /// </summary>
        static void PogoChain(Transform parent, string chain, float startFeetY, float endTopY, params (string name, float x, float y)[] cacti)
        {
            bool fromCactus = false;
            float fx = float.NaN, fy = startFeetY;
            foreach (var c in cacti)
            {
                bool side = fromCactus; // do chão, o gato escolhe de onde pula
                if (!PlatformRules.ReachesCactus(fromCactus, fx, fy, c.x, c.y, side))
                    Debug.LogWarning($"[Sete Vidas] {chain}: \"{c.name}\" fora de alcance (de y {fy:0.#} até y {c.y:0.#}, {Mathf.Abs(c.x - fx):0.#} para o lado).");
                Cactus(parent, c.name, c.x, c.y, Vector2.zero, 3f);
                fromCactus = true; fx = c.x; fy = c.y;
            }
            if (fromCactus && !PlatformRules.CactusReachesFloor(fy, endTopY))
                Debug.LogWarning($"[Sete Vidas] {chain}: o último cacto (y {fy:0.#}) não alcança o chão de chegada (y {endTopY:0.#}).");
        }

        /// <summary>Cacto Flutuante (bola de espinhos, ponto de pogo). <paramref name="travel"/> = vaivém a partir dali.</summary>
        internal static void Cactus(Transform parent, string name, float x, float y, Vector2 travel, float period)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Hazard) };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("cacto_flutuante");
            sr.sortingOrder = 3;
            var cactus = go.AddComponent<FloatingCactus>();
            cactus.Configure(0.6f, travel, period);
            var so = new SerializedObject(cactus);
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.FindProperty("returnToSafeGround").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static SpriteRenderer Visual(Transform parent, string sprite, Vector2 size, Vector3 local, int order)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingOrder = order;
            return sr;
        }

        internal static void Exit(Transform parent, string name, Vector2 center, Vector2 size, string scene, string entrance, string flag = "")
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(GameConstants.Layers.Interactable) };
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<RoomExit>().Configure(scene, entrance, flag);
        }

        internal static void Entrance(Transform parent, string name, Vector2 feet, string id, int facing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            go.AddComponent<RoomEntrance>().Configure(id, facing);
        }

        // ------------------------------------------------------------ arte provisória

        internal static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 BarkDark = new Color32(0x34, 0x26, 0x1e, 0xff);
        static readonly Color32 Bark = new Color32(0x52, 0x26, 0x1e, 0xff);
        static readonly Color32 BarkMid = new Color32(0x82, 0x3e, 0x2e, 0xff);
        static readonly Color32 Wood = new Color32(0x96, 0x6e, 0x40, 0xff);
        static readonly Color32 WoodLight = new Color32(0xc4, 0x98, 0x60, 0xff);
        static readonly Color32 Leaf1 = new Color32(0x2e, 0x48, 0x34, 0xff);
        static readonly Color32 Leaf2 = new Color32(0x4a, 0x68, 0x46, 0xff);
        static readonly Color32 Leaf3 = new Color32(0x6c, 0x86, 0x60, 0xff);
        static readonly Color32 Rotten = new Color32(0x6e, 0x6a, 0x64, 0xff);
        static readonly Color32 RottenDark = new Color32(0x46, 0x44, 0x42, 0xff);
        static readonly Color32 Pogo = new Color32(0x5f, 0xd3, 0xc7, 0xff);
        static readonly Color32 PogoLight = new Color32(0xb9, 0xf5, 0xec, 0xff);
        static readonly Color32 Stem = new Color32(0xe4, 0xe2, 0xdc, 0xff);
        static readonly Color32 Crack = new Color32(0xff, 0xd3, 0x4d, 0xff);
        static readonly Color32 CrackDark = new Color32(0x8a, 0x62, 0x1e, 0xff);
        static readonly Color32 Iron = new Color32(0xc0, 0xc6, 0xd2, 0xff);
        static readonly Color32 Outline = new Color32(0x1a, 0x14, 0x12, 0xff);

        /// <summary>Desenha os sprites provisórios (só se ainda não existem: dá para trocar pela arte de verdade no mesmo nome).</summary>
        internal static void EnsureSprites()
        {
            Directory.CreateDirectory(ArtFolder);
            Make("galho", 16, 6, (x, y) => y <= 2 ? (y == 0 ? BarkDark : (x % 5 == 0 ? BarkDark : Bark)) :
                                           y <= 3 ? Leaf1 : (x % 4 == 1 || x % 4 == 2) ? (y == 4 ? Leaf2 : Leaf3) : Clear);
            Make("galho_podre", 32, 8, (x, y) => y <= 5 ? (y == 0 || y == 5 ? RottenDark : (x % 7 == 3 ? RottenDark : Rotten)) : (x % 9 == 2 ? Rotten : Clear));
            Make("galho_podre_rachando", 32, 8, (x, y) => y <= 5 ? ((x == 10 || x == 11 || x == 21) && y > 1 ? Clear : y == 0 || y == 5 ? RottenDark : Rotten) : Clear);
            Make("folha_larga", 48, 6, (x, y) => y == 0 ? Leaf1 : y <= 3 ? (y == 2 && x > 3 && x < 44 ? Leaf2 : Leaf3) :
                                                 (x > 6 && x < 41 && y == 4 ? Leaf3 : Clear));
            Make("cogumelo", 32, 16, (x, y) =>
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 7f) / 8f;
                if (y >= 7 && dx * dx + dy * dy <= 1f) return (y >= 12 && x > 8 && x < 16) ? PogoLight : (y == 7 ? new Color32(0x34, 0x8c, 0xa0, 0xff) : Pogo);
                if (y < 7 && x >= 12 && x <= 19) return x == 12 || x == 19 ? new Color32(0xa8, 0xa4, 0x98, 0xff) : Stem;
                return Clear;
            });
            Make("cacto_flutuante", 24, 24, PaintCactus);
            // Galho Seco (24×32): tronco seco com braços; disfarçado = sem olhos e de braços colados
            Make("galho_seco", 24, 32, (x, y) => PaintDryBranch(x, y, 0));
            Make("galho_seco_braco", 24, 32, (x, y) => PaintDryBranch(x, y, 1));
            Make("galho_seco_preso", 24, 32, (x, y) => PaintDryBranch(x, y, 2));
            Make("galho_seco_disfarce", 24, 32, (x, y) => PaintDryBranch(x, y, 3));
            // Planta Carnívora (24×24): caule e cabeça; aberta = boca com dentes; recuo = cabeça baixa e flor turquesa (pogo)
            Make("planta_fechada", 24, 24, (x, y) => PaintPlant(x, y, 0));
            Make("planta_aberta", 24, 24, (x, y) => PaintPlant(x, y, 1));
            Make("planta_recuo", 24, 24, (x, y) => PaintPlant(x, y, 2));
            // porta de casa (32×48): moldura de madeira, porta, e a fechadura dourada quando trancada
            Make("porta_casa", 32, 48, (x, y) => PaintHouseDoor(x, y, false));
            Make("porta_casa_trancada", 32, 48, (x, y) => PaintHouseDoor(x, y, true));
            // Galho Seco alto e parado (24×88 = 5,5 tiles) e o Broto (com folhas e brotos no topo)
            for (int pose = 0; pose < 4; pose++)
            {
                int p = pose;
                string suffix = pose == 0 ? "" : pose == 1 ? "_braco" : pose == 2 ? "_preso" : "_disfarce";
                Make("galho_alto" + suffix, 24, 88, (x, y) => PaintTallBranch(x, y, p, sprout: false));
                Make("broto" + suffix, 24, 88, (x, y) => PaintTallBranch(x, y, p, sprout: true));
            }
            // Planta: base com folhas, gomo do pescoço e a cabeça (fechada, aberta, recuo com a flor do pogo)
            Make("planta_base", 24, 8, (x, y) => (y <= 3 && (x % 5 == 1 || x % 5 == 2)) || (y <= 1 && x > 4 && x < 19) ? Leaf2 : Clear);
            Make("planta_pescoco", 6, 6, (x, y) => { float dx = x - 2.5f, dy = y - 2.5f; return dx * dx + dy * dy <= 6.5f ? (dx + dy < 0 ? Leaf3 : Leaf2) : Clear; });
            Make("planta_cabeca", 24, 24, (x, y) => PaintPlantHead(x, y, 0));
            Make("planta_cabeca_aberta", 24, 24, (x, y) => PaintPlantHead(x, y, 1));
            Make("planta_cabeca_recuo", 24, 24, (x, y) => PaintPlantHead(x, y, 2));
            // Morcego (16×16): dormindo (asas fechadas, de cabeça para baixo) e duas batidas de asa
            Make("morcego_dormindo", 16, 16, (x, y) => PaintBat(x, y, 0));
            Make("morcego_voo_a", 16, 16, (x, y) => PaintBat(x, y, 1));
            Make("morcego_voo_b", 16, 16, (x, y) => PaintBat(x, y, 2));
            // lâmina pendular (32×12, aço com fio vermelho: vermelho = armadilha), haste (4×16) e o botão azul (24×8)
            Make("lamina", 32, 12, (x, y) =>
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 5.5f) / 5.5f;
                if (dx * dx + dy * dy > 1f) return Clear;
                if (y <= 1) return new Color32(0xc8, 0x3a, 0x3a, 0xff);         // o fio
                return dx * dx + dy * dy > 0.8f ? Outline : (y > 7 ? new Color32(0xe8, 0xec, 0xf2, 0xff) : Iron);
            });
            Make("haste", 4, 16, (x, y) => x == 0 || x == 3 ? Outline : (y % 4 == 0 ? BarkDark : BarkMid));
            Make("botao", 24, 8, (x, y) => y <= 1 ? Iron : (y <= 5 && x >= 4 && x <= 19 ? (y == 5 ? new Color32(0x9a, 0xd8, 0xff, 0xff) : new Color32(0x3a, 0x8a, 0xd8, 0xff)) : Clear));
            Make("botao_apertado", 24, 8, (x, y) => y <= 1 ? Iron : (y <= 2 && x >= 4 && x <= 19 ? new Color32(0x2a, 0x5a, 0x90, 0xff) : Clear));
            // Gancho de ferro (16×16): argola e a ponta curva
            Make("gancho", 16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 11f, d = dx * dx + dy * dy;
                if (d <= 12f && d >= 4f) return Iron;                                    // argola
                if (x >= 7 && x <= 8 && y >= 3 && y <= 8) return Iron;                   // haste
                if (y >= 2 && y <= 3 && x >= 4 && x <= 8) return Iron;                   // curva
                if (x == 4 && y >= 3 && y <= 5) return Iron;                             // ponta
                if (d < 4f && d > 1f) return Crack;                                      // brilho dourado (chama a atenção)
                return Clear;
            });
            Make("chave", 16, 8, (x, y) =>
            {
                var gold = new Color32(0xe0, 0xb0, 0x4a, 0xff);
                var shine = new Color32(0xff, 0xf4, 0xc2, 0xff);
                float dx = x - 3.5f, dy = y - 3.5f;
                float d = dx * dx + dy * dy;
                if (d <= 12f && d >= 3f) return x == 2 && y == 5 ? shine : gold;        // argola
                if (y >= 3 && y <= 4 && x >= 6 && x <= 14) return y == 4 && x < 12 ? shine : gold; // haste
                if ((x == 12 || x == 14) && y >= 1 && y <= 2) return gold;              // dentes
                return Clear;
            });
            Make("porta_fechada", 16, 64, (x, y) =>
            {
                if (x < 1 || x > 14) return Clear;
                if (x == 1 || x == 14 || y == 0 || y == 63) return Outline;
                if (y % 16 == 8) return BarkDark;                                     // tábuas
                if (x == 11 && y >= 28 && y <= 31) return Crack;                      // fechadura dourada
                return x % 4 == 0 ? BarkMid : Wood;
            });
            Make("porta_aberta", 16, 64, (x, y) => (x <= 2 && y > 0 && y < 63) ? (x == 1 ? Outline : Wood) : Clear);
            Make("portao_barras", 16, 64, (x, y) => (x % 5 == 2 || x % 5 == 3) ? (x % 5 == 2 ? Iron : new Color32(0x7a, 0x80, 0x8c, 0xff)) :
                                                     (y % 20 == 0 ? Iron : Clear));
            Make("bau_fechado", 16, 16, (x, y) => PaintChest(x, y, open: false));
            Make("bau_aberto", 16, 16, (x, y) => PaintChest(x, y, open: true));
            Make("tronco_rachado_intacto", 16, 64, (x, y) => PaintTrunk(x, y, damaged: false));
            Make("tronco_rachado_lascado", 16, 64, (x, y) => PaintTrunk(x, y, damaged: true));
            Make("tronco_lascas", 16, 8, (x, y) => y <= 2 && (x + y * 3) % 4 != 0 ? (x % 3 == 0 ? WoodLight : Wood) : (y == 3 && x % 5 == 1 ? Bark : Clear));
        }

        /// <summary>Galho Seco. pose: 0 de pé, 1 braço erguido (aviso), 2 braço preso no chão (vulnerável), 3 disfarçado.</summary>
        static Color32 PaintDryBranch(int x, int y, int pose)
        {
            var eye = new Color32(0xe8, 0xa6, 0x40, 0xff);
            // tronco (x 9..14, y 0..26), com casca
            if (x >= 9 && x <= 14 && y <= 26) return x == 9 || x == 14 ? Outline : ((x + y / 3) % 4 == 0 ? BarkDark : BarkMid);
            // galhinhos no topo
            if (y > 26 && y <= 30 && (x == 8 || x == 11 || x == 15) && (x + y) % 2 == 0) return Bark;
            // olhos (não no disfarce); no braço preso, a rachadura turquesa nas costas (pogo)
            if (pose != 3 && y == 21 && (x == 10 || x == 13)) return eye;
            if (pose == 2 && x == 12 && y >= 8 && y <= 16) return Pogo;
            // braço (lado esquerdo do desenho = a frente, o sprite vira com flipX)
            switch (pose)
            {
                case 1: if (x >= 3 && x <= 8 && y >= 22 && y <= 24) return Bark; if (x >= 3 && x <= 4 && y >= 24 && y <= 29) return BarkMid; break; // erguido
                case 2: if (x >= 1 && x <= 8 && y >= 1 && y <= 2) return Bark; if (x == 8 && y >= 2 && y <= 14) return Bark; break;              // no chão
                case 3: if (x >= 7 && x <= 8 && y >= 8 && y <= 18) return Bark; break;                                                           // colado
                default: if (x >= 4 && x <= 8 && y >= 12 && y <= 13) return Bark; if (x == 4 && y >= 6 && y <= 12) return Bark; break;          // de lado
            }
            if (y <= 1 && x >= 7 && x <= 16) return BarkDark; // raízes
            return Clear;
        }

        /// <summary>Galho Seco alto (24×88). pose: 0 de pé, 1 braço erguido, 2 braço preso, 3 disfarçado. Broto: folhas e brotos no topo.</summary>
        static Color32 PaintTallBranch(int x, int y, int pose, bool sprout)
        {
            var eye = new Color32(0xe8, 0xa6, 0x40, 0xff);
            const int top = 80;
            if (x >= 8 && x <= 15 && y <= top) return x == 8 || x == 15 ? Outline : ((x + y / 4) % 4 == 0 ? BarkDark : BarkMid);
            if (sprout)
            {
                float dx = x - 11.5f, dy = y - 82f;
                if (dx * dx / 90f + dy * dy / 30f <= 1f) return ((x + y) % 3 == 0) ? Leaf3 : Leaf2;  // copa de folhas
                if ((x == 3 || x == 20) && y >= 78 && y <= 80) return Leaf3;                       // brotos
            }
            else if (y > top && (x == 9 || x == 12 || x == 15) && (x + y) % 2 == 0) return Bark;   // galhinhos secos
            if (pose != 3 && y == 70 && (x == 10 || x == 13)) return eye;
            if (pose == 2 && x == 12 && y >= 30 && y <= 50) return Pogo;                           // rachadura: pode quicar
            switch (pose)
            {
                case 1: if (x >= 2 && x <= 7 && y >= 66 && y <= 68) return Bark; if (x >= 2 && x <= 3 && y >= 68 && y <= 76) return BarkMid; break;
                case 2: if (x >= 0 && x <= 7 && y >= 1 && y <= 2) return Bark; if (x == 7 && y >= 2 && y <= 40) return Bark; break;
                case 3: if (x >= 6 && x <= 7 && y >= 30 && y <= 60) return Bark; break;
                default: if (x >= 3 && x <= 7 && y >= 44 && y <= 45) return Bark; if (x == 3 && y >= 30 && y <= 44) return Bark; break;
            }
            if (y <= 1 && x >= 5 && x <= 18) return BarkDark;
            return Clear;
        }

        /// <summary>Cabeça da Planta (24×24, virada para cima). pose: 0 fechada, 1 boca aberta, 2 recuo (flor turquesa).</summary>
        static Color32 PaintPlantHead(int x, int y, int pose)
        {
            var head = new Color32(0x6c, 0x2e, 0x3a, 0xff);
            var headLight = new Color32(0x9a, 0x44, 0x50, 0xff);
            var tooth = new Color32(0xe4, 0xe2, 0xdc, 0xff);
            float cx = 11.5f, cy = 11f, dx = x - cx, dy = y - cy;
            if (dx * dx / 64f + dy * dy / 64f > 1f) return Clear;
            if (pose == 1 && Mathf.Abs(dx) < 2f && dy > -5f) return (y % 2 == 0) ? tooth : Outline;  // boca aberta para a frente
            if (pose == 2 && dy > 5f) return Pogo;                                                      // flor do pogo
            if (dx * dx / 64f + dy * dy / 64f > 0.8f) return Outline;
            return dx < 0f ? headLight : head;
        }

        /// <summary>Morcego (16×16). pose: 0 dormindo, 1 asas para cima, 2 asas para baixo.</summary>
        static Color32 PaintBat(int x, int y, int pose)
        {
            var body = new Color32(0x3c, 0x30, 0x4a, 0xff);
            var wing = new Color32(0x5a, 0x48, 0x6a, 0xff);
            var eye = new Color32(0xe8, 0xa6, 0x40, 0xff);
            if (pose == 0)
            {
                if (x >= 6 && x <= 9 && y >= 4 && y <= 13) return (x == 6 || x == 9) ? Outline : body; // enrolado
                if (x >= 7 && x <= 8 && y >= 14) return Outline;                                        // pés no poleiro
                return Clear;
            }
            if (x >= 6 && x <= 9 && y >= 6 && y <= 10) return (y == 8 && (x == 7 || x == 8)) ? eye : body;
            if (x >= 6 && x <= 9 && y == 11 && (x == 6 || x == 9)) return body;                        // orelhas
            int wy = pose == 1 ? 10 : 6;
            if ((x <= 5 || x >= 10) && Mathf.Abs(y - wy - (Mathf.Abs(x - 7.5f) - 3f) * (pose == 1 ? 0.5f : -0.5f)) < 1.2f && x >= 0 && x <= 15) return wing;
            return Clear;
        }

        /// <summary>Planta Carnívora. pose: 0 fechada, 1 boca aberta (aviso/mordida), 2 recuo (cabeça baixa, flor turquesa).</summary>
        static Color32 PaintPlant(int x, int y, int pose)
        {
            var stem = new Color32(0x4a, 0x68, 0x46, 0xff);
            var head = new Color32(0x6c, 0x2e, 0x3a, 0xff);
            var headLight = new Color32(0x9a, 0x44, 0x50, 0xff);
            var tooth = new Color32(0xe4, 0xe2, 0xdc, 0xff);
            int top = pose == 2 ? 14 : 22;              // no recuo a cabeça desce
            if (x >= 11 && x <= 12 && y < top - 7) return stem;
            if (y <= 2 && (x == 7 || x == 8 || x == 15 || x == 16)) return Leaf2; // folhas na base
            float cx = 11.5f, cy = top - 4f, dx = x - cx, dy = y - cy;
            if (dx * dx / 36f + dy * dy / 16f <= 1f)
            {
                if (pose == 1 && Mathf.Abs(dy) < 1.5f && dx > -2f) return (x % 2 == 0) ? tooth : Outline; // boca aberta
                if (pose == 2 && y >= top - 1) return Pogo;                                                   // flor: pode quicar
                return dy > 1f ? headLight : head;
            }
            return Clear;
        }

        static Color32 PaintHouseDoor(int x, int y, bool locked)
        {
            if (x < 2 || x > 29) return Clear;
            if (y >= 44) return y == 47 ? Outline : BarkDark;                                  // verga
            if (x <= 4 || x >= 27) return x == 2 || x == 29 ? Outline : BarkMid;               // batentes
            if (x >= 6 && x <= 25 && y <= 42)
            {
                if (locked && x >= 20 && x <= 22 && y >= 18 && y <= 22) return Crack;           // fechadura dourada
                return (x - 6) % 5 == 0 ? BarkDark : Wood;                                     // tábuas da porta
            }
            return new Color32(0x1a, 0x12, 0x10, 0xff);                                         // o escuro da entrada
        }

        /// <summary>Bola verde com 8 espinhos de ponta vermelha e a flor turquesa do pogo no topo (24×24).</summary>
        static Color32 PaintCactus(int x, int y)
        {
            var green = new Color32(0x2e, 0x6a, 0x4a, 0xff);
            var greenLight = new Color32(0x4a, 0x96, 0x5e, 0xff);
            var red = new Color32(0xc8, 0x3a, 0x3a, 0xff);
            float cx = 11.5f, cy = 11.5f, dx = x - cx, dy = y - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            // flor do pogo no topo
            if (y >= 18 && y <= 20 && x >= 10 && x <= 13) return (y == 20 || x == 10 || x == 13) ? Pogo : PogoLight;
            if (d <= 6.5f) return d > 5.5f ? Outline : (dx < -1f && dy > 1f ? greenLight : green);
            // espinhos: 8 direções
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f + Mathf.PI / 8f;
                float ux = Mathf.Cos(a), uy = Mathf.Sin(a);
                float along = dx * ux + dy * uy, across = Mathf.Abs(-dx * uy + dy * ux);
                if (along > 6f && along < 11.5f && across < 1.3f - (along - 6f) * 0.2f) return along > 9.5f ? red : Outline;
            }
            return Clear;
        }

        static Color32 PaintChest(int x, int y, bool open)
        {
            if (x < 1 || x > 14) return Clear;
            if (!open)
            {
                if (y > 12) return Clear;
                if (x == 1 || x == 14 || y == 0 || y == 12) return Outline;
                if (y == 7) return Iron;
                if ((x == 7 || x == 8) && (y == 5 || y == 6)) return Crack;   // fechadura dourada
                return y > 7 ? WoodLight : Wood;
            }
            if (y > 14) return Clear;
            if (y >= 8) return (x == 1 || x == 14 || y == 14) ? Outline : (y == 8 ? Iron : BarkMid); // tampa aberta, de pé
            if (x == 1 || x == 14 || y == 0) return Outline;
            if (y == 7) return Outline;
            return y >= 5 ? BarkDark : Wood;
        }

        static Color32 PaintTrunk(int x, int y, bool damaged)
        {
            if (x < 1 || x > 14) return Clear;
            if (x == 1 || x == 14) return Outline;
            Color32 c = (x + y / 3) % 5 == 0 ? BarkDark : (x % 4 == 2 ? BarkMid : Bark);
            // rachadura em zigue-zague pelo meio (amarela: "a marreta quebra isto")
            int zig = 7 + ((y / 4) % 2 == 0 ? (y % 4) - 1 : 2 - (y % 4));
            if (y > 4 && y < 60 && (x == zig || (damaged && (x == zig + 1 || x == zig - 2)))) return y % 8 < 6 ? Crack : CrackDark;
            if (damaged && (y == 20 || y == 41) && x > 3 && x < 12) return CrackDark;
            return c;
        }

        static void Make(string name, int w, int h, System.Func<int, int, Color32> paint)
        {
            string path = $"{ArtFolder}/{name}.png";
            if (File.Exists(path)) return;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = paint(x, y);
            tex.SetPixels32(px);
            tex.Apply(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
