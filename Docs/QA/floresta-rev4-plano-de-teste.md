# Plano de teste — Floresta rev. 4 (anel, atalhos de verdade, serpentes, cenas de teste na fase)

Antes:
1. Rode **Tools > Sete Vidas > 21. Floresta: graybox (todas as cenas)**. Ele recria `Forest_Road`, `Forest_A_Roots` … `Forest_F_Heart` e a casa da Vulpis (`Forest_D_Cabin`).
2. Se o console avisar de alcance, rode `Tools/LevelPreview/checar_floresta.ps1`. O mapa inteiro sai em `SeteVidasDocs/Referencias/floresta/floresta_mapa.png`.

Play direto numa cena da floresta, com "Play começa na tela inicial" desmarcado, usa o save de testes. Para zerar os atalhos, apague esse save.

## Testes automáticos

- EditMode > `ForestGrayboxTests`:
  - `RealShortcut_OpensOnlyFromItsSide_AndThenForEveryone`
  - `Snake_StrikesFromTheHole_ThenStretches_ThenHides`
  - os anteriores

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | A1 | Acorde e ande para o leste | Templário **zumbi** patrulhando (não há templário vivo nem fio de alarme na floresta); serpente num buraco entre as raízes |
| 2 | A2 | Chegue a 3 tiles da serpente, em pé | Chocalho, bote de 3 tiles; esticada, aceita pogo e apanha; depois volta ao buraco e não apanha (tink) |
| 3 | A2 | Agache na folhagem perto dela | Ela não dá o bote |
| 4 | A2 | Embaixo da raiz pendurada, aperte E | "Um cipó enrolado lá no alto…" (o atalho da Ravina está fechado) |
| 5 | A3 | Atravesse | É a `Forest_Test_A` inteira (tronco/W01, poço/W02, cactos); a porta do alto leva à A4 |
| 6 | A4 | Desça ao fundo e volte | Cogumelo na raiz grossa → pouso → cacto (pogo) → prateleira → vinha → porta da A6 |
| 7 | A6 | Aperte E na chegada (perto da parede da esquerda) | "O poço lá em cima está fechado…" |
| 8 | B1 | Atravesse | É a `Forest_Test_B`; santuário **S-F1B**; no patamar, "E: soltar o cipó" → a tampa some e um cipó desce pela parede até o chão |
| 9 | B2 | Suba | Vinha curta → **cacto** (pogo) → tronco com vinha → cacto que vai e vem → saída. Nenhuma vinha liga direto o chão à saída |
| 10 | B3 | Pegue o W07 | Folha → galho (ou o cogumelo) → plataforma do baú, com a planta pendurada do teto |
| 11 | B4 | No alto da torre, "E: soltar o cipó" | O cipó desce pela parede da esquerda até o chão; dá para subir a torre inteira por ele |
| 12 | C4 | Ao lado do alçapão do lago, "E: abrir o alçapão" | A tampa some; descendo o poço (cipó na parede) chega-se à A6 pelo teto; da A6, o cipó da parede sobe até o poço |
| 13 | C6 | No fundo (S-F2), "E: abrir o alçapão" | Cai-se na A2 (no galho sob o teto); da A2, o cipó da raiz pendurada sobe até a C6 |
| 14 | D3 | Atravesse | É a `Forest_Test_C`: santuário **S-F3**, arena das plantas, chave, botão das armadilhas, a cabana |
| 15 | D3 | Pegue a chave e entre na cabana | Cena `Forest_D_Cabin` (baú W16); "sair" volta para a frente da porta na D3 |
| 16 | E2 | No alto, "E: soltar o cipó" | O vão da vinha da direita (18 a 22) se fecha; a parede inteira vira subida |
| 17 | E4 | Ao lado do santuário S-F4, "E: abrir o alçapão" | O poço comprido desce até a C1 (galhos de descanso no caminho); da C1, a raiz pendurada com cipó sobe até a E4 |
| 18 | Qualquer atalho | Feche e reabra o jogo (Continuar) | O atalho continua aberto (flag no save), dos dois lados |
| 19 | Toda a floresta | Procure templários vivos, fios de alarme, lanças, fogo, lançador de flechas | Nenhum |
| 20 | Espinhos | Passe pelos espinhos do caminho | Nenhum espinho colado numa porta ou num pouso obrigatório; dá para quicar neles ou contorná-los |
