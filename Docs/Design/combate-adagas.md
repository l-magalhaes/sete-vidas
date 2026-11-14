# Combate com adagas e ataque pelas costas
**Pilar:** 3 (combate com múltiplos estilos) | **Escopo:** MVP, Fase 2 | **Status:** rascunho para teste

## Objetivo de experiência
O Ladino é rápido e fraco de frente, mas letal pelas costas. O jogador deve **sentir** a diferença: o golpe pelas costas trava a tela um pouco mais e mostra um número maior. Isso prepara a escolha da Fase 3: se esconder e atacar por trás, ou lutar de frente.

## Regras
1. **Botão de ataque** (mouse esquerdo / botão oeste do controle) inicia um golpe. Cada golpe tem 3 fases: **preparação** (`Windup`) → **ativo** (`Active`, a hitbox acerta) → **recuperação** (`Recovery`).
2. **Combo de 2 golpes:** apertar de novo durante o 1º golpe enfileira o 2º, que começa ao fim da recuperação do 1º. Depois do 2º golpe há uma recuperação maior (`FinalRecovery`). Apertar depois disso recomeça no 1º golpe.
3. **Direção:** o golpe sai para o lado em que o gato olha **no início** do golpe. Virar durante o golpe não muda a hitbox.
4. **Movimento:** andar e pular continuam livres durante o ataque (Ladino ágil). Agachado, o golpe sai na altura do corpo agachado.
5. **Não ataca** pendurado, subindo uma borda ou escalando.
6. **Cada golpe acerta cada alvo uma vez.** A hitbox fica ativa por vários frames, mas o mesmo alvo não toma dano duas vezes no mesmo golpe.
7. **Classificação do golpe** (classe C# pura `DamageCalculator`):
   - **Comum (1,0×):** o gato está do lado para onde o alvo olha (ou exatamente alinhado).
   - **Pelas costas (1,5×):** o gato está do lado oposto ao que o alvo olha.
   - **Furtivo pelas costas (2,5×):** pelas costas **e** o alvo ainda não está em combate.
8. **Dano:** `Dano base × multiplicador`, arredondado, nunca menor que 1. Com base 2: comum **2**, costas **3**, furtivo **5**.
9. **Impacto:** *hitstop* (o jogo congela por um instante) mais longo quanto mais forte o golpe; o alvo recua um pouco e pisca.
10. **Tipo de dano:** adagas causam dano `Normal`. A parede rachada (Fase 5) só aceita `Heavy` (marreta).
11. **Ataque para cima (melhorias 14/11/2026, todas as armas):** **{subir} + ataque**, no chão ou no ar. Um golpe só (sem combo, sem carga), com a caixa em cima da cabeça: adagas 1,4 × 1,3 tiles, marreta 1,9 × 1,6 (`MeleeAttackSettings.UpAttack` / `UpHitbox`). O efeito é a meia-lua do golpe para baixo, virada. Bom contra morcegos, cactos e inimigos em plataformas acima.
12. **Dano pelos corações (melhorias 14/11/2026):** cada coração recuperado **depois da maldição** multiplica o dano de todos os golpes por **1,3** (2 corações = 1,3×; 3 = 1,69×; 4 = 2,2×...). Arredonda (0,5 para cima), nunca menos que 1. Antes da maldição, com os 7 corações do Ladino, não há bônus (`HeartDamageRules`). Ex.: adaga 2 → **3** com 2 corações. **[PROPOSTA: revisar no playtest; com 7 corações dá 4,8×]**

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Unidade | Faixa de teste | Onde vive |
|---|---|---|---|---|
| BaseDamage | 2 | dano | 1–3 | `MeleeAttackSettings` (asset `RogueDaggers`) |
| ComboSteps | 2 | golpes | 1–3 | idem |
| Windup | 0,04 | s | 0–0,1 | idem |
| Active | 0,08 | s | 0,05–0,12 | idem |
| Recovery | 0,12 | s | 0,08–0,2 | idem |
| FinalRecovery | 0,22 | s | 0,15–0,35 | idem |
| HitboxSize | 1,1 × 0,9 | tiles | — | idem |
| Hitstop comum / costas / furtivo | 0,05 / 0,07 / 0,12 | s | 0,03–0,15 | idem |
| Multiplicador comum / costas / furtivo | 1,0 / 1,5 / 2,5 | × | GDD §5.2 (provisório) | idem |

Combo completo: 2 × (0,04 + 0,08) + 0,12 + 0,22 ≈ **0,58 s**.

## Balanceamento (boneco de treino com 10 de vida)
| Abordagem | Golpes para derrubar | Conta |
|---|---|---|
| Só de frente | 5 | 2 × 5 = 10 |
| Pelas costas, já em combate | 4 | 3 × 4 = 12 |
| Furtivo + costas | 3 | 5 + 3 + 3 = 11 |

O 1º golpe pelas costas vale quase o dobro de dois golpes de frente. É isso que deve tornar a furtividade atraente na Fase 3.

## Interação com outras classes e sistemas
- **Furtividade (Fase 3):** "em combate" vira o estado da IA (`Em combate`). No boneco, ele fica em combate por 3 s depois de cada golpe.
- **Classes (Fase 5):** `MeleeAttackSettings` serve também para a marreta (tipo `Heavy`, preparação longa). O bônus de costas vale para o Ladino (GDD §5.2); nas outras classes, decidir na Fase 5.
- **Vida (Fase 4):** o mesmo `DamageInfo` será usado quando inimigos atacarem o gato.

## Implementação (Unity)
- `Scripts/Combat`: `IDamageable`, `IFacingTarget`, `DamageInfo`, `DamageCalculator` (C# puro), `AttackSequence` (C# puro), `MeleeAttackSettings`, `CombatController`, `Hitstop`, `TrainingDummy`, `DamagePopup`.
- Boneco na layer `Enemy` com colisor *trigger*: o gato passa através dele para chegar às costas.

## Critérios de aceite
- Dado o boneco olhando para a direita, quando o gato o acerta vindo da esquerda (costas) sem tê-lo acertado antes, então aparece **5 FURTIVO** e a tela trava visivelmente mais que num golpe comum.
- Dado o boneco em combate (acertado há menos de 3 s), quando o gato acerta pelas costas, então aparece **3 COSTAS**.
- Dado o gato de frente para o boneco, quando acerta, então aparece **2**.
- Dado o gato apertando ataque duas vezes rápido, então saem dois golpes seguidos; apertando três vezes, o terceiro não sai até o combo terminar.
- Dado o boneco com 0 de vida, então ele some e volta com a vida cheia depois de 2 s.

## Perguntas abertas
- [PROPOSTA] Movimento livre durante o ataque (sem travar o gato no chão).
- [PROPOSTA] Dano base 2 e boneco com 10 de vida (contas acima).
- [PROPOSTA] Ataque no ar permitido, igual ao do chão (golpes aéreos ficam para o Monge).
