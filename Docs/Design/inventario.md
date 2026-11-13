# Inventário
**Pilar:** 1 (sete corações, sete classes) e 2 (exploração com gating) | **Escopo:** MVP (substitui a versão em texto da melhoria 10) | **Status:** implementado (falta conferir no Unity)

## Objetivo de experiência
Abrir o inventário deve mostrar, num relance, **quem o gato é agora** (a classe equipada, animada) e **o que ele já recuperou**. Por enquanto só aparece o que o gato tem: classes e habilidades que faltam ficam escondidas (decisão do usuário, 12/11/2026).

## Referências (o que pegamos de cada uma)
| Jogo | O que vale para nós |
|---|---|
| Hollow Knight | Três colunas: personagem à esquerda, grade de ícones no meio, descrição à direita. Nada de texto na grade: o texto mora no painel da direita |
| Stardew Valley / Sea of Stars | Quadrados de slot com moldura e fundo afundado (bevel), cursor que salta de slot em slot, contadores no canto do ícone |
| Celeste / Dead Cells | Ícone de 16×16 lido sem cor (silhueta clara), animação curta no item selecionado |

## Regras
1. **Abrir e fechar:** Tab, I, Start ou Select abrem; a mesma tecla, Esc ou B fecham. Pausa o jogo (`timeScale` 0) e trava o controle do gato. Não abre em diálogo, morto, caído ou no meio de um tranco.
2. **Layout 400×214** (pixels do jogo), pergaminho escuro com moldura:
   - **Personagem (esquerda, 100 px):** o gato em **2×**, na animação **idle** (respira e pisca, mesmo com o jogo pausado), com a **arma da classe equipada** (marreta nas costas do Paladino). A capa e a cauda aparecem como estavam no instante da pausa. Embaixo: os corações (iguais aos do HUD), as moedas com o ícone e o último santuário.
   - **Grade (meio, 5 × 6 slots):** quadrados de 22×22 com ícone de 16×16 e contador no canto (moedas, lacres). Slot vazio = fundo afundado; travado = silhueta escura com "?".
   - **Detalhe (direita, 142 px):** o ícone em 2× numa moldura, o nome (na cor da classe), de onde veio (ex.: "Vínculo com Koda, o Dourado"), a descrição e um rodapé dourado ("Equipada", "Enter: equipar", "2 de 3 lacres acesos").
3. **Abas** (ícone + nome, no topo): **Classes** · **Habilidades** · **Itens** · **Diário** · **Controles** (12/11/2026: a lista de comandos do aparelho em uso, ver `controles-na-tela.md`). Q/E ou LB/RB trocam (os botões aparecem em pixel art nas pontas); a aba lembra o último slot.
4. **Cursor:** setas, WASD ou D-pad andam na grade (para nos cantos, sem dar a volta). O slot selecionado ganha cantos dourados que pulsam; o ícone sobe 1 px.
5. **Classes:** só as recuperadas, na ordem das classes. A equipada tem moldura dourada. **Enter / A equipa** a classe selecionada: o gato da esquerda troca na hora.
6. **Habilidades:** as que o gato já tem (escalar com o pulo de parede, agachar e furtividade, ataque pelas costas, estocada para baixo), com o rodapé "Vale em qualquer classe", e depois os **pergaminhos de magia**: por enquanto o da cura do Koda (lacres no canto), que aparece depois do vínculo.
7. **Itens:** moedas (contador). Os corações não entram aqui: já estão no painel do gato. O resto da grade fica vazio (espaço para o futuro).
8. **Diário por capítulo:** cada capítulo começa numa linha nova com o título em dourado ("Capítulo 1 · A Cidadela") e um filete; embaixo, um slot por placa lida, na ordem do caminho. O cursor pula a linha do título. O capítulo atual aparece mesmo sem placa lida. Por enquanto só existe o capítulo 1 (`InventoryTexts.Chapters`, por cena).

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Unidade | Faixa de teste | Onde vive |
|---|---|---|---|---|
| Escala do gato | 2 | × | 2–3 | `InventoryMenu.previewScale` |
| Tamanho do slot (e espaço entre eles) | 22 (+2) | px | fixo na arte / 1–3 | `InventoryIcons.SlotSize`, `InventoryMenu.slotGap` |
| Colunas × linhas da grade | 5 × 6 | slots | 4–6 × 3–7 | `InventoryMenu.columns`, `rows` |
| Pulso do cursor | 3 | Hz | 2–4 | `InventoryMenu.cursorPulse` |

## Interação com outras classes e sistemas
- Trocar de classe pelo inventário usa a mesma regra da troca normal (`ClassManager.TrySwitch`): não troca no meio de um golpe nem morto.
- Os corações seguem a mesma regra do HUD (antes da maldição, os 7 do Ladino; depois, um por classe).
- As dicas do tutorial se ligam e desligam só na tela inicial (a opção saiu do inventário, para a grade ficar limpa).

## Implementação (Unity)
- `InventoryMenu` (UI montada em código, sem prefab) + `InventoryIcons` (desenho dos ícones em C# puro, também usado na prévia fora do Unity).
- O gato vem do `CharacterAnimationSet` do `PlayerVisual` (clip `idle`) tocado em tempo real (sem `timeScale`), e a camada da classe do mesmo `PlayerVisual`.

## Critérios de aceite
- Dado o inventário aberto com o Ladino, **então** o gato aparece animado (respira) e sem marreta; ao equipar o Paladino na aba Classes, a marreta aparece nas costas na hora.
- Dado o cursor no slot da direita, **quando** aperto → **então** ele não sai da grade.
- Dado uma classe travada selecionada, **quando** aperto Enter, **então** nada acontece (sem erro).
- Dado uma placa lida, **então** ela aparece no Diário e o texto dela no painel da direita.
- Fechando, o jogo volta exatamente como estava.

## Perguntas abertas
- [PROPOSTA] Quando as classes e habilidades que faltam voltarem a aparecer, usar a silhueta escura com "?" (já desenhada em `InventoryIcons.Silhouette`).
- [PROPOSTA] Mouse: por enquanto só teclado e controle.

## Arte (gerada em código)
- `InventoryIcons` (C# puro, também roda na prévia fora do Unity): 19 ícones 16×16 com contorno automático (#140f18), a silhueta "?" das coisas que faltam, o slot 22×22 com bisel (normal, equipado em dourado, vazio), o cursor de cantos dourados 26×26 (respira 1 px) e as molduras 9-slice (janela com filete dourado e rebites, painel afundado, aba e aba ativa).
- Ícones das classes: adaga (Ladino), marreta (Paladino), fogo (Mago), bastão com faixas (Monge), nota (Bardo), arco (Caçador), broto com flor (Druida). Abas: coração do Ladino, pata, bolsa, livro.
- `InventoryCatPreview`: o gato em 2× com os quadros do jogo (idle + piscada), a cauda e a capa com a mesma física do jogo e a marreta nas costas quando o Paladino está equipado.
