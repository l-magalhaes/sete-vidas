# Plano de teste — Armadilhas

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080.
Antes: rode **Tools > Sete Vidas > 11. Armadilhas**. Do spawn (x 0,5), ande para a **esquerda**: o circuito vai de x -8 a x -74.
Dica: ligue Gizmos para ver as áreas de perigo (vermelho) e, com o `Player` selecionado, o último chão seguro (círculo azul).

| ID | Armadilha | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| TRP-01 | Espinhos | x -8 | Andar para os espinhos (x -12,5 a -9,5) | "-1 hit", tela pisca, volta ao chão antes dos espinhos | ☐ |
| TRP-02 | Espinhos | x -8 | Pular por cima | Sem dano | ☐ |
| TRP-03 | Fio | De pé | Andar até o fio (x -17) | Fio arrebenta; brilho no lançador; 3 flechas na altura do peito; "?" em Sentinelas por perto (não há aqui) | ☐ |
| TRP-04 | Fio | Agachado | Passar pelo fio e pela passagem baixa (x -22) | Nada dispara; passa por baixo do pilar | ☐ |
| TRP-05 | Flechas | Fio disparado | Agachar quando as flechas vêm | Passam por cima | ☐ |
| TRP-06 | Fio | Disparado | Esperar 3 s | Fio volta | ☐ |
| TRP-07 | Placa | De pé | Pisar na placa (x -26) | Placa afunda; chão treme; lanças sobem (x -29 a -25) | ☐ |
| TRP-08 | Placa | Agachado | Andar sobre a placa | Não afunda | ☐ |
| TRP-09 | Chamas | x -29 | Observar | Fagulhas antes de cada chama; os jatos acendem em onda (um depois do outro) | ☐ |
| TRP-10 | Chamas | x -29 | Atravessar pelas 3 plataformas (topo em 3) | Sem dano | ☐ |
| TRP-11 | Chamas | Em cima de uma plataforma | Esperar | A chama não alcança | ☐ |
| TRP-12 | Serra | x -47 | Pular por cima ou esperar na plataforma (x -55 a -51) | Passa sem dano; a serra pausa nas pontas | ☐ |
| TRP-13 | Frágeis | x -61 | Pular nas duas plataformas sem parar | Tremem e caem atrás do gato; ele chega a x -72 | ☐ |
| TRP-14 | Frágeis | Parar em cima | — | Cai nos espinhos: "-1 hit" e volta ao chão entre a serra e o poço (~x -60) | ☐ |
| TRP-15 | Frágeis | Depois de cair | Esperar 3 s | A plataforma volta | ☐ |
| TRP-16 | Morte | Com 1 hit restante (⋮ "tomar 1 hit") | Tocar espinhos | Morte normal: volta ao santuário | ☐ |
| TRP-17 | Barulho | Sentinela por perto (arraste uma cópia do prefab Tripwire para perto da Sentinela, x ~130) | Disparar o fio | A Sentinela ouve e vai investigar | ☐ |
| AUTO-08 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite:** tocar espinhos durante a invencibilidade (nada); tocar espinhos pendurado numa borda; serra e chama no mesmo instante (só 1 hit); disparar o fio pulando baixo; morrer no fogo com a placa disparada (rearma ao voltar); o chão seguro nunca ser em cima de uma plataforma frágil.
