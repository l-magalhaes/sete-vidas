# F7 (descartada) — A fuga do Ninho

> **Descartada em 17/11/2026** (o usuário: "vamos remover a perseguição, não está ficando legal"). No lugar dela, a Isyathy fica embaixo da terra e dá botes (ver `plano-implementacao.md`, F7). O código da fuga foi apagado; este plano fica só como registro.
>
> **Implementado em 17/11/2026 (o usuário pediu "implemente ela"), com as propostas da §8:** começa na E1, termina entalada na E4, morrer volta ao começo do trecho, música fica para a F10. Feito: F7a, a parte de despertar e fim da F7c, e da F7b a rota limpa (sem inimigos). Falta: E5–E6 com o rastro, conferência de tempo no `ForestReach`, arte definitiva (F7d). Roteiro: `Docs/QA/floresta-f7-plano-de-teste.md`.
>
> Status original: **[PROPOSTA]** (pedido do usuário, 17/11/2026: "ao entrar na sala, ela acorda e começa uma perseguição; se encostar, mata; temos que fugir rápido", com inspiração nas fugas de *Ori and the Will of the Wisps*). Os números são ponto de partida para o playtest.
> Substitui a furtividade da F7 (medidor de alerta e bote de busca, `floresta-mecanicas.md` §4) no Ninho.

## 1. O que aprender com as fugas do Ori

As fugas de *Ori* (a árvore Ginso no primeiro jogo; o verme de areia, a Mora e a fuga final no *Will of the Wisps*) funcionam por um conjunto de regras que vale copiar como **princípios**, sem copiar nada dos jogos:

| Princípio | Como fica no Sete Vidas |
|---|---|
| **Morre fácil, volta na hora.** Um toque mata, mas o jogo recomeça em menos de 1 s, perto de onde errou. Não há tela de morte. | Encostar na cabeça = morte instantânea (ignora os corações). Escurece 0,3 s e o gato volta ao começo do trecho. Sem perder nada e sem `WorldReset`. |
| **Uma rota só, legível em velocidade.** Nada de beco sem saída, escolha de caminho nem coleta no trajeto. | A rota da fuga é única. Baús e moedas saem do caminho (ou ficam para a volta, com ela já longe). |
| **Trechos curtos, um ponto de controle invisível por trecho.** | 3 trechos de 20–35 s (E1, E2, E3), cada um com o seu ponto de volta. |
| **O perseguidor estica e encolhe (rubber band).** Está sempre na borda da tela: quem joga bem ganha distância, mas ele nunca some. | A velocidade da cabeça depende da distância do gato (§4). |
| **Usa o que o jogador acabou de aprender.** | Dash e gancho do Caçador (o Ninho só abre depois do Portão do Templo), pogo no cacto, cogumelo, vinhas. |
| **O cenário desaba atrás.** Árvores caindo e chão quebrando dão urgência e impedem voltar. | Galhos e troncos quebram quando ela passa (o `FragileBranch` vira "desaba atrás"). |
| **A câmera olha para a frente.** | A câmera adianta 5–6 tiles no sentido da fuga e abre um pouco o zoom. |
| **A dificuldade sobe aos poucos e o fim é um alívio.** | O 1º trecho é generoso, o 3º pede precisão. A fuga termina numa passagem estreita onde ela fica entalada. |
| **Feita de propósito, quadro a quadro.** | Cada salto é testado na velocidade da fuga (alcance com tempo, §6). |

## 2. A sequência

### 2.1. O despertar (cutscene, ao entrar na E1)

1. O gato entra na E1 vindo da D4. O controle trava (`Cutscene`, que já existe).
2. A câmera recua e mostra a encosta do fundo: o que parecia um rio de raízes **respira** (D-NINHO-01, adaptado: "...Isso não é raiz.").
3. Folhas e escamas caem. Os olhos se abrem no fundo (`isy_olhos`), primeiro meio fechados, depois inteiros, com o verde da praga.
4. A cabeça se ergue entre as árvores (desenho novo: a cabeça subindo de perfil) e solta o rugido. A tela treme, e as árvores do fundo balançam.
5. Ela olha para o gato. O Lion: **"CORRE!"** (fala nova, curta).
6. A cabeça mergulha atrás do gato, do lado oeste (de onde ele veio). O controle volta e a fuga começa.

Duração: ~4 s. **Pulável a partir da segunda vez** (`AllowSkipAfterFirstTime`). Ao morrer na fuga, ela não se repete: o trecho recomeça com um rugido curto de 0,8 s.

### 2.2. A fuga (E1 → E2 → E3)

| Trecho | Sala | Direção | Ela vem | Ideia | Mecânicas |
|---|---|---|---|---|---|
| 1 | E1 Sulcos (60 × 25) | → leste | de trás (oeste), rente ao chão, rasgando a terra | Aprender a fugir: correr, pular os sulcos, um dash obrigatório perto do fim | rampas, pulo, 1 dash, cogumelo para subir à saída |
| 2 | E2 A Raiz que Respira (30 × 51) | ↑ subindo | **de baixo**, subindo pelo poço (como água que sobe) | Escalada sob pressão: vinhas, galhos que caem quando ela bate na parede, pogo no cacto | vinha, pulo de parede, pogo, gancho na quina |
| 3 | E3 Travessia das Escamas (60 × 34) | → leste | de trás, com **botes** avisados (os olhos piscam e ela avança 0,5 s depois) | O teste: plataformas temporárias, espinhos e o bote que pede dash na hora certa | dash, gancho, galho podre, espinhos |
| fim | E4 Clareira do Musgo | — | entala na entrada | A passagem de raízes é estreita: a cabeça bate, fica presa, ruge e recua. Alívio, santuário S-F4 | — |

Duração total: ~75–100 s para quem não erra.

### 2.3. O fim da fuga (cutscene curta, na E4)

A cabeça entala nas raízes da entrada da E4, tenta morder, recua e some no fundo. O Lion respira ("...Ainda tenho as sete? Tenho." ou uma fala a definir no roteiro). Grava a flag **`ninho_fuga`**. O S-F4 está ali: é o primeiro descanso depois da fuga.

### 2.4. Depois da fuga

- **E5 e E6 ficam quietas e tensas**: o rastro dela (árvores esmagadas, terra revirada), os olhos no fundo de vez em quando, sem perseguição. Elas levam à arena (F1), onde ela espera. A D-ISYATHY-00 muda de "despertar" para "ela me esperou".
- **Com a flag `ninho_fuga`**, entrar de novo na E1–E3 não dispara nada. As salas mostram a destruição e dá para andar com calma nos dois sentidos, inclusive para buscar os baús que saíram da rota (§5).
- **Morrer no chefe** volta ao S-F4 (E4), sem repetir a fuga.

## 3. Decisões aprovadas que esta mudança substitui

| Antes | Agora (proposta) |
|---|---|
| Fora da luta, cada ataque da Isyathy tira **meio coração**, sem morte instantânea [APROVADA 13/11] | Na fuga, **encostar mata** (pedido do usuário, 17/11). A volta é imediata e não custa nada |
| E5 "sem volta": a furtividade vai até a arena [APROVADA 13/11] | A fuga é na E1–E3; a E5–E6 é o caminho calmo até a arena |
| Medidor de alerta e bote de busca (F7 parte 1, código de 17/11) | Sai do Ninho. As regras ficam no código (`IsyathyRules`) e podem voltar em outro lugar. O `FragileBranch` é reaproveitado como "desaba atrás" |
| D-ISYATHY-00: o despertar na entrada da arena | O despertar passa para a E1; na arena, ela já está acordada |

## 4. O perseguidor (`IsyathyChase`)

**Corpo:** só a **cabeça e a frente do pescoço** têm colisão (caixa de ~3 × 2,5 tiles na boca). O resto do pescoço e o corpo ficam no fundo, sem colisão.

**Trilho:** ela anda por um caminho de pontos desenhado no `ForestLayout` para cada trecho (`ChasePath("E1", ...)`), não segue o gato livremente. Assim, a fuga é sempre a mesma e dá para ajustar cada salto.

**Velocidade (rubber band):** o gato corre a 7 tiles/s, e o dash leva 6 tiles em 0,2 s.

| Distância até o gato (pelo trilho) | Velocidade dela |
|---|---|
| mais de 14 tiles (fora da tela) | 9 tiles/s (alcança) |
| 4 a 14 tiles | 6,3 tiles/s (um pouco abaixo do gato) |
| menos de 4 tiles | 5,5 tiles/s (a "quase mordida": dá tempo de corrigir) |
| trecho 2 (vertical) | sobe a 3,5 tiles/s, com o mesmo esquema de distância |

Quem corre sem parar abre vantagem devagar. Quem para ou erra um pulo é alcançado em 1–2 s.

**Botes avisados (trecho 3):** em pontos marcados no trilho, os olhos piscam e ela recua um pouco; 0,5 s depois avança 5 tiles de uma vez. Para escapar, é preciso dar dash ou subir.

**Destruição:** ao passar, ela quebra os galhos frágeis, derruba árvores do primeiro plano (desenho caindo) e levanta terra. A tela treme de leve o tempo todo, com tremores maiores nos botes.

**Morte:** a cabeça encosta no gato → `PlayerHealth` com um caminho novo de morte instantânea (sem animação longa) → escurece 0,3 s → o gato volta ao ponto do trecho, e a cabeça volta para 10 tiles atrás dele, parada por 1 s → rugido curto → a fuga continua.

## 5. Level design: o que muda nas salas

As salas E1–E3 de hoje foram feitas para furtividade (sombras, desvios, inimigos patrulhando). Para a fuga:

- **Rota única** em cada sala, sem becos. O `checar_floresta.ps1` ganha uma conferência de fuga: a rota precisa existir e não pode ter pulo que exija parar (§6).
- **Sem inimigos na rota.** Templários, serpentes e plantas saem da E1–E3. Ou ficam como espetáculo: ela os esmaga ao passar.
- **Sem coleta na rota.** O W17 (E3) e o baú da E1 vão para desvios que só se alcançam depois da fuga (com `ninho_fuga`). O W18 (E5) continua onde está.
- **Sem armadilha de esperar** (pêndulo, serra com tempo). Os espinhos ficam, sempre com o pulo à vista.
- **Ganchos e cactos bem visíveis** e na linha da corrida (o gancho na quina do chão, como pede a regra dos ganchos).
- **Saída de cada sala sempre adiante e iluminada** (a luz na saída, regra de 13/11).
- **Trecho 2 (E2, vertical):** precisa de subida contínua. Hoje há patamares de descanso e o vão da vinha com o atalho `atalho_raiz`. A proposta é que o próprio pulo pela vinha seja a rota da fuga, e o atalho só seja usado depois.

## 6. Ferramentas e conferência

- **`ForestReach` com tempo:** para cada trecho, simular a rota na velocidade do gato e comparar com o trilho dela. Precisa sobrar pelo menos **1,5 s de folga** em cada ponto, para quem joga bem.
- **`sala_ascii.ps1`:** mostrar o trilho dela e os pontos de bote.
- **Depuração no Play:** uma tecla de editor que pula para o começo de cada trecho.

## 7. Etapas

| Etapa | O que | Pronto quando |
|---|---|---|
| **F7a** Perseguidor | `ChaseRules` (velocidade por distância, toque mata, pontos de volta, botes) + `IsyathyChase` (trilho, cabeça, morte instantânea, volta rápida). Testes EditMode | Na E1 atual, ela persegue pelo trilho e mata; a volta leva menos de 1 s |
| **F7b** Salas da fuga | Redesenho da E1–E3 no `ForestLayout` (rota única, sem inimigos e baús na rota, trilhos e botes). Conferência de tempo no `ForestReach`. `ForestLayout.Version` 27 | `checar_floresta.ps1` passa com folga ≥ 1,5 s |
| **F7c** Começo e fim | Cutscene do despertar (E1), fim entalada na E4, flag `ninho_fuga`, salas calmas depois, E5–E6 com o rastro. Falas no roteiro | A sequência inteira roda do D4 ao S-F4 |
| **F7d** Arte | Cabeça subindo (despertar, 3–4 quadros), cabeça perseguindo (boca abrindo e fechando, 3 quadros), bote, entalada; árvores caindo; terra levantando. Câmera adiantada | Prévia aprovada pelo usuário |
| **F7e** Limpeza | Tirar o medidor e o olho do HUD do Ninho; atualizar `floresta-mecanicas.md` §4, mapa §7 (E), decisões §4 do plano; roteiro de teste | Docs batem com o jogo |

**Som:** pela decisão de 14/11, todo som fica para a F10. Mas uma fuga sem música perde muito. **Sugestão:** adiantar só a música da fuga e o rugido para a F7 (a skill de sound design ajuda).

## 8. Em aberto (para o usuário decidir)

1. **Onde começa:** na E1, ao entrar no Ninho (proposta), ou na E5 "Olho no Fundo", mantendo a furtividade na E1–E4?
2. **Onde termina:** entalada na entrada da E4 (proposta, com alívio e santuário), ou a fuga vai até a arena e emenda na luta?
3. **O que acontece na morte:** volta ao começo do trecho (proposta, como no Ori), ou ao começo da fuga inteira?
4. **Adiantar a música da fuga** da F10 para a F7?
