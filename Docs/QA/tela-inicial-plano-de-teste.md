# Plano de teste — Tela inicial

Antes: rode **Tools > Sete Vidas > 12. Tela inicial**. Abra `Scenes/MainMenu/MainMenu` e aperte Play.

| ID | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|
| MNU-01 | Sem save (Save > Apagar save) | Play | "Continuar" apagado; resumo "nenhum jogo salvo"; "Novo jogo" selecionado com a seta | ☐ |
| MNU-02 | Sem save | Enter em "Novo jogo" | Abre a Playground no início, 7 corações | ☐ |
| MNU-03 | Descansou no santuário antes da arena | Play no menu | "Continuar" selecionado; resumo com corações, classes e "Santuário: Antes da arena" | ☐ |
| MNU-04 | MNU-03 | Enter em "Continuar" | Abre a Playground com o gato no santuário de x 172 | ☐ |
| MNU-05 | Com save | "Novo jogo" | Pergunta de confirmação; "Não" selecionado | ☐ |
| MNU-06 | MNU-05 | "Não" | Fecha a pergunta; o save continua (Continuar ativo) | ☐ |
| MNU-07 | MNU-05 | "Sim, apagar" | Jogo novo: 7 corações, início da fase; o Continuar some na próxima vez | ☐ |
| MNU-08 | — | Navegar com setas, W/S, controle e mouse | A seta acompanha; o mouse seleciona ao passar | ☐ |
| MNU-09 | — | "Sair" | Para o Play (no editor) | ☐ |
| MNU-10 | Build de Windows | Abrir o .exe | Abre na tela inicial | ☐ |
| AUTO-09 | — | Test Runner | Todos passam | ☐ |
