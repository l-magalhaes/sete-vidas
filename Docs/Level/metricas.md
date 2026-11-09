# Métricas do jogador

Unidade: **tiles** (1 tile = 16 px = 1 unidade Unity). Preencher na **Fase 1**, medindo na cena `Playground` com os valores de `PlayerMovementSettings` (`Assets/_Game/ScriptableObjects/Player/`).

| Métrica | Configurado | Medido (tiles) | Folga de design | Medido em |
|---|---|---|---|---|
| Altura do pulo simples | 4 (`JumpHeight`) [PROPOSTA] | ? | desenhar até altura − 0,5 | |
| Altura alcançável agarrando borda | ≈ 5 (4 + patas a 1,25) [PROPOSTA] | ? | gate de pulo duplo ≥ 6 tiles | |
| Distância do pulo simples correndo | ≈ 4,6 (calculado) | ? | desenhar até distância − 1 | |
| Altura com pulo duplo | — | ? (pós-MVP) | | |
| Distância com dash | — | ? (pós-MVP) | | |
| Altura do gato em pé | 1,375 (22 px) [APROVADA] | — | | |
| Altura do gato agachado | 0,875 (14 px) [APROVADA] | — | passagem baixa = 1 tile | |
| Largura do gato | 0,75 (12 px) | — | vão mínimo = 1 tile | |
| Alcance de visão do inimigo (claro / escuro) | — | ? / ? (Fase 3) | | |

**Como medir:** suba nos degraus da Playground (1 a 6 tiles) e tente os buracos (2 a 5 tiles). O maior degrau e o maior buraco vencidos **com folga** viram os valores da coluna "Medido".

Com `JumpHeight = 4`, o esperado é: alcança o degrau de 4 tiles, não alcança o de 5, e vence todos os buracos (até 5 tiles). Se o pulo for alterado no Inspector, atualize a coluna "Configurado".
