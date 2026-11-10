# Mapa e dependências (grafo de progressão)

Gerado a partir de `Docs/Level/fortaleza-mvp.md`. Cada seta diz o que é preciso para passar.

```mermaid
graph TD
    M[1 Montanha<br/>início, y 60] -->|cair: mão única| T[2 Trilha]
    T -->|pular| P[3 Pátio de treino<br/>S1]
    P -.->|Paladino: porta rachada| E[Estábulo: cofre<br/>pós-MVP]
    P -->|agachar| P2[torre de escalada<br/>opcional: pulo de parede]
    P -->|escalar vinha 1| W1[4 Muralha 1]
    W1 -->|escalar vinha 2| W2[Muralha 2: vigia]
    W2 -->|escalar vinha 3| G[5 Galeria da muralha<br/>S2]
    G -->|pulo de parede| B[6 Torre do Sino<br/>S3]
    B --> H[7 Salão dos Votos]
    H -->|pular: fogo, serra, frágeis| A[8 Galeria das Armadilhas]
    A -->|pulo de parede| C[9 Câmara do Selo<br/>S4, chefe: Koda]
    C -->|vencer Koda| TO[Tomo: maldição + juramento<br/>= PALADINO]
    TO -->|Paladino: porta rachada| D[10 Passagem dos fundos]
    D -->|Paladino| V0[cofre do guardião]
    D -->|cair| Q[Poço: patamares 72, 43, 15]
    Q -->|Paladino| V1[cofres do poço]
    Q -->|Paladino: portão| FIM[Estrada para Miaravéu<br/>fim da demonstração]
```

## Requisitos por conexão

| De → para | Requisito |
|---|---|
| Montanha → Trilha | nenhum (queda, mão única) |
| Trilha → Pátio | pulo |
| Pátio → Muralha 1 | escalar |
| Pátio → Estábulo | Paladino (volta pós-MVP) |
| Muralhas → Galeria | escalar |
| Galeria → Torre do Sino → Salão | escalar + pulo de parede |
| Salão → Galeria das Armadilhas | pulo |
| Galeria das Armadilhas → Câmara | escalar + pulo de parede |
| Câmara → Tomo | vencer o chefe |
| Tomo → Passagem → Poço → Estrada | Paladino |

**Simulação:** só com o Ladino, tudo até a Câmara é alcançável. O Paladino é ganho antes do primeiro gate obrigatório de marreta. Nenhuma sala alcançável fica sem caminho de volta a um santuário: na descida, morrer devolve ao S4. **Sem softlock.**
