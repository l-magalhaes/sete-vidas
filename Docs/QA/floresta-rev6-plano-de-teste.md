# Plano de teste — Floresta rev. 6 (ajustes à mão, Mercado dos Primatas, pêndulo de espinheiro, Galho Seco)

Antes:
1. Rode **Tools > Sete Vidas > Floresta: gerar as cenas** (o `ForestLayout` está na versão 25).
2. Rode **Tools > Sete Vidas > Mapa do mundo e Diário** (o mapa e as placas P-PRIM-09 e P-PRIM-10 no Diário).
3. Se mexer à mão de novo numa cena da Floresta, rode `Tools/LevelPreview/comparar_floresta.ps1` para listar o que mudou.

## Testes automáticos

- EditMode > `ForestFoundationTests.DryBranch_WakesWhenTouched_EvenWithTheCatHidden`
- EditMode > `ForestRuinsTests.GeneratedArt_HasRuinsAndSightings` (os desenhos do mercado)
- `Tools/LevelPreview/checar_floresta.ps1`: "Tudo alcançável, sem softlock"

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | R2, A2 | Atravesse | Sem a folhagem alta no fim da R2 e perto da serpente da A2 |
| 2 | A5 | Entre no vagão B | O relógio parado pendurado do teto do vagão B |
| 3 | B4 | Da viga de 26, pule no cacto que vai e vem e quique | Chega à viga alta (36); sem a escada quebrada no fundo |
| 4 | B7 | Com o Caçador, olhe as paredes | Três ganchos onde o usuário os pôs |
| 5 | C3 térreo | Entre pelo leste | Mercado: parede de fundo, letreiro torto, caixa, carrinho, gôndolas (uma tombada), vinhas do teto; o templário patrulhando; as duas sombras do térreo sem árvore (é o teto) |
| 6 | C3 térreo | Leia a caixa e a prateleira de latas | P-PRIM-10 e P-PRIM-09; as duas aparecem no Diário |
| 7 | C3 térreo | No fundo, chegue ao baú | Dica "abra o baú: coração fantasma"; ao abrir, +1 coração temporário (fantasminha no HUD) |
| 8 | C3 térreo | Volte para o leste | Dicas "o fantasma apanha no seu lugar" e "não cura e some se você morrer"; um golpe do templário tira o fantasma antes da vida |
| 9 | C3 andares | Suba | Sem janelas, escada, folhagem e vitrine; árvore com sombra à esquerda no andar 2 e à direita no andar 3; máquina e planta nos lugares novos |
| 10 | A6, D3 | Olhe os pêndulos | Tora de espinheiro presa por cipó (não mais lâmina de aço); fere igual e aceita pogo |
| 11 | C2 (ou outro Galho Seco) | Disfarçado, ande contra o tronco **escondido na sombra** | "!" na hora, estalo, ele vira para o gato e bate |
| 12 | qualquer árvore de sombra | Encoste e atravesse | A árvore fica parada e não faz barulho |
| 13 | D1–D6, E2, G2, G3 | Atravesse | Objetos nos lugares do ajuste à mão; a D5 sobe até as moedas de cima pelo gancho novo; a D6 sem o gancho da quina da laje 3 continua dando a Chave do Templo; a G3 sobe pelo zigue-zague |

## Rodada de 16/11/2026 (ajustes à mão e diálogos)

Antes: **Tools > Sete Vidas > Diálogos: atualizar textos (sem recriar cenas)**, depois **Floresta: gerar as cenas** e **Mapa do mundo e Diário**.

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 14 | Road, A, B, C5, E, F1, G1 | Atravesse | Sem as placas de texto do graybox; continuam "o lago escoa por aqui" (C4), "aurora ↑" (G3) e as da D |
| 15 | A6 | Sem o Caçador, olhe o nicho acima da porta | O W04 não se alcança mais (o galho saiu); com o Caçador, o gancho da quina leva até ele |
| 16 | E6, G2 | Atravesse | Sem a folhagem da E6; o morcego da G2 mais alto, perto dos cogumelos |
| 17 | Fortaleza | Fale com o Golden e leia as placas | Os textos novos (ex.: "Ei, ladrãozinho... Não feche os olhos."); placas sem "bebe vidas" |
| 18 | D5 | Fale com a Vulpis | A conversa nova ("Desde que caiu daquela ponte..."); depois, as três falas de espera novas |
| 19 | Fim da Fortaleza / água | Caia na água; saia da Cidadela | "Odeio água."; D-FIM sem a fala da moeda |
