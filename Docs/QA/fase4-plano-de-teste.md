# Plano de teste — Fase 4 (vida, morte, santuário e save)

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080.
Antes: rode **Tools > Sete Vidas > 7. Fase 4** e depois **Tools > Sete Vidas > Save > Apagar save** (começa um jogo novo).
Santuários: x 3 (início) e x 119,5 (antes da Sentinela). Atalhos de depuração: selecione o `Player` durante o Play e use o menu ⋮ do componente `PlayerHealth` ("+1 coração", "tomar 1 hit").

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| HP-01 | HUD | Jogo novo | Play | 1 coração cheio e 6 escurecidos no canto superior esquerdo, nítidos | ☐ |
| HP-02 | Dano | Jogo novo | Levar 1 estocada da Sentinela | Coração pela metade, "-1 hit", recuo e pisca ~1 s | ☐ |
| HP-03 | Invencibilidade | Logo após HP-02 | Levar outro golpe em menos de 1 s | Não conta | ☐ |
| HP-04 | Morte | Com meio coração | Levar outro golpe | Tomba, tela escurece, volta ao início (x 0,5) com vida cheia | ☐ |
| HP-05 | Sem controle morto | Durante a morte | Apertar teclas | Nada acontece | ☐ |
| HP-06 | Inimigos ignoram o morto | Durante a morte | Observar a Sentinela | Não continua atacando o corpo | ☐ |
| HP-07 | Mais corações | ⋮ "+1 coração" duas vezes | Olhar o HUD; levar 3 golpes | 3 corações; com 3 hits: cheio, meio, vazio | ☐ |
| DRP-01 | Drop | Sentinela selecionada no Play | Menu ⋮ do `EnemyAI` > "Depuração: soltar meio coração" | Um meio coração salta e cai no chão, flutuando | ☐ |
| DRP-02 | Vida cheia | Vida cheia | Encostar no meio coração | Nada acontece; ele continua no chão | ☐ |
| DRP-03 | Cura | Com meio coração faltando | Encostar | "+1 hit", coração cheio de novo; o item some | ☐ |
| DRP-04 | Nunca excede | 2 corações (⋮ "+1 coração"), faltando 1 hit | Encostar | Volta a 4 hits; nunca 5 | ☐ |
| DRP-05 | Some | Deixar no chão | Esperar ~12 s | Pisca nos últimos 3 s e some | ☐ |
| DRP-06 | Reinício | Meio coração no chão | Descansar no santuário | O item some | ☐ |
| DRP-07 | Chance baixa | Matar a Sentinela ~20 vezes (ela volta em ~7 s) | Contar os drops | Por volta de 3 (15%) | ☐ |
| SNC-01 | Aviso | Andar até o santuário de x 119,5 | Ficar perto / se afastar | "E: descansar" aparece e some | ☐ |
| SNC-02 | Descansar | Com vida faltando, perto do santuário | Apertar E | Vida cheia, "Salvo", velas e olhos acendem | ☐ |
| SNC-03 | Encostar não salva | Santuário apagado | Passar por ele sem apertar E e morrer | Volta ao santuário anterior (ou ao início) | ☐ |
| SNC-04 | Checkpoint | Descansou no de x 119,5 | Morrer em qualquer lugar | Volta nele | ☐ |
| SNC-05 | Inimigos voltam | Matar a Sentinela | Descansar no santuário | Ela reaparece no início da patrulha | ☐ |
| SNC-06 | Inimigos voltam ao morrer | Matar a Sentinela, depois morrer | — | Ela reaparece | ☐ |
| SNC-07 | Troca de checkpoint | Descansou no de x 119,5 | Voltar e descansar no de x 3; morrer | Volta no de x 3; só ele fica aceso | ☐ |
| SAV-01 | Continuar | Descansou no de x 119,5 | Parar o Play e apertar Play de novo | Começa no santuário de x 119,5 | ☐ |
| SAV-02 | Corações salvos | "+1 coração" (grava) | Parar e dar Play | Continua com 2 corações | ☐ |
| SAV-03 | Apagar | Tools > Sete Vidas > Save > Apagar save | Play | Jogo novo: 1 coração, começa em x 0,5 | ☐ |
| SAV-04 | Arquivo | Tools > Sete Vidas > Save > Abrir pasta do save | Abrir `save_slot1.json` | JSON legível com os campos da spec | ☐ |
| SAV-05 | Save corrompido | Editar o JSON e apagar metade | Play | Jogo novo sem erro; aparece `save_slot1.json.corrupt` e um aviso amarelo no Console | ☐ |
| AUTO-04 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam (os testes usam um save temporário: o seu não é tocado) | ☐ |

**Casos-limite (QA):** morrer exatamente ao tocar o santuário (não salvou, volta ao anterior); apertar E no mesmo frame em que leva o golpe fatal (morto não descansa); levar dano pendurado ou escalando (solta e recua); morrer caindo de uma borda logo após descansar; recuo empurrando o gato para dentro de uma parede ou para fora da passarela; fechar o Play durante a tela preta da morte (o save não muda ao morrer, então nada se perde).

**Pronto quando** (documento "Primeiros passos"): você morre, volta ao santuário, fecha o jogo, abre de novo e continua de onde salvou. Os testes passam no Test Runner.
