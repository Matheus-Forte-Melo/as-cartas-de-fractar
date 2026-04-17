# Ferramenta de onboarding (Tutorial.Onboarding)

Guia curto para quem for estender ou ligar novos tutoriais no projeto. O núcleo fica em `Assets/Scripts/Tutorial/Onboarding/`.

## Peças principais

| Peça | Função |
|------|--------|
| **`TutorialManager`** | Orquestra passos, overlay (spotlight + bloqueio de cliques), tooltip com “Continuar”, tecla Esc = pular. |
| **`TutorialStepDefinition`** | Um passo: texto, `RectTransform` alvo (opcional), modo de avanço, padding do furo, etc. |
| **`TutorialAdvanceMode`** | `OnEvent` (espera `EventBridge`), `AfterDelay`, `ContinueButton`. |
| **`EventBridge`** | `EventBridge.TriggerEvent(string id)` dispara eventos; o passo atual com `OnEvent` compara com `advanceWhenEventId`. |
| **`TutorialProgressTracker`** | Lê/grava **`SaveData.main_tutorial_step_index`** e **`main_tutorial_completed`** em `save.json` (sem PlayerPrefs). |
| **`TutorialOnboardingJsonLoader`** | Monta a lista de passos a partir de JSON em `StreamingAssets` + ficheiro de strings separado. |

## Fluxo típico numa cena

1. **Execution order**: um bootstrap (ex. `-500`) deve chamar `TutorialManager.LoadStepsFromStreamingAssets(...)` **antes** do `Awake` do `TutorialManager` (ex. `-400`), para `_steps` já vir preenchido quando o manager criar o canvas em runtime.
2. No **`TutorialManager`**: `_tutorialId` único por fluxo; `_autoStart` liga o fluxo no `Start`.
3. **`_respectCompletedFlag`**: se ligado, não reabre o onboarding se `save.json` tiver **`main_tutorial_completed`**.
4. **Persistência:** passo e conclusão ficam só no save (`main_tutorial_step_index`, `main_tutorial_completed`). Ao concluir ou pular, grava **`main_tutorial_completed = true`**. `ResetTutorialProgress()` zera o passo e define **`main_tutorial_completed = false`** (útil em dev).

## JSON em StreamingAssets

- **Passos**: array `steps` com `stepId`, `targetPath` (nome do objeto sob o Canvas ou caminho `Pai/Filho`), `stringKey`, `advanceMode` (0 = evento, 2 = botão Continuar), `advanceWhenEventId` quando for evento, offsets, **`tooltipPanelWidth` / `tooltipPanelHeight`** (opcionais; ambos &gt; 0 definem o tamanho do modal em px — se omitidos ou zero, usa-se **520×160**), etc.
- **Textos**: arquivo à parte (`strings[]` com `key` / `text`) para editar copy sem mexer na estrutura dos passos.

O loader resolve `targetPath` a partir do `RectTransform` raiz do jogo (normalmente o `Canvas` da cena).

## Eventos a partir do jogo

Nos controllers, após ações relevantes, chamar por exemplo:

```csharp
EventBridge.TriggerEvent(TutorialBlackjackEventIds.PlayerHit);
```

Os IDs estáveis do blackjack de tutorial estão em `TutorialBlackjackEventIds`.

## Save (`save.json`)

- **`main_tutorial_step_index`**: retomada do passo do onboarding principal.
- **`main_tutorial_completed`**: quando `true`, o fluxo guiado não reabre (se `_respectCompletedFlag` estiver ligado).

## Reativar o onboarding em dev

- Chamar **`ResetTutorialProgress()`** no `TutorialManager` (zera passo e `main_tutorial_completed` no save), ou editar/apagar `save.json` manualmente.

## Referência de cena

A cena `TutorialDefaultBlackjack` liga `DefaultBlackjackGuidedSession` + `TutorialManager` + UI guiada; mesa forçada e passos vêm dos JSON em `StreamingAssets` com prefixo `tutorial_blackjack_guided_*`. O componente **`GuidedBlackjackNarrative`** é adicionado em runtime pelo session para o passo `dealer_watch` (esconder overlay e forçar a vez da mesa).

## Evento `tutorial.bj.dealer_turn_visual_done`

Disparado pelo controller de tutorial ao **terminar** a corrotina da mesa. Um passo com `advanceMode` = evento e `advanceWhenEventId` igual a esse id avança depois da jogada visível.

## Como posicionar a **área clicável** em cima de um elemento

O recorte (“buraco”) do `SpotlightOverlay` + `TutorialBlocker` é calculado a partir do `RectTransform` indicado no `target` do passo. Todos os cliques **dentro desse rect** passam; tudo à volta é bloqueado pelas 4 barras. Há dois casos:

### 1) Alvo é um elemento de UI já existente (como no blackjack)

- **Ex.:** os passos do blackjack apontam para `DealerHand`, `PlayerHand`, `BtnHit`, `BtnStand` — `RectTransform`s já colocados no Canvas pela cena.
- No JSON basta `targetPath: "DealerHand"`. O `TutorialOnboardingJsonLoader` faz `Find` recursivo a partir do Canvas e devolve o `RectTransform`.
- O `SpotlightOverlay` lê `GetWorldCorners` desse rect e desenha as 4 barras ao redor. O `TutorialBlocker` em `SetHole` deixa o rect **clicável** (raycast por baixo passa) e bloqueia o resto.

### 2) Alvo é um objecto no mundo (como o nó do mapa)

- Um objecto 3D/2D do mundo **não** tem `RectTransform`, por isso não pode ser usado directamente como alvo.
- Usa-se **`TutorialWorldTargetBridge`**: um `RectTransform` sob o Canvas do tutorial que se posiciona **em cima** do objecto no mundo via `Camera.WorldToScreenPoint`.
- Fluxo mínimo (`MapTutorialOnboardingSession` é a referência):
  ```csharp
  var bridgeGo = new GameObject("MyWorldTargetBridge", typeof(RectTransform));
  bridgeGo.transform.SetParent(tutorialCanvas.transform, false);
  var bridge = bridgeGo.AddComponent<TutorialWorldTargetBridge>();
  bridge.Configure(tutorialCanvasRect, worldTargetTransform, new Vector2(168f, 168f));
  step.target = bridgeGo.GetComponent<RectTransform>();
  ```
- `Configure` já chama `UpdateFollowNow()` — o rect fica na posição correcta no **mesmo frame** em que o tutorial arranca.
- `SpotlightOverlay` e `TutorialBlocker` têm um `LateUpdate` interno que re-aplica o recorte a cada frame — se o alvo se mover (câmara, scroll do mapa, etc.) o buraco acompanha.
- Para avançar ao **clicar** no mundo: disparar um evento via `EventBridge` do próprio controller do mundo (ex.: `NodeInteraction.SelectNode` chama `EventBridge.TriggerEvent(TutorialMapEventIds.NodeSelected)`) e configurar o passo com `advanceMode = OnEvent` + `advanceWhenEventId`.

## Terceira cena da cadeia: `CoreTutorial` (combate guiado com baralho forçado + sandbox)

Depois do `MapTutorial`, clicar no nó `(0,0)` leva o jogador para `CoreTutorial` — 3 rodadas rigadas que ensinam turnos e dano, seguidas de um **modo sandbox** onde o jogador continua a jogar com cartas reais até clicar em **ENCERRAR**.

### Peças (todas em `Assets/Scripts/Tutorial/CoreTutorial/`)

| Script | Função |
|--------|--------|
| **`CoreTutorialBlackjackController`** | Réplica isolada do `BlackjackController` — nunca toca em `SaveManager`/`RunState`/itens/recompensas. Alterna entre baralho forçado (rodadas 1–3) e baralho embaralhado de multiplicação fácil (sandbox). Sincroniza suas corrotinas com o tutorial via `PauseIfTutorialExplaining` — se o passo corrente é `ContinueButton`, a corrotina pausa até o jogador ler e clicar em **Continuar**. |
| **`CoreTutorialBlackjackGame`** | Clone enxuto do `BlackjackGame` (reusa `Duelist`, `Enemy`, `GameState`, `RoundDamageResolver`) mas **pula** o `RoundStartBalance`. Expõe `NewRoundWithForcedDeck` (rig) e `NewRoundWithShuffledPool` (sandbox). |
| **`CoreTutorialDeck`** | Pilha determinística: `LoadSequence(topFirst)` para rig, `LoadShuffled(pool)` (Fisher-Yates) para sandbox. |
| **`CoreTutorialRoundRig`** (+ loader) | DTO/loader do JSON de rigging. |
| **`CoreTutorialGuidedSession`** | Bootstrap da cena (execution order `-500`) — carrega passos/strings e injeta no `TutorialManager` com `_tutorialId = core_onboarding`. |
| **`CoreTutorialGuidedUi`** | Cria/gerencia o botão **ENCERRAR** (canto superior direito, vermelho) — oculto durante as 3 rodadas guiadas, aparece quando `TutorialManager.CompletedOrSkipped` dispara (ou quando o tutorial já foi concluído antes). Também esconde o `BtnNewGame` herdado do clone do `Core`. |
| **`TutorialCoreEventIds`** | Ids do `EventBridge`: `NewRoundStarted`, `EnemyTurnTaken`, `ReturnedToPlayerTurn`, `RoundSummaryShown`, `RoundContinue`, `BattleEnded`. |

### JSON em `StreamingAssets`

| Ficheiro | Conteúdo |
|----------|----------|
| `tutorial_core_rounds.json` | `playerMaxHealth = 100`, `enemyMaxHealth = 100` (vida "normal" durante as rodadas guiadas), `sandboxHealth = 1000` (HP restaurado quando o combate entra em modo sandbox), `enemyStandThreshold = 17`, `damageMultiplier = 10` e `rounds[]` com 3 entradas. Cada rodada tem `playerOpening[2]`, `enemyOpening[2]` e `drawStack[]` (equações + flag `ace`). |
| `tutorial_core_steps.json` | 15 passos. Botões (`Pedir`/`Parar`) avançam via `OnEvent`; **todos** os outros (intro, teoria, setup, observação do inimigo, resumos, sandbox) avançam via `ContinueButton` — é o que dá tempo ao jogador de ler antes da UI seguir em frente. |
| `tutorial_core_strings.json` | Textos pt-BR dos tooltips (chaves `core_00_intro`, `core_00b_multi_theory`, `core_01_round1_setup`, …, `core_14_sandbox`). Linguagem direta, didática, com exemplos (`2 × 3 = 2 + 2 + 2 = 6`). |

### Sincronização corrotina ↔ tutorial

Para evitar que o tooltip suma antes do jogador terminar de ler (problema clássico quando o passo é `OnEvent` e a corrotina dispara o evento logo em seguida), cada ponto didático agora funciona assim:

1. A corrotina faz sua ação visível (inimigo compra, resumo aparece, etc.).
2. Chama `PauseIfTutorialExplaining()` — se o passo corrente do `TutorialManager` está em modo `ContinueButton`, a corrotina faz `yield return null` em loop até o `CurrentStepId` mudar.
3. Quando o jogador clica em **Continuar** no tooltip, `TutorialManager.AdvanceAndSave` mexe em `_stepIndex` e o `CurrentStepId` muda; a corrotina segue.

Isso é suportado por uma nova propriedade `TutorialManager.CurrentStepId` (null quando o tutorial não está a correr).

### Roteiro rigado (HP 100/100 nas rodadas guiadas, multiplicador de dano 10)

- **Rodada 1 (jogador perde)** — P: `2×3 + 1×5 = 11` → Hit compra `1×5` → 16. E: `3×3 + 5×2 = 19` → passa (19 ≥ 17). Comparação: 19 > 16, **jogador perde 50 HP** (`(21−16) × 10`). Fim R1: P 50 / E 100.
- **Rodada 2 (inimigo estoura)** — P: `2×5 + 3×3 = 19` → Stand. E: `3×3 + 1×7 = 16` obrigado a comprar (16 < 17), puxa `2×5 = 10` → 26, **bust**, perde 50 HP. Fim R2: P 50 / E 50.
- **Rodada 3 (inimigo estoura de novo)** — P: `2×5 + 5×2 = 20` → Stand. E: mesma armadilha (16 → 26) → perde 50 HP. Fim R3: P 50 / E 0.

Ao fim da rodada 3, `ResolvePostRoundFlow` detecta que o rig acabou e chama `EnterSandboxMode()` **antes** de verificar morte: o HP dos dois duelistas é restaurado para `sandboxHealth` (1000), o `TutorialManager` termina no passo `sandbox_intro`, o botão **ENCERRAR** aparece e as rodadas seguintes usam `NewRoundWithShuffledPool` com as cartas de `StreamingAssets/Easy/config_cards_easy_multiplication.json`.

### Encerramento (sandbox)

- Clicar **ENCERRAR** → `CoreTutorialBlackjackController.EndBattle()` → persiste `core_onboarding_completed = true` e `main_tutorial_completed = true`, muda `ActiveContext = Campaign` e carrega `Menu`.
- Se durante o sandbox o HP de qualquer duelista zerar, o mesmo `EndBattleRoutine` é disparado automaticamente.
- Como `main_tutorial_completed == true`, o modal do menu **não** volta a abrir — o `Jogar` vai direto ao `Map` de campanha.
- **Exit precoce**: se o jogador voltar ao menu antes de concluir, `MenuPrincipalManager.Jogar` detecta `!core_onboarding_completed` e devolve-o ao `MapTutorial`; de lá clica no `(0,0)` outra vez e retoma.

### Isolamento face ao core de produção

- A cena `CoreTutorial.unity` tem o script swappado — `BlackjackController` (`guid a62713aa893f7fc068fe1923204b8814`) foi substituído por `CoreTutorialBlackjackController` (`guid 4a3f2e1d0c9b8a7f6e5d4c3b2a190877`) preservando as serializações comuns.
- Reutilizam-se sem mudança: `Card`, `Hand`, `Duelist`, `Enemy`, `GameState`, `RoundDamageResolver`, `CardView`, `CardHandDisplay`, `DeckConfig`. Todo o resto (shuffle de produção, balance, save, items, rewards, post-victory flow) **não** é tocado pelo tutorial.
