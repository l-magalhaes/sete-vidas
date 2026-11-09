# Diálogos do MVP (placeholder)

Limites: até ~90 caracteres e 2 linhas por caixa; até 6 caixas por conversa obrigatória.
No jogo: interface de diálogo com retratos (Docs/Design/dialogo.md); **E**, Espaço ou clique avançam. Os textos ficam em `ScriptableObjects/Dialogue` (edite lá; este arquivo é a referência do roteiro).

## D-GOLDEN-00 — Entrada da arena (obrigatório)
GOLDEN: Alto, ladrão. Esse tomo dorme selado há cem anos. E vai continuar dormindo.
GATO (irônico): Engraçado. Meu contratante jurou que ele estava bem acordado.
GOLDEN: Então volte e diga a ele que encontrou um paladino no caminho.

## D-GOLDEN-DERROTA — Golden vencido (obrigatório)
GOLDEN: Chega... Você é rápido, admito. Rápido demais para o próprio bem.
GOLDEN: Pegue, se precisa. Mas o selo cobra um preço que ninguém paga duas vezes.
GATO (irônico): Todo mundo diz isso. Ninguém cobra.

## D-TOMO-01 — Pegar o tomo (obrigatório)
SISTEMA: O tomo é pesado e frio. Algo lá dentro respira.
GATO: ...Ele está me olhando?

## D-TOMO-02 — A maldição (obrigatório, depois dos corações se apagarem)
SISTEMA: O selo se rompe. Seis das suas sete vidas se apagam.
SISTEMA: Resta uma última centelha. E ela está se apagando.

## D-GOLDEN-01 — Resgate e juramento (obrigatório, rev. 2)
GOLDEN: Respire, ladrãozinho. Seu último coração ainda bate... por pouco.
GATO (irônico): Você... me salvou? Depois da surra que eu te dei?
GOLDEN: Quem aguenta o selo e ainda respira é forte. Força assim não se desperdiça.
GOLDEN: Jure destruir esse tomo. Em troca, minha força é sua.
GATO: Juro. Não que eu tenha muita escolha.
[AÇÃO: um coração acende no HUD. Paladino desbloqueado]
SISTEMA: Paladino desbloqueado! Aperte 2 (ou Q/R) para trocar de classe.

## D-GOLDEN-02 — Conversas seguintes (opcional, uma por vez, em rodízio)
(No jogo: D-GOLDEN-02a, 02b e 02c.)
GOLDEN: Paredes rachadas não resistem a um golpe honesto.
GOLDEN: O tomo ainda sussurra, não é? Não o escute. Lembre do juramento.
GOLDEN: Força não substitui cuidado. Volte inteiro, ladrãozinho.
GATO (irônico): Inteiro é o que eu menos estou.

## Ficha de voz (resumo)
| Personagem | Voz | Nunca diria |
|---|---|---|
| Gato | Cínico, irônico, frases curtas e defensivas. Muda devagar (aqui ainda não agradece) | Discursos sobre honra |
| Golden | Calmo, caloroso e solene sem ser pomposo. Antes da luta chama o gato de "ladrão"; depois do resgate, de "ladrãozinho", com carinho [PROPOSTA]. Responde ironia com firmeza gentil | Sarcasmo, ameaças |
