# Diálogos do MVP

Limites: até ~90 caracteres por caixa; até 6 caixas por conversa obrigatória (testado automaticamente).
No jogo: interface de diálogo com retratos (`Docs/Design/dialogo.md`); E, Espaço ou clique avançam. Os textos ficam em `ScriptableObjects/Dialogue` (este arquivo é a referência do roteiro). Lore: `lore.md`.

## D-CONTRATO — Prólogo: a leitura do contrato (documento, página cheia)
Abre no Novo jogo, antes da Fortaleza. Pergaminho à luz de vela; E avança, Esc pula.
CONTRATO DE SERVIÇO
1. Ao Clã das Sombras, a Guilda dos Ladrões. Aos cuidados do melhor gatuno da casa.
2. Objeto: um tomo de capa roxa, guardado na Câmara do Selo da Cidadela da Ordem dos Paladinos Cães.
3. Sou colecionador de relíquias antigas. A Ordem esconde esse livro há cem anos e se recusa a vendê-lo, emprestá-lo ou sequer mostrá-lo a quem não late.
4. A Cidadela tem muralhas altas, sentinelas que não dormem, armadilhas em cada corredor e um paladino de guarda diante do selo.
5. Mandei soldados. Mandei mercenários. Mandei magos. Nenhum voltou. Só a Guilda dos Ladrões entra onde ninguém entra e sai sem que ninguém perceba. Por isso escrevo a vocês.
6. Pagamento: uma bolsa de ouro, entregue em mãos assim que o tomo deixar o pedestal.
7. *(letra miúda)* Cláusula 7: o portador assume, de livre vontade, todos os custos pessoais decorrentes do manuseio do objeto.
8. Assinado, O Contratante *(lacre de cera roxo com um "C" dourado)*
GATO (irônico): Uma bolsa de ouro por um livro velho. Colecionador, é? Gente rica tem cada mania.
GATO: Muralhas, sentinelas, armadilhas, um paladino... Ele tem razão: é trabalho para a Guilda.
GATO (irônico): Cláusula sete... letra miúda é coisa de advogado. Eu sou ladrão.
GATO: Entrar, pegar, sair. Esta noite.

> A cláusula 7 é a armadilha: "custos pessoais do manuseio" são as seis vidas. Na traição, a capivara cita a cláusula.

## D-FORT-01 — Chegada à Cidadela (obrigatório, uma vez; rev. 3)
SISTEMA: A Cidadela da Ordem, onde os cães viram templários e os templários, paladinos.
GATO (irônico): Muralhas, sentinelas, armadilhas. O contratante não exagerou.
GATO: Bom. Seria chato se fosse fácil.

## Placas de lore (opcionais, E para ler)
- **D-PLACA-01 (pátio):** "Pátio dos Escudeiros. Aqui o filhote vira templário, e o templário, paladino." / "Quem não aguenta o boneco de palha não aguenta a praga."
- **D-PLACA-02 (alojamento):** "Alojamento dos Templários. Silêncio depois do toque do sino." / "Arranhado embaixo, com letra torta: 'o Golden ronca'." / GATO (irônico): "Bom saber."
- **D-PLACA-03 (salão):** "Juramento da Ordem: guardar o selo. Servir o reino. Nunca abandonar um ferido." / GATO (irônico): "Bonito. Pena que eu não sou do reino."
- **D-PLACA-04 (câmara):** "Câmara do Selo. Há cem anos a Ordem prendeu aqui um tomo que bebe vidas." / "Que ninguém o desperte. Que nenhum guardião o abandone." / GATO: "...Ninguém falou nada de beber vidas."

## D-GOLDEN-00 — Entrada da câmara (obrigatório)
GOLDEN: Alto, ladrão. Esse tomo dorme selado há cem anos. E vai continuar dormindo.
GATO (irônico): Engraçado. Meu contratante jurou que ele estava bem acordado.
GOLDEN: Então volte e diga a ele que encontrou um paladino no caminho.

## D-GOLDEN-DERROTA — Golden vencido (obrigatório)
GOLDEN: Chega... Você é rápido, admito. Rápido demais para o próprio bem.
GOLDEN: Pegue, se precisa. Mas o selo cobra um preço que ninguém paga duas vezes.
GATO (irônico): Todo mundo diz isso. Ninguém cobra.

## D-TOMO-01 — Pegar o tomo (obrigatório; rev. 2)
SISTEMA: O tomo é pesado e frio. As runas acendem sob as suas patas.
GATO: ...Ele está me olhando?
SISTEMA: O selo se rompe com um estalo seco. Algo puxa o ar dos seus pulmões.
[AÇÃO: clarão roxo; os corações se apagam um a um, de 7 até 1; o gato cai]

## D-TOMO-02 — A maldição (obrigatório; rev. 2)
SISTEMA: Uma a uma, seis das suas sete vidas se apagam.
GATO: Não... não, não, não. Isso não estava no contrato.
SISTEMA: Resta uma última centelha. E ela está se apagando.
[AÇÃO: clarão roxo; o Contratante surge ao lado do gato]

## D-CAPIVARA-01 — A revelação (obrigatório)
CONTRATANTE: Ah, estava sim. Cláusula sete. Você leu... e achou graça.
GATO: Você... o contratante. O que você fez comigo?
CONTRATANTE: Eu? Nada. O tomo dorme há cem anos e só acorda bebendo vidas.
CONTRATANTE: Sete vidas de gato. Raras. Deliciosas. Exatamente o que ele precisava.
GATO: Você me usou... desde o começo.
CONTRATANTE: Como se usa uma chave, gatinho. E chaves não reclamam da porta.
[AÇÃO: ele toma o tomo; uma moeda de ouro é jogada e quica ao lado do gato]

## D-CAPIVARA-02 — O pagamento (obrigatório)
SISTEMA: Uma moeda de ouro tilinta no chão, ao lado do seu focinho.
CONTRATANTE: O pagamento está feito pelos seus serviços. Não diga que não sou honesto.
CONTRATANTE: Adeus. Aproveite a sua última vida. Ela não vai durar.
SISTEMA: Ele some numa névoa roxa, levando o tomo. Só a moeda fica para trás.
[AÇÃO: clarão roxo; a tela escurece; o Golden chega]

## D-GOLDEN-01 — O resgate (obrigatório; rev. 3)
GOLDEN: Ladrãozinho! Aguente firme. Seu último coração ainda bate... por pouco.
GATO (irônico): Ótimo. Agora o cão de lata vai me dar sermão antes de eu morrer.
GOLDEN: Sermão nenhum. Quem fez isso? Quem levou o tomo?
GATO: O contratante. Uma capivara. Me pagou com uma moeda... e com seis vidas.
GOLDEN: Com o tomo desperto, a praga vai se espalhar. Sozinho, não chego a tempo.
GOLDEN: Jure me ajudar a recuperar o tomo e destruí-lo. Em troca, minha força é sua.

## D-GOLDEN-01b — O juramento (obrigatório)
GATO: Destruir o tomo... tanto faz. Eu quero a capivara.
GATO: Ninguém me usa como chave e me paga com troco. Ninguém.
GOLDEN: Vingança é um começo ruim. Mas é um começo. Jure, e levante.
GATO: Eu juro. Pelo tomo, pela moeda... e pelas seis vidas que ele me tirou.
[AÇÃO: um coração acende no HUD]
SISTEMA (D-GOLDEN-01-SISTEMA): Paladino desbloqueado! Aperte 2 (ou Q/R) para trocar de classe.

## D-GOLDEN-02 — Conversas seguintes (opcional, uma por vez, em rodízio)
- 02a — GOLDEN: Paredes rachadas não resistem a um golpe honesto.
- 02b (rev. 3) — GOLDEN: Onde aquela capivara for, a praga vai atrás. Vamos alcançá-la. / GATO (irônico): E devolver a moeda. Enfiada onde ela não gosta.
- 02c — GOLDEN: Força não substitui cuidado. Volte inteiro, ladrãozinho. / GATO (irônico): Inteiro é o que eu menos estou.

## D-FIM — Saída da Cidadela (fim da demonstração; rev. 2)
SISTEMA: Você deixa a Cidadela sem o tomo. Com uma moeda de ouro e um juramento.
GATO: Uma moeda. Seis vidas. Vou cobrar o troco com juros, capivara.
SISTEMA: Fim da demonstração. Obrigado por jogar!

## Ficha de voz (resumo)
| Personagem | Voz | Nunca diria |
|---|---|---|
| Gato | Cínico, irônico, frases curtas. Na maldição, a ironia quebra; no juramento, vira raiva fria | Discursos sobre honra |
| Golden | Calmo, caloroso, solene. "Ladrão" antes da luta; "ladrãozinho" depois do resgate | Sarcasmo, ameaças |
| Contratante | Lento, educado, debochado. "Gatinho", "chave". Crueldade gentil | Gritos, ameaças diretas |
