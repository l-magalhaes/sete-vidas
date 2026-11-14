# Plano de teste — Melhorias 3 (14/11/2026)

Pedidos do usuário: (1) coração fantasma; (2) troncos que dava para pular; (3) transições estranhas; (4) fundo vazio da D1; (5) mira do arco; (6) alerta e detecção nos inimigos da Floresta.

Antes: rode o menu **21** (`ForestLayout` versão **14**) e o **22** (mapa).

## Testes automáticos (EditMode)

- `ForestHunterTests`: `Bow_FasterFire_UpToThreeArrows`, `Bow_AimSnapsTo15Degrees_From45DownToStraightUp`, `Bow_AimAssist_NearestInFront_NoLockBehind`.
- `ForestBestiaryTests`: `Senses_DefaultIsTheSentinels_BatIsMuchFaster_AndSeesAround`, `Senses_ShadowsAndDistanceSlowTheMeter`.

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | 1 | Abra o W13 | O coração temporário é um **fantasminha** (cabeça de coração, olhos, barra ondulada), meio transparente, flutuando 1 px |
| 2 | 1 | Leve um golpe | O fantasma fica pela metade (lado direito apagado) e depois some |
| 3 | 2 | C3 (escritório do W10) e E3 (por baixo, W17) | O tronco vai do chão ao teto: não dá para pular por cima; marreta quebra (3 golpes) |
| 4 | 3 | Saia por uma porta lateral (ex.: A1 → A2) | O gato sai andando reto, sem cair no vazio enquanto escurece |
| 5 | 3 | Desça por um buraco no chão (ex.: poço do Lago) | Desce devagar ~2 tiles e escurece rápido (0,3 s); sem pose de dano |
| 6 | 3 | Suba por um buraco no teto | Continua subindo enquanto escurece (não cai de volta) |
| 7 | 4 | D1, fundo do poço oeste (embaixo das flechas cravadas) | 3 moedas e o baú **W20**: só "+1 coração temporário" (sem madeira) |
| 8 | 5 | Caçador: atire sem apontar | Reto para a frente; com um inimigo na frente (até ±40°), a flecha vai nele |
| 9 | 5 | Segure cima / diagonal e atire | 90° / 45°; com o analógico, de 15 em 15°; linha pontilhada curta mostra a mira |
| 10 | 5 | Segure baixo no chão e atire | 45° para baixo. No ar, baixo + ataque = estocada das adagas (pogo) |
| 11 | 5 | Inimigo nas costas ou atrás da parede | A mira assistida ignora |
| 12 | 5 | Aperte ataque rápido | Até 3 flechas no ar, uma a cada 0,2 s |
| 13 | 6 | Chegue perto de um Galho Seco acordado pela frente | "?" enchendo, depois "!" e só então o golpe |
| 14 | 6 | Mesmo Galho, agachado na sombra | Demora bem mais ou nem percebe |
| 15 | 6 | Galho disfarçado | Sem ícone até acordar |
| 16 | 6 | Morcego dormindo: passe perto | "!" quase na hora (bem mais rápido que os outros), em qualquer direção |
| 17 | 6 | Planta na parede: passe atrás dela / atrás de um bloco | Não vê |
| 18 | 6 | Fuja e se esconda | "!" vira "?" depois de 2 s e some |
| 19 | 6 | Acerte um bicho desatento | "!" na hora |

## Riscos conhecidos

- **Ataque da Planta e da Serpente ficou mais lento**: agora esperam o "!" (~0,6 s de perto). Pode ficar fácil passar correndo; ajustar `ForestSenseRules` se for o caso.
- **Arena do D3**: os bichos das levas precisam ver o gato antes de atacar (na arena isso é rápido).
- **Troncos esticados**: o desenho de 16×64 foi esticado na vertical; arte própria depois.
