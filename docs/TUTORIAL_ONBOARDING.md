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

- **Passos**: array `steps` com `stepId`, `targetPath` (nome do objeto sob o Canvas ou caminho `Pai/Filho`), `stringKey`, `advanceMode` (0 = evento, 2 = botão Continuar), `advanceWhenEventId` quando for evento, offsets, etc.
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
