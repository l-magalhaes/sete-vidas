# Paleta mestre do cenário

> Arquivo gerado por `Tools/EnvironmentSprites/gerar.ps1` a partir de `Tools/EnvironmentSprites/EnvPalette.cs`. **Não edite aqui:** mude o `.cs` e rode o script.

Arquivos: `Docs/Arte/paleta_fortaleza.gpl` (abre no Aseprite e no GIMP), `Docs/Arte/paleta_fortaleza.hex` e a prévia `SeteVidasDocs/Referencias/paleta_fortaleza.png`.

**Regras:** as cores reservadas nunca aparecem em enfeites (o validador do `gerar.ps1` confere). O fundo fica abaixo de 25% de brilho nas áreas noturnas e internas. Rocha natural é quente, pedra trabalhada é fria (plano de assets, 4.2).

## Reservadas (jogo)

| Cor | Hex | Uso |
|---|---|---|
| Armadilha | `#c83a3a` | Só armadilhas (dano). Nenhum enfeite pode usar. |
| Hera escalável (ramo) | `#4f5a2e` | Só parede escalável. |
| Hera escalável (folha) | `#8a9a4e` | Só parede escalável. |
| Praga (roxo) | `#5b2a6e` | Só a praga e o Tomo. O que identifica a praga é o PAR roxo + verde-ácido: o céu do pôr do sol pode ter roxo (o validador não checa o roxo nos fundos). |
| Praga (verde-ácido) | `#8fd14f` | Só a praga e o Tomo. |
| Matinho (esconderijo) | `#456633` | Só a moita jogável 32×16. |

## Objetos barulhentos

| Cor | Hex | Uso |
|---|---|---|
| Cacos de vidro (brilho) | `#e8f0ff` | Brilho de 1 px sobre o chão. Não é exclusivo (a espuma da água é parecida), mas no chão do plano do jogo só os cacos brilham assim. |
| Folhas secas | `#a0582e` | Tapete de folhas no exterior e no pátio. |
| Folhas secas (sombra) | `#6e3a22` | Sombra das folhas. |
| Sininhos (latão) | `#b08a3a` | Sininhos pendurados na altura da cabeça. |

## Céu do pôr do sol

| Cor | Hex | Uso |
|---|---|---|
| Céu 1 | `#1f1533` | Topo |
| Céu 2 | `#2e1a42` | Faixa do gradiente |
| Céu 3 | `#4a2150` | Faixa do gradiente |
| Céu 4 | `#6e2a5a` | Faixa do gradiente |
| Céu 5 | `#9a3a5a` | Faixa do gradiente |
| Céu 6 | `#c4504f` | Faixa do gradiente |
| Céu 7 | `#e0703f` | Faixa do gradiente |
| Céu 8 | `#f09a45` | Faixa do gradiente |
| Céu 9 | `#f7c063` | Horizonte |
| Sol | `#ffe9a8` | Disco do sol |
| Sol (borda) | `#ffd070` | Borda do sol, janelas acesas de longe |
| Nuvem | `#e88a7a` | Nuvem na sombra |
| Nuvem (luz) | `#f2b0a0` | Nuvem na luz |

## Silhuetas de fundo

| Cor | Hex | Uso |
|---|---|---|
| Montanha longe | `#3a2247` | bg_longe |
| Montanha longe (luz) | `#6a3a6a` | Borda voltada para o sol |
| Montanha longe (névoa) | `#8a5a7a` | Faixa de névoa |
| Cidadela longe | `#2a1838` | Silhueta da Cidadela |
| Morro | `#24162f` | bg_medio |
| Morro (luz) | `#33203f` | Borda do morro |
| Pinheiro longe | `#1a1024` | Pinheiros do bg_medio |

## Rocha natural (quente)

| Cor | Hex | Uso |
|---|---|---|
| Rocha escura | `#3b3346` | Sombra, base |
| Rocha | `#574c62` | Corpo |
| Rocha clara | `#7b6d80` | Face iluminada |
| Rocha (luz) | `#a08f98` | Quinas e estratos |

## Pedra trabalhada (fria)

| Cor | Hex | Uso |
|---|---|---|
| Fundação | `#4a455c` | Blocos de Golden (ano 423) |
| Fundação (luz) | `#5e5872` | Face dos blocos grandes |
| Soluki | `#7a748e` | Tijolo militar |
| Reforma | `#9a94aa` | Pedra nova, mais clara |
| Argamassa nova | `#c8c2b8` | Juntas de reforma |
| Placeholder chão | `#5a556e` | Tile de chão provisório |
| Placeholder borda | `#3a364a` | Contorno do tile provisório |
| Placeholder luz | `#77728c` | Topo de rampa, ranhuras |

## Madeira

| Cor | Hex | Uso |
|---|---|---|
| Madeira velha | `#6b4a2b` | Estruturas antigas |
| Madeira velha (luz) | `#9a7044` | Topo das tábuas |
| Madeira nova | `#b08a5a` | Reformas (tábuas novas da arquibancada) |

## Ordem da Presa Dourada

| Cor | Hex | Uso |
|---|---|---|
| Carmesim | `#8f2a2a` | Tecido da Ordem (não é o vermelho das armadilhas) |
| Carmesim escuro | `#5a161c` | Sombra do tecido |
| Dourado | `#d9a441` | Pata dourada, filetes |
| Dourado escuro | `#8a621e` | Sombra do dourado |
| Vitral azul | `#2a4a8f` | Vidro |
| Vitral azul (luz) | `#4a7ac8` | Vidro na luz |
| Pergaminho | `#e9dcb8` | Placas, alvos |

## Vegetação

| Cor | Hex | Uso |
|---|---|---|
| Folha escura | `#2b3d2c` | Copa na sombra |
| Folha | `#3f5a34` | Copa |
| Folha clara | `#6f8a45` | Copa na luz |
| Tronco escuro | `#3d2a22` | Tronco na sombra |
| Tronco | `#5e4030` | Tronco |
| Tronco claro | `#7d5a3e` | Casca na luz |
| Hera decorativa | `#36524a` | Hera de fundo, azulada (nunca no plano do jogo) |
| Musgo | `#36524a` | Juntas úmidas |
| Luz de borda do pôr do sol | `#e0a060` | Rim light do lado do sol |
| Vaga-lume | `#e6f2c8` | Partícula de 1 px (não amarelo: não parecer moeda) |

## Água

| Cor | Hex | Uso |
|---|---|---|
| Água | `#9fd4f0` | Corpo |
| Água média | `#6fb0e0` | Meio-tom |
| Espuma | `#e8f8ff` | Espuma, brilho |
| Água funda | `#4a86c0` | Sombra |

## Fogo e luz

| Cor | Hex | Uso |
|---|---|---|
| Chama (miolo) | `#fff2b0` | Centro da chama |
| Chama | `#ffa83a` | Corpo da chama |
| Chama (ponta) | `#e0702a` | Ponta da chama de tocha (laranja, longe do vermelho das armadilhas) |

## Primatas

| Cor | Hex | Uso |
|---|---|---|
| Estrada negra | `#15171c` | Pedra lisa sem juntas |
| Estrada negra (luz) | `#2a2e36` | Face da placa |
| Cristal de Éter | `#7fd8e0` | Brilho frio dos Vestígios |

## Tinta

| Cor | Hex | Uso |
|---|---|---|
| Tinta | `#1c1a24` | Contorno e sombra mais escura do cenário |

