# Catálogo da biblioteca de cenário

> Gerado por `Tools/EnvironmentSprites/gerar_catalogo.ps1` a partir dos `manifest.json`. **Não edite à mão**: mude o manifesto e rode o script de novo. Como usar a biblioteca: `guia-de-assets.md`.

**149 assets** em 22 manifestos.

## Backgrounds/Interior

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/bg_int_seteira.png) | `bg_int_seteira` | Seteira (vista de dentro) | decorativo | parede_fundo (-9) | 1 | **apoio parede.** Moldura; a abertura é recortada na parede do fundo por bg_int_seteira_mascara (SpriteMask) e mostra o céu daquela altura. Colocadas pelo FortressBackgrounds.Windows (Etapa 6B). |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/bg_int_janela.png) | `bg_int_janela` | Janela (vista de dentro) | decorativo | parede_fundo (-9) | 1 | **apoio parede.** Moldura com travessa; a máscara bg_int_janela_mascara recorta a parede e mostra o céu e a cordilheira daquela altura. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/bg_int_janela_grande.png) | `bg_int_janela_grande` | Janela grande (vista de dentro) | decorativo | parede_fundo (-9) | 1 | **apoio parede.** A câmara: janelas amplas com mainel, o último raio de sol e a montanha do início lá atrás. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/fg_corrente.png) | `fg_corrente` | Corrente de primeiro plano | decorativo | fg | 1 | **apoio teto.** Silhueta quase preta pendendo do teto, na frente de tudo (3.5): só onde não cobre gato, inimigo, armadilha ou rota (nunca sobre passarela, plataforma, sacada ou arena). |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/fg_nevoa_saida.png) | `fg_nevoa_saida` | Escuridão da saída da fase | decorativo | fg (25) | 1 | **apoio livre.** Melhoria 8: gradiente pontilhado de 7 tiles na frente de tudo (repete na vertical), transparente no começo e preto no fim; depois dele, fg_nevoa_preto. O gato some nele ao seguir para Miaravéu e o fim do mapa nunca aparece. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/fg_nevoa_entrada.png) | `fg_nevoa_entrada` | Escuridão da entrada da fase | decorativo | fg (25) | 1 | **apoio livre.** A mesma escuridão, de 3 tiles e escura à esquerda, na parede de rocha do começo da fase. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Interior/fg_nevoa_preto.png) | `fg_nevoa_preto` | Escuridão cheia | decorativo | fg (25) | 1 | **apoio livre.** Continua a escuridão para além das pontas do mapa (Tiled), para a câmera nunca ver o vazio. |

## Backgrounds

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Sky/bg_ceu_crepusculo.png) | `bg_ceu_crepusculo` | CÃ©u do crepÃºsculo frio | decorativo | ceu | 1 · paralaxe 0/0 | Embaixo, o sol jÃ¡ se pÃ´s: sÃ³ as nuvens altas pegam luz laranja. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Sky/bg_ceu_borda_do_sol.png) | `bg_ceu_borda_do_sol` | CÃ©u com a borda do sol | decorativo | ceu | 1 · paralaxe 0/0 | Subindo a muralha, a borda do sol volta a aparecer. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Sky/bg_ceu_meio_sol.png) | `bg_ceu_meio_sol` | CÃ©u do meio sol | decorativo | ceu | 1 · paralaxe 0/0 | Andar 72: meio sol no horizonte. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Sky/bg_ceu_ultimo_raio.png) | `bg_ceu_ultimo_raio` | CÃ©u do Ãºltimo raio | decorativo | ceu | 1 · paralaxe 0/0 | No topo: o sol inteiro e os raios dourados (o Sol Eterno). Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Sky/bg_ceu_noite.png) | `bg_ceu_noite` | CÃ©u da noite | decorativo | ceu | 1 · paralaxe 0/0 | Depois da maldiÃ§Ã£o: lua crescente Ã  esquerda, estrelas e um vÃ©u roxo fraco (a praga solta). Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_cordilheira_a.png) | `bg_ext_cordilheira_a` | Cordilheira com a Cidadela pequena | decorativo | bg_longe | 1 · paralaxe 0,06/0,15 | 960 px, emenda sem costura; acontecimentos: cachoeira distante, torre em ruÃ­na, a Cidadela Ã  direita. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_cordilheira.png) | `bg_ext_cordilheira` | Cordilheira (sem a Cidadela) | decorativo | bg_longe | 1 · paralaxe 0,06/0,15 | Vista de dentro da Cidadela: o horizonte baixa conforme o gato sobe. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_cordilheira_noite.png) | `bg_ext_cordilheira_noite` | Cordilheira de noite | decorativo | bg_longe | 1 · paralaxe 0,06/0,15 | Borda iluminada pela lua (esquerda). Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_floresta_vale.png) | `bg_ext_floresta_vale` | Floresta do vale | decorativo | bg_medio | 1 · paralaxe 0,2/0,3 | Bosques de 3â€“7 Ã¡rvores com clareiras, a trilha serpenteando atÃ© o castelo e um lago numa clareira. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_copas_de_cima.png) | `bg_ext_copas_de_cima` | Copas vistas de cima | decorativo | bg_medio | 1 · paralaxe 0,2/0,3 | Mar de copas com nÃ©voa na borda de cima. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_montanha_inicio.png) | `bg_ext_montanha_inicio` | Montanha do inÃ­cio (do vale) | decorativo | bg_longe | 1 · paralaxe 0,06/0,15 | Marco: o morro do inÃ­cio com o penhasco, Ã  esquerda; fica para trÃ¡s aos poucos conforme o gato anda (sem a cachoeira: a de verdade estÃ¡ ao lado). Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Ext/bg_ext_montanha_inicio_longe.png) | `bg_ext_montanha_inicio_longe` | Montanha do inÃ­cio (do topo) | decorativo | bg_longe | 1 · paralaxe 0,06/0,15 | Do topo, olhando para trÃ¡s: o caminho jÃ¡ feito, pequeno, Ã  esquerda. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Cid/bg_cid_fachada_perto.png) | `bg_cid_fachada_perto` | A Cidadela de perto | decorativo | bg_medio | 1 · paralaxe 0,2/0,3 | Metade da tela, torres saindo pelo alto, janelas acesas nos andares. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |
| ![](../../Assets/_Game/Art/Environments/Backgrounds/Mia/bg_mia_telhados_horizonte.png) | `bg_mia_telhados_horizonte` | MiaravÃ©u no horizonte | decorativo | bg_medio | 1 · paralaxe 0,2/0,3 | A estrada descendo atÃ© a prÃ³xima cidade: telhados, janelas acesas e fumaÃ§a. Gerado por Tools/EnvironmentSprites/BackgroundSpriteGen.cs (Etapa 6). A Ã¢ncora de cada conjunto vem do FortressBackgrounds. |

## Citadel/Culture

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_estandarte_desbotado.png) | `env_cid_cul_estandarte_desbotado` | Estandarte desbotado | narrativo | decor_atras | 1 | **apoio livre.** VersÃ£o velha do estandarte da Ordem: onde ninguÃ©m troca mais (fundos, torres). O novo fica no salÃ£o e na cÃ¢mara. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_escudo_nome_0.png) | `env_cid_cul_escudo_nome` | Escudo de paladino caÃ­do com nome | narrativo | decor_atras | 3 | **apoio parede.** Escudos dos paladinos caÃ­dos com a plaquinha do nome: novo, desbotado e rachado. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_vela_0.png) | `env_cid_cul_vela` | CastiÃ§al com velas | luz | decor_atras | 1 · anim 3q | **apoio chao.** Velas do salÃ£o (cerimÃ´nia). Precisa de luz em volta. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_vela_apagada_0.png) | `env_cid_cul_vela_apagada` | CastiÃ§al com velas apagadas | narrativo | decor_atras | 1 · anim 3q | **apoio chao.** CorrupÃ§Ã£o (6.3): velas apagadas sem motivo, fumacinha de 1 px. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_tapete.png) | `env_cid_cul_tapete` | Tapete do salÃ£o | decorativo | decor_atras | 1 | **apoio chao.** Repete a cada tile (fileira); gasto no meio do caminho. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_placa_juramento.png) | `env_cid_cul_placa_juramento` | Placa do juramento | narrativo | decor_atras | 1 | **apoio parede.** O juramento da Ordem gravado em ouro. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_emblema.png) | `env_cid_cul_emblema` | Emblema da pata | decorativo | decor_atras | 1 | **apoio parede.** Pata dourada em relevo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_sino.png) | `env_cid_cul_sino` | Sino de alarme | narrativo | decor_atras | 1 | **apoio teto.** O sino da Torre do Sino, pendurado no teto do salÃ£o sobre a chaminÃ©, com a corda descendo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_pombal.png) | `env_cid_cul_pombal` | Pombal | narrativo | decor_atras | 1 | **apoio chao.** Pombos-correio da Ordem, num poste que sai do chÃ£o (preso no ar parecia um baÃº flutuando). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Culture/env_cid_cul_penas.png) | `env_cid_cul_penas` | Penas no chÃ£o | decorativo | decor_atras | 1 | **apoio chao.** Embaixo do pombal. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Citadel/Magic

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Citadel/Magic/env_mag_runa_0.png) | `env_mag_runa` | Runa dourada da Ordem | narrativo | decor_atras | 3 | **apoio parede.** Runas de proteÃ§Ã£o. Pulsam fora de tempo com as veias (Etapa 7: PulseLight). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Magic/env_mag_selo_cera_0.png) | `env_mag_selo_cera` | Selo de cera | narrativo | decor_atras | 2 | **apoio livre.** Lacre carmesim num pergaminho: intacto (0) e rompido (1). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Magic/env_mag_corrente.png) | `env_mag_corrente` | Corrente pendurada | decorativo | decor_atras | 1 | **apoio teto.** Correntes do selo, do teto da cÃ¢mara. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Magic/env_mag_selo_porta_0.png) | `env_mag_selo_porta` | Selo das sete portas | narrativo | decor_atras | 7 | **apoio parede.** Uma variaÃ§Ã£o por porta (1.3 do plano): 1 intacto e limpo, 2 intacto, 3 com correntes, 4 fissura roxa, 5 veias roxas, 6 runas falhando, 7 rompido. Use Fixed com a variaÃ§Ã£o = porta - 1. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Citadel

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_cid_fundacao_escalavel_0_0.png) | `env_cid_est_fundacao` | Alvenaria da fundação de Golden (tileset) | plataforma | jogo | 92 | **apoio livre.** Escolhida por célula pelo TerrainAutotile: topo (pedra de cobertura), miolo, meio-tom (3º tile do ar), interior escuro (4º+), parede do fundo (escura, roxa) e parede escalável (ranhuras + hera seca). A paridade (x+y) diz qual metade do bloco de 32 px. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_cid_soluki_escalavel_0_0.png) | `env_cid_est_soluki` | Tijolo militar de Soluki (tileset) | plataforma | jogo | 46 | **apoio livre.**  |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_cid_reforma_escalavel_0_0.png) | `env_cid_est_reforma` | Alvenaria reformada (tileset) | plataforma | jogo | 46 | **apoio livre.** Os tijolos antigos (#7a748e) no meio dos novos contam a reforma. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_cid_abandono_escalavel_0_0.png) | `env_cid_est_abandono` | Alvenaria abandonada (tileset) | plataforma | jogo | 92 | **apoio livre.** Rachaduras sempre escuras: dourada é só a parede quebrável. 4 variações (limpa, tijolo caído, rachadura, musgo pesado). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_sol_madeira_escalavel_0_0.png) | `env_sol_est_madeira` | Madeira da arquibancada (tileset) | plataforma | jogo | 46 | **apoio livre.** Tábuas novas (claras) entre as velhas: a arquibancada foi consertada. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/tile_ext_terra_batida_topo_1_0.png) | `env_ext_ter_terra_batida` | Terra batida do pátio (topo) | plataforma | jogo | 24 | **apoio livre.** Só o topo; embaixo usa o solo do exterior. Tufos raros (12%): o mato invade as bordas do pátio. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_sol_arq_gargula_l.png) | `env_sol_arq_gargula` | Gárgula / calha em cima das vinhas | narrativo | terreno_detalhe | 4 | **apoio parede.** Colocada sozinha pelo TerrainPainter no topo de TODA vinha, na face da parede (plano, 4.4: a água da chuva sai por ela e alimenta a vinha). Na madeira, uma calha. |
| ![](../../Assets/_Game/Art/Environments/Citadel/env_cid_cul_estatua_gigante.png) | `env_cid_cul_estatua_gigante` | Estátua gigante de cão paladino | narrativo | decor_atras | 1 | **apoio chao.** Fica entre a tocha acesa e a sombra jogável: é ela que bloqueia a luz e faz a sombra (toda sombra precisa de um motivo visível). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_cid_parede_quebravel_abandono_danificada.png) | `env_cid_parede_quebravel` | Parede quebrável por época (gate do Paladino) | gate | jogo (1) | 12 | **apoio chao.** Uma porta antiga fechada com alvenaria que está cedendo: rede GRANDE de rachaduras escuras de cima a baixo, tijolos saltados, entulho no pé. Sem dourado. As outras paredes não têm rachaduras. O FortressSetup escolhe a época pela pedra do lugar. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_cid_arq_alcova_10x5.png) | `env_cid_arq_alcova_10x5` | Alcova em arco (sombra de 10 × 5) | narrativo | parede_fundo (-9) | 1 | **apoio chao.** Recuo na parede do fundo com pilares e arco: a luz das tochas não entra, por isso a sombra jogável (mesma largura e altura) fica aqui. Toda sombra de interior precisa de uma. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_cid_arq_alcova_8x5.png) | `env_cid_arq_alcova_8x5` | Alcova em arco (sombra de 8 × 5) | narrativo | parede_fundo (-9) | 1 | **apoio chao.**  |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_sol_arq_gota_0.png) | `env_sol_arq_gota` | Gota da gárgula | decorativo | terreno_detalhe (2) | 1 · anim 14q | **apoio livre.** Colocada sozinha embaixo de cada gárgula/calha (TerrainPainter), cada uma com atraso próprio: forma, cai 3 tiles ao longo da vinha, espirra e pausa. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_sol_arq_poca_0.png) | `env_sol_arq_poca` | Poça da gárgula | decorativo | terreno_detalhe (2) | 1 · anim 8q | **apoio chao.** Posta pelo TerrainPainter no chão embaixo do fio d'água de cada gárgula, menos onde já há um balde ou uma tina: a água que pinga se junta ali. Rasa e meio transparente: nunca parece buraco nem plataforma. |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_cid_arq_ameias_fundo.png) | `env_cid_arq_ameias_fundo` | Ameias no alto da parede do fundo | decorativo | parede_fundo (-9) | 1 | **apoio chao.** No fundo (escuro, sem colisão): fecha o alto da torre. Nunca no plano do jogo (pareceria degrau). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Architecture/env_cid_parede_mancha.png) | `env_cid_parede_evento` | Acontecimentos da parede do fundo | narrativo | parede_fundo (-9) | 15 | **apoio livre.** Remendo de outra pedra e mancha de água: ~1 a cada 10 tiles, 1 tile acima do chão, colocados pelo TerrainPainter (regra 4.5 contra repetição). Sem janela tapada nem nicho: pareciam janelinhas competindo com as janelas da Etapa 6B. |
| ![](../../Assets/_Game/Art/Environments/Citadel/env_cid_cul_estatua_paladino.png) | `env_cid_cul_statue_paladin` | Estátua de cão paladino | decorativo | decor_atras | 1 | **apoio chao.** Atrás dos personagens. Não pode parecer inimigo: sem olho claro, cor de pedra. |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_banner.png) | `env_cid_cul_banner_order` | Estandarte da Ordem | decorativo | decor_atras | 1 | **apoio teto.** Carmesim #8f2a2a é da Ordem; nunca usar o vermelho das armadilhas #c83a3a. Emblema não espelha. |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_stained_glass.png) | `env_cid_arq_stained_glass` | Vitral em arco | decorativo | decor_atras | 1 | **apoio parede.** Mais tarde vira janela com máscara mostrando o céu daquela altura (plano, 7.5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_shield.png) | `env_cid_cul_shield_paladin` | Escudo de paladino caído | narrativo | decor_atras | 1 | **apoio parede.** Em fileira na parede do salão, um por paladino caído. |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_lore_plaque.png) | `env_cid_cul_lore_plaque` | Placa de pedra (lore) | interativo | decor_atras (-4) | 1 | **apoio chao.** Colocada pelas placas de lore do FortressLayout (tipo lore), não pelo FortressDressing. |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_weapon_rack.png) | `env_sol_obj_weapon_rack` | Suporte de armas de treino | decorativo | decor_atras | 1 | **apoio chao.** Armas de madeira: não brilham como arma de verdade. |
| ![](../../Assets/_Game/Art/Environments/Citadel/decor_archery_target.png) | `env_sol_obj_archery_target` | Alvo de arco | decorativo | decor_atras | 1 | **apoio chao.** Anéis em carmesim da Ordem, não no vermelho das armadilhas. |

## Citadel/Structures

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_seteira.png) | `env_cid_arq_seteira` | Seteira | narrativo | terreno_detalhe (1) | 1 | **apoio parede.** AtrÃ¡s de cada lanÃ§ador de flechas (explica de onde vÃªm as flechas). Ordem 1: na frente da parede, atrÃ¡s do lanÃ§ador (2). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_arco_passagem.png) | `env_cid_arq_arco_passagem` | Arco de passagem | decorativo | decor_atras | 1 | **apoio chao.** Moldura de uma abertura de 3 tiles; o selo da porta vai na pedra de fecho. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_portao_levantado.png) | `env_cid_arq_portao_levantado` | Grade levantada | decorativo | decor_atras | 1 | **apoio teto.** Barra de baixo reta, sem pontas: nÃ£o pode parecer perigo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_viga.png) | `env_cid_arq_viga` | Viga do teto | decorativo | decor_atras | 1 | **apoio teto.** Ponta de viga com mÃ£os-francesas, em fileira no teto. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_coluna.png) | `env_cid_arq_coluna` | Coluna do salÃ£o | decorativo | decor_atras | 1 | **apoio chao.** Do chÃ£o ao teto (10 tiles). Nunca atrÃ¡s de plataforma (pareceria capitel). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Structures/env_cid_arq_escada_fundo.png) | `env_cid_arq_escada_fundo` | Escada de fundo | decorativo | parede_fundo (-9) | 1 | **apoio chao.** Escura e sem contorno, sempre no fundo: no plano do jogo pareceria degrau. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Citadel/Traps

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_tubo_oleo.png) | `env_cid_obj_tubo_oleo` | Cano de Ã³leo | narrativo | decor_atras | 1 | **apoio parede.** Corre pela parede acima dos jatos de fogo; emenda a cada 16 px. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_tubo_descida.png) | `env_cid_obj_tubo_descida` | Descida do cano de Ã³leo | narrativo | decor_atras | 1 | **apoio parede.** Liga o cano a cada jato de fogo: explica de onde vem o fogo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_alavanca.png) | `env_cid_obj_alavanca` | Alavanca | decorativo | decor_atras | 1 | **apoio parede.** SÃ³ cenÃ¡rio (nÃ£o Ã© interativa). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_engrenagem.png) | `env_cid_obj_engrenagem` | Engrenagem na parede | decorativo | decor_atras | 1 | **apoio parede.** Mecanismo das armadilhas. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_registro.png) | `env_cid_obj_registro` | Registro de manutenÃ§Ã£o | narrativo | decor_atras | 1 | **apoio parede.** Papel pregado com a mancha de fuligem dos jatos. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_elmo_amassado.png) | `env_cid_obj_elmo_amassado` | Elmo amassado | narrativo | decor_atras | 1 | **apoio chao.** No fosso: quem falhou. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Citadel/Traps/env_cid_obj_escudo_amassado.png) | `env_cid_obj_escudo_amassado` | Escudo amassado | narrativo | decor_atras | 1 | **apoio chao.** No fosso, com a pata dourada. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Common/Guild

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Common/Guild/env_com_cul_marca_guilda_0.png) | `env_com_cul_marca_guilda` | Marca de giz da Guilda das Nove Sombras | narrativo | decor_atras | 3 | **apoio parede.** Dica de furtividade de outros ladrÃµes: olho (sombra boa), seta com 9 pontos (por aqui), nove riscos (jÃ¡ passamos). Perto de sombras e passagens, 1 tile acima do chÃ£o. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Guild/env_com_obj_corda_cortada.png) | `env_com_obj_corda_cortada` | Corda cortada com gancho | narrativo | decor_atras | 1 | **apoio teto.** AlguÃ©m jÃ¡ tentou: gancho preso no teto e a corda cortada. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Common/Life

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Common/Life/env_com_obj_buraco_rato.png) | `env_com_obj_buraco_rato` | Buraco de rato | decorativo | decor_atras | 1 | **apoio chao.** Para onde o rato foge (Etapa 7). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Life/env_ext_ani_passaro_0.png) | `env_ext_ani_passaro` | PÃ¡ssaro (foge do gato) | decorativo | decor_atras | 1 | **apoio chao.** Pousado; a 4 tiles do gato levanta voo (env_ext_ani_passaro_voo_0..1) e some. SÃ³ visual: nÃ£o faz barulho para a IA (decidido). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Life/env_com_ani_rato_0.png) | `env_com_ani_rato` | Rato (foge do gato) | decorativo | decor_atras | 1 | **apoio chao.** A 3 tiles do gato corre (env_com_ani_rato_corre_0..1) e some no buraco. SÃ³ visual. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Common

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Common/env_com_obj_wall_torch_0.png) | `env_com_obj_wall_torch` | Tocha de parede | luz | decor_atras | 1 · anim 4q | **apoio parede.** Gerada por Tools/EnvironmentSprites (PropSpriteGen). Metal e madeira claros o bastante para aparecer na parede escura. Toda tocha acesa precisa de LightZone iluminada em volta; toda sombra jogável perto de tocha precisa de motivo (tocha apagada). |
| ![](../../Assets/_Game/Art/Environments/Common/env_com_obj_wall_torch_apagada_0.png) | `env_com_obj_wall_torch_apagada` | Tocha de parede apagada (fumegando) | narrativo | decor_atras | 1 · anim 2q | **apoio parede.** Dentro de cada sombra jogável de interior: a ronda apagou esta tocha, por isso está escuro aqui (plano, 4.4). |

## Common/Noisy

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Common/Noisy/env_com_obj_cacos_0.png) | `env_com_obj_cacos` | Cacos de vidro (barulhento) | interativo | decor_atras | 1 · anim 4q | **apoio chao.** NoisyObject Glass: barulho pisando, atÃ© agachado; pule por cima. Sempre perto de um vidro quebrado. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Noisy/env_com_obj_folhas_secas_0.png) | `env_com_obj_folhas_secas` | Folhas secas (barulhento) | interativo | decor_atras | 1 · anim 4q | **apoio chao.** NoisyObject DryLeaves: barulho correndo; agachado nÃ£o. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Noisy/env_com_obj_palha_0.png) | `env_com_obj_palha` | Palha (barulhento) | interativo | decor_atras | 1 · anim 4q | **apoio chao.** NoisyObject DryLeaves: perto do estÃ¡bulo e do feno. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Noisy/env_com_obj_sininhos_0.png) | `env_com_obj_sininhos` | Sininhos na passagem (barulhento) | interativo | decor_atras | 1 · anim 5q | **apoio chao.** NoisyObject Chimes: sininhos a 17-21 px do chÃ£o; agachado passa por baixo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Common/Noisy/env_com_obj_balde_0.png) | `env_com_obj_balde` | Balde de serviÃ§o (barulhento) | interativo | decor_atras | 1 · anim 5q | **apoio chao.** NoisyObject Bucket: cai uma vez correndo contra; volta ao lugar ao descansar no santuÃ¡rio. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Exterior

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Exterior/Terrain/tile_ext_solo_rocha_0_0.png) | `env_ext_ter_solo` | Solo natural (tileset) | plataforma | jogo | 112 | **apoio livre.** Escolhido por célula pelo TerrainAutotile: grama no topo, terra 1-2 tiles abaixo, rocha mais fundo; lados expostos com contorno e luz à direita (sol). Verdes azulados: longe da hera escalável e do matinho. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Terrain/tile_ext_rocha_rocha_0_0.png) | `env_ext_ter_rocha` | Rocha natural (tileset) | plataforma | jogo | 48 | **apoio livre.** Pedras do tutorial de pulo e paredes de rocha. Estratos caindo para a direita, contínuos entre tiles (fase x par/ímpar). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Terrain/env_ext_ter_rampa_11_l.png) | `env_ext_ter_rampa` | Rampa de solo (pedaços 1:1 e 1:2) | plataforma | jogo | 6 | **apoio livre.** Montada pelo SlopeSetup a partir de TerrainAutotile.SlopePieces: um pedaço por coluna na diagonal e tiles de solo embaixo. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Terrain/env_ext_ter_tufo_0.png) | `env_ext_ter_tufo` | Tufo de grama | decorativo | terreno_detalhe | 12 | **apoio chao.** Espalhado sozinho sobre o solo (TerrainAutotile.TopDetails): sobe 2-5 px e quebra a linha reta do chão sem mudar a colisão. Atrás do gato. |
| ![](../../Assets/_Game/Art/Environments/Exterior/env_ext_roc_seixo_0.png) | `env_ext_roc_seixo` | Seixos | decorativo | terreno_detalhe | 4 | **apoio chao.** Espalhados sozinhos sobre solo e rocha (0-1 por tile). |
| ![](../../Assets/_Game/Art/Environments/Exterior/env_ext_roc_fossil_a_cranio.png) | `env_ext_roc_fossil` | Fóssil (crânio, costelas, osso longo, coluna) | narrativo | terreno_detalhe | 4 | **apoio livre.** Só dentro da terra ou da rocha (no corte do terreno), nunca na superfície nem saindo de parede. Variações em ordem: 0 crânio, 1 costelas, 2 osso longo (14 px, cabe em 1 tile), 3 coluna. Ossos de bichos de antes do Despertar [PROPOSTA de lore]. |
| ![](../../Assets/_Game/Art/Environments/Exterior/env_ext_roc_pequena_a.png) | `env_ext_roc_pequena` | Pedra pequena | decorativo | decor_atras | 3 | **apoio chao.** Atrás do gato. Nunca na borda de um pulo (pode parecer degrau). |
| ![](../../Assets/_Game/Art/Environments/Exterior/env_ext_roc_media_a.png) | `env_ext_roc_media` | Pedra média | decorativo | decor_atras | 3 | **apoio chao.** Atrás do gato, mais escura que as pedras jogáveis (que são tiles). Em grupo com pequenas. |
| ![](../../Assets/_Game/Art/Environments/Exterior/env_ext_roc_rochedo_a.png) | `env_ext_roc_rochedo` | Rochedo | decorativo | decor_atras | 2 | **apoio chao.** Grande: só em fundo de cena, encostado na rocha ou meio dentro da lagoa. |

## Exterior/Objects

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Exterior/Objects/env_ext_obj_fogueira_apagada.png) | `env_ext_obj_fogueira_apagada` | Fogueira apagada | narrativo | decor_atras | 1 | **apoio chao.** AlguÃ©m acampou aqui antes do gato. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Objects/env_ext_obj_mochila.png) | `env_ext_obj_mochila` | Mochila esquecida | narrativo | decor_atras | 1 | **apoio chao.** Ao lado da fogueira apagada. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Objects/env_ext_obj_barraca_comercio.png) | `env_ext_obj_barraca_comercio` | Posto de comÃ©rcio estrangeiro | narrativo | decor_atras | 1 | **apoio chao.** RÃ©pteis e Aves negociavam fora dos muros: lona azul, potes de ervas, balanÃ§a, placa em vÃ¡rias escritas, pena de mensageiro. Abandonado. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Exterior/Vegetation

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_muda_a_0.png) | `env_ext_veg_muda` | Muda de árvore | decorativo | decor_atras | 3 · anim 4q/var | **apoio chao.** Mudas nascendo entre os tocos mostram que a manutenção da zona de tiro falhou (plano, 4.4). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_jovem_a_0.png) | `env_ext_veg_jovem` | Árvore jovem | decorativo | decor_atras | 3 · anim 4q/var | **apoio chao.**  |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_adulta_a_0.png) | `env_ext_veg_adulta` | Árvore adulta | decorativo | decor_atras | 3 · anim 4q/var | **apoio chao.** Folhagem atrás do gato; nunca na frente do caminho. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_ancia_a_0.png) | `env_ext_veg_ancia` | Árvore anciã | decorativo | decor_atras | 2 · anim 4q/var | **apoio chao.** Inclinada para a direita pelo vento da montanha (que sopra na direção do salto). Não espelhar. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_morta_a.png) | `env_ext_veg_morta` | Árvore morta | narrativo | decor_atras | 1 | **apoio chao.**  |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_tronco_caido_a.png) | `env_ext_veg_tronco_caido` | Tronco caído | decorativo | decor_atras | 1 | **apoio chao.**  |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_toco_a.png) | `env_ext_veg_toco` | Toco cortado | narrativo | decor_atras | 3 | **apoio chao.** A Ordem cortou as árvores perto do muro para ter linha de tiro (plano, 4.4). O toco b tem um broto. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_arbusto_fundo_a_0.png) | `env_ext_veg_arbusto_fundo` | Arbusto de fundo | decorativo | decor_atras (-7) | 3 · anim 2q/var | **apoio chao.** Maior e bem mais escuro que o matinho (32×16, #456633), que é esconderijo: este é só cenário. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_galho_frente_a.png) | `env_ext_veg_galho_frente` | Galho de primeiro plano | decorativo | fg | 2 · paralaxe 1/1 | **apoio teto.** Silhueta escura pendurada do alto da tela (plano, 3.5): no máximo 15% da tela, nunca no meio, nunca na frente do caminho. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_flor_0.png) | `env_ext_veg_flor` | Flores silvestres | decorativo | terreno_detalhe | 3 | **apoio chao.** Espalhadas sozinhas (TerrainAutotile.TopDetails, 5% do solo). Rosa, lilás e branco: nunca amarelo (moeda). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Vegetation/env_ext_veg_raiz_0.png) | `env_ext_veg_raiz` | Raiz pendurada | narrativo | terreno_detalhe | 3 | **apoio teto.** Colocadas sozinhas nos tetos da alvenaria abandonada (TerrainAutotile.CeilingDetails): a mata está entrando. |

## Exterior/Water

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_cachoeira_0.png) | `env_ext_ani_cachoeira` | Cachoeira (coluna) | decorativo | decor_atras (-6) | 1 · anim 4q | **apoio livre.** Gerada por WaterSpriteGen. Pedaços de 2 tiles empilhados, colados na face do rochedo (não por cima dela). Contorno ciano claro e corpo translúcido, como em Celeste. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_cachoeira_nascente_0.png) | `env_ext_ani_cachoeira_nascente` | Nascente da cachoeira (fenda na rocha) | narrativo | terreno_detalhe | 1 · anim 4q | **apoio livre.** Topo da cachoeira: a água sai de uma fenda na face do penhasco (a fenda fica sobre os tiles de rocha, x 0..10 do sprite) e faz a curva até virar a coluna (x 8..31). |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_cachoeira_espuma_0.png) | `env_ext_ani_cachoeira_espuma` | Espuma da cachoeira | decorativo | decor_frente (9) | 1 · anim 4q | **apoio livre.** Onde a cachoeira bate na lagoa: a linha da água fica na linha 9 do sprite. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_lagoa_0.png) | `env_ext_ani_lagoa` | Lagoa (tile de água) | decorativo | decor_frente (8) | 1 · anim 4q | **apoio livre.** Um tile por célula da bacia, na frente do gato (ele cai dentro). Superfície 2 px abaixo do topo do tile; as ondas emendam entre tiles. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_respingo_grande_0.png) | `env_ext_ani_respingo_grande` | Respingo grande | decorativo | decor_frente (9) | 1 · anim 6q | **apoio livre.** Toca uma vez (WaterSurface) quando o gato entra na água caindo forte (o salto de fé): coluna d'água, gotas que voltam e dois anéis. Só na água, nunca no chão. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_respingo_pequeno_0.png) | `env_ext_ani_respingo_pequeno` | Respingo pequeno | decorativo | decor_frente (9) | 1 · anim 4q | **apoio livre.** Toca uma vez quando o gato entra na água numa queda curta ou num pulo, com um anel. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_onda_pe_0.png) | `env_ext_ani_onda_pe` | Ondinha nos pés | decorativo | decor_frente (9) | 1 · anim 3q | **apoio livre.** A cada 0,4 s nos pés do gato andando dentro da água, na superfície. |
| ![](../../Assets/_Game/Art/Environments/Exterior/Water/env_ext_ani_anel_0.png) | `env_ext_ani_anel` | Anel na água | decorativo | decor_frente (9) | 1 · anim 3q | **apoio livre.** O círculo que se abre na superfície depois de um respingo (dois no respingo grande, um no pequeno). |

## Necro

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Necro/env_nec_veg_vaso_seco_a.png) | `env_nec_veg_vaso_seco` | Vaso com planta morta | narrativo | decor_atras | 2 | **apoio chao.** Perto do Tomo a vida some (plano, 6.3): plantas secas nos vasos da câmara. |

## Necro/Plague

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Necro/Plague/env_nec_veia_0.png) | `env_nec_veia` | Veias da praga | narrativo | decor_atras | 3 | **apoio parede.** Intensidade 1 (finas), 2 (galhos), 3 (grossas com pontos verdes). Sobe com a altura (6.3). Use Fixed com a intensidade - 1. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Necro/Plague/env_nec_fissura_vitral.png) | `env_nec_fissura_vitral` | Fissura roxa no vitral | narrativo | decor_frente | 1 | **apoio livre.** Por cima do vitral do salÃ£o: primeiro sinal da praga; os cacos ficam embaixo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Necro/Plague/env_nec_obj_inseto_morto_0.png) | `env_nec_obj_inseto_morto` | Insetos mortos | narrativo | decor_atras | 2 | **apoio chao.** CorrupÃ§Ã£o crescente (6.3): na galeria das armadilhas a praga jÃ¡ chegou. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Outdoors

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Outdoors/env_sky_sunset.png) | `bg_ceu_por_do_sol` | Céu do pôr do sol | decorativo | ceu | 1 · paralaxe 0/0 | Preso à câmera. Na Etapa 6 vira um dos 4 céus escolhidos pela altura. |
| ![](../../Assets/_Game/Art/Environments/Outdoors/env_mountains_far.png) | `bg_ext_cordilheira_cidadela` | Montanhas ao longe com a Cidadela | decorativo | bg_longe | 1 · paralaxe 0,06/0,06 | Etapa 6: refazer em 960 px e paralaxe vertical 0,15 (plano, 7.3). |
| ![](../../Assets/_Game/Art/Environments/Outdoors/env_hills_near.png) | `bg_ext_morros_pinheiros` | Morros com pinheiros | decorativo | bg_medio | 1 · paralaxe 0,15/0,15 | Repetição de 640 px aparece na tela: Etapa 6 refaz com 960 px e um acontecimento único por volta. |

## Primates

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Primates/env_pri_ter_placa_a.png) | `env_pri_ter_placa` | Placa da estrada negra | narrativo | terreno_detalhe | 3 | **apoio livre.** Meio enterrada no chão ou embutida na rocha, nunca saindo para fora (pareceria plataforma). Reta demais para ser natural: é o sinal de que algo muito antigo está embaixo. O cristal pulsa na Etapa 7. |

## Primates/Objects

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Primates/Objects/env_pri_obj_cristal_apagado.png) | `env_pri_obj_cristal_apagado` | Cristal Primata | narrativo | decor_atras | 1 | **apoio chao.** Apagado. Aceso (env_pri_obj_cristal_aceso_0..2) quando o gato chega a 3 tiles (Etapa 7: ReactOnProximity). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Primates/Objects/env_pri_obj_marco.png) | `env_pri_obj_marco` | Marco da estrada negra | narrativo | decor_atras | 1 | **apoio chao.** Pedra lisa demais para ser natural, com sulcos que jÃ¡ brilharam. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Primates/Objects/env_pri_obj_reliquia_0.png) | `env_pri_obj_reliquia` | RelÃ­quia Primata confiscada | narrativo | decor_atras | 3 | **apoio chao.** Uma em cada cofre: disco com glifos, mÃ¡scara, tabuleta. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

## Soluki

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Soluki/env_sol_obj_tenda.png) | `env_sol_obj_tenda` | Tenda do pátio | narrativo | decor_atras | 1 | **apoio chao.** É o que faz a sombra jogável embaixo dela (LightZone com a mesma largura). Toda sombra precisa de um motivo visível. |
| ![](../../Assets/_Game/Art/Environments/Soluki/env_sol_obj_toldo.png) | `env_sol_obj_toldo` | Toldo da arquibancada | decorativo | decor_atras | 1 | **apoio livre.** Bem acima do topo da arquibancada (sem parecer plataforma): cobre o lugar do atirador. |
| ![](../../Assets/_Game/Art/Environments/Soluki/env_sol_obj_poste_curto.png) | `env_sol_obj_poste_curto` | Poste de sustentação (1 tile) | decorativo | decor_atras | 1 | **apoio chao.** Embaixo da arquibancada, nas pontas: mostra que ela se apoia no chão. Atrás do gato (a passagem baixa continua livre). |
| ![](../../Assets/_Game/Art/Environments/Soluki/env_sol_obj_poste_toldo.png) | `env_sol_obj_poste_toldo` | Poste do toldo | decorativo | decor_atras | 1 | **apoio chao.**  |

## Soluki/Objects

| | id | nome | função | camada | variações | regra de uso |
|---|---|---|---|---|---|---|
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_banco.png) | `env_sol_obj_banco` | Banco de madeira | decorativo | decor_atras | 1 | **apoio chao.** Encostado em mesa, no varal ou perto dos bonecos: alguÃ©m senta aqui. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_balde_agua.png) | `env_sol_obj_balde_agua` | Balde de Ã¡gua com esponja | decorativo | decor_atras | 1 | **apoio chao.** Ãgua e esponja do treino: os recrutas lavam os bonecos. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_quadro_treino.png) | `env_sol_obj_quadro_treino` | Quadro da escala de treino | narrativo | decor_atras | 1 | **apoio chao.** Tracinhos de giz dos recrutas e uma pata desenhada. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_amolador.png) | `env_sol_obj_amolador` | Pedra de amolar | decorativo | decor_atras | 1 | **apoio chao.** Onde as lÃ¢minas de treino sÃ£o afiadas. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_aljava.png) | `env_sol_obj_aljava` | Aljava com flechas | decorativo | decor_atras | 1 | **apoio chao.** Defesa: perto de seteiras e atiradores. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_virotes.png) | `env_sol_obj_virotes` | Caixote de virotes | decorativo | decor_atras | 1 | **apoio chao.** MuniÃ§Ã£o da muralha. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_corda.png) | `env_sol_obj_corda` | Rolo de corda | decorativo | decor_atras | 1 | **apoio chao.** Corda de iÃ§ar. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_braseiro_0.png) | `env_sol_obj_braseiro` | Braseiro aceso | luz | decor_atras | 1 · anim 4q | **apoio chao.** SÃ³ onde jÃ¡ hÃ¡ luz (perto de tocha acesa): todo braseiro aceso precisa de LightZone iluminada em volta. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_beliche.png) | `env_sol_obj_beliche` | Beliche | decorativo | decor_atras | 1 | **apoio chao.** Cobertor carmesim da Ordem jogado na cama de baixo. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_bau.png) | `env_sol_obj_bau` | BaÃº | decorativo | decor_atras | 1 | **apoio chao.** Ao pÃ© da cama ou no depÃ³sito. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_mesa_dados.png) | `env_sol_obj_mesa_dados` | Mesa com a partida de dados | narrativo | decor_atras | 1 | **apoio chao.** Caneca, dados, cartas e a moeda da aposta largados no meio: a ronda chamou. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_varal_0.png) | `env_sol_obj_varal` | Varal com roupas | decorativo | decor_atras | 1 · anim 2q | **apoio chao.** Camisa, tabardo da Ordem, meia e toalha. BalanÃ§am 1 px sÃ³ onde hÃ¡ vento ou perto de seteira. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_panela_0.png) | `env_sol_obj_panela` | Panela no fogo baixo | decorativo | decor_atras | 1 · anim 3q | **apoio chao.** Vapor subindo: alguÃ©m vai voltar para comer. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_vassoura.png) | `env_sol_obj_vassoura` | Vassoura encostada | decorativo | decor_atras | 1 | **apoio chao.** Encostada na parede, no meio da limpeza. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_escala.png) | `env_sol_obj_escala` | Escala da guarda | narrativo | decor_atras | 1 | **apoio parede.** Pregada na parede, 1,5 tile acima do chÃ£o, com o selo carmesim. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_prateleira.png) | `env_sol_obj_prateleira` | Prateleira do arquivo | decorativo | decor_atras | 1 | **apoio chao.** Rolos, livros e caixas de arquivo dos fundos. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_barril.png) | `env_sol_obj_barril` | Barril | decorativo | decor_atras | 1 | **apoio chao.** DepÃ³sito e cozinha. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_caixote.png) | `env_sol_obj_caixote` | Caixote velho | decorativo | decor_atras | 1 | **apoio chao.** Decorativo (o esconderijo de caixotes Ã© outro objeto). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_sacos.png) | `env_sol_obj_sacos` | Sacos de grÃ£o | decorativo | decor_atras | 1 | **apoio chao.** Com o carimbo carmesim da Ordem. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_sol_obj_porta_fechada_0.png) | `env_sol_obj_porta_fechada` | Porta de madeira fechada | decorativo | decor_atras | 2 | **apoio chao.** Corredor que existe mas o gato nÃ£o usa: tranca com cadeado (v0) ou tÃ¡buas pregadas (v1). Nunca rachada nem dourada (nÃ£o Ã© parede quebrÃ¡vel). Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |
| ![](../../Assets/_Game/Art/Environments/Soluki/Objects/env_com_obj_teia.png) | `env_com_obj_teia` | Teia de canto | decorativo | decor_atras | 1 | **apoio teto.** SÃ³ em cantos de teto dos fundos e de salas esquecidas. Gerado por Tools/EnvironmentSprites/ObjectSpriteGen (Etapa 5). |

