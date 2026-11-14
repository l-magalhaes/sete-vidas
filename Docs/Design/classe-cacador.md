# Classe Caçador (F4b)

> Dada pela **Vulpis** no Posto da Vulpis (D5), depois da arena do Refúgio. Status: **[APROVADO]** (pedido do usuário, 14/11/2026); números **[PROPOSTA]**, a balancear no playtest.
> Código: `HunterRules` (regras puras), `DashAbility`, `GrappleAbility`, `BowAbility`, `PlayerArrow`. Testes: `ForestHunterTests`.

## Vínculo

- `CompanionBond` da Vulpis: flag `vinculo_vulpis`, classe `Hunter`, **+1 coração** (o 3º depois da maldição).
- O coração mostra a **flecha atravessada** (`ClassHeartPixels`). Os corações seguem a **ordem em que as classes foram ganhas** (`HeartsHud.HeartClass` com `unlockedClasses`): Ladino, Paladino, Caçador...
- **Ordem das classes:** o GDD lista as classes sem ordem; a ordem vai sendo definida no desenvolvimento. O Caçador é a 3ª classe ganha (depois do Ladino e do Paladino).

## Onde vale cada coisa

| | Classe |
|---|---|
| Arco | só com o Caçador ativo |
| Dash | qualquer classe, depois do vínculo |
| Flecha com corda | qualquer classe, depois do vínculo |

(Regra do GDD: locomoção vale em qualquer classe; decisão do usuário de 14/11/2026.)

## Arco

| | Valor |
|---|---|
| Botão | Ataque (clique / X) com o Caçador, na direção do direcional / analógico |
| Mira (melhorias 3) | De 15 em 15°, de 45° para baixo até reto para cima (como a besta dos atiradores); sem apontar, reto. Linha pontilhada curta ao apontar na diagonal ou para cima |
| Mira assistida | Sem apontar: o inimigo mais perto na frente, em ±40°; apontando: corrige até ±20°. Inimigo atrás da parede ou nas costas não conta. Não trava |
| Dano (melhorias 5) | **40% de um golpe de adaga** (mínimo 1), já com o bônus dos corações; a fração que sobra passa para a próxima flecha (adaga 3 → flechas 1, 1, 1, 1, 2…) |
| Velocidade / alcance | 22 tiles/s / 12 tiles |
| Intervalo | 0,35 s; até 2 flechas no ar (melhorias 3 tinha subido para 0,2 s e 3; voltou por estar forte) |
| Barulho | 0,15 (raio 2,5): o arco é silencioso |
| Para baixo | {agachar} + ataque no ar continua a **estocada das adagas** (pogo): o Caçador não perde o pogo |

A flecha acerta o primeiro inimigo do caminho, crava na parede e some. Não quebra parede rachada nem tronco.

## Dash

| | Valor |
|---|---|
| Botão | Dash (**Shift** / gatilho direito) |
| Distância | **6 tiles em 0,2 s** (generoso); sem gravidade durante o avanço |
| No chão | intervalo de 0,3 s entre dashes |
| No ar | **um** por vez; volta ao tocar o chão, agarrar parede/borda, chegar num gancho ou quicar num pogo |
| Fim | sobra 45% da velocidade (não para seco no ar); encostou na parede, acaba ali |
| Dano | sem invencibilidade (fica para um upgrade) |
| Barulho | 0,2 (raio 3) |

## Flecha com corda (gancho)

| | Valor |
|---|---|
| Botão | Special (**botão direito do mouse** / **Y**) |
| Mira | direcional em 8 direções (parado: para a frente; no chão, "para baixo" vira para a frente) + **mira automática**: o gancho mais alinhado num cone de 30° |
| Alcance | 9 tiles; não atravessa parede |
| Sequência | a flecha voa até o gancho (40 tiles/s, o gato espera no ar) → **puxão** a 20 tiles/s → **pendurado** 0,4 s |
| Pendurado | {pular}: pulo cheio (com o lado apertado); {dash}: solta e avança; sem nada, cai |
| Errou | sem gancho na mira: a corda vai até a parede (ou o alcance) e volta; o gato segue livre |
| Travou | o puxão para de avançar (quina, teto) por 0,12 s: solta |

Pegar o gancho devolve o dash do ar: gancho → pendurado → dash é o combo da D6.

## Ganchos já postos na Floresta

D3 (3) e E1–E3 (pensados para "a volta com o Caçador") ficam usáveis **antes** do Ninho. Pendente: revisar se algum vira atalho que pula trecho do E (o `ForestReach` ainda não conhece dash e gancho).
