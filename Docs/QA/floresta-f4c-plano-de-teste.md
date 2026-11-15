# Plano de teste: Floresta F4c, arte definitiva do bestiário (14/11/2026)

Também cobre duas coisas da F5:
- a Isyathy aparecendo muitas vezes no fundo;
- os documentos dos devotos nas cabanas.

Antes:
1. `powershell -ExecutionPolicy Bypass -File Tools/EnemySprites/gerar_floresta.ps1` (já rodado) e `Tools/EnvironmentSprites/gerar.ps1` (já rodado). Volte ao Unity e espere a importação.
2. Rode o menu **21**. Ele refaz os `*_infectado.png` a partir da arte nova, porque o gerador apagou os antigos. O console não pode ter "falta …png" nem "Campo … não existe".
3. Rode o menu **22**.

Prévias:
- inimigos: `SeteVidasDocs/Referencias/floresta_bestiario_f4c.png`;
- documentos e a Isyathy: `SeteVidasDocs/Referencias/floresta_ruinas_f5.png`.

## Testes automáticos

- `ForestBestiaryArtTests`: todos os quadros existem.
- `ForestRuinsTests`: a Isyathy sorteia a animação certa por sala.
- `ForestBestiaryTests`, `ForestFoundationTests`, `ForestGrayboxTests`, `ForestHunterTests`: as mecânicas não mudaram.

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | A4 (vespas no teto) | Chegue perto devagar | Três vespas agarradas de ponta-cabeça na raiz; de vez em quando uma antena mexe |
| 2 | A4 | Acorde-as | Voam juntas: asas batendo, as três se mexendo em volta. Leitura clara contra o fundo; olho vermelho-alaranjado. Mecânica igual à do morcego (persegue, recua, volta à raiz) |
| 3 | Broto (B1, D3) | Acorde-o | Casulos claros na copa; as vespas saem de lá |
| 4 | Galho Seco (B1, C3) | Olhe sem chegar perto | **Parece uma árvore do cenário.** A copa balança de leve; a cada ~4 s um olho se abre na casca por um instante |
| 5 | Galho Seco | Chegue perto | Dois olhos âmbar acesos nos nós; o galho-braço sai de lado, na frente da copa. O braço sobe e treme (aviso) e bate no chão à frente (preso: pogo e dano dobrado como antes) |
| 6 | Galho Seco | Os pés | O tronco encosta no chão; o corpo sólido continua de 5,5 tiles (não dá para pular por cima) |
| 7 | Planta (A2, B4, C3…) | Olhe parada | Bulbo vinho com pintas claras, folhas-orelha; respira |
| 8 | Planta | Provoque o bote | A boca abre em V, treme, mostra a língua; o pescoço é um caule verde com nós. Na volta, murcha com a **flor turquesa** no alto: pogo só nela |
| 9 | Planta na parede/teto | Olhe | A cabeça gira para onde vai, como antes |
| 10 | Serpente (A2, A4, C5…) | Olhe o chão | A toca é um **montinho igual ao chão** (musgo e terra), quase invisível; só os olhos laranja na fresta, que brilham e piscam |
| 11 | Serpente | Chegue perto | Aviso: a cabeça sai da toca, a língua aparece. Bote: boca aberta com as presas; os gomos do corpo têm escamas |
| 12 | Serpente que rasteja (A5, C5, C7…) | Olhe | O corpo ondula ao andar |
| 13 | Infectados (C4–C7, D, E) | Olhe os mesmos inimigos infectados | A arte nova em roxo e verde-ácido; a flor turquesa da Planta continua turquesa |
| 14 | Qualquer sala de A a D | Fique ~30 s | A Isyathy aparece **várias vezes** no fundo (a cada 5–11 s), alternando: o pescoço que sobe, abre os olhos, põe a **língua** para fora e afunda; os olhos piscando entre as árvores; o corpo atravessando atrás das copas |
| 15 | E e F | Fique na sala | Ela **não** aparece no fundo (no Ninho e no Coração ela está perto de verdade) |
| 16 | Cabanas: A3, B1, C3, E3 | Quebre a porta rachada e entre | Ao lado do baú há um documento (estaca com papel e a serpentezinha de madeira; caderno; carta; página rasgada com mancha roxa). Leia: D-DEVOTO-01 a 04, nessa ordem. Vão para o Diário |
| 17 | Casa da Vulpis (D3, porta da cabana) | Entre | Parede de toras e assoalho de tábuas; lareira acesa no meio (o fogo mexe, luz quente em volta), cama com manta ruiva embaixo da janela, pele no bastidor, lenha, tapete, mapa na parede, ervas do teto, mesa com a vela acesa, banco, prateleira com potes e o arco. Sem o texto flutuante "Cabana da Vulpis". Fala: "Vazia. Mas a lareira está acesa." |
| 18 | Casa da Vulpis | Suba na prateleira | É a mesma plataforma de antes (topo a 3 do chão, mão única); o desenho agora é a tábua com potes; a moeda continua no meio |
