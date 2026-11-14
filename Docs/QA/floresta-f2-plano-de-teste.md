# Plano de teste — Floresta F2 (graybox da floresta inteira)

Antes: rode **Tools > Sete Vidas > 21. Floresta F2: graybox (7 cenas)**. Ele confere o alcance (console: "alcance conferido") e gera `Forest_Road` e `Forest_A_Roots` … `Forest_F_Heart` em `Scenes/World/Forest/`. A `Forest_A_Roots` abre sozinha.
Para dar Play direto numa delas, desmarque **Tools > Sete Vidas > Play começa na tela inicial**. Assim o graybox usa o **save da área de testes** (`save_playground.json`), com Paladino e 2 corações, e nunca mexe no save do jogo.
Blocos e arte provisória. Inimigos são placeholders: **Sentinela** no lugar do Sentinela Infectado e da Serpente; Plantas nos "buracos de serpente". O que está em teste é **escala, caminho e softlock**.
Mudou algo? Ajuste em `Assets/_Game/Editor/ForestLayout.cs` (a cena é recriada) e rode `Tools/LevelPreview/checar_floresta.ps1` antes de abrir o Unity.

## Testes automáticos

- Test Runner > EditMode > `ForestGrayboxTests` (save do graybox, lado fechado do atalho) e `ForestFoundationTests`.
- Alcance: **Tools > Sete Vidas > 21b. Floresta F2: conferir alcance** (ou o `.ps1`): os 12 baús, todas as saídas e atalhos, sem softlock, também só pela rota principal de cada cena. É física aproximada: pulos "no limite" passam lá e podem ser difíceis aqui.

## Roteiro manual

| # | Passo | Esperado |
|---|---|---|
| 1 | Play na `Forest_A_Roots` | O gato acorda na A1 (Fundo do Vale) |
| 2 | Atravesse A1 → A2 | Patrulha na A1 e na A2; sombras e folhagem para passar sem ser visto; S-F1 entre as raízes |
| 3 | A3: suba pelos galhos, quebre o tronco (Paladino), abra o W01 | Madeira 1/12. O tronco de baixo leva à A5 |
| 4 | A5 (opcional): pela chaminé de vinhas, chegue ao teto do vagão | W02 (madeira + 5 moedas); serra no teto do vagão A |
| 5 | A4: suba as 3 telas pelas duas colunas (galhos / vinha) | Os morcegos acordam; as colunas se cruzam no galho que balança |
| 6 | A6: atravesse por cima (galhos), pelo meio (galhos que balançam) ou por baixo (patrulha, Galho Seco, espinhos) | A lâmina pendular varre a rota de cima. No fim, a raiz AT8: "E: descer pela raiz" leva à A2; lá, a raiz agora sobe de volta |
| 7 | Antes de abrir a AT8, tente usar a raiz da A2 | Balão "Não abre deste lado." |
| 8 | Saída da A6 → B1 | Troca de cena; a vida continua igual |
| 9 | B1: vão de espinhos, cogumelo, cacto; o degrau final só com pogo nos espinhos | Sem pogo não sobe; com pogo atravessa o buraco do teto |
| 10 | B2 → B3 → B4 → B5 | Cactos em escada (ou a vinha da esquerda); plantas com pogo no recuo (W03 no alto); torre com chaminé ou escada quebrada (W04 atrás do tronco) |
| 11 | B3: buraco do teto acima do W03 → B7 (opcional) | Subida só de pogo; cair volta ao começo; W05 no topo |
| 12 | B5: cipó AT1 (perto da entrada) | Desce para a A6; o cipó da A6 passa a subir |
| 13 | B5 → C1 (escuro) → C2 → C3 | Patrulhas, folhagem, fio de alarme (o barulho chama). C2: Galho Seco na coluna de galhos, plantas na coluna da vinha |
| 14 | C3 (prédio): suba por dentro (andar a andar) ou por fora (vinha + janelas) | W06 no escritório: só pela fresta de baixo (agachado), com a patrulha na frente |
| 15 | C3 → C7 (porta à esquerda no 3º andar, opcional) | Galhos podres, Galho Seco, Broto, planta; W08 no topo |
| 16 | C2 (topo): raiz AT5 | Leva à B3; lá a raiz passa a subir |
| 17 | C4 lago → C5 estrada → C6 ravina | Ilha com o W07 (furtiva); C5 com atirador no poste, serra e lançador; túnel AT6 no fim da C5 volta para a C1; S-F2 na base da C6 |
| 18 | C6: suba pela chaminé ou pelos galhos e entre no buraco do teto | Carrega a `Forest_D_Refuge` (D1) |
| 19 | D1 → D2 → D3 → D4 | Flechas como degraus; patrulhas na D2; S-F3 e W09 no telhado (pogo nos espinhos); cipó AT2 na D4 desce para a Ravina |
| 20 | D4 → E1: rampa 1:2, vão de espinhos (só pogo), rampa 1:1, descida correndo, pulo para a vinha do pilar | Dá para fazer sem parar. Também dá para voltar (pilar tem vinha dos dois lados) |
| 21 | E2: rampas dos patamares → galho que balança → vinha; galho podre até a vinha de cima | Cair nunca mata: patamares embaixo |
| 22 | E3 por cima: rampa 1 cai na rampa 2, pulo no galho podre sobre os espinhos | O galho podre cai ~0,6 s depois de pisar. Por baixo: tronco + W10 + patrulha |
| 23 | Conte os ganchos (aros dourados) em E1–E4 | Vários por sala, no alto (Caçador, futuro) |
| 24 | E4: S-F4 e a trilha AT7 | A trilha leva à E1; lá ela passa a subir. A placa avisa "daqui em diante não há volta" |
| 25 | E4 → E5: caia no focinho | **Não dá para subir de volta para a E4** (nem pelos galhos). W11 no nicho embaixo, com patrulha |
| 26 | E5 → E6 → F1 | Furtividade contínua; a F1 não tem saída para o oeste. Morrer aqui volta para o S-F4 (E4) |
| 27 | F1 → G1 → G2 → G3 → R5 | Arena em blocos (o chefe vem na F8); W12 no desvio da G1; G2: cogumelos ou chaminé; porta AT3 desce para o Refúgio; G3 sai na cabeceira leste (S-F5) |
| 28 | Abra a aba **Mapa** em várias regiões | As salas visitadas aparecem no lugar certo, com baús e santuários |
| 29 | **Escala:** cronometre a rota crítica (A1 → R5) e a coleta das 12 madeiras | Anote os tempos e os trechos com mais de 2 telas sem decisão |
| 30 | Feche o Play e dê Play de novo numa cena já visitada | Troncos quebrados, baús abertos e atalhos abertos lembrados |

## Para anotar no playtest

- Pulos que passaram no `checar_floresta` mas ficaram difíceis no controle (sala, posição).
- Salas longas demais ou vazias; onde cortar (a subida total ficou bem maior que a da rev. 2).
- Onde a furtividade não funciona (patrulha que vê tudo, sombra sem utilidade).
