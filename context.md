# As Cartas de Fractar — GDD Atualizado (v2.0)
> Atualizado com base no Replanejamento de Escopo (fev/2026). Itens **cortados** foram removidos. Itens despriorizados foram mantidos com marcação `[BAIXA PRIORIDADE]`.

---

## Regras para o assistente de código

- **Ler e respeitar este `context.md`** em tarefas de gameplay, combate, mapa, save, UI de batalha e convenções do projeto. É a fonte de verdade do GDD e do registo técnico; não contradizer §2 (Gameplay) e §8 (Interface) sem actualizar o documento.
- **Actualizar este ficheiro** quando o comportamento implementado divergir do texto (regras de dano, fluxo rodada/turno, HUD, balance inicial, etc.) ou quando adicionares sistemas documentáveis: ajustar a secção de Gameplay/HUD e acrescentar entrada em **§11 Registro de ações técnicas** com data e ficheiros tocados.
- **NUNCA remover comentários do usuário** ao refatorar, mover ou reescrever código. Comentários escritos pelo usuário devem ser preservados integralmente, inclusive ao fazer split de arquivos ou mover métodos entre classes. A única exceção é quando o próprio comentário se refere a algo que foi removido ou refatorado (ex: "TODO: refatorar X" após X ter sido refatorado).

---

## 2. Gameplay

Jogo de cartas com estratégia matemática e elementos roguelite. Inspirado em Slay The Spire, Balatro e o Blackjack da DLC do Resident Evil 7.

### 2.1 Sistema Roguelite e Navegação

O jogo funciona em ciclos de runs. Cada run percorre um mapa semi-aleatório de nós até o topo da torre.

**Tipos de sala mantidos:**
- **Duelos Comuns** — Blackjack contra inimigo comum
- **Loot** — Baú com recompensa

**Cortados:**
- ~~Duelos Elite (boss rooms intermediárias)~~
- ~~Eventos Aleatórios (eventos narrativos/batalha)~~
- ~~Loja durante a run~~

Ao ser derrotado, todo progresso da run é perdido, **exceto as moedas atemporais**. O mapa é embaralhado a cada run.

### 2.2 Sistema de Batalhas *(prioridade máxima)*

Cada batalha aborda um tema matemático. No início do duelo, a mesa distribui duas cartas para cada jogador.

**Por turno, o jogador pode:**
- Comprar uma carta
- Usar uma habilidade
- Passar o turno

O duelista mais próximo do limite ao final do turno vence a rodada e causa dano equivalente à diferença entre o total inimigo e o limite. O processo continua até a vida de um dos dois se esgotar.

> **O total da mão não é exibido** durante a jogabilidade ativa da rodada — o jogador calcula sozinho (núcleo pedagógico). **Excepção:** no **resumo de fim de rodada** (UI pós-mão) os totais reais das mãos são mostrados, com diferença e dano aplicado.

### Glossário de fluxo (batalha / rodada / turno)

- **Batalha** — confronto até um duelista chegar a HP ≤ 0.
- **Rodada** — desde o deal inicial até resultado terminal (bust, comparação, empate).
- **Turno** — acção do jogador (Hit/Stand) ou decisão do inimigo (comprar/passar) dentro da rodada; vários turnos por rodada.

### 2.3 Sistema de Habilidades `[BAIXA PRIORIDADE]`

Ao derrotar inimigos, o jogador pode escolher uma habilidade do inimigo derrotado para levar até o fim da run. Deck especial de habilidades é distribuído no início do duelo.

> **Escopo:** Implementação gradual. Na fase inicial, apenas as **habilidades passivas** dos inimigos (que alteram a representação das cartas) serão implementadas. As habilidades ativas virão após o núcleo de batalha estar estável.

### 2.4 Integração Narrativa `[BAIXA PRIORIDADE]`

Narrativa contida. Os textos de contextualização existem, mas não haverá peso narrativo denso. Fractar recita seu discurso no início de cada run — o ciclo é a narrativa.

---

## 3. Inimigos

4 inimigos recorrentes representando as 4 operações matemáticas. Cada um altera como os números nas cartas são apresentados (habilidade passiva — **prioritária**). Habilidades ativas são `[BAIXA PRIORIDADE]`.

### 3.1 Adição — O Acumulador

**Passiva (prioritária):** Cartas exibidas como equações de soma. Ex: `8` → `(3 + 1 + 4)`

- Habilidade 1: Reforço de Carga — +2 ao valor da próxima carta `[BAIXA PRIORIDADE]`
- Habilidade 2: Descarte Somatório — descarta carta, soma metade a outra aleatória `[BAIXA PRIORIDADE]`
- Habilidade 3: Limite Estendido — +2 ao limite, cumulativo até +4 `[BAIXA PRIORIDADE]`

*Visual: Golem de fragmentos que se acumulam, símbolos de adição, blocos se empilhando.*

### 3.2 Subtração — O Drenador

**Passiva (prioritária):** Cartas exibidas como equações de subtração. Ex: `8` → `(14 – 4 – 2)`

- Habilidade 1: Drenagem Psíquica — -1 ao limite do oponente, até -2 `[BAIXA PRIORIDADE]`
- Habilidade 2: Subtração Caótica — -2 de uma carta aleatória `[BAIXA PRIORIDADE]`
- Habilidade 3: Defesa Nula — reduz dano recebido em 2 se perder; não causa dano se vencer `[BAIXA PRIORIDADE]`

*Visual: Figura etérea se desfazendo, aura de remoção de energia, símbolos de subtração flutuando.*

### 3.3 Multiplicação — O Proliferador

**Passiva (prioritária):** Cartas exibidas como equações multiplicativas. Ex: `8` → `(2 × 2 × 2)`

- Habilidade 1: Duplicação Leve — duplica valor de uma carta da mão `[BAIXA PRIORIDADE]`
- Habilidade 2: Multiplicação Estrondosa — multiplica uma carta por si mesma `[BAIXA PRIORIDADE]`
- Habilidade 3: Ataque Arriscado — dobro do dano se vencer, triplo recebido se perder `[BAIXA PRIORIDADE]`

*Visual: Criatura com partes replicadas, múltiplos apêndices, padrões geométricos expansivos.*

### 3.4 Divisão — O Fracionador

**Passiva (prioritária):** Cartas exibidas como equações de divisão. Ex: `8` → `(16 ÷ 2)`

- Habilidade 1: Absorver Fração — nova carta valendo ¼ a 1 do total inimigo `[BAIXA PRIORIDADE]`
- Habilidade 2: Dividir — divide valor de uma carta da mão por 2 `[BAIXA PRIORIDADE]`
- Habilidade 3: Proliferamento Divisível — divide carta, resto vai pro limite total `[BAIXA PRIORIDADE]`

*Visual: Entidade angular com rachaduras, partes flutuando separadas mas conectadas, símbolos de divisão.*

---

## 4. Antagonista — Fractar

### Identidade
- **Nome:** Fractar / O Mago Impossível / Mago Matemático
- **Idade:** Desconhecida — entidade não-humana ligada aos fractais
- **Papel:** Boss final único no topo da torre

### História
Possivelmente um mago que, ao tentar alcançar o conhecimento total, fundiu-se com os Fractais e a Teoria do Caos. Não busca destruição — busca um intelecto à sua altura. A torre é seu laboratório. Você é seu experimento.

### Habilidade — O Cálculo Supremo
Pode usar habilidades de qualquer inimigo da torre. As cartas são representadas por equações progressivamente mais complexas:

| Encontro | Exemplo de equação |
|---|---|
| 1º | `(10 + 5 – 7)` |
| 2º | `((12 + 2 * 2 / 2) - 3)` |
| Final | `((-5) * -1 / 2 (5 + 2))` |

> **Escopo:** Encontros recorrentes com Fractar ao longo da torre foram **cortados**. Fractar aparece **apenas no confronto final**.

*Visual: Silhueta em manto com padrões fractais em movimento. Interior do manto é um cosmos de fórmulas e diagramas.*

---

## 5. Controles

Jogável totalmente com mouse. Todas as ações acessíveis via interface.

**Atalhos de teclado:**
- `Esc` — Abrir menu
- `Espaço` — Pular turno
- `B` — Comprar carta
- `WASD / Setas` — Navegar pela interface
- `Enter` — Confirmar ação

---

## 6. Câmera

Câmera estática focada na área de duelo durante batalhas. Na seleção de caminho no mapa, câmera controlável via scroll de mouse. Após escolha, retorna à posição fixa.

---

## 7. Universo — Magiterra

O mundo chama-se **Magiterra**. Masmorras, criaturas e magias são comuns. A Torre de Fractar é única — hostil, impossível, fractal. Cada run embaralha os caminhos. Não é possível voltar atrás, apenas seguir.

### Trilha Sonora `[BAIXA PRIORIDADE]`
Instrumentos clássicos (cordas, piano, sopros). Batalhas: acelerada. Exploração: suave. Menus: místico/enigmático. Salas desconhecidas: suspense.

---

## 8. Interface

### 8.1 HUD de Batalha

Tema escuro e místico, runas e símbolos matemáticos, luzes azuis/cianas. Mesa de duelo circular como elemento central.

- **Canvas (cenas Menu / Map / Core):** `CanvasScaler` em **Constant Pixel Size** (`Scale Factor` 1) — tamanhos de UI em **pixels de ecrã** fixos; desenhar e testar com **Game View 1920×1080**. O build usa **1920×1080** por defeito (`defaultIsNativeResolution` desligado em `ProjectSettings`).
- **Cartas do jogador:** exibidas na parte inferior da mesa. Equações mostradas sem resultado.
- **Cartas do inimigo:** parte superior. Uma carta pode iniciar virada.
- **Pontuação:** o total da mão **não é exibido** durante a jogada ativa — o jogador calcula sozinho (ver **§2.2** e excepção do **resumo de fim de rodada**).
- **`txtRoundLabel` (topo):** **“Rodada N”** + linha de baixo com **fase** (`TurnPhaseLabel`). **`txtBattleCenter` (centro):** **feed** + **resumo** curto (`font` ~**22**). **`txtPlayerHandValue` / `txtEnemyHandValue`:** **só no resumo de fim de rodada**; texto **`Valor da mão: N`** (totais de `RoundDamageOutcome`); **vazios** durante o turno. **Posição:** junto às áreas de mão — jogador: mesma âncora Y ~**0,22** que `PlayerHandArea`, pivot inferior, **+86** px; inimigo: âncora Y ~**0,8** como `EnemyHandArea`, pivot superior, **−82** px (`font` ~**17**). **Áreas de cartas:** inimigo ~**0,8** em Y, jogador ~**0,22**.
- **Limite (21):** exibido centralmente, adjacente ao cristal fractal.
- **Vida do jogador:** texto TMP ancorado no **canto inferior direito** do canvas.
- **Vida do inimigo:** texto TMP ancorado no **canto superior esquerdo** do canvas.
- **Botões de acção (combate Core):** **Hit** e **Stand** na base, mais próximos (âncora Y ~0,1; **X ≈ ±58** em 1920×1080).
- **Painel de habilidades:** canto inferior esquerdo.
- **Moedas atemporais:** canto inferior direito.

### 8.2 Menus

**Tela Inicial:** imagem da Torre, logo, 3 botões — Adentrar Torre / Opções / Sair.

**Menu de Opções:** painel central sobre fundo desfocado (mesmo fluxo visual na cena Menu ou como overlay DontDestroy quando se usa o botão Config do chrome global). Volume e ajustes de música/UI existentes no painel Opções (`AudioMixer` em `Resources/Settings/Volume` quando aplicável).

**Menu de Pause:** pausa o jogo, fundo desfocado. Botões: Resumir / Opções / Voltar à tela inicial.

**Hub / Tela de Itens:** estética de mesa de matemático arcanista. Exibe moedas atemporais, catálogo de melhorias permanentes e botão "Adentrar Torre".

**Cursor (global):** `SoftwareCustomCursor` num objecto DDOL próprio (**`PersistentCustomCursor`** na cena Menu); sprite em **`Resources/Cursor/cursor`**; sobrepõe o HUD com `Canvas` Overlay, **`overrideSorting`** e ordenação **dinâmica** sobre todos os outros `Canvas` Overlay / Screen Space Camera activos — para ficar acima de modais (tutorial no menu, etc.). Durante **`FullscreenVideoOverlay`**, repõe‑se o cursor nativo (`ActiveOverlayCount`).

### 8.3 Música (BGM por cena)

- **Configuração:** `StreamingAssets/Music/scene_music_config.json` mapeia **nome de cena** → faixa em **`Resources/`** (caminho sem extensão, ex. `Music/nome_do_ficheiro`). Faixa por omissão: `defaultTrackResourcePath` quando a cena não tem entrada.
- **Comportamento:** `SceneMusicDirector` (no mesmo objeto persistente que `MusicManager` na cena Menu) reage a `SceneManager.sceneLoaded` e chama `MusicManager.PlayTrackResource`. Em cada entrada de cena, a faixa pode começar num **instante aleatório** no clip (salvo `disableRandomStart` na entrada). `MusicManager` mantém **volume do utilizador** (slider) × **factor de fade** (vídeos/cutscenes).
- **Vídeo fullscreen:** `FullscreenVideoOverlay.PlayRequest` pode pedir *duck* da música (`duckBackgroundMusic`); intro do menu e *outro* do boss usam fade-out antes do vídeo e fade-in ao terminar (a cena seguinte ou o mesmo `PlayTrackResource` repõe o factor de fade ao trocar faixa). **`VideoWarmupService`** pré-prepara o mesmo vídeo quando o jogador está no menu (primeira vez, antes de reproduzir a intro) ou no combate final contra Fractar, para o primeiro fotograma chegar mais depressa).

---

## 9. Progressão e Hub

- **`SaveData.main_tutorial_completed`** e **`main_tutorial_step_index`:** no `save.json` para o tutorial principal (onboarding guiado). Com **`main_tutorial_completed`** `true`, o `TutorialManager` **não** auto-inicia o fluxo (se `_respectCompletedFlag` estiver ligado). Ao concluir ou pular, grava **`main_tutorial_completed = true`**. **`ResetTutorialProgress()`** volta o passo a 0 e **`main_tutorial_completed`** a `false`. Detalhe em **`docs/TUTORIAL_ONBOARDING.md`**.
- Moedas atemporais persistem entre runs
- Hub com melhorias permanentes básicas compráveis antes de cada run
- Mapa com nós semi-randomizados (majoritariamente duelos e loot)
- Dificuldade cresce por andares

**Cortado:**
- ~~Loja durante a run~~
- ~~Eventos narrativos / salas de evento~~
- ~~Duelos elite intermediários~~
- ~~Encontros recorrentes com Fractar~~

---

## 10. Cronograma

| Tarefa | Status |
|---|---|
| GDD Completo | ✅ Concluído |
| Replanejamento de Escopo | ✅ Concluído |
| Arte dos personagens | 🔄 Em progresso |
| Arte dos cenários | 🔄 Em progresso |
| Sistema de controle do jogador | 📋 Planejado |
| Sistema de mapas e fases | 📋 Planejado |
| Detecção de colisão | 📋 Planejado |
| Sistema de pontuação / batalha | 📋 Planejado |
| Implementar inimigos | 📋 Planejado |

---

## 11. Registro de ações técnicas

### 2026-05-23 — Chrome Ajuda + Config DDOL + wiki global + opções em overlay

- **HUD persistente:** `PersistenteGlobalChrome` (`Assets/Scripts/HUD/PersistenteGlobalChrome.cs`) criado por `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`, `DontDestroyOnLoad`: botões topo-esquerdo (**Ajuda**/**Config**) em `Canvas` Overlay com **`CanvasScaler.ConstantPixelSize`** ×1 (`sortingOrder` ~2995) e sprites em `Assets/Resources/UI/GlobalChrome/`; **`FullscreenVideoOverlay.ActiveOverlayCount > 0`** só oculta a linha do chrome (mantém `UIDocument` Wiki activo para fluxos como revisão Core).
- **Ajuda/Wiki:** `Assets/Scripts/HUD/PersistenteGlobalChrome.cs` + **`MapWikiAccess`**/`UIDocument` usando `Assets/Resources/UI/Wiki/` (`WikiPersistentPanelSettings`, `WikiView.uxml`, `WikiStyles.uss`), `ApplyPersistentDdOlConfiguration()` (sem botão runtime à direita); `MapWikiAccess.RegisterDdOlAjudaHotspot` alinha cliques com **`IsPointerPressOnWikiOpenButton`** na Core. **Removido** o objeto `WikiOverlay` de `Assets/Scenes/Map.unity` e `Assets/Scenes/Core.unity` para evitar duplicar o host.
- **Exclusões de chrome:** `GameFlowScenes.ShouldHidePersistentChromeButtons` (Menu + cenas tutorial blackjack/mapa/core/loja) + fecho da wiki ao entrar nelas (`MainMenuOptionsModal.TryCloseDdOlWikiIfExcluded`).
- **Opções globais:** `MainMenuOptionsModal` (`Assets/Scripts/Menus/MainMenuOptionsModal.cs`) na raiz **Opções** do `Menu.unity` + prefab `Assets/Resources/UI/MainMenuOptionsModal.prefab` (gerado por `tools/extract_main_menu_options_prefab.py` a partir do subárvore Opções); **`MenuPrincipalManager`** delega **`AbrirOpcoes`/`FecharOpcoes`**; Hub e mapa usam `MainMenuOptionsModal.OpenGlobalDdOl()` (`HubMenus`, `HUDExploracao`). Pausa **`Time.timeScale`** via `PersistenteGlobalChrome.TryPauseForOverlay` ao abrir o overlay fora do menu.
- **`Resources/Settings/Volume.mixer`:** cópia runtime do master para o slider de volume global do modal.

### 2026-03-17

- Refatorado `Assets/Scripts/Core/Blackjack/Core/Duelist.cs` (antes `Core/Duelist.cs`): removido campo privado `_health` e simplificado para auto-property `Health`; adicionado `HasStood` para estado de turno.
- Criado `Assets/Scripts/Core/Blackjack/Core/Enemy.cs` (antes `Core/Enemy.cs`): nova classe `Enemy : Duelist` com `StandThreshold`, `IsFirstCardHidden` e método `ShouldHit(int handLimit)`.
- Refatorado `Assets/Scripts/Core/Blackjack/Core/BlackjackGame.cs` (antes `Core/BlackjackGame.cs`): removidas flags locais de "stand" e migrada lógica para `Player.HasStood` / `Enemy.HasStood`; `Enemy` agora é tipado como classe própria e decisão de compra usa `Enemy.ShouldHit(...)`.
- Refatorado `Assets/Scripts/Unity/BlackjackController.cs`: removido estado local `_enemyFirstCardHidden` e migrado para `_game.Enemy.IsFirstCardHidden`.
- Verificação pós-refatoração: sem erros de lint nos arquivos alterados.

### 2026-03-17 — Mapa procedural estilo Slay the Spire (protótipo isolado)

- Criados scripts isolados da gameplay principal em `Assets/Scripts/Map/`:
  - `MapNode.cs` com `Row`, `Col`, `WorldPosition`, `Children` e tipo `CombateNormal`.
  - `MapGenerator.cs` (`MonoBehaviour`) com grafo em `Dictionary<(int row, int col), MapNode>`.
- Implementada geração determinística com `Random.InitState(seed)` para grade padrão `7x15` com `6` caminhos, cada caminho com deslocamento por linha em `-1/0/+1` com clamp de coluna.
- Instanciação apenas de nós visitados por ao menos um caminho e deduplicação de conexões via `HashSet` para impedir arestas repetidas.
- Visualização implementada com:
  - raízes `Nodes` e `Connections`;
  - uma instância visual por nó;
  - `LineRenderer` por conexão;
  - centralização na origem com `colSpacing` e `rowSpacing`;
  - jitter determinístico `±0.15` em X/Y.
- Adicionado `[ContextMenu("Regenerate Map")]` para limpar e recriar o mapa em edição.
- Criada cena separada `Assets/Scenes/Map.unity` com `MapGenerator` configurado para o protótipo (sem integração com o loop de gameplay atual).

### 2026-03-17 — Distribuição de tipos, ícones/bordas e split do MapGenerator

- Separado `MapGenerator` em dois componentes ortogonais:
  - `MapGenerator.cs` — geração de grafo (paths, nós, conexões, atribuição de tipo).
  - `MapVisualizer.cs` — visualização (spawn de nós/conexões, labels, materiais, cleanup).
- Adicionado sistema de distribuição de `MapNodeType` por peso percentual:
  - classe `NodeTypeWeight` com `type` + `weight`, serializada no Inspector.
  - `OnValidate` sincroniza a lista automaticamente com os valores do enum `MapNodeType` (adiciona novos, remove obsoletos, preserva pesos do usuário).
  - Na geração, tipos são sorteados via weighted random com pesos normalizados.
- Preparado sistema de ícones por tipo e bordas por dificuldade (future-proof):
  - `NodeTypeVisual` (`MapNodeType` + `Sprite icon`) auto-populado via `OnValidate`.
  - `DifficultyBorder` (`CombatEquationDifficulty` + `Sprite border`) auto-populado via `OnValidate`.
  - Ambos no `MapVisualizer`, prontos para atribuição visual quando os sprites forem criados.
- Corrigidos bugs: `GetOrCreateNode` com variáveis quebradas, `CreateNode` sem args, `AttachTypeLabel` referenciando enum removido `CombateNormal`.
- Cena `Map.unity` atualizada com ambos os componentes no mesmo GameObject.

### 2026-03-23 — Ciclo de Roguelite: save system, navegação no mapa, transição de cenas

- Renomeada cena `MapPrototype.unity` → `Map.unity`. Adicionadas cenas `Map` e `Core` ao `EditorBuildSettings.asset`.
- Criado sistema de save em JSON (`Application.persistentDataPath/save.json`):
  - `Assets/Scripts/Core/Save/SaveData.cs` — dados persistidos: `currentRun`, `playerRow`/`playerCol`, `coins`, `currentSeed`, `playerHealth`, `main_tutorial_completed`, `main_tutorial_step_index`, `seedHistory`.
  - `SeedHistoryEntry` — no mesmo ficheiro que `SaveData`.
  - `Assets/Scripts/Core/Save/SaveManager.cs` — classe estática com `Load()`, `Save()`, `Delete()`. Cria defaults seguros no primeiro load (run=1, pos=(-1,-1), seed aleatória, health=100).
- Criado `Assets/Scripts/Core/Utils/RunState.cs` — classe estática para transporte de dados voláteis entre cenas (`CurrentNodeType`, `CurrentCombatDifficulty`, `LastBattleResult`).
- `MapGenerator.cs` — inicialização movida para `Awake()` em play mode; seed carregada do `SaveData`; adicionados `Seed` property, `GenerateWithSeed()`, `ConnectionSet`.
- `BlackjackController.cs` — carregamento de deck por tipo de node via mapeamento de prefixos (`combat_add_cards.json`, etc.) com fallback para `config_cards.json`; vida do player carregada do save; detecção de fim de batalha (health ≤ 0) com lógica de morte/reset de run ou vitória; transição automática de volta à cena Map.
- Criado `NodeInteraction.cs` — seleção de nodes no mapa via clique (com threshold para distinguir de drag); acessibilidade: pos (-1,-1) habilita qualquer node da row 0, senão apenas nodes conectados; ao selecionar, salva posição e carrega cena Core; exibe mensagem de transição ao retornar do Core.
- `MapVisualizer.cs` — adicionado `ApplyAccessibility()` que reduz opacidade (alpha ~0.3) de nodes e conexões inacessíveis; destaque visual do node atual do jogador.

### 2026-04-05 — Dificuldade por faixa de linha (row) no mapa

- `MapGenerator.cs` — a dificuldade de combate de cada nó segue o **índice de linha** do nó (`row` 0-based), não um sorteio em [0,1].
- Dois limites **exclusivos** (serializáveis): `easyRowEndExclusive` e `mediumRowEndExclusive`.
  - **Easy:** `row` em `[0, easyRowEndExclusive)`
  - **Medium:** `row` em `[easyRowEndExclusive, mediumRowEndExclusive)`
  - **Hard:** `row` em `[mediumRowEndExclusive, rows)` (até `rows - 1` inclusive).
- Exemplo com `rows = 10` (linhas 0–9): `easyRowEndExclusive = 3` e `mediumRowEndExclusive = 7` → linhas 0–2 fáceis, 3–6 médias, 7–9 difíceis (equivalente à ideia 1–3 / 3–7 / 7–10 em numeração começando em 1).
- `autoEqualDifficultyRowBands` (default **true**): ao mudar `rows`, recalcula os dois limites para **três faixas o mais iguais possível** (resto de `rows % 3` distribuído nas primeiras faixas). Com `false`, os limites são só validados (clamp) contra `rows`.
- Baralhos em `StreamingAssets` por pasta Easy/Medium/Hard + `RunState.CurrentCombatDifficulty` permanecem alinhados a esse `row`.

### 2026-04-05 — Hook pós-vitória ao voltar ao mapa

- Criado `Assets/Scripts/Core/Utils/PostVictoryReturnFlow.cs` (`RunAfterVictoriousBattle`): chamado em `BlackjackController` após persistir vida do jogador numa vitória, **antes** do `LoadScene("Map")`. Corpo atual só com `Debug.Log` (placeholder).
- **Extensibilidade (ideias, não obrigatório implementar agora):**
  - **Evento estático multicast** (`event Action` ou `event Action<BattleContext>`): vários sistemas subscrevem sem o fluxo central conhecer nomes — bom para loot, achievements, áudio.
  - **Interface `IPostVictoryListener`** + registo explícito (lista na inicialização ou `[RuntimeInitializeOnLoadMethod]`): ordem controlada, testável, fácil de mockar.
  - **ScriptableObject** “channels” ou assets de regra: designers ligam efeitos no Inspector sem tocar no código do combate.
  - **UnityEvent** num `MonoBehaviour` persistente (DontDestroyOnLoad): útil para ligações visuais no Editor; menos ideal para lógica pura de domínio.
  - **Payload único** (struct/context com `MapNodeType`, `CombatEquationDifficulty`, vida restante, etc.) passado ao hook quando existir mais de um consumidor — evita cada um ir buscar estado global à parte.
- Manter **um único ponto de chamada** a partir do combate (`BlackjackController` ou futuro `BattleFlowCoordinator`) mantém o desenho **ortogonal**: o que acontece depois da vitória não espalha `if (won)` pelo projeto.

### 2026-04-05 — Árvore `Assets/Scripts/Core/` (pós-refatoração)

Organização por **grupo lógico**; referências no código e no histórico acima devem usar estes caminhos.

```
Core/
├── Save/                    # Persistência JSON
│   ├── SaveData.cs          # SaveData + SeedHistoryEntry (namespace global)
│   └── SaveManager.cs
├── Utils/                   # Estado volátil entre cenas, hooks de fluxo
│   ├── RunState.cs
│   └── PostVictoryReturnFlow.cs
└── Blackjack/
    ├── Core/
    │   ├── BlackjackGame.cs
    │   ├── GameState.cs     # enum + GameStateSemantics
    │   ├── Duelist.cs
    │   └── Enemy.cs
    ├── Balance/
    │   ├── RoundBalance.cs
    │   └── RoundStartBalance.cs
    ├── Combat/
    │   ├── RoundDamageResolver.cs  # RoundDamageOutcome + dano fim de rodada
    │   └── EnemyCombatBalance.cs   # vida e mult. de dano do inimigo por dificuldade
    └── Decks/
        ├── Card.cs
        ├── Hand.cs
        ├── Deck.cs
        ├── DeckConfig.cs
        └── ExpressionEvaluator.cs
```

- **Namespaces:** `Blackjack.Core` — jogo (`BlackjackGame`, `GameState`, `GameStateSemantics`, `RoundBalance`, `RoundStartBalance`, `RoundDamageResolver`, `Duelist`, `Enemy`). `Blackjack.Decks` — cartas e baralho.
- **Quem referencia o quê:** `BlackjackController` e `CardHandDisplay` usam `using Blackjack.Core;` e `using Blackjack.Decks;`. `Save*` e `RunState` permanecem no namespace global (acessíveis sem `using` extra em `Map/` e `Unity/`).
- **Novos ficheiros em Core:** colocar na pasta do grupo (Save / Utils / `Blackjack/Core` / `Blackjack/Decks`) e no namespace `Blackjack.*` apenas quando forem código de domínio do baralho ou da mesa.

### 2026-04-05 — Combate: dano unificado, balance inicial, HUD rodada/turno

- **`GameState.cs`:** enum único + **`GameStateSemantics`** (`IsTurnPhase`, `IsRoundResolutionPhase`, `IsRoundTerminal`, `IsRoundOver`).
- **`RoundStartBalance`:** após deal inicial, se `Hand.Value` do jogador ou inimigo for `> 21` ou `== 21`, `Deck.Reset()` e novo deal (até 32 tentativas); **`Debug.Log` em todo acionamento** `[RoundStartBalance]` e logs por redeal. **Não há natural 21 no deal** enquanto esta regra existir.
- **`RoundDamageResolver` / `RoundDamageOutcome`:** dano ao fim da rodada = distância da **mão do perdedor** ao limite 21 × multiplicador (10); empate 0; **`[RoundDamage]`** no console. `BlackjackGame.CommitRoundDamage()` único caminho após estado terminal.
- **`Duelist.ApplyDamage(int)`** — dano aplicado pelo resolver; `ReceiveDamageByHandDiff` mantido como legado.
- **`BlackjackGame`:** `CurrentRoundNumber`, `LastRoundDamageOutcome?`, integração com balance e resolver; naturals no deal removidos (cobertos pelo balance).
- **`BattleUiCopy` + `BlackjackController`:** `TurnPhaseLabel`, `PlayerRoundResultCaption`; **`txtRoundLabel`** (rodada + fase no topo), **`txtBattleCenter`** (feed + resumo no centro); coroutine de resumo + input para continuar.

### 2026-04-05 — Cena Core: HUD central unificado

- **`Core.unity`:** `CanvasScaler` **Constant Pixel Size**; `txtPlayerHealth` / `txtEnemyHealth` nos cantos; **`txtBattleCenter`** (centro, ~780×420); **`txtRoundLabel`** de novo no topo só para “Rodada N”; removidos `txtSummary*`, `txtTurnPhase`, `TxtEnemyFeed` como objetos separados.
- **`ApplyRoundSummaryUi`:** escreve o bloco completo em `txtBattleCenter` (rich text). **Hit/Stand** na base; sem botão Nova rodada no fluxo.

### 2026-04-05 — (histórico) Feed e resumos em vários TMP

- Obsoleto: múltiplos TMP para resumo e feed; consolidado em **`txtBattleCenter`** (ver entrada “HUD central unificado”).

### 2026-04-09 — Loja, sistema de itens e recompensas

- **Pasta `AssetsTempLoja/`** (projeto externo) integrada como cena isolada; assets úteis migrados, lixo deletado.
- **Cena `Assets/Scenes/Store.unity`** no Build Settings. Camera + `UIDocument` (UI Toolkit) + `StoreController`. Entrada na campanha: **`HubInicial`** (`HubManager`). `StoreController` volta com `SceneManager.LoadScene(GameFlowScenes.ExitStoreDestination)` (hub na campanha, mapa-tutorial no modo tutorial).

#### Sistema de itens (`Assets/Scripts/Items/`)

- **`ItemType.cs`:** enum `Attack`, `Defense`, `Consumable`.
- **`ItemId.cs`:** enum alinhado ao catálogo actual (ver entrada **2026-04-23 — Catálogo da loja**).
- **`ItemDefinition.cs`** + **`ItemCatalogData`:** classes `[Serializable]` com `id`, `displayName`, `description`, `type`, `price`, `icon`, `attackMultiplier`, `bonusHealth`, `consumableEffect`, `consumableValue`. Mapeamento `type` → `ItemType` e `id` → `ItemId` via switch expression; **`ParseConsumableAction()`** mapeia `consumableEffect` (ex. `heal`) → **`ConsumableActionType`**.
- **`ConsumableActionType.cs`:** enum `None`, `Heal`.
- **`ConsumableBattleEffects.cs`:** `TryApply(ItemDefinition, Duelist player, out message)` — hoje só **Heal** usando `consumableValue` (cura até `MaxHealth`).
- **`ItemCatalog.cs`:** classe estática; carrega `StreamingAssets/Items/item_catalog.json` (lazy, indexado por `id`). Expõe `All` e `Get(string id)`.
- **`PlayerItemStats.cs`:** `CalculateMaxHealth` = `100 + soma bonusHealth` por cada entrada defensiva em `ownedItemIds` (ids repetidos acumulam). `CalculateDamageMultiplier` = `1 + soma (attackMultiplier - 1)` por entrada ofensiva (acumulação aditiva do bónus; ex.: anel 1,15 + tomos 1,35 + cajado 1,5 → ×2,0).
- **JSON:** `item_catalog.json` — ver entrada **2026-04-23** para a lista actual de itens.

#### Save (`Assets/Scripts/Core/Save/SaveData.cs`)

- **`ownedItemIds`:** itens **attack** e **defense** comprados na loja; o mesmo `id` pode aparecer **várias vezes** para acumular bónus.
- **`consumableSlots`:** até **3** IDs de consumíveis, **sem stack** (o mesmo `id` pode aparecer em slots distintos). Não entra em `ownedItemIds`.
- **Na derrota:** `save.playerHealth = PlayerItemStats.CalculateMaxHealth(save)` (antes era hardcoded 100).

#### Duelist (`Assets/Scripts/Core/Blackjack/Core/Duelist.cs`)

- **Novos campos:** `MaxHealth` (int, default 100), `DamageMultiplier` (float, default 1f).
- **`BlackjackController.Awake`:** após carregar save, seta `Player.MaxHealth` e `Player.DamageMultiplier` via `PlayerItemStats`.
- **`RoundDamageResolver`:** quando dano é ao enemy, multiplica pelo `player.DamageMultiplier`; quando dano é ao player, multiplica pelo `enemy.DamageMultiplier`.

#### Inimigo por dificuldade (`Assets/Scripts/Core/Blackjack/Combat/EnemyCombatBalance.cs`)

- **`GetEnemyMaxHealth` / `GetEnemyDamageMultiplier`:** Easy 80 HP / ×1; Medium 150 / ×1,5; Hard 300 / ×2 (valores default = Easy).
- **`BlackjackController.Awake`:** após criar `BlackjackGame`, aplica HP/MaxHP e `DamageMultiplier` do inimigo com `RunState.CurrentCombatDifficulty` (alinhado ao deck e às moedas de vitória).
- **Resumo de dano (`ApplyRoundSummaryUi`):** texto explica `gap × 10 ×` multiplicador do atacante (jogador ou inimigo, conforme o alvo do dano).

#### Recompensas (`Assets/Scripts/Core/Blackjack/Rewards/BattleRewardResolver.cs`)

- **`GetCoinReward`:** Easy=100, Medium=250, Hard=500 (valores em `BattleRewardResolver`).
- **`ApplyVictoryRewards(SaveData, CombatEquationDifficulty)`:** soma moedas ao save. Retorna quantidade.
- Chamado em `BlackjackController.EndBattleRoutine` na vitória, **antes** de `SaveManager.Save`. UI mostra `+N moedas`.
- **Extensível:** ponto de adição para drops de itens consumíveis no futuro.

#### Loja (`Assets/Scripts/Store/StoreController.cs`)

- **`ItemCatalog`** + `SaveData.coins`. **`MaxConsumableSlots` = 3** (constante na loja).
- **Attack/Defense:** compra → `ownedItemIds.Add(id)` (pode repetir o mesmo id) + defesa aplica `bonusHealth` em `playerHealth` no momento da compra; grelha da loja recarrega após confirmar.
- **Consumíveis:** compra → só `consumableSlots.Add(id)` se `Count < 3`; **não** grava em `ownedItemIds`. Cabeçalho **`Consumíveis: N/3`** (`ConsumableSlotsLabel` no UXML). Com 3/3: botões dos consumíveis **“Inventário cheio (3/3)”**; modal de confirmação bloqueado com mensagem para usar na batalha primeiro.
- **UI:** `StoreView.uxml` + `StoreStyles.uss` (classe `.consumable-slots-label`). Fundo e ícones como antes.
- **Botão Voltar:** `SceneManager.LoadScene(GameFlowScenes.ExitStoreDestination)`.

#### Batalha — consumíveis (`BlackjackController`)

- **`_save`** mantido em memória na cena; `consumableSlots` mutável; **teclas 1 / 2 / 3** usam o slot de índice 0–2 **só em `GameState.PlayerTurn`** (e sem resolução de rodada / uso já em curso).
- **Coroutine:** desativa Hit/Stand; `txtBattleCenter` mostra `Usando "NOME"...` depois o resultado (ex. `+N vida`); remove o slot, `SaveManager.Save`, atualiza HUD.

#### Árvore de novos arquivos

```
Scripts/
├── Items/
│   ├── ItemId.cs
│   ├── ItemType.cs
│   ├── ConsumableActionType.cs
│   ├── ConsumableBattleEffects.cs
│   ├── ItemDefinition.cs   # ItemDefinition + ItemCatalogData
│   ├── ItemCatalog.cs
│   └── PlayerItemStats.cs
├── Store/
│   └── StoreController.cs
└── Core/Blackjack/Rewards/
    └── BattleRewardResolver.cs

StreamingAssets/Items/
└── item_catalog.json

UI/Store/
├── StoreView.uxml
├── StoreItemView.uxml
├── StoreStyles.uss
└── DefaultPanelSettings.asset

Art/Store/
└── MagicDungeonBackground.png

Resources/Icons/
├── pocao_pequena.png, pocao_grande.png
├── anel_inequacao.png, tomos_lineares.png, cajado_fractal.png
├── amuleto_progressivo.png, escudo_hipotenusa.png, tunica_fractal.png
└── CoinFrames/ (1-6.png), moeda.gif

Scenes/
└── Store.unity
```

#### Extensibilidade

- **Novos consumíveis:** novo `consumableEffect` + ramo em `ConsumableBattleEffects` / `ParseConsumableAction`.
- **Drops de itens:** `BattleRewardResolver` pode ser estendido com tabela de drops por dificuldade.
- **Equip slots:** futuramente adicionar `equippedItemIds` ao save para separar posse de uso.
- **Novos itens:** adicionar entrada no `item_catalog.json` + novo valor no `ItemId` enum.

### 2026-04-09 — Combate: inimigo por dificuldade da fase

- **`EnemyCombatBalance`:** mapeia `CombatEquationDifficulty` → vida máxima e `DamageMultiplier` do `Enemy` (Easy/Medium/Hard).
- **Entrada na batalha:** `BlackjackController.Awake` preenche `_game.Enemy` antes do primeiro `NewRound`.
- **Dano:** `RoundDamageResolver` aplica `enemy.DamageMultiplier` quando o dano é ao jogador; UI do resumo de rodada mostra a cadeia `× 10 × mult` do atacante.

### 2026-04-09 — Consumíveis (poção, save, loja, uso em combate)

- **`SaveData.consumableSlots`**, máximo 3; poções no JSON: `heal` + `consumableValue` (100 / 250 conforme o item).
- **Loja:** contador N/3; compra não usa `ownedItemIds` para consumíveis.
- **Combate:** atalhos numéricos 1–3; feedback em `txtBattleCenter`; item removido do save após uso.

### Hub da campanha (`HubInicial`)

- **`GameFlowScenes.HubInicial`:** área-meta entre runs; **«Jogar»** no menu (campanha) carrega esta cena após vídeo-intro se aplicável; **derrota em combate** na campanha redirecciona ao hub em vez do mapa. A cadeia tutorial mantém‑se como antes (`TutorialDefaultBlackjack`, `MapTutorial`, etc.).
- **Navegação:** `Assets/Scripts/Hub (MenusPersistentes)/HubMenus.cs` (`HubManager`): Menu, Loja (`Store`), Mapa (`Map`), painel configurações interno.

### Histórico — atalho «Loja [TEMP]» no mapa (removido)

- **Removido:** `TemporaryMapStoreAccess`; acesso à loja na campanha é pelo **`HubInicial`**. Mantêm‑se **`EventSystem`**, **`TransitionCanvas`** e o tratamento UI em **`MapUiRaycasts`** / **`NodeInteraction`** (úteis ao mapa, não só ao botão apagado).

### 2026-04-13 — Tutorial principal no save + doc de onboarding

- **`SaveData.main_tutorial_completed`** e **`main_tutorial_step_index`:** persistidos em `save.json`; o `TutorialManager` não usa PlayerPrefs. `SaveManager.EnsureMainTutorialCompletedKeyInSaveFile()` na entrada da cena garante `main_tutorial_completed` no JSON com `false` se faltar.
- **`GuidedBlackjackNarrative` + evento `tutorial.bj.dealer_turn_visual_done`:** fluxo guiado (mesa após Parar).
- **Documentação:** [`docs/TUTORIAL_ONBOARDING.md`](docs/TUTORIAL_ONBOARDING.md) — `TutorialManager`, JSON, `EventBridge`, save.

### 2026-04-16 — Onboarding do mapa (MapTutorial): UX, texto e registo técnico

- **`TutorialManager`** (`Assets/Scripts/Tutorial/Onboarding/TutorialManager.cs`): removidos o botão runtime **«Pular treino»** (canto superior direito), o `WireSkipIfPresent` e o atalho **Escape** que chamavam `SkipEntireTutorial()` — o fluxo de skip deixava saves/estado inconsistentes. O método `SkipEntireTutorial()` mantém-se público por compatibilidade, mas **não** há mais UI nem input padrão associados.
- **Tamanho do modal por passo (JSON):** campos `tooltipPanelWidth` / `tooltipPanelHeight` em cada entrada de `tutorial_*_steps.json`. Se ambos forem &gt; 0, o `TooltipUI` aplica esse `sizeDelta`; caso contrário **520×160** (`TooltipUI.DefaultPanelWidth/Height`). O mapa e o Core usam **760×300** nos JSON (substitui o antigo flag `UseExpandedTooltipPanelNext`).
- **`TutorialStepDefinition.blockEntireScreenInput`:** flag adicionada para forçar bloqueio em ecrã inteiro mesmo com alvo definido (não usado actualmente, mantido para futuras secções estritamente narrativas).
- **`MapGenerator.GeneratePaths`:** em `GameFlowScenes.MapTutorial`, o **primeiro caminho** começa sempre na **coluna 0**, garantindo o nó **(0,0)** no grafo.
- **Restrição de clique no `MapTutorial`:** `NodeInteraction.IsAccessible` / `GetAccessibleNodeKeys` só consideram **(0,0)** como acessível enquanto o jogador estiver em `playerRow=-1` na cena `MapTutorial`. `MapVisualizer.ApplyAccessibility` baixa o alpha dos não-acessíveis para `inaccessibleAlpha`, logo (0,0) é o único totalmente opaco; clicar em qualquer outro é ignorado.
- **Pulso no nó clicável:** `MapTutorialNodePulse` é anexado em `NodeInteraction.Start` ao `Transform` do nó (0,0) enquanto `playerRow=-1` em `MapTutorial`. Componente auto-contido: pulsa a escala em `LateUpdate` (amplitude 18%, ~3.6 rad/s), restaura a escala base em `OnDisable`, sem dependências do fluxo de tutorial.
- **`MapTutorialOnboardingSession`** ficou minimalista: regista os passos do JSON, substitui o texto de `map_spotlight` com **tipo + dificuldade do nó (0,0) + “Continuar”**, e cria o `TutorialManager`. Sem `target`, sem bridge, sem pulso — o tooltip é centrado e avança por **botão Continuar** (modo do JSON, valor 2). Texto base do `map_00` actualizado em `TUTORIAL/map/tutorial_map_strings.json`.
- **Tracking de alvo em tempo real (utilitário, não usado neste passo):** `SpotlightOverlay` e `TutorialBlocker` recalculam o recorte em `LateUpdate` enquanto há alvo activo, e `TutorialWorldTargetBridge.Configure` posiciona o rect no mesmo frame via `UpdateFollowNow()` — fica disponível para futuros passos que precisem destacar elementos do mundo.
- **`TutorialMapEventIds.NodeSelected`** disparado em `NodeInteraction.SelectNode` antes de `LoadScene(GameFlowScenes.CurrentCore)` — fica disponível para qualquer passo que queira avançar no clique de um nó (não usado pelo passo final actual, que avança via Continuar).
- **Constante de semente:** `TutorialMapConstants.GenerationSeed` em `Assets/Scripts/Map/TutorialMapConstants.cs` — o `MapGenerator` em `GameFlowScenes.MapTutorial` força esta seed no `Awake`, garantindo o mesmo layout entre sessões do tutorial.
- **Ficheiros de conteúdo:** passos em `Assets/StreamingAssets/TUTORIAL/map/tutorial_map_steps.json`; strings base em `TUTORIAL/map/tutorial_map_strings.json` (o texto rico do spotlight continua a ser sobrescrito em runtime pelo passo acima).

### 2026-04-16 — Onboarding do combate (`CoreTutorial`): terceira e última etapa da cadeia

Terceira etapa guiada, acedida clicando no nó (0,0) do `MapTutorial` (graças à seed determinística, o nó é sempre **Combate de Multiplicação — Fácil**). Ensina turnos alternados e regra de dano com 3 rodadas riggadas: **1 derrota** didáctica + **2 vitórias** por bust do inimigo. Ao terminar, o fluxo devolve ao `Menu`, **sem prompt de tutorial**.

- **Isolamento por cena:** a cena `CoreTutorial.unity` deixou de usar o `BlackjackController` de produção — o script foi swappado (GUID `a62713aa...` → `4a3f2e1d...`) por `Tutorial.CoreTutorial.CoreTutorialBlackjackController`. O tutorial **não** toca em `SaveManager`/`RunState`/`PlayerItemStats`/`EnemyCombatBalance`/`BattleRewardResolver`/`PostVictoryReturnFlow`.
- **Lógica de jogo:** `CoreTutorialBlackjackGame` replica o `BlackjackGame` mantendo **a alternância de turnos** (cada Hit passa a vez ao inimigo; EnemyAct devolve a vez ao jogador). Reusa-se `Duelist`, `Enemy`, `GameState`, `RoundDamageResolver` e a UI (`CardHandDisplay`, `CardView`). O `RoundStartBalance` é pulado — as mãos de abertura são exatamente as do rig.
- **Baralho rigado:** `CoreTutorialDeck` substitui o `Deck` do core para eliminar o `Shuffle`; `LoadSequence(topFirst)` define a ordem exacta (`P0, E0, P1, E1, draws...`). Rig completo em `Assets/StreamingAssets/TUTORIAL/core/tutorial_core_rounds.json`:
  - **Round 1** — P `2×3 + 1×5` (= 11) + Hit compra `1×5` → 16; E `3×3 + 5×2` (= 19) passa. `EnemyWin`, player perde **(21−16) × 10 = 50** HP.
  - **Round 2** — P `2×5 + 3×3` (= 19) stand; E `3×3 + 1×7` (= 16) é forçado a comprar (<17), saca `2×5 = 10` → **26 bust**, perde 50 HP.
  - **Round 3** — mesmo padrão: P 20, E 16→26 bust. Enemy HP → 0, combate acaba.
- **Eventos didácticos:** `Assets/Scripts/Tutorial/Onboarding/TutorialCoreEventIds.cs` — `NewRoundStarted`, `EnemyTurnTaken` (após cada acção do inimigo), `ReturnedToPlayerTurn`, `RoundSummaryShown`, `RoundContinue` (clique/tecla para seguir depois do resumo) e `BattleEnded`. O controller dispara-os em pontos exactos e os passos `TUTORIAL/core/tutorial_core_steps.json` avançam sobre eles.
- **Passos (14):** `TUTORIAL/core/tutorial_core_steps.json` + textos em `TUTORIAL/core/tutorial_core_strings.json`. Alternância entre **continue button** (intro + setup de cada rodada) e **OnEvent** (pressionar Hit/Stand, observar turno do inimigo, ler resumo). Alvos: nomes `BtnHit`/`BtnStand` resolvidos por busca recursiva no Canvas.
- **Save:** `MainProfileData.core_onboarding_completed` (global) + `SaveData.core_onboarding_step_index` (retomada). Novos helpers em `SaveManager` (`SetCoreOnboardingCompleted`, `Load/Save/ClearCoreOnboardingStepIndex`). `TutorialProgressTracker` trata `TutorialIds.CoreOnboarding` — ao concluir, marca também `main_tutorial_completed = true`, fechando a cadeia. O `MapOnboarding` deixou de marcar `main_tutorial_completed` (essa bandeira agora é exclusiva do fim do `CoreTutorial`).
- **Cadeia do menu:** `MenuPrincipalManager.Jogar` passa a considerar `core_onboarding_completed`; entre `map_onboarding_completed` e `core_onboarding_completed` falsos, devolve o jogador ao `MapTutorial` (onde o nó (0,0) continua a ser o único clicável).
- **Pós-combate:** `CoreTutorialBlackjackController.EndBattleRoutine` persiste o progresso via tracker, força `SaveManager.ActiveContext = Campaign` e carrega a cena `Menu` (2,5 s de pausa no ecrã de vitória). Como `main_tutorial_completed == true`, o modal de tutorial **não** reabre e o `Jogar` seguinte vai direto ao `Map` da campanha.
- **Cena:** além do swap do controller, foi adicionado um GameObject raiz `CoreTutorialOnboarding` com `CoreTutorialGuidedSession` (execution order `-500`) + `TutorialManager` (`_tutorialId = core_onboarding`, `_respectCompletedFlag = 1`) + `CoreTutorialGuidedUi` (desativa o `BtnNewGame` herdado do clone).
- **Ficheiros criados:** `Assets/Scripts/Tutorial/CoreTutorial/{CoreTutorialDeck,CoreTutorialRoundRig,CoreTutorialBlackjackGame,CoreTutorialBlackjackController,CoreTutorialGuidedSession,CoreTutorialGuidedUi}.cs`; `Assets/Scripts/Tutorial/Onboarding/TutorialCoreEventIds.cs`; `Assets/StreamingAssets/TUTORIAL/core/{tutorial_core_rounds,tutorial_core_steps,tutorial_core_strings}.json`.
- **Ficheiros tocados:** `Assets/Scripts/Core/Save/{MainProfileData,SaveData,SaveManager}.cs`; `Assets/Scripts/Tutorial/Onboarding/{TutorialIds,TutorialProgressTracker}.cs`; `Assets/Scripts/Menus/MenuPrincipalManager.cs`; `Assets/Scenes/CoreTutorial.unity`.

### 2026-04-16 — `CoreTutorial`: HP 1000, sandbox livre, botão ENCERRAR, sincronização de corrotinas e revisão pt-BR

Revisão do fluxo Default → Map → Core após feedback. O tutorial do combate deixa de ser "só 3 rodadas e fim" e passa a oferecer um **modo livre** para o jogador praticar à vontade.

- **Linguagem pt-BR, direta e didática.** Todos os textos de `Assets/StreamingAssets/TUTORIAL/core/tutorial_core_strings.json` foram reescritos em português brasileiro com explicações curtas e exemplos concretos. Inclui um novo passo **teoria da multiplicação** (`core_00b_multi_theory`) antes da primeira rodada, explicando multiplicação como soma repetida (`2 × 3 = 2 + 2 + 2 = 6`, etc.).
- **HP 1000/1000 no combate tutorial.** `TUTORIAL/core/tutorial_core_rounds.json` sobe `playerMaxHealth` e `enemyMaxHealth` para 1000 (o rig das 3 rodadas continua a aplicar 50 de dano cada — fim das rodadas guiadas: P 950 / E 900). Sobra HP de sobra para o sandbox.
- **Modo sandbox.** Quando o controller precisa duma 4ª rodada em diante, entra em sandbox: `CoreTutorialDeck.LoadShuffled` (Fisher-Yates) usa o pool lido de `Assets/StreamingAssets/Easy/config_cards_easy_multiplication.json` (via `DeckConfig.Load`). O HUD passa a mostrar "Rodada N (treino livre)".
- **Botão ENCERRAR.** Novo botão vermelho no canto superior direito (`CoreTutorialGuidedUi` cria em runtime se não existir). Fica oculto durante as 3 rodadas guiadas e aparece quando `TutorialManager.CompletedOrSkipped` dispara. Clique → `CoreTutorialBlackjackController.EndBattle()` → persiste `core_onboarding_completed = true` e `main_tutorial_completed = true`, força `ActiveContext = Campaign` e carrega `Menu`. Morte em sandbox (HP 0 em qualquer lado) também chama `EndBattle`.
- **Sincronização tutorial ↔ corrotina.** Problema original: passos ligados a eventos de corrotina (`EnemyTurnTaken`, `RoundSummaryShown`) avançavam antes do jogador ter tempo de ler, pois o evento disparava 0.8–2 s após o início da explicação. Solução: o passo passa a `advanceMode = 2` (ContinueButton) e a corrotina chama `CoreTutorialBlackjackController.PauseIfTutorialExplaining()` logo após disparar o feedback visual. Este helper consulta a nova propriedade pública `TutorialManager.CurrentStepId` (adicionada ao manager) e faz `yield return null` enquanto o passo atual for `ContinueButton` e ainda não avançou. Resultado: a UI do combate congela no último estado visível até o jogador clicar em **Continuar** no tooltip.
- **Passos atualizados (15).** `TUTORIAL/core/tutorial_core_steps.json` inclui agora: `intro_combat`, `multi_theory` (novo), `round1_setup`, `round1_press_hit` (OnEvent), `round1_observe_enemy` (ContinueButton), `round1_press_stand` (OnEvent), `round1_summary` (ContinueButton), `round2_setup`, `round2_press_stand` (OnEvent), `round2_enemy_busts` (ContinueButton), `round2_summary` (ContinueButton), `round3_setup`, `round3_press_stand` (OnEvent), `round3_enemy_busts` (ContinueButton), `round3_summary` (ContinueButton), `sandbox_intro` (ContinueButton, apresenta o botão ENCERRAR).
- **Validação da cadeia.** Reconfirmado: `DefaultBlackjackGuidedUi.GoToMap` → `GameFlowScenes.CurrentMap` (=`MapTutorial` em contexto Tutorial) → `NodeInteraction.SelectNode` no nó (0,0) → `GameFlowScenes.CurrentCore` (=`CoreTutorial` em contexto Tutorial) → sandbox → ENCERRAR → `Menu` (campanha, sem modal porque `main_tutorial_completed = true`).
- **Ficheiros criados neste ajuste:** nenhum (script reaproveita `CoreTutorial*` existentes).
- **Ficheiros tocados:** `Assets/Scripts/Tutorial/Onboarding/TutorialManager.cs` (nova `CurrentStepId`); `Assets/Scripts/Tutorial/CoreTutorial/{CoreTutorialDeck,CoreTutorialBlackjackGame,CoreTutorialBlackjackController,CoreTutorialGuidedUi}.cs`; `Assets/StreamingAssets/TUTORIAL/core/{tutorial_core_rounds,tutorial_core_steps,tutorial_core_strings}.json`; `docs/TUTORIAL_ONBOARDING.md`.

### 2026-04-20 — `StreamingAssets/TUTORIAL/`: pastas por fluxo + `TutorialContentPaths`

- **Organização:** todos os JSON de tutorial deixaram a raiz de `Assets/StreamingAssets/` e passaram para `TUTORIAL/blackjackguided/`, `TUTORIAL/defaultblackjack/`, `TUTORIAL/map/` e `TUTORIAL/core/` (cada uma com o respectivo `.meta` de pasta).
- **Código:** novo `Assets/Scripts/Tutorial/TutorialContentPaths.cs` com constantes dos caminhos relativos à raiz de `StreamingAssets`; `DefaultBlackjackGuidedSession`, `DefaultBlackjackController`, `MapTutorialOnboardingSession`, `CoreTutorialGuidedSession` e `CoreTutorialBlackjackController` usam essas constantes nos defaults.
- **Cenas:** `TutorialDefaultBlackjack.unity`, `CoreTutorial.unity` e `Assets/_Recovery/0 (4).unity` — campos serializados de ficheiros JSON atualizados para os novos caminhos.
- **Documentação:** `docs/TUTORIAL_ONBOARDING.md` (secção de layout + tabelas do Core); entradas históricas em `context.md` que citavam `Assets/StreamingAssets/tutorial_*.json` alinhadas aos novos caminhos.

### 2026-04-21 — Wiki / Ajuda (UI Toolkit) + revisão de teoria no início do duelo (Core)

- **UI:** `Assets/UI/Wiki/WikiView.uxml` + `WikiStyles.uss`; controlador `Assets/Scripts/Map/MapWikiAccess.cs` (Wiki servida globalmente por `PersistenteGlobalChrome`/`Resources/UI/Wiki` desde 2026‑05‑23; anteriormente instâncias em Map/Core estavam nas respectivas cenas). Conteúdo por abas (`WikiTab_*` / `WikiPage_*`); mapeamento teoria ↔ tipo de nó em `Assets/Scripts/Map/TheoryWikiPage.cs` (`TheoryWikiPageMapping.FromMapNodeType` / `ToTabId` / `DisplayName`).
- **Revisão automática (Core):** `BlackjackController` inicia a mesa (`ApplyNewRoundStateBody` sem parar a corrotina de arranque), espera frames para o layout, e se o jogador **não** tiver pedido para saltar aquele tipo, chama `MapWikiAccess.OpenForRevision(TheoryWikiPage)` — reutiliza o mesmo modal: **sem sidebar**, **sem botão X**, título **«Revisão»**, rodapé com **Prosseguir** e toggle **«Não mostrar novamente para &lt;tipo&gt;»** (só **Prosseguir** fecha; **Esc** desligado neste modo). `IsOpen` exposto para corrotinas que esperam o fecho.
- **Persistência:** `SaveData.theoryRevisionSkippedTabIds` (ids de aba, ex. `math_add`); `MapWikiAccess.IsRevisionSkippedForPage` + gravação no fecho quando o toggle está marcado.
- **Input na Core:** com a wiki aberta, o input do canvas de combate fica suprimido (`CanvasGroup` no canvas principal) até fechar.
- **Overlay a largura total do Game View:** o `rootVisualElement` da wiki é dimensionado/posicionado com `RuntimePanelUtils.ScreenToPanel` + `GeometryChangedEvent` no `panel.visualTree` para não ficar só na faixa central quando o PanelSettings / aspect do Editor não coincidem com o ecrã.
- **Corrotinas:** `OnNewRound` agenda `StopAllCoroutines` via `Invoke` no frame seguinte para não cortar a corrotina de arranque que ainda está a correr.
- **Removido do projeto:** modal UXML/C# dedicado `TheoryRevisionModal` (fluxo unificado na wiki).

### 2026-04-22 — Nó `Bossfight` (Fractar), balance Hard e wiki

- **Mapa:** `MapNodeType.Bossfight` em `MapNode.cs`; `MapGenerator` após `AssignNodeTypesAndDifficulty` chama `InsertBossFightNode()` (omitido em `MapTutorial`): linha extra `row == rows`, coluna `bossFightColumn` (-1 = centro); todos os nós `(rows-1, *)` ligam ao boss. `Bossfight` com peso **0** no sorteio (`SyncTypeWeights` força peso 0; `PickRandomType` / `CalculateTotalWeight` ignoram o tipo).
- **Visual:** `BossFightNodeVisual` + campo em `MapVisualizer` (cor, `Sprite` ícone, `nodeScale`); label **Fractar (Boss final)**.
- **Combate:** `BossFightBalance` (600 HP, dano ×2); `BlackjackController.Awake` aplica para `RunState.CurrentNodeType == Bossfight`; revisão automática da wiki **não** abre no boss. `StreamingAssetsDeckPaths` devolve `Boss/config_cards_boss.json` para o boss.
- **Dificuldade global:** `EnemyCombatBalance.GetEnemyDamageMultiplier(Hard)` = **1,75** (antes 2).
- **Vitória no boss:** `BattleRewardResolver` com `Hard` (+500 moedas); reset de posição/seed/`currentRun` como na derrota; `SceneManager.LoadScene(GameFlowScenes.Menu)`. Constante `GameFlowScenes.Menu`.
- **Wiki:** `WikiPage_difficulty` — linha Difícil ×1,75; nova linha Boss + secção Fractar; texto do multiplicador ajustado. (Em 2026-04-23 a tabela numérica passou para a aba Combate; ver entrada «Catálogo da loja» no mesmo dia para Economia.)

### 2026-04-23 — Catálogo da loja: novos itens, ícones e acumulação attack/defense

- **`item_catalog.json`:** 8 itens — poção pequena (cura 100, 120 moedas), poção grande (250, 320); ofensivos **Anel da Inequação** (×1,15, 220), **Tomos Lineares** (×1,35, 480), **Cajado Fractal** (×1,5, 820); defensivos **Amuleto Progressivo** (+100 HP, 300), **Escudo Hipotenusa** (+150, 560), **Túnica Fractal** (+250, 980). Preços subindo com o poder; recompensas de vitória em duelo: **100 / 250 / 500** moedas (Fácil / Médio / Difícil), conforme `BattleRewardResolver`.
- **`PlayerItemStats.CalculateDamageMultiplier`:** deixa de multiplicar entrada a entrada; passa a **`1 + Σ(attackMultiplier - 1)`** por cada entrada ofensiva em `ownedItemIds` (incluindo repetições).
- **`PlayerItemStats.CalculateMaxHealth`:** mantém **`100 + Σ bonusHealth`** por entrada defensiva (já compatível com ids repetidos).
- **`StoreController`:** removido bloqueio «Comprado» por `Contains(id)` em attack/defense; após compra permanente chama-se `LoadStoreData()` como nos consumíveis.
- **`ItemId.cs` + `ItemDefinition.ItemId`:** mapeamento para os novos `id` do JSON.
- **`SaveData.ownedItemIds`:** comentário de documentação sobre repetição de `id`.
- **Arte:** removidos placeholders antigos `potion_icon.png`, `shield_icon.png`, `sword_icon.png`, `amulet_icon.png`; ícones actuais com nomes em ficheiro sem espaços sob `Resources/Icons/`.
- **Wiki (`WikiView.uxml` + `MapWikiAccess` + cenas Map/Core):** nova aba **Economia** (`WikiTab_economy` / `WikiPage_economy`) concentra moedas, loja, equipamento e consumíveis; removida a aba **Loja e inventário**. A aba **Combate** passa a ter a **tabela** de dificuldade (vida do oponente, mult. de dano ao jogador ao perder a rodada, moedas na vitória, incluindo boss); a aba **Dificuldade** fica narrativa + remissão à tabela da aba Combate. **Visão geral** remete à Economia em vez de detalhar a loja. Tabelas alinhadas a `EnemyCombatBalance`, `BattleRewardResolver` e `BossFightBalance`; **`MapWikiAccess.SanitizeLegacyTabIds`** + `OnValidate` convertem `shop_inventory` → `economy` em `tabIds` antigos do Inspector.

### 2026-05-07 — Baralho do boss: `Boss/config_cards_boss.json`

- **`StreamingAssetsDeckPaths`:** confronto `MapNodeType.Bossfight` passa a resolver `StreamingAssets/Boss/config_cards_boss.json` em vez de `Easy/config_cards_easy_multiplication.json`. O nó de boss mantém `CombatEquationDifficulty.Hard` no mapa (recompensas/`BattleRewardResolver`); apenas a fonte do JSON de cartas mudou.

### 2026-05-07 — Boss Fractar: 600 HP

- **`BossFightBalance.MaxHealth`:** 500 → **600**; tabelas/copy na wiki (`WikiView.uxml`, linhas Boss) alinhadas.

### 2026-05-07 — Vitória no boss: wipe de save + agradecimento no menu

- Após o vídeo final (ou a espera se não houver vídeo), **antes** de `LoadScene(Menu)`: `MainMenuTransitionState.RequestRunCompleteThanks()`, `RunState.ClearVolatileBattleContext()`, `SaveManager.Delete()` (apaga perfiles + campanha + tutorial + legado + `.migrated.bak`, repõe `_migrationChecked`).
- **`MenuPrincipalManager.Start`:** se `ConsumeRunCompleteThanks()`, mostra overlay uGUI com mensagem de agradecimento e **Continuar** (`Destroy` do painel).
- Ficheiros: `MainMenuTransitionState.cs`, `BlackjackController.BossOutroCompleteAndGoToMenu`, `SaveManager.Delete` alargado, `RunState.ClearVolatileBattleContext`.

### 2026-05-10 — BGM por cena, fades e *duck* em vídeo fullscreen

- **Config:** `Assets/StreamingAssets/Music/scene_music_config.json` (mapeamento `sceneName` → `trackResourcePath` em Resources; `defaultTrackResourcePath`; por entrada `disableRandomStart`). Loader e DTOs em `Assets/Scripts/Songs/SceneMusicConfig.cs` (`SceneMusicConfigLoader`, `JsonUtility`).
- **Faixa:** `Assets/Resources/Music/watermelon_beats-medieval-folk-music-505203.mp3` (mesmo GUID que antes em `Assets/Songs/`; ficheiro retirado de `Songs/`). `Resources.Load<AudioClip>` sem extensão.
- **`MusicManager`:** volume = utilizador × fade; `PlayTrackResource`, `BeginExclusiveAudio` / `EndExclusiveAudio` (coroutines com `unscaledDeltaTime`); `PlayTrackResource` cancela fades e repõe factor 1. Singleton `DontDestroyOnLoad` inalterado na raiz do Menu.
- **`SceneMusicDirector`:** `Assets/Scripts/Songs/SceneMusicDirector.cs` na cena `Menu.unity` (GameObject `MusicManager`); subscreve `sceneLoaded` (ignora `Additive`); resolve config e aplica faixa.
- **Vídeo:** `FullscreenVideoOverlay.PlayRequest` com `duckBackgroundMusic`, `duckFadeOutSeconds`, `restoreFadeInSeconds` (opcionais); `IntroVideoPlayer` e `BossOutroFlow` activam *duck* nos fluxos com overlay. `BlackjackController` continua a usar apenas `BossOutroFlow` para o *outro*.
- **Documentação produto:** secção **8.3 Música (BGM por cena)** neste ficheiro.

### 2026-05-10 — *Pre-warm* de vídeos fullscreen (intro + outro boss)

- **`VideoWarmupService`** (`Assets/Scripts/Video/VideoWarmupService.cs`): singleton `DontDestroyOnLoad`; chama `VideoPlayer.Prepare()` antecipadamente para o mesmo clip ou path em StreamingAssets que `FullscreenVideoOverlay` vai usar depois.
- **`StreamingAssetsVideoUrl`:** normalização de path relativo + URL sob `streamingAssetsPath`, partilhada com o overlay.
- **`FullscreenVideoOverlay`:** antes de criar novo `VideoPlayer`, tenta **`TryTakePreparedMatching`** no serviço; se coincidir, reutiliza o host preparado e salta novo `Prepare()`.
- **Menu:** `MenuPrincipalManager.Start` — se `intro_video_seen == false`, `EnsureWarmMenuIntro(introVideoClip, introVideoStreamingPath)`.
- **Boss Fractar:** `BlackjackController.Awake` — com `Bossfight` e path do vídeo de vitória configurado, `WarmStreamingRelativePath`; ao **perder** o duelo contra o boss, `CancelWarmup()` liberta recursos caso o vídeo nunca reproduza.

### 2026-05-10 — Cartas de multiplicação: operador ASCII `x`

- **`ExpressionEvaluator`:** `x` / `X` tratados como multiplicação ao mesmo nível que `*` e `×`.
- **`StreamingAssets`:** equações nos baralhos de multiplicação (Easy/Medium/Hard), grupo boss com multiplicações, e `TUTORIAL/core/tutorial_core_rounds.json` — ` " * " ` → ` x ` nos textos de equação.

### 2026-05-10 — Cursor software global + coexistência com vídeo fullscreen

- **`SoftwareCustomCursor`** ([`Assets/Scripts/Menus/SoftwareCustomCursor.cs`](Assets/Scripts/Menus/SoftwareCustomCursor.cs)): singleton `DontDestroyOnLoad` no **`GameObject` `PersistentCustomCursor`** (`Menu.unity`); `Resources.Load<Sprite>("Cursor/cursor")`; Canvas **Screen Space Overlay** sem `GraphicRaycaster` (imagem `raycastTarget` falso).
- **`FullscreenVideoOverlay`:** **`ActiveOverlayCount`** incrementado quando `Play` instancia overlay com êxito; decremento em **`Complete()`** todas as saídas; **`ActiveOverlayCountChanged`** para o cursor esconder a UI própria e activar **`Cursor.visible`** durante o vídeo.
