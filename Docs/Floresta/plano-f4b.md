# F4b — Vulpis, Caçador e a Chave do Templo: plano

> Pedido do usuário (14/11/2026), antes da F5. Status: **[APROVADO]** (as 4 decisões do §8 foram aprovadas em 14/11/2026: todas como propostas). Números são ponto de partida para playtest.

## 1. O que o usuário pediu

1. A passagem do **Refúgio (D)** para o **Ninho (E)** fica **trancada com uma chave**.
2. Depois da **arena do D3**, uma **sala nova** onde o gato encontra a **Vulpis**.
3. A Vulpis conta que **observa o gato pela floresta** e precisa de ajuda para deter a **serpente infectada que espalha a praga**; para chegar a ela é preciso a **chave do templo da serpente, perdida há muitos anos**.
4. A Vulpis dá a classe dela: **+1 coração (o coração do Caçador)** e a classe **Caçador**.
5. Habilidades do Caçador: **arco e flecha** (tiro à distância), **flecha com corda** (gancho, como o *hookshot* de Ocarina of Time) e **dash** (no chão e no ar, distância generosa).
6. Um **trecho de plataforma** que pede **dash e gancho**; no fim, a chave num **baú protegido por uma parede de pedras de ruínas antigas**.
7. A **chave da cabana** da Vulpis e a **chave do templo** precisam ser **diferentes** à vista.

## 2. Como fica o caminho

```
C6 ──► D1 Trilha ──► D2 Resgate ──► D3 Refúgio (cabana, S-F3)
                                       │
                                  arena D3 ──► salinha do baú W13
                                                   │ (parede leste abre depois da arena)
                                                   ▼
                                  D5 Posto da Vulpis   ◄── vínculo: +1 coração, Caçador
                                                   │
                                                   ▼
                                  D6 Ruínas do Templo  (plataforma: dash + gancho)
                                     └─ baú da Chave do Templo, atrás da parede de ruínas
                                     └─ atalho de volta (cipó/laje que cai) para o D3 ou D4
                                                   │
D3 (saída ↑) ──► D4 Mirante ──► [Portão do Templo, trancado] ──► E1 Sulcos
```

- **A tranca** fica na saída do **D4 para a E1** (é a única passagem física do D para o E). Vira o **Portão do Templo da Serpente**: arco de pedra com a serpente entalhada.
- **Sala da imagem** (salinha à direita da arena, com o baú W13): a **parede leste** ganha uma passagem para a sala nova. Só se chega lá depois de vencer a arena (o portão já abre com `arena_vencida_D3_arena`).
- **Espaço no mapa:** a leste do D3 (x ≈ 291–333, y ≈ −116 a −74) está livre; D5 e D6 cabem ali sem encostar no C3/C4. Confirmar com `checar_floresta.ps1` (salas sobrepostas).

## 3. Salas novas (level design)

Seguindo as diretrizes da Floresta: salas orgânicas, mistura de mecânicas, atalho físico de volta.

### D5 — Posto da Vulpis (≈ 30 × 17, respiro)
- Galho largo alto, fogueira apagada, flechas cravadas, alvos de treino de palha (ensinam o arco sem risco).
- **Vulpis** no chão, ao lado da fogueira (NPC com `CompanionBond`). Santuário? **Não**: o S-F3 fica a poucos passos (D3).
- Depois do vínculo, **três lições curtas** na própria sala, cada uma com uma placa:
  1. **Arco:** derrubar uma colmeia/fruto do teto que abre a passagem (ou acertar os alvos).
  2. **Dash:** um vão de 6 tiles que o pulo não vence.
  3. **Gancho:** um gancho alto acima da saída para a D6.

### D6 — Ruínas do Templo (≈ 45 × 40, desafio)
- Ruínas dos Primatas engolidas pela árvore: colunas quebradas, escadarias tortas, raízes.
- **Ritmo** (cada trecho apresenta, depois mistura):
  1. Dash no chão sobre espinhos (vão de 6).
  2. Gancho → gancho (subida vertical; o gato pendura e pula).
  3. **Dash no ar depois do gancho** (soltar do gancho e cruzar um vão de espinhos).
  4. Galho podre que cai + gancho logo acima (decidir rápido).
  5. Inimigos do lugar: morcegos (o arco os derruba de longe), uma planta na parede no meio de uma subida.
- **Baú da Chave do Templo** num nicho, atrás de uma **parede de pedras de ruínas** (rachada, com pedras cinza-esverdeadas e musgo, diferente do tronco rachado). Proposta: **a marreta do Paladino quebra** (reusa o `BreakableWall`, material "pedra"), obrigando a trocar de classe no fim. Ver decisão em §8.
- **Atalho de volta** (Dark Souls): do nicho, uma laje solta ou um cipó desce até o D4 (perto do Portão), aberto só deste lado. O gato não refaz o desafio para voltar.

### Revisão do que já existe
- **Ganchos já postos** (D3 e 20 em E1–E3, pensados "para a volta com o Caçador"): agora o Caçador chega **antes** do E. Revisar cada um: viram rota alternativa para a frente ou continuam segredo (baús).
- **Baú W15** (D3, marcado "falta: Arqueiro"): passa a ser alcançável depois da D5.
- **ForestReach** passa a saber dash e gancho (só depois da flag do vínculo), para conferir alcance e softlock antes e depois do Caçador.

## 4. As duas chaves

| | Chave da cabana | Chave do Templo da Serpente |
|---|---|---|
| Id / flag | `cabana` / `chave_cabana` (já existe) | `templo` / `chave_templo` |
| Porta | `cabana_vulpis` (porta de madeira) | `portao_templo` (Portão do Templo, D4 → E1) |
| Desenho no chão | pequena, **ferro escuro**, argola redonda, um dente; brilho dourado fraco | **grande (2× maior)**, **pedra verde-musgo** com a **cabeça de serpente** na argola, olho **turquesa** que pulsa; partículas turquesa |
| Ícone no inventário | ícone atual | ícone novo (cabeça de serpente) |
| Ao pegar | "Chave da cabana" (dourado) | "Chave do Templo da Serpente" (turquesa) + fanfarra curta + fala do Lion |
| Porta sem a chave | Lion: fala atual | Lion: "Uma serpente entalhada. E uma fechadura do tamanho da minha cabeça." |

Código: `KeyRules` ganha o nome e o `DoorOfKey` da chave nova; `KeyPickup` e `LockedDoor` ganham o **visual por chave** (sprites e cor do brilho); o **baú** (`WoodChest`) passa a poder guardar uma chave (hoje guarda madeira e moedas).

## 5. A Vulpis e o vínculo

- **NPC Vulpis** com `CompanionBond` (o mesmo do Golden): `grantsClass = Hunter`, `bondFlag = vinculo_vulpis`, conversa de vínculo, dica da troca de classe e falas em rodízio.
- **Diálogo novo** (texto final na skill de narrativa, `floresta-roteiro.md`), batidas:
  1. Ela já o observava: "Desde as raízes. Um gato que cai de pontes não passa despercebido."
  2. A praga chegou na **Isyathy**, a guardiã; dela a infecção corre pelas raízes para a floresta toda.
  3. O caminho para o ninho passa pelo **Templo da Serpente**, fechado. A chave se perdeu **há muitos anos**, nas ruínas atrás do posto.
  4. Ela não pode ir: precisa vigiar o ninho de cima. Dá ao gato o **arco**, a **flecha com corda** e ensina o **avanço** (dash). O Lion responde com humor.
  5. Vínculo: **+1 coração** e classe **Caçador**. Linha de sistema com a troca de classe.
- **Coerência com o resto do roteiro:**
  - **D2 O Resgate** (cutscene da F6: a flecha que salva o gato): proposta é **manter** como o primeiro relance dela, que some no alto ("Venha. Antes que o cheiro de gato acorde a floresta inteira."). A conversa de verdade passa para a D5. Ver §8.
  - D-VULPIS-02 (na cabana) é reescrita para não repetir o que a D5 conta; as falas 03a–03d continuam.
  - F8: a Vulpis continua ajudando contra a Isyathy (`ArcherSupport`).

## 6. Classe Caçador

### 6.1 Coração do Caçador
- `heartsRecovered` passa de 2 para **3**. O desenho do coração do Caçador (flecha atravessada) já existe em `ClassHeartPixels`.
- **Ajuste necessário:** hoje o coração *n* mostra a classe *n* da lista (o 3º seria o do Mago). Passa a mostrar **na ordem em que as classes foram ganhas** (Ladino, Paladino, Caçador...).
- **Ordem das classes:** a lista do GDD não é uma ordem (o usuário define a ordem durante o desenvolvimento); o Caçador é a **3ª** ganha. O enum não muda (o save guarda o nome). A F8 deve ser balanceada com **3 corações**, não 2.

### 6.2 Habilidades

| Habilidade | Botão (proposta) | Como funciona | Números iniciais |
|---|---|---|---|
| **Arco** | Ataque (com o Caçador) | Flecha reta para a frente; {cima} + ataque atira para cima; no ar também. Segurar não carrega (por enquanto). Acerta inimigos e alvos (interruptores, frutos, colmeias). Barulho baixo (furtividade) | dano 1 (× bônus dos corações), velocidade 18 t/s, alcance 12 t, recarga 0,35 s, 1 flecha no ar por vez |
| **Flecha com corda** (gancho) | **botão direito** / **Y** (ação Special; Q e RB já trocam de classe) | Dispara na direção do direcional (8 direções; parado = para a frente) **com mira automática** no gancho mais próximo num cone de 30°. Pegou num gancho (`GrappleAnchor`) ou alvo de madeira: **puxa o gato até lá**; chegando, fica pendurado 0,4 s e pode pular (pulo cheio) ou dar dash. Em parede comum: a flecha ricocheteia e a corda volta | alcance 9 t, puxão 20 t/s, cancela com pulo |
| **Dash** | **Shift** / **RT** | Avanço horizontal **no chão e no ar**, sem gravidade durante o dash. **Um no ar** por vez: recarrega ao tocar o chão, agarrar a parede, chegar num gancho ou quicar num pogo. Sem invencibilidade (fica para um upgrade) | 6 tiles em 0,2 s (generoso), recarga no chão 0,3 s |

- **Em qual classe vale** (GDD §"decisão arquitetural": locomoção vale em qualquer classe): proposta é **dash e gancho em qualquer classe** depois do vínculo; o **arco só com o Caçador**. Assim o trecho da D6 termina trocando para o Paladino na parede de ruínas sem perder o movimento. Ver §8.
- **Arquitetura:** `DashAbility` e `GrappleAbility` como `PlayerAbility` (como a `WallClimbAbility`), ligadas pela flag `vinculo_vulpis`; `BowAbility` como `ClassAbility` (forClass = Hunter); `PlayerArrow` (projétil do gato, à parte do `EnemyBolt`); regras puras em `DashRules`, `GrappleRules`, `BowRules`. `GrappleAnchor` deixa de ser só marcador.
- **Entrada:** ações novas `Dash` e `Grapple` no `InputSystem_Actions`, glifos no `InputGlyphs`/`InputPrompts`, linha nos controles na tela (`controles-na-tela.md`).
- **Inventário:** aba de classes mostra o Caçador e as três habilidades; o Diário ganha a entrada da Vulpis.

## 7. Etapas

| # | Etapa | Entrega | Pronto quando |
|---|---|---|---|
| 1 | **Design** (skill game design) | ficha do Caçador em `Docs/Design/classe-cacador.md`: números do §6, regras de recarga, interação com pogo, escalada, furtividade e inimigos | aprovada |
| 2 | **Habilidades** | `DashRules`/`GrappleRules`/`BowRules` + testes EditMode; `DashAbility`, `GrappleAbility`, `BowAbility`, `PlayerArrow`; ações de entrada; placeholders (corda, flecha, rastro do dash); sons provisórios | dá para testar as três numa cena de teste (menu 20, sala nova "Forest_Test_D") |
| 3 | **Classe e coração** | `ClassDefinition` do Caçador; coração na ordem de ganho (`HeartsHud.HeartClass`); inventário; save | vínculo dá o 3º coração com a flecha e a troca de classe inclui o Caçador |
| 4 | **Chaves** | `templo` em `KeyRules`; visuais distintos de chave e porta; baú com chave; Portão do Templo | as duas chaves são reconhecíveis à primeira vista; o portão só abre com a do templo |
| 5 | **Level** | D5 e D6 no `ForestLayout` (Version 12), passagem leste da salinha W13, Portão no D4, atalho da D6; `ForestReach` com dash e gancho; revisão dos ganchos de E1–E3 e do W15 | `checar_floresta.ps1` sem softlock, nas duas situações (antes e depois do vínculo); `sala_ascii` mostra escolha e mistura |
| 6 | **Narrativa** | diálogo do vínculo, falas do Lion (portão, chave, primeiro dash/gancho), placas das lições, entrada do Diário; revisão de D-VULPIS-01/02 | textos no `floresta-roteiro.md` e nos `DialogueDefinition` |
| 7 | **Arte e áudio provisórios** | Vulpis placeholder (parada, falando), retrato com 2 expressões, chave do templo, portão, parede de ruínas, alvos de palha; sons de arco, corda, dash, chave do templo | tudo legível no graybox |
| 8 | **QA** | `Docs/QA/floresta-f4b-plano-de-teste.md` | roteiro passado no Unity |

Ordem sugerida: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. A 2 é a maior (três habilidades novas); a 5 depende dela para medir os vãos.

## 8. Decisões (aprovadas pelo usuário em 14/11/2026: sim às 4)

1. **Dash e gancho em qualquer classe** (arco só no Caçador), como diz o GDD para a locomoção? *Proposta: sim.*
2. **Parede de ruínas:** quebra com a **marreta do Paladino** (troca de classe no fim do desafio) ou com algo do Caçador (flecha num ponto fraco que derruba as pedras)? *Proposta: marreta.*
3. **D2 O Resgate:** manter a cutscene da flecha como primeiro relance da Vulpis (ela some e o encontro de verdade é na D5)? *Proposta: manter.*
4. **Tranca:** na passagem D4 → E1 (o Portão do Templo). *Proposta: sim (é a única ligação física do D com o E).*

## 9. Riscos

- **Três habilidades de uma vez** mexem no `PlayerController` (estados de dash e de puxão do gancho convivendo com escalada, pogo, transição de sala e dano). Testes PlayMode para dash + escalada e gancho + transição.
- **Ganchos antigos** ficam usáveis cedo: podem abrir atalhos que pulam trechos do E. O `ForestReach` com as habilidades acusa.
- **Isyathy (F8)** foi pensada para 2 corações; com 3, rever o dano ou o número de golpes.
- **Mapa de regiões do GDD:** conferir o "Desfiladeiro dos Caçadores" (a Vulpis agora está na Floresta). A lista de classes do GDD não é ordem: não precisa mudar.

## 10. Estado (14/11/2026): implementado, falta testar no Unity

Roteiro: `Docs/QA/floresta-f4b-plano-de-teste.md`. Ficha: `Docs/Design/classe-cacador.md`.

- **Habilidades:** `HunterRules`, `DashAbility` (Shift / RT), `GrappleAbility` (botão direito / Y, ação Special), `BowAbility` + `PlayerArrow` (arma `HunterBow` com `Ranged`; o pogo das adagas continua). `GrappleAnchor` virou o alvo da corda.
- **Classe e coração:** `Hunter.asset` (ícone do arco, arco nas costas); o gato tem as três habilidades (menu 21 reconstrói o prefab); corações na ordem de ganho (`HeartsHud.HeartClass`).
- **Vulpis:** NPC com `CompanionBond` (flag `vinculo_vulpis`), retrato e desenho provisórios, diálogos `D-VULPIS-VINCULO`.
- **Chaves:** `KeyRules.TempleKey`/`TempleGate`; baú com chave (`WoodChest.SetKey`); aviso turquesa, ícone de serpente e textos próprios; Portão do Templo de pedra verde no D4.
- **Salas:** D5 Posto da Vulpis (20 × 30) e D6 Ruínas do Templo (24 × 30, torre com dash e gancho), `ForestLayout.Version` 12. A D5 ficou com 30 de altura para não sobrar telhado onde o checador via softlock. Voltar da D6 é cair pela torre.
- **Falta:** o `ForestReach` conhecer dash e gancho; revisar os ganchos de E1–E3; arte e som de verdade; lições com placas na D5.
