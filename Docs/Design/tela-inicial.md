# Tela inicial
**Escopo:** MVP | **Status:** rascunho para teste

## Objetivo
Entrar no jogo em segundos: continuar de onde parou (o santuário) ou começar do zero sem perder o save por acidente.

## Regras
1. É a **primeira cena da build** (`Scenes/MainMenu/MainMenu`).
2. **Novo jogo:** sem save, começa na hora. Com save, abre a confirmação "Começar um jogo novo? O progresso salvo será apagado." com **Não** já selecionado (o padrão é o seguro). **Sim, apagar** apaga o save e começa do início da fase com **7 corações**.
3. **Continuar:** só fica ativo se existe save no disco. Embaixo dele, um resumo: corações · classes · santuário (ex.: "2 corações · Ladino, Paladino · Santuário: Antes da arena"). Ao confirmar, abre a cena do save e o gato começa no último santuário onde descansou (sem santuário: no início da fase).
4. Com save, o **Continuar já vem selecionado**; sem save, o **Novo jogo**.
5. **Sair** fecha o jogo (no editor, para o Play).
6. **Controles:** setas/WASD ou analógico/direcional para escolher; **Enter** ou botão sul do controle para confirmar; o mouse seleciona ao passar por cima e confirma com clique. Uma seta dourada marca o botão selecionado.
7. Cada santuário tem um **nome** (campo `displayName`), gravado no save para aparecer no resumo.

## Visual (480×270, escala ×4)
- Fundo: céu noturno em faixas, estrelas, lua crescente e a **Fortaleza dos Paladinos** em silhueta, com o brilho roxo e verde da praga na torre do tomo.
- Título **SETE VIDAS** em dourado, com as **sete vidas** (corações) embaixo; o gato à esquerda olhando para a fortaleza.
- Botões na caixa de pergaminho dos diálogos.

## Implementação (Unity)
- `MainMenu` (Scripts/UI), `SelectOnHover`, `SaveSummary` (C# puro), `SaveManager.HasSaveFile`, `SaveData.lastSanctuaryName`.
- Cena gerada por **Tools > Sete Vidas > 12. Tela inicial**; também coloca a cena em primeiro na build.

## Perguntas abertas
- [PROPOSTA] Um único save (sem escolher slot).
- [PROPOSTA] Sem tela de opções no MVP (volume, controles); entra no Vertical Slice.
- [PROPOSTA] Sem "voltar ao menu" durante o jogo no MVP (pausa com menu fica para depois).
