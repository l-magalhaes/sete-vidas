# Escalada
**Pilar:** 2 (exploração com gating por habilidade) | **Escopo:** MVP, Fase 2 | **Status:** rascunho para teste

## Objetivo de experiência
O gato sobe paredes marcadas com facilidade e sem gastar recurso. O jogador lê de longe qual parede é escalável e decide a rota: subir, descer ou saltar para outra parede.

## Regras
1. **Só a layer `Climbable` é escalável.** Paredes `Ground` continuam lisas: o gato pode agarrar a quina do topo (borda), mas não sobe pela face.
2. **Agarrar a parede.**
   - **No ar:** encostar numa parede escalável segurando a direção dela. Vale subindo ou caindo.
   - **No chão:** encostar e apertar **W** (cima). Andar contra a parede sem W não agarra.
   - Agachado não agarra.
3. **Escalando:** sem gravidade, virado para a parede. **W** sobe a `ClimbUpSpeed`, **S** desce a `ClimbDownSpeed`. Sem entrada, fica parado.
4. **Topo da parede:** subindo, quando as patas passam do topo, o gato sobe para cima dela sozinho (mesma animação da borda), se houver espaço para ficar de pé. Sem espaço, para no topo.
5. **Base da parede:** descendo até tocar o chão, solta a parede e fica de pé. Se a parede acabar no ar, solta e cai.
6. **Pular da parede (Espaço):** impulso diagonal para longe da parede, com altura `WallJumpHeight` e velocidade horizontal `WallJumpSpeedX`. Durante `WallJumpInputLock`, a direção do jogador não freia o impulso. O gato pode agarrar outra parede escalável no caminho.
7. **Soltar (segurar a direção oposta):** solta e cai. Durante `ReattachCooldown` não agarra de novo.
8. **Tetos:** escalar contra um teto para o gato; nada atravessa colisão.
9. **Prioridade:** numa parede escalável, a escalada vence o "agarrar borda". Em paredes comuns, o agarrar borda da Fase 1 continua igual.
10. **Ataques:** não ataca escalando (Fase 2). Atacar exige estar no chão ou no ar livre.

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Unidade | Faixa de teste | Onde vive |
|---|---|---|---|---|
| ClimbUpSpeed | 4 | tiles/s | 3–6 | `WallClimbSettings` |
| ClimbDownSpeed | 6 | tiles/s | 4–9 | idem |
| WallJumpHeight | 2,5 | tiles | 1,5–3,5 | idem |
| WallJumpSpeedX | 7 | tiles/s | 5–9 | idem |
| WallJumpInputLock | 0,15 | s | 0,08–0,25 | idem |
| ReattachCooldown | 0,2 | s | 0,1–0,4 | idem |

O pulo da parede usa a mesma gravidade do pulo normal (`PlayerMovementSettings`), então a altura em tiles é exata como no pulo comum.

## Interação com outras classes e sistemas
- **Locomoção global:** escalar funciona em qualquer classe depois de desbloqueada (GDD §14). No MVP o Ladino já começa com ela.
- **Furtividade (Fase 3):** escalar não é agachar; a IA vê o gato normalmente. Parede escalável serve como rota alternativa para fugir do cone de visão.
- **Troca de classe (Fase 5):** trocar escalando é permitido e não solta a parede.
- **Vida (Fase 4):** levar dano escalando solta a parede (o recuo vence).

## Uso na exploração
- **Gate do Ladino:** parede alta (≥ 6 tiles, acima do alcance de pulo + borda) marcada como escalável.
- **Chaminé:** duas paredes escaláveis frente a frente; o pulo da parede leva de uma à outra.
- **Linguagem visual:** ranhuras de apoio + hera seca (ver `Docs/Arte/decisoes.md`). Nunca usar esse visual em parede que não é escalável.

## Implementação (Unity)
- `PlayerAbility` (`Scripts/Abilities`): base das habilidades de locomoção. O `PlayerController` encontra todas no mesmo objeto e passa o controle para a que pedir. **Novas habilidades (pulo duplo, dash) não exigem editar o `PlayerController`.**
- `WallClimbAbility : PlayerAbility` + `WallClimbSettings` (ScriptableObject em `ScriptableObjects/Player/`).

## Critérios de aceite
- Dado o gato no ar ao lado da parede verde, quando segura a direção dela, então ele gruda e fica parado.
- Dado o gato escalando, quando segura W até o topo, então ele sobe para cima da parede sem travar.
- Dado o gato escalando, quando aperta Espaço, então salta para longe e consegue agarrar a parede da frente na chaminé.
- Dado o gato escalando perto do chão, quando segura S, então solta ao tocar o chão.
- Dado o gato encostado numa parede cinza (`Ground`), quando segura a direção e W, então não escala.

## Perguntas abertas
- [PROPOSTA] Escalada sem estamina. Se as salas ficarem fáceis demais, considerar um limite de tempo no Vertical Slice.
- [PROPOSTA] No chão, agarrar só com W (andar contra a parede não agarra), para não grudar sem querer.
- [PROPOSTA] Pular da parede sempre para longe dela (não existe "pulo para cima" escalando).
