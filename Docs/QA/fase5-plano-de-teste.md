# Plano de teste — Fase 5 (troca de classe e o Paladino)

> **Nome (10/11/2026):** o paladino "Golden" agora se chama **Koda, o Dourado** no jogo. "Golden" continua só nos nomes do código.

> **Atualizado:** o Golden agora é o chefão da arena e o vínculo acontece depois do tomo. Os casos GLD-01 a GLD-05 foram substituídos pelo [plano do chefe](chefe-plano-de-teste.md) (BOS e TOM).

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080.
Antes: rode **Tools > Sete Vidas > 8. Fase 5** e **Tools > Sete Vidas > Save > Apagar save** (jogo novo).
Trecho I: x 154–177. Para ir direto, durante o Play mude o **Position X** do `Player` para **153**. Parede rachada em x 160 (dentro do túnel); Golden em cima da laje, em x 168,5 (suba agarrando a borda de x 156).

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| CLS-01 | HUD | Jogo novo | Play | Ícone do Ladino (azul, adagas) à esquerda dos corações | ☐ |
| CLS-02 | Bloqueada | Jogo novo | Apertar 2, 3…7, Q e R | Nada acontece | ☐ |
| WAL-01 | Parede antes | Jogo novo | Entrar no túnel e golpear a parede com adagas | "tink", treme, não perde vida | ☐ |
| WAL-02 | Leitura | — | Olhar a parede de longe | Lê como "quebrável" (rachaduras douradas), diferente da pedra comum | ☐ |
| GLD-01 | Aviso | Subir na laje (agarrar a borda em x 156) | Chegar perto do Golden | "E: falar" | ☐ |
| GLD-02 | Conversa | Perto do Golden | E várias vezes | 5 falas em balão, uma por E | ☐ |
| GLD-03 | Vínculo | Última fala | E | "+1 coração", dica "Aperte 2 (ou Q/R)", 2º coração aparece no HUD, "plim" | ☐ |
| GLD-04 | Sair no meio | Conversa pela metade | Afastar-se e voltar | Conversa recomeça do início; nada foi concedido | ☐ |
| GLD-05 | Depois | Vínculo feito | E de novo | Uma fala curta por vez, em rodízio | ☐ |
| CLS-03 | Troca | Vínculo feito | Apertar 2 / 1 / R / Q | Troca; nome da classe na cor dela, brilho, "plim", ícone muda | ☐ |
| CLS-04 | Visual | Paladino | Andar, pular, golpear | Mesmo gato com a marreta nas costas; erguida na preparação; no chão no golpe | ☐ |
| CLS-05 | Durante o golpe | Paladino golpeando | Apertar 1 na preparação | Não troca | ☐ |
| CLS-06 | No ar / escalando | Paladino | Trocar no ar, escalando e pendurado | Troca e continua no ar/escalando/pendurado | ☐ |
| CLS-07 | Escalada | Paladino | Escalar a parede de x 80 e a chaminé | Funciona igual ao Ladino | ☐ |
| HAM-01 | Marreta | Paladino, no chão | Clique | Golpe lento (~0,35 s de preparação), anda devagar durante o golpe | ☐ |
| HAM-02 | Sem combo | Paladino | Clicar várias vezes rápido | Um golpe por vez | ☐ |
| HAM-03 | Barulho | Paladino perto da Sentinela (de costas) | Golpear o ar a ~6 tiles | Ela ouve ("?") | ☐ |
| HAM-04 | Sentinela | Paladino | Golpear de frente / pelas costas | 4 por golpe nos dois casos (sem FURTIVO) | ☐ |
| WAL-03 | Quebrar | Paladino | Duas marretadas na parede | 1ª: "4" e rachaduras maiores; 2ª: some, entulho, barulho | ☐ |
| WAL-04 | Passagem | Parede quebrada | Andar pelo túnel | Chega à placa "Atalho secreto!" | ☐ |
| WAL-05 | Persistência | Parede quebrada | Parar o Play e dar Play | Continua quebrada; 2 corações; Paladino desbloqueado | ☐ |
| WAL-06 | Morte | Paladino ativo, parede quebrada | Morrer | Volta como Paladino; parede continua quebrada; 2 corações | ☐ |
| AUTO-05 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite (QA):** trocar de classe durante o dano (recuo) e durante a morte (não troca); trocar no exato frame em que o golpe começa; quebrar a parede e morrer no mesmo instante (a flag já foi gravada); falar com o Golden durante a invencibilidade; apertar E perto do Golden e de um santuário ao mesmo tempo; save antigo com `activeClass: "Paladin"` sem o Paladino desbloqueado (volta para o Ladino).

**Pronto quando** (documento "Primeiros passos"): o fluxo "vejo a parede → não consigo quebrar → ganho o Paladino → volto e quebro" funciona, e a escalada continua funcionando com o Paladino ativo.
