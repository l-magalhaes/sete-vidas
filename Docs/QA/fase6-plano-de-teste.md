# Plano de teste — Fase 6 (Fortaleza e playtest do MVP)

Antes: rode **Tools > Sete Vidas > 13. Fase 6: Fortaleza** e **12. Tela inicial**; depois **Save > Apagar save**. Abra `Scenes/MainMenu/MainMenu` e aperte Play.
Mapa e fichas: `Docs/Level/fortaleza-mvp.md`.

## Percurso (QA)
| ID | Sala | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| FRT-01 | Menu | Novo jogo | Abre a Fortaleza na entrada (x 3), 7 corações | ☐ |
| FRT-02 | Menu | Área de testes | Abre a Playground; o progresso da Fortaleza não muda (save separado) | ☐ |
| FRT-03 | 1 Entrada | Andar para a direita | Narração de chegada (uma vez só) | ☐ |
| FRT-04 | 1 Entrada | Pedras e fosso | Sobe as pedras de 2 e 3; pula o fosso de 4; cair nos espinhos volta ao chão antes do fosso | ☐ |
| FRT-05 | 2 Pátio | De pé no fio | Flechas da boca da passagem | ☐ |
| FRT-06 | 2 Pátio | Agachado | Passa o fio e a passagem baixa sem disparar nada | ☐ |
| FRT-07 | 3 Muralha | Ver a parede rachada embaixo da hera | Lê como quebrável; adagas fazem "tink"; não dá para pular por cima | ☐ |
| FRT-08 | 3 Muralha | Escalar a hera | Chega ao andar de cima; santuário S1 logo ali | ☐ |
| FRT-09 | 4 Corredor | Passar pela passarela e soltar pendurado | Sem "?" | ☐ |
| FRT-10 | 4 Corredor | Sombra/mato + golpe pelas costas | 5 FURTIVO derruba | ☐ |
| FRT-11 | 4 Corredor | Esbarrar no fio da sombra B | Barulho: a Sentinela vem investigar | ☐ |
| FRT-12 | 5 Salão | Lutar com as duas | Vence; às vezes cai meio coração | ☐ |
| FRT-13 | 6 | S2 e corredor das armadilhas | Fogo pelas plataformas, serra, plataformas frágeis | ☐ |
| FRT-14 | 7 | S3 → câmara | Grade fecha; luta; vitória abre o tomo | ☐ |
| FRT-15 | 7 | Tomo | Maldição 7 → 1, juramento, Paladino, 2 corações | ☐ |
| FRT-16 | Volta | Voltar à muralha, descer, 2 marretadas na parede | Quebra; a saída abre | ☐ |
| FRT-17 | Saída | Entrar na saída | Conversa final e volta à tela inicial | ☐ |
| FRT-18 | Continuar | No menu, Continuar | Volta ao último santuário da Fortaleza | ☐ |
| FRT-19 | Morte | Morrer em cada sala | Volta ao último santuário; nada trava | ☐ |
| FRT-20 | Tempo | Cronometrar a primeira jogada | Início → fim em ~8–15 min | ☐ |
| AUTO-10 | Testes | Test Runner | Todos passam | ☐ |

## Playtest (GDD §13)
1. Faça uma **build para Windows** (File > Build Profiles: a tela inicial já é a primeira cena) e chame **3 a 5 pessoas** que não conhecem o projeto.
2. **Antes:** só diga "WASD/setas, Espaço, S, clique, E". Peça para pensarem em voz alta.
3. **Durante (anote):** onde morrem; onde ficam paradas por mais de 10 s; se **notam a parede rachada** na primeira passagem; se usam **furtividade sem ser mandadas**; quantas vezes trocam de classe depois do Paladino.
4. **Depois:** nota de 1 a 5 para cada pergunta:
   1. Foi divertido controlar o gato só como Ladino?
   2. Você sentiu que tinha escolhas entre se esconder e lutar?
   3. Ganhar o Paladino mudou sua forma de jogar?
5. **Síntese:** separe o *observado* da *opinião* e termine com no máximo 3 recomendações. Se alguma resposta for "não", volte à fase correspondente antes do Vertical Slice.

| Jogador | Mortes (onde) | Notou a parede? | Usou furtividade? | Trocas de classe | P1 | P2 | P3 | Observações |
|---|---|---|---|---|---|---|---|---|
| 1 | | | | | | | | |
| 2 | | | | | | | | |
| 3 | | | | | | | | |
