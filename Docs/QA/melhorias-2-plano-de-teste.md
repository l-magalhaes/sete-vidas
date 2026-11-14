# Plano de teste — Melhorias 2 (14/11/2026, depois da F4b)

Pedidos do usuário: (1) corações temporários em baús; (2) escorregar nas rampas; (3) roda de troca rápida de classe; (4) Caçador detalhado no inventário.

Antes: rode o menu **21** (Floresta; `ForestLayout` versão **13**: baús com coração). O gato é reconstruído pelo mesmo menu.

## Testes automáticos

- EditMode > `TempHeartSlideWheelTests` (7): temporários absorvem primeiro, limite de vagas, desenho cheio/meio; escorregão (lado, aceleração, máxima, embalo); fatias da roda.
- PlayMode > `SlopeTests`: o teste "parado na rampa não escorrega" virou **`StandingOnWalkableSlope_SlidesDown`** (o comportamento mudou a pedido do usuário). Rode o PlayMode inteiro: a rampa mexe no controlador.

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | 1 | Abra o baú **W13** (recompensa da arena do D3) | "+1 coração temporário" em verde-água; um coração **verde-água respirando** aparece depois dos seus |
| 2 | 1 | Leve um golpe | O temporário some primeiro (meio por golpe); seus corações ficam inteiros |
| 3 | 1 | Cure com a magia do Paladino | Cura os corações normais; o temporário **não volta** |
| 4 | 1 | Descanse num santuário; troque de cena | O temporário continua |
| 5 | 1 | Morra | O temporário some |
| 6 | 1 | Veneno com temporário | Os tiques tiram o temporário primeiro |
| 7 | 1 | Com 2 temporários, abra outro baú com coração (W08, W10, W17) | "Sem espaço para mais um coração" (limite: 2) |
| 8 | 2 | Pare numa rampa (Fortaleza: margem da lagoa; Floresta: E1–E3) | O gato **escorrega** rampa abaixo, agachado, levantando poeira, cada vez mais rápido |
| 9 | 2 | Segure a direção **rampa acima** | Sobe andando (as rampas da rota continuam subíveis) |
| 10 | 2 | Segure rampa abaixo | Escorrega mais rápido |
| 11 | 2 | Pule no fim do escorregão / saia pela ponta | O embalo continua no ar e no chão (cai devagar até a velocidade de andar): a rampa "joga" o gato |
| 12 | 2 | Leve dano escorregando | O recuo corta o embalo |
| 13 | 3 | Com 2+ classes, **toque** R / RB (ou Q / LB) | Troca para a próxima, como antes (agora na soltura do botão) |
| 14 | 3 | **Segure** R / RB | Abre a **roda**: 7 fatias, Ladino em cima, sentido horário; o jogo em câmera lenta; as classes que faltam escuras com "?" |
| 15 | 3 | Aponte com o direcional, o analógico ou o mouse | A fatia acende na cor da classe; no meio, o nome e "n / total" |
| 16 | 3 | Solte o botão | Troca para a classe apontada; tempo volta ao normal |
| 17 | 3 | Solte apontando uma classe bloqueada ou sem apontar | Fica na classe atual |
| 18 | 3 | Com diálogo ou inventário aberto, segure R | A roda não abre |
| 19 | 4 | Inventário > Classes > Caçador | Arco explicado (frente, para cima, uma flecha por vez, 12 passos, silencioso, não quebra pedra, pogo das adagas) + o que a Vulpis ensinou (dash e flecha com corda) |
| 20 | 4 | Inventário > Habilidades | **Dash** e **Flecha com corda** com a descrição completa (6 passos, um por pulo, o que devolve; mira, 9 passos, pendurado) |
| 21 | 4 | Classe Paladino | Agora mostra também a cura (o campo de magias não aparecia antes) |

## Riscos conhecidos

- **Rampas da Fortaleza e da Floresta**: todas escorregam agora. Trechos de furtividade em rampa (agachado parado) passam a deslizar: conferir no playtest se algum esconderijo ficou numa rampa.
- **Toque de troca de classe** passou do aperto para a soltura (até 0,22 s depois).
- **Corações temporários** ficam no save em memória e vão para o disco no próximo salvamento (santuário, baú).
