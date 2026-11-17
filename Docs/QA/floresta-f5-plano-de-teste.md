# Plano de teste — Floresta F5, parte 1: cenário (14/11/2026)

O que mudou: só o **desenho**. A colisão, as salas, os inimigos e os objetos são os mesmos da rev. 4 / F4b.

Antes:
1. Rode `powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar.ps1` (já rodado: os PNGs estão em `Art/Environments/Forest/Terrain` e `Backgrounds/Flo`). Volte ao Unity e espere a importação.
2. Rode **Tools > Sete Vidas > Floresta: gerar as cenas**. O menu importa a biblioteca de cenário (fundos `bg_flo_*`), pinta o terreno e escreve no console, por cena: "N tiles, N de massa, N detalhes, N emissores". **Nenhum erro "sem arte"** deve aparecer.
3. Não precisa rodar o 22 (mapa do mundo): o tilemap Ground continua com as mesmas células.

Sem o Unity: `Tools/LevelPreview/conferir_arte_floresta.ps1` (todo tile pedido tem PNG, nas 7 cenas: conferido) e `Tools/LevelPreview/ver_floresta.ps1 <cena> <sala> [-Dx n -Dy n]` (uma tela com a arte; saída em `SeteVidasDocs/Referencias/floresta/arte_*.png`).

## Testes automáticos

- EditMode > `ForestSceneryTests` (6): toda arte que a regra pede existe; faixas do chão (musgo, terra, transição, rocha; concreto sem rocha); coluna fina vira tronco e o piso embaixo continua terra; sem 3 variações iguais seguidas; brotos da praga só onde ela chegou; rampas só 1:1 e 1:2 (e nunca no concreto).
- Rode também `TerrainAutotileTests` (a Fortaleza não mudou) e os da Floresta (`ForestFoundationTests`, `ForestGrayboxTests`, `ForestBestiaryTests`).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | A1 (acordar) | Olhe a tela parado | Céu índigo com a lua no alto à esquerda (halo em anéis lisos), copas, árvores médias e troncos próximos. **Nenhum pontilhado cinza** e nenhum facho solto: a sombra no chão do fundo sai do pé de cada árvore. Embaixo, **névoa em fiapos lisos andando devagar** (a de trás para a direita, a da frente para a esquerda), sem tremer nem mostrar emenda Chão com musgo, samambaias e terra; nada de caixas cinza |
| 2 | A1 | Ande para a direita e pule | O fundo desliza em camadas (as de perto mais rápido); **não treme** e não aparece emenda nas camadas (960 px) |
| 3 | Qualquer sala | Vá até a borda da sala | Fora da sala só há a **massa escura** (terra e raízes); nunca o céu cortado nem o vazio |
| 4 | A1 (teto aberto), saídas laterais, buracos | Olhe as aberturas | A massa abre um corredor na saída: o gato não parece entrar numa parede; a luz de passagem continua por cima |
| 5 | A3 / colunas finas | Olhe as colunas de 1–2 tiles | Parecem **troncos** (casca com veio), não paredões de rocha; o piso embaixo delas continua terra (sem faixa escura) |
| 6 | Vinhas (A6, B4, C2…) | Escale | Casca coberta de hera (folhas verde-oliva com luar): lê como "dá para subir". O gato agarra como antes |
| 7 | A5, B4, C3, C5, G2 | Entre | **Concreto** reto com musgo por cima, marcas de forma, ferrugem e vergalhão no teto; a massa em volta continua a da floresta |
| 8 | C4 → D | Ande | Poeira roxa rara e alguns brotos da praga (roxo com bulbo verde que pulsa) começam a aparecer |
| 9 | C7, E1–E6, F1 | Entre | Chão da **praga** (musgo morto arroxeado, veias), céu com véu roxo, troncos do fundo com veias. **As plataformas continuam legíveis** contra o fundo |
| 10 | E1, E2, E3 | Suba as rampas | Rampas com a arte da praga (sem triângulo cinza); a colisão é a mesma |
| 11 | Chão de A e B | Passe correndo pelas samambaias | Curvam para o lado em que o gato passa |
| 12 | Fungos azuis (A–D) | Olhe | Pulsam devagar, fora de tempo; **nunca** parecem o turquesa do pogo (cacto, flor da planta) |
| 13 | A e B | Fique parado | Vaga-lumes rente ao chão; folhas caindo de vez em quando. No máximo 40 partículas na tela |
| 14 | Pogo e rachaduras | Compare | Turquesa do pogo e amarelo das rachaduras continuam as únicas cores assim na tela |
| 15 | Desempenho | Ande pela C (a maior cena) | Sem queda de quadros perceptível (a massa é um tilemap só; detalhes ~ 1 por tile de superfície) |
| 16 | Zonas de sombra (A1, A6, C1, C3, E1…) | Olhe cada esconderijo | Há uma **árvore no fundo** com a copa por cima da zona e o tronco dentro dela (ou um teto logo acima). O escuro tem a borda de cima recortada como a copa, sem pontilhado; o gato some na sombra como antes (mesmo retângulo de jogo) |
| 17 | Galhos de mão única, galho que balança, folha larga | Pise, desça (baixo + pulo) | Casca com musgo e folhas; os pés do gato ficam **na linha clara do topo** (não flutuam nem afundam). As pontas do galho afinam; nos galhos largos o meio repete sem emenda |
| 18 | Galho podre | Pise | Madeira cinza-amarronzada (não pedra); ao tremer, troca para as fendas abertas e o meio cede; volta inteiro em 5 s |
| 19 | Cogumelo saltador e cacto | Olhe e quique | Cogumelo: chapéu turquesa com a pata de gato clara. Cacto: bola de gomos com a **flor turquesa no topo** (pogo só ali), espinhos nos lados e embaixo, torrão de terra embaixo |
| 20 | Cenas Forest_Test (A, B, C) | Abra cada uma e rode o menu **20b. Floresta: juntar desenho e colisão** | O console diz "desenho novo (12 px de altura), colisão no mesmo lugar" para galhos, folhas e galhos podres; salve a cena. Nenhuma colisão sai do lugar |
| 21 | Saídas para baixo (A2 alçapão, A6 poço, C1, G3 → F) e para cima (A2, A6, C1, G3 topo) | Olhe o buraco | A luz fica **dentro do poço**, mais forte lá no fundo; o poço tem parede escura (nada transparente) e vinhas nas duas laterais |
| 22 | G3 (topo, saída para a estrada) | Da plataforma do alto, pule na vinha da raiz ao lado do buraco e suba | O gato agarra, sobe e sai pela passagem para a cabeceira leste |
| 23 | Vinhas | Compare com a Fortaleza | Mesmo desenho (dois ramos e folhas de hera com contorno), só mais escuro; continua lendo "dá para subir" |
| 24 | Esconderijos com árvore | Olhe o escuro | Sem cantos retos: arredondado, com bojos, borda suave e manchas de luar |
| 25 | Gancho, cipó do atalho, baú | Olhe, abra o atalho e o baú | Gancho com argola e corda; cipó trançado com folhas (enrolado antes de abrir); baú com cadeado dourado e, aberto, o brilho dentro |
| 26 | Atalhos (A2/C6 Ravina, A6/C4 Lago, B Patamar e Torre, C1/E4 Musgo, D Raiz) | Chegue ao lado que abre | **Sem clarão**: cipó = estaca com o nó e a laçada; alçapão = argola na tampa de raízes. Ao abrir: o cipó desce amarrado na estaca / a tampa fica em pé ao lado do buraco. Recarregar a cena mantém o estado |
| 27 | Fim da Fortaleza (x 240) → R1 | Rode o "Fortaleza: gerar a cena" (Fortaleza) e o 21; ande até a saída e volte | **Sem faixa preta**: a estrada continua depois do marco da trilha; a câmera para antes do fim. Na R1, o mesmo céu, a cordilheira e o chão de terra da Fortaleza: parece a mesma noite |
| 28 | Estrada (R1–R5) | Olhe para cima | Sem teto nem paredes no céu (a colisão continua: não dá para sair pelo alto); telhados de Miaravéu no horizonte |
| 29 | Tochas (R1, R2, R3) e fogueira (D5) | Olhe | Poste com a chama animada e um halo liso, sem pontilhado; nenhuma luz solta sem objeto |
| 30 | Saídas laterais da Floresta | Olhe a borda da sala | Marco da trilha com a placa apontando para fora e a lanterna acesa (no lugar do clarão) |
| 31 | R4 Ponte do Último Marco | Atravesse até o meio | Ponte de pedra com parapeito e arco; o meio desaba como antes (blocos caem com o desenho) |
| 32 | R1 posto abandonado | Olhe e se esconda dentro | Posto de madeira quebrado com o estandarte de Soluki; o telhado é a plataforma de antes; a sombra de dentro vem do telhado |
| 33 | Troncos rachados (B1 W05, C3 W10, …) e parede rachada dos cofres da Fortaleza | Olhe antes de quebrar; quebre | Fenda escura com brilho dourado; o que há atrás (baú, moedas, placa) **não aparece** até quebrar ou entrar; depois some a cobertura e não volta |
| 34 | Sombra com o Lion falando | Entre numa sombra quando ele fala | O texto continua claro (só o corpo vira silhueta) |
| 35 | Tutoriais | B1 (cactos), B2 (cogumelo), atalhos (B1 patamar, A2, C1, torre), D5 e D6 depois da Vulpis | O pergaminho mostra: "no ar, ↓ + ataque no cacto: quique", "pule no cogumelo", "atalho: abre do outro lado", "solte o cipó / abra o alçapão", "Caçador: o ataque atira flechas", "dash: passe por cima dos espinhos", "mire no gancho: flecha com corda", "no ar, dash de novo"; cada um com o ícone da tecla do aparelho em uso |
| 36 | Troncos rachados (B1 W05, C3 W10…) | Olhe e quebre com a marreta | Uma cabaninha: porta de madeira rachada com brilho dourado, telhado com musgo, paredes de tábuas; dentro, fundo de tábuas (coberto até quebrar) |
| 37 | Cabana da Vulpis (Refúgio → entrar) | Entre | Tábuas, vigas, cama de peles, mesa com vela, arco e aljava, peles secando, janela com a lua; nada roxo provisório |
| 38 | Primeira queda da ponte (A1) | Pise no meio da ponte | Os pedaços da ponte caem junto com o gato e ficam no chão; voltando à A1 depois, já estão lá |
| 39 | R4 | Antes e depois da ponte nova | A corda fica na beira do abismo, num galho da cabeceira; só aparece (e funciona) depois da ponte nova |
| 40 | Plantas carnívoras | Passe perto, de pé e agachado na sombra | Percebem bem mais rápido ("!" em ~0,4 s à vista), em volta delas; a parede ainda bloqueia |
| 41 | Golpe em desatento com as adagas | Ataque um Galho Seco/morcego/serpente que não te viu, de frente e pelas costas | De frente ×1,75; pelas costas ×3 (antes ×2,5). Marreta e arco: sem mudança. Templário zumbi nunca é pego de surpresa (como antes) |
| 42 | Sombras e inimigos | Perto de cada esconderijo | O inimigo de chão mais perto fica de costas para a sombra (o templário vira a ronda a 1,5 tile dela): dá para sair e atacar pelas costas |
| 43 | Tochas, névoa, pinheiros, chão | Ande pela estrada e pelas salas altas | Chama calma; névoa sempre embaixo da tela; pinheiros em camadas no fundo; chão e massa sem padrão repetido |

## O que ainda não é desta parte

Objetos das ruínas com segredo, placas e entradas de diário dos Primatas (F5 parte 2); ambiências e músicas da floresta e das ruínas (F5 parte 3). A estrada (Forest_Road) ganhou o chão da floresta, mas os tetos das salas dela ainda ficam soltos no céu (arte da estrada: resto da F3).
