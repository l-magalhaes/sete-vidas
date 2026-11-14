# Plano de teste — Melhorias gerais (14/11/2026, antes da Floresta F4)

Antes:
1. Abra `Scenes/World/Fortress_MVP` e rode **Tools > Sete Vidas > 13e. Fortaleza: atiradores do meio (sem recriar a fase)**. Ele move só os dois atiradores e mantém os ajustes feitos à mão. Se o Unity perguntar "o mapa da Fortaleza mudou" (versão 18), responda **Depois** e use o 13e.
2. Rode **Tools > Sete Vidas > 22. Mapa do mundo: gerar silhuetas (Fortaleza + Floresta)**. Ele abre cada cena por trás, lê o terreno e grava `Resources/WorldMap.asset`. Console: "Mapa do mundo: 8 cenas". Se faltar alguma cena da Floresta, gere antes no menu 21.

## Testes automáticos

- EditMode > `GeneralImprovementsTests` (18 testes): golpe para cima, dano pelos corações, repulsa do Koda, mapa do mundo (compactação, exploração, pintura, enquadramento) e transições.
- Rode também os anteriores: `BossRulesTests`, `ComboRulesTests`, `CombatRulesTests`, `ForestFoundationTests`, e o PlayMode inteiro (`BossTests`, `HealingSpellTests`, `SanctuaryRestTests`, `CrossbowSentinelTests`).

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | 1 | Ladino no chão: segure **W/↑** e ataque | Um golpe só, meia-lua clara **em cima da cabeça**; sem combo, sem avanço |
| 2 | 1 | O mesmo pulando, e debaixo de um morcego ou de um inimigo numa plataforma acima | Acerta quem está em cima |
| 3 | 1 | Paladino: segure **W/↑** e ataque | Golpe para cima com a marreta (caixa maior, meia-lua dourada); segurar o ataque **não** carrega o terremoto |
| 4 | 1 | Ataque sem segurar cima, e agachado + ataque | Combo normal e golpe agachado, como antes |
| 5 | 2 | Depois da maldição, com **1 coração**, bata no boneco de treino | Adaga: 2 de frente (como antes) |
| 6 | 2 | Com **2 corações** (Paladino liberado) | Adaga: **3** de frente (2 × 1,3 = 2,6); marreta e terremoto também sobem 30% |
| 7 | 2 | Antes da maldição (7 corações do Ladino, início da Fortaleza) | Dano de sempre (sem bônus) |
| 8 | 3 | Luta com o Koda: quique na cabeça dele com a estocada para baixo, várias vezes | No 2º golpe de cima seguido (ou depois de ~0,6 s em cima) ele ergue a marreta, **brilho dourado sobe da cabeça** (aviso), e uma **coluna de luz** joga o gato para o lado: meio coração |
| 9 | 3 | Fique parado em pé em cima dele | Mesma repulsa depois de ~0,6 s |
| 10 | 3 | Um pogo só para passar por cima dele | Passa sem repulsa |
| 11 | 3 | Durante a cura dele, o atordoado e os relâmpagos | Não há repulsa (são as janelas do jogador) |
| 12 | 4 | Chegue a um santuário com outro já visitado na mesma zona | **Dois avisos juntos**: [E/B] descansar e, acima, [clique/X] viagem rápida |
| 13 | 4 | Aperte **E / B** | Descansa direto (sem menu) |
| 14 | 4 | Aperte **clique / X** | O gato **não golpeia**; abre "Viagem rápida" só com os santuários. Escolher leva até lá; Esc/B fecha |
| 15 | 4 | Santuário sem outro visitado, ou envenenado | Só o aviso de descansar |
| 16 | 5 | Em jogo, aperte **M** (ou **Exibir** no controle) | Inventário abre direto no mapa, **centrado na cabeça do Lion** (piscando). Apertar de novo fecha |
| 17 | 5 | Na Fortaleza, olhe o mapa | Só aparece o que você já viu, em salas lisas; nada das áreas onde não foi |
| 18 | 5 | Segure o direcional; aperte Enter/A | O mapa anda; o zoom troca 1× → 2× → 3× → 1× |
| 19 | 5 | Ande um pouco e reabra o mapa | O trecho novo aparece; santuários dos trechos vistos; **baús só depois de abertos** |
| 20 | 5 | Na Floresta, entre numa sala e abra o mapa | A **sala inteira** aparece, separada das vizinhas por um vão, com traços nas portas; o título mostra "Floresta Silenciosa" |
| 21 | 5 | Descanse, feche e use Continuar | O que foi explorado continua no mapa |
| 22 | 6 | Encoste num zumbi (veneno) e use a cura (F / LT) | O veneno **para** (o ícone some, sem mais tiques) e cura 1 coração |
| 23 | 6 | Envenenado com a vida cheia, use a cura | Corta o veneno e gasta um uso |
| 24 | 7 | Galeria da muralha (andar 43): entre pelo portão na luz | O **atirador na ponta da passarela** vê de longe e mira quase na hora |
| 25 | 7 | O mesmo pela sombra A e agachado atrás dos caixotes da passarela | Ele demora a perceber ou não percebe |
| 26 | 7 | Salão dos Votos (andar 72): suba a torre do sino | O **atirador na plataforma 1** está virado para você e atira quando você atravessa o salão |
| 27 | 7 | Chegue nele pelo lado do fogo (por trás) | Ataque pelas costas funciona |
| 28 | 8 | Na Floresta, passe por uma porta lateral | O gato **sai andando** enquanto escurece e **entra andando** na outra sala enquanto clareia; depois o controle volta |
| 29 | 8 | Caia por um buraco no chão para outra cena | Continua caindo (tropeçando) enquanto escurece |
| 30 | 8 | Estrada: deixe a ponte desabar e caia | A tela escurece mais devagar com o gato tropeçando na queda; na A1 ele **despenca do alto**, bate no chão (poeira, tremor, som pesado), **fica deitado** um instante e **levanta**; "Ugh. Caí de novo." |
| 31 | 8 | Saia da Fortaleza pela luz do fim (D-FIM) | Sai andando para a direita; entra andando na Estrada |
| 32 | 8 | Viagem rápida e morrer | Como antes: fade e o gato de pé no santuário |

## Riscos conhecidos

- O golpe para cima usa a pose do 1º golpe do combo (ainda não há arte própria `dagger_up_*`/`hammer_up_*`).
- O mapa da Fortaleza usa salas desenhadas à mão (`FortressLayout.MapRooms`): se uma área que você mudou à mão ficar de fora, avise.
- Entrar andando dura 0,3 s (~2 tiles): se uma entrada ficar perto de um perigo, avise a sala.
