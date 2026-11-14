# Plano de teste — Melhorias 4 (14/11/2026)

Pedidos do usuário: (1) flecha com metade do dano da adaga; (2) gancho saindo do arco, como flecha com corrente, mais dinâmico; (3) mais ganchos pela floresta.

Antes: rode o menu **21** (`ForestLayout` versão **15**) e o **22**.

## Teste automático (EditMode)

- `ForestHunterTests.Bow_ArrowIsHalfADaggerHit_CarryingTheRest`.

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | 1 | Com 2 corações, flechas num inimigo de 4 de vida | 4 flechas (1 cada); a adaga mata com 2 golpes |
| 2 | 1 | Com 3 corações (adaga dá 3) | As flechas alternam 1 e 2: duas flechas = um golpe de adaga |
| 3 | 2 | Botão direito / Y apontando para um gancho | Um arquinho aparece na mão do gato, virado para o gancho; a ponta de ferro sai dele puxando uma **corrente** que chicoteia no voo |
| 4 | 2 | A ponta crava | Faíscas, a câmera dá um toquinho e a corrente vibra; no puxão, esticada, com rastro de vento no gato |
| 5 | 2 | Pendurado | Corrente com uma barriguinha; pular ou dar dash solta |
| 6 | 2 | Atire sem gancho na mira | A corrente vai chicoteando, bate (tink na parede) e volta frouxa |
| 7 | 2 | Com o Ladino ou o Paladino | O arquinho também aparece (o gancho vale em qualquer classe) |
| 8 | 3 | Passe pelas salas com ganchos novos (lista abaixo) | Cada gancho alcançável de um chão ou de outro gancho; pendurado não fica dentro de bloco |
| 9 | 3 | Tente usar os ganchos para abrir atalho de mão única (A2, A6, C1, C6) ou passar o Portão do Templo | Não dá: nenhum gancho perto dos poços com cipó nem depois do portão |

## Onde estão os ganchos novos

A1 (3), A2 (2), A4 (3), A5 (2), A6 (2), B2 (2), B3 (3), B4 (2), B5 (3), B6 (1), C1 (2), C2 (4), C3 (4, um em cada vão da escada), C4 (4, por cima do lago), C5 (4), C6 (4), C7 (2), D1 (5), D2 (2), D4 (2), D5 (2), E6 (1), G1 (2), G2 (2), G3 (3).

Sem ganchos novos, de propósito: A3, B1 e D3 (desenhos do usuário), B7 (desafio de pogo), C4p e E4p (poços dos atalhos de mão única), E5 (sem volta), F1 (arena da Isyathy) e a Estrada.

## Riscos conhecidos

- O checador de alcance (`ForestReach`) não sabe usar o gancho: os ganchos não foram verificados por ele, só a olho no desenho em texto de cada sala.
- B2 (Escada de Cactos) e A4: os ganchos encurtam a subida que antes era só no pogo. É a volta com a classe nova; se tirar a graça, apague os de lá.

## Ajustes seguintes (14/11/2026)

| # | Passo | Esperado |
|---|---|---|
| 10 | Caçador: use o gancho (acertando ou errando) | O arco das costas some enquanto o arquinho do gancho está na mão; volta depois |
| 11 | Entre na cabana da Vulpis **antes** do vínculo | "Algum caçador mora aqui. E não está em casa." |
| 12 | Entre na cabana **depois** do vínculo (mesmo que já tenha entrado antes) | "Deve ser a casa daquela raposa." (uma vez) |
| 13 | Ande no chão logo acima de um morcego dormindo no teto da sala de baixo | Não acorda (nem pela visão nem pelos passos); no máximo um "?" abafado com barulho alto |
| 14 | Morcego na mesma sala, sem parede no meio | Continua acordando rápido; o "!" aparece embaixo dele |
