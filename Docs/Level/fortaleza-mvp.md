# Fortaleza — a Cidadela do Sol Eterno (MVP, versão vertical)

**Cena:** `Scenes/World/Fortress_MVP`, gerada por **Tools > Sete Vidas > 13. Fase 6: Fortaleza**. É onde o **Novo jogo** começa, logo depois do contrato.

**Fonte do mapa:** `Assets/_Game/Editor/FortressLayout.cs` reúne todos os tiles e objetos como dados.
- **Prévia sem abrir o Unity:** rode `Tools/LevelPreview/gerar_mapa.ps1`. Ela grava `SeteVidasDocs/Referencias/fortaleza_mapa.png`.
- **Para mudar o mapa:** edite o layout, gere a prévia, confira, e só então rode o menu 13.
- **Atenção:** o gerador recria a cena do zero.

**Unidades:**
- Coordenadas em tiles. "Chão com topo em Y" quer dizer tiles sólidos até Y−1.
- Métricas em `metricas.md`: pulo 4 (degraus de até 3), vão de até 4, agarrar borda 5, chaminé de pulo de parede com 3 de largura, passagem baixa de 1.

**Tamanho:** 273 × 129 tiles.
- A versão anterior tinha 226 × 25, quase toda plana.
- O caminho principal tem **cerca de 560 tiles**: ~250 andando, ~145 subindo e ~170 caindo. É **mais que o dobro** da versão anterior (~230).

## Esboço

```
 y 110 ┌───────────── topo das torres ──────────────────────────────┐
 y 101 │ S4 ┊ [9 CÂMARA DO SELO: Koda] ┃📕┃▓[10a PASSAGEM]▓ cofre  ↓┃▓ cofre 72
       │ ▲  └ chaminé do selo (3 de largura, serra)                ┃
 y  72 │ ▲ [8 ←ARMADILHAS: fosso frágil, serra, fogo][7 ←SALÃO DOS VOTOS: 3 guardas] S3 ┃▓ cofre 43
       │                        chaminé do sino (3 de largura, serra, seteira) ▲      ┃
 y  43 │ S2 [5 GALERIA DA MURALHA → passarela, sombras, alarme, lanças, 3 guardas] ▲  ┃▓ cofre 15
  ┌────┘ ▲ vinha 3 (muralha 3, topo 43)                                         [poço]┃
 y 29  ▲ vinha 2 (vigia de costas)                                                    ┃
 y 15  ▲ vinha 1 (seteira na passarela)                                                ┃
 y  0 [1 MONTANHA]↓ salto de fé [2 TRILHA] [3 PÁTIO DE TREINO] S1 ▓estábulo   ...  ▓portão → estrada → FIM
 y 60  penhasco + cachoeira
```

▓ = parede rachada (marreta do Paladino) · S = santuário · ▲ = subida · ↓ = queda

## Fichas das áreas

### Área 1 — Montanha (o salto de fé)
- **Região:** Montanha, do lado de fora.
- **Propósito:** história e abertura de impacto.
- **Tamanho:** 27 × 60, x −14..12, topo do penhasco em 60.
- **Entradas e saídas:** começa aqui (PlayerSpawn x 2,5, y 60). Saída para a direita, caindo do penhasco: **mão única**, justificada por ser o começo do jogo; nada depois exige voltar aqui.
- **O que tem:**
  - narração D-FORT-01: "Do alto da montanha, a Cidadela do Sol Eterno brilha no pôr do sol";
  - placa "salto de fé: corra e pule";
  - a **cachoeira** desce ao lado da queda;
  - o gato cai **60 tiles** e pousa na **lagoa** (D-FORT-02: "Água. Claro que tinha que ser água."). Queda não machuca: "sempre cai de pé".
- **Fundo:** céu de pôr do sol, montanhas ao longe com a silhueta da Cidadela e morros com pinheiros (paralaxe).
- **Risco de softlock:** nenhum.

### Área 2 — Trilha
- **Propósito:** tutorial de pulo.
- **Tamanho:** x 13..44, chão em 0.
- **O que tem:**
  - marco de pedra D-PLACA-10 ("Miaravéu, três dias para trás");
  - pedras de **2** e **3**;
  - fosso de **4** com espinhos (cair = 1 hit e volta ao chão seguro).
- **Moedas:** em cima da pedra de 3 e no ar, sobre o fosso.
- **Risco de softlock:** nenhum.

### Área 3 — Pátio de treino dos guardas
- **Propósito:** tutorial de agachar, combate e furtividade, e um desafio opcional de pulo de parede.
- **Tamanho:** x 44..111, chão em 0.
- **Arquibancada:** a passagem baixa de 1 tile (x 50..55) tem um **fio de alarme** com flechas na altura do peito. Agachado, o gato passa por baixo do fio e as flechas passam por cima. Moeda embaixo da arquibancada.
- **Atirador da arquibancada** (topo 7, olhando para a esquerda): mira em quem chega pela entrada. A passagem baixa embaixo dele é cobertura. Para pegá-lo de costas, sobe-se pela **vinha atrás da arquibancada** (x 55).
- **Campo de treino:**
  - placa D-PLACA-01, bonecos de palha (um de cada lado), suporte de armas e alvo de arco;
  - **Instrutor** patrulhando x 72–80.
- **Tenda:** sombra e um fardo de feno para se esconder.
- **Torre de escalada dos escudeiros** (x 88..94), opcional:
  - entra pela porta baixa da coluna esquerda;
  - sobe **pulando de parede em parede** (vão de 3) enquanto uma **seteira** atira no vão;
  - moeda no topo da coluna direita (19);
  - de lá cai **atrás do Guarda da torre de vigia** (x 95–103): ataque pelas costas.
- **Portão do pátio:** placa de pressão com **lanças** e o santuário **S1 "Pátio de treino"** (x 108).
- **Gate visível antes:** a porta rachada do **estábulo** (x 112) mostra a linguagem "rachaduras douradas... precisa de força". Atrás dela: 2 moedas de 5 e D-PLACA-05. **[PROPOSTA]** A volta a este pátio fica para depois do MVP (por enquanto a saída é pelos fundos).
- **Risco de softlock:**
  - nenhum: a torre de escalada tem saída pela porta de baixo;
  - o estábulo só abre com a marreta.

### Área 4 — Escalada das vinhas
- **Propósito:** escalada em três etapas, mostrando que o castelo é cada vez mais alto.
- **Tamanho:** x 112..135, de y 0 até 43.
- **Muralha 1** (topo 15):
  - vinha de y 4 a 14 (pula e gruda), com uma moeda no meio;
  - na passarela de cima, uma **seteira** atira a cada 2,2 s na altura do peito: agachar.
- **Muralha 2** (topo 29): vinha de 16 a 28. Lá em cima, o **Vigia da muralha** está **de costas** para a vinha: ataque pelas costas.
- **Muralha 3** (topo 43): vinha de 30 a 42, que leva ao portão da muralha.
- **Risco de softlock:** nenhum. Cair só leva para a etapa de baixo, e a vinha sempre está ao alcance (começa no máximo 1 tile acima do chão de cada etapa).

### Área 5 — Galeria da muralha (alojamento dos templários)
- **Propósito:** furtividade.
- **Tamanho:** x 136..210, andar 43, teto 52.
- **S2 "Portão da muralha"** (x 139) e a placa D-PLACA-02 (alojamento).
- **Trecho A:**
  - sombra com caixotes;
  - **Sentinela da galeria** (x 155–164);
  - **passarela** por cima (topo 48, sobe-se pela hera em x 153), com uma moeda.
- **Trecho B:**
  - sombra;
  - **fio de alarme** sem armadilha (chama as sentinelas);
  - **Sentinela do alojamento** (x 185–195);
  - placa de pressão com **lanças** (x 198).
- **Atirador do alojamento** numa **prateleira** (topo 48, agarrar a borda), cobrindo a sombra B e o fio de alarme.
- **Trecho C:** o **Guarda da torre do sino** (x 200–206) olha para a torre, ou seja, de costas para quem chega.
- **Duas soluções:** por cima (passarela e sombras, sem luta) ou combate direto.
- **Risco de softlock:** nenhum. O fim da galeria é a base da chaminé.

### Área 6 — Torre do Sino
- **Propósito:** ensinar o **pulo de parede** com risco, como teste depois da torre de treino do pátio.
- **Tamanho:** chaminé de 3 de largura (x 208..210), de 43 a 72.
- **Vinhas alternadas:**
  - direita, 44–54;
  - esquerda, 52–62;
  - direita, 60–71.
- **Perigos:** uma **seteira** (y 57) atira no vão, e uma **serra** cruza a chaminé (y 64,5) indo de parede a parede. Moeda logo acima da serra.
- **Saída:** por cima, no santuário **S3 "Torre do sino"** (x 214,5).
- **Risco de softlock:** nenhum. Cair só devolve à base da chaminé.

### Área 7 — Salão dos Votos
- **Propósito:** combate aberto, indo para a esquerda.
- **Tamanho:** x 176..216, andar 72, teto 82.
- **O que tem:**
  - placa D-PLACA-03 (o juramento), vitrais e escudos da Ordem;
  - **3 sentinelas** (x 196–204, 185–194, 176–183);
  - duas plataformas a 3 do chão para atacar por cima, uma com moeda.
  - **Atirador da sacada** (topo 78), cobrindo o salão inteiro. Chega-se nele pela plataforma do salão (3 acima, vão de 3).
- **Risco de softlock:** nenhum.

### Área 8 — Galeria das Armadilhas e Torre do Selo
- **Propósito:** desafio de plataforma desviando de coisas que machucam.
- **Tamanho:** x 140..175, andar 72.
- **A sequência, da direita para a esquerda:**
  - **6 jatos de fogo** em onda, com plataformas a 3 do chão para passar por cima;
  - **serra** no trilho, com plataforma por cima;
  - **fosso de 6 com espinhos** e duas **plataformas frágeis**, com uma moeda no meio do caminho;
  - 5 tiles de chão seguro.
- **Torre do Selo** (x 140..142, de 72 a 101): chaminé de 3 com vinhas alternadas, uma **seteira** (y 89) e uma **serra** cruzando (y 90,5). Moeda perto do topo.
- **Risco de softlock:**
  - nenhum: espinhos devolvem ao último chão seguro;
  - cair da chaminé devolve ao chão da galeria.

### Área 9 — Câmara do Selo (o topo)
- **Propósito:** chefe e história.
- **Tamanho:** andar 101, arena x 146..177, teto 110.
- **S4 "Antes da câmara"** (x 137,5), placa D-PLACA-04, grades (entrada em 145,5 e tomo em 177,5).
- **Koda, o Dourado** (x 166) e o **tomo** (x 181,5). Depois: a maldição, a marretada que segura a última vida, o Contratante, a moeda de 100 e o juramento, que dá o **Paladino**.
- **Gate visível antes:** a **porta rachada da sala do tomo** (x 185) aparece antes do chefe ser vencido.
- **Risco de softlock:** nenhum. A saída exige o Paladino, que é ganho na própria sala.

### Área 10 — A descida pelos fundos (depois do Paladino)
- **Propósito:** recompensa (usar a marreta), segredos e lore, e a saída.
- **Passagem dos fundos** (x 186..222, andar 101):
  - o **Guarda da retaguarda** patrulha;
  - **parede rachada** do cofre do guardião, com 2 moedas de 5 e o **Diário do Guardião** (D-PLACA-06).
- **Poço dos fundos** (x 218..222, de 101 até 0):
  - **3 patamares** (72, 43 e 15). Os buracos alternam de lado, para cada queda parar no patamar seguinte;
  - **seteiras** atiram entre os patamares durante a queda.
- **Cofres:** em cada patamar, uma **parede rachada** à direita leva a um cofre com 2 moedas de 5 e uma placa:
  - 72: o Mural da Marcha Dourada (D-PLACA-07);
  - 43: a Pedra dos votos (D-PLACA-08: "Servir os vivos" raspado para "o reino"; **[PROPOSTA]**, ligada ao segredo do Koda);
  - 15: a Lista dos guardiões (D-PLACA-09).
- **Portão dos fundos** (x 223, parede rachada com o vão de cima fechado até y 12), a **estrada para Miaravéu** e o **fim da demonstração**.
- **Risco de softlock:**
  - nenhum: só se entra aqui com o Paladino;
  - os cofres são becos com saída pela própria porta;
  - o fundo do poço sempre tem o portão.

## Gates (todos com aviso antes)

| Gate | Onde aparece primeiro | Abre com | Leva a |
|---|---|---|---|
| Passagem baixa | Pátio (arquibancada) | agachar (Ladino) | resto do pátio |
| Vinhas (paredes escaláveis) | Muralhas 1–3, chaminés | escalar | o castelo |
| Pulo de parede (vão de 3) | Torre de escalada do pátio (opcional) | escalar + pular | Torre do Sino, Torre do Selo |
| Porta rachada do estábulo | Pátio, x 112 | Paladino | cofre do estábulo (volta pós-MVP) |
| Porta rachada do tomo | Sala do tomo | Paladino | descida pelos fundos, saída |
| Cofres do guardião e do poço | Passagem e poço | Paladino | moedas + lore |
| Portão dos fundos | Fundo do poço | Paladino | estrada → fim |

## Moedas

São **24 moedas**:
- 13 comuns (valor 1) pelo caminho, quase todas em desvios ou em lugar de risco;
- 10 de valor 5 nos cofres (2 delas no estábulo, que só abre depois do MVP);
- a do Contratante (100), na sala do tomo.

No total, **163 moedas**; **153 dá para pegar no MVP** (1×13 + 5×8 + 100). A lista completa com ids está em `FortressLayout.Objects()`.

## Ensinar → testar → combinar

| Habilidade | Ensina | Testa | Combina |
|---|---|---|---|
| Pulo | Trilha (pedras, fosso) | Galeria das armadilhas | Fogo + serra + frágeis |
| Agachar | Arquibancada (fio + flechas) | Seteira da muralha 1 | Sombras da galeria |
| Escalar | Vinha 1 | Vinhas 2 e 3 (guarda em cima) | Chaminés |
| Pulo de parede | Torre de escalada do pátio (opcional, sem queda perigosa) | Torre do Sino (seteira, serra) | Torre do Selo (seteira, serra, mais alta) |
| Ataque pelas costas | Bonecos do pátio | Vigia da muralha (de costas) | Guarda da torre do sino |
| Cobertura contra tiro | Atirador da arquibancada (passagem baixa por baixo dele) | Atirador do alojamento (sombra + fio) | Atirador da sacada (salão com 3 lanceiros) |
| Marreta (Paladino) | Porta do tomo | Cofre do guardião | Poço com 3 cofres e portão |

## Checagem anti-softlock e de alcance

Simulei a progressão em ordem, só com o Ladino no começo:

1. **Montanha → trilha → pátio:** tudo alcançável andando, pulando e agachando. A queda da montanha é a única porta de mão única, e nada depois exige voltar ao alto.
2. **Pátio → muralhas:** as vinhas começam a no máximo 1 tile do chão de cada etapa. Ninguém fica preso numa etapa: cair leva para a de baixo.
3. **Galeria da muralha → Torre do Sino:** a chaminé tem 3 de largura (pulo de parede ✔) e vinhas que se sobrepõem 2 tiles (dá para trocar de parede). O topo sai num chão de 6 tiles com santuário. O buraco da chaminé no chão do salão (3 de largura) é pulável ✔.
4. **Salão → galeria das armadilhas → Torre do Selo:**
   - plataformas a 3 do chão ✔;
   - o fosso de 6 exige as plataformas frágeis, e cair nos espinhos devolve ao chão seguro ✔;
   - na Torre do Selo, vinhas sobrepostas e vão de 3 ✔.
5. **Câmara:** o Paladino é ganho na sala do tomo, antes do primeiro gate que exige a marreta (a porta do tomo) ✔.
6. **Descida:**
   - cada queda para no patamar seguinte (buracos alternados) ✔;
   - os cofres saem pela própria porta ✔;
   - o portão do fundo leva à saída ✔.
7. **Santuários:**
   - S1 pátio, S2 portão da muralha, S3 torre do sino, S4 antes da câmara;
   - nenhum trecho obrigatório passa de ~3 minutos sem santuário;
   - há santuário antes do chefe ✔;
   - na descida não há santuário: se morrer, volta ao S4 ✔. É curta e sem armadilhas obrigatórias além das seteiras.

**Resultado: nenhum softlock.** Pontos de atenção para o playtest:
- **Seteiras das chaminés:** conferir se não atiram rápido demais para quem está na vinha.
- **Serras nas chaminés:** cruzam a chaminé inteira; se ficar difícil, encurtar o trilho.
- **Queda de 60 tiles na abertura:** confirmar que a câmera acompanha bem.
