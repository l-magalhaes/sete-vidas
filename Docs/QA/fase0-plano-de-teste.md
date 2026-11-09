# Plano de teste — Fase 0

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| F0-01 | Projeto | Projeto aberto | Ver o Console após compilar | Nenhum erro vermelho | ☐ |
| F0-02 | Setup | F0-01 ok | Tools > Sete Vidas > Executar Fase 0 completa | Mensagens "[Sete Vidas]" de sucesso no Console, sem erros | ☐ |
| F0-03 | Layers | F0-02 ok | Project Settings > Tags and Layers | Layers 8–14 = Player, Enemy, Ground, Climbable, Breakable, Hazard, Interactable | ☐ |
| F0-04 | Colisão | F0-02 ok | Project Settings > Physics 2D > Layer Collision Matrix | Enemy×Enemy desmarcado; Interactable marcado só com Player | ☐ |
| F0-05 | Pacotes | F0-02 ok | Window > Package Manager > In Project | Cinemachine, Input System, 2D Tilemap Extras, 2D Aseprite Importer e Test Framework instalados | ☐ |
| F0-06 | Input | — | Abrir `Settings/Input/GameControls` | Mapa Player com 16 ações e esquemas Keyboard&Mouse e Gamepad | ☐ |
| F0-07 | Cena | F0-02 ok | Abrir `Scenes/Dev/Playground` | Grid com Background, Ground, Climbable e Foreground; degraus, buracos, passagem baixa e parede visíveis | ☐ |
| F0-08 | Pixel art | F0-07 ok | Game view em 1920×1080, apertar Play | Tiles nítidos (sem borrão) e sem tremer; câmera com Pixel Perfect Camera 480×270 / PPU 16 | ☐ |
| F0-09 | Importação | — | Arrastar qualquer PNG para `Assets/_Game/Art/UI` | Inspector: Sprite, PPU 16, Point, Compression None | ☐ |
| F0-10 | Testes | F0-02 ok | Test Runner > EditMode > Run All | 4 testes passam | ☐ |
| F0-11 | Testes | F0-02 ok | Test Runner > PlayMode > Run All | 1 teste passa | ☐ |
| F0-12 | Build | F0-02 ok | File > Build Profiles | Playground e Fortress_MVP na lista de cenas | ☐ |
| F0-13 | Git | — | `git status` após o primeiro commit | Nenhuma pasta Library/Temp/Logs/UserSettings rastreada | ☐ |

**Pronto quando** (Docs/Fase 0): projeto abre sem erros, primeiro commit no GitHub e a `Playground` mostra tiles nítidos em 1920×1080.
