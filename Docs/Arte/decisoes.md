# Decisões de arte

Status: **[PROPOSTA]** aguarda aprovação · **[APROVADA]** em uso.

| Data | Decisão | Status | Impacto |
|---|---|---|---|
| 09/11/2026 | Estilo pixel art | [APROVADA] (GDD §14) | Tudo |
| 09/11/2026 | Resolução interna 480×270 (escala ×4 para 1080p) | [PROPOSTA] | Câmera, UI, level design |
| 09/11/2026 | Tile 16×16 px, Pixels Per Unit 16 | [PROPOSTA] | Física, métricas, tilesets |
| 09/11/2026 | Gato com ~20–24 px de altura, canvas 48×48, pivot nos pés | [PROPOSTA] | Colisor do jogador, animações |
| 09/11/2026 | Aseprite como ferramenta; importador `.aseprite` do Unity | [PROPOSTA] | Pipeline de arte |
| 09/11/2026 | Corpo-base do gato + camadas por classe | [PROPOSTA] | Custo de produção das 7 classes |

## Já configurado na Fase 0

- **Pixel Perfect Camera** nas cenas: Reference Resolution 480×270, Assets PPU 16.
- **Importação automática** (`PixelArtImportPostprocessor`): todo PNG em `Assets/_Game/Art/` usa Point, sem compressão, PPU 16, sem mipmaps. Personagens e inimigos ficam com pivot nos pés; o resto, centralizado.
- **Arquivos `.aseprite`** usam o importador próprio. Ao importar o primeiro, ajuste nele: Pixels Per Unit = 16, Filter Mode = Point, Compression = None. Depois salve isso como Preset.
- **Tiles placeholder** em `Art/Environments/Placeholder/`: chão (cinza-arroxeado), parede escalável (verde com listras) e fundo. Têm o tamanho final de 16×16, então podem ser trocados sem refazer as fases.
