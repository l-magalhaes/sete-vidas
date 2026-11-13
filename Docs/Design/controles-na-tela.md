# Comandos na tela (controle de Xbox × teclado e mouse)

> Pedido do usuário (12/11/2026). Status: **[PROPOSTA]**.

## Regra
- O jogo mostra os comandos do **último aparelho usado** (`InputDevices`): apertar um botão, gatilho ou mexer um
  analógico/direcional do controle → **controle de Xbox**; apertar uma tecla, clicar ou rolar o mouse → **teclado e
  mouse**. Ao abrir o jogo, começa no controle se houver um ligado. Desligar o controle volta para o teclado.
- Tudo se redesenha na hora da troca.

## Onde aparece
| Lugar | O que mostra |
|---|---|
| Avisos no mundo (`WorldPrompt`) | [botão] + verbo na fonte em pixel: falar, ler, descansar, pegar o tomo (E / B) |
| Dicas do tutorial (`TutorialScroll`) | o ícone da tecla/botão no pergaminho |
| Inventário | botões das abas (Q/E · LB/RB), rodapé ([setas/direcional] escolher · [Enter/A] equipar · [Esc/B] fechar), "[A] equipar" no detalhe, descrições com o botão certo (ex.: "No ar, S + clique" / "No ar, ↓ + X") |
| Inventário → aba **Controles** (nova, a 5ª) | a lista de comandos do aparelho em uso (só o que o gato já sabe fazer; trocar de classe e curar depois do Koda) e qual aparelho está em uso |
| Prólogo | [Enter/A] continuar · [Esc/Menu] pular |
| Diálogos | as marcas `{trocar_classe}`, `{curar}`, `{pular}`, `{agachar}`, `{subir}`, `{atacar}`, `{interagir}`, `{dash}`, `{inventario}` viram o nome do botão |

## Mapa (a mesma tabela do GameControls)
| Ação | Teclado e mouse | Controle de Xbox |
|---|---|---|
| Andar | A/D (setas) | analógico esquerdo (direcional) |
| Pular | Espaço | A |
| Agachar | S | ↓ |
| Escalar | W | ↑ |
| Atacar | clique | X |
| Interagir | E | B |
| Curar | F | LT |
| Dash | Shift | RT |
| Trocar de classe | Q / R (ou 1–7) | LB / RB |
| Inventário | Tab (ou I) | Menu (ou Exibir) |
| Confirmar | Enter | A |
| Voltar / fechar | Esc | B |
| Abas do inventário | Q / E | LB / RB |
| Pular o prólogo | Esc | Menu |

Quando o gato está na cena, os nomes vêm do próprio GameControls (se um botão mudar lá, a tela acompanha).

## Ícones (`InputGlyphs`, desenhados em código, 11 px de altura, contorno #140f18)
- Controle: A verde #5cb03c, B vermelho #d8483c, X azul #3a78d0, Y amarelo #e8b830 (círculos com a letra clara);
  LB/RB pílula escura; LT/RT gatilho; Menu (≡); Exibir; analógico com a seta da direção; direcional com o braço aceso.
- Teclado: tecla clara #e8e0d0 com bisel #a89c88 e o nome dentro (E, ESPAÇO, SHIFT, TAB, ESC, ↑...).
- Mouse: com o botão aceso em dourado (#ffe08a).

## Testes
`InputPromptsTests` (nomes, marcas dos textos, tamanho dos ícones).
