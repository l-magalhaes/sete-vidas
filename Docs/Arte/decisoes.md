# Decisões de arte

Status: **[PROPOSTA]** aguarda aprovação · **[APROVADA]** em uso.

| Data | Decisão | Status | Impacto |
|---|---|---|---|
| 09/11/2026 | Estilo pixel art | [APROVADA] (GDD §14) | Tudo |
| 09/11/2026 | Resolução interna 480×270 (escala ×4 para 1080p) | [APROVADA] 09/11/2026 | Câmera, UI, level design |
| 09/11/2026 | Tile 16×16 px, Pixels Per Unit 16 | [APROVADA] 09/11/2026 | Física, métricas, tilesets |
| 09/11/2026 | Gato com 22 px de altura (14 px agachado), canvas 48×48, pivot nos pés | [APROVADA] 09/11/2026 | Colisor do jogador, animações |
| 09/11/2026 | Aseprite como ferramenta; importador `.aseprite` do Unity | [PROPOSTA] | Pipeline de arte |
| 09/11/2026 | Corpo-base do gato + camadas por classe | [PROPOSTA] | Custo de produção das 7 classes |
| 09/11/2026 | Parede escalável = a mesma pedra do chão + ranhuras de apoio + hera seca (#4f5a2e / #8a9a4e). Nunca usar esse visual em parede não escalável | [PROPOSTA] | Leitura do gate do Ladino em todas as regiões |
| 09/11/2026 | Corte da adaga: arco claro (#f4f7fb / #a9bcd6) de 18×16 px, um desenho por golpe do combo | [PROPOSTA] | Efeitos de combate do Ladino |
| 09/11/2026 | Boneco de treino de palha 16×26 px com rosto só de um lado (frente e costas legíveis de longe) | [PROPOSTA] | Leitura do ataque pelas costas |
| 09/11/2026 | Sentinela placeholder: cão de armadura 26 px (canvas 32×28), tabardo vermelho, olho claro só na frente; lança recuada na preparação e esticada no golpe | [PROPOSTA] | Leitura de ameaça, frente/costas e aviso de ataque |
| 09/11/2026 | Sombra jogável = retângulo escuro (alfa 0,5) desenhado por cima do gato: quem entra escurece. Ícones "?" amarelo e "!" vermelho sobre o inimigo | [PROPOSTA] | Legibilidade da furtividade até existir luz 2D de verdade |
| 09/11/2026 | Matinho: moita 32×16 px (2 tiles) com 15 px de altura, verde #456633 com contorno #243822 e pontas secas #9a924a, desenhada na frente do gato: agachado (14 px) ele some, de pé (22 px) a cabeça aparece | [PROPOSTA] | Leitura do esconderijo sem UI |
| 09/11/2026 | Corações do HUD 16×16 (13×11 de desenho): cheio #d6304a com brilho #ff8a9a, meio (metade esquerda), vazio #4a2a33, bloqueado cinza #3b3b48 com rachadura. Canvas 480×270 escalado ×4 | [PROPOSTA] | UI de vida |
| 09/11/2026 | Santuário felino 32×32: pedestal de pedra, estátua de gato sentado e duas velas; aceso = chamas e olhos dourados #ffd34d | [PROPOSTA] | Leitura do checkpoint ativo |
| 09/11/2026 | Meio coração no chão: metade esquerda do coração do HUD (7×11 px) com brilho rosa claro em volta, flutuando de leve | [PROPOSTA] | Leitura do drop de vida |
| 09/11/2026 | Paladino = mesmo gato + camada da marreta (canvas 32×28: nas costas, erguida, no chão). Cabo #6b4a2b, cabeça #b8bfcc com faixa dourada #d9a441 | [PROPOSTA] | Identidade do gato em todas as classes |
| 09/11/2026 | Ícones de classe 16×16: Ladino (adagas cruzadas em azul-noite), Paladino (marreta em dourado escuro) | [PROPOSTA] | HUD |
| 09/11/2026 | Parede rachada 16×64: pedra do chão + rachadura em zigue-zague dourada (#ffd34d / #8a621e); danificada com galhos | [PROPOSTA] | Gate do Paladino |
| 09/11/2026 | Golden placeholder 24×34: cão dourado de armadura, tabardo dourado, orelha caída, marreta apoiada | [PROPOSTA] | Vínculo do Paladino |
| 09/11/2026 | Interface de diálogo: caixa de pergaminho #e9dcb8 com contorno #3b2a1e e cantos chanfrados (9-slice), placa de madeira #6b4a2b com filete dourado, moldura dourada do retrato. Escala inteira (1 pixel = 1 unidade da tela 480×270) | [PROPOSTA] | Toda a UI de texto |
| 09/11/2026 | Retratos 48×48: gato (neutro e irônico, com o lenço do Clã das Sombras) olhando para a direita; Golden (orelhas caídas, gola de armadura) olhando para a esquerda | [PROPOSTA] | Diálogos |
| 09/11/2026 | Fonte de texto provisória (embutida do Unity) até escolher uma fonte pixel com acentos | [PROPOSTA] | Diálogos, placas |
| 09/11/2026 | Golden chefão 40×40: mesmo corpo do vínculo com a marreta em 8 posturas (parado, andando, erguida, golpe, agachado para saltar, no ar, ajoelhado curando, invocando raios) | [PROPOSTA] | Leitura dos golpes do chefe |
| 09/11/2026 | Relâmpago: aviso = coluna clara piscando cada vez mais rápido; raio = núcleo branco em zigue-zague com bordas amarelas. Cura = anel dourado pulsando | [PROPOSTA] | Legibilidade dos ataques |
| 09/11/2026 | Tomo corrompido 14×12: capa roxa #5b2a6e com runas verde-ácido #8fd14f (cores exclusivas da praga) e um olho; grade de ferro 16×64; pedestal de pedra | [PROPOSTA] | Sala do tomo |
| 09/11/2026 | Armadilhas: **vermelho (#c83a3a) só em armadilhas** (pontas dos espinhos, marcas do fio, da placa, penas da flecha, brilho do lançador). Aço #c0c6d2, chama branco→laranja→vermelho, plataforma frágil em pedra mais clara com rachaduras | [PROPOSTA] | Leitura de perigo em menos de 1 s |

## Já configurado na Fase 0

- **Pixel Perfect Camera** nas cenas: Reference Resolution 480×270, Assets PPU 16.
- **Importação automática** (`PixelArtImportPostprocessor`): todo PNG em `Assets/_Game/Art/` usa Point, sem compressão, PPU 16, sem mipmaps. Personagens e inimigos ficam com pivot nos pés; o resto, centralizado.
- **Arquivos `.aseprite`** usam o importador próprio. Ao importar o primeiro, ajuste nele: Pixels Per Unit = 16, Filter Mode = Point, Compression = None. Depois salve isso como Preset.
- **Tiles placeholder** em `Art/Environments/Placeholder/`: chão (cinza-arroxeado), parede escalável (pedra com ranhuras e hera seca, desde a Fase 2) e fundo. Têm o tamanho final de 16×16, então podem ser trocados sem refazer as fases.
