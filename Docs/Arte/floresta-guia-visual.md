# Floresta Silenciosa — guia visual (F0)

> Paleta: `paleta_floresta.gpl` (GDD §3.1). Regras do jogo continuam valendo: 480 × 270, tile 16 px, PPU 16, gato de 22 px,
> fundo noturno abaixo de 25% de valor para o capuz não sumir. Status: **[PROPOSTA]**.

## Cores reservadas (só para gameplay)

| Cor | Significado | Nunca usar em |
|---|---|---|
| Vermelho #c83a3a | armadilha (como na Fortaleza) | cenário comum |
| **Turquesa #5fd3c7** (+ brilho #b9f5ec) | **pode quicar aqui (pogo)** | fungos, água, magia decorativa (use o azul #58c4e8 e o fungo #348ca0) |
| Amarelo #ffd34d / #8a621e | **rachadura de tronco quebrável** (como a parede rachada) | casca comum, flores |
| Roxo #5b2a6e e verde-ácido #8fd14f | **praga** | vegetação saudável |
| Âmbar #e8a640 | refúgio / lugar seguro (santuário, Refúgio, lanterna da Tobi) | inimigos |

## Leituras que o jogador precisa fazer em menos de 1 s

| Pergunta | Resposta visual |
|---|---|
| Posso quicar? | ponta com **gota turquesa** que pulsa (1 s). Espinho comum: ponta escura, sem brilho |
| Esse tronco quebra? | rachadura **amarela em zigue-zague** + lascas no chão. Tronco comum: casca lisa |
| Posso me esconder? | folhagem alta 2 tiles (15 px de altura, como o matinho): agachado (14 px) some |
| É infectado? | olho verde-ácido + rachaduras roxas/verdes que brilham |
| É seguro? | luz âmbar |
| A serpente está acordando? | o olho no fundo abre (amarelo-esverdeado), escamas contraem, galhos tremem |

## Camadas de fundo (parallax)

| Camada | Conteúdo | Valor |
|---|---|---|
| 0 Céu | degradê índigo #0c0e20 → #262c58, lua #e2e8f0 com halo | 5–15% |
| 1 Silhuetas distantes | copas recortadas #121e1e | 10% |
| 2 Árvores médias | troncos #1a1412, folhas #1e3028; fachos de luar diagonais (alfa 0,15) | 12–18% |
| 3 Próximas | troncos grandes #34261e, raízes, ruínas dos Primatas | 18–24% |
| Névoa | faixa rasteira alfa 0,2 na base da tela | — |
| Partículas | vagalumes #c8f0a0, esporos (áreas da praga), folhas caindo | — |

No ninho, o **corpo da Isyathy** fica entre as camadas 2 e 3 (segmentos de escamas #2e4834 com ventre #52664c), primeiro lido como raiz.

**Isyathy de longe** (antes do ninho): só olhos amarelo-esverdeados (2 × 2 px cada, brilho suave) ou a silhueta do corpo uma camada mais escura que as árvores do fundo. **Espírito** (depois da morte): cabeça e pescoço translúcidos (alfa 0,6) em azul-claro da magia natural #58c4e8 com núcleo branco #e2e8f0 e brilho dourado suave (nunca o turquesa do pogo), olhos âmbar #e8a640 calmos; partículas de vagalume em volta. Morta, a cabeça do corpo fica cinza e opaca.

**A ponte no fundo**: silhueta da ponte quebrada na camada 1, posicionada pela altura da sala: quanto mais alto o gato está, maior e mais nítida ela fica. É o marcador de progresso da fase.

## Tamanhos dos sprites novos

| Asset | Canvas | Observação |
|---|---|---|
| Vulpis | 32 × 32 (24 px de altura) | um pouco maior que o gato; arco claro para ler contra o fundo |
| Retrato Vulpis | 48 × 48, olhando para a esquerda | 4 expressões |
| Tobi | 24 × 24 (14 px de altura) | menor que o gato; chave inglesa de 28 px nos quadros cômicos (canvas 48 × 32) |
| Retrato Tobi | 48 × 48, olhando para a esquerda | 4 expressões |
| Cabeça da Isyathy | 96 × 64 | cabe na tela de 270 px de altura com folga; pescoço em segmentos de 32 × 32 |
| Corpo da Isyathy | segmentos de 48 × 48 em sequência | no fundo, sem colisão |
| Tronco rachado | 32 × 64 (2 × 4 tiles) | 4 estados |
| Baú | 16 × 16 | madeira com ferragem de ferro; aberto/fechado |
| Madeira (ícone) | 12 × 12 | tábua clara com veio |
| Santuário da floresta | 32 × 32 | estátua de gato coberta de musgo, velas em cogumelos âmbar |
| Ponte de pedra | módulos de 16 × 16 | intacta, rachada, blocos caindo |
| Ponte de madeira | módulos de 16 × 16 + corrimão de corda | corda pendurada de 1 × N tiles |

## Ruínas dos Primatas

- Linhas **retas e ângulos de 90°** (nada de pedra talhada à mão): contraste com o orgânico da floresta e com a pedra da Fortaleza.
- Concreto #3c4042 / #626868, vidro #36505c com reflexo de lua, ferrugem #623e2c, estrada negra #222226 com faixa gasta.
- Sempre com **musgo, raiz ou trepadeira por cima**: nada fica limpo.
- Máquinas com **uma** luz pequena (verde, piscando devagar): misterioso, nunca colorido demais.
- Ruína **com segredo** = tem baú, alavanca ou tronco rachado visível na entrada. Ruína **só de cenário** fica atrás dos personagens (ordem −5) e sem entradas escuras que pareçam passagem.
