# Sete Vidas

Metroidvania 2D em pixel art — Unity 6 (6000.6.8f1), URP 2D, C#.
GDD: [`Docs/GDD_Sete_Vidas_v0.1.md`](Docs/GDD_Sete_Vidas_v0.1.md)

## Fase 0 — como concluir a configuração

1. Abra o projeto no Unity e espere a compilação terminar (sem erros vermelhos no Console).
2. Menu **Tools > Sete Vidas > Executar Fase 0 completa**. Isso:
   - cria as pastas de `Assets/_Game/`;
   - cria as layers de física 8–14 (`Player`, `Enemy`, `Ground`, `Climbable`, `Breakable`, `Hazard`, `Interactable`) e ajusta a matriz de colisão;
   - força serialização em texto e confirma o novo Input System;
   - gera tiles placeholder de 16×16 e as cenas `Scenes/Dev/Playground` e `Scenes/World/Fortress_MVP` (com Pixel Perfect Camera 480×270, PPU 16) e as coloca no Build Profile;
   - instala o Cinemachine (última etapa; o Unity recompila).
3. **Window > General > Test Runner**: rode os testes **EditMode** e **PlayMode**. Todos devem passar.
4. Aperte Play na `Playground` com a Game view em 1920×1080: os tiles devem estar nítidos e sem tremer.
5. Faça o primeiro commit (abaixo).

Cada item do menu pode ser executado de novo sem problema. Atenção: o item 2 **recria a Playground** (é uma cena de testes); a `Fortress_MVP` nunca é sobrescrita.

## Fase 1 — controlador do gato

1. Menu **Tools > Sete Vidas > 4. Criar jogador e câmera (Fase 1)**. Isso cria os sprites placeholder do gato, o asset `ScriptableObjects/Player/PlayerMovementSettings`, o prefab `Prefabs/Player/Player` e a câmera Cinemachine na `Playground`.
2. Aperte Play na `Playground`: A/D ou setas para andar, Espaço para pular, S para agachar (o controle também funciona).
3. Com o jogo rodando, selecione `PlayerMovementSettings` e ajuste os valores no Inspector. **Atenção:** mudanças no asset durante o Play **ficam salvas** (é um ScriptableObject).
4. Meça o pulo nos degraus e buracos e anote em `Docs/Level/metricas.md`.
5. Spec e critérios de aceite: `Docs/Design/controlador-gato.md`.

Rodar "2. Criar cenas" de novo recria a `Playground` e recoloca o jogador e a câmera automaticamente.

## Fase 2 — escalada, adagas e ataque pelas costas

1. Menu **Tools > Sete Vidas > 5. Fase 2: escalada, adagas e boneco de treino**. Isso atualiza o prefab do gato (escalada + adagas), cria `ScriptableObjects/Player/WallClimbSettings`, `ScriptableObjects/Combat/RogueDaggers`, o prefab `Prefabs/Enemies/TrainingDummy` e recria a `Playground` com a chaminé (x 92–104) e dois bonecos (x 110 e 115).
2. Controles novos: **W/S** sobem e descem escalando, **Espaço** salta da parede, **clique esquerdo** ataca (botão oeste no controle).
3. Specs: `Docs/Design/escalada.md` e `Docs/Design/combate-adagas.md`. Plano de teste: `Docs/QA/fase2-plano-de-teste.md`.
4. Habilidades de locomoção novas (pulo duplo, dash) entram como componentes `PlayerAbility`, sem editar o `PlayerController`.

## Fase 3 — Sentinela e furtividade

1. Menu **Tools > Sete Vidas > 6. Fase 3: Sentinela e furtividade**. Isso faz o gato atravessar inimigos (matriz de colisão), adiciona ao gato a visibilidade/ruído (`StealthController`) e o dano no gato (hoje `PlayerHealth`, da Fase 4), cria `ScriptableObjects/Player/StealthSettings`, `ScriptableObjects/Enemies/SentinelSettings`, o prefab `Prefabs/Enemies/Sentinel` e recria a `Playground` com o trecho H (x 122–151).
2. Ligue **Gizmos** na Game view para ver o cone de visão, a última posição conhecida e os ruídos. A visibilidade do gato aparece no canto superior esquerdo.
3. Spec: `Docs/Design/furtividade-sentinela.md`. Plano de teste: `Docs/QA/fase3-plano-de-teste.md`.
4. O dano no gato é provisório (conta hits, empurra e pisca, sem morte) até a Fase 4.

## Fase 4 — vida, morte, santuário e save

1. Menu **Tools > Sete Vidas > 7. Fase 4: vida, santuário e save**. Isso cria o HUD de corações (`Prefabs/UI/HUD`), o santuário (`Prefabs/Interactables/Sanctuary`), troca o dano provisório do gato pelo `PlayerHealth` e recria a `Playground` com santuários em x 3 e x 119,5.
2. **E** perto de um santuário: descansa, cura e salva. Morrer volta ao último santuário; inimigos reaparecem ao descansar e ao morrer.
3. O save fica em `Application.persistentDataPath/save_slot1.json`. **Tools > Sete Vidas > Save** apaga o save ou abre a pasta dele. Os testes usam um save temporário.
4. Spec: `Docs/Design/vida-morte-save.md`. Plano de teste: `Docs/QA/fase4-plano-de-teste.md`.

## Fase 5 — troca de classe e o Paladino

1. Menu **Tools > Sete Vidas > 8. Fase 5: classes, Paladino e parede rachada**. Cria `ScriptableObjects/Classes/Rogue` e `Paladin`, a marreta `ScriptableObjects/Combat/PaladinHammer`, os prefabs `BreakableWall` e `Golden`, coloca o `ClassManager` no gato, o ícone da classe no HUD e recria a `Playground` com o trecho I (x 154–177).
2. Controles: **R/Q** giram entre as classes desbloqueadas, **1–7** escolhem direto, **E** fala com o Golden.
3. Specs: `Docs/Design/classes-paladino.md`; falas em `Docs/Narrativa/dialogos-mvp.md`. Plano de teste: `Docs/QA/fase5-plano-de-teste.md`.
4. Nova classe = um `ClassDefinition` + um componente `ClassAbility` (ver a spec).

## Diálogos

1. Menu **Tools > Sete Vidas > 9. Diálogos (interface com retratos)**. Cria os falantes e as conversas em `ScriptableObjects/Dialogue`, a interface de diálogo no HUD e atualiza o Golden.
2. E perto de um NPC abre a conversa: retrato de quem fala (gato à esquerda, NPCs à direita), texto letra por letra; E/Espaço/clique avançam. O jogo pausa enquanto a conversa está aberta.
3. Para mudar um texto, edite o asset da conversa no Inspector. Spec: `Docs/Design/dialogo.md`. Plano de teste: `Docs/QA/dialogo-plano-de-teste.md`.

## Chefão: Golden, arena e tomo

1. Menu **Tools > Sete Vidas > 10. Chefão: Golden, arena e tomo**. Cria o Golden chefão, os relâmpagos, as grades, o tomo, a barra de vida no HUD e recria a `Playground` com a arena (x 180–210) e a sala do tomo (x 213–218).
2. **Novo jogo começa com 7 corações.** Pegar o tomo apaga 6; o juramento com o Golden devolve o 2º e dá o Paladino.
3. Spec: `Docs/Design/chefe-golden.md`. Plano de teste: `Docs/QA/chefe-plano-de-teste.md`. Golpes do chefe para teste: menu ⋮ do `GoldenBoss` no Inspector.

## Armadilhas

1. Menu **Tools > Sete Vidas > 11. Armadilhas (circuito à esquerda do início)**. Cria os prefabs em `Prefabs/Traps`, dá ao gato o "último chão seguro" e recria a `Playground` com o circuito K: do início, ande para a **esquerda**.
2. Espinhos tiram 1 hit e devolvem o gato ao último chão seguro. Fio de alarme e placa de pressão não disparam com o gato agachado. Disparar faz barulho (Sentinelas ouvem).
3. Spec: `Docs/Design/armadilhas.md`. Plano de teste: `Docs/QA/armadilhas-plano-de-teste.md`.

## Git e GitHub

O `.gitignore` e o `.gitattributes` (com Git LFS para arte e áudio) já estão na raiz. No terminal, dentro desta pasta:

```bash
git init
git lfs install
git add .
git commit -m "Fase 0: configuração inicial do projeto"
git branch -M main
git remote add origin https://github.com/<seu-usuario>/sete-vidas.git
git push -u origin main
```

Crie antes o repositório vazio no GitHub (sem README nem .gitignore). Se preferir, o GitHub Desktop faz o mesmo: *Add existing repository* → esta pasta → *Publish*.

## Estrutura

| Caminho | Conteúdo |
|---|---|
| `Assets/_Game/` | Todo o conteúdo do jogo (GDD §9.2) |
| `Assets/_Game/Scripts/` | Código de runtime (assembly `SeteVidas.Runtime`) |
| `Assets/_Game/Editor/` | Ferramentas de editor (setup da Fase 0, importação de pixel art) |
| `Assets/_Game/Settings/Input/GameControls.inputactions` | Controles de teclado e controle (GDD §4.3) |
| `Assets/_Game/Tests/` | Testes EditMode e PlayMode |
| `Docs/` | GDD, métricas, decisões de arte, planos de QA |

## Regras do projeto

- Valores de gameplay ficam em `ScriptableObject`, nunca fixos no código.
- Todo PNG dentro de `Assets/_Game/Art/` é importado automaticamente como pixel art (Point, sem compressão, PPU 16).
- Um commit ao fim de cada passo que funciona.
