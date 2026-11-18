# Plano de teste — melhorias da Floresta (17/11/2026)

Pedidos do usuário: o Galho Seco contra o arco e a morte dele, o mel e a colmeia das vespas, a ponte maior e a queda dela, os olhos roxos da Isyathy nas aparições, e o dobro de botes no Ninho.

Antes:
- Rode **Tools > Sete Vidas > Floresta: gerar as cenas**. O `ForestLayout` está na versão 31: a ponte da R4 tem 104 tiles e a horda tem 8 zumbis.
- A arte da ponte nova (`ponte_pedra_52_*`) já foi gerada. Se faltar, rode `Tools/EnvironmentSprites/gerar_ponte.ps1`.
- A colmeia, o mel e o brilho dos olhos são desenhados pelo código. Não precisa gerar nada.

## Testes automáticos

- **EditMode > `ForestTweaksTests` (7):**
  - a flecha no Galho Seco;
  - a queda dele ao morrer;
  - o mel a 20%;
  - os desenhos da colmeia e do mel;
  - a ponte só treme com o peso da horda;
  - a horda chega amontoada atrás do gato;
  - o brilho dos olhos.
- **EditMode > `NestBurrowTests` (17):** inclui os trilhos curvos, a onda pela velocidade, o bote em arco e o intervalo de 3 a 5 s.
- **`Tools/LevelPreview/checar_floresta.ps1`:** deve dar "Tudo alcançável, sem softlock" e nenhuma sala sobreposta (conferido em 17/11/2026).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | Qualquer Galho Seco | Com o Caçador, atire flechas de longe | Cada flecha crava com um "tóc" e o tronco treme. Ele aguenta uns 15 tiros. |
| 2 | Qualquer Galho Seco | Bata de perto (adagas ou marreta) | O dano é o de sempre, e dobrado pelas costas quando o braço está preso. |
| 3 | Qualquer Galho Seco | Derrube um | Ele estala, tomba para longe do gato (a base fica no chão), bate no chão levantando lascas e poeira, quica e some. |
| 4 | Galho Seco infectado | Derrube um | Além da queda, solta a nuvem tóxica no pé, como antes. |
| 5 | Salas com vespas (A4, B4…) | Olhe o teto onde elas dormem | Há uma colmeia pendurada em cada lugar. As vespas que saem do Broto não têm colmeia. |
| 6 | Salas com vespas | Mate umas 10 | Mais ou menos 1 em 5 solta uma gota de mel. O mel cai, pousa e cura meio coração. Com a vida cheia, fica no chão. |
| 7 | Forest_Road, R4 | Ande até a ponte | A ponte tem 104 tiles (4× a original) e começa bem antes. |
| 8 | Forest_Road, R4 | Pise 3 tiles dentro da ponte | A horda de 8 zumbis aparece na cabeceira, atrás do gato. O controle sai e o gato corre sozinho. Os zumbis correm mais que o normal e alcançam o gato, vindo em fila colada atrás dele, sem ferir. |
| 9 | Forest_Road, R4 | Observe | A ponte só treme quando 3 ou mais zumbis estão em cima, e treme mais forte com mais zumbis. Caem pedrinhas. |
| 10 | Forest_Road, R4 | Observe | O gato para no meio, vira para a horda e diz "...Será que a ponte aguenta tanto peso?". A ponte já está cheia de zumbis perto dele. 1,6 s depois, o meio desaba e cai todo mundo. O controle volta durante a queda. |
| 11 | Forest_Road → A1 | Caia | A queda leva à A1, como antes. |
| 12 | Salas com aparição | Espere os olhos aparecerem no fundo | Os olhos continuam verde-amarelados, com um halo roxo pulsando em volta. O halo fica mais fraco com o olho meio fechado. |
| 13 | Ninho E1–E3 | Ande pelo chão | Os botes vêm a cada 3 a 5 s, mais ou menos o dobro de antes. |
| 14 | Ninho E1–E3 | Observe um bote | Ela sai em S, acompanha o gato com a cabeça, recua, dá o bote em arco, passa do ponto, sacode na mordida e volta para a terra se contorcendo. |
| 15 | Salas com serpente na toca | Leve o bote dela | O corpo esticado sai da toca sem nenhum risco escuro entrando na terra. |
