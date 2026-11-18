# Plano de teste — Floresta F7 (a Isyathy embaixo da terra)

Spec: `Docs/Floresta/plano-implementacao.md`, F7 ("Dinâmica atual"). Substitui o roteiro da fuga, que foi descartada em 17/11/2026.

Antes, escolha um caminho:
- **Para não perder os ajustes à mão:** rode **Tools > Sete Vidas > Floresta: Ninho F7 (a Isyathy embaixo da terra, sem recriar a cena)**. O menu:
  - abre a `Forest_E_Nest`;
  - troca o objeto antigo ("Isyathy (fuga)" ou "Isyathy (ninho)") por "Isyathy (embaixo da terra)";
  - garante a E1–E3 sem inimigos.
- **Ou rode** **Floresta: gerar as cenas** (o `ForestLayout` está na versão 29).

O desenho da Isyathy é feito em código, então não há PNG para gerar. Para ver a arte fora do Unity: `powershell -ExecutionPolicy Bypass -File Tools/EnvironmentSprites/previa_isyathy.ps1`.

O Ninho só abre depois do Portão do Templo (Chave do Templo, com o Caçador). Para ver o despertar de novo: use um save novo ou apague a flag `cena_vista_ninho_despertar`.

## Testes automáticos

- **EditMode > `NestBurrowTests` (17):**
  - o corpo e a onda;
  - o trilho que continua para o fundo da terra;
  - o lugar do bote (na frente do gato);
  - a subida inclinada para longe dele;
  - o alcance e a altura mínima do bote;
  - a mordida (um coração);
  - a zona segura dos santuários;
  - o tempo para desviar;
  - o olho verde com o halo roxo, fechado quando ela dorme;
  - os dentes;
  - o pescoço do tamanho do corpo;
  - as feridas e as veias;
  - o corpo escondido embaixo do chão no buraco.
- **`Tools/LevelPreview/checar_floresta.ps1`:** deve dar "Tudo alcançável, sem softlock" (conferido em 17/11/2026).

## Roteiro manual

| # | Onde | Passo | Esperado |
|---|---|---|---|
| 1 | E1, entrada | Entre no Ninho pela D4 | Ela está deitada na frente da tela: o rabo no bloco alto, o corpo por cima dos espinhos e a cabeça na rampa, de olho fechado. O corpo respira devagar, numa onda leve. |
| 2 | E1 | Ande uns 4 tiles para dentro | A cena começa. A terra treme e aparece "...Isso não é raiz.". |
| 3 | E1 | Assista | O olho abre: verde, com o brilho roxo em volta. A cabeça levanta e se vira para o gato, e o corpo inteiro escorrega atrás dela. |
| 4 | E1 | Assista | Ela ruge de boca aberta (dentes, garganta roxa) e a tela treme forte. |
| 5 | E1 | Assista | Ela arqueia, mergulha no patamar e o corpo inteiro some na terra pela boca do buraco. Voa terra e a tela treme. Não aparece nenhum pedaço dela abaixo do chão. |
| 6 | E1 | Depois do mergulho | Fica a cratera no patamar. Aparecem "Ela entrou na terra." e "Se o chão tremer, não fico parado.". O controle volta. |
| 7 | E1 | Recarregue a cena | Ela não está mais deitada, e a cratera continua lá. |
| 8 | E1–E3 | Ande pelo chão uns 4 s | A tela treme cerca de 1 s e o chão racha a uns 3,5 tiles do gato, na frente dele, com terrinha pulando. |
| 9 | E1–E3 | Fique parado olhando o buraco | Ela sai do buraco, se arma inclinada para trás e abre a boca. Depois dá o bote onde o gato estava e volta para a terra. A mordida tira **um coração**. |
| 10 | E1–E3 | Recue ou pule assim que a tela tremer | Dá tempo de desviar. O bote passa e não acerta. |
| 11 | E1–E3 | Repare no corpo | O corpo é um tubo só, sem emendas: escamas, manchas de anaconda, ventre claro, feridas abertas e veias roxas que brilham. Ele faz a curva junto com a cabeça. |
| 12 | E1–E3 | Repare na cratera do bote | A borda de terra fica na frente do corpo. Depois do bote, a cratera some aos poucos. |
| 13 | E1–E3 | Conte o tempo entre um bote e outro | De 3 a 5 s. Ela nunca ataca com o gato no ar, em vinha ou em galho. |
| 14 | E1 | Fique perto dos espinhos | O buraco nunca abre em cima dos espinhos nem em chão torto. |
| 15 | E1 | Fique perto do santuário (S-F3B, no começo da E1) | Ali ela não ataca. Dá para salvar em paz. O Ninho só tem 2 santuários: este e o S-F4 na E6, antes do chefe (18/11/2026). |
| 16 | E2–E3 | Bote perto de um galho simples | O galho quebra e volta em 8 s. |
| 17 | E4 | Entre na clareira do musgo | Ela não ataca mais. |
| 18 | E1 | Repita o despertar com o save que já viu a cena | Dá para pular a cena. Pulando, ela some e a cratera aparece. |
| 19 | — | Morra para um bote e volte | Ela volta a atacar normalmente. Nada trava. |

## O que observar no playtest

- **Ritmo:**
  - O aviso de 1,1 s está curto ou longo?
  - O intervalo de 6 a 10 s cansa ou fica previsível?
- **O bote:** ele lê bem e dá para entender de onde vem?
- **Desempenho:** o corpo é redesenhado a cada quadro enquanto ela aparece. Se cair o FPS, anote em qual sala.
