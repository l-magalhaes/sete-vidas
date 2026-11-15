# Plano de teste: Floresta F5, parte 2 (ruínas, placas, Diário e a Isyathy de longe), 14/11/2026

O que mudou:
- 8 placas dos Primatas que se leem, cada uma com desenho próprio.
- Objetos das ruínas: postes, máquinas com luz verde, janelas, escadas, relógio.
- **Cabine secreta na Estação (A5)**.
- **Diário** com as placas lidas de qualquer cena.
- A **Isyathy vista de longe** em 5 salas.

Som: nada novo (tudo na F10).

Antes:
1. Rode `powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar.ps1`. Já está rodado: os PNG `pri_*` e `isy_*` estão em `Art/Environments/Forest/Placeholder`. Volte ao Unity e espere a importação.
2. Rode **Tools > Sete Vidas > 21. Floresta: graybox (todas as cenas)**.
   - O console não pode ter "falta pri_…/isy_…" nem "Placa sem texto".
   - Precisa ter "Diário: catálogo com N placas" (N = as da Fortaleza + 8).
3. Rode o **22. Mapa do mundo**: a passarela e a cabine da A5 mudaram o terreno.
4. Se você só rodar a Fortaleza (13), o catálogo também é refeito. Sozinho: **21c. Diário: catálogo de placas**.

Sem o Unity:
- `Tools/LevelPreview/checar_floresta.ps1`: tudo alcançável, sem softlock (conferido).
- `Tools/LevelPreview/sala_ascii.ps1 Forest_A_Roots A5`: a passarela e a cabine.
- Prévia da arte: `SeteVidasDocs/Referencias/floresta_ruinas_f5.png`.

## Testes automáticos

- EditMode > `ForestRuinsTests` (5):
  - a arte existe;
  - a Isyathy não aparece depois de morta, a de D4 fica sempre e as outras aparecem uma vez por visita;
  - o lago só some com o gato agachado no chão;
  - a respiração da D4;
  - o catálogo não duplica placas.
- Rode também os da Floresta (`ForestFoundationTests`, `ForestGrayboxTests`, `ForestBestiaryTests`, `ForestSceneryTests`, `ForestHunterTests`).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | A2 (em cima do arco) | Chegue perto da placa | Placa de metal verde num poste, com seta e o desenho de um carro. Aparece "E: ler" acima dela. Ao ler: P-PRIM-01 ("Uma carroça que anda sozinha? Ok.") |
| 2 | Diário | Abra o inventário > Diário | Capítulo 2 · A Floresta Silenciosa, com "Placa da estrada" |
| 3 | A5 (plataforma) | Leia o painel | Mapa de linhas coloridas em pé, sobre dois pés: P-PRIM-02. A bilheteria ao lado tem **uma** luz verde piscando devagar |
| 4 | A5 | Olhe o alto da estação | Relógio parado pendurado do teto; passarela de concreto no alto, à esquerda, com uma cabine fechada por blocos de pedra com musgo. **Não dá para ver dentro da cabine** |
| 5 | A5 | Do teto do vagão A (cuidado com a serra), pule para a passarela | Dá para alcançar (3 de altura, 1 de vão). Outra máquina com luz verde na ponta |
| 6 | A5, cabine | Bata nos blocos com a marreta (Paladino); depois tente com a adaga | A adaga não quebra. A marreta quebra e a cabine aparece: mesa, caneca, a lista na parede, **2 moedas de 5**. Leia: P-PRIM-08 |
| 7 | A5 | Saia da cena e volte | Os blocos continuam quebrados; as moedas pegas não voltam |
| 8 | A4 | Fique na sala (3–7 s) | De um dos lados da tela, **lá no fundo**, atrás das árvores e da névoa, dois olhos pequenos acendem, piscam e somem. Na 1ª vez, balão "..." e "Não. Eu não vi nada…". Nada fere o gato |
| 9 | A4 | Continue na sala | Os olhos voltam **de vez em quando** (a cada 22–40 s), num lugar diferente; a fala não repete |
| 10 | B4 | Leia a luz na viga alta | Cúpula verde: acesa, acesa, apagada (≈ 0,6 s cada). P-PRIM-04. Escada quebrada no fundo, na viga de baixo; máquina no pé da torre (não cobre o santuário) |
| 11 | B4 | Fique na torre | De vez em quando um trecho do corpo, pequeno, **atravessa o fundo** atrás das copas (~9 s), entrando e saindo devagar. Balão D-SILHUETA-01 na 1ª vez |
| 12 | C3 | Leia a vitrine (andar 1) | Vidro com reflexo de lua; cadeiras viradas para a parede clara. P-PRIM-03. Janelas nos andares mostram **terra e raízes**, nunca um buraco escuro que pareça passagem |
| 13 | C4 | Fique na caverna | Olhos pequenos no fundo, entre as árvores, piscando |
| 14 | C4 | Com os olhos à vista, agache (parado) | Somem rápido (o gato se escondeu); de pé, somem devagar depois de ~4 s |
| 15 | C5 | Vá até o entulho da direita | Carroça de metal enferrujada em cima do entulho: P-PRIM-06. Postes de pé e um caído, atrás dos inimigos |
| 16 | C6 | Fique na Ravina | **O pescoço sobe** entre as árvores do fundo (pequeno, escuro como o fundo), abre os olhos, olha, se mexe de lado, pisca e afunda de novo. Balão "...Raiz não se mexe assim." na 1ª vez |
| 17 | D4 Mirante | Fique na sala e ande | A mesma animação do pescoço, um pouco maior (mais perto). Ande durante a animação: ela anda com o **paralaxe do fundo** (devagar), nunca parece solta no ar. Nada fica parado à vista entre uma aparição e outra. Balão D-MIRANTE-01 na 1ª vez |
| 18 | D6 | Perto da entrada | Lápide de pedra com primatas olhando para cima: P-PRIM-07 (no lugar do aviso "ruínas dos Primatas") |
| 19 | G2, topo da coluna | Leia | Painel do elevador com botões: P-PRIM-05 |
| 20 | Diário, em outra cena | Leia placas em A, C e G, depois abra o Diário na D | **Todas** as placas lidas aparecem no capítulo 2, na ordem do caminho (da esquerda para a direita no mapa) |
| 21 | Diário, Fortaleza | Volte à Fortaleza (se der) ou leia uma placa lá | O capítulo 1 continua com as placas da Fortaleza; o 2 mostra as da Floresta lidas |
| 22 | Save com `isyathy_morta` | Ponha a flag no save de teste e entre em A4, C4, D4 | Nenhuma aparição (o espírito é da F8) |
| 24 | E2 (paredão de vinha) | Suba pela vinha até a sombra do alto | A árvore que faz a sombra tem o **tronco até o chão** (antes flutuava) |
| 23 | Cores | Olhe as máquinas e a luz da torre | O verde das luzinhas é pequeno e não se confunde com o turquesa do pogo nem com o verde dos olhos da praga |

### Portões (14/11/2026, pedido do usuário)

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 25 | D4, Portão do Templo, **sem** a chave | Tente passar por cima: pule, quique no cacto, agarre a borda | Não passa: a parede vai do alto do portão até o teto. Só a chave abre |
| 26 | D4, portão | Olhe | Portal de pedra do templo: dois pilares com musgo, a verga com a cabeça da serpente e olhos turquesa **na frente da parede**, o portão de pedra com a serpente em relevo e a fechadura turquesa |
| 27 | D4, com a chave | Abra (E) | O portão some no chão (sobra a borda rente ao piso); a verga e os pilares ficam |
| 28 | D3, arena | Entre na arena | As grades descem: barras de ferro grossas com travessas rebitadas, ferrugem e pontas embaixo; sobem para dentro da parede ao vencer |
