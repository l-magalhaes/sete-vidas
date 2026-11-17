# Plano de teste — Floresta F4b: Vulpis, Caçador e a Chave do Templo (14/11/2026)

Antes:
1. Rode **Tools > Sete Vidas > Floresta: gerar as cenas**. Ele reconstrói o gato (dash, gancho, arco) e as cenas; o `ForestLayout` está na **versão 12** (D5, D6 e o Portão do Templo). Na primeira vez gera o retrato e o desenho provisórios da Vulpis, o portão e a classe Caçador (`ScriptableObjects/Classes/Hunter.asset`, `Combat/HunterBow.asset`).
2. Rode o **22** (mapa do mundo): há duas salas novas.
3. Comece do **S-F3 (Refúgio)** com a viagem rápida, ou pela cena `Forest_D_Refuge`.

## Testes automáticos

- EditMode > `ForestHunterTests` (9): dash, mira e puxão da corda, arco, 3º coração do Caçador, chave do templo.
- Rode também `ClassHeartTests`, `ForestBestiaryTests`, `ForestFoundationTests`, `ForestGrayboxTests` e o PlayMode inteiro (o gato ganhou componentes novos).
- Alcance: `Tools/LevelPreview/checar_floresta.ps1` → sem softlock (conferido). O baú **T01** aparece como "só com o Caçador" (o checador ainda não sabe dash e gancho).

## Roteiro manual

| # | Item | Passo | Esperado |
|---|---|---|---|
| 1 | Portão | No **D4 Mirante**, vá até a saída leste (para o Ninho) | **Portão de pedra verde com uma serpente** entalhada e olho turquesa; sólido. E: "Uma serpente entalhada. E uma fechadura do tamanho da minha cabeça." |
| 2 | Caminho | Vença a **arena do D3** e passe pelo portão da direita | Salinha do baú W13 com a **parede leste aberta** para a D5 |
| 3 | Vulpis | Na **D5**, fale com a raposa (E) | Conversa D-VULPIS-VINCULO (retrato provisório da raposa à direita) + continuação + 3 linhas de sistema |
| 4 | Vínculo | Depois da conversa | **+1 coração** (3 no total), o novo com a **flecha atravessada**; som de classe; a troca de classe (Q/R, LB/RB, 1–7) inclui o **Caçador** (ícone do arco) |
| 5 | Vínculo | Fale de novo | Falas em rodízio (ganchos nas ruínas; parede de pedra; "não mate a guardiã") |
| 6 | Arco | Com o Caçador, ataque (clique / X) | Flecha reta para a frente; o arco aparece erguido; uma flecha por vez (0,35 s) |
| 7 | Arco | {subir} + ataque; no ar também | Flecha para cima |
| 8 | Arco | Atire no boneco de palha e num morcego | Acerta e some; na parede, crava e some |
| 9 | Arco | No ar, {agachar} + ataque sobre espinhos | **Pogo** (estocada das adagas) continua funcionando com o Caçador |
| 10 | Dash | Em qualquer classe, Shift / RT parado e andando | Avança 6 tiles rápido para o lado apertado (ou para onde olha), com rastro claro |
| 11 | Dash | No ar: pule e dê dash; tente um 2º | Só um no ar; volta ao tocar o chão, agarrar parede/borda, pegar gancho ou quicar |
| 12 | Dash | Dash contra uma parede | Para encostado nela, sem atravessar |
| 13 | Gancho | Botão direito / Y apontando para um gancho a até 9 tiles | Corda sai, crava, **puxa o gato** até o gancho; fica pendurado ~0,4 s |
| 14 | Gancho | Pendurado: {pular}; de novo, pendurado: {dash} | Pulo cheio; ou dash (o dash do ar volta ao pegar o gancho) |
| 15 | Gancho | Sem gancho na mira | A corda vai até a parede e volta; o gato não para |
| 16 | Gancho | Mire em diagonal com um gancho ~20° fora da diagonal | A mira automática pega |
| 17 | D6 | Entre nas **Ruínas do Templo** pela D5 | Chão com 7 de espinhos: o pulo não passa; **dash** passa |
| 18 | D6 | Suba: gancho na ponta da laje 1 → pulo | Em cima da laje 1 |
| 19 | D6 | Da laje 1, pule e dê **dash no ar** para a esquerda | Alcança a laje 2 |
| 20 | D6 | Da laje 2, gancho no meio do vazio → **dash** para a direita | Alcança a laje 3 |
| 21 | D6 | Da laje 3, gancho na quina do nicho → pulo | No nicho, a **parede de ruínas** (musgo) bloqueia o baú |
| 22 | Ruínas | Adaga e flecha na parede | Não quebra; **marreta (Paladino)** quebra |
| 23 | Chave | Abra o baú T01 | "Chave do Templo da Serpente" em **turquesa**, brilho forte, fala do Lion "Pesada. E me olhando. Ótimo." |
| 24 | Chave | Inventário > Itens | Chave do templo com ícone **verde de serpente** e nome turquesa, diferente da chave da cabana (dourada) |
| 25 | Volta | Caia pela torre até a porta | Sem softlock; D5 → arena → D3 → D4 |
| 26 | Portão | E no portão com a chave | Abre ("O templo abriu..."), fica aberto para sempre; a chave some do inventário |
| 27 | Inventário | Abas Classes e Habilidades; lista de controles | Caçador descrito; **Dash** e **Flecha com corda** aparecem depois do vínculo; controles com dash e flecha com corda |
| 28 | Save | Descanse, saia e use Continuar | Caçador, 3 corações, chave/portão e parede quebrada continuam |

## Riscos conhecidos

- **Arte e som provisórios**: Vulpis, portão, flecha e corda são placeholders; sons emprestados (estocada, flecha, flecha_crava).
- **Ganchos antigos** (D3, E1–E3) já funcionam antes do Ninho: podem encurtar trechos do E. O `ForestReach` ainda não modela dash e gancho.
- **F8 (Isyathy)** foi pensada para 2 corações: agora o gato chega com 3.
- O dash não dá invencibilidade: atravessar um inimigo fere.
