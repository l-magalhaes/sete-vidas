# Plano de teste — Floresta F1 (base técnica)

Antes: rode **Tools > Sete Vidas > 20. Floresta F1: cenas de teste (A e B)**. A cena `Scenes/World/Forest/Forest_Test_A` abre sozinha: aperte Play nela.
As cenas de teste deixam o save como o de quem saiu da Fortaleza: **Paladino liberado e 2 corações** (`ForestTestBootstrap`). Elas usam o **save da área de testes** (o mesmo da Playground), nunca o do jogo.
Para o Play começar na cena aberta, desmarque **Tools > Sete Vidas > Play começa na tela inicial**.
Para repetir do zero, apague só `save_playground.json` (**Tools > Sete Vidas > Save > Abrir pasta do save**). Cuidado: **Save > Apagar save** apaga também o save do jogo.
Arte provisória (blocos e sprites simples). O que está em teste é o comportamento.

## Testes automáticos

Test Runner > EditMode > `ForestFoundationTests` (26 testes) e `ProjectSetupTests` (a layer **Platform** no índice 15).

## Roteiro manual

| # | Passo | Esperado |
|---|---|---|
| 1 | Encoste no santuário da esquerda (Clareira de teste) e aperte E | Descansa (só há um santuário visitado: sem menu) |
| 2 | Ande até o tronco rachado e bata com a **adaga** (Ladino) | Faísca e "tink"; o tronco não perde vida |
| 3 | Troque para o **Paladino** e bata 3 vezes com a marreta | Lascas de madeira, som grave; quebra no 3º golpe |
| 4 | Abra o baú W01 (E) | Tampa abre, aviso "Madeira de Reconstrução (1/12)"; inventário (Tab) > Itens: "Madeira de Reconstrução 1/12" |
| 5 | Entre no poço: pise no **cogumelo** | Quica ~6 tiles sem atacar; o chapéu amassa |
| 6 | Suba pelos **galhos de mão única** pulando por baixo deles | Atravessa por baixo e para em cima; não bate a cabeça |
| 7 | Em cima de um galho: **baixo + pulo** | Cai através do galho (não pula) |
| 8 | No chão sólido: baixo + pulo | Pulo agachado normal (não atravessa o chão) |
| 9 | Fique na **folha larga** | Afunda devagar (até 3 tiles); ao sair, ela volta |
| 10 | Pise no **galho que balança** | Afunda um pouquinho e volta |
| 11 | Pise no **galho podre** | Treme, cai em ~0,6 s e volta em 5 s. Na Floresta, o Lion pode reclamar num balão ("Ah, qual é. Até os galhos?") |
| 12 | Caia de 10 tiles ou mais (do topo do poço até o chão) | Balão curto do Lion sobre a cabeça ("Caí de pé. Claro." etc.), no máximo um por minuto |
| 13 | Caia por mais de 1 s; depois, agachado e parado no chão por 1 s | A câmera desce (antecipa o pouso / mostra embaixo) e volta ao normal |
| 14 | No alto do poço, como **Ladino**: pule e ataque para baixo (baixo + ataque, no ar) no **cacto** do lugar do galho 7 | Quica alto (~7 tiles) e alcança o patamar do baú e a borda da direita. Encostar no cacto sem atacar: perde meio coração e é empurrado |
| 14b | Abra o baú W02 no topo do poço | Madeira 2/12 e +5 moedas |
| 14c | Atravesse a borda para o lado dos cactos e desça quicando nos 3 cactos (e, se cair, nos espinhos do chão) até a plataforma alta e o corredor | Cada estocada para baixo quica; cair nos espinhos sem atacar tira meio coração e volta ao último chão seguro. Com o Paladino a marreta não quica (queda pesada) |
| 15 | Pelo corredor no alto, vá até a porta da direita | Escurece, carrega a **Forest_Test_B**, o gato aparece na chegada olhando para a direita, **com a mesma vida** |
| 16 | Na B, encoste no santuário "Vale de teste" e aperte E | Abre o **menu do santuário**: Descansar / Viajar: Clareira de teste |
| 17 | Escolha **Viajar: Clareira de teste** | Fade, carrega a A, o gato aparece de pé no santuário da A, de vida cheia |
| 18 | Volte para a B pela porta | O **tronco da A continua quebrado** e os **baús continuam abertos** |
| 19 | Na B, quebre o tronco e abra o baú W03; ande até o fundo do corredor | A porta interna leva para o patamar alto **sem trocar de cena** |
| 20 | Desça pelos galhos de mão única | Baixo + pulo funciona em cada um |
| 21 | Descanse na B, volte para a A e morra (encostar nos cactos até acabar a vida, ou "Depuração: tomar 1 hit" no PlayerHealth) | Renasce na **B** (o último santuário de descanso é de outra cena) |
| 22 | Feche o Play e aperte Play na A de novo (ou Continuar no menu) | Tronco quebrado, baús abertos, madeira 3/12, destinos da viagem lembrados |
| 23 | Envenenado (se houver zumbi por perto) ou durante a troca de cena, aperte E no santuário | Sem viagem: só descansa |

## O que fica para depois (não é bug)

- **Tela de mapa** (aba nova do inventário): ainda não existe; é o próximo passo da F1.
- Sons próprios da Floresta (o tronco usa o som da parede, mais grave; o baú usa o da moeda).
- Sem música nas cenas de teste.
