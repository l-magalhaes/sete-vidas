# Plano das músicas — Prólogo / Fortaleza (MVP)

**Skill:** `sete-vidas-sound-design` · **Status:** rascunho [PROPOSTA] · **Data:** 12/11/2026
**Faixas do jogo (12/11/2026):** compostas pelo usuário no Treblo ("Castália, Sleeping") e escolhidas por análise (andamento, tom, energia). Ficam em `Assets/_Game/Audio/Music` (WAV, importadas como Vorbis com streaming) e tocam pelo `MusicDirector` (menu 13). Os rascunhos sintetizados de `Tools/Audio/` vão para `Tools/Audio/rascunhos/`.

| Papel | Faixa | Arquivo |
|---|---|---|
| MUS-01 Menu | Citadel of Light | `mus_tema_principal` |
| MUS-02 Montanha | (sem faixa própria: o início usa a música da fase, pedido do usuário) | — |
| MUS-03 Fase | The Golden Citadel | `mus_fortaleza_base` |
| MUS-04 Combate | The Palace Daggers | `mus_fortaleza_combate` |
| MUS-05 Furtividade | Infiltrating the Golden Hall | `mus_fortaleza_furtivo` |
| MUS-06 Chefe | Koda, Golden Paladin (loop até 150 s; o resto é o desfecho na derrota) | `mus_chefe_koda` |
| MUS-07 Maldição | A Plague Sealed Under Stone | `mus_maldicao` |
| MUS-08 Santuário | Sanctuary Within the Fortress | `mus_santuario` |

Como são faixas separadas (tons diferentes), fase, furtividade e combate trocam por crossfade (0,5 s para entrar no combate, 2,5 s para sair; 1,5 s na furtividade), não por camadas. O resto do documento é o plano original, que vale como referência de clima.
**Pilares:** o som é informação (3, 4, 5) · dois climas, uma identidade (todas) · memória de jogo antigo (todas) · o silêncio é um instrumento (2, 6, 7)

## Visão geral

| ID | Música | Quando toca | Clima | Andamento | Tom e modo | Forma |
|---|---|---|---|---|---|---|
| MUS-01 | **Sete Vidas** (tema principal) | Tela inicial | Contemplativo épico | 72 BPM, 3/4 | Ré dórico | Loop de ~100 s |
| MUS-02 | **O Penhasco** | Começo da fase, no alto da montanha, até o pulo | Contemplativo | 64 BPM, livre | Ré lídio | Loop de ~75 s + "o pulo" |
| MUS-03 | **A Cidadela do Sol Eterno** | A fase, fora de combate | Aventuresco | 116 BPM, 4/4 | Ré eólio | Camada base, loop de 8+8 compassos |
| MUS-04 | **Lâminas na Muralha** (combate) | Uma Sentinela "Em combate" | Aventuresco forte | 116 BPM, 4/4 | Ré eólio | Camada de combate (mesmo loop) |
| MUS-05 | **Nove Sombras** (furtividade) | Sentinela suspeita/caçando, ou gato escondido perto de uma | Tenso contido | 116 BPM (sensação de 58) | Ré eólio | Camada furtiva (mesmo loop) |
| MUS-06 | **O Guardião Dourado** (chefe) | Duelo com o Koda | Aventuresco nobre | 132 BPM, 4/4 | Fase 1: Ré menor · Fase 2: Mi menor | 2 loops + entrada + final |
| MUS-07 | **A Sétima Ruína** (maldição) | Ao tirar o tomo do pedestal, as vidas sendo consumidas | Opressivo | 60 → 40 BPM (ritardando) | Ré frígio | Drone + 7 golpes (um por vida) |

**[PROPOSTA] As músicas 3, 4 e 5 são uma música só em três camadas.** Mesmo andamento (116), mesmo tom e a mesma progressão de 8 compassos. Assim o jogo troca de exploração para furtividade e para combate **sem corte**: a camada nova sobe de volume no tempo forte seguinte, e o jogador sente a mudança sem ouvir a "emenda". É o padrão de metroidvania moderno com som de console antigo.

---

## O material comum: o motivo das sete vidas [PROPOSTA]

Toda a trilha nasce de **7 notas**. Elas aparecem inteiras no menu, em pedaços na fase, e são "comidas" uma a uma na maldição.

```
Motivo (Ré dórico), compasso 3/4:
| Ré4 (mínima)  Lá4 (semínima) | Dó5 (sem. pontuada)  Si4 (colcheia)  Lá4 (semínima) |
| Sol4 (mínima)  Lá4 (semínima ~ ligada) | Lá4 (mínima pontuada) |
  1               2                3                    4                 5
  6              7
Acordes:  Rém  |  Fá  |  Sol  |  Lá(sus4 → Lá)
```

- **Termina no Lá, sem voltar ao Ré:** é um tema **sem resolução**. O gato perdeu as vidas; a frase fica "em aberto" o jogo todo. **Só no final do jogo, com os 7 corações, a frase sobe para o Ré5 e resolve.** (Recompensa de música para quem terminar.)
- **O Si natural é a "esperança"** (cor do dórico). Na maldição, ele vira **Si bemol**: a praga literalmente estraga uma nota do tema.
- **Ritmo de assinatura:** longa–curta | longa-curtíssima-curta. Dá para reconhecer o motivo só batendo palmas, então ele funciona até na percussão do combate.

Motivos de apoio (curtos, para combinar com o principal):
- **Lion / Guilda:** pizzicato saltitante em colcheias, `Ré-Fá-Lá-Sol-Fá-Mi-Ré` em staccato. Irônico, ágil.
- **Koda:** metais em **quintas paralelas** (`Ré+Lá → Dó+Sol → Si♭+Fá → Lá+Mi`), largo. Nobre e pesado.
- **Capivara Necromante:** cromática descendente no grave (`Ré-Dó#-Dó-Si`), sempre no **timbre da praga**.

---

## MUS-01 — Sete Vidas (tema principal, menu)

**Função:** a primeira coisa que o jogador ouve. Diz "aventura com saudade": um gato sozinho olhando a Cidadela no pôr do sol (a tela inicial). Tem que dar vontade de apertar "Novo jogo" e ficar na cabeça.

- **Andamento / compasso / tom:** 72 BPM, 3/4 (balanço de valsa lenta), Ré dórico.
- **Duração do loop:** ~100 s (30 compassos), loop no compasso 5 (pula a introdução).
- **Instrumentos (8 vozes):** harpa (arpejos), flauta (melodia), cordas suaves (pad), pizzicato (Lion), trompa (Koda/Cidadela), sino da torre, tímpano raro, caixinha de música (só no fim).
- **Estrutura:**

| Compassos | Seção | O que acontece |
|---|---|---|
| 1–4 | Introdução | Só harpa em arpejo de Rém + vento. Sino da torre no compasso 1 (longe, com eco) |
| 5–12 | A | Flauta toca o **motivo inteiro** (2×). Cordas entram baixo |
| 13–20 | B | Pizzicato com o motivo do Lion por baixo; acordes `Si♭ – Dó – Rém – Lá`; a flauta responde |
| 21–26 | A' (clímax) | Trompa + cordas tocam o motivo uma oitava abaixo, tímpano no 1º tempo. Ponto mais forte (mf–f) |
| 27–30 | Final em aberto | Caixinha de música toca o motivo **só até a 6ª nota** e para. Silêncio de 1 compasso, volta ao 5 |

- **Ligação com a tela:** as 7 centelhas da tela inicial cintilam; quando uma se apaga, um **sino agudo e suave** (SFX de interface, não da música) toca no tom de Ré, para combinar com qualquer ponto da faixa.
- **Transição:** "Novo jogo" ou "Continuar" → a música cai em 1,5 s junto com o escurecimento da tela.
- **Referência de sensação (sem copiar):** a abertura melancólica de um metroidvania de 16 bits + a valsa de caixinha de música de um RPG antigo.

## MUS-02 — O Penhasco (montanha, antes do pulo)

**Função:** respiro antes da missão. O jogador aprende a andar ouvindo o vento; a Cidadela está lá embaixo. Calma, mas com a expectativa do que vem.

- **Andamento / tom:** 64 BPM, sem marcação forte (rubato), Ré lídio (o Sol# deixa "no alto", leve).
- **Duração do loop:** ~75 s, com **2 pausas de ~10 s só de vento** dentro do loop.
- **Instrumentos (4 vozes):** pad suave, harpa espaçada, flauta (só as 3 primeiras notas do motivo, `Ré–Lá–Dó`, como quem lembra de algo), sinos de vento.
- **Ambiência junto:** vento forte e frio no penhasco, pássaros longe. **A ambiência é mais alta que a música** (−6 dB a música).
- **Transição — "o pulo" [PROPOSTA]:** quando o gato entra na área da dica "salto" (a corrida para o abismo), a música segura o acorde; **no pulo**, um *stinger* de 2 s (glissando de harpa subindo + prato em crescendo) e, **na aterrissagem**, entra a MUS-03 no primeiro tempo. O jogador sente que "a missão começou".

## MUS-03 — A Cidadela do Sol Eterno (fase, fora de combate)

**Função:** infiltrar-se numa fortaleza solene e perigosa. Precisa manter o ritmo de aventura por muito tempo sem cansar, e deixar espaço para os SFX da furtividade (passos, vidro, sinos).

- **Andamento / compasso / tom:** 116 BPM, 4/4, Ré eólio.
- **Progressão (comum às camadas 3, 4 e 5), 8 compassos:** `Rém | Dó | Si♭ | Dó | Rém | Fá | Sol | Lá7`, depois a variação `Si♭ | Dó | Lá/Dó# | Rém` (+4).
- **Duração do loop:** 16 compassos ≈ 33 s, com uma 2ª parte de 16 (≈ 66 s no total) para não repetir rápido.
- **Instrumentos (6 vozes):** órgão de pulso (ostinato em colcheias, o "relógio" da fortaleza), cordas em staccato, baixo de pulso, caixa militar suave (marcha da Ordem), clarinete (melodia, voz do Lion), metal dourado em notas longas (a Ordem da Presa Dourada).
- **Motivos usados:** pedaços do motivo das sete vidas no clarinete; o pizzicato do Lion nas pontes.
- **Mixagem:** melodia esparsa (2 compassos tocando, 2 calados), para os passos e o "?" da Sentinela aparecerem. Sem agudos fortes entre 2 e 5 kHz (é a faixa dos SFX do gato).
- **Ambiência junto:** pátio aberto = vento nas muralhas e bandeiras; corredor = eco curto e tochas.
- **Santuário:** a camada cai em 2 s e entra o tema de Sethra (fica para MUS-08, ver "Faltando" abaixo).

## MUS-04 — Lâminas na Muralha (combate)

**Função:** o gato foi visto. Urgência, mas sem atrapalhar ouvir o telégrafo da Sentinela (o som do golpe antes do golpe).

- **Mesmo andamento, tom e progressão da MUS-03.**
- **Instrumentos (8 vozes):** tudo da base + tímpano e bumbo nos tempos 1 e 3, metais em ataques curtos no contratempo, arpejo de chip (pulso 12,5%) em semicolcheias, a melodia vira o **ritmo do motivo** nos metais.
- **Regra de legibilidade:** a camada de combate **abaixa −4 dB durante a preparação do golpe da Sentinela** (ducking pelo evento do telégrafo), para o aviso nunca se perder.
- **Entra:** no próximo tempo forte depois do "!" (máx. 0,5 s de espera). **Sai:** quando nenhuma Sentinela está em combate, volta para a base em 4 compassos (~8 s), passando pela furtiva se ainda houver alguém caçando.
- **Fim de combate (vitória):** última Sentinela cai → acorde de Rém em metais + prato, e a base retoma no compasso seguinte.

## MUS-05 — Nove Sombras (furtividade)

**Função:** "prenda a respiração". O jogador está perto de ser pego ou escondido bem ao lado do inimigo. A música encolhe e deixa os sons do mundo na frente.

- **Mesmo andamento e progressão**, mas em **meio-tempo** (sensação de 58 BPM): os acentos caem só nos tempos 1 e 3.
- **Instrumentos (4 vozes):** pizzicato grave (ostinato de 2 notas), bloco de madeira nos contratempos (o "tique-taque"), corda grave sustentada, batida grave suave tipo coração nos tempos 1 e 3. **A melodia e a caixa saem.**
- **Quando toca [PROPOSTA]:**
  - Uma Sentinela está **Suspeita**, **Investigando** ou **Caçando** (o "?" laranja), **ou**
  - o gato está **escondido** (sombra ou cobertura) a menos de 8 tiles de uma Sentinela.
  - Junto: filtro passa-baixa suave (~2 kHz) no barramento de música.
- **Sobe a tensão por estado:** Suspeito = só pizzicato e coração; Investigando/Caçando = + bloco de madeira e corda grave, e o coração acelera para colcheias.
- **Saída:** a Sentinela volta a patrulhar e o gato sai da cobertura → base em 4 compassos.

## MUS-06 — O Guardião Dourado (chefe: Koda)

**Função:** o duelo com o Koda. **Ele não é vilão:** é um guardião honrado cumprindo o dever (e vai virar o primeiro companheiro). A música tem que soar **épica e nobre, quase trágica**, não maligna. O jogador deve sentir respeito pelo adversário.

- **Entrada:** ao entrar na arena, a música da fase some e ficam **3 s só de ambiência** (a câmara, o eco). Então o sino da torre toca 3 vezes e entra a fase 1.
- **Fase 1 (vida cheia → metade):** 132 BPM, Ré menor. Órgão de pulso, metais com o **motivo do Koda em quintas**, cordas em staccato, tímpano, coro "aah" nos acordes longos. Loop de 16 compassos (~29 s).
- **Troca de fase [PROPOSTA]:** quando o Koda chega à metade da vida (fica mais rápido e solta mais raios), um *stinger* de 1 compasso (prato invertido + trovão grave) e a música **sobe um tom, para Mi menor**, com bumbo duplo e o arpejo de chip. É a mesma melodia mais alta e mais apertada: o jogador ouve que a luta esquentou.
- **Ataques com som próprio (SFX, não música):** marretada, salto, cura concentrada (um coro sustentado que **o jogador precisa interromper**: o som avisa), chuva de relâmpagos. A música abaixa −3 dB durante o aviso dos raios.
- **Derrota:** o Koda ajoelha → corte para um acorde de coro sustentado em Ré maior (respeito, não vitória festiva) → silêncio. O pedestal do tomo fica só com a ambiência.
- **Referência de sensação (sem copiar):** chefe de cavaleiro em castelo gótico de 16 bits, com o peso de um duelo e não de um monstro.

## MUS-07 — A Sétima Ruína (as vidas sendo consumidas)

**Função:** o momento mais forte do prólogo. O gato tira o tomo do pedestal, o selo rompe e a sétima ruína **come as vidas, uma a uma**. A música precisa contar isso: **o tema principal perde notas.**

- **Estrutura [PROPOSTA], ligada aos eventos do jogo:**
  1. **O selo rompe:** silêncio de 1 s → batida grave + o **timbre da praga** aparece pela primeira vez no jogo (desafinação lenta, modulação em anel, reverb invertido). Entra um drone grave em Ré frígio (Ré + Mi♭: incômodo).
  2. **Cada vida consumida = um golpe:** um impacto grave + o motivo das sete vidas tocado na caixinha de música, **com uma nota a menos a cada vida** (7 notas, depois 6, 5... até 1). O Si natural vira **Si bemol** desde a 1ª vida perdida.
  3. **Andamento caindo:** cada golpe chega mais devagar (60 → 40 BPM), como um coração cedendo. Uma batida de coração sobe na mixagem entre os golpes e enfraquece.
  4. **A última centelha:** sobra **uma nota só (Ré)**, sustentada e frágil.
  5. **O clarão roxo (a Capivara):** o motivo dela (cromática descendente no grave, timbre da praga) por cima do Ré sustentado. Fala dela com a música quase parada.
  6. **O Koda segura a última vida:** quando ele crava a marreta, os **metais do Koda pegam o Ré** e sustentam em quinta (`Ré+Lá`). A nota que ia morrer é segurada pela música do Koda. Fim em aberto, segue para a cena de diálogo.
- **Implementação:** um drone em loop (`mus_maldicao_drone.ogg`) + 7 golpes curtos (`mus_maldicao_vida_7.ogg` … `_1.ogg`), cada um disparado pelo evento da vida consumida (o `CursedTome` já consome as vidas em sequência). Assim a música acompanha o tempo exato da cena, mesmo se o ritmo dela mudar.
- **Referência de sensação (sem copiar):** a cena de virada de um RPG de 16 bits em que a música se desfaz.

---

## Quando cada música toca (o "diretor de música")

```
Tela inicial ──> MUS-01
Novo jogo ─────> (fade) MUS-02 no penhasco
Pulo no abismo ─> stinger "o pulo" ─> MUS-03 (base)
                   │  Sentinela suspeita/caçando, ou gato escondido perto ─> + camada MUS-05
                   │  Sentinela em combate ─────────────────────────────────> + camada MUS-04
                   │  Santuário ─> tema de Sethra (MUS-08, a fazer)
Arena do chefe ──> 3 s de silêncio ─> MUS-06 (fase 1 → fase 2 em Mi menor) ─> derrota: coro
Tirar o tomo ───> MUS-07 (drone + 7 golpes) ─> Capivara ─> Koda segura o Ré
```

Prioridade quando duas coisas acontecem juntas: **cena (MUS-07) > chefe (MUS-06) > combate (04) > furtividade (05) > base (03)**.

## Entregáveis (arquivos)
Formato: OGG Vorbis 44,1 kHz estéreo, *Streaming*, pontos de loop exatos no compasso (sem silêncio no começo/fim). As camadas 03/04/05 com **o mesmo comprimento exato**, para tocarem em sincronia.

| Arquivo | Conteúdo |
|---|---|
| `mus_tema_principal.ogg` | MUS-01 (introdução + loop; ponto de loop no compasso 5) |
| `mus_penhasco.ogg` · `mus_penhasco_pulo.ogg` | MUS-02 e o stinger do pulo |
| `mus_fortaleza_base.ogg` · `mus_fortaleza_furtivo.ogg` · `mus_fortaleza_combate.ogg` | As 3 camadas da fase (mesmo comprimento) |
| `mus_fortaleza_vitoria.ogg` | Final do combate (1 compasso) |
| `mus_chefe_koda_entrada.ogg` · `_fase1.ogg` · `_virada.ogg` · `_fase2.ogg` · `_derrota.ogg` | O duelo |
| `mus_maldicao_drone.ogg` · `mus_maldicao_vida_7.ogg` … `_1.ogg` · `mus_maldicao_capivara.ogg` · `mus_maldicao_koda.ogg` | A cena da maldição |

## Ordem de produção sugerida
1. **Motivo das sete vidas** (8 compassos, só melodia e acordes): é a base de tudo. Validar com o usuário antes de seguir.
2. **MUS-01 Tema principal:** dá a identidade; já pode tocar na tela inicial.
3. **MUS-03/04/05** juntas (uma sessão só, mesmo projeto no tracker), porque são camadas da mesma música. Testar as trocas no jogo cedo.
4. **MUS-07 A Sétima Ruína:** depende do motivo; é a cena mais emocional.
5. **MUS-06 Chefe.**
6. **MUS-02 Penhasco:** a mais simples (pad + fragmento + vento).

Junto com a música: o `MusicDirector` no Unity (camadas em sincronia com `AudioSettings.dspTime`, troca no tempo forte, prioridade da tabela acima) e os snapshots do Mixer (Exploração, Furtivo com passa-baixa, Combate, Pausado).

## Critérios de aceite
- Dado o menu aberto, **então** o motivo das 7 notas é cantarolável depois de ouvir 2 vezes (teste com 3 pessoas).
- Dado o gato explorando, **quando** uma Sentinela fica suspeita, **então** a camada furtiva entra no próximo tempo forte, sem corte audível.
- Dado o combate, **quando** a Sentinela prepara o golpe, **então** o som do telégrafo é audível por cima da música (−4 dB de ducking).
- Dada a luta com o Koda, **quando** ele chega à metade da vida, **então** a música sobe para Mi menor com o stinger.
- Dado o tomo retirado, **então** cada vida perdida tira uma nota do motivo, e no fim sobra só o Ré, segurado pelos metais do Koda.
- Nenhuma faixa usa melodia, progressão marcante ou som reconhecível de outro jogo.

## Faltando na lista (sugestões, fora das 7)
- **MUS-08 Santuário de Sethra** (contemplativo sagrado: harpa, sino, coro, a fonte): o lugar seguro precisa soar seguro. Curta (~40 s).
- **Morte / renascer:** o motivo em uma voz só, lento (~4 s), e o renascer no santuário.
- **Vínculo com o Koda** (o primeiro coração reacende): o motivo ganha a nota no metal do Paladino.

## Perguntas abertas
- [PROPOSTA] As músicas 3/4/5 como camadas da mesma música (recomendado) ou três faixas separadas com crossfade?
- [PROPOSTA] A resolução do motivo (Lá → Ré) guardada para o final do jogo.
- [PROPOSTA] O chefe nobre (não maligno) e a virada para Mi menor na fase 2.
- Quem compõe: você no tracker (Furnace/OpenMPT/BeepBox), um compositor contratado, ou começamos com rascunhos gerados para testar o sistema no jogo?
