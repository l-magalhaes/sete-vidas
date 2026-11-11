# Objetos barulhentos (cacos de vidro, folhas secas, sininhos, baldes)
**Pilar:** exploração e furtividade | **Escopo:** MVP (cenário da Fortaleza) | **Status:** implementado (componente, arte com reação e colocação na Fortaleza: Etapa 5A)

## Objetivo de experiência
O cenário também conta para a furtividade: o jogador olha o chão, vê cacos de vidro ou folhas secas e decide pular por cima, andar agachado ou dar a volta. O perigo é legível antes de ser pisado.

> Decisão de 10/11/2026: **pássaros e ratos que fogem do gato são só visuais** e não fazem barulho para a IA. O barulho vem de objetos parados, que o jogador vê antes.

## Regras
1. O objeto tem um trigger. Quando o gato está dentro e a condição do tipo é cumprida, ele emite um ruído no `NoiseSystem` (o mesmo dos passos e da aterrissagem), que as Sentinelas já ouvem.
2. Tipos e quando fazem barulho:

| Tipo | Faz barulho quando | Silencioso quando | Raio / volume | Como evitar |
|---|---|---|---|---|
| **Cacos de vidro** | pisa andando, **até agachado** | parado em cima, ou no ar | 10 tiles / 0,7 (era 5 / 0,5) | pular por cima (faixas de 1–3 tiles) |
| **Folhas secas, palha** | correndo (≥ 60% da velocidade) | andando devagar ou agachado | 3 / 0,2 | andar agachado |
| **Sininhos pendurados** | encosta de pé ou pulando através | agachado (passa por baixo) | 11 / 0,75 (era 6 / 0,6; os de alarme da galeria, 16) | agachar |
| **Balde, panela** | correndo contra (cai **uma vez**) | andando devagar ou agachado | 4 / 0,4 | andar devagar; depois de caído não faz mais barulho |

3. Entre dois barulhos há um tempo mínimo (cacos 0,5 s, folhas 0,4 s, sininhos 1,5 s), para o ruído não virar metralhadora.
4. Cada barulho toca uma pequena reação visual (cacos tilintando, sininho balançando, balde caindo), que é a confirmação para o jogador.

## Parâmetros tunáveis
Presets em `NoisyObjectRules.Preset` (valores [PROPOSTA]). Para uma peça especial, desligue `usePreset` no Inspector e ajuste `custom`: raio, volume, silencioso agachado, precisa de chão, velocidade mínima, intervalo, uma vez só.

## Leitura visual ([PROPOSTA], arte na Etapa 5)
- **Cacos de vidro:** 16×4 px por tile, brilhos brancos de 1 px (#e8f0ff) sobre o chão, perto de uma janela ou vitral quebrado (explica de onde vieram).
- **Folhas secas:** tapete 16×3 px em laranja-ferrugem (#a0582e / #6e3a22), só no exterior e no pátio.
- **Sininhos:** fio com 3 sininhos de latão (#b08a3a) na altura da cabeça do gato em pé (entre 15 e 22 px do chão), pendurados em portas da guarda.
- **Balde:** 10×10 px de madeira com aro de ferro, encostado em parede nas áreas de serviço.
- **Nada disso usa o vermelho das armadilhas:** não causam dano, só barulho.

## Onde colocar na Fortaleza
| Área | Objeto | Motivo no mundo |
|---|---|---|
| Trilha e pátio | folhas secas, palha | mata e estábulo |
| Galeria (alojamento) | sininhos na porta, balde | alarme improvisado dos templários; limpeza |
| Salão dos Votos | cacos de vidro | vitral rachado (primeiro sinal da praga) |
| Galeria das armadilhas | cacos de vidro | lamparinas quebradas pelos jatos de fogo |
| Fundos | balde, panela | área de serviço |

**Regra de level design:** sempre deixe um jeito de evitar (pular, agachar ou dar a volta). Ninguém é obrigado a fazer barulho num trecho furtivo.

## Implementação
- `Scripts/World/NoisyObject.cs`: componente (`NoisyObject`) + regras puras (`NoisyObjectRules`).
- Precisa de um `Collider2D` (vira trigger no `Awake`) numa layer que encosta no `Player` (ex.: `Interactable`).
- Testes: `Tests/EditMode/NoisyObjectRulesTests.cs`.

## Critérios de aceite
- **Dado** o gato andando agachado sobre cacos de vidro, **então** a Sentinela a até 10 tiles ouve.
- **Dado** o gato pulando por cima dos cacos, **então** nada acontece.
- **Dado** o gato andando agachado sobre folhas secas, **então** nada acontece; **correndo**, faz barulho.
- **Dado** o gato agachado sob os sininhos, **então** nada acontece; **de pé**, tocam.
- **Dado** um balde já derrubado, **quando** passar correndo de novo, **então** nada acontece.

## Perguntas abertas
- ~~O balde derrubado volta ao lugar ao descansar no santuário?~~ **Decidido (10/11/2026): sim**, junto com os inimigos (`NoisyObject.ResetState` no `GameEvents.WorldReset`).

## Implementado (Etapa 5A)
- **Arte:** `Tools/EnvironmentSprites/ObjectSpriteGen.cs` → `Art/Environments/Common/Noisy/env_com_obj_{cacos|folhas_secas|palha|sininhos|balde}_{0..n}.png` (0 = repouso; o resto = reação).
- **Na fase:** objetos `noisy` no `FortressLayout` (texto = tipo, `w` = largura em tiles dos tapetes); o `FortressSetup.Noisy` monta o trigger, o desenho (tapetes em modo Tiled) e o `NoisyObject.Setup`.
- **Sininhos:** uma passagem de madeira com 3 sininhos a 17–21 px do chão; o trigger fica só nessa altura.
- **Alarme da galeria (11/11/2026):** o fio de alarme esticado virou sininhos (`noisy` com `a: 16` → `NoisyObject.SetRadius(16)`): mesmo alcance do fio, mas com a regra dos sininhos (agachado passa por baixo). Os fios esticados continuam só nas armadilhas de flecha.
