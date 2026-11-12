# Tela inicial
**Escopo:** MVP | **Status:** rascunho para teste

## Objetivo
Entrar no jogo em segundos: continuar de onde parou (o santuário) ou começar do zero sem perder o save por acidente.

## Regras
1. É a **primeira cena da build** (`Scenes/MainMenu/MainMenu`). No editor, o **Play também começa por ela**, mesmo com outra cena aberta (desligue em **Tools > Sete Vidas > Play começa na tela inicial** para testar uma cena direto; os testes automáticos não são afetados).
   - **Área de testes:** abre a Playground com um **save separado** (`save_playground.json`): testar nunca mexe no progresso do jogo. Apertar Play direto na Playground também usa o save de testes.
2. **Novo jogo:** sem save, começa na hora. Com save, abre a confirmação "Começar um jogo novo? O progresso salvo será apagado." com **Não** já selecionado (o padrão é o seguro). **Sim, apagar** apaga o save, abre o **prólogo (a leitura do contrato)** e depois a **entrada da Fortaleza** (`Fortress_MVP`) com **7 corações**.
3. **Continuar:** só fica ativo se existe save no disco. Embaixo dele, um resumo: corações · classes · santuário (ex.: "2 corações · Ladino, Paladino · Santuário: Antes da arena"). Ao confirmar, abre a cena do save (a Fortaleza) e o gato começa no último santuário onde descansou (sem santuário: no início da fase).
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

## No tema (melhoria 12, 12/11/2026)

- **Fundo vivo:** a mesma vista do alto da montanha (conjunto A da fase): o céu do pôr do sol, a cordilheira com a Cidadela em silhueta bem na frente do sol poente e a floresta do vale, com a cor de preenchimento de cada camada embaixo. As camadas andam devagar (0,5 e 1,5 px/s, repetindo a cada 960 px), então a Cidadela fica à vista por vários minutos.
- **Primeiro plano:** um penhasco escuro à esquerda, com a borda de cima iluminada, e o gato sentado nele, **de costas** (capuz da Guilda com as orelhas para fora, a capa com a faixa do Ladino), olhando a Cidadela; a cauda balança em 3 quadros.
- **Logo "SETE VIDAS":** letras de 5×7 em blocos de 3 px, dourado com brilho em cima, contorno e sombra (172×26), com **sete centelhas** num arco por cima; de 5 a 9 s, uma se apaga e volta devagar (a maldição).
- **Menu em pergaminho**, à direita: Continuar (com o resumo do save embaixo), Novo jogo (com a confirmação de antes), **Dicas: ligadas/desligadas** (a opção fica nas preferências do jogador, não no save: um jogo novo não religa as dicas), Área de testes e Sair. **Cursor de patinha.**
- **Área de testes só em build de desenvolvimento** (e no editor).
- **Entrada:** fade a partir do preto (0,8 s).
- Ficou de fora: o gato se levantar e a câmera descer ao escolher (a cena troca direto, como antes).
- Arte gerada pelo `MenuSetup` (menu 12) em `Art/UI/Menu`; vida da tela em `TitleScreenFx`. Maquete: `SeteVidasDocs/Referencias/tela_inicial_maquete.png`.
