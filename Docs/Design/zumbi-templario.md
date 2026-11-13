# Templário Zumbi e envenenamento

> Pedido do usuário (12/11/2026). Status: **[PROPOSTA]** até o playtest.

## Objetivo
Depois que o gato pega o tomo, a praga toma a Fortaleza: os templários viram mortos-vivos. A volta (e a saída) fica
diferente da ida. Os zumbis não pensam nem procuram: andam para a frente e, ao sentir o gato, correm atrás dele.
O perigo é **encostar**: o toque envenena.

## Quando aparecem
- Marca `tomo_pego` no save → o `ZombieOutbreak` desliga **todos os templários e atiradores** da Fortaleza e põe
  um zumbi **no posto de cada um** (mesmo lugar, mesma faixa de patrulha). Vale para os inimigos movidos à mão.
- **Postos extras** (só depois do tomo; `FortressLayout.ZombiePosts`):
  | Nome | Posto | Faixa |
  |---|---|---|
  | Zumbi do cofre do guardião | x 205, andar 101 | 202–208 |
  | Zumbi da beira do poço | x 213, andar 101 | 209–216,5 |
  | Zumbi da estrada | x 236, chão (y 0) | 228–243 |
- Carregar um save que já tem o tomo: começa com zumbis. Descansar ou morrer: os zumbis voltam aos postos (como os templários).

## Comportamento (`ZombieAI`, regras em `ZombieRules`)
| Estado | O que faz |
|---|---|
| Vagando | Anda sempre para a frente a 0,8 tile/s (arrastando os pés); vira na parede, na beira e no fim da faixa do posto, com uma pausa de 0,5 s |
| Sentiu | Tranco da cabeça (0,5 s) e o gemido `zumbi_sente` (o "!" dele, sem ícone) |
| Perseguindo | Anda rápido a **3,2 tiles/s** (o templário corre a 4,5; era 5,5, difícil demais no playtest de 12/11/2026) em direção ao gato; para na beira e na parede (não se joga do andar); desiste depois de 3 s sem sentir o gato |

**Sentidos:** vê à frente até 6 tiles (a parede tapa); sente de qualquer lado a 2 tiles (o faro); escondido na sombra, só
o faro e pela metade (1 tile); ouve os barulhos do gato (NoiseSystem) dentro do raio deles; só no mesmo andar (±2 tiles).

## Toque e envenenamento (`PoisonRules`, `PlayerHealth.Poison`)
- O corpo do zumbi **não é sólido**: o gato atravessa, e encostar envenena. Um toque a cada 0,5 s no máximo.
- **Veneno:** meio coração (1 hit) por segundo, durante 3 s (3 tiques; o primeiro 1 s depois do toque).
- **Encostar de novo reinicia os 3 s** (o relógio do próximo tique continua, senão o contato contínuo travaria o veneno).
- **Sozinho, o veneno para no último meio coração** (proposta aceita pelo usuário). Depois de um **segundo toque
  durante o veneno**, ele mata.
- Os tiques ignoram a invencibilidade do dano comum. Morrer, renascer e descansar curam o veneno.
- Depois da maldição o gato tem 2 corações (4 hits): um toque tira 1,5 coração; dois toques seguidos matam.

## Combate
- Vida 6. Adagas: 2 de frente, **4 pelas costas** (o zumbi nunca é pego de surpresa: sem golpe furtivo, mas o golpe
  pelas costas vale **2×**, `IBackDamageMultiplier`). Marreta e terremoto derrubam (`IKnockdown`).
- Morte: cai e se desfaz em fiapos roxos (`death`); morto pelas costas, tomba para a frente (`death_stealth`).

## Arte (Tools/EnemySprites, `ZombieTemplarAnimationSet`)
O corpo do templário sem arma, braços estendidos à frente, pelo morto (#7d7058), aço sem brilho com ferrugem,
sobreveste suja e rasgada, **olho verde-ácido (#8fd14f) e veias roxas (#5b2a6e)**, o par da praga.
Clipes: idle, walk (arrastando), run (inclinado), alert, hurt, death, death_stealth.

## Retorno visual e sonoro do veneno
- Gato: pisca verde a cada tique, encolhe 1 px, bolhas verdes subindo (mais rápidas quando o veneno pode matar).
- HUD: os corações piscam em verde enquanto envenenado (mais rápido quando pode matar).
- Sons: `zumbi_toque` (tapa úmido + chiado ácido), `veneno_tique` (bolha + nota que cai; mais grave quando pode matar,
  fraco e agudo quando segurou no último meio coração), `veneno_passa`. Ver `Docs/Audio/plano-sfx.md` §Zumbi.

## Parâmetros (ZombieSettings)
MaxHealth 6 · BackMultiplier 2 · WanderSpeed 0,8 · ChaseSpeed 3,2 · TurnPause 0,5 · SightRange 6 · CloseRange 2 ·
SenseHeight 2 · LoseTime 3 · AlertTime 0,5 · ContactSize 0,85×1,5 · ContactCooldown 0,5.

## Critérios de aceite
- Antes do tomo, só templários; depois, nenhum templário de pé e um zumbi por posto + 3 extras (`ZombieTests`).
- Encostar envenena; 3 tiques de 1 hit; reiniciar não atrasa o tique; sozinho não mata; com o segundo toque mata (`ZombieRulesTests`).
- O zumbi é mais lento que o templário (mas acelera ao perseguir) e vira na parede e na beira.

## Ferramentas
- `Tools > Sete Vidas > 13c. Fortaleza: zumbis depois do tomo` põe o `ZombieOutbreak` na Fortaleza aberta sem recriar a fase
  (o menu 13 também o cria).
