# Plano de teste — Chefão Golden e o tomo

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080.
Antes: rode **Tools > Sete Vidas > 10. Chefão** e **Tools > Sete Vidas > Save > Apagar save** (começa com 7 corações).
Caminho: Position X do `Player` = 153 → agarre a borda da laje em x 156 → siga pela laje para a direita (santuário em x 172) → arena em x 180.
Depuração: selecione o Golden durante a luta e use o menu ⋮ do `GoldenBoss` para forçar salto, cura, relâmpagos ou marretada.

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| BOS-01 | Início | Jogo novo | Play | HUD com 7 corações cheios; só o Ladino | ☐ |
| BOS-02 | Entrada | Na laje | Entrar na arena | A grade de trás desce; conversa D-GOLDEN-00; depois a barra "Golden, Guardião do Tomo" aparece | ☐ |
| BOS-03 | Grade | Luta em andamento | Tentar voltar | A grade bloqueia | ☐ |
| BOS-04 | Marretada | Perto dele | Observar | Marreta erguida (~0,6 s) antes do golpe; recuar evita | ☐ |
| BOS-05 | Salto | ⋮ "salto" | Correr por baixo dele | Ele cai onde o gato estava; passando por baixo, sem dano | ☐ |
| BOS-06 | Punir o salto | Depois do salto | Atacar | Ele fica parado ~0,8 s | ☐ |
| BOS-07 | Cura | Vida abaixo de 70%, ⋮ "cura" | Esperar | Anel dourado; a barra sobe aos poucos | ☐ |
| BOS-08 | Interromper | Durante a cura | Bater ~3 vezes | "interrompido!", ele pisca atordoado ~1,5 s | ☐ |
| BOS-09 | Relâmpagos | ⋮ "relâmpagos" | Observar o chão | Colunas claras piscando ~1,2 s antes de cada raio; sempre uma faixa sem coluna; uma coluna onde o gato está | ☐ |
| BOS-10 | Lugar seguro | Relâmpagos | Ficar na faixa sem coluna | Sem dano em todas as ondas | ☐ |
| BOS-11 | 2ª fase | Vida abaixo da metade | Observar | "(furioso)" na barra; golpes mais rápidos; mais raios | ☐ |
| BOS-12 | Costas | Atrás dele | Golpear | "3 COSTAS" (nunca FURTIVO) | ☐ |
| BOS-13 | Morrer na luta | Perder os 14 hits | — | Volta ao santuário de x 172; a grade está aberta; o Golden está de vida cheia e espera | ☐ |
| BOS-14 | Vitória | Golden em 0 | — | Ajoelha; as duas grades sobem; conversa D-GOLDEN-DERROTA | ☐ |
| BOS-15 | Vitória salva | Depois de vencer | Parar e dar Play | O Golden continua ajoelhado; grades abertas | ☐ |
| TOM-01 | Tomo | Sala do tomo | Chegar perto | "E: pegar o tomo"; o livro flutua | ☐ |
| TOM-02 | Maldição | — | E | Narração; clarão roxo; corações se apagam um a um até sobrar 1 (meio cheio); o gato cai | ☐ |
| TOM-03 | Resgate | — | Avançar as falas | Tela escurece; o Golden aparece ao lado; juramento (D-GOLDEN-01) | ☐ |
| TOM-04 | Vínculo | Fim do juramento | — | +1 coração (2 no HUD); "Paladino desbloqueado!"; o gato levanta e volta o controle | ☐ |
| TOM-05 | Paladino | Depois do vínculo | Apertar 2 | Troca para o Paladino; quebra a parede do túnel (trecho I) | ☐ |
| TOM-06 | Persistência | Depois do vínculo | Parar e dar Play | 2 corações; Paladino disponível; pedestal vazio; Golden de pé na sala, conversa curta com E | ☐ |
| TOM-07 | Fechar no meio | Fechar o Play durante a maldição | Dar Play | Golden de pé ao lado do pedestal; E faz o juramento | ☐ |
| AUTO-07 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite:** morrer no exato golpe que derrota o Golden; o Golden saltando quando morre (cai no chão); relâmpagos ainda caindo quando ele é derrotado (somem); pausar no diálogo de entrada com um raio no ar; descansar no santuário com a luta em andamento (não dá: a grade fechou); voltar à arena depois do vínculo (o Golden não luta de novo).
