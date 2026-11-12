# Plano de teste — Cenário da Fortaleza (Etapa 9 do plano de assets)

Antes: rode **Tools > Sete Vidas > 13. Fase 6: Fortaleza** (aceite recriar a fase) e **Save > Apagar save**. Abra a Fortaleza e aperte Play. Mapa: `Docs/Level/fortaleza-mvp.md`. Guia: `Docs/Arte/guia-de-assets.md`.

Severidade (QA): **S1** bloqueante · **S2** mecânica central quebrada ou leitura que engana o jogador (ex.: enfeite confundido com plataforma ou perigo) · **S3** comportamento errado de pouco impacto · **S4** cosmético.

## 1. Leitura de gates e objetos de jogo

| ID | Onde | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| CEN-01 | Estábulo, porta do tomo, cofres, portão | Olhar cada parede rachada antes de ter o Paladino | Rede de rachaduras escuras, sem brilho dourado; nenhuma outra parede da fase tem rachadura | ☐ |
| CEN-02 | Muralhas, torres, poço | Olhar cada vinha | Hera com folhas, brilho e contorno escuro (#4f5a2e/#8a9a4e/#b8c46c/#232a14) só nas paredes escaláveis, visível de longe; nenhuma planta de enfeite tem esses verdes | ☐ |
| CEN-03 | Galeria, salão, fundos | Olhar as portas de madeira fechadas | Tranca de ferro ou tábuas em X; nenhum jogador tenta abrir mais de uma vez | ☐ |
| CEN-04 | Pátio, muralha, torres, poço | Olhar cada lançador | Bloco de pedra dentro da parede, boca de bronze virada para o lado do tiro, faixa vermelha; nenhum solto da parede (no editor e no jogo) | ☐ |
| CEN-05 | Armadilhas, torres, poço | Olhar cada serra | Trilho de ferro atrás, do começo ao fim do caminho, com um suporte em cada ponta | ☐ |
| CEN-06 | Galeria (x 170,5 e 179) | De pé e depois agachado sob os sininhos | De pé: tocam e a Sentinela vem (alarme, até 16 tiles); agachado: silêncio | ☐ |
| CEN-07 | Salão e armadilhas | Andar agachado nos cacos; depois pular por cima | Agachado: barulho; pulando: nada | ☐ |
| CEN-08 | Trilha e pátio | Correr e depois andar agachado nas folhas e na palha | Correndo: barulho; agachado: nada | ☐ |
| CEN-09 | Galeria e fundos | Correr contra o balde; descansar no santuário | Cai uma vez; volta em pé após o descanso | ☐ |
| CEN-10 | Pátio, galeria (x 162,3, 183,45, 208,6; passarela x 158 e 164,6) | Agachar atrás do feno e de cada caixote | O gato some; de pé, a cabeça aparece | ☐ |
| CEN-10b | Pátio, muralhas, torre de escalada | Passar agachado pela tina embaixo da gárgula; depois passar andando; depois cair em cima | O gato passa por ela (não empurra nem sobe). Agachado: fica em pé. Andando, correndo ou caindo: tomba de lado, derrama numa poça e faz barulho (a sentinela perto investiga). Descansar no santuário: volta ao lugar | ☐ |
| CEN-10c | Câmara, galeria, Torre do Sino | Olhar o chão embaixo de cada gárgula | Uma poça rasa com ondinha; nenhuma parece buraco nem plataforma | ☐ |

## 2. Primeiro plano e fundo

| ID | Onde | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| CEN-11 | Galeria, salão, câmara | Atravessar com inimigos na tela | Nenhuma corrente de primeiro plano cobre o gato, um inimigo, uma armadilha ou uma plataforma | ☐ |
| CEN-12 | Toda a fase | Procurar enfeite que pareça plataforma ou perigo | Nenhum (tapete, cano, vigas e arcos já foram trocados por isso) | ☐ |
| CEN-13 | Queda do penhasco | Cair da montanha até a lagoa | Uma troca suave de fundo (0,6 s), do pôr do sol para o crepúsculo | ☐ |
| CEN-13b | Lagoa | Fazer o salto de fé; depois pular de pouca altura na lagoa; andar dentro dela; sair | Salto de fé: respingo grande com dois anéis. Pulo curto: respingo pequeno. Andando: ondinhas nos pés. Saindo: pingos por meio segundo. Nenhum respingo fora da água | ☐ |
| CEN-14 | Pátio → muralhas → galeria | Subir as três muralhas | Nenhuma troca de fundo: o céu esquenta aos poucos e o horizonte baixa | ☐ |
| CEN-15 | Chaminés, porta do tomo, poço | Passar por cada soleira | Troca de fundo só ali (andar 72, câmara, noite) | ☐ |
| CEN-16 | Galeria, salão, armadilhas, câmara, fundos | Olhar as janelas | Mostram o céu daquela altura e andam com a paralaxe (borda do sol, meio sol, só céu, último raio, noite) | ☐ |
| CEN-17 | Toda a fase | Procurar objeto entrando na parede | Nenhum (tenda, santuários e caixinha de areia conferidos) | ☐ |
| CEN-17b | Estrada para Miaravéu e alto da montanha | Andar até o fim da estrada; depois andar até a ponta esquerda do começo | Nunca se vê a parede do fim do mapa nem o vazio: escurece aos poucos (pontilhado) até o preto; na saída, o gato some no escuro antes da tela final | ☐ |

## 3. Mundo vivo

| ID | Onde | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| CEN-18 | Corrida do salto, marco da trilha | Chegar a 4 tiles do pássaro | Levanta voo para o lado contrário do gato e só some fora da tela | ☐ |
| CEN-19 | Cozinha da galeria, fundos | Chegar a 3 tiles do rato | Corre até o buraquinho na parede e some nele | ☐ |
| CEN-20 | Trilha (vestígio Primata) | Chegar a 3 tiles do cristal e se afastar | Acende (3 quadros) e apaga de novo | ☐ |
| CEN-21 | Salão (altar) | Passar correndo pelas velas | A chama inclina para o lado do movimento | ☐ |
| CEN-22 | Torre do Selo, câmara | Olhar runas e veias | Pulsam fora de tempo; perto do tomo as runas falham de vez em quando | ☐ |
| CEN-23 | Salas de combate e armadilhas | Jogar a sala | Nenhum efeito faz o jogador reagir achando que é perigo | ☐ |
| CEN-24 | Santuário | Descansar | Escurece, o gato aparece cochilando na caixinha, acorda (boceja, espreguiça) e levanta; sem dano e sem controle até o fim | ☐ |

## 4. Poço com armadilhas

| ID | Onde | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| CEN-25 | Poço, cada buraco | Cair reto por cada buraco | Sempre para numa saliência embaixo (ninguém cai às cegas) | ☐ |
| CEN-26 | Poço, 72 → 43 | Descer da saliência quando a serra vai para a direita | Passa pela serra e pela linha das flechas sem dano; o tempo se repete igual depois de 5 min jogando | ☐ |
| CEN-27 | Poço, patamares 72, 43 e 15 | Quebrar cada cofre | A parede rachada continua acessível pela direita | ☐ |

## 5. Desempenho e orçamento

| ID | Onde | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| CEN-28 | Montanha, pátio, câmara | Profiler (CPU) parado e andando | 60 fps na máquina de teste | ☐ |
| CEN-29 | Toda a fase | Contar os elementos de fundo animados por tela | No máximo 6 (copas, arbustos, gotas) | ☐ |
| CEN-30 | Câmara | Ficar 30 s parado | Partículas vivas ≤ 40 (`AmbientEmitter.AliveInScene`) | ☐ |
| CEN-31 | Fora da área de um emissor | Andar para longe | As partículas somem e o emissor para | ☐ |

## 6. Testes automatizados

| ID | Teste | Status |
|---|---|---|
| AUTO-20 | EditMode: `SceneryLibraryTests`, `TerrainAutotileTests`, `NoisyObjectRulesTests`, `BackgroundZoneRulesTests`, `LivingWorldRulesTests` | ☐ |
| AUTO-21 | PlayMode `FortressTests`: enfeites da biblioteca (189), barulhentos (8), janelas (12), trilho das serras, troca de fundo, mundo vivo | ☐ |
| AUTO-22 | PlayMode `SanctuaryRestTests` | ☐ |

## 7. Playtest do cenário (3 a 5 pessoas)

1. **Build para Windows**, pessoas que não conhecem o projeto. Diga só os controles.
2. **Durante (anote):**
   - se **notam a parede rachada** na primeira passagem (antes do Paladino);
   - se **usam as sombras e os esconderijos** sem ser mandadas;
   - se **hesitam mais de 10 s** diante de algum enfeite (achando que é plataforma, passagem ou perigo); anote qual;
   - se tentam abrir as portas fechadas ou interagir com enfeites;
   - onde morrem no poço e quantas tentativas levam no trecho 72 → 43.
3. **Depois, perguntas abertas:**
   - "O que aconteceu neste castelo?" (sem ajuda). Conte quantos destes pontos aparecem: a Ordem vivia e treinava aqui; alguém tentou entrar antes (Guilda); a praga sobe do tomo; o tempo passou (dia → noite).
   - "Em algum momento você não soube se uma coisa era perigosa?" (nota 1–5 de clareza).
   - "O lugar parecia vivo?" (nota 1–5).
4. **Síntese:** separe o *observado* da *opinião*. Qualquer confusão de leitura que fez alguém tomar dano ou travar é **S2**; registre com o modelo de bug da skill de QA. Termine com no máximo 3 recomendações.

**Pronto quando:** nenhum S1/S2 de leitura, e pelo menos 3 de 5 pessoas contam a história do castelo com 2 ou mais dos pontos acima.
