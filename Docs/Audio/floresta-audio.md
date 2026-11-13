# Floresta Silenciosa — plano de áudio (F0)

**Skill:** `sete-vidas-sound-design` · **Status:** [PROPOSTA] · **Data:** 13/11/2026
Regras e convenções de `plano-sfx.md` continuam valendo (nomes `sfx_<grupo>_<nome>_<nn>.wav`, `amb_<lugar>_<nome>.ogg`, telégrafo com a mesma duração do aviso visual, 3–5 variações).
Músicas: compostas pelo usuário (como as da Fortaleza) e tocadas pelo `MusicDirector`. Este documento é o **briefing** de cada faixa.

## Pilar da fase: o silêncio cresce

A Floresta é "Silenciosa": quanto mais perto do Coração, **menos som**. Raízes da Queda têm a floresta viva; o Ninho tem só a respiração. O jogador deve sentir o silêncio como perigo.

## Sons dos infectados [APROVADO 13/11/2026]

**Por enquanto, todo inimigo infectado usa os sons do Templário Zumbi** que já existem no `Sfx` (`zumbi_passo`, `zumbi_ocioso`, `zumbi_sente`, `zumbi_caca`, `zumbi_dano`, `zumbi_morte`, `zumbi_toque`). Sons próprios por criatura vêm depois. A regra do timbre da praga não muda: ele continua só no tomo, na maldição e na Capivara; a Isyathy o usa a partir da Fase II.

## Músicas

| Id | Papel | Cue (`MusicCue`) | Briefing |
|---|---|---|---|
| MUS-F01 | Estrada de Soluki | Road | Variação sombria do tema da Fortaleza, em tom menor, metais abafados; marcha cansada. 90 BPM |
| MUS-F02 | Fuga da ponte | (stinger + loop curto) | Percussão acelerando, cordas em ostinato, 140 BPM; termina no **corte seco** da cutscene 1 |
| MUS-F03 | Floresta (exploração) | Forest | Harpa/pizzicato e flauta, muito espaço entre as notas; calma e curiosa. 80 BPM. Loop longo (≥ 2 min) para a coleta |
| MUS-F04 | Ruínas dos Primatas | Ruins | Pad sintético frio, arpejo lento como máquina antiga; **o único timbre "eletrônico"** do jogo até agora |
| MUS-F05 | Floresta corrompida | Blight | MUS-F03 desafinada e com o timbre da praga; percussão orgânica irregular |
| MUS-F06 | Furtividade na floresta | Stealth | Grave sustentado + batidas de madeira suaves; mesmo tom de MUS-F03 para crossfade |
| MUS-F07 | Combate na floresta | Combat | Percussão de madeira, cordas rápidas, mesmo tom de MUS-F03 |
| MUS-F08 | Ninho Adormecido | Nest | **Quase silêncio**: um drone muito baixo que some até E7. A respiração da Isyathy é a "música" |
| MUS-F09 | Isyathy | Isyathy | 3 camadas por fase (I: graves e tambores; II: + coro e timbre da praga; III: tudo, mais rápido). Na derrota (morte), a música para de vez; silêncio total antes do espírito |
| MUS-F10 | Tema da Vulpis | Vulpis | Flauta solo, melodia curta e direta (como uma flecha); entra na cutscene do resgate e nas flechas da luta (motivo de 3 notas) |
| MUS-F11 | Tema da Tobi | Tobi | Fagote e pizzicato em chiptune, cômico e saltitante; acompanha a cutscene 2 com "hits" nas batidas |
| MUS-F12 | Ponte reconstruída | (jingle) | 4–6 s, fanfarra pequena de madeira e sinos; resolve no tema principal |
| MUS-F13 | Santuário da floresta | Sanctuary | Versão de `mus_santuario` com harpa e vagalumes |
| MUS-F14 | Espírito da Isyathy | IsyathySpirit | O tema da Isyathy reescrito em paz: só coro suave e harpa, sem graves nem praga. Toca na aparição do espírito e nas conversas opcionais |

**Transições:** fase ↔ furtividade ↔ combate por crossfade (mesmos tempos da Fortaleza: 0,5 s para entrar no combate, 2,5 s para sair, 1,5 s na furtividade). Região corrompida: crossfade lento (4 s) de Forest para Blight conforme o gato avança.

## Ambiências

| Arquivo | Onde | Conteúdo |
|---|---|---|
| `amb_floresta_viva.ogg` | A, B, D | vento nas folhas, grilos, coruja distante, água pingando, rangidos de madeira |
| `amb_floresta_ruinas.ogg` | A6, B4, C3, C4 | goteiras em metal, ressonância metálica, zumbido elétrico raro |
| `amb_floresta_corrompida.ogg` | C7, partes de C e E | ar rarefeito, estalos orgânicos, bolhas, nenhum animal |
| `amb_ninho.ogg` | E | quase nada: vento muito baixo e eco |
| `amb_estrada.ogg` | Estrada | vento aberto, tochas crepitando, gemidos distantes |

Pontuais (`AmbientEmitter`): coruja, galho caindo, sapo no lago (C5), máquina dos Primatas (bip lento).

## Respiração da Isyathy (camada principal do ninho)

| Alerta | Respiração |
|---|---|
| 0–24 | ciclo de 4 s, inspiração grave longa, quase infrassom (60–120 Hz) |
| 25–49 | ciclo de 2,5 s, mais alta |
| 50–79 | ciclo de 1,5 s + escamas raspando + galhos rangendo |
| 80–100 | rugido grave subindo (1,2 s, a mesma duração do aviso do bote) |

O som é espacializado pela posição do corpo no fundo. **Prioridade máxima** no ninho, junto com os passos do gato.

## SFX novos

| Grupo | Sons |
|---|---|
| Mundo | `sfx_mundo_tronco_golpe` (×4, grave e seco), `sfx_mundo_tronco_quebra`, `sfx_mundo_tronco_adaga` ("tunk"), `sfx_mundo_bau_abre`, `sfx_mundo_madeira_pega` (jingle curto), `sfx_mundo_galho_treme`, `sfx_mundo_galho_quebra`, `sfx_mundo_raiz_refaz`, `sfx_mundo_alavanca_primatas`, `sfx_mundo_porta_metal`, `sfx_mundo_cipo_solta`, `sfx_mundo_corda_desce` |
| Gato | `sfx_gato_pogo_espinho` (tom cristalino turquesa), `sfx_gato_pogo_cacto` ("boing" abafado), `sfx_gato_pogo_carne` (planta, galho, serpente), `sfx_gato_queda_galhos` (cutscene 1) |
| Inimigos | Por inimigo (ver `Docs/Design/floresta-bestiario.md`): aviso, ataque, dano, morte da versão normal. **Infectados: sons do zumbi** por enquanto (ver acima) |
| Isyathy | `sfx_isyathy_respira` (4 níveis), `sfx_isyathy_aviso_bote`, `sfx_isyathy_bote`, `sfx_isyathy_mordida`, `sfx_isyathy_varredura`, `sfx_isyathy_impacto`, `sfx_isyathy_cuspe`, `sfx_isyathy_esporos`, `sfx_isyathy_rugido`, `sfx_isyathy_dano`, `sfx_isyathy_fuga` |
| Vulpis | `sfx_vulpis_arco_tensiona` (0,5 s, aviso para o jogador), `sfx_vulpis_flecha_voa`, `sfx_vulpis_flecha_crava` |
| Ponte | `sfx_ponte_pedra_racha`, `sfx_ponte_pedra_cai` (×3), `sfx_ponte_desaba` (cutscene 1), `sfx_ponte_madeira_range` |
| Tobi (cômicos) | `sfx_tobi_chave_gigante` ("clank" exagerado), `sfx_tobi_cambaleia`, `sfx_tobi_cava`, `sfx_tobi_emerge` ("pop"), `sfx_tobi_martelo_rapido`, `sfx_tobi_serrote`, `sfx_tobi_broca_gira`, `sfx_tobi_plink` (×4), `sfx_tobi_bolso` |
| Vozes | `vox_vulpis_*` (atenção, irônica, esforço), `vox_tobi_*` (animada, ofendida, risada), `vox_isyathy_espirito_*` (sussurro grave com eco suave, sem palavras; acompanha o texto) |

## Mixagem (prioridades)

1. Telégrafos de perigo (aviso do bote, arco da Vulpis tensionando, aviso dos inimigos)
2. Respiração da Isyathy (no ninho)
3. Passos e ações do gato
4. Golpe e quebra de tronco
5. Música
6. Ambiência

Cutscene 2 (Tobi): os SFX cômicos sobem +3 dB acima do normal (pedido do plano: exagerados).
