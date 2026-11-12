using System.Collections.Generic;

namespace SeteVidas.EditorTools
{
    /// <summary>
    /// Enfeites da Fortaleza como DADOS (plano de assets, seção 3 e Etapa 8), separados do mapa jogável
    /// (<see cref="FortressLayout"/>): só cenário, sem colisão nem lógica. Cada item aponta para um id da
    /// biblioteca (manifest.json); variação, espelho e deslocamento saem de uma semente fixa pela posição
    /// (<c>DressingRules</c>), então o resultado é sempre o mesmo.
    ///
    /// C# 5 e sem UnityEngine (compila também no PowerShell para a prévia do mapa).
    /// Convenção de Y: apoio "chao" = y do chão; "teto" = y do teto; "parede"/"livre" = centro.
    /// </summary>
    public static class FortressDressing
    {
        public class Item
        {
            public string AssetId;
            public string Name;
            public float X, Y;
            /// <summary>Repetições em fileira (escudos), a cada <see cref="Step"/> tiles para a direita.</summary>
            public int Count = 1;
            public float Step;
            /// <summary>Área do mapa (1–10), para a montagem e a revisão área por área.</summary>
            public int Area;
            /// <summary>Variação fixa (-1 = sorteio pela posição). Use quando o tamanho importa para a leitura.</summary>
            public int Variant = -1;
            /// <summary>Grupo de objetos a que a peça pertence (Etapa 5), ou null.</summary>
            public string Group;
        }

        const string Statue = "env_cid_cul_statue_paladin";
        const string Banner = "env_cid_cul_banner_order";
        const string Window = "env_cid_arq_stained_glass";
        const string Shield = "env_cid_cul_shield_paladin";
        const string Torch = "env_com_obj_wall_torch";
        const string Rack = "env_sol_obj_weapon_rack";
        const string Target = "env_sol_obj_archery_target";
        const string Door = "env_sol_obj_porta_fechada";

        const string Pebble = "env_ext_roc_pequena", Medium = "env_ext_roc_media", Boulder = "env_ext_roc_rochedo";
        // Fósseis (variações em ordem): 0 crânio 20 px, 1 costelas 22 px, 2 osso longo 14 px, 3 coluna 24 px.
        // Só dentro da terra/rocha, nunca na superfície do caminho nem saindo de parede.
        const string Fossil = "env_ext_roc_fossil";
        const int Skull = 0, Ribs = 1, Bone = 2, Spine = 3;

        public static List<Item> Items()
        {
            var d = new List<Item>();
            const int WallWalk = FortressLayout.WallWalk, BellHall = FortressLayout.BellHall, SealFloor = FortressLayout.SealFloor;
            const int Cliff = FortressLayout.CliffTop;

            // ===== 1) Montanha (Etapa 2). A corrida até o salto (x 4..12) fica limpa: nada que pareça degrau.
            Add(d, 1, Boulder, "Rochedo atrás do início", -3.5f, Cliff);
            Group(d, 1, "montanha_estrada_negra", -5.6f, Cliff);   // a estrada Primata antiga termina no penhasco
            Fixed(d, 1, Fossil, Skull, "Fóssil na terra do início", 0.6f, Cliff - 1.6f);
            Fixed(d, 1, Fossil, Ribs, "Fóssil na parede de rocha", -12.5f, Cliff + 6f);   // 22 px: cabe em x -14..-11
            // Face do penhasco: o gato vê na queda de 60 tiles (x <= 12, dentro da rocha)
            Fixed(d, 1, Fossil, Spine, "Fóssil no penhasco (coluna)", 6f, 45f);
            Fixed(d, 1, Fossil, Skull, "Fóssil no penhasco (crânio)", 9.5f, 28f);
            Fixed(d, 1, Fossil, Ribs, "Fóssil no penhasco (costelas)", 4f, 12f);

            // ===== 2) Trilha (Etapa 2). Pedras jogáveis (2 e 3) são tiles de rocha; estas são só cenário, atrás.
            Fixed(d, 2, Boulder, 0, "Rochedo na lagoa", 17.2f, -1f);      // sem musgo; no fundo da bacia, meio dentro d'água
            Add(d, 2, Medium, "Pedra média do marco", 25.1f, 0f);         // na margem, depois do marco (a placa saiu da beira da água)
            // Osso de 14 px, todo dentro da parede esquerda do fosso (célula x 36): a vala descobriu o fóssil.
            // Nunca maior: saindo da parede, pareceria uma plataforma em cima dos espinhos.
            Fixed(d, 2, Fossil, Bone, "Fóssil no fosso", 36.45f, -1.5f);
            Fixed(d, 2, Fossil, Spine, "Fóssil na terra da trilha", 29.5f, -2.4f);

            // ===== Etapa 4: vegetação. A densidade cai da montanha (mata antiga) para a trilha (mata invadindo a estrada)
            // e para o pátio (só nas bordas); perto da muralha, a zona de tiro limpa: tocos com mudas voltando.
            // Nada na corrida do salto (x 4..12 só com a árvore morta, atrás), nas placas, nos pulos nem nas patrulhas.
            const string Seedling = "env_ext_veg_muda", Young = "env_ext_veg_jovem", Adult = "env_ext_veg_adulta", Ancient = "env_ext_veg_ancia";
            const string Dead = "env_ext_veg_morta", Stump = "env_ext_veg_toco", Shrub = "env_ext_veg_arbusto_fundo", Fg = "env_ext_veg_galho_frente";
            // 1) Montanha
            Fixed(d, 1, Ancient, 1, "Anciã do morrinho", -9f, Cliff + 1f);       // no alto do morrinho, meio atrás da rocha
            Fixed(d, 1, Ancient, 0, "Anciã do início", -1f, Cliff);              // sobre o rochedo, perto do início
            Add(d, 1, Young, "Jovem do início", 4.3f, Cliff);
            Add(d, 1, Seedling, "Muda da rampa", -4.9f, Cliff);
            Fixed(d, 1, Dead, 0, "Árvore morta na beira do salto", 11.6f, Cliff); // castigada pelo vento, na beira do penhasco
            Add(d, 1, Shrub, "Arbusto do início", -6.3f, Cliff);
            Add(d, 1, Shrub, "Arbusto da corrida", 6.8f, Cliff);
            Fixed(d, 1, Fg, 0, "Galho na frente (esquerda)", -6f, 72.5f);        // no alto da tela, nunca no meio
            Fixed(d, 1, Fg, 1, "Galho na frente (direita)", 9f, 73f);
            // 2) Trilha (a lagoa, as pedras do tutorial e o fosso ficam livres)
            Add(d, 2, Adult, "Adulta entre as pedras", 29.6f, 0f);
            Add(d, 2, Young, "Jovem antes do fosso", 35.1f, 0f);
            Add(d, 2, Shrub, "Arbusto do marco", 24.4f, 0f);
            // (o arbusto depois do fosso deu lugar ao posto de comércio da Etapa 5B)
            // 3) Pátio: zona de tiro limpa perto da muralha (o guarda patrulha na frente, os tocos ficam atrás)
            Add(d, 3, Stump, "Toco da zona de tiro 1", 97.2f, 0f);
            Fixed(d, 3, Stump, 1, "Toco da zona de tiro 2 (com broto)", 101f, 0f);
            Add(d, 3, Stump, "Toco da zona de tiro 3", 104.6f, 0f);
            Add(d, 3, Seedling, "Muda entre os tocos 1", 99.1f, 0f);
            Add(d, 3, Seedling, "Muda entre os tocos 2", 102.8f, 0f);
            Add(d, 3, Shrub, "Mato na borda do pátio", 98.6f, 0f);   // saiu de 106,2: lá fica a cesta do santuário
            // 10) A estrada para Miaravéu, depois do portão dos fundos: a mata volta
            Add(d, 10, Adult, "Adulta da estrada 1", 236.6f, 0f);
            Add(d, 10, Young, "Jovem da estrada", 241.2f, 0f);
            Fixed(d, 10, Ancient, 0, "Anciã da estrada", 248.4f, 0f);
            // melhoria 9: placas de seta no lugar dos textos de direção (variação 0 direita, 1 esquerda, 2 cima, 3 baixo)
            Fixed(d, 10, "env_sol_obj_placa_seta", 0, "Placa: estrada para Miaravéu", 238f, 0f);
            Fixed(d, 7, "env_sol_obj_placa_seta", 2, "Placa: Torre do Selo", 142f, BellHall);
            Fixed(d, 9, "env_sol_obj_placa_seta", 3, "Placa: desça pelo poço", 219.02f, SealFloor);   // posição do usuário
            Add(d, 10, "env_ext_veg_tronco_caido", "Tronco caído da estrada", 243.8f, 0f);
            // (o arbusto da estrada 1 deu lugar ao posto de comércio dos estrangeiros)
            Add(d, 10, Shrub, "Arbusto da estrada 2", 246.2f, 0f);

            // ===== 3) Pátio de treino
            Add(d, 3, Statue, "Estátua do guardião", 44.5f, 0f);
            Group(d, 3, "patio_treino", 67f, 0f);                   // suporte de armas + balde + alvo
            // Nada flutua e toda sombra tem motivo (correções da revisão):
            // a tenda faz a sombra do fardo de feno (sombra x 82..88, até 5 de altura)
            Add(d, 3, "env_sol_obj_tenda", "Tenda do pátio", 84.4f, 0f);   // 7 tiles: termina antes da torre de escalada (x 88)
            // a arquibancada se apoia no chão (postes nas pontas da passagem baixa) e tem um toldo sobre o atirador
            Add(d, 3, "env_sol_obj_poste_curto", "Poste da arquibancada (esquerda)", 50.2f, 0f);
            Add(d, 3, "env_sol_obj_poste_curto", "Poste da arquibancada (direita)", 55.8f, 0f);
            Add(d, 3, "env_sol_obj_poste_toldo", "Poste do toldo (esquerda)", 50.2f, 7f);
            Add(d, 3, "env_sol_obj_poste_toldo", "Poste do toldo (direita)", 55.8f, 7f);
            Add(d, 3, "env_sol_obj_toldo", "Toldo da arquibancada", 53f, 10.55f);
            // o alto da torre de escalada (parede do fundo): ameias só sobre o fundo, nunca no topo da coluna onde se anda
            Row(d, 3, "env_cid_arq_ameias_fundo", "Ameias da torre", 88.5f, 19f, 5, 1f);

            // ===== 5) Galeria da muralha
            Add(d, 5, Torch, "Tocha da galeria 1", 165f, WallWalk + 2.6f);
            Add(d, 5, Torch, "Tocha da galeria 2", 186f, WallWalk + 2.6f);
            Add(d, 5, Banner, "Estandarte da galeria", 170f, 51.9f);
            // As sombras da galeria: a luz vem das tochas à direita; uma estátua gigante entre a tocha e a sombra
            // bloqueia a luz, e a tocha de dentro da sombra está apagada (a ronda passou)
            // A sombra é o fundo de uma alcova (recuo em arco na parede) onde a luz das tochas não chega:
            // mesma largura e altura da sombra jogável (FortressLayout: A 10×5 em x 148, B 8×5 em x 176)
            Add(d, 5, "env_cid_arq_alcova_10x5", "Alcova da sombra A", 148f, WallWalk);
            Add(d, 5, "env_cid_arq_alcova_8x5", "Alcova da sombra B", 176f, WallWalk);
            Add(d, 5, "env_cid_cul_estatua_gigante", "Estátua gigante da galeria A", 155.4f, WallWalk);
            Add(d, 5, "env_com_obj_wall_torch_apagada", "Tocha apagada da sombra A", 148f, WallWalk + 2.6f);
            Add(d, 5, "env_cid_cul_estatua_gigante", "Estátua gigante da galeria B", 181.2f, WallWalk);
            Add(d, 5, "env_com_obj_wall_torch_apagada", "Tocha apagada da sombra B", 176f, WallWalk + 2.6f);

            // ===== 7) Salão dos Votos
            Add(d, 7, Banner, "Estandarte do salão", 192f, 81.9f);

            // ===== 9) Câmara do Selo
            Add(d, 9, Statue, "Estátua da câmara 1", 149f, SealFloor);
            Add(d, 9, Statue, "Estátua da câmara 2", 174.5f, SealFloor);
            // perto do Tomo a vida some: plantas secas nos vasos ao pé das estátuas (Etapa 4)
            Add(d, 9, "env_nec_veg_vaso_seco", "Vaso seco da câmara 1", 150.7f, SealFloor);
            Add(d, 9, "env_nec_veg_vaso_seco", "Vaso seco da câmara 2", 172.8f, SealFloor);
            Add(d, 9, Banner, "Estandarte do selo", 161.5f, 109.9f);
            Add(d, 9, Banner, "Estandarte do tomo", 181.5f, 106.9f);

            // ===== Etapa 5A: quem vive e trabalha em cada área (grupos de objetos, seção 5 do plano).
            // Nada na frente de placas, alavancas de jogo, pulos ou do caminho das armadilhas; tudo atrás (decor_atras).
            // 1) Montanha: alguém acampou antes do gato
            Group(d, 1, "exterior_acampamento", 0.3f, Cliff);   // ao lado do início, antes da corrida do salto (x 4..12)
            // 3) Pátio: a estação de treino (suporte e alvo já existem) ganha o balde; o canto dos recrutas embaixo da tenda
            Group(d, 3, "patio_mantimentos", 82.5f, 0f);
            // 4) Escalada: munição no alto das muralhas (a defesa de verdade)
            Group(d, 4, "defesa_muralha", 113.4f, 15f);
            // o pombal dos mensageiros no alto da muralha, ao ar livre (pedido do usuário: antes ficava dentro da galeria)
            Group(d, 4, "muralha_pombal", 113.4f, 15f);
            // a aljava e os virotes na arquibancada do pátio (o usuário levou da muralha para cá)
            Add(d, 3, "env_sol_obj_aljava", "Aljava na arquibancada", 51.97f, 7f);
            Add(d, 3, "env_sol_obj_virotes", "Virotes na arquibancada", 52.82f, 7f);
            Group(d, 4, "defesa_braseiro", 133.9f, WallWalk);
            // 5) Galeria (alojamento): beliche e baú, a partida de dados largada, a cozinha, a escala da guarda
            // (pedido do usuário: é o alojamento, então mais camas e mesas; a armadilha foi para o pé da torre do sino)
            Group(d, 5, "alojamento_dormitorio", 157.9f, WallWalk);     // dois beliches embaixo da passarela
            Add(d, 5, "env_sol_obj_escala", "Escala da guarda", 163.3f, WallWalk + 1.9f);
            Group(d, 5, "alojamento_cama", 167.6f, WallWalk);
            Group(d, 5, "alojamento_cama", 185.4f, WallWalk);
            Group(d, 5, "alojamento_mesa", 189.9f, WallWalk);
            Group(d, 5, "alojamento_cozinha", 193.7f, WallWalk);
            Group(d, 5, "alojamento_mesa", 198f, WallWalk);
            // Portas fechadas (pedido do usuário): corredores que existem mas o gato não usa. No fundo das alcovas da
            // galeria, no salão, na passagem dos fundos e na entrada da câmara. Variação fixa: 0 trancada, 1 tábuas em X
            Fixed(d, 5, Door, 0, "Porta da alcova A", 146.6f, WallWalk);
            Fixed(d, 5, Door, 1, "Porta da alcova B", 174.6f, WallWalk);
            Fixed(d, 7, Door, 0, "Porta do salão (embaixo do escudo)", 179.2f, BellHall);   // fora do arco da saída
            Fixed(d, 10, Door, 1, "Porta da passagem dos fundos", 194.4f, SealFloor);
            // 8) Galeria das armadilhas: o cano de óleo corre no rodapé, atrás dos bicos dos jatos (no alto ou descendo
            // pela parede ele parecia corrimão e grade); elmo e escudo de quem falhou no fosso (o mecanismo, o registro e a
            // alavanca saíram a pedido do usuário: poluíam o lado da armadilha)
            Row(d, 8, "env_cid_obj_tubo_oleo", "Cano de óleo dos jatos", 161.5f, BellHall + 0.17f, 13, 1f);
            Group(d, 8, "armadilhas_fosso", 146.3f, BellHall - 2f);
            // 10) Fundos: o arquivo e o depósito que ninguém visita
            Group(d, 10, "servico_arquivo", 208.9f, SealFloor);
            Group(d, 10, "servico_deposito", 212.6f, SealFloor);
            // ===== Etapa 5B: cultura, estrangeiros, Primatas, magia, praga, as sete portas e a arquitetura
            // 2) Trilha: o posto de comércio dos estrangeiros (fora dos muros) e um vestígio da estrada negra
            Group(d, 2, "trilha_carga_largada", 42.3f, 0f);   // a carga que ficou para trás (a barraca foi para a estrada, fora dos muros)
            Group(d, 2, "vestigio_primata", 36.4f, 0f);
            // 3) Pátio: porta 1 (selo intacto e limpo) no alto da torre dos escudeiros (o próprio lançador de flechas já é
            // um bloco de pedra embutido com a boca de bronze: as seteiras de enfeite atrás deles saíram)
            Fixed(d, 3, "env_mag_selo_porta", 0, "Selo da porta 1", 91f, 15.6f);
            // 4) Muralhas: seteira atrás do lançador
            // 5) Galeria: grade levantada e porta 2 na entrada e a corda cortada (a marca de giz ao lado da placa saiu:
            // parecia um ícone de interface; as pontas de viga no
            // teto saíram: pareciam comportas)
            Add(d, 5, "env_cid_arq_portao_levantado", "Grade levantada da entrada", 137.5f, 52f);
            Fixed(d, 5, "env_mag_selo_porta", 1, "Selo da porta 2", 137.5f, 50.3f);
            Add(d, 5, "env_com_obj_corda_cortada", "Corda cortada da Guilda", 153f, 52f);
            // 6) Torre do Sino: o sino no teto do salão sobre a chaminé, porta 3 com correntes,
            // seteira atrás do lançador
            Group(d, 6, "torre_sino", 209.5f, 82f);
            Fixed(d, 6, "env_mag_selo_porta", 2, "Selo da porta 3", 207f, 50.4f);
            // 7) Salão dos Votos: colunas (nunca atrás de plataforma), tapete, o altar do juramento com uma vela apagada
            // (primeiro sinal da praga), a fissura roxa no vitral dos cacos e a porta 4 na saída para as armadilhas
            Group(d, 7, "salao_colunas", 188f, BellHall);
            Row(d, 7, "env_cid_cul_tapete", "Tapete do salão", 184.5f, BellHall, 12, 1f);
            Group(d, 7, "salao_altar", 205f, BellHall);
            Add(d, 7, "env_nec_fissura_vitral", "Fissura no vitral", 186f, BellHall + 6f);
            Fixed(d, 7, "env_mag_selo_porta", 3, "Selo da porta 4", 176.6f, BellHall + 7.6f);   // (o arco de passagem saiu a pedido do usuário)
            // 8) Armadilhas e Torre do Selo: veias finas, depois com galhos; porta 5; seteira atrás do lançador
            Fixed(d, 8, "env_nec_veia", 0, "Veias finas da galeria 1", 150.5f, BellHall + 4f);
            Fixed(d, 8, "env_nec_veia", 0, "Veias finas da galeria 2", 158.6f, BellHall + 5f);
            Fixed(d, 8, "env_nec_veia", 1, "Veias da torre do selo 1", 140.8f, 86.5f);
            Fixed(d, 8, "env_nec_veia", 1, "Veias da torre do selo 2", 141.3f, 95.5f);
            Fixed(d, 8, "env_mag_selo_porta", 4, "Selo da porta 5", 143.6f, BellHall + 8f);
            // 9) Câmara do Selo: correntes e selos, runas e veias grossas subindo para o tomo; portas 6 e 7
            Group(d, 9, "camara_correntes", 152.5f, 110f);
            Group(d, 9, "camara_runas", 146.9f, SealFloor);
            Fixed(d, 9, "env_mag_selo_porta", 5, "Selo da porta 6", 143.4f, SealFloor + 3.3f);
            Fixed(d, 9, "env_mag_selo_porta", 6, "Selo da porta 7 (rompido)", 184.6f, SealFloor + 3.3f);
            Fixed(d, 9, "env_mag_selo_cera", 1, "Selo de cera rompido do tomo", 181.6f, SealFloor + 2.4f);
            // 10) Estrada: o posto de comércio dos estrangeiros logo depois do portão dos fundos (fora dos muros, como na lore)
            Group(d, 10, "posto_comercio", 240f, 0f);
            // 10) Fundos: estandarte velho, marca da Guilda e corda, relíquias Primatas nos cofres
            Add(d, 10, "env_cid_cul_estandarte_desbotado", "Estandarte velho dos fundos", 190.5f, 104.6f);
            Add(d, 10, "env_com_obj_corda_cortada", "Corda cortada dos fundos", 198f, 106f);
            Fixed(d, 10, "env_pri_obj_reliquia", 0, "Relíquia do cofre 72", 228.05f, BellHall);
            Fixed(d, 10, "env_pri_obj_reliquia", 1, "Relíquia do cofre 43", 228.05f, WallWalk);
            Fixed(d, 10, "env_pri_obj_reliquia", 2, "Relíquia do cofre 15", 228.05f, 15f);
            // (os caixotes espalhados pela galeria são esconderijos de verdade: objetos "bush" no FortressLayout)
            // ===== Etapa 7: mundo vivo (o resto vem do FortressLife: vento, partículas, bichos, pulsos)
            Fixed(d, 8, "env_nec_obj_inseto_morto", 0, "Insetos mortos da galeria 1", 159f, BellHall);
            Fixed(d, 8, "env_nec_obj_inseto_morto", 1, "Insetos mortos da galeria 2", 177.2f, BellHall);
            Add(d, 10, "env_com_obj_buraco_rato", "Buraco do rato dos fundos", 215.6f, SealFloor);
            Add(d, 5, "env_com_obj_buraco_rato", "Buraco do rato da cozinha", 200.5f, WallWalk);   // o rato da cozinha some aqui
            // ===== Etapa 6B: primeiro plano em silhueta, só encostado no teto (nunca na altura do gato nem sobre rota)
            Add(d, 5, "fg_corrente", "Corrente na frente (galeria)", 193.5f, 52f);
            Add(d, 7, "fg_corrente", "Corrente na frente (salão 1)", 179.5f, 82f);
            Add(d, 7, "fg_corrente", "Corrente na frente (salão 2)", 207.3f, 82f);   // fora da frente do emblema (205)
            Add(d, 9, "fg_corrente", "Corrente na frente (entrada da câmara)", 141.2f, 110f);
            return d;
        }

        /// <summary>
        /// Grupos (plano, Etapa 5): objetos que contam juntos quem vive ou trabalha ali. Cada peça é
        /// "id|dx|dy" ou "id|dx|dy|variação" (tiles, a partir do ponto do grupo; dy soma ao y — para apoio "parede" é o centro).
        /// </summary>
        public static readonly Dictionary<string, string[]> Groups = new Dictionary<string, string[]>
        {
            { "exterior_acampamento", new[] { "env_ext_obj_fogueira_apagada|0|0", "env_ext_obj_mochila|1.05|0" } },
            { "patio_mantimentos", new[] { "env_sol_obj_barril|0|0", "env_sol_obj_sacos|3.9|0" } },
            { "defesa_muralha", new[] { "env_sol_obj_corda|1.85|0" } },   // a aljava e os virotes foram para a arquibancada (usuário)
            { "defesa_braseiro", new[] { "env_sol_obj_braseiro|0|0", "env_sol_obj_virotes|-1.05|0" } },
            { "alojamento_cama", new[] { "env_sol_obj_beliche|0|0", "env_sol_obj_bau|1.55|0" } },
            { "alojamento_dormitorio", new[] { "env_sol_obj_beliche|0|0", "env_sol_obj_beliche|2.1|0" } },
            { "alojamento_mesa", new[] { "env_sol_obj_banco|-1.55|0", "env_sol_obj_mesa_dados|0|0", "env_sol_obj_banco|1.55|0" } },
            { "alojamento_cozinha", new[] { "env_sol_obj_barril|0|0", "env_sol_obj_panela|0.95|0", "env_sol_obj_vassoura|1.75|0" } },
            { "armadilhas_mecanismo", new[] { "env_cid_obj_engrenagem|0|2.6", "env_cid_obj_alavanca|0.95|1.1", "env_cid_obj_registro|-0.95|1.5" } },
            { "armadilhas_fosso", new[] { "env_cid_obj_elmo_amassado|0|0", "env_cid_obj_escudo_amassado|2.6|0" } },
            { "servico_arquivo", new[] { "env_sol_obj_prateleira|0|0", "env_sol_obj_prateleira|1.98|0", "env_sol_obj_prateleira|-1.98|0", "env_com_obj_teia|-7.45|5" } },   // arranjo do usuário
            // Etapa 5B
            { "montanha_estrada_negra", new[] { "env_pri_obj_marco|0|0", "env_ext_roc_pequena|0.75|0" } },
            { "patio_treino", new[] { "env_sol_obj_weapon_rack|0|0", "env_sol_obj_balde_agua|1.45|0", "env_sol_obj_archery_target|3|0" } },
            { "posto_comercio", new[] { "env_ext_obj_barraca_comercio|0|0", "env_cid_cul_penas|-1.05|0" } },
            { "vestigio_primata", new[] { "env_pri_obj_marco|0|0", "env_pri_obj_cristal_apagado|-6|0" } },
            { "torre_sino", new[] { "env_cid_cul_sino|0|0" } },
            { "muralha_pombal", new[] { "env_cid_cul_pombal|0|0", "env_cid_cul_penas|-1.2|0" } },
            { "caixotes_pilha", new[] { "env_sol_obj_caixote|0|0", "env_sol_obj_caixote|0.85|0", "env_sol_obj_caixote|0.42|0.69" } },
            { "caixotes_dupla", new[] { "env_sol_obj_caixote|0|0", "env_sol_obj_caixote|0.85|0" } },
            { "trilha_carga_largada", new[] { "env_sol_obj_caixote|-0.6|0", "env_sol_obj_sacos|0.6|0" } },
            { "salao_colunas", new[] { "env_cid_arq_coluna|0|0", "env_cid_arq_coluna|8|0" } },
            { "salao_altar", new[] { "env_cid_cul_vela|-1.4|0", "env_cid_cul_vela_apagada|1.4|0" } },   // a placa e o emblema saíram (usuário)
            { "camara_correntes", new[] { "env_mag_corrente|0|0", "env_mag_corrente|11|0", "env_mag_selo_cera|0|-3.3|0", "env_mag_selo_cera|11|-3.3|1" } },
            { "camara_runas", new[] { "env_nec_veia|10.1|1.5|2", "env_nec_veia|19.1|3.5|2" } },   // as runas saíram (usuário)
            { "servico_deposito", new[] { "env_sol_obj_barril|0|0", "env_sol_obj_sacos|1.05|0", "env_sol_obj_caixote|2.15|0" } },
        };

        static void Group(List<Item> d, int area, string group, float x, float y)
        {
            foreach (string piece in Groups[group])
            {
                var p = piece.Split('|');
                float dx = float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture);
                float dy = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                int variant = p.Length > 3 ? int.Parse(p[3]) : -1;
                d.Add(new Item { Area = area, AssetId = p[0], Name = group + ": " + p[0], X = x + dx, Y = y + dy, Group = group, Variant = variant });
            }
        }

        static void Add(List<Item> d, int area, string id, string name, float x, float y)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y });
        }

        static void Fixed(List<Item> d, int area, string id, int variant, string name, float x, float y)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y, Variant = variant });
        }

        static void Row(List<Item> d, int area, string id, string name, float x, float y, int count, float step)
        {
            d.Add(new Item { Area = area, AssetId = id, Name = name, X = x, Y = y, Count = count, Step = step });
        }
    }
}
