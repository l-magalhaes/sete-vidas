# Sistema de diálogo

> **Nome (10/11/2026):** o paladino "Golden" agora se chama **Koda, o Dourado** no jogo. "Golden" continua só nos nomes do código.
**Pilar:** 4 (companheiros com histórias próprias) | **Escopo:** MVP (antes da Fase 6) | **Status:** rascunho para teste

## Objetivo de experiência
Conversar com um companheiro é um momento à parte: o jogo para, os rostos aparecem e dá para ler no próprio ritmo. A troca de lado do retrato deixa claro quem fala sem precisar ler o nome.

## Regras
1. **Abrir:** perto de um NPC aparece "E: falar". E abre a conversa na interface de diálogo.
2. **Interface** (tela 480×270, escala ×4 em 1080p):
   - **Caixa** de pergaminho embaixo, centralizada (380 × 62).
   - **Placa com o nome** em cima da caixa, na cor do falante.
   - **Retrato 48×48** numa moldura dourada, **do lado de quem fala**: o gato à **esquerda**, NPCs à **direita**. A cada fala o retrato e a placa trocam de lado conforme o falante.
   - **Linha de sistema** (sem falante): sem retrato e sem placa, texto centralizado em itálico (ex.: "Paladino desbloqueado!").
   - **Seta piscando** no canto quando a fala terminou de aparecer.
3. **Texto letra por letra** (45 letras/s) com um **bipe de voz** a cada 2 letras, na altura de cada falante (gato agudo, Golden grave). Linha de sistema não bipa.
4. **Avançar:** E, Espaço ou clique. Com o texto ainda aparecendo, o primeiro aperto **mostra a fala inteira**; o seguinte vai para a próxima. Na última, fecha.
5. **O jogo pausa** durante a conversa (inimigos, física, tempo). O gato não se mexe, não pula, não ataca e não troca de classe.
6. **O controle volta no frame seguinte** ao fechamento: o aperto que fechou a conversa não reabre nem faz o gato pular.
7. **Expressões:** cada fala pode pedir uma expressão do retrato (ex.: gato "ironico"); sem ela, usa o retrato padrão.
8. **Conversas encadeadas:** ao terminar, uma conversa pode abrir outra (vínculo → dica de sistema).

## Dados (Unity)
- `SpeakerProfile` (ScriptableObject): nome, retrato padrão, expressões, lado da tela, cor do nome, altura da voz. Em `ScriptableObjects/Dialogue/Speakers/`.
- `DialogueDefinition` (ScriptableObject): ID do roteiro (ex.: D-GOLDEN-01), se é obrigatória e a lista de falas (falante, expressão, texto). Em `ScriptableObjects/Dialogue/`. **O texto é dado, não código**: dá para revisar no Inspector, e a tradução para inglês fica mais fácil depois.
- `DialogueRunner` (no prefab do HUD): `Play(conversa, controle, aoTerminar)`.
- O setup **não sobrescreve** conversas já criadas: edite o texto direto no asset.
- **Limites da narrativa** (testados automaticamente): até 90 caracteres por caixa e até 6 caixas numa conversa obrigatória.

## Parâmetros tunáveis
| Parâmetro | Valor inicial | Onde vive |
|---|---|---|
| Letras por segundo | 45 | `DialogueRunner` (HUD) |
| Bipe a cada N letras / volume | 2 / 0,12 | idem |
| Altura da voz | Gato 1,35 · Golden 0,8 | cada `SpeakerProfile` |

## Critérios de aceite
- Dado o gato perto do Golden, quando aperta E, então a caixa abre, o jogo pausa e o retrato do Golden aparece à direita com o nome dele.
- Dada a fala seguinte do gato, então o retrato e a placa passam para a esquerda, com o rosto irônico.
- Dado o texto ainda aparecendo, quando aperta E, então a fala aparece inteira; outro E vai para a próxima.
- Dada a última fala, quando aperta E, então a caixa fecha, o jogo volta e o gato não pula nem reabre a conversa.

## Perguntas abertas
- [PROPOSTA] Fonte: a fonte embutida do Unity é provisória. Trocar por uma fonte pixel com acentos (7–9 px de altura, regra da skill de arte).
- [PROPOSTA] Sem botão de "pular conversa inteira" no MVP.
- [PROPOSTA] Retratos fixos (sem animação de boca/piscar) no MVP.
