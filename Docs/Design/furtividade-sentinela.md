# Furtividade e Sentinela
**Pilar:** 3 (combate com estilos) e 1 (identidade do Ladino) | **Escopo:** MVP, Fase 3 | **Status:** rascunho para teste

## Objetivo de experiência
O jogador sente que **escolhe**: esperar na sombra, seguir a Sentinela agachado e matá-la pelas costas, passar por cima sem ser visto, ou lutar de frente (mais arriscado). Ser descoberto nunca é instantâneo: o "?" avisa e dá tempo de recuar (GDD §5.1, "agachar não garante invisibilidade").

## Regras — percepção
1. **Visibilidade do gato (0 a 1)** = exposição da luz × (agachado ? `CrouchVisibilityMultiplier` : 1).
   - Fora de zona de sombra, exposição = 1. Numa `LightZone`, exposição = valor da zona (sombra padrão 0,3). Se houver várias zonas, vale a mais escura.
   - Exemplos: luz em pé 1,0 · luz agachado 0,6 · sombra em pé 0,3 · sombra agachado **0,18**.
   - **Matinho (`HidingSpot`):** agachado dentro dele, a visibilidade é multiplicada também por `Concealment` (0,25). Na luz: 0,6 × 0,25 = **0,15** (a Sentinela só percebe a ~1,2 tile). **De pé no matinho não esconde nada** (1,0): a cabeça aparece por cima das folhas. O matinho é desenhado na frente do gato, então agachado ele some visualmente.
2. **Cone de visão:** alcance `VisionRange` e meio-ângulo `VisionHalfAngle` a partir dos olhos (`EyeHeight`), na direção para onde a Sentinela olha. **Não vê pelas costas.**
3. **Alcance efetivo** = `VisionRange` × visibilidade. Sombra + agachado: 8 × 0,18 ≈ **1,4 tile** (só percebe se encostar).
4. **Linha de visão:** um raio dos olhos até o peito do gato; paredes e plataformas (`Ground`, `Climbable`, `Breakable`) bloqueiam.
5. **Em combate**, a visibilidade do gato nunca fica abaixo de `CombatVisibilityFloor` (0,6): depois de descoberto, a sombra ajuda menos.
6. **Ruído:** eventos com raio e intensidade. A Sentinela ouve se estiver dentro do raio (paredes não abafam no MVP).
   | Fonte | Raio | Intensidade | Condição |
   |---|---|---|---|
   | Passos correndo | 3 tiles | 0,15 por passo (a cada 0,35 s) | No chão, em pé, velocidade ≥ 60% da máxima |
   | Aterrissagem | 5 tiles | 0,4 | Queda ≥ `LandingNoiseHeight` (4,5 tiles) |
   | Golpe de adaga | 4 tiles | 0,3 | Cada golpe que entra na fase ativa |
   | Agachado | — | 0 | Andar agachado não faz barulho |
   - Com 4,5 tiles: um pulo normal (4) é silencioso; **cair** de uma plataforma de 5 faz barulho, mas **se pendurar e soltar** (3,75) não.

## Regras — medidor de suspeita (0 a 1)
7. **Enche** enquanto a Sentinela vê o gato: `SightFillRate` × (0,5 + 1,5 × (1 − distância / alcance efetivo)). Na borda do alcance enche devagar (0,4/s); colado, rápido (1,6/s).
8. **Ruído** soma a intensidade de uma vez.
9. **Esvazia** a `DrainRate` (0,15/s) depois de `DrainDelay` (1 s) sem estímulo. Em combate não esvazia.
10. Cada estímulo atualiza a **última posição conhecida** (onde viu ou ouviu).

## Regras — estados da Sentinela (`EnemyAI`)
| Estado | Comportamento | Sai para |
|---|---|---|
| **Patrulhando** | Anda entre os pontos da rota a `PatrolSpeed`, pausa `PatrolPause` em cada ponta e vira | Suspeito (suspeita ≥ 0,25) · Em combate (≥ 1) |
| **Suspeito** "?" | Para e vira para a última posição conhecida | Em combate (≥ 1) · Investigando (após `SuspiciousTime` com suspeita ≥ 0,5 e sem ver o gato) · Patrulhando (suspeita < 0,1) |
| **Investigando** "?" | Anda até a última posição conhecida a `InvestigateSpeed`; lá, olha para os dois lados por `SearchTime` | Em combate (≥ 1) · Patrulhando (procurou e não achou: suspeita zera) |
| **Em combate** "!" | Persegue a `ChaseSpeed` (mais lenta que o gato: dá para fugir) e ataca a até `AttackRange` | Investigando (sem ver nem ouvir o gato por `LoseSightTime`) |
| **Morta** | Tomba, some e (só na Playground) volta depois de `RespawnDelay` | — |

11. **Ataque:** preparação `AttackWindup` (0,45 s, lança recuada: o jogador **vê e pode desviar**) → golpe `AttackActive` → recuperação `AttackRecovery` (0,6 s, janela de contra-ataque). Acerta 1 hit no gato.
12. **Tomar dano** coloca a Sentinela em combate na hora, virada para o atacante. Apanhar não interrompe o ataque dela.
13. **Não cai de bordas** nem atravessa paredes: perseguindo, para na beira.
14. **Ataque pelas costas:** a Sentinela é `IFacingTarget`; "em combate" = estado Em combate. Patrulhando, suspeita ou investigando, um golpe pelas costas é **furtivo**.

## Gato tomando dano
15. Ver `Docs/Design/vida-morte-save.md` (Fase 4): cada golpe tira 1 hit, com recuo e 1 s de invencibilidade; em 0 hits o gato morre e volta ao último santuário.

## Balanceamento (Sentinela com 5 de vida)
| Abordagem | Golpes | Conta |
|---|---|---|
| Furtivo pelas costas | **1** | 5 ≥ 5 |
| Pelas costas, em combate | 2 | 3 + 3 |
| De frente | 3 | 2 + 2 + 2 |

Lutando de frente, o jogador precisa de 3 golpes (um combo e meio) e a Sentinela ataca a cada ~1,2 s com aviso de 0,45 s: dá para vencer sem tomar dano, mas exige atenção. A furtividade resolve em 1 golpe, sem risco: é a recompensa da paciência.

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Faixa | Onde vive |
|---|---|---|---|
| CrouchVisibilityMultiplier | 0,6 | 0,4–0,8 | `StealthSettings` |
| Exposição da sombra | 0,3 | 0,1–0,5 | cada `LightZone` |
| Concealment do matinho (só agachado) | 0,25 | 0,1–0,5 | cada `HidingSpot` |
| LandingNoiseHeight | 4,5 tiles | 4,1–5,5 | `StealthSettings` |
| VisionRange / VisionHalfAngle | 8 tiles / 35° | 6–10 / 25–45° | `SentinelSettings` |
| SightFillRate | 0,8/s | 0,5–1,5 | idem |
| Limiares suspeito / investigar | 0,25 / 0,5 | — | idem |
| DrainDelay / DrainRate | 1 s / 0,15/s | — | idem |
| SuspiciousTime / SearchTime / LoseSightTime | 1,2 / 3 / 2 s | — | idem |
| PatrolSpeed / InvestigateSpeed / ChaseSpeed | 2 / 3 / 4,5 tiles/s | ChaseSpeed < 7 | idem |
| AttackRange / Windup / Active / Recovery | 1,3 tile / 0,45 / 0,12 / 0,6 s | — | idem |
| MaxHealth | 5 | — | idem |

## Implementação (Unity)
- **C# puro (EditMode):** `StealthMath`, `PerceptionMath`, `AlertBrain` (máquina de estados + medidor).
- **Gato:** `StealthController` (visibilidade, passos, aterrissagem, golpes → `NoiseSystem`), `PlayerHealth` (dano, desde a Fase 4).
- **Mundo:** `LightZone` (retângulo de sombra com visual escuro). `HidingSpot` (matinho: só esconde agachado).
- **Inimigo:** `EnemyAI` (estados, movimento, ataque, morte, ícones "?"/"!") + `EnemyPerception` (cone, linha de visão, audição) + `SentinelSettings`.
- **Depuração:** Gizmos do cone (alcance base e efetivo), última posição conhecida e ruídos recentes; texto com estado e suspeita sobre a Sentinela; visibilidade do gato no canto da tela.

## Critérios de aceite
- Dado o gato agachado na sombra a 2 tiles da Sentinela, então ela não o percebe (nem "?").
- Dado o gato agachado num matinho na luz, quando a Sentinela passa ao lado, então ela não o percebe; dado o gato de pé no mesmo matinho, então ela o vê normalmente.
- Dado o gato em pé na luz à frente dela a 6 tiles, então aparece "?" em menos de 0,5 s e "!" em menos de 2 s.
- Dado o gato atrás da Sentinela, mesmo na luz e em pé, então ela não o vê.
- Dado o gato correndo a 2 tiles atrás dela, então ela ouve, vira ("?") e vai investigar.
- Dada a Sentinela patrulhando, quando o gato a acerta pelas costas, então aparece **5 FURTIVO** e ela morre num golpe.
- Dado o gato fugindo, então a Sentinela perde o rastro, vai até a última posição conhecida, procura e volta à patrulha.
- Na Playground (trecho H) há pelo menos duas formas de passar: por cima (passarela) ou pelas costas (sombra), além de lutar.

## Perguntas abertas
- [PROPOSTA] Sem dano por contato: encostar na Sentinela não machuca; só o golpe dela.
- [PROPOSTA] Gato e inimigos não colidem fisicamente (o gato atravessa); necessário para chegar às costas.
- [PROPOSTA] Paredes não abafam ruído no MVP.
- [PROPOSTA] Nenhuma indicação de "você está escondido" além do escurecimento da sombra e do número de visibilidade (depuração). Avaliar um ícone de olho no Vertical Slice.
