# Diálogos do MVP

Limites: até ~90 caracteres por caixa; até 6 caixas por conversa obrigatória (testado automaticamente).
No jogo: interface de diálogo com retratos (`Docs/Design/dialogo.md`); E, Espaço ou clique avançam. Os textos ficam em `ScriptableObjects/Dialogue` (este arquivo é a referência do roteiro). Lore: `lore.md`.

## D-CONTRATO — Prólogo: a leitura do contrato (documento, página cheia)
Abre no Novo jogo, antes da Fortaleza. Pergaminho à luz de vela; E avança, Esc pula.
CONTRATO DE SERVIÇO
1. À Guilda das Nove Sombras, em Miaravéu. Aos cuidados do melhor gatuno da casa.
2. Objeto: um tomo de capa roxa, guardado na Câmara do Selo da Cidadela do Sol Eterno, sede da Ordem da Presa Dourada.
3. Sou colecionador de relíquias antigas. A Ordem esconde esse livro há cinco séculos e se recusa a vendê-lo, emprestá-lo ou sequer mostrá-lo a quem não late.
4. A Cidadela tem muralhas altas, sentinelas que não dormem, armadilhas em cada corredor e um paladino de guarda diante do selo.
5. Mandei soldados. Mandei mercenários. Mandei magos. Nenhum voltou. Só a Guilda das Nove Sombras entra onde ninguém entra e sai sem que ninguém perceba. Por isso escrevo a vocês.
6. Pagamento: uma bolsa de ouro, entregue em mãos assim que o tomo deixar o pedestal.
7. *(letra miúda)* Cláusula 7: o portador assume, de livre vontade, todos os custos pessoais decorrentes do manuseio do objeto.
8. Assinado, O Contratante *(lacre de cera roxo com um "C" dourado)*
GATO (irônico): Uma bolsa de ouro por um livro velho. Colecionador, é? Gente rica tem cada mania.
GATO: Muralhas, sentinelas, armadilhas, um paladino... Ele tem razão: é trabalho para a Guilda.
GATO (irônico): Cláusula sete... letra miúda é coisa de advogado. Eu sou ladrão.
GATO: Entrar, pegar, sair. Esta noite.

> A cláusula 7 é a armadilha: "custos pessoais do manuseio" são as seis vidas. Na traição, a capivara cita a cláusula.

## D-FORT-01 — No alto da montanha (obrigatório, uma vez; rev. 5)
SISTEMA: Do alto da montanha, a Cidadela do Sol Eterno brilha no pôr do sol.
GATO (irônico): Muralhas, sentinelas, armadilhas. O contratante não exagerou.
GATO: Caminho mais curto até lá? Para baixo.
[AÇÃO: placa "salto de fé: corra e pule"; o gato cai 60 tiles ao lado da cachoeira]

## D-FORT-02 — A lagoa (obrigatório, uma vez, ao cair na água)
GATO (irônico): Água. Claro que tinha que ser água.
GATO: Caí de pé, pelo menos. Como sempre.

## Placas de lore (opcionais, E para ler)
- **D-PLACA-01 (pátio):** "Pátio dos Escudeiros. Aqui o filhote vira templário, e o templário, paladino." / "Quem não aguenta o boneco de palha não aguenta a praga."
- **D-PLACA-02 (alojamento):** "Alojamento dos Templários. Silêncio depois do toque do sino." / "Arranhado embaixo, com letra torta: 'o Koda ronca'." / GATO (irônico): "Bom saber."
- **D-PLACA-03 (salão):** "Juramento da Ordem: guardar o selo. Servir o reino. Nunca abandonar um ferido." / GATO (irônico): "Bonito. Pena que eu não sou do reino."
- **D-PLACA-04 (câmara):** "Câmara do Selo. Há cinco séculos, Golden, o Dourado, prendeu aqui um tomo que bebe vidas." / "Que ninguém o desperte. Que nenhum guardião o abandone." / GATO: "...Ninguém falou nada de beber vidas."
- **D-PLACA-05 (estábulo, atrás da porta rachada do pátio):** "Estábulo da Ordem. Aviso de Soluki: só cães passam deste portão." / GATO (irônico): "Ótimo. Eu nem sou de passar por portões."
- **D-PLACA-06 (cofre do guardião, passagem dos fundos):** "Diário do Guardião do Selo: o tomo voltou a sussurrar à noite. Pedi reforços." / "A resposta de Soluki chegou hoje. Uma palavra: 'Superstição.'" / GATO: "...O cão de lata avisou. Ninguém ouviu."
- **D-PLACA-07 (cofre do patamar 72):** "Mural da Marcha Dourada: quarenta dias subindo os salões de Aetherion." / "À frente, Golden, o Dourado. No fim, entre os escombros, um livro que sussurrava."
- **D-PLACA-08 (cofre do patamar 43):** "Pedra antiga dos votos: 'Guardar o selo. Servir os vivos. Nunca abandonar um ferido.'" / "Alguém raspou 'os vivos' e gravou 'o reino' por cima." / GATO (irônico): "Engraçado. Os cães também têm letra miúda." **[PROPOSTA, ligada ao segredo do Koda]**
- **D-PLACA-09 (cofre do patamar 15):** "Guardiões do Selo: Golden, o Dourado. Depois dele, cinco séculos de Dourados." / "O último nome da lista ainda tem a tinta fresca: Koda."
- **D-PLACA-10 (marco da trilha):** "Marco da estrada: Cidadela do Sol Eterno, adiante. Miaravéu, três dias para trás." / GATO: "Três dias de volta. Com o livro, dois."

## D-GOLDEN-00 — Entrada da câmara (obrigatório)
KODA: Alto, ladrão. Esse tomo dorme selado há cinco séculos. E vai continuar dormindo.
GATO (irônico): Engraçado. Meu contratante jurou que ele estava bem acordado.
KODA: Então volte e diga a ele que encontrou um paladino no caminho.

## D-GOLDEN-DERROTA — Koda vencido (obrigatório)
KODA: Chega... Você é rápido, admito. Rápido demais para o próprio bem.
KODA: Pegue, se precisa. Mas o selo cobra um preço que ninguém paga duas vezes.
GATO (irônico): Todo mundo diz isso. Ninguém cobra.

## D-TOMO-01 — Pegar o tomo (obrigatório; rev. 2)
SISTEMA: O tomo é pesado e frio. As runas acendem sob as suas patas.
GATO: ...Ele está me olhando?
SISTEMA: O selo se rompe com um estalo seco. Algo puxa o ar dos seus pulmões.
[AÇÃO: clarão roxo; os corações se apagam um a um, de 7 até 1; o gato cai]

## D-KODA-SELO — Koda segura a última vida (obrigatório; novo)
SISTEMA: A última centelha vacila. O roxo avança sobre ela.
KODA: Não! Nenhuma morte reclama o que ainda pertence à vida!
[AÇÃO: Koda, de joelhos, ergue a marreta e a crava no chão; clarão dourado. A maldição para]

## D-TOMO-02 — A maldição (obrigatório; rev. 3)
SISTEMA: Um clarão dourado detém a maldição. O paladino tomba, sem forças.
SISTEMA: Seis das suas sete vidas se foram. Resta uma última centelha.
GATO: Não... não, não, não. Isso não estava no contrato.
[AÇÃO: clarão roxo; o Contratante surge ao lado do gato]

## D-CAPIVARA-01 — A revelação (obrigatório)
CONTRATANTE: Ah, estava sim. Cláusula sete. Você leu... e achou graça.
GATO: Você... o contratante. O que você fez comigo?
CONTRATANTE: Eu? Nada. O tomo dorme há cinco séculos e só acorda bebendo vidas.
CONTRATANTE: Sete vidas de gato. Raras. Deliciosas. Exatamente o que ele precisava.
GATO: Você me usou... desde o começo.
CONTRATANTE: Como se usa uma chave, gatinho. E chaves não reclamam da porta.
[AÇÃO: ele toma o tomo; uma moeda de ouro é jogada e quica ao lado do gato]

## D-CAPIVARA-02 — O pagamento (obrigatório)
SISTEMA: Uma moeda de ouro tilinta no chão, ao lado do seu focinho.
CONTRATANTE: O pagamento está feito pelos seus serviços. Não diga que não sou honesto.
CONTRATANTE: Adeus. Aproveite a sua última vida. Ela não vai durar.
SISTEMA: Ele some numa névoa roxa, levando o tomo. Só a moeda fica para trás.
[AÇÃO: clarão roxo; a tela escurece; o Koda chega]

## D-MOEDA-01 — A moeda do Contratante (obrigatório, uma vez, ao pegar a moeda depois do juramento)
SISTEMA: Uma moeda de ouro pesada, cunhada com um olho roxo. Vale 100 moedas comuns.
GATO (irônico): Cem moedas por seis vidas. Já fiz negócios piores. Não lembro quando.
GATO: Vou guardar. Um dia eu devolvo... com juros.

## D-GOLDEN-01 — O resgate (obrigatório; rev. 3)
KODA: Ladrãozinho! Aguente firme. Seu último coração ainda bate... por pouco.
GATO (irônico): Ótimo. Agora o cão de lata vai me dar sermão antes de eu morrer.
KODA: Sermão nenhum. Quem fez isso? Quem levou o tomo?
GATO: O contratante. Uma capivara. Me pagou com uma moeda... e com seis vidas.
KODA: Com o tomo desperto, a praga vai se espalhar. Sozinho, não chego a tempo.
KODA: Jure me ajudar a recuperar o tomo e destruí-lo. Em troca, minha força é sua.

## D-GOLDEN-01b — O juramento (obrigatório)
GATO: Destruir o tomo... tanto faz. Eu quero a capivara.
GATO: Ninguém me usa como chave e me paga com troco. Ninguém.
KODA: Vingança é um começo ruim. Mas é um começo. Jure, e levante.
GATO: Eu juro. Pelo tomo, pela moeda... e pelas seis vidas que ele me tirou.
[AÇÃO: um coração acende no HUD]
SISTEMA (D-GOLDEN-01-SISTEMA): Paladino desbloqueado! Aperte 2 (ou Q/R) para trocar de classe.

## D-GOLDEN-02 — Conversas seguintes (opcional, uma por vez, em rodízio)
- 02a — KODA: Paredes rachadas não resistem a um golpe honesto.
- 02b (rev. 3) — KODA: Onde aquela capivara for, a praga vai atrás. Vamos alcançá-la. / GATO (irônico): E devolver a moeda. Enfiada onde ela não gosta.
- 02c — KODA: Força não substitui cuidado. Volte inteiro, ladrãozinho. / GATO (irônico): Inteiro é o que eu menos estou.

## D-FIM — Saída da Cidadela (fim da demonstração; rev. 2)
SISTEMA: Você deixa a Cidadela sem o tomo. Com uma moeda de ouro e um juramento.
GATO: Uma moeda. Seis vidas. Vou cobrar o troco com juros, capivara.
SISTEMA: Fim da demonstração. Obrigado por jogar!

## Ficha de voz (resumo)
| Personagem | Voz | Nunca diria |
|---|---|---|
| Gato | Cínico, irônico, frases curtas. Na maldição, a ironia quebra; no juramento, vira raiva fria | Discursos sobre honra |
| Koda | Calmo, caloroso, solene. "Ladrão" antes da luta; "ladrãozinho" depois do resgate | Sarcasmo, ameaças |
| Contratante | Lento, educado, debochado. "Gatinho", "chave". Crueldade gentil | Gritos, ameaças diretas |
