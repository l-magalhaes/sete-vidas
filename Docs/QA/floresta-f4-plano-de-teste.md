# Plano de teste — Floresta F4: bestiário infectado (14/11/2026)

Antes:
1. Rode **Tools > Sete Vidas > 21. Floresta F2: graybox (7 cenas)**. O `ForestLayout` subiu para a **versão 11**: as cenas são recriadas e os inimigos marcados ganham o componente `Infected`. Na primeira vez o menu gera os sprites `*_infectado.png` em `Art/Environments/Forest/Placeholder` (troca de paleta: roxo e verde-ácido).
2. Não muda terreno: não precisa rodar o 22 (mapa do mundo) de novo.

Como reconhecer um infectado: tons roxos, olhos e rachaduras verde-ácido, um pontinho verde subindo de vez em quando. Na cena, o nome do objeto termina em "(infectado)".

## Testes automáticos

- EditMode > `ForestBestiaryTests` (10 testes): números de cada variante infectada, zigue-zague, cacto (inchar e cruz), tremida, nuvem tóxica.
- Rode também os anteriores da Floresta: `ForestFoundationTests`, `ForestGrayboxTests` (as regras normais não mudaram).
- Alcance sem o Unity: `Tools/LevelPreview/checar_floresta.ps1` → "Tudo alcançável, sem softlock" (já conferido).

## Onde estão os infectados

| Cena | Infectados |
|---|---|
| A (Raízes) | nenhum: o gato aprende os bichos normais primeiro |
| B | B1 planta · B2 planta · B3 serpente e planta · B4 morcego · B5 morcego |
| C | C1 morcego · C2 serpente · C3 Galho Seco · C4 morcego · C5 planta e morcego · C6 planta · C7 Broto e serpente |
| D | D1, D2 serpente · D3 (Refúgio) 3 morcegos, planta e Broto |
| E (Ninho) | todos (menos cactos): E1 serpente e planta · E2 planta · E3 serpente e 2 morcegos · E5 serpente e planta · E6 serpente |
| F | G3 morcego |

Cactos: **todos normais** na Floresta (o cacto infectado está pronto no código e fica para outras fases).

## Roteiro manual

| # | Inimigo | Passo | Esperado |
|---|---|---|---|
| 1 | Galho Seco (C3) | Aproxime-se devagar, sem estar escondido, e olhe o tronco | Antes de acordar, os **olhos verdes piscam** de vez em quando (dá para notar o tronco falso) |
| 2 | Galho Seco | Deixe-o atacar | Braço sobe e bate **mais rápido** que o normal (0,4 s); fica preso **menos** (0,7 s): o pogo e o golpe pelas costas têm janela menor |
| 3 | Galho Seco | Derrube-o (tem 1 de vida a mais que o normal) | Vira uma **nuvem verde e roxa** 2 × 2 no pé do tronco por 2 s; entrar nela **envenena uma vez** (não fica letal sozinha) |
| 4 | Broto (C7, D3) | Fique perto | Solta morcegos como o normal; os morcegos que saem dele também são **infectados** |
| 5 | Planta (B1) | Fique a ~6,5 tiles na frente dela | A infectada **alcança** (a normal, não): o pescoço estica até 7 |
| 6 | Planta | Desvie do bote e acerte a cabeça na volta (pogo) | O pogo funciona na volta (flor turquesa **continua turquesa**), mas a volta é mais curta (0,5 s) |
| 7 | Planta | Acerte a cabeça | Sai uma **nuvem de esporos** de 1,5 tile em volta da cabeça (uma de cada vez); ficar nela envenena uma vez |
| 8 | Serpente (B3) | Ande em pé até 4 tiles do buraco | A infectada dá o bote a **4** (a normal só a 3) |
| 9 | Serpente | Agache na folhagem/sombra perto dela | **Não** dá o bote (a folhagem ainda esconde) |
| 10 | Serpente | Leve a mordida | Meio coração **e veneno** (um só, mesmo encostando de novo durante a invencibilidade) |
| 11 | Serpente | Tente o pogo com ela esticada | Janela menor (0,4 s); volta para o buraco mais cedo |
| 12 | Morcego (B4) | Compare com um normal: chegue andando e faça barulho a meia distância | O infectado acorda de mais longe (9 tiles em vez de 7) e com barulho menor (0,2 em vez de 0,3) |
| 13 | Morcego | Deixe-o perseguir | Voa **mais rápido em zigue-zague** (de lado, em degraus); depois de encostar, recua pouco e **volta logo** |
| 14 | Morcego | Mate-o | Aguenta 3 golpes (o normal, 2) |
| 15 | Cacto (E2, G2, B2) | Olhe e quique neles | Normais, como antes da F4 (não tremem nem disparam) |
| 16 | Todos | Descanse num santuário / morra | Os infectados voltam vivos e infectados; nuvens no ar somem |
| 17 | Todos | Inimigos **normais** (A, e a maioria do B) | Iguais a antes da F4 |

## Riscos conhecidos

- **Sons**: os infectados usam os mesmos sons provisórios (os do zumbi), como os normais; os espinhos do cacto usam o "virote_crava". Sons próprios ficam para a F10/sound design.
- **Arte**: a troca de paleta é automática (placeholder). O gomo da Serpente é feito na hora e só ganha a tinta roxa.
- **Distribuição** é [PROPOSTA]: ajuste a parte de cada trecho em `ForestLayout.InfectionShare` e rode o 21 de novo.
