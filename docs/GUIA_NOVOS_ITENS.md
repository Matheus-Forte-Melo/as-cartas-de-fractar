# Guia: adicionar itens manualmente

Este fluxo descreve como incluir um item novo no catálogo usado pela loja, pelo save e pelo combate. Detalhes de implementação estão em `Assets/Scripts/Items/` e em `context.md` §11.

---

## 1. Entrada no catálogo (obrigatório)

Editar:

`Assets/StreamingAssets/Items/item_catalog.json`

Acrescentar um objeto no array `items` com os campos:

| Campo | Descrição |
|-------|-----------|
| `id` | Identificador único, preferir snake_case (ex.: `ring_fire`). Usado no save e na loja. |
| `displayName` | Nome na UI da loja. |
| `description` | Texto do cartão / verso. |
| `type` | `"attack"`, `"defense"` ou `"consumable"`. |
| `price` | Custo em moedas. |
| `icon` | Caminho relativo a `Resources`, sem extensão (ex.: `"Icons/Espada de Ouro"`). |
| `attackMultiplier` | Só relevante para `attack`: multiplicador de dano (ex.: `1.15`). |
| `bonusHealth` | Só relevante para `defense`: bónus de vida máxima. |
| `consumableEffect` | Só `consumable`: string do efeito (hoje: `"heal"`). |
| `consumableValue` | Só `consumable`: valor numérico (ex.: cura com `heal`). |

O `ItemCatalog` carrega este ficheiro (lazy). Após alterar o JSON, reinicia o jogo ou recarrega a cena para ver mudanças.

---

## 2. Ícone (se for asset novo)

Colocar o sprite em `Assets/Resources/Icons/` (ou subpasta) e garantir que o campo `icon` no JSON corresponde ao nome do asset acessível via `Resources.Load`.

---

## 3. Comportamento por tipo

### Ataque (`type: "attack"`)

- Compra na loja → id vai para `SaveData.ownedItemIds`.
- `PlayerItemStats.CalculateDamageMultiplier` multiplica pelos `attackMultiplier` de todos os itens de ataque possuídos.

### Defesa (`type: "defense"`)

- Compra → `ownedItemIds` + opcionalmente `bonusHealth` aplicado à vida no momento da compra (loja).
- `PlayerItemStats.CalculateMaxHealth` soma `bonusHealth` de todos os itens de defesa possuídos.

### Consumível (`type: "consumable"`)

- Compra → entra em `SaveData.consumableSlots` (máximo **3** entradas; não stack; mesmo `id` pode repetir em slots diferentes). **Não** usa `ownedItemIds`.
- Efeitos em combate: ver `ConsumableBattleEffects.cs` e `ItemDefinition.ParseConsumableAction()`.
- Hoje só `"heal"` está implementado (`consumableValue` = pontos de cura, respeitando `MaxHealth`).

Para um **novo efeito** de consumível:

1. Adicionar valor em `ConsumableActionType.cs`.
2. Mapear a string em `consumableEffect` em `ItemDefinition.ParseConsumableAction()`.
3. Implementar o efeito em `ConsumableBattleEffects.TryApply()`.

---

## 4. Enum `ItemId` (opcional)

`Assets/Scripts/Items/ItemId.cs` e o `switch` em `ItemDefinition.ItemId` são **opcionais** para código que prefira enum em vez de string. A loja e o save funcionam só com o `id` do JSON.

---

## 5. Verificação rápida

- Loja: item aparece na grelha (`ItemCatalog.All`).
- Ataque/defesa: comprar e iniciar combate — vida máxima / multiplicador devem refletir `PlayerItemStats`.
- Consumível: comprar com slots livres; em combate, teclas **1 / 2 / 3** usam os slots (só no turno do jogador).

---

## Ficheiros de referência

| Área | Ficheiro |
|------|----------|
| Dados | `Assets/StreamingAssets/Items/item_catalog.json` |
| Definição / parse | `Assets/Scripts/Items/ItemDefinition.cs` |
| Catálogo | `Assets/Scripts/Items/ItemCatalog.cs` |
| Stats do jogador | `Assets/Scripts/Items/PlayerItemStats.cs` |
| Efeitos em batalha (consumível) | `Assets/Scripts/Items/ConsumableBattleEffects.cs` |
| Loja | `Assets/Scripts/Store/StoreController.cs` |
| Save | `Assets/Scripts/Core/Save/SaveData.cs` |
