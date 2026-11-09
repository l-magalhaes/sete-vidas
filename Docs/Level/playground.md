# Cena Playground (testes)

Cena de testes de movimento, gerada por `Tools > Sete Vidas > 2. Criar cenas`. Não é uma fase do jogo: ela é recriada sempre que o comando roda.

Coordenadas em células (1 célula = 1 tile). A superfície do chão fica em y = 0.

| Trecho | X (células) | O que testa | Fase |
|---|---|---|---|
| A. Spawn | -4 a 6 | Ponto `PlayerSpawn` em (0,5; 1) | 1 |
| B. Degraus | 8, 12, 16, 20, 24, 28 | Alturas de 1 a 6 tiles (2 de largura) para medir o pulo. Os de 1 e 2 são blocos maciços; os de 3 a 6 são plataformas de 1 tile de espessura, com passagem por baixo | 1 |
| C. Buracos | 36, 42, 49, 57 | Vãos de 2, 3, 4 e 5 tiles. O fundo fica só 2 tiles abaixo, então ninguém fica preso | 1 |
| D. Passagem baixa | 68 a 75 | Vão de 1 tile de altura: só passa agachado | 1 |
| E. Parede escalável | 80 a 81 | 10 tiles da layer `Climbable` e uma plataforma no topo (82 a 86, y = 9). Escalar até o topo e subir sobre ela | 2 |
| F. Chaminé | 92 a 104 | Duas paredes escaláveis frente a frente com vão de 3 tiles (x 94 a 96). Esquerda: x 92–93, do chão até o topo em y 7. Direita: x 97–98, de y 3 até o topo em y 11 (começa no ar). Plataforma de saída x 99–104 (topo em y 11). Treina subir, descer e saltar de uma parede para a outra | 2 |
| G. Bonecos de treino | 108 a 118 | Dois `TrainingDummy`: x 110,5 olhando para a direita e x 115,5 olhando para a esquerda. O gato atravessa os bonecos (trigger) para chegar às costas | 2 |
| H. Furtividade | 122 a 151 | Sentinela patrulhando x 133–140 (pausa de 1,5 s em cada ponta). Matinho em x 137–139 (e outro de teste em x 105–107, antes dos bonecos). Sombra A em x 122–130 e sombra B em x 144–152 (y 0 a 5, exposição 0,3). Rota por cima: parede escalável x 128–129 (dentro da sombra A, topo em y 5) e passarela x 130–143 (topo em y 5), que bloqueia a visão da Sentinela. Embaixo da passarela há 4 tiles livres | 3 |
| Santuários (Fase 4) | 3 e 119,5 | `playground_inicio` (perto do spawn) e `playground_furtividade` (antes da Sentinela). E para descansar e salvar | 4 |
| I. Paladino | 154 a 177 | Túnel embaixo de uma laje (x 156–220, y 4, topo em 5; o túnel termina em x 177). Parede rachada em x 160 fecha o túnel (vista antes da marreta). A laje, alcançada agarrando a borda de 5 tiles em x 156, leva à arena. Atrás da parede: placa "Atalho secreto!" | 5 |
| J. Chefão | 172 a 218 | Santuário em x 172 (em cima da laje). Arena x 180–210 (chão em 5, teto em 14) com grades em x 179 (entrada) e x 211 (sala do tomo). Golden em x 200. Sala do tomo x 213–218, tomo em x 216,5 | Chefão |
| Limites | -6/-5 e 221/222 | Paredes sólidas nas pontas | — |

**Checagem anti-softlock:** os degraus de 3 a 6 são plataformas suspensas, então o chão abaixo deles fica livre: não existe vão entre pilares onde o gato fique preso, e o caminho de volta ao spawn nunca exige pulo maior que 2 tiles. (A versão anterior, com pilares maciços, prendia o gato nos vãos entre os pilares de 4, 5 e 6 tiles.) Todo buraco tem fundo raso e pode ser escapado com um pulo de mais de 2 tiles. A passagem baixa tem saída dos dois lados. A plataforma da parede escalável permite descer caindo. Na chaminé, a parede direita começa a 3 tiles do chão: dá para passar andando por baixo dela, e a plataforma de saída (topo em y 11) desce caindo para o chão. Os bonecos são triggers, então nunca bloqueiam o caminho. No trecho H todo o chão é contínuo: a passarela é opcional (sobe pela parede escalável e desce caindo ou pendurando na ponta), e o gato atravessa a Sentinela, que nunca o prende. Sem habilidade nova, sempre há volta ao spawn. Nenhum trecho exige uma habilidade além de andar, pular e agachar para sair.

Tilemaps: `Background` (sem colisão, ordem -10) · `Ground` (layer Ground) · `Climbable` (layer Climbable) · `Foreground` (ordem 10).

## Trecho H — ficha da sala (Fase 3)
Propósito: ensinar furtividade | Tamanho: 30 × 6 tiles | Requisitos: andar, agachar, escalar
- **Solução furtiva 1 (sombra + costas):** esperar agachado na sombra A enquanto a Sentinela anda para a direita; segui-la agachado (sem ruído) e golpear pelas costas: 5 FURTIVO derruba (5 de vida).
- **Solução furtiva 2 (por cima):** escalar a parede x 128–129 dentro da sombra, atravessar a passarela (a Sentinela não vê através dela) e descer **pendurando na ponta** (3,75 tiles, silencioso). Cair direto (5 tiles) faz barulho e chama a Sentinela.
- **Solução furtiva 3 (matinho):** esperar agachado no matinho de x 137–139, no meio da patrulha; a Sentinela passa ao lado sem ver, e o gato sai atrás dela para o golpe furtivo. De pé no matinho, ela vê.
- **Solução de combate:** lutar de frente (3 golpes) desviando da estocada (aviso de 0,45 s).
- **Patrulha legível:** 7 tiles a 2 tiles/s = 3,5 s por perna + 1,5 s de pausa = ciclo de 10 s.
- **Risco de softlock:** nenhum.

## Trecho I — ficha da sala (Fase 5)
Propósito: ensinar o Paladino (gate visível → vínculo → volta) | Tamanho: 24 × 6 tiles | Requisitos: andar, pular, agarrar borda
- **Ver antes de poder:** o caminho natural pelo chão entra no túnel e bate na parede rachada (adagas: "tink").
- **Vínculo:** a laje tem topo em 5 tiles: o gato agarra a borda em x 156 e sobe. O Golden está lá.
- **Volta e quebra:** do Golden, desce e volta ao túnel (4 tiles) com o Paladino: duas marretadas abrem a passagem.
- **Risco de softlock:** nenhum. O túnel e a laje se acessam pelo chão contínuo; a parede só bloqueia uma área opcional.
- **Sem atalho indevido:** a laje vai até o limite direito (x 177). Uma versão anterior parava em x 176 e deixava uma fresta de 1 tile por onde dava para cair atrás da parede sem a marreta.

## Trecho J — ficha da sala (chefão)
Propósito: chefão (prova do Ladino) + sala do tomo (história) | Tamanho: arena 31 × 9 tiles | Requisitos: andar, pular, agarrar borda
- **Entrada:** pela laje, com santuário logo antes (x 172). Entrar fecha a grade de trás; morrer reinicia a arena e reabre a grade.
- **Arena plana e larga** (31 tiles): espaço para passar por baixo do salto e para a faixa segura dos relâmpagos (≥ 2,5 tiles). Teto em 14: o salto (5 tiles acima do chão em 5) não bate no teto.
- **Saída:** a grade da direita só abre com o Golden derrotado; a sala do tomo é um beco sem perigo.
- **Risco de softlock:** nenhum. Depois da vitória as duas grades ficam abertas para sempre (flag no save); o caminho de volta é a laje e o chão.
- **Golden fora da laje:** a Fase 5 tinha o Golden parado em cima da laje (x 168,5); agora ele é o chefão da arena.
