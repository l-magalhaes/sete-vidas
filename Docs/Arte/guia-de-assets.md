# Guia de assets de cenário

Como montar uma área nova de Sete Vidas com a biblioteca de cenário, a partir do que foi feito na Fortaleza (plano `SeteVidasDocs/Sete Vidas — Assets e Cenários da Fortaleza.md`, Etapas 1–9).

| Documento | Para quê |
|---|---|
| `catalogo-cenario.md` | Todos os assets com miniatura, id, função, camada, variações e regra de uso. **Gerado** por `Tools/EnvironmentSprites/gerar_catalogo.ps1` |
| `biblioteca-cenario.md` | A parte técnica: manifesto, campos, importação, terreno, grupos, fundos, mundo vivo |
| `paleta.md` | A paleta mestre por grupo de cor |
| `decisoes.md` | Cada decisão de arte com data e motivo, inclusive as que foram desfeitas |

---

## 1. Ordem de montagem de uma área

Monte nesta ordem: cada passo depende do anterior. Ao fim de cada um, gere a prévia (`Tools/LevelPreview/gerar_cena.ps1 x y nome`) e olhe também a versão `_cinza`.

1. **A história e a função.** Responda primeiro: quem vive ou trabalha aqui, o que aconteceu e em que época foi construído. A função da zona decide os objetos (tabela da seção 5 do plano: defesa, treino, alojamento, cerimônia, armadilhas, selo, serviço, exterior).
2. **Terreno por época.** Em `FortressTerrain.Regions()` (ou o equivalente da fase nova), defina o material de cada trecho: `ext_solo`, `ext_rocha`, `ext_terra_batida`, `cid_fundacao`, `cid_soluki`, `cid_reforma`, `cid_abandono` ou `sol_madeira`. Pedra mais antiga embaixo, reformas por cima.
3. **Grupos de objetos.** Pelo menos 2 grupos por área, que contam quem vive ali (`FortressDressing.Groups`). Ex.: mesa + bancos + dados; beliche + baú; barril + panela + vassoura.
4. **Vegetação e rochas.** Densidade caindo da natureza para a ordem. Nada em borda de pulo, placa ou patrulha.
5. **Fundo.** Um conjunto por trecho (`FortressBackgrounds`). Num exterior contínuo, use **um conjunto só** que muda com a altura (seção 4 deste guia).
6. **Janelas** dos interiores (`FortressBackgrounds.Windows()`): mostram o fundo daquela altura.
7. **Primeiro plano:** só silhuetas encostadas no teto ou nos cantos de cima.
8. **Mundo vivo** (`FortressLife`): vento, partículas, bichos, pulsos.
9. **Luz:** toda sombra jogável tem motivo visível; toda fonte acesa fica em área iluminada.
10. **Teste de leitura:** prévia em cor e em cinza, e a pergunta "dá para contar a história desta sala sem texto?".

## 2. Regras de leitura (o que as revisões ensinaram)

Cada regra abaixo veio de um problema real visto na Fortaleza.

| Regra | Por quê / exemplo |
|---|---|
| **Nada flutua.** Todo objeto encosta no chão, na parede ou pende do teto | Arquibancada e torre de escalada flutuando no céu; pombal preso no ar parecia um baú |
| **Toda sombra tem motivo** (alcova, tenda, estátua tapando a tocha, tocha apagada) | Sombras jogáveis "do nada" na galeria |
| **Nada no fundo parece plataforma** nem perigo | Pontas de viga no teto pareciam comportas; tapete com barra dourada parecia uma linha de laser; cano de óleo no alto parecia corrimão |
| **Nada entra na parede** | A tenda e a caixinha de areia do santuário invadiam a parede: confira a largura do sprite (a tenda tem 7 tiles) |
| **Não duplique marcos** | A cachoeira de verdade e a do fundo apareciam lado a lado: o marco do fundo só aparece onde o de verdade não está na tela |
| **Primeiro plano só no teto** | Viga escura cobria a coluna rachada (a rachadura é o sinal do Paladino) |
| **Janela só onde mostra algo** | Janelinhas escuras (nicho, janela tapada) competiam com as janelas de verdade |
| **Enfeite não imita interface** | Marca de giz ao lado da placa parecia um ícone |
| **Armadilha integrada** | O lançador de flechas é um bloco da pedra da parede com a boca de bronze, dentro da parede (centro = face + 0,06 à direita, − 0,125 à esquerda); serra sempre com trilho |
| **Bichos não somem do nada** | O pássaro some só fora da tela; o rato some num buraco na parede |
| **Rachadura só na parede quebrável** | É a linguagem do Paladino; nenhuma outra parede tem rachadura |
| **Menos é mais perto da armadilha** | Engrenagem, alavanca e registro ao lado dos jatos poluíam a leitura e saíram |

## 3. Cores reservadas

O validador (`Tools/EnvironmentSprites/gerar.ps1`) avisa quando um enfeite (`decorativo`, `narrativo`, `luz`) usa uma destas cores ou uma parecida (distância RGB < 28).

| Cor | Hex | Só em |
|---|---|---|
| Armadilha | `#c83a3a` | Armadilhas (dano): espinhos, penas da flecha, faixa do lançador |
| Hera escalável (ramo) | `#4f5a2e` | Parede escalável |
| Hera escalável (folha) | `#8a9a4e` | Parede escalável |
| Hera escalável (brilho e contorno) | `#b8c46c`, `#232a14` | Parede escalável (não entram no validador; use só na hera) |
| Praga (roxo) | `#5b2a6e` | A praga e o Tomo (liberado nos fundos e em assets da época `praga`) |
| Praga (verde-ácido) | `#8fd14f` | A praga e o Tomo (liberado em assets da época `praga`) |
| Matinho (esconderijo) | `#456633` | A moita jogável 32×16 |

Também evite: amarelo-dourado brilhante em coisas que não se pegam (parece moeda; por isso o vaga-lume é `#e6f2c8`); verdes da hera em vegetação decorativa (use os verdes azulados `#1b2a24`…`#8fb070`).

## 4. Fundo de uma região nova

**As três perguntas (plano, 7.2):** de onde o gato está olhando, a que altura e em que momento da história? O fundo é a vista real daquele ponto.

**Camadas (7.3):**

| Camada | Paralaxe (x / y) | Largura | Brilho |
|---|---|---|---|
| `ceu` | 0 / 0 (preso à câmera) | 480×270 | 30–90% |
| `bg_longe` | 0,06 / 0,15 | 960, repete | 20–35% |
| `bg_medio` | 0,2 / 0,3 | 960, repete ou pintado | 15–28% |
| `bg_perto` | 0,35–0,5 | pintado por área | 10–22% |

**Regras (7.6):** três escalas de forma em toda silhueta; nada reto nem simétrico; árvores em bosques de 3–7 com clareiras; camadas repetidas com 960 px e um acontecimento único por volta; **uma luz só** por conjunto (a borda iluminada do mesmo lado em todas as camadas); névoa na base das camadas de longe.

**Continuidade:** dentro de um mesmo exterior, **não troque de conjunto**. Mude com a altura: céu de cima com `SkyHeightBlend` (`Lb(id, deY, ateY)`) e a paralaxe vertical baixando o horizonte. Troque de conjunto só onde o exterior não está à vista (chaminés, portas) ou numa mudança grande (a queda do penhasco, a noite depois da maldição).

**Âncora:** o centro da camada aparece na tela deslocado (âncora − câmera) × paralaxe. Escolha uma câmera de referência por conjunto e calcule a âncora para o horizonte e os marcos ficarem no lugar.

**Reaproveitáveis em qualquer região:**

| Asset | Serve para |
|---|---|
| `bg_ceu_por_do_sol`, `bg_ceu_crepusculo`, `bg_ceu_borda_do_sol`, `bg_ceu_meio_sol`, `bg_ceu_ultimo_raio`, `bg_ceu_noite` | A luz pela altura e pela hora da história |
| `bg_ext_cordilheira`, `bg_ext_cordilheira_noite` | Horizonte de montanhas (sem marco) |
| `bg_ext_floresta_vale`, `bg_ext_copas_de_cima`, `bg_ext_morros_pinheiros` | Mata vista de longe, de cima e de perto |
| `bg_int_seteira`, `bg_int_janela`, `bg_int_janela_grande` (+ máscaras) | Janelas de qualquer interior |
| `fg_corrente` | Primeiro plano de interior |

**Específicos (refazer para cada região):** a vista de perto do lugar (`bg_cid_fachada_perto`), os marcos (`bg_ext_cordilheira_a` com a Cidadela pequena, `bg_ext_montanha_inicio`) e o horizonte da próxima região (`bg_mia_telhados_horizonte`, que já serve para a fase de Miaravéu).

## 5. O que reaproveitar em cada tipo de região

| Região | Reaproveite | Crie |
|---|---|---|
| `com` (comum) | Tochas (acesa e apagada), objetos barulhentos (cacos, folhas, palha, sininhos, balde), marcas da Guilda, corda cortada, partículas, bichos, buraco de rato, teia, santuário (normal e compacto) | Nada, salvo um objeto barulhento novo |
| `ext` (exterior) | Solo, rocha, rampas, tufos, seixos, rochas, fósseis, árvores por idade, arbustos, flores, raízes, água (cachoeira, lagoa), fogueira, mochila, posto de comércio | A rocha e a vegetação próprias se o bioma mudar (ex.: neve), mantendo as mesmas regras de idade e densidade |
| `sol` (Soluki) | Alvenaria `cid_soluki` e madeira, gárgulas, toldo, tenda, postes, todos os objetos de treino, defesa, alojamento e serviço, porta fechada, esconderijos (caixotes, feno) | Só o que for exclusivo do lugar |
| `cid` (Cidadela) | Fundação, reforma, abandono, paredes quebráveis por época, estátuas, estandartes, escudos com nome, vitrais, velas, colunas, armadilhas (cano, elmo, escudo amassado), selos e runas | A arquitetura de outra cidade (outra região, outra paleta de pedra) |
| `pri` (Primatas) | Placas e marco da estrada negra, cristal (apagado e aceso), relíquias | Ruínas maiores, quando a narrativa pedir |
| `nec` (praga) | Veias em 3 intensidades, fissura, poeira roxa, insetos mortos, vasos secos | A forma da praga daquela região (sempre no par roxo + verde-ácido) |

## 6. Orçamento de efeitos

| Item | Limite |
|---|---|
| Elementos animados no fundo | 6 por tela |
| Partículas | 40 na cena (`AmbientEmitter.GlobalCap`); poeira só em feixes de luz |
| Amplitude | 1 px (2 px só em bandeira e água) |
| Salas de combate e armadilha | Só efeitos lentos e raros (poeira roxa); nada que pareça perigo |
| Barulho | Nenhum efeito de cenário faz barulho para a IA; só os objetos barulhentos, que o jogador vê antes de pisar |

## 7. Ferramentas

| Comando (na raiz) | Faz |
|---|---|
| `powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/gerar.ps1` | Gera toda a arte de cenário e valida os manifestos (ids, campos, imagens, cores reservadas) |
| `... Tools/EnvironmentSprites/gerar_catalogo.ps1` | Regera `catalogo-cenario.md` |
| `... Tools/LevelPreview/gerar_cena.ps1 x y nome` | Prévia de uma tela (cor e cinza) sem abrir o Unity |
| `... Tools/LevelPreview/gerar_mapa_arte.ps1 [escala]` | A fase inteira com a arte |
| `... Tools/LevelPreview/gerar_mapa.ps1` | Mapa esquemático (tiles e objetos de jogo) |
| Unity: *Tools > Sete Vidas > 13. Fase 6: Fortaleza* | Monta a cena a partir dos dados |
