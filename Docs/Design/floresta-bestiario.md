# Floresta Silenciosa — bestiário (F0)

> Dano em hits (1 hit = meio coração; o gato tem 4 hits depois da maldição). Vida no mesmo padrão dos atuais
> (Sentinela 5, Zumbi 6, Koda 45). Velocidades em tiles/s. Status: **[PROPOSTA]**, a balancear no playtest.
> Pogo por estado: `floresta-mecanicas.md` §1.

## Variante infectada (regra geral)

Componente `Infected` sobre a mesma IA: troca parâmetros e paleta. Mesmas cores da praga da Fortaleza: **roxo #5b2a6e** e **verde-ácido #8fd14f**, olhos verde-ácido e rachaduras que brilham. Distribuição: até metade do Vale, 1 infectado para cada 3 normais; do Bosque Morto (C7) em diante, metade e metade; no Ninho, só infectados.

---

## Sentinela Canino Infectado (Estrada, Vale)

Base: `ZombieAI` (vaga, sente, persegue a 3,2; toque envenena). Silhueta militar: elmo, tabardo vermelho rasgado.

| Variante | Vida | Ataque | Diferença |
|---|---|---|---|
| Espada | 6 | golpe de espada: alcance 1,5, aviso 0,5 s, **1 hit** + veneno do toque | ataca quando o gato está perto, além do toque |
| Escudo | 6 | só o toque (veneno) | **bloqueia de frente**: a adaga ricocheteia (faísca, sem dano). Quebra a guarda: **2 golpes de marreta** de frente, ou ataque pelas costas |

Ensina (R2): ir pelas costas como Ladino, ou usar o Paladino.

## Morcego Noturno (A, B, D)

| | Normal | Infectado |
|---|---|---|
| Vida | 2 | 3 |
| Repouso | dorme pendurado de cabeça para baixo | idem, com brilho verde |
| Desperta | barulho ≥ 0,3 a até 6 tiles, ou gato a 3 tiles **em pé** (agachado passa) | barulho ≥ 0,2, ou gato a 4 tiles |
| Ataque | 0,4 s de aviso (asas abrem, guincho) → investida em linha reta a 6 até a posição do gato, **1 hit**; volta a um teto próximo | **3 investidas seguidas**, voo em zigue-zague |
| Pogo | não | não |

## Galho Seco (A, C, D)

Humanoide de madeira, 2 tiles de altura.

| | Normal | Infectado |
|---|---|---|
| Vida | 5 | 6 |
| Disfarce | parado como tronco seco (sem olhos) até o gato chegar a 3 tiles **sem estar escondido** | idem, mas os olhos verdes piscam de vez em quando (dá para notar) |
| Movimento | patrulha a 1,5 | 2,2 |
| Ataque | braço levanta (aviso **0,6 s**) e bate no chão à frente: alcance 2, **1 hit** | aviso 0,4 s |
| Vulnerável | **1,0 s** com o braço preso no chão: pogo + dano dobrado nas costas | 0,7 s |
| Ao morrer | vira lenha | **nuvem tóxica** 2 × 2 tiles por 2 s: veneno (`PoisonRules`) |

## Cacto Flutuante (B, F)

Ponto de pogo móvel. **Não tem vida** (não morre).

| | Normal | Infectado |
|---|---|---|
| Movimento | trajetória fixa (linha ou círculo) a 1,5 | trajetória com tremidas aleatórias de até 0,5 tile |
| Perigo | contato lateral ou por baixo: **1 hit** | idem + **4 espinhos** em cruz a cada 3 s (aviso: incha 0,5 s), alcance 5, 1 hit |
| Pogo | sempre, pelo topo | sempre, pelo topo |

## Planta Carnívora (B, C, F)

Estacionária, presa ao chão ou à parede.

| | Normal | Infectada |
|---|---|---|
| Vida | 4 | 5 |
| Gatilho | gato a 2,5 tiles (frente e acima) | 3,5 tiles |
| Ataque | boca abre (aviso **0,5 s**) e morde: **1 hit** | idem + **esporos** ao ser atingida: nuvem de 1,5 tile por 1,5 s, veneno |
| Recuo | 0,8 s: **pogo** | 0,6 s |

## Serpente da Floresta (C, E)

Escondida em buracos e raízes; só os olhos aparecem.

| | Normal | Infectada |
|---|---|---|
| Vida | 3 | 4 |
| Gatilho | gato a 3 tiles **em pé**, ou barulho a 4 tiles. **Agachado na folhagem, não dá o bote** | 4 tiles; dá o bote mesmo com o gato agachado fora da folhagem |
| Ataque | chocalho + olhos (aviso **0,4 s**) → bote de 3 tiles: **1 hit** | bote + **veneno** |
| Bote esticado | 0,6 s parada no ar: **pogo** e ponto fraco | 0,4 s |
| Depois | volta ao buraco por 1,5 s (invulnerável) | 1,0 s |

---

## Arte e áudio por inimigo (lista mínima)

| Inimigo | Tamanho (canvas) | Animações | Sons |
|---|---|---|---|
| Sentinela infectado | 32 × 28 (como a Sentinela) | vagar, sentir, perseguir, golpe (espada), guarda (escudo), guarda quebrada, dano, morte | gemido, espada, escudo bloqueando, guarda quebrando |
| Morcego | 16 × 16 | pendurado, acordar, voar, investida, dano, morte | guincho, asas, investida |
| Galho Seco | 24 × 32 | disfarce, acordar, andar, levantar braço, golpe, braço preso, dano, morte (lenha / nuvem) | estalos de madeira, golpe no chão, rangido ao acordar |
| Cacto | 16 × 16 | flutuar, inchar, disparar (infectado) | zumbido baixo, "boing" do pogo, disparo |
| Planta Carnívora | 24 × 24 | ociosa, abrir, morder, recuo, dano, morte, esporos | estalo da boca, mastigar, esporos |
| Serpente | 32 × 16 | escondida (olhos), aviso, bote, esticada, recolher, dano, morte | chocalho, sibilo, bote |

Os nomes dos sons seguem o padrão do `Sfx` atual (ver `Docs/Audio/floresta-audio.md`).
