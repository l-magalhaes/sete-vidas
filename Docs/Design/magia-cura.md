# Magia de cura (Paladino)

**Pilar:** 1 (sete corações, sete classes) | **Escopo:** MVP | **Status:** implementado, aguarda playtest

## Regra
- **Desbloqueio:** junto com a classe **Paladino**, no vínculo com Koda (D-GOLDEN-01-SISTEMA avisa: "Cura aprendida! Aperte F para recuperar 1 coração. 3 usos; o santuário recarrega.").
- **Efeito:** cura **1 coração** (2 hits), sem passar do máximo de corações recuperados, e **corta o veneno** (melhorias 14/11/2026). Envenenado e de vida cheia, a cura também vale (gasta o uso só para tirar o veneno).
- **Usos:** **3**. Quando acabam, só voltam ao **descansar num santuário** (`Sanctuary.Rest`). Morrer e renascer no santuário **não** recarrega. **[PROPOSTA de ajuste no playtest]**
- **Classe:** funciona em **qualquer classe** depois de aprendida. É uma magia que o Lion aprendeu com Koda, não um golpe da marreta. **[PROPOSTA]**
- **Controle:** **F** no teclado, **gatilho esquerdo (LT)** no controle (ação `Heal` em `GameControls`).

## Como funciona
1. Aperta F no chão, fora de ataque, com a vida incompleta.
2. **Concentração de 0,45 s:** o gato para, junta as patas no peito e fecha os olhos (animação `heal` do Lion). Centelhas douradas sobem até o peito.
3. **Cura:** o gato brilha em dourado, um anel de luz se abre, "+2 hit" aparece e um uso é gasto.
4. **Se tomar dano na concentração,** a cura é interrompida e **o uso não é gasto.**
5. **Não gasta uso quando:** a vida está cheia, ele está no ar, pendurado ou escalando, atacando, em diálogo, morto ou caído.

## Interface
- Três **orbes dourados** logo abaixo dos corações; cheio = uso disponível, escuro = gasto.
- Só aparecem depois que a magia é aprendida (`HeartsHud`).

## Dados e save
- `SaveData.healCharges` (0 a 3; saves antigos começam com 3). Gasto em memória e gravado no próximo salvamento; o santuário recarrega e salva.
- Ajustes no Inspector (`HealingSpell` no prefab Player): `maxCharges` 3, `hitsPerCast` 2, `castTime` 0,45 s.

## Código
- `Scripts/Player/HealingSpell.cs`: regra, concentração, interrupção e recarga.
- `PlayerVisual.HealFx` (efeitos) e a animação `heal` em `Tools/LionSprites`.
- `HeartsHud`: os orbes.
- `Sanctuary.Rest`: recarga.
- Testes: `Tests/PlayMode/HealingSpellTests.cs`:
  - bloqueada sem o Paladino;
  - 3 curas e depois sem usos;
  - recarga no santuário e no save;
  - vida cheia não gasta;
  - dano interrompe sem gastar.

## Critérios de aceite
- Dado o Paladino desbloqueado e a vida incompleta, apertar F no chão cura 2 hits e gasta 1 uso.
- Dado 0 usos, F não faz nada até descansar num santuário, que volta para 3.
- Dado dano durante a concentração, a vida não sobe e o uso continua.
