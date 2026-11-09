# Controlador do gato (andar, pular, agachar, câmera)
**Pilar:** 1 e 2 (base de toda classe e de toda exploração) | **Escopo:** MVP, Fase 1 | **Status:** aprovado (playtest 09/11/2026)

## Objetivo de experiência
Controlar o gato tem de ser gostoso antes de existir qualquer classe (GDD §13, pergunta 1). Ele é ágil: começa e para rápido, o pulo responde na hora e o jogador controla a altura pelo tempo do botão.

## Regras
1. **Andar.** A velocidade horizontal acelera até `MoveSpeed` com `Acceleration` e freia com `Deceleration`. No ar, o controle é o mesmo do chão (sem perda de controle aéreo no MVP).
2. **Direção.** O gato vira para o último lado em que houve entrada horizontal. Sem entrada, mantém o lado.
3. **Pulo.** Só pula se estiver no chão **ou** dentro do *coyote time* depois de sair de uma borda. A altura máxima, com o botão segurado, é `JumpHeight` tiles.
4. **Jump buffer.** Apertar pulo até `JumpBufferTime` antes de tocar o chão faz o gato pular ao aterrissar.
5. **Pulo variável.** Soltar o botão durante a subida multiplica a velocidade vertical por `JumpCutMultiplier`.
6. **Queda.** Descendo, a gravidade é multiplicada por `FallGravityMultiplier`. A velocidade de queda nunca passa de `MaxFallSpeed`.
7. **Um pulo por contato com o chão.** Depois de pular, o coyote time zera: não existe pulo duplo escondido.
8. **Agachar.** Só começa no chão. O colisor cai de `StandingHeight` para `CrouchHeight` (menos de 1 tile), e a velocidade é multiplicada por `CrouchSpeedMultiplier`. A flag `IsCrouching` fica disponível para a IA (Fase 3).
9. **Levantar.** Ao soltar o agachar, o gato só levanta se houver espaço acima. Sob teto baixo, continua agachado até sair dele. O colisor nunca cresce dentro de uma parede.
10. **Pular agachado.** Só é possível se houver espaço para ficar de pé. Numa passagem de 1 tile, o pulo é ignorado.
11. **Cair agachado** (sair de uma borda abaixado): continua agachado no ar e levanta assim que houver espaço.
12. **Agarrar borda [APROVADA].** Caindo (velocidade vertical ≤ 0) e segurando a direção de uma parede, o gato se pendura se a quina do topo estiver na altura das patas (`LedgeHandHeight` ± `LedgeGrabWindow`) e houver espaço para ficar de pé em cima. Não agarra agachado nem segurando S.
13. **Pendurado.** Fica parado, sem gravidade, virado para a parede. **Espaço** sobe para cima da borda em `LedgeClimbDuration`. **S** solta e cai; durante `LedgeRegrabCooldown` não agarra de novo (para não grudar na mesma quina).
14. **Colisor.** O colisor é uma caixa de fundo reto. Parado na ponta de uma borda, o gato não escorrega (a cápsula anterior deslizava pela quina).
15. **Câmera.** Segue o gato com *dead zone* e uma pequena antecipação horizontal, sempre com Pixel Perfect (480×270, PPU 16).

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Unidade | Faixa de teste | Onde vive |
|---|---|---|---|---|
| MoveSpeed | 7 | tiles/s | 5–9 | `PlayerMovementSettings` |
| Acceleration | 70 | tiles/s² | 40–120 | idem |
| Deceleration | 90 | tiles/s² | 50–150 | idem |
| CrouchSpeedMultiplier | 0,5 | × | 0,3–0,7 | idem |
| JumpHeight | 4 | tiles | 3–5 | idem |
| TimeToApex | 0,38 | s | 0,3–0,5 | idem |
| FallGravityMultiplier | 1,8 | × | 1,2–2,5 | idem |
| JumpCutMultiplier | 0,5 | × | 0,3–0,7 | idem |
| MaxFallSpeed | 20 | tiles/s | 15–25 | idem |
| CoyoteTime | 0,1 | s | 0,06–0,15 | idem |
| JumpBufferTime | 0,1 | s | 0,06–0,15 | idem |
| LedgeGrabEnabled | ligado | — | ligado/desligado | idem |
| LedgeHandHeight | 1,25 (20 px) | tiles | 1,1–1,3 | idem |
| LedgeGrabWindow | 0,4 | tiles | 0,2–0,6 | idem |
| LedgeClimbDuration | 0,2 | s | 0,1–0,4 | idem |
| LedgeRegrabCooldown | 0,3 | s | 0,2–0,5 | idem |
| ColliderWidth | 0,75 (12 px) | tiles | — | idem |
| StandingHeight | 1,375 (22 px) | tiles | — | idem |
| CrouchHeight | 0,875 (14 px) | tiles | < 1 | idem |

O pulo é definido por **altura e tempo até o topo**, e o código calcula a velocidade inicial e a gravidade. Assim, a altura em tiles que o level design usa é o próprio valor do Inspector. A conta já compensa o passo da física (50 Hz), então a altura medida na Playground deve bater com `JumpHeight` com erro de até ±0,05 tile.

Com os valores iniciais: gravidade = 2 × 4 / 0,38² ≈ 55,4 tiles/s², velocidade de saída ≈ 21,6 tiles/s, e a distância horizontal no pulo completo correndo é de cerca de 7 × (0,38 + 0,28) ≈ 4,6 tiles.

## Interação com outras classes e sistemas
- **Furtividade (Fase 3):** a IA lê `IsCrouching`. Agachar é locomoção global: funciona em qualquer classe.
- **Ataque pelas costas (Fase 2):** usa `FacingDirection`.
- **Troca de classe (Fase 5):** nenhum valor deste controlador depende da classe ativa.
- **Vida (Fase 4):** o knockback vai entrar como um impulso externo; o controlador não deve sobrescrevê-lo durante o recuo.

## Uso na exploração
- **Altura do pulo:** define a altura máxima de plataformas sem agarrar (até `JumpHeight − 0,5`).
- **Agarrar borda aumenta o alcance:** no topo do pulo, as patas ficam a 4 + 1,25 = 5,25 tiles do chão, e a janela de 0,4 tile permite pegar quinas até ~5,6 tiles. **Alcance prático: bordas de até 5 tiles.** Gates que devem exigir pulo duplo (Monge) precisam ter **6 tiles ou mais**; com 5, o Ladino já passa agarrando.
- **Agachar:** abre passagens de 1 tile, o primeiro gate global do Ladino.

## Implementação (Unity)
| Script | Responsabilidade |
|---|---|
| `PlayerMovementSettings` (ScriptableObject) | Todos os valores acima, lidos a cada frame (ajuste com o jogo rodando) |
| `JumpMath`, `JumpTimers` (C# puro) | Conta do pulo e timers de coyote/buffer, testados em EditMode |
| `PlayerInputReader` | Lê `GameControls` (mapa Player): mover, pular, agachar |
| `GroundCheck` | Chão e espaço acima com `Physics2D.OverlapBox` |
| `PlayerController` | Física com `Rigidbody2D` (gravidade própria, `gravityScale = 0`) |
| `PlayerVisual` | Sprite placeholder por estado (parado, correndo, subindo, caindo, agachado) e virar para o lado do movimento |
| Câmera | `CinemachineCamera` + `CinemachinePositionComposer` (dead zone e antecipação) + `CinemachinePixelPerfect` |

O prefab, o asset de valores e a câmera são criados por **Tools > Sete Vidas > 4. Criar jogador e câmera (Fase 1)**.

## Critérios de aceite
- **Dado** o gato parado no chão, **quando** segurar D por 0,2 s, **então** ele está a `MoveSpeed` e virado para a direita.
- **Dado** o gato a `MoveSpeed`, **quando** soltar a direção, **então** ele para em menos de 0,1 s.
- **Dado** o gato no chão, **quando** segurar Espaço, **então** sobe `JumpHeight` tiles (±0,1) e alcança o topo do degrau de 4 tiles da Playground, mas não o de 5.
- **Dado** o gato no chão, **quando** tocar Espaço rapidamente, **então** o pulo fica visivelmente mais baixo (cerca de metade da altura).
- **Dado** o gato saindo de uma borda, **quando** apertar Espaço até 0,1 s depois, **então** ele pula.
- **Dado** o gato caindo, **quando** apertar Espaço até 0,1 s antes de aterrissar, **então** ele pula ao tocar o chão.
- **Dado** o gato no ar depois de um pulo, **quando** apertar Espaço de novo, **então** nada acontece.
- **Dado** o gato agachado, **quando** andar, **então** a velocidade é metade da normal e ele passa pela passagem baixa da Playground (x 68–75).
- **Dado** o gato agachado sob a passagem baixa, **quando** soltar S, **então** continua agachado até sair dela, sem travar no teto.
- **Dado** o gato parado na ponta de uma borda, **quando** não houver entrada, **então** ele não escorrega.
- **Dado** o gato caindo ao lado do degrau de 5 tiles, **quando** segurar a direção do degrau, **então** se pendura na quina.
- **Dado** o gato pendurado, **quando** apertar Espaço, **então** sobe para cima da borda em ~0,2 s e fica de pé.
- **Dado** o gato pendurado, **quando** apertar S, **então** cai e não agarra a mesma quina de novo na queda.
- **Dado** o gato caindo ao lado de uma parede sem quina livre (ex.: parede que continua para cima), **então** não se pendura.
- **Dado** o gato andando, **quando** a câmera o segue, **então** a imagem não treme e os tiles continuam nítidos em 1920×1080.

## Perguntas abertas
- [APROVADA] Agarrar borda desde o início, como locomoção global do gato. O GDD só cita "se pendurar" nos pontos de ancoragem do Caçador; agarrar quinas comuns é uma mecânica nova. Se preferir, ela pode virar um desbloqueio (desligando `LedgeGrabEnabled`).
- [APROVADA] Agarrar só segurando a direção da parede. Alternativa: agarrar automaticamente ao sair andando de uma borda agachado (descer para a borda de forma furtiva).
- [APROVADA] `JumpHeight = 4` tiles. Se o pulo ficar alto demais para um gato de 1,4 tile, testar 3,5.
- [APROVADA] Controle aéreo igual ao do chão. Se ficar "escorregadio", adicionar um multiplicador de aceleração no ar.
- O dash (Shift) fica sem efeito até ser desbloqueado. Continua aberta a pergunta do GDD sobre antecipar o dash para antes do Caçador.
