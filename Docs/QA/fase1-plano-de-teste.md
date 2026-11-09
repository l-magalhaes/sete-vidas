# Plano de teste — Fase 1 (controlador do gato)

Cena: `Scenes/Dev/Playground` · Game view em 1920×1080 · valores padrão de `PlayerMovementSettings`.

| ID | Sistema | Pré-condição | Passos | Resultado esperado | Status |
|---|---|---|---|---|---|
| MOV-01 | Spawn | Play | Esperar 1 s | Gato parado no chão em x ≈ 0,5, sem deslizar | ☐ |
| MOV-02 | Andar | Gato parado | Segurar D | Anda para a direita a ~7 tiles/s, sprite olhando para a direita | ☐ |
| MOV-03 | Andar | Gato andando | Soltar D | Para quase na hora (< 0,1 s), sem escorregar | ☐ |
| MOV-04 | Virar | Gato parado | Tocar A | Sprite espelha para a esquerda | ☐ |
| MOV-05 | Pulo | Ao lado do degrau de 4 tiles (x 20) | Segurar Espaço e ir para a direita | Sobe no degrau de 4 | ☐ |
| MOV-06 | Pulo | Ao lado do degrau de 5 tiles (x 24) | Segurar Espaço e ir para a direita | **Não** pousa direto no topo; agarra a quina (ver LED-01) | ☐ |
| MOV-07 | Pulo variável | No chão | Tocar Espaço rápido | Pulo visivelmente mais baixo (cerca de metade) | ☐ |
| MOV-08 | Coyote | Correndo para fora de uma borda | Apertar Espaço logo após sair | Pula | ☐ |
| MOV-09 | Jump buffer | Caindo | Apertar Espaço pouco antes de tocar o chão | Pula ao aterrissar | ☐ |
| MOV-10 | Sem pulo extra | No ar após pular | Apertar Espaço de novo | Nada acontece | ☐ |
| MOV-11 | Buracos | Correndo | Pular os buracos de 2, 3, 4 e 5 tiles | Vence todos; ao cair num deles, sai pulando | ☐ |
| MOV-12 | Agachar | No chão | Segurar S | Sprite baixo, velocidade ~metade | ☐ |
| MOV-13 | Passagem baixa | Agachado | Atravessar x 68–75 | Passa sem travar | ☐ |
| MOV-14 | Levantar sob teto | Agachado dentro da passagem | Soltar S | Continua agachado; levanta sozinho ao sair | ☐ |
| MOV-15 | Pular sob teto | Agachado dentro da passagem | Apertar Espaço | Nada acontece; não atravessa o teto | ☐ |
| MOV-16 | Degraus suspensos | Andando no chão | Passar por baixo dos degraus de 3 a 6 | Passa sem bater a cabeça nem travar | ☐ |
| MOV-19 | Emendas dos tiles | Agachado no chão | Andar agachado de x 14 a x 35 (sob os degraus suspensos) e voltar, várias vezes | Nunca trava numa direção | ☐ |
| LED-01 | Agarrar borda | Ao lado do degrau de 5 tiles (x 24) | Pular segurando D | Se pendura na quina, virado para a parede | ☐ |
| LED-02 | Subir da borda | Pendurado | Apertar Espaço | Sobe e fica de pé no topo em ~0,2 s | ☐ |
| LED-03 | Soltar a borda | Pendurado | Apertar S | Cai e não agarra a mesma quina na queda | ☐ |
| LED-04 | Sem quina | Caindo rente à parede escalável (x 80, 10 tiles) no meio dela | Segurar D | Não se pendura (não há quina livre) | ☐ |
| LED-05 | Sem direção | Caindo ao lado de uma quina | Não segurar direção | Não se pendura | ☐ |
| LED-06 | Ponta da borda | Parado no canto de um degrau, metade para fora | Soltar tudo | Não escorrega | ☐ |
| LED-07 | Degrau suspenso | Pular em direção à quina de um degrau de 3 a 6 tiles | Segurar a direção | Agarra e sobe; não atravessa a plataforma | ☐ |
| MOV-17 | Parede | Encostado na parede escalável | Segurar D e pular | Não gruda na parede; cai normalmente | ☐ |
| MOV-18 | Controle | Gamepad conectado | Analógico + botão sul + baixo | Mesmos resultados de MOV-02, 05 e 12 | ☐ |
| CAM-01 | Câmera | Andando | Atravessar a Playground | Segue com dead zone, sem tremer, tiles nítidos | ☐ |
| AUTO-01 | Testes | — | Test Runner > EditMode e PlayMode | Todos passam | ☐ |

**Casos-limite a observar:** apertar S e Espaço ao mesmo tempo pendurado; agarrar a borda logo depois de soltar outra; subir uma borda com um teto baixo logo acima (não deve agarrar); apertar S e Espaço no mesmo frame; soltar S exatamente ao sair da passagem; cair agachado de uma borda; pular rente ao teto dos degraus suspensos.

**Pronto quando** (documento "Primeiros passos"): alguém que nunca jogou atravessa a `Playground` sem frustração, e as métricas de pulo estão em `Docs/Level/metricas.md`.
