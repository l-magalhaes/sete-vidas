# Plano de teste — Fase 3 (Sentinela e furtividade)

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080 · valores padrão de `StealthSettings` e `SentinelSettings`.
Antes: rode **Tools > Sete Vidas > 6. Fase 3**. Para ir direto ao trecho H, durante o Play mude o **Position X** do `Player` para **121**.
Dica: ligue **Gizmos** na Game view para ver o cone de visão (cinza = alcance base; colorido = alcance contra o gato agora), a última posição conhecida (X magenta) e os ruídos (círculos amarelos). O canto superior esquerdo mostra a visibilidade do gato.

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| STL-01 | Visibilidade | Na luz | Ficar em pé / agachar | 100% / 60% no canto da tela | ☐ |
| STL-02 | Sombra | Entrar na sombra A (x 122–130) | Ficar em pé / agachar | 30% / 18% e "[ESCONDIDO]"; o gato escurece | ☐ |
| STL-03 | Passos | Longe da Sentinela | Correr / andar agachado | Correndo aparecem círculos amarelos (Gizmos); agachado, nenhum | ☐ |
| STL-04 | Aterrissagem | Na passarela (y 5) | Cair direto da ponta | Círculo grande; a Sentinela (se perto) fica "?" | ☐ |
| STL-05 | Pendurar e soltar | Na ponta da passarela | Agarrar a borda (cair segurando a direção) e soltar com S | Sem círculo: silencioso | ☐ |
| STL-06 | Pulo normal | No chão | Pular | Sem barulho de aterrissagem | ☐ |
| STL-07 | Matinho | No matinho de x 106 (área livre) | Agachar / ficar de pé | Agachado: 15%, "agachado no mato", "[ESCONDIDO]" e o gato some atrás das folhas. De pé: 100% e a cabeça aparece | ☐ |
| STL-08 | Matinho na patrulha | Agachado no matinho de x 138 | Esperar a Sentinela passar | Ela passa ao lado sem "?"; depois o gato sai e golpeia pelas costas (5 FURTIVO) | ☐ |
| STL-09 | De pé no matinho | De pé no matinho de x 138 | Esperar a Sentinela chegar olhando para ele | Ela vê ("?" e depois "!") | ☐ |
| AI-01 | Patrulha | Play | Observar | Anda entre x 133 e 140, pausa ~1,5 s em cada ponta e vira | ☐ |
| AI-02 | Visão de frente | Em pé, na luz, à frente dela a ~6 tiles | Ficar parado | "?" em < 0,5 s, "!" em < 2 s | ☐ |
| AI-03 | Costas | Atrás dela, em pé e na luz | Ficar parado | Ela não reage | ☐ |
| AI-04 | Sombra | Agachado na sombra A, a 2+ tiles dela | Esperar ela passar perto | Não reage | ☐ |
| AI-05 | Paredes | Na passarela, acima dela | Andar de um lado ao outro | Não reage (a passarela bloqueia a visão) | ☐ |
| AI-06 | Barulho | Correndo a ~2 tiles atrás dela | Correr e parar | Vira com "?" e vai até onde ouviu | ☐ |
| AI-07 | Investigar | Depois de AI-06, escondido | Esperar | Procura olhando para os dois lados (~3 s) e volta a patrulhar | ☐ |
| AI-08 | Combate | Ser visto até "!" | Fugir correndo | Persegue mais devagar que o gato; sem ver por 2 s, vai investigar | ☐ |
| AI-09 | Bordas | Em combate, fugir subindo a parede da sombra A | Observar | Ela para na base; não atravessa paredes nem cai em buracos | ☐ |
| AI-10 | Ataque | Em combate, perto dela | Observar o golpe | Recua a lança (~0,45 s) antes de estocar; dá para pular para trás | ☐ |
| AI-11 | Dano no gato | Levar a estocada | — | "-1 hit", o gato é empurrado para longe e pisca por ~1 s; um 2º golpe nesse tempo não conta | ☐ |
| AI-12 | Dano pendurado/escalando | Levar a estocada pendurado ou escalando | — | Solta e é empurrado | ☐ |
| BCK-10 | Furtivo | Sentinela patrulhando, gato atrás | Um golpe | **5 FURTIVO**: ela tomba e some | ☐ |
| BCK-11 | Investigando | Sentinela com "?" de costas para o gato | Um golpe | Ainda é furtivo (não está em combate) | ☐ |
| BCK-12 | De frente | Sentinela patrulhando, gato à frente | Golpear | **2**; ela entra em combate na hora | ☐ |
| BCK-13 | Costas em combate | Sentinela com "!" | Pular por cima e golpear as costas | **3 COSTAS** | ☐ |
| AI-13 | Respawn | Sentinela morta | Esperar ~7 s | Volta no ponto inicial, patrulhando | ☐ |
| AI-14 | Atravessar | Em qualquer estado | Andar através dela | O gato passa; encostar não machuca | ☐ |
| SAL-01 | Rota por cima | Do x 121 | Escalar x 128, atravessar a passarela, pendurar e soltar na ponta | Chega a x 144+ sem nenhum "?" | ☐ |
| SAL-02 | Rota pelas costas | Do x 121 | Esperar na sombra, seguir agachado, golpear pelas costas | Furtivo derruba; sem "!" | ☐ |
| SAL-03 | Rota de combate | Do x 121 | Lutar de frente | Vence em 3 golpes desviando das estocadas | ☐ |
| AUTO-03 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite a observar:** golpear a Sentinela durante a preparação dela (o ataque dela continua); ser visto exatamente no frame do golpe furtivo (o golpe que a coloca em combate é o próprio furtivo, então vale 5); a Sentinela perseguindo até a sombra A com o gato agachado lá (em combate a sombra ajuda menos: visibilidade mínima 60%); cair da passarela em cima dela; a Sentinela investigando uma posição embaixo da passarela enquanto o gato está em cima; dois ruídos no mesmo frame.

**Pronto quando** (documento "Primeiros passos"): na `Playground`, há duas formas de passar pela Sentinela (furtiva e lutando), e o ataque furtivo pelas costas funciona quando ela ainda está patrulhando.
