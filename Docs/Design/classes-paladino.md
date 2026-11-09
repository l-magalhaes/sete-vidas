# Troca de classe e o Paladino
**Pilar:** 1 (sete corações, sete classes) e 2 (gating por habilidade) | **Escopo:** MVP, Fase 5 | **Status:** rascunho para teste

## Objetivo de experiência
O jogador vê uma parede rachada que não consegue quebrar, ganha o Paladino num vínculo, volta e quebra. Recuperar um coração precisa **mudar a forma de jogar** (GDD §13, pergunta 3): a marreta é lenta, barulhenta e forte; as adagas continuam melhores para a furtividade.

## Regras — troca de classe
1. **R / Q** (ombros no controle) giram entre as classes **desbloqueadas**, dando a volta. **1–7** escolhem direto (1 Ladino, 2 Paladino, ... 7 Druida).
2. Tecla de classe bloqueada: **nada acontece** (sem som, sem aviso).
3. **Não troca no meio de um ataque** (preparação, golpe ou recuperação). **No ar, escalando ou pendurado pode.**
4. A troca confirma com: nome da classe na cor dela sobre o gato, brilho rápido do sprite e um "plim" curto.
5. O **ícone da classe ativa** fica à esquerda dos corações.
6. **Locomoção é global:** agachar, escalar e agarrar borda funcionam em qualquer classe. Só a arma (e, no futuro, a habilidade especial) muda.
7. O gato continua o gato: o Paladino é o mesmo sprite com a marreta por cima (nas costas, erguida na preparação, no chão no golpe).
8. A classe ativa vai para o save no próximo salvamento; morrer não muda a classe.

## Regras — Paladino (marreta)
9. Um golpe só (sem combo): preparação **0,35 s** (marreta erguida), golpe, recuperação **0,45 s**.
10. Dano **4**, tipo **Heavy**. **Sem bônus de costas** (o bônus é do Ladino, GDD §5.2) [PROPOSTA].
11. Durante o golpe o gato anda a **30%** da velocidade.
12. O golpe faz **o dobro de barulho** das adagas (raio 8, intensidade 0,6): chama a Sentinela.

## Regras — parede rachada (`BreakableWall`)
13. Layer `Breakable`, sólida. **Só aceita dano Heavy:** adagas fazem "tink" e não tiram vida.
14. Vida **8** (duas marretadas). Com metade da vida, as rachaduras crescem.
15. Ao quebrar: some, deixa entulho, faz um barulho alto (raio 8) e **grava a flag `parede_quebrada_<id>` no save na hora**. Continua quebrada para sempre, mesmo depois de reabrir o jogo.
16. Visual: a mesma pedra do chão com **rachaduras de brilho dourado** (linguagem do gate do Paladino). Deve aparecer **antes** de o jogador ter a marreta.

## Regras — vínculo placeholder (Golden)
17. Perto do Golden aparece "E: falar". E abre a conversa D-GOLDEN-01 na interface de diálogo (`Docs/Design/dialogo.md`).
18. Ao fim da conversa: **+1 coração** (2º), **Paladino desbloqueado**, save gravado na hora, e a linha de sistema "Aperte 2 (ou Q/R)".
19. A conversa pausa o jogo e trava o controle até o fim (não dá para sair no meio).
20. Depois do vínculo, cada E mostra uma fala curta (D-GOLDEN-02, em rodízio).

## Balanceamento
| Alvo | Adagas | Marreta |
|---|---|---|
| Sentinela (5) de frente | 3 golpes (0,6 s de combo + 1) | 2 golpes (~1,8 s, barulhentos) |
| Sentinela (5) furtivo pelas costas | **1 golpe** | 2 golpes (sem bônus) |
| Parede rachada (8) | impossível | 2 golpes |

A marreta não substitui as adagas: na furtividade, o Ladino continua matando em 1 golpe e em silêncio. Isso responde ao risco de balanceamento do GDD ("o Ladino precisa continuar relevante").

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Onde vive |
|---|---|---|
| Marreta: BaseDamage / Windup / FinalRecovery | 4 / 0,35 s / 0,45 s | `ScriptableObjects/Combat/PaladinHammer` |
| Marreta: MoveMultiplierWhileAttacking / NoiseMultiplier | 0,3 / 2 | idem |
| Parede: vida / barulho | 8 / raio 8, 0,6 | cada `BreakableWall` |
| Cores e ícones | Ladino azul-noite, Paladino dourado | `ScriptableObjects/Classes/Rogue`, `Paladin` |

## Implementação (Unity)
- `CharacterClass` (enum na ordem do GDD), `ClassDefinition` (ScriptableObject: nome, ícone, cor, arma, camada visual).
- `ClassManager`: classes desbloqueadas (do save), classe ativa, evento `ClassChanged`, Q/R/1–7.
- `ClassAbility`: habilidade só da classe ativa; o `ClassManager` liga a da classe e desliga as outras. `MeleeWeaponAbility` equipa a arma da classe no `CombatController`.
- **Nova classe = um `ClassDefinition` + um componente `ClassAbility`.** Nada no `PlayerController` muda.
- `BreakableWall`, `CompanionBond` (Golden), `ClassIconHud`, `ClassSwitchFeedback` (som gerado por código, placeholder).

## Critérios de aceite
- Dado um jogo novo, quando aperta 2 ou Q/R, então nada acontece.
- Dado o gato com adagas, quando golpeia a parede rachada, então aparece "tink" e ela não muda.
- Dado o vínculo com o Golden concluído, então o HUD mostra 2 corações e o ícone do Ladino; apertando 2, o ícone vira o do Paladino e a marreta aparece nas costas do gato.
- Dado o Paladino ativo, quando golpeia a parede duas vezes, então ela quebra e o túnel abre; parando e dando Play de novo, ela continua quebrada.
- Dado o Paladino ativo, quando encosta na parede escalável e aperta W, então escala normalmente.
- Dado o gato no meio de um golpe, quando aperta Q/R, então não troca.

## Perguntas abertas
- [PROPOSTA] Marreta sem bônus de ataque pelas costas.
- [PROPOSTA] Quebrar a parede salva na hora (o atalho nunca se perde), sem esperar o santuário.
- [PROPOSTA] Trocar de classe é instantâneo (sem tempo de recarga).
