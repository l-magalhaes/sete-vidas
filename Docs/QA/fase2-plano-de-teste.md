# Plano de teste — Fase 2 (escalada, adagas e ataque pelas costas)

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080 · valores padrão de `WallClimbSettings` e `RogueDaggers`.
Antes: rode **Tools > Sete Vidas > 5. Fase 2**. Para ir direto aos trechos novos, durante o Play mude o **Position X** do `Player` no Inspector (chaminé: x 90; bonecos: x 106).

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| CLB-01 | Agarrar no ar | Ao lado da parede de x 80 | Pular segurando D | Gruda na parede, virado para ela, e fica parado | ☐ |
| CLB-02 | Agarrar no chão | Encostado na parede de x 80 | Segurar D sem W | **Não** gruda (só anda contra a parede) | ☐ |
| CLB-03 | Agarrar no chão | Encostado na parede de x 80 | Apertar W | Gruda e começa a subir | ☐ |
| CLB-04 | Subir / descer | Escalando | Segurar W, depois S | Sobe a ~4 tiles/s, desce mais rápido; parado sem entrada | ☐ |
| CLB-05 | Topo | Escalando a parede de x 80 | Segurar W até o fim | Sobe para cima da parede sozinho e fica de pé (x 80–86, y 10) | ☐ |
| CLB-06 | Base | Escalando perto do chão | Segurar S | Solta ao tocar o chão e fica de pé (ou agachado, se ainda segurar S) | ☐ |
| CLB-07 | Soltar | Escalando no meio da parede | Segurar A (direção oposta) | Solta e cai; não regruda na hora | ☐ |
| CLB-08 | Pular da parede | Escalando | Apertar Espaço | Salta na diagonal para longe da parede (~2,5 tiles de altura) | ☐ |
| CLB-09 | Chaminé | Escalando a parede esquerda da chaminé (x 92, lado de dentro) | Espaço e depois segurar D | Agarra a parede da direita (x 97) | ☐ |
| CLB-10 | Saída da chaminé | Na parede direita da chaminé | Subir até o topo | Sobe sobre ela e anda para a plataforma (x 99–104) | ☐ |
| CLB-11 | Parede no ar | Na parede direita, descendo | Segurar S até a base (y 3) | Solta e cai quando a parede acaba | ☐ |
| CLB-12 | Parede comum | Encostado no limite cinza (x 120) ou num degrau | Pular segurando D e W | **Não** escala (no máximo agarra a quina, como na Fase 1) | ☐ |
| CLB-13 | Agachado | Agachado encostado na parede de x 80 | Apertar W | Não escala | ☐ |
| CLB-14 | Visual | — | Olhar as paredes de x 80, 92 e 97 | Leem diferente do chão (ranhuras + hera seca) sem precisar explicar | ☐ |
| CMB-01 | Golpe | No chão | Clique esquerdo | Um golpe: corte aparece à frente por um instante | ☐ |
| CMB-02 | Combo | No chão | Dois cliques rápidos | Dois golpes seguidos (o 2º corte é diferente) | ☐ |
| CMB-03 | Limite do combo | No chão | Três cliques rápidos | Só dois golpes; o 3º clique é ignorado | ☐ |
| CMB-04 | Direção | Golpe começando olhando para a direita | Virar para a esquerda durante o golpe | O corte continua à direita | ☐ |
| CMB-05 | Movimento | Andando | Atacar enquanto anda e pula | O gato continua andando/pulando normalmente | ☐ |
| CMB-06 | Agachado | Agachado | Atacar | Corte sai mais baixo, na altura do corpo agachado | ☐ |
| CMB-07 | Bloqueios | Pendurado numa borda / escalando | Atacar | Nada acontece | ☐ |
| BCK-01 | De frente | Boneco de x 110,5 (olha para a direita), gato à direita dele | Acertar | Aparece **2** (branco); trava curta | ☐ |
| BCK-02 | Furtivo | Boneco calmo, gato à esquerda dele (costas) | Acertar | Aparece **5 FURTIVO** (vermelho); trava visivelmente maior | ☐ |
| BCK-03 | Costas em combate | Logo após BCK-02 (menos de 3 s) | Acertar pelas costas | Aparece **3 COSTAS** (laranja) | ☐ |
| BCK-04 | Fim do combate | Esperar 3 s sem bater | Acertar pelas costas | Volta a ser **5 FURTIVO**; o rótulo do boneco volta para "calmo" | ☐ |
| BCK-05 | Outro lado | Boneco de x 115,5 (olha para a esquerda) | Acertar vindo da direita | É pelas costas (FURTIVO/COSTAS) | ☐ |
| BCK-06 | Um acerto por golpe | Encostado no boneco | Um clique | O número aparece uma vez só | ☐ |
| BCK-07 | Derrota | Boneco com 10 de vida | Derrubar | Some e volta com 10/10 em ~2 s | ☐ |
| BCK-08 | Atravessar | Andando | Passar pelo boneco | O gato atravessa sem travar | ☐ |
| AUTO-02 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite a observar:** pular da parede e agarrar a mesma parede de novo; chegar ao topo com um teto logo acima; escalar e apertar ataque ao mesmo tempo; acertar o boneco exatamente alinhado com ele (deve contar como de frente); dois golpes no boneco durante o hitstop; apertar ataque durante a subida de uma borda.

**Pronto quando** (documento "Primeiros passos"): o gato sobe uma parede, desce, pula para outra e derrota o boneco. O ataque pelas costas se *sente* claramente mais forte que o comum.
