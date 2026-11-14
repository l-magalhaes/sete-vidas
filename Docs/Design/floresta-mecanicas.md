# Floresta Silenciosa — mecânicas (F0)

> Plano: `Docs/Floresta/plano-implementacao.md` · Mapa: `Docs/Level/floresta-mapa.md` · Inimigos: `floresta-bestiario.md`.
> **Dano em "hits"**: 1 hit = meio coração (como no resto do jogo). Depois da maldição, o gato tem 2 corações = **4 hits**.
> Status: **[PROPOSTA]** salvo onde diz [APROVADO].

## 1. Pogo por alvo

**Base atual (não muda):** ataque para baixo no ar; ao acertar inimigo ou superfície de pogo, o gato quica a `JumpHeight × PogoHeightFraction` = 4 × 1,8 = **7,2 tiles** e ganha **0,25 s** de graça (`PogoGrace`).

**Novo:** `IPogoTarget` decide **se o alvo aceita o pogo agora**. Fora da janela, o golpe para baixo acerta (se o alvo tiver vida), mas **não quica**, e o contato com o corpo fere normalmente.

| Alvo | Quando aceita pogo | Leitura visual | Fora da janela |
|---|---|---|---|
| Espinho de pogo (cenário) | sempre | pontas com **gota turquesa** (#5fd3c7) e brilho leve pulsando | — |
| Espinho comum (cenário) | **nunca** | pontas escuras, sem brilho. Fere 1 hit e devolve ao chão seguro (como `SpikeStrip`) | — |
| Cacto Flutuante | sempre, **só pelo topo** | topo achatado com flor turquesa | lateral fere 1 hit |
| Planta Carnívora | **só no recuo** (0,8 s depois da mordida) | boca fechada, cabeça baixa, flor turquesa aparece | mordida fere 1 hit |
| Galho Seco | **só vulnerável** (1,0 s depois do golpe, braço preso no chão) | braço enterrado, rachadura turquesa nas costas | golpe fere 1 hit |
| Serpente da Floresta | **só com o bote esticado** (0,6 s) | pescoço esticado, parada no ar | bote fere 1 hit |
| Cabeça da Isyathy | **só nas janelas** (cabeça exposta / atordoada pela flecha) | escamas da testa brilham turquesa | contato fere conforme o golpe |

**Regra de cor [PROPOSTA]:** turquesa (#5fd3c7) = "pode quicar aqui". Na Floresta, essa cor é usada **só** para isso. Na Fortaleza, o vermelho já marca armadilha.

**Métricas de layout:**
- Cadeia vertical: **6 tiles** entre alvos (folga de 1,2 sobre os 7,2).
- Cadeia horizontal: até **4 tiles** entre alvos (o gato anda ~4 tiles no ar durante o quique).
- Tutorial (B1): alvos sobre chão seguro; o primeiro degrau que exige pogo tem **6 tiles**.

## 2. Martelo do Paladino e troncos rachados

Componente `BreakableTree`: generaliza `BreakableWall` com `allowedTool = PaladinHammer`.

| Item | Valor |
|---|---|
| Golpes para quebrar | **3** de marreta (comum ou para baixo) |
| Adaga do Ladino | não fere: faísca, "tunk" seco, nada de lascas. Primeira vez: dica "Isso pede algo mais pesado" (D-RAIZES-01) |
| Estados visuais | inteiro → rachado fundo (1 golpe) → lascado (2) → quebra em pedaços (3) |
| Ruído por golpe | loudness 0,6, raio 10 tiles |
| Ruído da quebra | loudness 1,0, raio 14 tiles |
| Persistência | flag `tronco_quebrado_<id>` (nunca volta) |
| Leitura | rachadura em zigue-zague clara + lascas amarelas no chão. Tronco comum: casca lisa, sem rachadura |

O primeiro tronco (A3) fica sozinho, em chão plano, sem inimigos, com a madeira atrás.

## 3. Baú e Madeira de Reconstrução

| Item | Valor |
|---|---|
| Abrir | encostar + **Interagir** (E). Tampa abre em 0,4 s, a madeira sobe e entra na bolsa |
| Ruído | loudness 0,2, raio 3 (um rangido baixo; no ninho conta, mas pouco) |
| Persistência | flag `bau_<id>` (W01..W12) |
| Contador | `SaveData`: derivado das flags `bau_W*` (sem campo novo). Aba Itens: **"Madeira de Reconstrução: 8/12"** |
| Aviso ao pegar | faixa curta: "Madeira de Reconstrução (8/12)". No primeiro baú, D-MADEIRA-01 |
| Entrega | na Tobi, as 12 de uma vez + 20 moedas (`CoinPurse.TrySpend(20)`). Flag `ponte_reconstruida` |

## 4. Isyathy adormecida: medidor de alerta [APROVADO: meio coração por ataque]

`IsyathyAlert` existe só na cena do ninho. Escuta o `NoiseSystem` (só barulhos do gato) e tem um valor de **0 a 100**.

### Ganho de alerta

O `StealthController` já emite passos (0,15), aterrissagem (0,4) e golpe (0,3). O ninho converte isso em alerta, mais os casos novos:

| Ação (GDD §9.2) | Alerta |
|---|---|
| Parado | 0 |
| Agachado andando | **+1 por segundo** (sem passos audíveis) |
| Andando | +4 por segundo |
| Correndo | +10 por segundo |
| Aterrissagem barulhenta (queda ≥ 4,5 tiles) | +15 |
| Ataque (cada golpe) | +20 |
| Golpe no tronco rachado | +12 cada; **+25 na quebra** (3 golpes + quebra ≈ 60) |
| Abrir baú | +5 |
| Inimigo morrendo | +8 |

### Queda do alerta

| Situação | Queda |
|---|---|
| Escondido (folhagem ou sombra), depois de 1 s parado | −15 por segundo |
| Parado fora de cobertura | −5 por segundo |
| Se movendo | 0 |
| Na Clareira do Musgo (E4) | −30 por segundo |
| Depois de um bote | o valor vai para **60** (não zera) |

### Estados

| Faixa | Estado | Feedback | Consequência |
|---|---|---|---|
| 0–24 | Dormindo | respiração lenta (ciclo de 4 s) | — |
| 25–49 | Inquieta | respiração em 2,5 s, mais grave; folhas caem | — |
| 50–79 | Agitada | ciclo de 1,5 s; escamas contraem; galhos tremem; o olho abre no fundo | **galhos frágeis** tremem; se o gato ficar **1,5 s parado** num deles, ele cai (e se refaz em 8 s) |
| 80–100 | Busca | rugido grave; a cabeça sai do fundo | **bote de busca** (abaixo) |

### Bote de busca

1. A cabeça mira o **último barulho** do gato (posição do `NoiseEvent`), não o gato.
2. **Aviso de 1,2 s**: sombra no chão + escamas caindo + som grave subindo.
3. Bote: acerta uma área de 4 × 3 tiles. **Meio coração (1 hit)** se pegar o gato **[APROVADO]**. Galhos frágeis na área quebram (refazem em 8 s).
4. A cabeça volta ao fundo em 1,0 s. O alerta vai para 60. Nenhum novo bote por **4 s**.
5. Se o alerta passar de 80 de novo, repete. Não há morte instantânea. O perigo é ser acertado várias vezes (4 hits = morte).

**Morrer no ninho:** volta à Clareira do Musgo (E4) se ela já foi tocada; senão, a S-F3. O alerta zera.

## 5. Viagem rápida [APROVADO]

- Em qualquer santuário aparecem **dois avisos juntos** (melhorias 14/11/2026): **[Interagir] descansar** (E / B) e **[Ataque] viagem rápida** (clique / X). Descansar vai direto; a viagem abre a lista dos santuários. O aviso da viagem só aparece com outro santuário visitado e ligado a este, e sem veneno. Ali o botão de ataque não golpeia.
- Lista dos santuários já visitados (flag `santuario_visto_<id>`), agrupados por região, com o nome e o ícone no mapa.
- Viajar: fade de 0,5 s, carrega a cena, o gato aparece **em pé no santuário de destino**. Descansar no destino é separado (não é automático).
- **Bloqueios:** durante luta de chefe, com arena fechada, durante cutscene e com veneno ativo.
- **Ligação entre regiões:** a Floresta só se liga à Fortaleza depois de `ponte_reconstruida` (a queda é de mão única, e a subida termina na cabeceira leste, sem caminho para o oeste até a ponte existir).

## 6. Tela de mapa (aba nova do inventário)

**Mapa do mundo (melhorias 14/11/2026, estilo Hollow Knight).** Substitui a grade abaixo quando o menu **22. Mapa do mundo: gerar silhuetas** já rodou:
- Uma folha só com **todas as áreas ligadas**: a Fortaleza à esquerda, a Estrada de Soluki e a Floresta à direita, cada uma no lugar dela (a borda da Fortaleza encosta no começo da Estrada).
- A forma vem do tilemap `Ground` de cada cena (`Editor/WorldMapSetup.cs` → `Resources/WorldMap.asset`): na Floresta, o ar dentro das salas; na Fortaleza, o ar dentro das salas de `FortressLayout.MapRooms()` (montanha, queda, pátio, muralhas, galeria, chaminés, salão, câmara, poço, cofres, portão). Plataformas e saliências soltas no meio da sala somem (`WorldMapRules.FillIslands`), e a torre de escalada e o poço dos fundos são desenhados como salas inteiras, para o mapa ficar uniforme.
- **Onde o gato passou:** miolo escuro e borda clara, na cor do lugar (pedra azulada, areia, verde). **Onde ainda não passou, nada aparece.** Entrar numa sala revela a sala inteira: na Floresta, cada sala; na Fortaleza, cada sala do `FortressLayout.MapRooms()` (a queda do penhasco, as muralhas da hera...). Além disso, revela 8×8 tiles em volta do gato. Fica no save (`SaveData.mapChunks`).
- **Floresta legível:** no jogo as salas encostam umas nas outras (o anel); no mapa cada sala é uma peça com contorno próprio, **afastada das vizinhas** (o centro fica 25% mais longe da origem, `WorldMapSetup.Spread`), e portas e passagens viram **traços de ligação**. Raízes e blocos soltos no meio da sala não aparecem.
- Ícones: **cabeça do Lion** piscando onde ele está, santuários (aceso/apagado) dos trechos explorados e **baús só depois de abertos**.
- **Atalho rápido:** **M** no teclado ou **Exibir** no controle abre o inventário direto no mapa; o mesmo botão fecha. No mapa: direcional **anda** (segurando), **Enter/A** troca o **zoom** (1×, 2×, 3×). Abre centrado no gato.
- Depois de mudar uma cena (inclusive à mão na Fortaleza), rode o menu 22 de novo.

Grade antiga (fica como reserva enquanto o mapa do mundo não foi gerado):
- Grade com as salas da região atual. Cada sala é um retângulo no tamanho da sala, revelado ao entrar (`sala_vista_<id>`).
- Ícones: santuário (visitado), baú fechado (sala visitada com baú ainda fechado), baú aberto, gato (posição atual), atalho aberto.
- Trocar de região com as setas. Regiões nunca visitadas não aparecem.
- Da tela de mapa, num santuário, dá para escolher o destino da viagem.

## 7. Arena da Isyathy (F1)

- 40 × 30 tiles, **3 alturas**: chão central de 24 tiles; galhos laterais a 4 tiles (x 3–8 e x 32–37); dois galhos de mão única a 8 tiles; plataforma alta central a 12 tiles (**só com pogo**, na cabeça ou num cogumelo saltador). O Rugido Profundo derruba os galhos de mão única por alguns segundos.
- Espinhos comuns nas paredes laterais (1 hit, sem pogo).
- Galhos altos para a Vulpis: um em cada canto superior, fora do alcance do gato.
- A cabeça entra pelas laterais e por cima. O corpo fica no fundo, sem colisão.
- **Vida:** 60 (Koda tinha 45). Fases em **65%** (39) e **30%** (18).
- **Dano por golpe** [APROVADO: meio ou 1 coração; divisão PROPOSTA]: ver `Docs/Floresta/plano-implementacao.md` F8. Leves = 1 hit; pesados = 2 hits.

## 8. Ruído novo (`NoiseSystem`)

| Fonte | Loudness | Raio |
|---|---|---|
| Golpe no tronco | 0,6 | 10 |
| Tronco quebrando | 1,0 | 14 |
| Baú abrindo | 0,2 | 3 |
| Galho frágil caindo | 0,5 | 8 |

Esses barulhos também acordam Morcegos (ver bestiário).

## 9. Residências [APROVADO, pedido do usuário 13/11/2026]

- Porta de residência (`ResidenceDoor`): perto dela, **E: entrar**. A tela escurece e a cena muda para uma **cena pequena**, o interior. A mesma porta, do lado de dentro (**E: sair**), volta para a frente da casa.
- Pode ser **trancada**: sem a chave, o Lion diz "Trancada. Deve ter uma chave por aí."; com a chave, destranca para sempre (`porta_aberta_<id>`) e a chave some do inventário.
- Vale para o jogo todo: casas, **lojas** e outros estabelecimentos. Convenção: o interior tem a entrada `entrada` logo depois da porta; o exterior tem uma entrada com o id da porta (ex.: `cabana_vulpis`) na frente dela.
- Primeira: a **cabana da Vulpis** (trancada; a chave fica em outra sala do hub). Teste: cena `Forest_Test_Cabin`.

## 10. Chave, sala-arena e inimigos básicos (F1, para a sala-hub)

- **Chave** (`KeyPickup`): encostar pega (`chave_<id>`); aparece em Itens até a porta dela ser aberta.
- **Sala-arena** (`ArenaRoom`): entrar fecha os portões; derrotados todos os inimigos de dentro, abrem para sempre (`arena_vencida_<id>`) e os inimigos dela não voltam. Morrer reabre a arena.
  - **Levas:** a arena pode ter levas seguintes (`ArenaRoom.SetWaves`), desligadas até a anterior morrer; após ~0,9 s de aviso (faíscas onde vão sair) entram. A arena só abre depois da última. Morcegos soltos por um Broto dentro da área também contam. Morrer: as levas voltam a esperar. Na C de teste: plantas → Broto + 3 morcegos.
- **Galho Seco** e **Planta Carnívora** em versão básica (`DryBranch`, `CarnivorousPlant`, base `ForestEnemy`), com os valores do bestiário. As variantes infectadas e a arte final ficam para a F4.
