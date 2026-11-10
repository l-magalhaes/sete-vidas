# Moedas e a bolsa do Lion

**Pilar:** exploração (recompensa por desvios e rotas difíceis) | **Escopo:** MVP (a loja vem depois) | **Status:** implementado, aguarda playtest

## Regra
- **Bolsa:** o Lion guarda as moedas na bolsa de couro do figurino. O saldo fica no save (`SaveData.coins`) e **nunca se perde**: nem ao morrer, nem ao fechar o jogo.
- **Moedas no mundo:** giram e flutuam; vão para a bolsa ao encostar.
  - Cada uma tem um **id** e fica marcada no save (`moeda:<id>`): **não volta** ao descansar nem ao morrer.
  - Moedas sem id (soltas por inimigos, no futuro) não ficam marcadas.
- **Moeda do Contratante:** a **primeira moeda** do jogo. É a "moeda de pagamento" que a capivara joga ao lado do gato caído.
  - Vale **100 moedas**.
  - Só pode ser pega **depois do juramento com Koda**. No meio da cena, o gato está caído.
  - Ao pegá-la, toca a fala **D-MOEDA-01**.
  - Se o save foi feito antes de pegá-la, ela continua no chão da sala do tomo.
- **Loja (futura):** usa `CoinPurse.CanAfford(preço)` e `CoinPurse.TrySpend(preço)`. Gastar salva na hora; sem saldo, não gasta nada.
- **Teto:** 99.999 moedas.

## D-MOEDA-01 — A moeda do Contratante (obrigatório, uma vez)
SISTEMA: Uma moeda de ouro pesada, cunhada com um olho roxo. Vale 100 moedas comuns.
GATO (irônico): Cem moedas por seis vidas. Já fiz negócios piores. Não lembro quando.
GATO: Vou guardar. Um dia eu devolvo... com juros.

> O **olho roxo** cunhado é a marca de quem fez a moeda: pista para o Vertical Slice (de onde vem o ouro da capivara). **[PROPOSTA]**

## Onde ficam as moedas (MVP)

**Fortaleza** (8 moedas de 1, nos desvios e nas rotas difíceis):

| Id | Lugar | Posição (x, y) | O que pede |
|---|---|---|---|
| `fort_pedra` | Em cima da pedra alta do tutorial | 15; 3,7 | pulo |
| `fort_fosso` | No ar, sobre o fosso de espinhos | 21,5; 3,2 | pulo no tempo certo (risco) |
| `fort_passagem` | Dentro da passagem baixa | 45; 0,5 | agachar |
| `fort_hera` | No meio da hera da muralha | 71,4; 9 | escalar |
| `fort_passarela` | Na passarela alta do corredor | 92; 19,6 | rota furtiva por cima |
| `fort_salao` | No fim do salão das duas sentinelas | 140; 14,6 | passar pela luta |
| `fort_camara` | Antes da câmara do Koda | 182,5; 14,6 | passar pelas armadilhas |
| `fort_saida` | Atrás da parede rachada | 84; 0,6 | voltar com o Paladino |

**Playground:** 3 moedas de teste (`pg_inicio`, `pg_mato`, `pg_furtividade`).

As posições foram tiradas de `Docs/Level/fortaleza-mvp.md`. Conferir no editor se alguma ficou dentro de parede e ajustar.

## Interface
- Contador no canto superior esquerdo, **abaixo dos corações e dos orbes da cura**: ícone de moeda e o número.
- Ao ganhar, o número dá um pulinho e aparece "+N" em dourado.
- No mundo: "+1 moeda" / "+100 moedas" sobe da moeda.

## Código
- `Scripts/Progression/CoinPurse.cs`: saldo, `Add`, `CanAfford`, `TrySpend` e o evento `Changed`.
- `Scripts/World/CoinPickup.cs`: moeda no mundo (valor, id, giro, coleta, fala opcional). Prefab `Prefabs/Interactables/Coin.prefab`.
- `Scripts/UI/CoinHud.cs`: contador (no prefab HUD).
- `CursedTome`: trava a moeda do Contratante na cena e a libera depois do juramento.
- Arte: `Tools/CharacterSprites/ItemSpriteGen.cs` (rode `gerar.ps1`):
  - moeda comum 7×7 girando;
  - moeda do Contratante 10×10 com o olho roxo;
  - ícone `ui_coin` do HUD.
  - Prévia: `SeteVidasDocs/Referencias/moedas.png`.
- Testes:
  - `CoinTests`: bolsa, moeda que não volta e HUD;
  - `BossTests`: a moeda do Contratante vale 100 e fica travada durante a cena.
