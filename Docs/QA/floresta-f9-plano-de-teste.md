# Plano de teste — Floresta F9 (a Tobi e a ponte de madeira)

Spec: `Docs/Floresta/plano-implementacao.md`, F9 ("Estado"). Falas: `Docs/Narrativa/floresta-roteiro.md` (D-SAIDA-01, D-TOBI-01 a 05).

Antes:
- Rode **Tools > Sete Vidas > Floresta: gerar as cenas**. O `ForestLayout` está na versão 34, com a Tobi e a ponte de madeira na R4.
- Para pôr só a Tobi e a ponte, sem recriar a cena, use **Floresta: Tobi e a ponte F9 (sem recriar a cena)**.
- A arte já foi gerada; para refazer, rode `Tools/CharacterSprites/gerar_tobi.ps1`.
- Use um save que já passou da Isyathy (flag `isyathy_morta`) e tem a ponte caída (`ponte_caiu`).
- Para testar a entrega, o save precisa de 12 tábuas (as flags `madeira_W..`) e 20 moedas.

## Testes automáticos

- **EditMode > `TobiTests` (4):**
  - a conversa certa em cada momento (encontro, faltam tábuas, faltam moedas, reconstrução, depois);
  - "Faltam 3" e "Falta 1";
  - a ponte montada do oeste para o leste em menos de 5 s;
  - a arte: animações, retratos, blocos da ponte (e a escada do atalho, desenhada na montagem da cena).
- **`Tools/LevelPreview/checar_floresta.ps1`:** deve dar "Tudo alcançável, sem softlock" (conferido em 17/11/2026).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | G3 → R5 | Suba a G3 até o topo e saia na cabeceira leste | O gato diz "Do outro lado. Finalmente." e "E dessa vez eu subi. Ninguém me jogou de lugar nenhum." (só na primeira vez). |
| 2 | R5 → R4 | Ande para oeste, até a beira do abismo | A Tobi está na beira, olhando a ponte caída: toupeira de óculos de solda, focinho rosa, mochila gigante, animada (respira, fareja, a mochila balança). |
| 3 | R4 | Chegue perto dela | O encontro começa sozinho (D-TOBI-01 e 01b), com os retratos dela (animada, orgulhosa, gananciosa, ofendida) e as animações acompanhando cada fala. |
| 4 | R4 | E de novo, com menos de 12 tábuas | "Faltam N. E eu não trabalho fiado!" com a conta certa ("Falta 1" no singular) e a dica dos baús. |
| 5 | R4 | E com 12 tábuas e menos de 20 moedas | "Madeira eu tenho, pagamento não..." (ofendida, de braços cruzados). |
| 6 | R4 | E com 12 tábuas e 20 moedas | A cena: "Doze tábuas e vinte moedas!" (as 20 moedas saem do contador). Ela tira a chave inglesa maior do que ela, as tábuas voam do gato para os braços dela, ela cambaleia ("Segura... segurei!"), cava e some. |
| 7 | R4 | A cena continua | A câmera atravessa o abismo até o lado da Fortaleza. A Tobi sai do chão puxando a corda. A nuvem de obra (poeira, estrelinhas, ela alternando martelo e broca) volta para o leste montando a ponte tábua por tábua, com a câmera junto. A corda aparece pendurada no começo da ponte. |
| 8 | R4 | O fim da cena | A câmera volta para o gato; a Tobi pula do chão ao lado dele ("Essa aqui é a taxa de material."). O gato anda até a ponte e pisa: as tábuas balançam, a tela treme, silêncio... "Garantia de... uns cinquenta anos. Talvez." e as duas falas do gato. O controle volta. |
| 9 | R4 → R1 | Atravesse a ponte para oeste | Dá para passar (18/11/2026: uma faixa grossa de madeira, vigas escuras em cima e embaixo com as cabeças dos postes saindo, tábuas em pé avermelhadas no meio; sem corrimão e sem vigas em X) e voltar até a R1 e a Fortaleza. |
| 10 | R4, lado oeste | Logo antes da ponte, o estrado de tábuas no chão | Anda-se por cima normalmente. Em cima da escada ou do estrado, **segure ↓**: o gato agarra a escada e desce (a dica "segure ↓ na escada" aparece depois da ponte nova). Também dá para descer pelo estrado com ↓ + pulo e agarrar a escada de lado, como um cipó. Subindo até o topo, o gato sobe para cima dela. No meio da descida (sem chegar ao fundo do poço, que é fechado), o gato vai para as Raízes (A1) e chega ao pé de outra escada, na parede da esquerda. Subindo um pedaço dela, volta para a R4, em cima da escada de cima. Antes da ponte nova, o buraco está tampado (E: "Tábuas pregadas...") e o estrado não deixa cair. Vindo da A1 pela corda, chega ao lado do poço. |
| 11 | R4 | E na Tobi depois | Falas em rodízio: o poço da escada, os telhados de Miaravéu, "Não se acostume." |
| 12 | Qualquer | Saia e volte à Forest_Road | A ponte de madeira continua lá (a flag fica no save); a cena não se repete. |
| 13 | Inventário | Depois da ponte | O texto das tábuas muda para "Viraram ponte. Menos uma, que a Tobi guardou no bolso." |

## O que observar

- A cena dura uns 25 a 35 s? Lê bem com a câmera atravessando os 104 tiles?
- O gato anda até a ponte sozinho sem cair no abismo (a ponte já está montada nessa hora)?
- A Tobi fica no lugar certo depois da cena (de volta à beira leste, olhando o gato)?

## A saída da fase (18/11/2026)

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 20 | G3 → R5 | Suba depois da Isyathy e saia no topo | O fundo **amanhece**: uma faixa quente (laranja, rosa, violeta) sobe do horizonte atrás das montanhas e some no alto, onde as estrelas continuam. Ela acende conforme a câmera chega na cabeceira leste (já começa no fim da ponte). |
| 21 | R5, leste | Ande até o fim | O portão de pedra em ruínas: o arco quebrado no meio, o pilar da direita partido, escombros no chão, musgo e a placa rachada "BEM-VINDO A MIARAVÉU". Passar por ele abre a conversa do fim (D-MIARAVEU-PORTAO, uma vez): "Fim da Floresta Silenciosa..." e o jogo continua. |
| 22 | R5, alto | Suba pelos ganchos | Três galhos de mão única (a 11, 22 e 33 do chão), cada um com um gancho embaixo; sem o gancho não dá para subir (11 tiles de um para o outro). No último, E na pilha de pedras do teto: "O caminho das cachoeiras sobe por aqui...". O buraco do teto fica fechado (área futura). |
