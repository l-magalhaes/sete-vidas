# Floresta — melhorias de 19/11/2026: plano de teste

**Antes:** abra o Unity e espere compilar. Os sons já foram gerados (`Tools/Audio/gerar_sfx.ps1` e `Tools/Audio/importar_vozes.ps1`). Rode **Floresta: gerar as cenas** (`ForestLayout` v41). Rode os EditMode do Test Runner (`ForestPolishTests`, 12 testes, e os antigos `NestBurrowTests`, `IsyathyBossTests`, `TobiTests`, `SfxRulesTests`).

## 1. Colmeia no Broto
| # | Passo | Esperado |
|---|---|---|
| 1.1 | Chegue perto do Broto do D3 (o de cima, x 54) | Uma colmeia pendurada no alto do tronco, balançando de leve |
| 1.2 | Acorde o Broto | A colmeia treme forte ~0,6 s antes de cada vespa; a vespa sai da boca da colmeia, com lascas de cera e o som `colmeia_solta` |
| 1.3 | Derrube o Broto | A colmeia tomba junto com o tronco e some com ele |
| 1.4 | Morra e volte | A colmeia volta inteira |

## 2. Arena do D3
| # | Passo | Esperado |
|---|---|---|
| 2.1 | Mate a 1ª leva | Faíscas e o aviso grave; os da 2ª leva aparecem pequenos, escuros e mais altos, **atrás do cenário**, e crescem num pulo até o lugar deles, ganhando cor; poeira e um baque ao pousar |
| 2.2 | Durante a chegada | Eles não ferem, não levam dano e a arena **não** abre |
| 2.3 | Conte as vespas | 6 vespas na 2ª leva (eram 3), mais as do Broto; chegam já caçando e a colmeia delas aparece aos poucos no teto |
| 2.4 | Morra no meio da 2ª leva | A arena reabre; ao voltar, as levas funcionam de novo (nada some de vez) |

## 3. Tobi: a lojinha
| # | Passo | Esperado |
|---|---|---|
| 3.1 | Encontre a Tobi (o encontro sozinho) e depois aperte E | D-TOBI-06: o convite para a lojinha em Miaravéu |
| 3.2 | E de novo | As falas de sempre (faltam tábuas/moedas, a reconstrução ou o rodízio); o convite não repete |

## 4. O espírito da Isyathy pela floresta
| # | Passo | Esperado |
|---|---|---|
| 4.1 | Antes de vencer a Isyathy, vá a A4, B4, C4, C6, D4 | Nenhum espírito (só os olhos lá no fundo) |
| 4.2 | Depois da luta, entre numa dessas salas | O espírito aparece devagar no primeiro plano (A4 na prateleira do alto; B4 na viga do meio; C4 no patamar do santuário; C6 perto do S-F2; D4 no meio do Mirante), flutuando e soltando fiapos, olhando para o gato; na 1ª vez o Lion diz "Os olhos dela. Agora calmos." |
| 4.3 | E perto dele | 02a, depois 02b, 02c, 02d (em qualquer sala); depois em rodízio. A 02e só depois da ponte nova |
| 4.4 | Saia da sala | Ele some devagar |

## 5. A luta: falas e corrupção
| # | Passo | Esperado |
|---|---|---|
| 5.1 | Fim da apresentação | D-ISYATHY-00 com retrato (2 falas do gato) |
| 5.2 | 65% | Ela lança a casca **sem** balão; a Vulpis chega e abre D-ISYATHY-65 (começa com o gato: "O que ela fez?!"). Depois da flecha forte, D-ISYATHY-65b ("Agora, gato!") |
| 5.3 | 30% | Mesma coisa com D-ISYATHY-30 e D-ISYATHY-30b |
| 5.4 | Troca de fase (o rugido) | As manchas roxas **crescem** no corpo e a sombra roxa avança pela cabeça, com uma onda de faíscas; na fase III quase todo o corpo roxo, com veias acesas e a sombra até o queixo |
| 5.5 | Morra e recomece | Ela volta ao estágio da fase I |

## 6. F6: o resgate da Vulpis (D2)
| # | Passo | Esperado |
|---|---|---|
| 6.1 | D1 e D2 | Flechas dela (penas verdes) cravadas no tronco e no chão; vespas abatidas de costas |
| 6.2 | Entre na D2 pela D1 | Um Galho Seco infectado larga o disfarce na frente; o controle sai; "...Isso não é um tronco."; a Vulpis cai das folhas no galho do meio, toca o motivo dela, puxa o arco; a flecha verde derruba o tronco (ele tomba) |
| 6.3 | A conversa | D-VULPIS-01 com retrato e voz; ela some pelo alto; o controle volta. O gato não levou dano na cena |
| 6.4 | Saia, morra ou descanse e volte | A cena não repete e o Galho da cena não volta |

## 7. Som (F10)
| # | Passo | Esperado |
|---|---|---|
| 7.1 | Ande de A até D | Ambiência viva (vento, grilos, gotas; coruja de vez em quando); nas ruínas (A5, B4, C3, C5), goteiras em metal e o bip da máquina; na C4, sapos; a troca entre salas é suave |
| 7.2 | Inimigos | Cada um com o seu som: vespa (zumbido ao acordar, voo em loop, investida), galho (estalo de madeira antes da pancada), planta (chiado antes da mordida), serpente (sibilo antes do bote). Os infectados soam mais graves e rasgados |
| 7.3 | Tronco rachado e baú | Golpe grave de madeira, quebra forte; baú com rangido e o jingle da madeira |
| 7.4 | Pogo | No espinho, um tom cristalino; em inimigo, um baque "de carne" |
| 7.5 | Estrada e ponte | Vento e gemidos; a pedra rachando com a horda em cima; o desabamento e os galhos na queda |
| 7.6 | Ninho | Quase silêncio, cada sala mais quieta; a **respiração** dela, que acelera perto do bote; o tremor (o som mais alto) antes de cada bote; saída da terra, bote, mordida, mergulho |
| 7.7 | Luta | Arrasto embaixo da terra, tremor no aviso, saídas, mordidas, impactos, cuspe e poça, rugidos (com o timbre da praga da fase II em diante), casca e o estouro dela, flecha da Vulpis; na morte, o último rugido e **silêncio** (30 s) |
| 7.8 | Tobi | Cena da ponte com os sons exagerados (chave, tábuas voando, cava, ferramentas, plinks) e o jingle no fim; a ponte range alto |
| 7.9 | Vozes | Vulpis com a gravação Audio-vulpis (mais grave séria, mais aguda sorrindo); Tobi e o espírito com voz própria |
| 7.10 | Música | Sem as faixas da Floresta, a música não muda de repente nem dá erro; o santuário toca o da Fortaleza. Com arquivos `mus_floresta_*` na pasta e o menu **Floresta: som F10**, cada momento troca com crossfade |

**Ajustes prováveis no playtest:** volumes de ambiência (0,18), respiração (0,55), tremor (0,85); duração da chegada da leva (1,1 s).
