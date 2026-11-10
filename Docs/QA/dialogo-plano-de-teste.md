# Plano de teste — Sistema de diálogo

> **Nome (10/11/2026):** o paladino "Golden" agora se chama **Koda, o Dourado** no jogo. "Golden" continua só nos nomes do código.

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080.
Antes: rode **Tools > Sete Vidas > 9. Diálogos** e **Tools > Sete Vidas > Save > Apagar save**. O Golden fica em cima da laje em x 168,5 (Position X do `Player` = 153, depois agarre a borda em x 156).

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| DLG-01 | Abrir | Perto do Golden | E | Caixa embaixo; retrato do Golden à direita; placa "Golden" | ☐ |
| DLG-02 | Pausa | Conversa aberta | Observar | Inimigos e o gato parados; corações não mudam | ☐ |
| DLG-03 | Letra por letra | Fala aparecendo | Esperar | Texto surge letra a letra com bipe grave | ☐ |
| DLG-04 | Pular texto | Fala aparecendo | E | Fala aparece inteira; seta pisca | ☐ |
| DLG-05 | Alternar lados | Fala do Golden completa | E | Fala do gato: retrato à esquerda (cara irônica), placa "Gato", bipe agudo | ☐ |
| DLG-06 | Outras teclas | Conversa aberta | Espaço e clique | Também avançam | ☐ |
| DLG-07 | Controle travado | Conversa aberta | A/D, S, Q/R, 1–7 | Nada acontece | ☐ |
| DLG-08 | Fim do vínculo | Última fala do Golden | E | Fecha e abre a linha de sistema (sem retrato, itálico) "Paladino desbloqueado!"; 2º coração no HUD | ☐ |
| DLG-09 | Fechar | Linha de sistema | E (ou Espaço) | Fecha; o jogo volta; o gato **não** pula e a conversa **não** reabre | ☐ |
| DLG-10 | Falas seguintes | Vínculo feito | E várias vezes (uma por conversa) | Uma fala curta por vez, em rodízio; a 3ª tem resposta do gato | ☐ |
| DLG-11 | Nítido | — | Olhar a caixa e os retratos em 1920×1080 | Bordas e retratos sem borrão (escala inteira) | ☐ |
| DLG-12 | Editar texto | Selecionar `ScriptableObjects/Dialogue/D-GOLDEN-02a` | Mudar o texto e dar Play | O jogo mostra o texto novo | ☐ |
| AUTO-06 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam (inclui o limite de 90 caracteres em todas as conversas) | ☐ |

**Casos-limite:** abrir a conversa durante o hitstop de um golpe; abrir a conversa com a Sentinela em combate por perto (o jogo pausa, ninguém ataca); apertar E e Espaço no mesmo frame; fechar a conversa segurando A/D (o gato só anda depois do frame seguinte).
