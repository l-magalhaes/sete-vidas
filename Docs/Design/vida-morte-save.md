# Vida, morte, santuário e save
**Pilar:** 1 (sete corações, sete classes) | **Escopo:** MVP, Fase 4 | **Status:** rascunho para teste

## Objetivo de experiência
Errar custa tempo, nunca progresso. O jogador sente o peso de cada hit (com 1 coração, o 2º golpe mata), mas sabe que corações e classes recuperados são para sempre. Descansar no santuário é um momento de alívio: vida cheia e jogo salvo.

## Regras — vida
1. **Vida máxima em hits = corações recuperados × 2** (GDD §14; o §4.2 foi corrigido para dizer o mesmo). **Novo jogo: 7 corações = 14 hits** (prólogo: as 7 vidas do Ladino). O tomo amaldiçoado apaga 6 (sobra 1 = 2 hits); cada vínculo devolve um (ver `chefe-golden.md`).
2. Cada golpe inimigo comum tira **1 hit**.
3. Depois de tomar dano: recuo curto para longe do atacante (solta borda e parede) e **invencibilidade de 1 s** com o sprite piscando. Dois golpes no mesmo instante contam 1 hit.
4. **HUD:** os 7 corações no canto superior esquerdo. Recuperados: **cheio** (2 hits), **meio** (1) ou **vazio** (0). Não recuperados: **escurecidos com rachadura**.

## Regras — meio coração (drop)
- Ao morrer, cada inimigo tem uma **chance baixa** de soltar **meio coração** (Sentinela: `HeartDropChance` = 15%) [PROPOSTA].
- O meio coração salta, cai até o chão e flutua de leve. **Encostar cura 1 hit**, nunca acima de corações recuperados × 2.
- **Com a vida cheia, ele continua no chão** (dá para voltar e pegar depois) [PROPOSTA].
- Some depois de 12 s no chão (pisca nos últimos 3 s) e quando o mundo reinicia (descanso ou morte), para não acumular nem virar "fazenda" de vida.

## Regras — morte
5. Em 0 hits: o gato tomba (0,7 s), a tela escurece, ele volta ao **último santuário onde descansou** com vida cheia e a tela clareia. Sem santuário ainda: volta ao início da cena.
6. **Morrer nunca remove corações nem classes.** Nada do save muda ao morrer.
7. **Inimigos reaparecem** ao morrer e ao descansar: voltam ao ponto inicial, vida cheia, patrulhando *(decisão de 09/11/2026)*.
8. Morto, o gato não recebe dano nem é visto pelos inimigos.

## Regras — santuário
9. Perto do santuário aparece **"E: descansar"**. Apertar **Interagir (E / botão leste)**: vida cheia, **salva o jogo**, vira o checkpoint ativo e os inimigos reaparecem.
10. **Encostar não salva:** só o E. (Evita salvar sem querer num momento ruim.)
11. O santuário ativo fica **aceso** (velas e olhos dourados); os outros, apagados.

## Regras — save
12. Arquivo JSON em `Application.persistentDataPath/save_slot1.json`. Campos: `version`, `heartsRecovered`, `unlockedClasses`, `activeClass`, `lastSanctuaryId`, `scene`, `flags`.
13. **Gravação segura:** escreve num `.tmp` e só depois troca pelo arquivo real. Fechar o jogo no meio do salvamento mantém o save anterior.
14. **Save ilegível:** guarda uma cópia `.corrupt` e começa um jogo novo, sem travar.
15. **Dados impossíveis são corrigidos ao carregar:** corações entre 1 e 7; Ladino sempre desbloqueado; classes inexistentes removidas; classe ativa não desbloqueada volta para o Ladino.
16. **Continuar:** ao abrir o jogo, se o último santuário é da cena atual, o gato começa nele.
17. Vida atual **não** vai para o save: carregar sempre começa com vida cheia.

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Onde vive |
|---|---|---|
| InvulnerabilityTime | 1 s | `PlayerMovementSettings` (Dano e morte) |
| KnockbackSpeed / KnockbackHeight / KnockbackControlLock | 6 tiles/s / 1 tile / 0,25 s | idem |
| DeathAnimationTime | 0,7 s | idem |
| DeathFadeTime | 0,35 s (cada lado) | idem |
| HeartDropChance | 0,15 (15%) | `SentinelSettings` (por tipo de inimigo) |
| Tempo do meio coração no chão | 12 s (pisca nos últimos 3) | prefab `HalfHeartPickup` |

## Implementação (Unity)
- **C# puro (EditMode):** `HealthRules` (hits, dano, estados do HUD), `SaveData`, `SaveStore` (ler/gravar/corrigir).
- `PlayerHealth` (gato): vida, invencibilidade, recuo, morte e volta. Substitui o `PlayerHurtbox` provisório da Fase 3.
- `SaveManager`: criado sozinho no primeiro uso; guarda o save atual em memória.
- `Sanctuary` (prefab `Prefabs/Interactables/Sanctuary`, layer `Interactable`, colisor trigger).
- `HeartsHud` + `ScreenFader` (prefab `Prefabs/UI/HUD`, Canvas 480×270 escalado ×4).
- `GameEvents.WorldReset`: inimigos (`EnemyAI`) voltam ao início.
- `CharacterClass` (enum na ordem do GDD) já existe para o save; o `ClassManager` vem na Fase 5.
- Depuração: no Inspector do `PlayerHealth` (menu ⋮) há "+1 coração" e "tomar 1 hit". Menu **Tools > Sete Vidas > Save** apaga o save ou abre a pasta dele.

## Critérios de aceite
- Dado um jogo novo, então o HUD mostra 7 corações cheios. Depois da maldição do tomo, 1 coração e 6 escurecidos.
- Dado o gato com 1 coração, quando leva 1 golpe, então o coração fica pela metade e ele pisca por 1 s; quando leva o 2º (depois de 1 s), então morre.
- Dado que o gato descansou no santuário de x 119,5, quando morre em qualquer lugar, então volta nele com vida cheia e a Sentinela volta a patrulhar.
- Dado que o gato descansou e o jogo foi fechado, quando o Play é apertado de novo, então ele começa no mesmo santuário.
- Dado um save corrompido, quando o jogo abre, então começa um jogo novo sem erro.

## Perguntas abertas
- [PROPOSTA] Salvar só com E (não automático ao encostar).
- [PROPOSTA] Sem perda ao morrer (nenhum recurso cai no chão): o GDD não prevê moeda no MVP.
- [PROPOSTA] Um único slot de save no MVP.
