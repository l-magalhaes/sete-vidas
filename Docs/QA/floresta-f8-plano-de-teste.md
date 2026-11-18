# Plano de teste — Floresta F8 (a luta com a Isyathy e a volta da fase)

Spec: `Docs/Floresta/plano-implementacao.md`, F8 ("Estado").

Antes:
- Rode **Tools > Sete Vidas > Floresta: gerar as cenas**. O `ForestLayout` está na versão 33 (a arena com galhos de madeira, galhos podres temporários e 3 ganchos, sem pedra), com os portões de raízes da F1, a passagem de volta para a E6 e o cipó da E5.
- Para refazer só a arena, use o menu **Floresta: chefe Isyathy F8 (sem recriar a cena)**.
- A arte da Vulpis (17/11/2026): rode `Tools/CharacterSprites/gerar_vulpis.ps1` (já rodado) e o menu **Floresta: Vulpis com a arte nova (NPC da D5 e arena da F1, sem recriar as cenas)**.
- Para lutar de novo, use um save sem a flag `isyathy_morta`.
- O corpo e a cabeça dela são desenhados pelo código (como no Ninho). Refeita em 17/11/2026: anda pelas quatro bordas dentro da terra, sai, ataca e espera (inspirada nos chefes-serpente que andam pelas bordas da sala).

## Testes automáticos

- **EditMode > `IsyathyBossTests` (17):**
  - as fases;
  - as flechas da Vulpis;
  - os golpes por fase (sem nuvens);
  - o dano pelo tamanho do aviso;
  - a cabeça só exposta;
  - as combinações da fase III;
  - os buracos nunca embaixo do gato;
  - o cuspe cai onde mira;
  - a casca da praga bloqueia tudo até a flecha forte;
  - os degraus do zoom da cena mantêm a proporção;
  - a Vulpis tem todas as animações e os retratos;
  - os tempos legíveis (aviso, mira, espera, velocidade do montinho), as quatro bordas, o Arco (cabeça e rabo, nunca o corpo inteiro) e a poça de veneno.
- **`Tools/LevelPreview/checar_floresta.ps1`:** deve dar "Tudo alcançável, sem softlock" (conferido em 17/11/2026, com os portões da arena abertos).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | F1 | Entre pela E6 e ande uns 6 tiles | A apresentação: o controle trava e a câmera vai até o meio da arena com zoom. O chão treme e racha; ela sobe devagar, de olhos fechados, abre os olhos (o roxo acende) e encara o gato. Ela ruge: a tela treme, um soco de zoom, ondas roxas saem da boca e cai terra do teto. O letreiro "ISYATHY" (em ouro) e "a Devoradora Silenciosa" (lilás), entre faixas de ouro e jade, entra deslizando e some. A câmera volta para o gato, ele diz "Ela é... maior do que a floresta.", a barra dela aparece e a luta começa (uns 9 s). A Vulpis não aparece (ela só vem para atacar). |
| 2 | F1 | Olhe o teto e a parede de onde veio | Os dois estão fechados por raízes: não dá para sair. |
| 3 | F1 | Entre a luta | Ela some na terra e um montinho de terra corre direto até onde vai sair (rápido de longe, devagar perto) pelas bordas (chão, parede, teto, parede), rachando, soltando terra (do teto, cai) e tremendo a tela. Dá para acompanhar com os olhos. **O montinho não fere** (18/11/2026): passar por cima dele não tira vida. |
| 3a | F1 | A arena | Sem plataformas de pedra: galhos de madeira (mão única), galhos podres que caem ao pisar e voltam em 5 s, e 4 ganchos: os dois lados de cima e as duas pontas do galho do topo (o do meio, embaixo do galho podre de cima, saiu em 18/11/2026). Os vãos pedem dash. Dá para subir até a saída de cima (depois da luta). |
| 3c | F1 | Mais agressiva (18/11/2026) | Ela emenda golpes já na fase I (3 seguidos; 4 na II, 5 na III), quase sem respiro entre eles, e as esperas cansadas são curtas (~1 s). A mordida tira um coração (o Cuspe, meio; o fim da combinação da fase III, um e meio). **Só a boca fere**: se a boca encostar, tira vida, com ou sem dash. O Cuspe solta 5 bolas, mais rápidas. |
| 3d | F1 | Corpo sólido (18/11/2026) | Encostar no corpo não fere (só a boca fere), mas o corpo e a cabeça são sólidos: o gato não atravessa (nem com o dash, que para nela como numa parede). Dá para pular por cima e até pisar no corpo deitado. Quando ela passa, empurra o gato. As bolas do Cuspe e as poças não tratam o corpo dela como chão. |
| 3e | F1 | O corpo nunca sai inteiro (18/11/2026) | Em todos os golpes (bote do chão, avanço da parede, descida do teto até o chão, Cuspe), a ponta do rabo fica sempre dentro do buraco: o corpo todo nunca aparece solto na arena. Ela ficou mais comprida (32 tiles), então a descida do teto chega ao chão com o rabo ainda no teto. No Arco, o meio do corpo fica embaixo do chão. |
| 3b | F1 | Ela mira em você | Ao se armar, a cabeça encara o gato e treme; o bote e o avanço vão **onde o gato estava 0,38 s antes** (0,32 s na fase II, 0,27 s na III); a mordida alcança 1,8 tile: parado, ela acerta; dando o dash (ou saindo do lugar) na hora da mira, ela erra. A descida do teto segue o gato pairando (a sombra vai junto) e trava pouco antes de cair. Na espera, cansada, ela continua olhando para o gato. |
| 4 | F1, fase I | Bote (do chão) | O montinho para num vão perto do gato (nunca embaixo dele); o chão racha e brilha roxo (~0,85 s). Ela sai, se arma, mira com os olhos piscando (~0,7 s), dá o bote (meio coração) e cai com o queixo no chão, cansada (~1,9 s, soltando bafo): bata na cabeça. Volta devagar para a terra. |
| 5 | F1, fase I | Avanço (da parede) | A parede perto do gato racha na altura dele. Ela sai de lado, mira e avança reto (até ~11 tiles; meio coração), depois fica caída, pendurada da parede (~1,7 s). |
| 6 | F1, fase I | Descida (do teto) | O teto racha em cima do gato (num vão sem plataforma), caem pedrinhas e a sombra cresce no chão (~1 s). Ela desce até meia altura, balança com a boca aberta e cai mordendo até o chão (um coração). Fica pendurada com a cabeça perto do chão (~2,1 s): bata. |
| 7 | F1 | Bata nela | Fora da terra, o corpo todo leva dano (do pescoço ao rabo). Na espera cansada, a cabeça leva 1,25×. Só a casca da praga faz o golpe resvalar ("tink" roxo). |
| 8 | F1 | Pogo | Funciona na cabeça sempre que ela está fora da terra (e no rabo, no Arco). |
| 9 | F1 | Leve a vida a 65% (324 de vida no total) | Ela para o que está fazendo (ou sobe do vão do meio), ergue a cabeça e lança o feitiço: um círculo de runas roxas gira e cresce em volta dela, runas sobem do corpo, e no estouro um escudo de placas roxas translúcidas cobre o corpo todo, pulsando. O gato diz "O que ela está fazendo?!" e depois "Não entra nada!...". A Vulpis cai do alto no galho lateral do lado do gato, pousa e fala duas vezes ("Segura ela, gato!..." e "Só a magia da floresta atravessa isso..."), animada na pose pronta (arco baixo, magia verde juntando na flecha). Por uns 4,5 s, todo golpe dá "tink" com faíscas roxas, mesmo nas janelas. |
| 9a | F1 | Espere a casca durar | Ela sobe longe da Vulpis e ruge. O jogo pausa, o resto da tela escurece e a câmera vai até a Vulpis com zoom. Ela agacha, pula e puxa o arco no ar, com a magia verde se juntando na ponta da flecha, e atira. A câmera segue a flecha até a cabeça; o escudo estoura: cada placa vira cacos roxos, com um clarão verde. Depois vem o zoom out, a câmera volta para o gato, a Vulpis desce para o galho, grita "Agora, gato!" e vai embora pulando (some). A cabeça desce de olhos fechados por uns 5,5 s e leva 1,5× o dano. |
| 9c | F1 | Depois da flecha forte | Da cabeça dela cai um potinho de mana (frasco azul com rolha, soltando faíscas). Ele pousa no chão; encostando, os 3 lacres da Cura do Paladino acendem de novo ("Mana! Os lacres da cura acenderam de novo."). Com os lacres cheios, ele fica no chão até precisar (some em 30 s). Acontece nas duas flechas (65% e 30%). |
| 9d | Qualquer fase | Mate inimigos | Todos (templários, zumbis e os da Floresta) soltam meio coração com 15% de chance e o potinho de mana com 10%. Antes de ganhar o Paladino (a primeira fase, na Fortaleza), o potinho não cai. |
| 9b | F1 | Na cena da flecha | O gato não se mexe nem leva dano; a luta não anda (poças e cuspes parados). Nada fica preso depois: o controle volta, o zoom volta ao normal e a câmera segue o gato. |
| 10 | F1, fase II | Observe | Ela fica num tom mais roxo e ganha: o **Arco** (a cabeça sai de um vão e o rabo do vão vizinho, do lado de longe; ficam ~3,4 s; o rabo balança e leva dano sempre; a cabeça morde curto se o gato chegar perto, com a boca abrindo antes) e o **Cuspe** (sai da parede de longe, junta a praga na boca e cospe 3 bolas em arco; cada uma vira uma poça de veneno no chão por ~5,5 s, meio coração ao pisar; some piscando). Sem nuvens de fumaça. O corpo nunca sai inteiro da terra. |
| 11 | F1 | Leve a vida a 30% | A casca de novo e a segunda flecha forte, com a fala da praga tomando a guardiã. |
| 11a | F1 | Morra durante a casca | A luta reinicia sem casca. Se for no meio da cena, o tempo, a câmera e o controle voltam ao normal. |
| 11b | D5 e conversas | Fale com a Vulpis | Raposa de capuz verde (sem lanterna), animada; mexe a boca nas falas dela. Retratos novos: sério, irônico, triste e sorriso. |
| 11c | HUD | Com o Caçador | O coração do Caçador brilha em verde claro (folhas de luz girando devagar). O do Paladino continua dourado. |
| 12 | F1, fase III | Observe | Anda mais rápido pelas bordas e emenda 2 golpes (o segundo, depois de andar só um pouco, tira um coração). A descida do teto solta uma onda pelo chão (pular passa). Os avisos e as esperas ficam um pouco mais curtos, mas continuam legíveis. |
| 13 | F1 | Morra na luta | Volta ao santuário S-F4, na E6 (logo antes da arena). A luta recomeça do zero ao entrar de novo. |
| 9e | F1 | A primeira vez que a Vulpis chega (65%) | Ela cai na plataforma do lado do gato e a conversa abre **com retrato** (D-ISYATHY-65: "Segura ela, gato!"): o jogo pausa até o fim. Na segunda vez (30%), ela fala em balões, sem pausar. |
| 14 | F1 | Vença | Último rugido; ela se debate e afunda na terra (a terra voa). Silêncio. O chão racha perto do gato (nunca embaixo dele), a cabeça sai devagar, fraca, com o brilho dos olhos falhando, balança, desaba de lado com o queixo no chão (para longe do gato) e os olhos apagam e fecham. Só a cabeça e o pescoço ficam para fora. As falas são **conversas com retrato** (o jogo pausa; avança com o botão): a derrota (D-ISYATHY-DERROTA); o espírito sobe do corpo, a serpente inteira de luz azul-clara em S, com plumas e tiara e o rabo se desfazendo em fiapos, olhando para o gato (não é uma cabeça flutuante); ele conversa com o retrato dele (D-ISYATHY-ESPIRITO-01 e 01b), o corpo de carne some na terra, o espírito se dissolve e vem a despedida da Vulpis (D-ISYATHY-FIM). A Vulpis fica. |
| 15 | F1 | Depois da luta | As raízes do teto e da parede oeste somem. Pelo teto, a subida G1–G3; pela parede, a volta para a E6. |
| 16 | E6 → E5 → E4 | Volte pela fase | Da E6, desça o zigue-zague da E5. Na parede da esquerda há um cipó: suba, atravesse o galho e entre na E4. |
| 17 | E4 → E1 → D4 | Continue voltando | Dá para voltar até a D4 (a E2 e a E3 descem; o cipó da E2 leva à E1). |
| 18 | Ninho E1–E3 | Depois da luta | Ela não ataca mais de baixo da terra. A cratera continua. |
| 19 | Floresta | Depois da luta | Os olhos dela não aparecem mais no fundo. |

## O que observar

- A luta lê bem? Dá para entender de onde ela vai sair?
- As janelas são longas o bastante para 2 ou 3 golpes?
- Com 54 de vida, a luta dura uns 4 a 6 minutos?
- O desempenho: o corpo é redesenhado a cada quadro durante a luta.
