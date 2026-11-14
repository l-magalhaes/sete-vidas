# Cão Sentinela Atirador (inimigo de besta)

**Pilares:** 2 (furtividade) e 3 (combate) | **Escopo:** MVP (Fortaleza) | **Status:** implementado, aguarda playtest

## Papel
É o templário da Ordem da Presa Dourada que **guarda os lugares altos**: arquibancadas, prateleiras e sacadas. Fica no posto e **atira para baixo** com uma besta. Força o jogador a usar cobertura e o tempo da recarga, ou a subir e pegá-lo de costas.

## Comportamento

São o mesmo cérebro e a mesma percepção da Sentinela (`EnemyAI` + `AlertBrain` + `EnemyPerception`): patrulhando → suspeito → investigando → combate, com luz, sombra, barulho e ataque pelas costas.

**Fica no posto.** Não desce para perseguir (`HoldPosition`):
- ao investigar, só olha para o lugar do barulho e procura dali;
- em combate, gira para acompanhar o gato.

**Visão:**
- inclinada **30° para baixo**, com meia-abertura de 50°: cobre do horizontal até ~80° para baixo;
- alcance de 11 tiles, reduzido pela visibilidade do gato (sombra, agachado);
- não vê atrás de si.

**O tiro**, quando vê o gato a até 11 tiles e entre 20° acima e 70° abaixo:
1. **Mira (0,9 s).** Uma **linha pontilhada vermelha** sai da besta até onde o virote vai. Ela acompanha o gato e **trava nos últimos 0,25 s** (fica firme e mais forte). Sair do lugar marcado nessa hora faz o virote errar.
2. **Disparo.** O virote voa reto (12 tiles/s) na direção mirada, tira 1 hit e crava na parede.
3. **Recarga (1,6 s).** Baixa a besta, puxa a corda e põe outro virote. É a **janela para chegar perto**.

**Corpo:** 3 de vida (mais frágil que o lanceiro, que tem 5). 25% de chance de soltar meio coração.

## Como vencer
- **Cobertura:** paredes, a passagem baixa da arquibancada e o teto bloqueiam a linha de visão.
- **Sombra e agachar** reduzem o alcance em que ele percebe o gato, como na Sentinela.
- **Tempo:** depois do tiro, a recarga dá ~1,6 s para atravessar ou subir.
- **Subir por trás:** cada posto tem uma subida (vinha, borda ou plataforma). Por trás, o ataque pelas costas dói mais.

## Onde está (Fortaleza)

| Nome | Posto | Cobre | Como chegar nele |
|---|---|---|---|
| Atirador da arquibancada | arquibancada do pátio (topo 7), olhando para a esquerda | a entrada do pátio, o fio e as flechas | vinha atrás da arquibancada (x 55) |
| Atirador do alojamento | **ponta direita da passarela** (x 166, topo 48), olhando para a esquerda | quem chega do portão da muralha: a passarela e o chão embaixo dela | pela passarela, de caixote em caixote; ou pelo chão, agarrando a borda do lado direito (pelas costas) |
| Atirador da sacada | **plataforma 1 do salão** (x 182,5, topo 75), olhando para a **direita** | quem sobe a torre do sino e atravessa o salão (sentinelas A e B) | pelo lado do fogo (por trás); ou na recarga |

**Melhorias 14/11/2026:** os dois do meio mudaram de lugar (antes o do salão ficava de costas para quem chegava, e o do alojamento cobria pouco). Rode **Tools > Sete Vidas > 13e** na Fortaleza aberta para mover só eles. A besta também ficou mais atenta: **alcance 13** (vista e tiro, era 11), **meia-abertura 55°** (era 50°), **enche a suspeita 2,5× mais rápido** (na luz entra em combate e mira quase na hora; na sombra, agachado ou escondido continua difícil) e mira até **30° para cima** (era 20°).

## Ajustes (Inspector)
Ficam em `ScriptableObjects/Enemies/CrossbowSentinelSettings.asset` (é um `SentinelSettings`, seção "Atirador (besta)"):
- `ShootRange`, `AimTime`, `AimLockTime`, `ReloadTime`;
- `BoltSpeed`, `BoltDamage`;
- `MaxAimDown`, `MaxAimUp`, `VisionTiltDown`;
- `HoldPosition`, `Ranged`.

## Código e arte
- **Mecânica:** `EnemyAI` (modo atirador: `FightRanged`, `StepRanged`, linha de mira), `EnemyBolt` (virote), `PerceptionMath` (cone inclinado e pose de mira).
- **Prefab:** `Prefabs/Enemies/CrossbowSentinel.prefab`, montado por `EnemySetup.CreateCrossbowSentinelPrefab()` a partir da Sentinela.
- **Arte:** `Tools/EnemySprites/TemplarSpriteGen.cs` (rode `gerar.ps1`), em `Art/Enemies/TemplarCrossbow/` e `CrossbowTemplarAnimationSet.asset`. Prévia: `SeteVidasDocs/Referencias/atirador_animacoes.png`.
- **Testes:** `EditMode/CrossbowRulesTests` (cone e pose de mira) e `PlayMode/CrossbowSentinelTests` (o atirador da arquibancada mira para baixo, atira e não sai do posto).

## Critérios de aceite
- Dado o gato na luz, à frente e abaixo dele, a até 11 tiles, ele entra em combate, mostra a linha de mira e atira.
- Dado o gato atrás de cobertura (passagem baixa, parede), ele não atira.
- Dado que o gato sai do lugar depois que a mira trava, o virote erra.
- Ele nunca sai do posto.
