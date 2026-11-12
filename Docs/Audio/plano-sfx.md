# Plano dos efeitos sonoros (SFX), vozes e ambiência — Prólogo / Fortaleza (MVP)

**Skill:** `sete-vidas-sound-design` · **Status:** rascunho [PROPOSTA] · **Data:** 12/11/2026
**Fontes:** specs de `Docs/Design/` (combate, furtividade, atirador, armadilhas, objetos barulhentos, chefe, Paladino, cura, moedas, vida/save, diálogo, inventário, tela inicial), `Docs/Level/fortaleza-mvp.md` e `Docs/Narrativa/dialogos-mvp.md`.
**Música:** fica em `plano-musicas.md`. Este documento cobre todo o resto: o gato, os inimigos, o chefe, o mundo, as armadilhas, a ambiência, a interface e as **vozes sem palavras**.

---

## 1. Regras que valem para todos os sons

| Regra | O que significa na prática |
|---|---|
| **O som é informação** | O jogador tem que entender o perigo de olhos fechados: "?" e "!" do guarda, mira da besta travando, marreta erguida, relâmpago armando |
| **Telégrafo** | Todo ataque inimigo e toda armadilha tem um som de **preparação** com a mesma duração do aviso visual (ex.: lança da Sentinela 0,45 s, mira da besta 0,9 s, fagulhas da chama 0,6 s) |
| **Som = regra** | O volume e o alcance de um som de ruído seguem o raio do `NoiseSystem` (cacos raio 10 soam alto e longe; andar agachado não soa quase nada) |
| **Faixas de frequência** | Gato nos médios-agudos (1–5 kHz) · inimigos nos médios-graves (150 Hz–1,5 kHz) · ambiência larga e baixa · praga com timbre exclusivo |
| **Impacto em 3 camadas** | transiente (clique, no frame do contato) + corpo (tom) + cauda (ruído/eco). O hitstop congela a imagem, **não** o som |
| **Variação** | Sons repetidos têm 3–5 variações + pitch aleatório (±3% a ±8%). Nada de "metralhadora" |
| **Timbre da praga** | Desafinação lenta + modulação em anel + reverb invertido. Só o tomo, a maldição e a Capivara usam. Nenhum outro som do mundo |
| **Pausa** | Interface toca em tempo real (`timeScale = 0` na pausa, no diálogo e no inventário) |

### Convenções
- **Nomes:** `sfx_<grupo>_<nome>_<nn>.wav`, `vox_<personagem>_<emoção>_<nn>.wav`, `amb_<lugar>_<nome>.ogg`, `ui_<nome>.wav`.
- **Pastas:** `Assets/_Game/Audio/SFX/<Gato|Inimigos|Chefe|Mundo|Armadilhas>/`, `Audio/Voz/`, `Audio/Ambience/`, `Audio/UI/`.
- **Formato:** SFX e vozes em WAV mono 44,1 kHz (ADPCM, *Decompress On Load*); ambiência em loop em Vorbis estéreo com *Streaming*.
- **Barramentos (AudioMixer):** Música · Ambiência · SFX/Jogador · SFX/Inimigos · SFX/Mundo · Voz · Interface.
- **Prioridade:** **A** (nunca pode sumir: avisos de perigo, dano no gato), **B** (golpes, passos de inimigos), **C** (enfeite: ambiência, passos do gato).
- **Escopo:** **MVP** = entra na demonstração da Fortaleza · **VS** = Vertical Slice ou depois.

---

## 2. Vozes sem palavras (o "murmúrio" dos personagens) [PROPOSTA]

### A ideia
Nenhum personagem fala palavras reais. Cada um tem um **idioma inventado de murmúrios**: sílabas sem sentido gravadas por uma pessoa, com o timbre e o jeito do personagem. É a mesma *sensação* dos metroidvanias contemplativos (uma vocalização curta que dá personalidade sem traduzir nada), mas com **sílabas, melodia e timbres criados do zero** para o Sete Vidas.

**Como toca no jogo:**
1. **Frase-assinatura no início de cada caixa de fala** (0,4–1,5 s): a voz "diz" algo no tom da emoção da fala. É o som principal.
2. **Murmúrio de fundo opcional** enquanto o texto aparece: sílabas soltas bem baixas (−12 dB em relação à frase), uma a cada 4–6 letras, só nas falas longas.
3. A **linha de sistema** não tem voz (só o som de pergaminho da interface).
4. Avançar a fala corta a voz anterior com fade de 60 ms (nunca duas vozes do mesmo personagem ao mesmo tempo).

Isso substitui o bipe sintético atual do `DialogueRunner` (um bipe a cada 2 letras). O bipe continua como fallback para quem não tiver banco de voz.

**Dados (Unity):** `VoiceBank` (ScriptableObject) ligado ao `SpeakerProfile`:
- listas de clipes por emoção: `neutro`, `ironico`, `bravo`, `triste`, `dor`, `riso`, `surpresa`, `pergunta`;
- sílabas soltas para o murmúrio de fundo;
- pitch base e faixa aleatória.

A `DialogueDefinition` já tem a expressão do retrato por fala: a **mesma expressão escolhe a emoção da voz**.

### As vozes

**Lion (o gato, Ladino)** · registro médio-agudo · rápido e seco · cínico
| Emoção | Exemplo do murmúrio (fonético) | Como soa |
|---|---|---|
| Neutro | *"mrah-ne, tsi-tah."* | Frase curta e rápida, cai no fim |
| Irônico (a mais usada) | *"hmf. nyah-mi... tsk."* | Um "hmf" pelo nariz, frase que sobe e termina num estalo de língua |
| Pergunta | *"mrre-nah?"* | Sobe no fim |
| Surpresa | *"mrrp?!"* | Um trinado curto de gato (o "prr" que gato faz) |
| Bravo (juramento) | *"hrrnn... ka-tah."* | Grave para ele, lento, entre dentes |
| Dor / maldição | *"nnh... mh-ah..."* | Respirado, quebrado, sem a ironia |
- **Assinatura:** o "tsk" e o trinado "mrrp". Sem miado de desenho animado: ele é um ladrão, não um mascote.
- **Pitch:** voz gravada normal +2 a +3 semitons.

**Koda, o Dourado (paladino, cão)** · grave · lento · caloroso e solene
| Emoção | Exemplo | Como soa |
|---|---|---|
| Neutro | *"hrrm. do-ra, va-hum."* | Grave, pausado, cada sílaba inteira |
| Solene (juramento) | *"vaa... dor-um, ha-ra-don."* | Quase cantado, com eco de salão |
| Bravo (luta) | *"HRRAH-do!"* | Curto, de peito, sem gritar demais |
| Triste / cansado | *"hhm... ra-dah..."* | Suspiro antes |
| Riso | *"hoh-hoh-hm."* | Riso grave e contido |
- **Assinatura:** o "hrrm" no começo (pigarro de cão grande) e as vogais abertas (o, a, u).
- **Pitch:** −4 a −5 semitons, com leve reverb de sala de pedra (é um paladino de catedral).

**O Contratante (Capivara Necromante)** · médio-grave · lentíssimo · educado e debochado
| Emoção | Exemplo | Como soa |
|---|---|---|
| Neutro | *"ehhh... mm-hmm. shu-la-mé."* | Arrastado, sílabas que escorregam |
| Deboche | *"heh-heh... ssshh-la-la."* | Riso pelo nariz, sibilante |
| Revelação | *"mmmh... va-shu, gatinh... -nah."* | Sussurro que vira voz cheia |
- **Assinatura:** "mm-hmm" satisfeito e os "sh" longos.
- **Processamento:** é o único personagem com o **timbre da praga** na voz (uma camada sussurrada com reverb invertido por baixo e desafinação lenta). O jogador aprende: "essa voz = corrupção".
- **Pitch:** −2 semitons, com um chorus bem leve (duas vozes quase iguais).

**Templários (Sentinelas, Atiradores, Instrutor, Guardas)** · ver §4. Eles não "conversam", só **latem, rosnam e resmungam** em combate e na patrulha.

**Sethra / placas de lore:** sem voz. Ler uma placa usa o som de pedra ou papel (§7).

### Como gravar (barato, em casa)
1. **Uma pessoa pode fazer todas as vozes.** O pitch e o processamento separam os personagens.
2. Para cada personagem, grave **3 tomadas de cada emoção** (frases de 0,5 a 1,5 s), improvisando sílabas no estilo da tabela, mais **12 sílabas soltas** para o murmúrio.
3. Microfone a 20 cm, quarto com cobertores (sem eco). Fale com a intenção da cena, não "lendo".
4. **Processamento:** corte de graves (< 90 Hz), compressor leve, pitch/formante do personagem, EQ, reverb do personagem. Normalizar a −3 dBFS de pico.
5. **Total MVP:** Lion ~20 clipes · Koda ~15 · Contratante ~10 · Templários ~25 = **~70 clipes curtos**.

---

## 3. O gato (Lion)

### 3.1 Locomoção
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| G-01 | Passo na pedra | Frame do pé no chão, andando/correndo | Foley: ponta dos dedos batendo de leve em madeira dura + um "tic" de unha | 5 | Correndo (≥ 60%) = ruído raio 3: o passo correndo é ~4 dB mais alto que andando | MVP |
| G-02 | Passo na terra/grama | Trilha, pátio, montanha | Dedos em caixa com terra; farfalhar curto | 4 | idem | MVP |
| G-03 | Passo na madeira | Passarelas, prateleiras, arquibancada | Batida leve em tábua oca | 4 | idem | MVP |
| G-04 | Passo na água rasa | Lagoa | Dedo batendo em bacia com 1 cm de água | 3 | — | MVP |
| G-05 | Passo agachado | Andando agachado | O mesmo passo, −12 dB, com passa-baixa e um roçar de tecido (a capa) | 3 | **Agachado = ruído 0**: tem que soar quase nada | MVP |
| G-06 | Pulo | Saída do chão | Roçar de capa rápido (tecido) + "fff" curto de ar | 3 | — | MVP |
| G-07 | Aterrissagem leve | Queda < 4,5 tiles | Pata macia, abafada | 3 | Silencioso para a IA: o som tem que ser **suave** | MVP |
| G-08 | Aterrissagem pesada | Queda ≥ 4,5 tiles | Batida mais cheia + poeira (ruído curto) | 3 | Ruído raio 5: soa claramente mais alto que a leve | MVP |
| G-09 | Agachar / levantar | Começa e termina o agachar | Roçar curto de tecido, −10 dB | 2+2 | — | MVP |
| G-10 | Agarrar borda | Frame em que pendura | Unhas raspando pedra (garfo em tijolo, bem curto) | 3 | — | MVP |
| G-11 | Subir da borda | Animação de subir (0,2 s) | Esforço curto + capa | 2 | — | MVP |
| G-12 | Agarrar a hera | Gruda na parede escalável | Folhas secas amassadas + unhas na madeira | 3 | — | MVP |
| G-13 | Escalando (loop por passo) | Cada movimento subindo/descendo | Folha + unha, alternando | 4 | — | MVP |
| G-14 | Pulo de parede | Espaço na parede | Impulso: unhas raspando + "fff" de ar mais forte que o pulo | 3 | — | MVP |
| G-15 | Escorregar na rampa íngreme | Rampa 2:1 | Areia escorrendo em loop curto | 1 loop | — | MVP |
| G-16 | Queda grande na lagoa | A queda de 60 tiles do salto de fé | **Splash grande** em 3 camadas: estouro de água + corpo grave + gotas caindo por 1,5 s | 1 | Momento de abertura: o mais "cheio" dos sons do gato | MVP |
| G-17 | Splash pequeno / sair da água | Entrar e sair da lagoa andando | Mão na bacia, escorrer | 2+2 | — | MVP |
| G-18 | Sacudir a água | Logo após sair da lagoa (se houver animação) | Sacudida de pano molhado + gotas | 1 | — | VS |
| G-19 | Entrar no esconderijo (matinho, feno, caixote) | Agacha dentro do `HidingSpot` | Farfalhar de folhas/palha, −8 dB | 3 | O próprio esconderijo não faz ruído para a IA | MVP |

### 3.2 Combate do Ladino (adagas)
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| G-20 | Golpe 1 (corte no ar) | Início da fase ativa | "Swish" agudo e curto: vareta fina cortando o ar, pitch alto | 4 | Ruído raio 4 por golpe | MVP |
| G-21 | Golpe 2 do combo | 2º golpe | Swish mais longo e mais grave + tilintar de lâmina | 3 | idem | MVP |
| G-22 | Acerto comum (2) | Frame do contato | Clique seco + corpo de "tuc" (faca em repolho) + cauda curta | 4 | Hitstop 0,05 s | MVP |
| G-23 | Acerto pelas costas (3) | idem, costas | G-22 + camada metálica média | 3 | Hitstop 0,07 s | MVP |
| G-24 | **Acerto furtivo (5)** | idem, furtivo | G-22 mais grave + **"ting" agudo de crítico** (sino pequeno, 2 kHz) + 80 ms de silêncio antes da cauda | 2 | Hitstop 0,12 s; tem que ser **o som mais satisfatório do Ladino** | MVP |
| G-25 | Acerto na armadura (de frente) | Quando o alvo é um templário | Camada de metal batendo (colher em panela, abafada) | 3 | — | MVP |
| G-26 | Adaga na parede rachada ("tink") | Adaga acerta `BreakableWall` | "Tink" metálico seco, sem cauda: diz "não adianta" | 2 | A parede só aceita Heavy | MVP |
| G-27 | Adaga no escudo sagrado do Koda | Golpe na bolha | Ver C-15 | — | — | MVP |
| G-28 | Estocada para baixo | Início da estocada no ar | Swish que desce de pitch | 2 | — | MVP |
| G-29 | Quique da estocada | Acerto e pulo para cima | Acerto + "boing" de tecido elástico, curto | 2 | — | MVP |
| G-30 | Golpe no boneco de treino | Acerto no `TrainingDummy` | Palha socada + madeira | 3 | — | MVP |

### 3.3 Paladino, troca de classe e cura
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| G-40 | Troca para o Ladino | Troca de classe confirmada | "Plim" curto: clarinete/pizzicato em Ré + roçar de capa | 1 | Instrumento-assinatura da classe | MVP |
| G-41 | Troca para o Paladino | idem | "Plim" de metal dourado (sino grave + metal) + peso da marreta nas costas | 1 | idem | MVP |
| G-42 | Tecla de classe bloqueada | — | **Nenhum som** (regra da spec) | — | Regra 2 da troca | MVP |
| G-43 | Marreta erguida (preparação) | Início da preparação (0,35 s) | Rangido de couro + metal pesado subindo, crescendo | 2 | Telégrafo do próprio gato | MVP |
| G-44 | Marreta no ar | Fase ativa | "Whoom" grave e lento | 2 | Ruído raio 8 (dobro das adagas) | MVP |
| G-45 | Marreta acerta inimigo | Contato | Transiente forte + corpo grave + armadura amassando + tremor curto | 3 | Dano 4, Heavy | MVP |
| G-46 | Marreta no chão (erro) | Golpe sem alvo, no frame do chão | Pedra batida + poeira | 3 | — | MVP |
| G-47 | Marreta na parede rachada | Acerto 1 (vida 8 → 4) | Pedra rachando + "crack" seco + pedrinhas | 1 | Com metade da vida as rachaduras crescem | MVP |
| G-48 | Parede rachada quebrando | Acerto 2 | Desmoronar: pedras caindo por 1,2 s, poeira, um "dong" grave no fim; som alto | 2 | Ruído raio 8; o som tem que dizer "abriu um caminho" | MVP |
| G-49 | Cura: concentração | 0,45 s de concentração | Coro "aah" baixinho + centelhas (sinos agudos subindo) | 1 | Interrompível | MVP |
| G-50 | Cura: efeito | Fim da concentração | Acorde dourado (Ré maior aberto) + anel de luz (whoosh brilhante) | 1 | +2 hits | MVP |
| G-51 | Cura interrompida | Dano na concentração | O coro corta com um "fsst" para baixo (pitch caindo) | 1 | O uso não é gasto | MVP |
| G-52 | Cura sem efeito (sem uso / vida cheia) | F sem poder curar | "Tuc" abafado bem baixo, ou nada [PROPOSTA: nada com vida cheia, "tuc" sem uso] | 1 | — | MVP |

### 3.4 Vida, dano e morte
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| G-60 | Gato toma 1 hit | Frame do dano | Impacto curto + **voz do Lion "nngh!"** (vox) + nota de chip descendo | 3 | Prioridade A | MVP |
| G-61 | Coração ficou pela metade / vazio | HUD atualiza | Rachadura de vidro pequena (meio) / mais cheia (vazio) | 2 | — | MVP |
| G-62 | **Última vida (batimento)** | Com 1 hit só | Batimento grave (2 batidas a ~70 bpm) em loop, −14 dB, com passa-baixa leve na música | 1 loop | Para ao curar ou morrer | MVP |
| G-63 | Morte | Tomba (0,7 s) | Voz "mh..." do Lion + o motivo das sete vidas em uma voz, lento (ligado à música) + corpo caindo | 1 | — | MVP |
| G-64 | Renascer no santuário | A tela clareia | Sino do santuário + respiração do gato | 1 | — | MVP |
| G-65 | Espinhos | Encosta nos espinhos | Perfurar (garfo em couro) + G-60 | 2 | — | MVP |
| G-66 | Voltar ao chão seguro | A tela pisca e o gato reaparece | "Fwip" curto, invertido | 1 | — | MVP |
| G-67 | Meio coração solta | Inimigo deixa cair | Pulinho de cristal + quique | 1 | 15% de chance | MVP |
| G-68 | Meio coração pego | Encostar | Nota de harpa + "+1" (acorde curto) | 1 | — | MVP |
| G-69 | Meio coração piscando / sumindo | Últimos 3 s / some | Tique-tique suave / "puf" de bolha | 1+1 | — | MVP |

---

## 4. Templários da Ordem da Presa Dourada (cães)

Os guardas são **cães de armadura**. O som deles mistura **metal** (cota de malha, placas) com **voz de cão** (rosnado, farejar, latido curto e abafado pelo elmo). Os vocais são o que conta o estado da IA, então são **prioridade A** e espaciais (o jogador ouve de fora da tela de onde vem).

### 4.1 Sentinela lanceiro (e Instrutor, Vigia, Guardas)
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| I-01 | Passos de armadura | Cada passo na patrulha | Bota pesada + tilintar de cota de malha (chaveiro sacudido) | 5 | Audível a ~8 tiles: o jogador ouve o guarda chegando | MVP |
| I-02 | Passos correndo | Perseguição e caçada | I-01 mais rápido, mais metal, respiração | 4 | — | MVP |
| I-03 | Ocioso na patrulha | Pausa nas pontas da rota | Vox baixinho: pigarro, bocejo de cão, resmungo *"hrmf"*, farejar | 6 | Só a cada 6–15 s, aleatório | MVP |
| I-04 | **"?" Suspeito** | Entra em Suspeito | Vox: *"hrr?"* curto subindo (orelha de cão levantando) + chip de 2 notas subindo (o ícone) | 4 | Prioridade A | MVP |
| I-05 | Investigando | Andando até o barulho | Farejar *"snff-snff"* + resmungo baixo | 4 | — | MVP |
| I-06 | **"!" Viu o gato** | Entra em Em combate | Vox: **latido curto e abafado *"WUF!"* / *"HRAH!"*** + estalo metálico (lança empunhada) + chip agudo de alerta | 4 | Prioridade A; o som mais legível do jogo | MVP |
| I-07 | Caçando (o "?" laranja) | Entra na caçada | Rosnado contínuo + farejar mais rápido | 3 | Diferente de I-05: diz "ele não esqueceu" | MVP |
| I-08 | Desistiu (volta à patrulha) | Suspeita zera / fim da caçada | Vox: *"hmph."* (bufo de cão) + metal relaxando | 3 | O jogador sabe que pode sair | MVP |
| I-09 | **Ataque: preparação** | Lança recuada (0,45 s) | Rangido de couro + inspiração de esforço + metal arranhando, **crescendo até o golpe** | 3 | Telégrafo de 0,45 s | MVP |
| I-10 | Ataque: estocada | Fase ativa | Swish grave + vox *"hah!"* | 3 | — | MVP |
| I-11 | Lança acerta o gato | Contato | (usa G-60 do gato) + metal | — | — | MVP |
| I-12 | Toma dano | Acerto do gato | Ganido curto *"yip!"* / grunhido + metal | 4 | — | MVP |
| I-13 | Morte | Tomba | Ganido que cai + armadura desabando (lata grande caindo) + lança batendo no chão | 3 | — | MVP |
| I-14 | Esbarrão | Gato esbarra no guarda | Metal surdo + *"hrn?"* | 2 | De frente = combate, pelas costas = suspeita | MVP |
| I-15 | Instrutor treinando | Patrulha no pátio | Golpes de madeira no boneco, distantes, ritmo irregular | 3 | Ambiência viva do pátio | VS |

### 4.2 Atirador (besta)
| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| I-20 | **Mira** (0,9 s) | Linha pontilhada aparece | Corda de besta esticando: rangido de madeira crescente + zumbido fino subindo | 2 | Telégrafo de 0,9 s; dura o mesmo que a mira | MVP |
| I-21 | **Mira travada** (últimos 0,25 s) | Linha fica firme | "Clique" metálico seco e alto (trava) | 1 | Prioridade A: é a hora de sair do lugar | MVP |
| I-22 | Disparo | Solta o virote | "Tchak" da corda + "fwip" | 3 | — | MVP |
| I-23 | Virote voando | Em voo (12 tiles/s) | Assobio curto que passa (efeito Doppler simples: pitch cai) | 2 | Espacial | MVP |
| I-24 | Virote crava na parede | Bate na parede | "Thunk" de madeira + vibração da haste (boing curto) | 3 | — | MVP |
| I-25 | Recarga (1,6 s) | Baixa a besta | Manivela/catraca (zíper lento) + virote encaixando; termina com "clac" | 2 | A janela para chegar perto: o fim da recarga avisa que acabou | MVP |
| I-26 | Vocais | Igual ao lanceiro (I-03 a I-13) | O mesmo banco, pitch +1 semitom (mais magro) | — | — | MVP |

### 4.3 Seteiras (atiradoras automáticas na parede)
| ID | Som | Quando | Como fazer | Var. | Esc. |
|---|---|---|---|---|---|
| I-30 | Mecanismo armando | Antes do tiro (período 2,2 s) | Tique de catraca de madeira nos últimos 0,3 s | 2 | MVP |
| I-31 | Disparo da seteira | Tiro | "Tchak" mais seco e metálico que a besta | 3 | MVP |

---

## 5. O chefe: Koda, o Dourado

Koda é **pesado, nobre, não maligno**. Os sons dele são graves e "dourados" (metal limpo, sinos), e os mais perigosos têm os avisos mais longos e mais claros. Prioridade A para todos os telégrafos.

| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| C-01 | Grade da arena fechando | Ao entrar | Correntes + grade de ferro batendo no chão, eco grande | 1 | — | MVP |
| C-02 | Passos do Koda | Cada passo | Placas grandes + bota pesada; mais grave e lento que a Sentinela | 4 | 3,6 tiles/s | MVP |
| C-03 | **Marretada: erguer** | 0,5 s (0,33 s no combo) | Metal subindo + inspiração funda + vox *"hrrm—"* | 3 | Telégrafo 0,5 s | MVP |
| C-04 | Marretada: impacto | Golpe | Impacto grave forte + pedra + tremor de tela (com ducking de 0,3 s na ambiência) | 3 | 1 hit | MVP |
| C-05 | Combo | Emenda outra marretada | C-03 mais curto e mais agudo (pitch +2 st): o jogador ouve que vem outra | 2 | 45%, até 3 | MVP |
| C-06 | **Salto: agachar** | 0,5 s | Joelhos de metal rangendo + inspiração grande + vox *"HRAH-"* | 1 | Telégrafo | MVP |
| C-07 | Salto: no ar | Subida de 5 tiles | Whoosh grave subindo, capa batendo | 1 | — | MVP |
| C-08 | Salto: queda | Aterrissa | O impacto mais forte da luta: estouro grave + pedras + eco longo | 2 | 4 hits, área 3,2 | MVP |
| C-09 | **Cura: ajoelhar e anel** | Cura concentrada | Coro grave "ohm" + anel dourado pulsando (sino em loop, um pulso por segundo) | 1 loop | O som diz "puna agora" | MVP |
| C-10 | Cura interrompida | 5 de dano | O coro quebra (vidro + pitch caindo) | 1 | — | MVP |
| C-11 | Atordoado (1,5 s) | Depois da interrupção | Sininhos girando (passarinhos) em versão metal + respiração pesada | 1 | — | MVP |
| C-12 | **Relâmpagos: marreta ao céu** | 0,8 s | Trovão distante rolando + carga elétrica subindo + vox solene *"vaa-dor!"* | 1 | Telégrafo | MVP |
| C-13 | **Coluna de aviso** | Cada coluna piscando 1,2 s | Zumbido elétrico crescente, espacial (o jogador ouve de qual lado vêm) | 2 | Telégrafo de 1,2 s; a faixa livre fica **sem zumbido** | MVP |
| C-14 | Raio | Cada raio | Estalo de chicote + trovão perto + chiado | 4 | 8 hits | MVP |
| C-15 | Escudo sagrado | Bolha liga / desliga / golpe bate | Liga: acorde de sino dourado subindo · loop: zumbido doce baixo · golpe: **"dong" de sino abafado** · desliga: acorde descendo | 1+1+3+1 | Invulnerável: o "dong" diz "não adianta" | MVP |
| C-16 | Fase 2 ("furioso") | 60% da vida | Rugido de cão grande (não monstruoso) + metal sacudindo + acorde de metais | 1 | Golpes 45% mais rápidos | MVP |
| C-17 | Koda toma dano | Acerto | Metal grosso + grunhido curto (não ganido: ele aguenta) | 4 | — | MVP |
| C-18 | Derrota (ajoelha) | 0 de vida | Marreta caindo no chão, joelho de metal, respiração, silêncio de 1 s | 1 | A música some em 2 s | MVP |
| C-19 | Grades abrindo | Depois da vitória | C-01 ao contrário (subindo) | 1 | — | MVP |
| C-20 | Selo: marreta no chão | `SealStrike` na cena do tomo | Golpe enorme + sino de igreja + clarão dourado (acorde Ré maior); a praga corta seco | 1 | Momento-chave da história | MVP |

---

## 6. O tomo, a maldição e o Contratante (timbre da praga)

| ID | Som | Quando | Como fazer | Var. | Esc. |
|---|---|---|---|---|---|
| P-01 | Zumbido do tomo | Loop perto do pedestal | Drone grave desafinando devagar + sussurros invertidos, muito baixo; cresce ao aproximar | 1 loop | MVP |
| P-02 | Pegar o tomo | E no pedestal | Estalo de couro + **clarão roxo** (reverb invertido que "suga" para dentro) | 1 | MVP |
| P-03 | Tomo flutuando | Escapa da pata e abre | Páginas virando sozinhas em loop + vento sobrenatural | 1 loop | MVP |
| P-04 | Gato caindo de joelhos / se contorcendo | `curse_kneel`, `curse_writhe` | Corpo no chão + voz do Lion dor + respiração | 2 | MVP |
| P-05 | **Coração arrancado** (×6, a cada 0,5 s) | Cada coração voa até o livro | Sucção (whoosh invertido) + **uma nota do motivo das sete vidas** desafinando e sumindo; cada um uma nota diferente | 6 | MVP |
| P-06 | Fiapos de vida sugados | Loop entre os corações | Sussurro agudo puxado | 1 loop | MVP |
| P-07 | Tomo repelido, fecha e cai | Golpe do Koda | Livro batendo + a praga "engasgando" (corte do drone) | 1 | MVP |
| P-08 | Contratante surge | Clarão roxo | Estouro invertido + risada abafada *"heh..."* | 1 | MVP |
| P-09 | Contratante toma o tomo | — | Páginas fechando + sussurro | 1 | MVP |
| P-10 | **Moeda jogada no chão** | "O pagamento" | Moeda de ouro pesada: tilintar, rolar e assentar (som grande e lento, com eco) | 1 | MVP |
| P-11 | Contratante some | — | Reverb invertido para dentro + silêncio | 1 | MVP |
| P-12 | Tela escurece / juramento | Fade | Respiração do gato, vento; o sino do Koda volta | 1 | MVP |
| P-13 | +1 coração (vínculo) | 2º coração acende | Nota do motivo no instrumento da classe (metal do Paladino) + acorde de harpa subindo | 1 | MVP |

---

## 7. Mundo e objetos

### 7.1 Objetos barulhentos (furtividade)
O volume e o alcance do som **são** o raio da regra: o jogador aprende o tamanho do perigo pelo ouvido.
| ID | Som | Regra (raio / intensidade) | Como fazer | Var. | Esc. |
|---|---|---|---|---|---|
| M-01 | Cacos de vidro | 10 / 0,7, até agachado | Vidro triturado sob a bota: crocante e alto | 4 | MVP |
| M-02 | Folhas secas | 3 / 0,2, só correndo | Folhas amassadas, curto | 4 | MVP |
| M-03 | Palha | 3 / 0,2 | Palha de vassoura amassada | 3 | MVP |
| M-04 | Sininhos | 11 / 0,75 (alarme 16) | 3 sininhos de latão batendo, cauda de 1,2 s | 3 | MVP |
| M-05 | Balde / panela caindo | 4 / 0,4, uma vez | Balde de lata caindo e rolando | 2 | MVP |

### 7.2 Interativos
| ID | Som | Quando | Como fazer | Var. | Esc. |
|---|---|---|---|---|---|
| M-10 | Moeda comum | Pegar | "Plin" metálico agudo (2 notas: Lá-Ré) | 3 | MVP |
| M-11 | Moeda de 5 | Pegar | "Plin" mais cheio + tilintar de várias moedas | 2 | MVP |
| M-12 | Moeda do Contratante (100) | Pegar | Ouro pesado + nota grave da praga bem baixa (o olho roxo) | 1 | MVP |
| M-13 | Moeda girando no mundo | Loop perto da moeda | Brilho bem baixo (sininho a cada 2 s), só a < 3 tiles | 1 | VS |
| M-14 | Placa de pedra lida | E na placa | Pedra arrastada leve + poeira | 2 | MVP |
| M-15 | Placa/diário de papel | E no diário | Pergaminho desenrolando | 2 | MVP |
| M-16 | Santuário: perto | Aparece "E: descansar" | Sino de vento baixinho (loop do lugar) | 1 | MVP |
| M-17 | **Santuário: descansar** | E | Velas acendendo (fósforo + chama) + sino suave + o motivo das sete vidas em harpa (3 notas) + "salvo" | 1 | MVP |
| M-18 | Tocha / vela acesa | Loop espacial | Crepitar de fogo | 1 loop | MVP |
| M-19 | Grade de ferro (portas) | Abrir/fechar | Igual a C-01/C-19 | — | MVP |
| M-20 | Cachoeira | Loop espacial na face do penhasco | Água caindo (largo, grave) | 1 loop | MVP |
| M-21 | Fardo de feno / caixote (esconderijo) | Gato entra | Igual a G-19 | — | MVP |

---

## 8. Armadilhas
Todas têm **som de aviso** com o mesmo tempo do aviso visual. O vermelho das armadilhas tem o seu "par" sonoro: **tique mecânico** = armadilha armando.

| ID | Som | Quando | Como fazer | Var. | Regra ligada | Esc. |
|---|---|---|---|---|---|---|
| A-01 | Fio de alarme disparado | Gato de pé encosta | Fio esticado estalando ("twang") + sineta de alarme | 2 | Ruído raio 8 / 0,6 | MVP |
| A-02 | Placa de pressão ("CLIC!") | Afunda | Pedra afundando + clique metálico alto | 2 | Ruído raio 8 / 0,6 | MVP |
| A-03 | Fio/placa rearmando | 3 s depois | Tique leve de mola | 1 | — | MVP |
| A-04 | Flechas: aviso | Brilho na boca (0,3 s) | Catraca rápida "trrrk" | 1 | Telégrafo 0,3 s | MVP |
| A-05 | Flechas: rajada | 3 flechas, 0,25 s entre elas | "Fwip" curto por flecha (3 variações em sequência) | 3 | — | MVP |
| A-06 | Flecha batendo | Parede | "Tok" de madeira | 3 | — | MVP |
| A-07 | Lanças: aviso | Tremor do chão (0,25 s) | Rangido de pedra grave + pedrinhas | 1 | Telégrafo 0,25 s | MVP |
| A-08 | Lanças sobem / descem | Saem e voltam (0,6 s no alto) | "Shhhk" metálico rápido / arrastar descendo | 2+1 | — | MVP |
| A-09 | Jato de chama: fagulhas | 0,6 s de aviso | Chiado de faísca + "tss" de gás | 2 | Telégrafo 0,6 s | MVP |
| A-10 | Jato de chama: fogo | 1,2 s de chama | "Fwoosh" de abertura + rugido de fogo em loop curto | 2 | Os 6 jatos em onda: cada um espacial, para ouvir a onda andar | MVP |
| A-11 | Serra (loop) | Sempre girando | Zumbido metálico agudo em loop, espacial (o pitch sobe na velocidade, cai na pausa das pontas) | 1 loop | Ouve-se antes de ver | MVP |
| A-12 | Serra parando nas pontas | Pausa de 0,4 s | Rangido de trilho | 2 | — | MVP |
| A-13 | Plataforma frágil: treme | 0,5 s antes de cair | Pedra rachando + pedrinhas caindo | 2 | Telégrafo 0,5 s | MVP |
| A-14 | Plataforma frágil: cai / volta | Cai / volta em 3 s | Desabar curto / pedra se encaixando ("tuc") | 1+1 | — | MVP |

---

## 9. Ambiência por área

Cada área tem um **loop de fundo** (estéreo, 30–60 s) + **sons soltos aleatórios** (one-shots com intervalo). Feitos com `AmbienceProfile` e `AmbienceZone` (crossfade de 1,5 s entre zonas). Em lugar contemplativo, a ambiência é alta e a música pode sumir; em lugar de ação, a ambiência fica baixa.

| ID | Área | Loop | Sons soltos (intervalo) | Esc. |
|---|---|---|---|---|
| AM-01 | 1 · Montanha | Vento alto no penhasco, sopros largos | Gavião distante (20–40 s), pedrinha rolando (15–30 s), sino da Cidadela longe (45–90 s) | MVP |
| AM-02 | 1 · Lagoa e cachoeira | Água caindo + lagoa | Gotas, sapo (VS) | MVP |
| AM-03 | 2 · Trilha | Vento médio, folhas | Pássaros do fim de tarde (8–20 s), grilos começando | MVP |
| AM-04 | 3 · Pátio de treino | Vento, bandeiras batendo | Golpes de treino distantes, cavalo no estábulo (relincho baixo, 30–60 s), martelo de ferreiro longe | MVP |
| AM-05 | 4 · Escalada das vinhas | Vento forte nas muralhas (sobe com a altura) | Folhas da hera, pássaro passando | MVP |
| AM-06 | 5 · Galeria / alojamento | Interior abafado, lareira crepitando | **Templário roncando** atrás da parede (15–30 s), panela na cozinha, tosse de cão, madeira rangendo | MVP |
| AM-07 | 6 · Torre do Sino | Vento no oco da torre (assobio) | **Sino grande da torre** muito grave e longo (40–80 s), corda rangendo | MVP |
| AM-08 | 7 · Salão dos Votos | Eco grande de catedral, ar parado | Vitral rachado tilintando de leve (a praga, 20–40 s), velas | MVP |
| AM-09 | 8 · Galeria das Armadilhas | Mecanismos dentro da parede (engrenagens lentas) | Gotas de óleo, corrente rangendo | MVP |
| AM-10 | 9 · Câmara do Selo | Silêncio pesado + P-01 (o zumbido do tomo) por baixo | Sussurros da praga raros (30–60 s) | MVP |
| AM-11 | 10 · Fundos / poço | Vento descendo o poço, gotas com eco | Pedra caindo lá embaixo (eco longo) | MVP |
| AM-12 | Fim · Estrada para Miaravéu | Campo à noite: grilos, vento suave | Coruja (20–40 s) | MVP |
| AM-13 | Santuários | Sino de vento + água da fonte do Éter | Sino agudo do Éter (10–20 s) | MVP |
| AM-14 | Prólogo (contrato) | Taverna/quarto à luz de vela: crepitar baixo | Ver §11 | MVP |
| AM-15 | Tela inicial | Vento do penhasco suave + pássaros do pôr do sol, sob a música | — | MVP |

**Zonas de sombra:** ao entrar numa sombra, a ambiência ganha um passa-baixa leve (−3 dB acima de 4 kHz), igual ao "prender a respiração" da música. Sutil, mas o jogador sente que está escondido. [PROPOSTA]

---

## 10. Interface
| ID | Som | Quando | Como fazer | Var. | Esc. |
|---|---|---|---|---|---|
| UI-01 | Cursor (menu) | Trocar de botão | "Tic" de papel/madeira curto, macio | 2 | MVP |
| UI-02 | Confirmar | Enter / A | 2 notas de harpa subindo (Ré-Lá) | 1 | MVP |
| UI-03 | Voltar / cancelar | Esc / B | 2 notas descendo (Lá-Ré), mais baixas | 1 | MVP |
| UI-04 | "Novo jogo" com save (aviso de apagar) | Abre a confirmação | Nota grave sustentada + papel | 1 | MVP |
| UI-05 | Save apagado / começar | "Sim, apagar" | Página rasgada + acorde menor curto | 1 | MVP |
| UI-06 | Botão desativado | "Continuar" sem save | "Tuc" abafado | 1 | MVP |
| UI-07 | Centelha do menu se apagando | De 5 a 9 s | Sino agudo e suave em Ré (afina com a música) | 2 | MVP |
| UI-08 | Inventário abrir / fechar | Tab, I | Bolsa de couro abrindo + pergaminho / o inverso | 1+1 | MVP |
| UI-09 | Trocar aba | Q/E | Página virando | 2 | MVP |
| UI-10 | Cursor no inventário | Mover na grade | "Tic" mais metálico que o do menu | 2 | MVP |
| UI-11 | Equipar classe | Enter na classe | Igual a G-40/G-41 | — | MVP |
| UI-12 | Slot travado | Enter em classe travada | Nada (regra da spec) | — | MVP |
| UI-13 | Diálogo abrir / fechar | Começa / termina | Pergaminho abrindo / enrolando | 1+1 | MVP |
| UI-14 | Avançar fala | E | "Tic" de papel bem baixo (a voz da próxima fala faz o resto) | 2 | MVP |
| UI-15 | Linha de sistema / desbloqueio | "Paladino desbloqueado!" | Fanfarra curta de 4 notas (harpa + metal) | 1 | MVP |
| UI-16 | Dica de tutorial | Pergaminho da dica aparece | Papel + sino baixinho | 1 | MVP |
| UI-17 | Moeda no HUD (+N) | Pulinho do contador | Tilintar curto, empilha se vier rápido (máx. 3 vozes) | 2 | MVP |
| UI-18 | Orbe da cura gasto / recarregado | Usar / descansar | Vidro apagando / 3 notas subindo | 1+1 | MVP |
| UI-19 | Barra do chefe aparece | Fim do D-GOLDEN-00 | Golpe de tímpano + metal | 1 | MVP |
| UI-20 | "(furioso)" na barra | Fase 2 | Junto com C-16 | — | MVP |
| UI-21 | Fade da tela (entrada) | 0,8 s | Nenhum (silêncio) | — | MVP |

---

## 11. Prólogo: a leitura do contrato
| ID | Som | Quando | Como fazer | Esc. |
|---|---|---|---|---|
| PR-01 | Pergaminho do contrato abrindo | Começo | Papel grosso desenrolando | MVP |
| PR-02 | Virar/passar o olho | Cada caixa | Papel deslizando | MVP |
| PR-03 | "Cláusula sete" | Fala da letra miúda | Uma nota do timbre da praga, quase inaudível (pista) | MVP |
| PR-04 | Selo de cera / assinatura | Aceitar o contrato | Pena riscando + selo de cera prensado | MVP |
| PR-05 | Saída para a Fortaleza | Fade | Vento da montanha entrando | MVP |

---

## 12. Fora do MVP (Vertical Slice, para registrar)
- **Outras classes** (cada uma com a troca no seu instrumento e o golpe com o timbre dela): Mago (pulso com vibrato, fogo), Monge (bambu, pulo duplo "fwup"), Bardo (alaúde, ecos), Caçador (trompa, corda de arco, dash), Druida (kalimba, folhas, raízes).
- Ratos e pássaros que fogem do gato (só visuais pela regra, mas o som deixa o mundo vivo; **não** entram no `NoiseSystem`).
- Pausa com menu, tela de opções com volumes separados.
- Murmúrio de fundo das vozes (o item 2 do §2) se a frase-assinatura sozinha não bastar.

---

## 13. Mixagem e limites
| Item | Valor [PROPOSTA] |
|---|---|
| Alvo final | −16 LUFS integrado, pico ≤ −1 dBTP |
| Música | 4–6 dB abaixo dos SFX de jogo (já está como fundo) |
| Voz | Abaixa a música −6 dB e a ambiência −4 dB enquanto toca |
| Impacto forte (C-04, C-08, C-14, C-20, G-48) | Abaixa a ambiência por 0,3 s |
| Vozes simultâneas | Máximo 24 fontes. Passos do gato 2 · passos de inimigos 4 · moedas 3 · flechas 3 · vocais de inimigos 3 |
| Espacial | Inimigos, armadilhas e objetos em 2D com atenuação por distância (som some a ~14 tiles); gato e interface sem atenuação |
| Prioridade A nunca corta | Telégrafos (I-09, I-20, I-21, C-03, C-06, C-12, C-13, A-04, A-07, A-09, A-13), "?" e "!", dano no gato |

---

## 14. Como produzir (ferramentas gratuitas)
| Tipo de som | Melhor caminho |
|---|---|
| Acentos de chip (ícones "?"/"!", interface, crítico, moedas) | jsfxr / ChipTone, ou o gerador em `Tools/Audio/` (mesmo jeito dos rascunhos de música) |
| Foley (passos, capa, palha, metal, papel, vidro) | Gravar em casa com celular + Audacity. Ver a lista abaixo |
| Vozes sem palavras | Gravar a própria voz (§2) e processar no Audacity/Reaper |
| Fogo, água, vento, trovão | Bibliotecas livres (licença CC0, ex.: Freesound filtrando por CC0), editadas e com bitcrush leve só na ambiência |

**Lista de foley para gravar numa tarde:**
- chaveiro e corrente de bijuteria (cota de malha);
- panela e tampa (armadura, balde);
- garfo arranhando tijolo (unhas, agarrar borda);
- pano grosso e casaco (capa, agachar);
- folhas secas, palha de vassoura, arroz em saco plástico (passos, cacos leves);
- copo de vidro embrulhado em pano e esmagado com cuidado (cacos);
- tábua e ponta dos dedos (passos na madeira);
- repolho e faca (acertos de adaga);
- bacia com água (passos e splash);
- livro grosso e papel (tomo, pergaminhos, placas);
- régua batendo na mesa e vibrando (virote cravado).

### Ordem de produção (lotes)
1. **Lote 1, o que é regra de jogo:** vocais dos guardas ("?", "!", desistiu), telégrafos (lança, mira e trava da besta, armadilhas), objetos barulhentos, dano/morte do gato.
2. **Lote 2, o sentir do combate:** golpes e acertos do Ladino (com o furtivo), marreta, parede rachada, passos.
3. **Lote 3, o chefe e a maldição:** §5 e §6.
4. **Lote 4, vozes dos personagens** (§2) e diálogo.
5. **Lote 5, ambiência e interface** (§9, §10, §11).

**Total do MVP:** ~150 eventos de som (~330 arquivos com as variações) + ~70 clipes de voz + 15 loops de ambiência.

---

## Implementação (resumo)
- `SoundEvent` (ScriptableObject): clipes, volume, pitch, barramento, prioridade, máximo de instâncias, cooldown, espacial/raio.
- `AudioService`: pool de `AudioSource`, `Play(SoundEvent, posição)`. Os scripts de jogo (`CombatController`, `EnemyAI`, `TrapBase`, `NoisyObject`, `GoldenBoss`, `CursedTome`...) só chamam eventos; nada de clipe no código.
- **Ruído e som juntos:** quando `NoiseSystem.Emit(raio, intensidade)` dispara, o mesmo ponto toca o `SoundEvent` com volume derivado da intensidade. Assim som e regra nunca discordam.
- `VoiceBank` no `SpeakerProfile` (§2); o `DialogueRunner` toca a frase pela expressão da fala.
- `AmbienceProfile` + `AmbienceZone` (§9).
- Todo `static` novo (instância do `AudioService`, listas) com `ResetStatics` em `SubsystemRegistration`.
- **QA:** cada som de regra vira caso de teste (ex.: "dado o gato pisando nos cacos a 9 tiles da Sentinela, então ela fica Suspeita e o som de vidro é audível").

---

## [PROPOSTAS] deste documento
1. **Vozes sem palavras** por personagem (murmúrios inventados, uma frase por caixa de fala, escolhida pela expressão), substituindo o bipe por letra.
2. **Templários soam como cães de armadura** (metal + rosnar, farejar, latido abafado) e os vocais contam o estado da IA.
3. O **Contratante é o único personagem com o timbre da praga** na voz.
4. **Os 6 corações arrancados tocam as notas do motivo das sete vidas**, desafinando.
5. **Som de ruído ligado ao `NoiseSystem`** (o mesmo evento dispara a regra e o som).
6. **Passa-baixa leve na ambiência dentro da sombra.**
7. **Batimento na última vida.**
8. Cura com vida cheia não faz som; cura sem uso faz um "tuc".

---

## Implementado (12/11/2026): sons provisórios no jogo

**Gerar os sons:** `powershell -ExecutionPolicy Bypass -File Tools/Audio/gerar_sfx.ps1` (ou `... gerar_sfx.ps1 guarda` para só os que têm "guarda" no nome). São 307 WAVs sintetizados por código (`Tools/Audio/SfxSynth.cs` faz o som, `Tools/Audio/SfxBank.cs` tem as receitas) em `Assets/_Game/Resources/Audio/SFX/<Gato|Inimigos|Chefe|Praga|Mundo|Armadilhas|UI|Voz>/`. Regerar substitui os arquivos e mantém os `.meta`.

**Vozes:** sintetizador de formantes (pulso glótico, 3 formantes, consoantes de ruído, entonação por frase). Cada personagem tem altura, timbre e ritmo próprios. O Contratante passa pelo timbre da praga. Os guardas têm rosnado e capacete (passa-baixa). Essas vozes são provisórias: quando houver gravação de verdade (§2), é só trocar os WAVs com o mesmo nome.

**No jogo (`Scripts/Audio`):**
| Parte | Onde | Como |
|---|---|---|
| `Sfx` / `SfxPlayer` / `SfxCatalog` | `Sfx.cs` | `Sfx.Play("id", posição)`. Um conjunto de 24 fontes, variação sem repetir o mesmo arquivo, pitch aleatório, limite de cópias, atenuação pela distância da câmera e pan. Volume geral em PlayerPrefs `volume_efeitos` |
| `SfxLoop` | `Sfx.cs` | Loops que seguem um objeto: serra, zumbido do tomo, páginas, cura do Koda, batimento |
| `PlayerAudio` | o `StealthController` adiciona | Passos (o agachado quase mudo), pulo, aterrissagem leve/pesada (pela mesma altura da regra de ruído), borda, hera, pulo de parede, esconderijo, golpes, acertos (comum, costas, **furtivo**), marreta, quique, dano, espinhos, morte, renascer, cura, batimento na última vida |
| `EnemyAudio` | a `EnemyAI` adiciona | Passos de armadura, resmungos na patrulha, "?" suspeito, farejando, "!" alerta, caçando, desistiu, preparação e golpe da lança, mira / trava / disparo / recarga da besta, dano, morte |
| `BossAudio` | o `GoldenBoss` adiciona | Barra do chefe, marretada (combo mais agudo), salto, queda, cura em loop, cura interrompida e atordoado, relâmpagos, escudo, fúria da 2ª fase, passos, dano, derrota |
| Chamadas diretas | armadilhas, objetos barulhentos, parede rachada, moedas, meio coração, santuário, placas, água, grades, relâmpago, virote, cena do tomo, diálogo, troca de classe, menu, inventário, dicas, centelhas da tela inicial | uma linha `Sfx.Play` no ponto do evento |

**Diálogo:** cada fala com falante toca um murmúrio `vox_<voz>_<emoção>`. A emoção vem da expressão do retrato, com apelidos ("miuda" vira pergunta, "deboche" do gato vira irônico) e com "pergunta" quando a fala termina em "?". O bipe por letra só toca quando o falante não tem voz. A voz sai do `SpeakerProfile.VoiceId` ou, se vazio, do nome (Lionel → lion, Koda → koda, Contratante → capivara). Regra pura `DialogueRunner.VoiceClipId`, testada em `SfxRulesTests`.

**Ainda não feito:**
- ambiência das áreas (§9: loops e `AmbienceZone`) e o passa-baixa na sombra;
- AudioMixer com barramentos e ducking (§13);
- tela de opções com volumes separados;
- sons das outras classes (VS).

**Gerados mas ainda sem uso:** `cura_negada`, `ui_sistema`, `ui_orbe_gasto`, `ui_orbe_recarga`.

## Foley gravado (12/11/2026)

Passos e água agora vêm de gravação (`Tools/Audio/foley/originais/`: `corrida.wav` e `pulo_agua.wav`, cópias de `D:\5. Desenvolvimento\Jogos\EfeitosSonoros`). O corte é reproduzível: `powershell -ExecutionPolicy Bypass -File Tools/Audio/importar_foley.ps1` (código em `Tools/Audio/FoleyImport.cs`).

| Som | De onde | Tratamento |
|---|---|---|
| `gato_passo_01..08` (correndo) | 8 passos da corrida (2–19 s), limpos e de volume parecido, espalhados no tempo | mono, corte no início do passo (no máx. 0,22 s, sem pegar o seguinte), fades, nivelado pela intensidade |
| `gato_passo_lento_01..03` (andando devagar) | os passos mais fracos do fim (19,3–22 s) | igual |
| `gato_splash_grande_01` (o salto de fé) | o respingo inteiro (1,6 s) | estéreo 48 kHz → mono 44,1 kHz, sem o silêncio do fim, fade de 0,35 s |
| `gato_splash_01..02` (entrar na água) | o 1º impacto / um respingo menor, 0,3 s | mais agudo (×1,12 e ×1,2): corpo de gato |

O jogo toca `gato_passo` correndo (≥ 60% da velocidade, o mesmo limite do ruído de passo) e `gato_passo_lento` andando; agachado continua o passo sintético quase mudo. As receitas sintéticas desses sons ficaram desligadas no `SfxBank` (`UseSynthFoley`), então o `gerar_sfx.ps1` não sobrescreve as gravações.
