# Plano de teste — Melhorias 5 (14/11/2026)

Pedidos do usuário: (1) morcegos ainda percebiam o gato em cima do chão; (2) arco ainda forte; (3) terremoto do Paladino mata a serpente entocada; (4) ganchos com acesso real, orgânicos e onde se sobe; (5) rampa mais lenta para subir; (6) vãos maiores no Ninho (dash).

Antes: rode o menu **21** (`ForestLayout` versão **16**) e o **22**.

## Testes automáticos

- EditMode: `ForestHunterTests.Bow_FireRate_UpToTwoArrows`, `Bow_ArrowIs40PercentOfADaggerHit_CarryingTheRest`; `TempHeartSlideWheelTests.Uphill_IsSlower_SteeperIsSlowest`.
- Fora do Unity: `Tools/LevelPreview/checar_floresta.ps1` (tudo alcançável; o Ninho e o Coração agora contam dash e gancho) e **`Tools/LevelPreview/ganchos_floresta.ps1`** (novo: 49 ganchos, todos alcançáveis e numa quina onde o gato sobe).

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | 1 | Ande e pouse no chão logo acima de morcegos dormindo no teto da sala de baixo (o lugar do print) | Nenhum "?" nem "!": a parede bloqueia a visão e o som |
| 2 | 1 | Desça até a sala dos morcegos | Eles percebem rápido, como antes |
| 3 | 2 | Caçador com 3 corações num inimigo | Flechas 1, 1, 1, 1, 2 (40% da adaga); uma a cada 0,35 s; até 2 no ar |
| 4 | 3 | Paladino: terremoto (marreta carregada) perto de uma serpente no buraco | A serpente morre na hora (antes: "tink") |
| 5 | 3 | Queda com a marreta (o tremorzinho ao pousar) perto do buraco | Também mata |
| 6 | 3 | Golpe normal ou pogo na serpente entocada | Continua "tink" (só o tremor alcança dentro do buraco) |
| 7 | 5 | Suba uma rampa de 45° andando | Sobe, a 45% da velocidade; a de 1:2, mais rápido que essa; descendo e no plano, normal |
| 8 | 6 | E1: do fim da descida até a vinha do pilar | 6 tiles: só com dash (ou o gancho no alto do pilar). Errou: cai no chão com a planta e o templário |
| 9 | 6 | E2: do patamar 2 até a vinha da direita | 10 tiles: pulo + dash (o galho podre saiu) |
| 10 | 6 | E3: a subida pelos galhos (11 → 19 → 26) e a saída (8 adiante) | Cada vão pede dash |
| 11 | 6 | E5: zigue-zague | Vãos de 7 entre as colunas: pulo + dash a cada degrau; da chegada não se volta |
| 12 | 4 | Ganchos (lista abaixo): mire, puxe, pule | Cada um está na quina de um chão; pendurado, um pulo sobe nele; o chão leva ao que diz a lista |

## Onde estão os ganchos (e para quê)

- **A:** A1 raízes das moedas (2); A2 arco com a placa e moedas; A4 a prateleira (sem o pogo) e o nicho das moedas; A5 teto do vagão B (W03); A6 nicho do W04.
- **B:** B2 a saída e o nicho do W06; B3 plataforma do W07; B4 viga alta e o topo (alavanca do cipó); B5 galhos das moedas (2); B6 moedas do alto.
- **C:** C1 fugir do chão para a copa (2); C2 nicho do W09 e a volta para cima (3); C3 bordas dos andares (2); C4 a folha sobre o lago, que abre os galhos das moedas (antes ninguém alcançava); C5 galhos das moedas (2); C6 subir a Ravina de volta até a saída (3); C7 a volta até o pouso (2).
- **D:** D1 topo da árvore e a saída; D2 o galho largo da Vulpis; D3 três quinas (já existiam, corrigidas para a quina); D4 o galho das moedas; D5 até o galho do alto (moedas novas lá); D6 o do meio foi para a quina da laje 3 (a laje 2 cresceu 1 para alcançar).
- **E:** E1 alto do pilar; E2 galhos do alto (saída e alavanca); E3 o alto e a saída por baixo; E4 a saída da clareira.
- **G:** G1 o W19 e as moedas da plataforma.

Sem ganchos, de propósito: A3, B1 (desenhos do usuário), B7 (desafio de pogo), C4p e E4p (atalhos de mão única), E5 (sem volta), E6, F1 (arena), G2 e G3 (já se sobe tudo sem eles) e a Estrada.

## Riscos conhecidos

- Rampas mais lentas: os pulos encadeados que saem de rampa subindo (E1, E2) ficam mais curtos; o checador não modela a rampa lenta.
- O dash no checador é aproximado (6 tiles no chão ou no alto do pulo); vãos no limite podem ser difíceis no controle.
