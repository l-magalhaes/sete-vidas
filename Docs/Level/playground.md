# Cena Playground (testes)

Cena de testes de movimento, gerada por `Tools > Sete Vidas > 2. Criar cenas`. Não é uma fase do jogo: ela é recriada sempre que o comando roda.

Coordenadas em células (1 célula = 1 tile). A superfície do chão fica em y = 0.

| Trecho | X (células) | O que testa | Fase |
|---|---|---|---|
| A. Spawn | -4 a 6 | Ponto `PlayerSpawn` em (0,5; 1) | 1 |
| B. Degraus | 8, 12, 16, 20, 24, 28 | Alturas de 1 a 6 tiles (2 de largura) para medir o pulo. Os de 1 e 2 são blocos maciços; os de 3 a 6 são plataformas de 1 tile de espessura, com passagem por baixo | 1 |
| C. Buracos | 36, 42, 49, 57 | Vãos de 2, 3, 4 e 5 tiles. O fundo fica só 2 tiles abaixo, então ninguém fica preso | 1 |
| D. Passagem baixa | 68 a 75 | Vão de 1 tile de altura: só passa agachado | 1 |
| E. Parede escalável | 80 a 81 | 10 tiles da layer `Climbable` e uma plataforma no topo (82 a 86, y = 9) | 2 |
| Limites | -6/-5 e 90/91 | Paredes sólidas nas pontas | — |

**Checagem anti-softlock:** os degraus de 3 a 6 são plataformas suspensas, então o chão abaixo deles fica livre: não existe vão entre pilares onde o gato fique preso, e o caminho de volta ao spawn nunca exige pulo maior que 2 tiles. (A versão anterior, com pilares maciços, prendia o gato nos vãos entre os pilares de 4, 5 e 6 tiles.) Todo buraco tem fundo raso e pode ser escapado com um pulo de mais de 2 tiles. A passagem baixa tem saída dos dois lados. A plataforma da parede escalável permite descer caindo. Nenhum trecho exige uma habilidade além de andar, pular e agachar para sair.

Tilemaps: `Background` (sem colisão, ordem -10) · `Ground` (layer Ground) · `Climbable` (layer Climbable) · `Foreground` (ordem 10).
