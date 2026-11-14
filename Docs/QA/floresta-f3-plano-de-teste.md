# Plano de teste — Floresta F3 (da Fortaleza à queda da ponte)

Antes:
1. Abra `Scenes/World/Fortress_MVP` e rode **Tools > Sete Vidas > 13d. Fortaleza: saída para a Estrada (sem recriar a fase)**. Ele troca só o fim da fase e mantém os ajustes feitos à mão. Se o Unity perguntar "o mapa da Fortaleza mudou", responda **Depois** e use o 13d.
2. Rode **21. Floresta F2: graybox (7 cenas)** para gerar a `Forest_Road` nova (infectados, ponte, luzes).

Para o teste completo, jogue pela tela inicial com um save que já passou da maldição (ou chegue ao fim da Fortaleza). Para testar só a estrada, dê Play direto na `Forest_Road` com "Play começa na tela inicial" desmarcado (usa o save de testes).

## Testes automáticos

- EditMode > `ForestGrayboxTests`: regra do escudo e flag da ponte.
- PlayMode > `FortressTests`: `Exit_IsBehindTheBackGate` (a saída é uma passagem para a estrada e existe a chegada de volta) e `EdgeFog_CoversBothEndsOfTheMap` (luz na saída, escuro na entrada).

## Roteiro manual

| # | Passo | Esperado |
|---|---|---|
| 1 | Na Fortaleza, ande até o fim (depois do portão dos fundos) | No lugar do escuro, uma **luz dourada pontilhada**; o gato some nela |
| 2 | Entre na luz | Conversa D-FIM (sem "Fim da demonstração"); ao fechar, fade e a **Forest_Road** carrega, o gato na R1 com **a mesma vida** e as mesmas classes |
| 3 | Volte para a esquerda, na luz da R1 | Volta à Fortaleza, perto da saída, virado para a esquerda |
| 4 | Ande de novo até a saída da Fortaleza | **Sem a conversa**: passa direto para a estrada |
| 5 | R1: infectado de espada patrulhando; esconda-se no posto (sombra) | Ele não vê o gato na sombra; o Lion comenta a estrada uma vez |
| 6 | R2: ataque o infectado de **escudo** de frente com a adaga | "Tink", sem dano, e ele percebe o gato |
| 7 | Suba na torre e caia atrás dele; ataque pelas costas | Leva dano normalmente (por trás) |
| 8 | Ataque de frente com a **marreta** (Paladino) | Passa pelo escudo |
| 9 | R3 → R4: pise na ponte | Balão "Ah, não. Eles me seguiram."; a **horda** aparece atrás (R3) correndo; a pedra treme e racha, cada vez mais |
| 10 | Corra até o meio | "Não, não, não—", tremor forte; os blocos caem do meio para as pontas e o gato cai |
| 11 | Na queda | Fade; carrega a `Forest_A_Roots`, o gato na A1; balão "Ugh. Caí de novo." |
| 12 | Fique parado na ponte sem chegar ao meio | Depois de ~10 s ela desaba do mesmo jeito |
| 13 | Morra na fuga (deixe a horda pegar) | Renasce no último santuário; a ponte está **inteira** de novo (a flag só grava no desabamento) |
| 14 | Depois de cair, volte à `Forest_Road` (pela G3 → R5) e ande para oeste na ponte | O meio já está caído; cair leva à A1 |
| 15 | Feche e reabra (Continuar) | A ponte continua caída (`ponte_caiu` no save) |
| 16 | Em toda saída da Floresta (portas laterais, buracos no teto e no chão) | Luz verde-clara pontilhada do lado de fora; portas de atalho com um clarão atrás |
