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

## Já configurado na Fase 0

- **Pixel Perfect Camera** nas cenas: Reference Resolution 480×270, Assets PPU 16.
- **Importação automática** (`PixelArtImportPostprocessor`): todo PNG em `Assets/_Game/Art/` usa Point, sem compressão, PPU 16, sem mipmaps. Personagens e inimigos ficam com pivot nos pés; o resto, centralizado.
- **Arquivos `.aseprite`** usam o importador próprio. Ao importar o primeiro, ajuste nele: Pixels Per Unit = 16, Filter Mode = Point, Compression = None. Depois salve isso como Preset.
- **Tiles placeholder** em `Art/Environments/Placeholder/`: chão (cinza-arroxeado), parede escalável (pedra com ranhuras e hera seca, desde a Fase 2) e fundo. Têm o tamanho final de 16×16, então podem ser trocados sem refazer as fases.
