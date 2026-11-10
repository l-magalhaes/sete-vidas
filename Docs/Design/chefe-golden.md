# Chefão: Koda, o Dourado, guardião do tomo

> **Nome (10/11/2026):** o paladino se chama **Koda, o Dourado**, descendente de Golden, o Dourado (fundador da Cidadela). "Golden" continua só nos nomes do código (`GoldenBoss`, `GoldenBossSettings`). Ficha: `SeteVidasDocs/Personagens.md` §2.

**Pilar:** 1 (sete corações, sete classes) e 3 (combate) | **Escopo:** MVP | **Status:** rascunho para teste

## História (decisão de 09/11/2026)
O Ladino começa com **as 7 vidas**. O Golden, paladino da Ordem, protege o tomo selado. O gato o derrota na arena, pega o tomo e o selo se rompe: **seis vidas se apagam** (sobra 1, quase vazia). O Golden, que reconhece a força de quem aguentou o selo, salva o gato e dá a **classe Paladino em troca do juramento de destruir o tomo**. Esse vínculo devolve o 2º coração.

## Objetivo de experiência
Uma luta de leitura: cada golpe do Golden tem um aviso claro e uma resposta clara. O jogador aprende a **passar por baixo** do salto, a **punir** a cura e a **procurar o lugar seguro** nos relâmpagos.

## Fluxo
1. Da laje do trecho I, o gato segue para a direita até a **arena** (x 180–210). Santuário antes, em x 172.
2. Ao entrar, a **grade de trás fecha** e o Golden fala (D-GOLDEN-00). A luta começa ao fim da conversa.
3. **Vitória:** o Golden ajoelha, as duas grades abrem e a vitória vai para o save (`chefe_golden_derrotado`). Conversa D-GOLDEN-DERROTA.
4. **Sala do tomo:** E pega o tomo (D-TOMO-01) → clarão roxo → **o tomo escapa da pata e flutua aberto (aura roxa, páginas virando); o gato cai de joelhos e tomba, e no chão se contorce com veias roxas e olhos verdes, estendendo a pata para o livro (`curse_kneel`, `curse_writhe`)** → **o tomo suga fiapos de vida dele e, a cada 0,5 s, arranca um coração inteiro que voa até o livro (7 → 1)** (`TomeDrainFx`) → o golpe do Koda **repele o tomo, que fecha e cai no chão** → **D-KODA-SELO: Koda, de joelhos, crava a marreta no chão (`GoldenBoss.SealStrike`), clarão dourado, e a maldição para na última vida** → D-TOMO-02 → **o Contratante (capivara necromante) surge num clarão roxo**, revela o plano (D-CAPIVARA-01), **toma o tomo**, **joga uma moeda de ouro** e some (D-CAPIVARA-02) → tela escurece → o Golden aparece ao lado → **resgate e juramento por vingança** (D-GOLDEN-01 e 01b) → **+1 coração (2) e Paladino** → dica de troca de classe. A moeda fica no chão.
5. **Morrer na luta** (ou descansar): a arena reinicia (grade aberta, Golden com vida cheia).
6. Estado recuperável: fechar o jogo no meio da cena do tomo deixa o Golden de pé ao lado do pedestal; falar com ele faz o juramento.

## Golpes
| Golpe | Aviso | Como escapar / punir | Dano |
|---|---|---|---|
| **Marretada** (perto, até 1,8 tile) | Marreta erguida atrás da cabeça, 0,5 s | Recuar; atacar na recuperação (0,5 s). **Combo:** se o gato fica perto, 45% de chance de emendar outra (até 3 seguidas), com preparação mais curta (0,33 s) | **1 hit** (meio coração) |
| **Salto com marretada** | Agacha com a marreta no alto, 0,5 s; salta 5 tiles de altura até onde o gato estava | **Passar por baixo** dele para o outro lado; atacar quando ele cai (0,6 s parado). Se cair do lado do gato, levanta já batendo (marretada) | **4 hits** (2 corações; área de 3,2 tiles) |
| **Cura concentrada** (só abaixo de 70% da vida) | Ajoelha com um anel dourado pulsando | Cura **5 de vida por segundo** por até 4 s (até 20); **5 de dano interrompem** e o deixam atordoado 1,5 s | — |
| **Chuva de relâmpagos** | Marreta para o céu (0,8 s); colunas claras piscam no chão por 1,2 s antes de cada raio | Ficar numa faixa **sem coluna**. Cada onda tem um raio onde o gato está e uma faixa livre de pelo menos 2,5 tiles | **8 hits** (4 corações) |
| **Escudo sagrado** (junto com os relâmpagos) | Bolha dourada em volta dele, do momento em que ergue a marreta até o último raio | **Invulnerável de todos os ângulos** (frente, costas, cima). O golpe bate na bolha ("escudo!"). Use o tempo para desviar, não para atacar | — |

- **Agressivo (10/11/2026):** pausa entre golpes 0,4 s (era 0,7), anda a 3,6 tiles/s (era 2,5), relâmpagos a cada 7 s (era 9), cura a cada 13 s (era 15).
- **2ª fase (60% da vida):** golpes 45% mais rápidos, pausas menores, uma onda e um raio a mais por onda. A barra mostra "(furioso)".
- **Pelas costas:** vale 1,5× (ele está sempre em combate, nunca furtivo).
- **Contato:** encostar nele não machuca.

## Balanceamento
- Koda: **45 de vida** (era 30). Adagas: 2 de frente, 3 pelas costas → ~18 golpes, mais o que ele curar.
- O gato tem **7 corações = 14 hits** nessa luta. **Dano por golpe (decisão de 09/11/2026):** marretada 1 hit, salto 4, relâmpago 8. O aviso mais longo carrega o castigo maior: dá para levar muitas marretadas, mas dois relâmpagos matam (8 + 8 > 14). Um relâmpago sozinho nunca mata com a vida cheia.

## Parâmetros tunáveis
Todos em `ScriptableObjects/Enemies/GoldenBossSettings` (vida, tempos, alturas, larguras, dano, recargas, `ComboChance`, `SecondPhaseSpeed`, `ShieldDuringLightning`). Valores iniciais na tabela acima.

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
- Dados os relâmpagos, então o escudo está ativo e nenhum dano passa, de nenhum ângulo; depois do último raio ele volta a tomar dano.
- Dado o Golden com 0 de vida, então as grades abrem; o tomo pode ser pego.
- Dado o tomo pego, então os corações caem de 7 para 1, o Golden salva o gato e o HUD mostra 2 corações e o Paladino disponível.

## Perguntas abertas
- [PROPOSTA] Vida 45 do Koda, cura 5/s e combo de 45%: playtest para ver se a luta ficou longa demais.
- [PROPOSTA] A arena reinicia ao morrer (sem "checkpoint no meio da luta").
- [PROPOSTA] O tomo fica com o gato só como flag no save (sem inventário no MVP).
