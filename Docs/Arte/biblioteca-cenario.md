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

- **Materiais prontos:** `ext_solo` (grama, terra, transição, rocha), `ext_rocha` (pedras, paredes de rocha-mãe), `ext_terra_batida` (pátios) e a alvenaria por época: `cid_fundacao`, `cid_soluki`, `cid_reforma`, `cid_abandono` e `sol_madeira`. Células sem material ficam com o tile provisório.
- **Alvenaria** pinta os três tilemaps: chão (topo com pedra de cobertura → miolo → meio-tom → interior escuro), parede do fundo (escura, roxa) e parede escalável (a pedra da época + ranhuras + hera seca). O `TerrainPainter` põe sozinho uma gárgula (ou calha, na madeira) no topo de toda vinha e os "acontecimentos" nas paredes do fundo. Arte: `Tools/EnvironmentSprites/ArchitectureSpriteGen.cs` → `Art/Environments/Citadel/Architecture`.
- **Mudar o material de um trecho:** acrescente uma região em `Editor/Scenery/FortressTerrain.cs` (regiões listadas depois valem por cima).
- **Rampas:** em `FortressLayout.Slopes()`; com material `ext_solo` e proporção 1:1 ou 1:2 usam a arte final, senão o triângulo provisório.
- **Detalhes automáticos:** tufos e seixos na superfície, fendas na rocha funda (`TerrainAutotile.TopDetails` / `DeepDetails`), na camada `terreno_detalhe`. Para tirar de um trecho (ex.: fundo da lagoa), use `FortressTerrain.NoDetails()`.
- **Arte:** `Tools/EnvironmentSprites/TerrainSpriteGen.cs` desenha todos os nomes que `TerrainAutotile.AllTileNames` pode pedir. O teste `TerrainAutotileTests.GeneratedArt_HasEveryTileTheRuleCanAskFor` avisa se faltar algum.
- **Material novo** (ex.: piso de pedra da Cidadela na Etapa 3): acrescente o nome em `TerrainAutotile.Materials`, as faixas que ele usa e o desenho no gerador.

## Conferir sem abrir o Unity

`powershell -ExecutionPolicy Bypass -File Tools/LevelPreview/gerar_cena.ps1 <x> <y> <nome>` monta uma tela 480×270 com a câmera em (x, y) usando a arte real (fundo, terreno, rampas, detalhes, enfeites, cachoeira, espinhos, moedas e o gato) e grava `SeteVidasDocs/Referencias/cena_<nome>.png` e a versão `_cinza` (teste de leitura). Sem luz 2D nem animação.

**Animação:** `animacao.quadros` + `animacao.fps`. A montagem põe um `SpriteLoop`, e cada objeto começa num ponto diferente do ciclo (pela semente da posição). Para assets com variações animadas (árvores), use `animacao.quadros_por_variacao: n`: cada sprite de `sprites` termina em `_0.png` e tem os irmãos `_1` … `_{n-1}`; a montagem pega os quadros da variação escolhida.

**Vegetação:** coloque árvores, arbustos e tocos em `FortressDressing.Items()` (são poucos e cada um conta história). Tufos, flores, seixos, fendas e raízes são espalhados sozinhos pelo `TerrainPainter`. Tufos ganham o `BendOnPass` (curvam quando o gato passa). Regras: verdes azulados (nunca os da hera escalável nem o #456633 do matinho), arbustos decorativos sempre mais escuros e maiores que o matinho, nada em borda de pulo, placa ou patrulha.

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

## Grupos de objetos (Etapa 5)

Objetos que contam juntos quem vive ou trabalha num lugar (mesa + bancos + dados, beliche + baú, barril + panela + vassoura…). Ficam em `FortressDressing.Groups` como dados: cada peça é `"id|dx|dy"` (tiles a partir do ponto do grupo). Coloque um grupo com `Group(d, área, "nome", x, y)`. Para outra fase, copie os grupos que servem e crie os da região. Regra: pelo menos 2 grupos por área, todos atrás do jogo (`decor_atras`) e nunca na frente de placa, pulo, armadilha ou patrulha.

**Objetos barulhentos** não são enfeites: entram no mapa jogável (`noisy` no `FortressLayout`), ver `Docs/Design/objetos-barulhentos.md`.

**Variações com significado** (use `Fixed` com a variação, nunca o sorteio): `env_mag_selo_porta` = porta − 1 (0 intacto e limpo … 6 rompido); `env_nec_veia` = intensidade − 1 (0 finas, 1 com galhos, 2 grossas); `env_sol_obj_porta_fechada` 0 trancada, 1 tábuas pregadas; `env_com_cul_marca_guilda` 0 olho (sombra boa), 1 seta (por aqui), 2 nove riscos (já passamos). Em grupos, a variação é o 4º campo: `"id|dx|dy|variação"`.

**Cores da praga:** só assets com `epoca: "praga"` podem usar o roxo #5b2a6e e o verde-ácido #8fd14f (o validador libera essa época).

## Fundos por trecho (Etapa 6)

- **Conjuntos e zonas:** `Editor/Scenery/FortressBackgrounds.cs`. Cada conjunto é a vista real de um trecho (céu, longe, médio, perto). Num exterior contínuo, use UM conjunto e deixe a altura mudar a vista (céu com `Lb(id, deY, ateY)` → `SkyHeightBlend`, paralaxe vertical). Troque de conjunto só onde o exterior não está à vista ou a mudança é grande (queda, chaminé, porta).
- **Âncora:** o centro da camada aparece na tela deslocado (âncora − câmera) × paralaxe. Escolha uma câmera de referência e calcule a âncora para o horizonte e os marcos ficarem no lugar certo.
- **Janelas:** `FortressBackgrounds.Windows()`; a arte é a moldura + a máscara (`_mascara.png`). Não desenhe a paisagem na janela: ela vem do fundo ativo.
- **Primeiro plano (fg):** só silhuetas escuras encostadas no teto ou nos cantos de cima; nunca na altura do gato.

## Mundo vivo (Etapa 7)

Montado pelo `Editor/Scenery/FortressLife.cs` (copie para outra fase e troque os dados):
- `Wind(...)`: zonas de vento (`WindZone2D`). Folhas e fumaças seguem `WindZone2D.At(posição)`.
- `Emitter(...)`: `AmbientEmitter` com tipo (poeira, folhas, vaga-lumes, poeira da praga), área, taxa e máximo. Poeira só em feixes de luz; nas salas de armadilha, só a poeira roxa rara. O teto global é 40.
- `Creature(...)`: bicho que foge (`ReactOnProximity` Flee). Só visual.
- Reações por id da biblioteca: cristal → Swap (acende perto), vela → Lean (inclina), runas, veias e selos corrompidos → `PulseLight`.
