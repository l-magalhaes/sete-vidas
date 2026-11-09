# Armadilhas
**Pilar:** 2 (exploração) e 1 (identidade do Ladino: furtividade) | **Escopo:** MVP | **Status:** rascunho para teste

## Objetivo de experiência
O percurso tem perigos que se **leem antes de machucar**. Os ativos pedem plataforma e tempo certo; os inativos premiam o jogador cuidadoso: **quem anda agachado não ativa** fios nem placas, a mesma lógica da furtividade.

## Tipos
| Armadilha | Tipo | Aviso | Como passar | Ao tocar |
|---|---|---|---|---|
| **Espinhos** | sempre ativo | pontas de aço com ponta vermelha | pular por cima | **1 hit e volta ao último chão seguro** (sem recuo) |
| **Fio de alarme** | inativo | fio claro com marcas vermelhas na altura do peito, entre dois postes | **agachado passa por baixo** (ou pulando por cima) | dispara a armadilha ligada (flechas) e faz **barulho alto** |
| **Placa de pressão** | inativa | placa clara com marca vermelha no chão | **andando agachado não afunda** (ou pulando por cima) | dispara a armadilha ligada (lanças) e faz **barulho alto** |
| **Flechas** | disparada (ou automática) | brilho vermelho na boca do lançador (0,3 s) | voam na altura do peito: **agachado passam por cima** | 1 hit + recuo |
| **Lanças do chão** | disparada | tremor no chão (0,25 s) | sair de cima | 1 hit + recuo |
| **Jato de chama** | ativo, em ciclo | fagulhas no bocal (0,6 s) | **plataformas por cima** ou passar entre as chamas; os jatos vizinhos formam uma onda | 1 hit + recuo |
| **Serra** | ativa, vai e volta no trilho | girando, pausa nas pontas | pular por cima ou esperar na plataforma | 1 hit + recuo |
| **Plataforma frágil** | ativa ao pisar | rachaduras; treme 0,5 s antes de cair | **não parar** | cai (volta em 3 s) |

## Regras
1. **Último chão seguro:** o gato guarda o último lugar onde pisou firme: no chão (não em plataforma frágil), parado na vertical, sem estar escalando ou pendurado e a mais de 1,2 tile de qualquer armadilha. Os espinhos devolvem o gato para lá (tela pisca rapidinho, controle volta em ~0,3 s).
2. Se o hit do espinho for o último, é **morte normal** (volta ao santuário).
3. **Invencibilidade de 1 s** depois de qualquer hit, igual aos golpes de inimigos.
4. **Fio e placa** rearmam sozinhos em 3 s. Disparar faz barulho (raio 8, intensidade 0,6): **Sentinelas por perto ouvem e vêm investigar**.
5. **Morrer ou descansar** devolve as plataformas frágeis e rearma fios e placas.
6. Toda armadilha registra sua área de perigo; o "chão seguro" nunca fica colado numa delas.

## Parâmetros tunáveis (no Inspector de cada armadilha)
| Armadilha | Valores iniciais |
|---|---|
| Fio | altura 1,05 tile (entre agachado 0,875 e em pé 1,375), rearma 3 s |
| Flechas | rajada de 3, 0,25 s entre elas, 12 tiles/s, aviso 0,3 s |
| Lanças | 4 tiles de largura, 0,25 s de aviso, 0,6 s no alto, 1,6 tile de altura |
| Jato de chama | desligado 1,6 s · fagulhas 0,6 s · chama 1,2 s · altura 2 tiles · deslocamento 0,35 s por jato |
| Serra | 4 tiles/s, pausa 0,4 s, raio 0,55 |
| Plataforma frágil | treme 0,5 s, volta em 3 s |

## Regras de level design
- **Plataformas sobre o fogo** com o topo a pelo menos 1 tile acima da chama (chama 2 → plataforma com topo em 3).
- **Fio com passagem baixa logo depois** força o agachar (senão o jogador pula o fio por cima).
- **Espinhos** não podem ficar a menos de ~2,5 tiles de outra armadilha dos dois lados, senão o "chão seguro" fica longe demais.
- Aviso sempre **antes** do perigo e legível em menos de 1 s (vermelho = perigo, só em armadilhas).

## Implementação (Unity)
- `Scripts/World/Traps`: `TrapRules` (C# puro: ciclo, placa, fio, registro de áreas), `TrapBase`, `SpikeStrip`, `TrapTrigger` (fio/placa), `TrapEffect`, `ArrowLauncher` + `TrapArrow`, `SpearTrap`, `FlameJet`, `SawBlade`, `CrumblingPlatform`.
- Gato: `SafeGroundTracker` + `PlayerHealth.TakeHazardDamage`.
- Prefabs em `Prefabs/Traps` para usar nas salas da Fortaleza (Fase 6). Ligue o fio/placa às armadilhas no campo `effects`.

## Critérios de aceite
- Dado o gato encostando nos espinhos, então perde 1 hit e reaparece no último chão firme antes deles.
- Dado o gato agachado, quando passa pelo fio e pela placa, então nada dispara; de pé, o fio dispara as flechas e a placa as lanças.
- Dados os jatos de chama, então é possível atravessar sem dano só pelas plataformas.
- Dada uma plataforma frágil, quando o gato fica em cima, então ela cai depois de ~0,5 s e volta em 3 s.

## Perguntas abertas
- [PROPOSTA] Espinhos tiram 1 hit (não matam na hora).
- [PROPOSTA] Disparar armadilha faz barulho que alerta Sentinelas.
- [PROPOSTA] O Paladino (mais pesado) também não afunda a placa agachado — a regra vale para o gato em qualquer classe.
