# Biblioteca de cenário: como usar

Base do guia de assets (Etapa 9 do plano `SeteVidasDocs/Sete Vidas — Assets e Cenários da Fortaleza.md`). Vale para a Fortaleza e para as próximas fases.

## Como funciona

```
PNG (gerado por código)  ─┐
                          ├─► manifest.json da pasta ─► EnvAssetDefinition ─► FortressDressing / fundos ─► cena
paleta (EnvPalette.cs) ───┘      (metadados, à mão)      (importador, Unity)     (dados da fase)           (menu 13)
```

- **PNG:** desenhado por código (`Tools/...`, `PixelKit`) ou pelo editor (`CitadelDecorSetup`). Use só cores da paleta (`Docs/Arte/paleta.md`).
- **`manifest.json`:** um por pasta em `Assets/_Game/Art/Environments/`. É a **fonte de verdade** dos metadados: região, função, camada, apoio, variações, animação, paralaxe.
- **`EnvAssetDefinition`:** criado em `Assets/_Game/ScriptableObjects/Environment/<id>.asset`. **Não edite esses assets**: eles são recriados a partir do manifesto (sozinhos quando o JSON muda, ou pelo menu *Tools > Sete Vidas > Cenário > Importar biblioteca de cenário*).
- **Dados da fase:** `Assets/_Game/Editor/Scenery/FortressDressing.cs` diz qual id vai em qual posição. O menu *13. Fase 6: Fortaleza* monta tudo.

## Adicionar um asset novo

1. Desenhe o PNG na pasta da região (ex.: `Art/Environments/Exterior/env_ext_veg_tree_adult_a.png`), com o nome `env_<região>_<categoria>_<nome>_<variação>.png`.
2. Acrescente a entrada no `manifest.json` da pasta (copie uma parecida). Campos mínimos: `id`, `categoria`, `funcao`, `camada`, `sprites`, `posicionamento.apoio`.
3. Rode `powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar.ps1`. Ele confere ids, campos, imagens e **cores reservadas**.
4. No Unity, o manifesto é reimportado sozinho. Coloque o asset na fase em `FortressDressing.Items()` com `Add(d, área, "id", "Nome", x, y)`.
5. Gere a Fortaleza (menu 13).

**Variações:** liste vários PNGs em `sprites`, ou use `sprites_prefixo` (ex.: `"env_ext_roc_media_"`) para pegar todos os PNGs que começam assim. A montagem escolhe um por semente fixa da posição (sempre o mesmo resultado) e nunca repete a mesma variação 3 vezes seguidas. `espelhar: true` deixa espelhar às vezes; `posicionamento.deslocamento_px` dá um deslocamento de até N px. Quando o tamanho importa para a leitura (ex.: placa que não pode sair da parede), fixe a variação com `Fixed(d, área, "id", variação, "Nome", x, y)`.

## Terreno (tilesets)

O terreno não é colocado item por item: cada célula de chão do `FortressLayout` recebe o tile que o `TerrainAutotile` escolhe (vizinhos expostos + profundidade), conforme o material de `FortressTerrain.Regions()`.

- **Materiais prontos:** `ext_solo` (grama, terra, transição, rocha) e `ext_rocha` (pedras, paredes de rocha-mãe). Células sem material ficam com o tile provisório.
- **Mudar o material de um trecho:** acrescente uma região em `Editor/Scenery/FortressTerrain.cs` (regiões listadas depois valem por cima).
- **Rampas:** em `FortressLayout.Slopes()`; com material `ext_solo` e proporção 1:1 ou 1:2 usam a arte final, senão o triângulo provisório.
- **Detalhes automáticos:** tufos e seixos na superfície, fendas na rocha funda (`TerrainAutotile.TopDetails` / `DeepDetails`), na camada `terreno_detalhe`. Para tirar de um trecho (ex.: fundo da lagoa), use `FortressTerrain.NoDetails()`.
- **Arte:** `Tools/EnvironmentSprites/TerrainSpriteGen.cs` desenha todos os nomes que `TerrainAutotile.AllTileNames` pode pedir. O teste `TerrainAutotileTests.GeneratedArt_HasEveryTileTheRuleCanAskFor` avisa se faltar algum.
- **Material novo** (ex.: piso de pedra da Cidadela na Etapa 3): acrescente o nome em `TerrainAutotile.Materials`, as faixas que ele usa e o desenho no gerador.

## Conferir sem abrir o Unity

`powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/gerar_cena.ps1 <x> <y> <nome>` monta uma tela 480×270 com a câmera em (x, y) usando a arte real (fundo, terreno, rampas, detalhes, enfeites, cachoeira, espinhos, moedas e o gato) e grava `SeteVidasDocs/Referencias/cena_<nome>.png` e a versão `_cinza` (teste de leitura). Sem luz 2D nem animação.

**Animação:** `animacao.quadros` + `animacao.fps`. A montagem põe um `SpriteLoop`.

**Fundo (categoria `plx`):** preencha `fundo.paralaxe` [x, y], `fundo.ancora` [x, y] e `fundo.repete`. Detalhes na seção 7 do plano.

## Campos

| Campo | Valores |
|---|---|
| `regiao` | `com` · `ext` · `sol` · `cid` · `pri` · `nec` (e as próximas: `mia`, `aet`, `val`) |
| `categoria` | `ter` · `est` · `arq` · `veg` · `roc` · `obj` · `cul` · `pri` · `mag` · `plx` · `fx` · `ani` |
| `funcao` | `decorativo` · `colisor` · `plataforma` · `interativo` · `gate` · `perigo` · `esconderijo` · `luz` · `narrativo` |
| `camada` (ordem) | `ceu` (-100) · `bg_longe` (-90) · `bg_medio` (-80) · `bg_perto` (-60) · `parede_fundo` (-10) · `decor_atras` (-5) · `jogo` (0) · `terreno_detalhe` (1) · `decor_frente` (10) · `fg` (20). `sorting` ≠ 0 sobrescreve |
| `posicionamento.apoio` | `chao` (a base fica no y) · `teto` (o topo fica no y) · `parede`, `borda`, `livre` (o centro fica no y) |
| `epoca` | `natural` · `primata` · `fundacao` · `soluki` · `reforma` · `abandono` · `praga` |

## Cores reservadas

O validador avisa quando um enfeite (`decorativo`, `narrativo`, `luz`) usa uma cor reservada ou parecida (distância RGB < 28): vermelho das armadilhas, brilho da rachadura, hera escalável, verde-ácido e roxo da praga (o roxo é liberado nos fundos) e o verde do matinho. Gates, perigos e esconderijos podem usar as suas.
