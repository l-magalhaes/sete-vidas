# Chefão: Golden, guardião do tomo
**Pilar:** 1 (sete corações, sete classes) e 3 (combate) | **Escopo:** MVP | **Status:** rascunho para teste

## História (decisão de 09/11/2026)
O Ladino começa com **as 7 vidas**. O Golden, paladino da Ordem, protege o tomo selado. O gato o derrota na arena, pega o tomo e o selo se rompe: **seis vidas se apagam** (sobra 1, quase vazia). O Golden, que reconhece a força de quem aguentou o selo, salva o gato e dá a **classe Paladino em troca do juramento de destruir o tomo**. Esse vínculo devolve o 2º coração.

## Objetivo de experiência
Uma luta de leitura: cada golpe do Golden tem um aviso claro e uma resposta clara. O jogador aprende a **passar por baixo** do salto, a **punir** a cura e a **procurar o lugar seguro** nos relâmpagos.

## Fluxo
1. Da laje do trecho I, o gato segue para a direita até a **arena** (x 180–210). Santuário antes, em x 172.
2. Ao entrar, a **grade de trás fecha** e o Golden fala (D-GOLDEN-00). A luta começa ao fim da conversa.
3. **Vitória:** o Golden ajoelha, as duas grades abrem e a vitória vai para o save (`chefe_golden_derrotado`). Conversa D-GOLDEN-DERROTA.
4. **Sala do tomo** (x 213–218): E pega o tomo (D-TOMO-01) → clarão roxo → **corações se apagam um a um (7 → 1)** → o gato cai → D-TOMO-02 → tela escurece → o Golden aparece ao lado → **juramento** (D-GOLDEN-01) → **+1 coração (2) e Paladino** → dica de troca de classe.
5. **Morrer na luta** (ou descansar): a arena reinicia (grade aberta, Golden com vida cheia).
6. Estado recuperável: fechar o jogo no meio da cena do tomo deixa o Golden de pé ao lado do pedestal; falar com ele faz o juramento.

## Golpes
| Golpe | Aviso | Como escapar / punir | Dano |
|---|---|---|---|
| **Marretada** (perto, até 1,8 tile) | Marreta erguida atrás da cabeça, 0,6 s | Recuar; atacar na recuperação (0,7 s) | **1 hit** (meio coração) |
| **Salto com marretada** | Agacha com a marreta no alto, 0,5 s; salta 5 tiles de altura até onde o gato estava | **Passar por baixo** dele para o outro lado; atacar quando ele cai (0,8 s parado) | **4 hits** (2 corações; área de 3,2 tiles) |
| **Cura concentrada** (só abaixo de 70% da vida) | Ajoelha com um anel dourado pulsando | Cura 2 de vida por segundo por até 4 s; **5 de dano interrompem** e o deixam atordoado 1,5 s | — |
| **Chuva de relâmpagos** | Marreta para o céu (0,8 s); colunas claras piscam no chão por 1,2 s antes de cada raio | Ficar numa faixa **sem coluna**. Cada onda tem um raio onde o gato está e uma faixa livre de pelo menos 2,5 tiles | **8 hits** (4 corações) |

- **2ª fase (metade da vida):** golpes 35% mais rápidos, pausas menores, uma onda e um raio a mais por onda. A barra mostra "(furioso)".
- **Pelas costas:** vale 1,5× (ele está sempre em combate, nunca furtivo).
- **Contato:** encostar nele não machuca.

## Balanceamento
- Golden: **30 de vida**. Adagas: 2 de frente, 3 pelas costas → ~12 golpes (6 combos).
- O gato tem **7 corações = 14 hits** nessa luta. **Dano por golpe (decisão de 09/11/2026):** marretada 1 hit, salto 4, relâmpago 8. O aviso mais longo carrega o castigo maior: dá para levar muitas marretadas, mas dois relâmpagos matam (8 + 8 > 14). Um relâmpago sozinho nunca mata com a vida cheia.

## Parâmetros tunáveis
Todos em `ScriptableObjects/Enemies/GoldenBossSettings` (vida, tempos, alturas, larguras, dano, recargas). Valores iniciais na tabela acima.

## Implementação (Unity)
- `BossRules` (C# puro): escolha do golpe e colunas dos relâmpagos (sempre com lugar seguro). Testado em EditMode.
- `GoldenBoss` (estados, golpes, dano, derrota, virar companheiro), `LightningStrike`, `GoldenBossSettings`.
- `BossArena` (gatilho, grades, conversas, save), `ArenaGate`, `CursedTome` (cena da maldição), `BossHealthBar` (HUD).
- O Golden carrega um `CompanionBond` desligado; a cena do tomo liga e faz o vínculo.
- Depuração: menu ⋮ do `GoldenBoss` no Inspector força cada golpe.

## Critérios de aceite
- Dado o gato entrando na arena, então a grade de trás fecha e o Golden fala; ao fim, a barra de vida aparece.
- Dado o salto, quando o gato corre por baixo dele, então não toma dano.
- Dada a cura, quando o gato causa 5 de dano, então ela para e ele fica atordoado.
- Dados os relâmpagos, então sempre existe uma faixa sem aviso onde o gato fica a salvo.
- Dado o Golden com 0 de vida, então as grades abrem; o tomo pode ser pego.
- Dado o tomo pego, então os corações caem de 7 para 1, o Golden salva o gato e o HUD mostra 2 corações e o Paladino disponível.

## Perguntas abertas
- [PROPOSTA] Vida 30 do Golden.
- [PROPOSTA] A arena reinicia ao morrer (sem "checkpoint no meio da luta").
- [PROPOSTA] O tomo fica com o gato só como flag no save (sem inventário no MVP).
