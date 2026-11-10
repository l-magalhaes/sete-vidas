# Métricas do jogador

Unidade: **tiles** (1 tile = 16 px = 1 unidade Unity). Preencher na **Fase 1**, medindo na cena `Playground` com os valores de `PlayerMovementSettings` (`Assets/_Game/ScriptableObjects/Player/`).

| Métrica | Configurado | Medido (tiles) | Folga de design | Medido em |
|---|---|---|---|---|
| Altura do pulo simples | 4 (`JumpHeight`) [APROVADA] | **4 no limite** (pés chegam ao topo do degrau de 4 no ápice) | **degraus de até 3 tiles** (4 só como desafio) | Playground, 09/11/2026 |
| Altura alcançável agarrando borda | ≈ 5 (4 + patas a 1,25) [APROVADA] | **5** (agarra o degrau de 5, não alcança o de 6) | gate de pulo duplo ≥ 6 tiles ✔ | Playground, 09/11/2026 |
| Distância do pulo simples correndo | ≈ 4,6 (calculado) | **5 no limite** (buraco de 5 vencido na risca; acima disso agarra a borda ou cai) | **buracos de até 4 tiles** | Playground, 09/11/2026 |
| Altura com pulo duplo | — | ? (pós-MVP) | | |
| Distância com dash | — | ? (pós-MVP) | | |
| Velocidade escalando (sobe / desce) | 4 / 6 tiles/s (`WallClimbSettings`) [PROPOSTA] | ? | parede de 10 tiles ≈ 2,5 s para subir | |
| Pulo da parede (altura / alcance horizontal) | 2,5 tiles / vão de 3 tiles [PROPOSTA] | ? | chaminés com vão de até 3 tiles | |
| Altura do gato em pé | 1,375 (22 px) [APROVADA] | — | | |
| Altura do gato agachado | 0,875 (14 px) [APROVADA] | — | passagem baixa = 1 tile | |
| Largura do gato | 0,75 (12 px) | — | vão mínimo = 1 tile | |
| Rampa andável | até 46° (`MaxSlopeAngle`): **1:1 e 1:2** | ? | 2:1 ou mais íngreme = parede (escorrega). Sem patrulha de inimigo em rampa | Playground, trecho L |
| Pulo partindo de rampa | igual ao plano (4) | ? | conte a altura a partir do ponto da rampa onde o gato pula | Playground, trecho L |
| Alcance de visão da Sentinela (luz em pé / luz agachado / sombra em pé / sombra agachado) | 8 / 4,8 / 2,4 / 1,4 tiles, cone de ±35° [PROPOSTA] | ? | esconderijos de sombra a ≥ 2 tiles da rota da patrulha | |
| Queda que faz barulho | ≥ 4,5 tiles (`StealthSettings`) [PROPOSTA] | ? | plataformas de 5 sobre rotas de patrulha: pendurar e soltar é silencioso | |
| Velocidades da Sentinela (patrulha / investigação / perseguição) | 2 / 3 / 4,5 tiles/s [PROPOSTA] | — | patrulhas de 4–8 s por perna | |

**Como medir:** suba nos degraus da Playground (1 a 6 tiles) e tente os buracos (2 a 5 tiles). O maior degrau e o maior buraco vencidos **com folga** viram os valores da coluna "Medido".

Com `JumpHeight = 4`, o esperado é: alcança o degrau de 4 tiles, não alcança o de 5, e vence todos os buracos (até 5 tiles). Se o pulo for alterado no Inspector, atualize a coluna "Configurado".
