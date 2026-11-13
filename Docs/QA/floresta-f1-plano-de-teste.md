# Plano de teste — Floresta F1 (base técnica)

Antes: rode **Tools > Sete Vidas > 20. Floresta F1: cenas de teste (A e B)** (gera as cenas A, B, C e o interior da cabana). A cena `Scenes/World/Forest/Forest_Test_A` abre sozinha: aperte Play nela.
As cenas de teste deixam o save como o de quem saiu da Fortaleza: **Paladino liberado e 2 corações** (`ForestTestBootstrap`). Elas usam o **save da área de testes** (o mesmo da Playground), nunca o do jogo.
Para o Play começar na cena aberta, desmarque **Tools > Sete Vidas > Play começa na tela inicial**.
Para repetir do zero, apague só `save_playground.json` (**Tools > Sete Vidas > Save > Abrir pasta do save**). Cuidado: **Save > Apagar save** apaga também o save do jogo.
Arte provisória (blocos e sprites simples). O que está em teste é o comportamento.

## Testes automáticos

Test Runner > EditMode > `ForestFoundationTests` (40 testes) e `ProjectSetupTests` (a layer **Platform** no índice 15).

## Roteiro manual

| # | Passo | Esperado |
|---|---|---|
| 1 | Encoste no santuário da esquerda (Clareira de teste) e aperte E | Descansa (só há um santuário visitado: sem menu) |
| 2 | Ande até o tronco rachado e bata com a **adaga** (Ladino) | Faísca e "tink"; o tronco não perde vida |
| 3 | Troque para o **Paladino** e bata 3 vezes com a marreta | Lascas de madeira, som grave; quebra no 3º golpe |
| 4 | Abra o baú W01 (E) | Tampa abre, aviso "Madeira de Reconstrução (1/12)"; inventário (Tab) > Itens: "Madeira de Reconstrução 1/12" |
| 5 | Entre no poço: pise no **cogumelo** | Quica ~6 tiles sem atacar; o chapéu amassa |
| 6 | Suba pelos **galhos de mão única** pulando por baixo deles | Atravessa por baixo e para em cima; não bate a cabeça |
| 7 | Em cima de um galho: **baixo + pulo** | Cai através do galho (não pula) |
| 8 | No chão sólido: baixo + pulo | Pulo agachado normal (não atravessa o chão) |
| 9 | Fique na **folha larga** | Afunda devagar (até 3 tiles); ao sair, ela volta |
| 10 | Pise no **galho que balança** | Afunda um pouquinho e volta |
| 11 | Pise no **galho podre** | Treme, cai em ~0,6 s e volta em 5 s. Na Floresta, o Lion pode reclamar num balão ("Ah, qual é. Até os galhos?") |
| 12 | Caia de 10 tiles ou mais (do topo do poço até o chão) | Balão curto do Lion sobre a cabeça ("Caí de pé. Claro." etc.), no máximo um por minuto |
| 13 | Caia por mais de 1 s; depois, agachado e parado no chão por 1 s | A câmera desce (antecipa o pouso / mostra embaixo) e volta ao normal |
| 14 | No alto do poço, como **Ladino**: pule e ataque para baixo (baixo + ataque, no ar) no **cacto** do lugar do galho 7 | Quica alto (~7 tiles) e alcança o patamar do baú e a borda da direita. Encostar no cacto sem atacar: perde meio coração e é empurrado |
| 14b | Abra o baú W02 no topo do poço | Madeira 2/12 e +5 moedas |
| 14c | Atravesse a borda para o lado dos cactos e desça quicando nos 3 cactos (e, se cair, nos espinhos do chão) até a plataforma alta e o corredor | Cada estocada para baixo quica; cair nos espinhos sem atacar tira meio coração e volta ao último chão seguro. Com o Paladino a marreta não quica (queda pesada) |
| 15 | Pelo corredor no alto, vá até a porta da direita | Escurece, carrega a **Forest_Test_B**, o gato aparece na chegada olhando para a direita, **com a mesma vida** |
| 16 | Na B, encoste no santuário "Vale de teste" e aperte E | Abre o **menu do santuário**: Descansar / Viajar: Clareira de teste |
| 17 | Escolha **Viajar: Clareira de teste** | Fade, carrega a A, o gato aparece de pé no santuário da A, de vida cheia |
| 18 | Volte para a B pela porta | O **tronco da A continua quebrado** e os **baús continuam abertos** |
| 19 | Na B, quebre o tronco e abra o baú W03; ande até o fundo do corredor | A porta interna leva para o patamar alto **sem trocar de cena** |
| 20 | Desça pelos galhos de mão única | Baixo + pulo funciona em cada um |
| 21 | Descanse na B, volte para a A e morra (encostar nos cactos até acabar a vida, ou "Depuração: tomar 1 hit" no PlayerHealth) | Renasce na **B** (o último santuário de descanso é de outra cena) |
| 22 | Feche o Play e aperte Play na A de novo (ou Continuar no menu) | Tronco quebrado, baús abertos, madeira 3/12, destinos da viagem lembrados |
| 23 | Envenenado (se houver zumbi por perto) ou durante a troca de cena, aperte E no santuário | Sem viagem: só descansa |

## Mapa, cena B e sala-hub (cena C) — redesenho do usuário de 13/11/2026

| # | Passo | Esperado |
|---|---|---|
| 24 | Ande pelas salas e abra o inventário (Tab) > aba **Mapa** | Salas vistas (blocos verdes), santuário (âmbar), baús (claro = fechado, escuro = aberto), o gato piscando. A B aparece à direita da A |
| 25 | Na **B**, suba pelos cactos (pogo como Ladino) até o abrigo no alto à esquerda | Dois **Galhos Secos**: parados como troncos até você chegar perto; levantam o braço (aviso) e batem no chão. Com o braço preso no chão: pogo e dano dobrado pelas costas |
| 26 | No abrigo, quebre o tronco (Paladino) e abra o baú W03 | Madeira +1 |
| 27 | **Plantas Carnívoras** (chão e parede da direita) | Abrem a boca (aviso), mordem quem está na frente; no recuo (cabeça baixa, flor turquesa) dá para quicar |
| 28 | Canto de baixo à direita da B: suba o degrau até a plataforma | 5 moedas |
| 29 | Pelo patamar alto (com mais 2 Galhos Secos), vá para a **C** | Carrega a Encruzilhada |
| 30 | Na C, escale o **pilar e a parede de vinhas** (chaminé de 3: escalada e pulo de parede) | Chega ao galho da entrada da arena e, no alto, ao galho grosso da cabana |
| 31 | Entre na **arena** | Os portões fecham; os inimigos são 3 Plantas Carnívoras. Morrer reabre; derrotadas todas, "Arena vencida" e o baú W04 depois dela |
| 32 | Embaixo à direita (passando por baixo da arena) | Santuário da Encruzilhada, uma planta presa na parede e uma moeda |
| 33 | Tente entrar na **cabana da Vulpis** (porta no galho grosso, à esquerda) | "Trancada. Deve ter uma chave por aí." |
| 34 | No alto (em cima da arena), passe pelos 2 Galhos Secos e pegue a **chave** no canto direito | Aviso "Chave da cabana"; inventário > Itens |
| 35 | Volte à cabana e aperte E | Destranca, escurece e **entra numa cena pequena**: o interior da cabana, com o baú W05 e uma moeda na prateleira. A chave some do inventário |
| 36 | Dentro, aperte E na porta | Volta para fora, na frente da cabana |
| 37 | No alto, suba a plataforma da direita e a parede de vinhas até o buraco do teto | **Saída por cima** (por enquanto leva de volta para a A) |

## O que fica para depois (não é bug)

- Sons próprios da Floresta (o tronco usa o som da parede, mais grave; o baú usa o da moeda).
- Sem música nas cenas de teste.

## Inimigos ajustados, morcegos, ganchos e a cabana (13/11/2026)

| # | Passo | Esperado |
|---|---|---|
| 38 | Chegue perto de um **Galho Seco** (abrigo ou patamar da B, alto da C) | Ele não anda; é alto: não dá para pular por cima; esbarra nele. Derrube (braço preso: pogo ou golpe pelas costas) para passar |
| 39 | Fique perto de um **Broto** (patamar da B, à direita; alto da C) | Das folhas saem morcegos (até 2) que perseguem o gato |
| 40 | Chegue a uns 6 tiles de uma **Planta** | Ela abre a boca e a cabeça vai até você com o pescoço esticado; na volta, dá para quicar na cabeça |
| 41 | No hub da C, passe embaixo do galho grosso (ou faça barulho) | 3 **morcegos** acordam e perseguem; agachado e devagar, dá para passar por baixo sem acordar |
| 42 | No alto à esquerda da C (acima da cabana) | Duas paredes penduradas com **ganchos** brilhando na ponta e um beiral com o baú W06 e 3 moedas, **fora de alcance** (para a classe Arqueiro, no futuro) |
| 43 | Entre na cabana | O Lion comenta: "Vazia." / "Arco na parede, flechas no chão, peles secando..." / "Algum caçador mora aqui. E não está em casa." (só na primeira vez) |

## Redesenho da C e armadilhas (13/11/2026, segunda leva)

| # | Passo | Esperado |
|---|---|---|
| 44 | Acorde um morcego perto de uma parede ou de uma plataforma | Ele **não atravessa** chão, parede, teto nem galho: desliza pela superfície |
| 45 | Hub da C, lado esquerdo | Três plataformas com **ganchos** na ponta (Arqueiro, futuro) e morcegos embaixo; os 2 de baixo já acordam quando você entra. Só a do meio se alcança hoje (pilar de vinhas) |
| 46 | A cabana | Agora é uma casa desenhada (fachada atrás do gato, telhado) em volta da porta |
| 47 | Pela passagem de baixo do hub, vá para a direita | Planta logo na entrada; **lâminas pendulares** penduradas de massas de terra; degrau de terra; **fosso de espinhos** (atravesse quicando neles); degraus e morro |
| 48 | Do outro lado, pise no **botão azul** (perto do santuário) | "Clique. Melhor assim."; as lâminas e os espinhos somem para sempre (para a volta) |
| 49 | No canto de baixo à direita (teto baixo) | Planta pendurada do teto, a planta da parede e a **chave da cabana** no chão |
| 50 | No alto da C, canto direito | Onde era a chave agora há o baú W07 |
