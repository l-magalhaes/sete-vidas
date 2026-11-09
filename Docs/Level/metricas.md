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
| Alcance de visão do inimigo (claro / escuro) | — | ? / ? (Fase 3) | | |

**Como medir:** suba nos degraus da Playground (1 a 6 tiles) e tente os buracos (2 a 5 tiles). O maior degrau e o maior buraco vencidos **com folga** viram os valores da coluna "Medido".

Com `JumpHeight = 4`, o esperado é: alcança o degrau de 4 tiles, não alcança o de 5, e vence todos os buracos (até 5 tiles). Se o pulo for alterado no Inspector, atualize a coluna "Configurado".
