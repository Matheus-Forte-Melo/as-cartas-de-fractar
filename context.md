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

**Menu de Opções:** painel central sobre fundo desfocado. Volume e ajustes de vídeo.

**Menu de Pause:** pausa o jogo, fundo desfocado. Botões: Resumir / Opções / Voltar à tela inicial.

**Hub / Tela de Itens:** estética de mesa de matemático arcanista. Exibe moedas atemporais, catálogo de melhorias permanentes e botão "Adentrar Torre".

---

## 9. Progressão e Hub

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
  - `Assets/Scripts/Core/Save/SaveData.cs` — dados persistidos: `currentRun`, `playerRow`/`playerCol`, `coins`, `currentSeed`, `playerHealth`, `seedHistory`.
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
    │   └── RoundDamageResolver.cs  # RoundDamageOutcome + dano fim de rodada
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
- **Cena `Assets/Scenes/Store.unity`** no Build Settings. Camera + `UIDocument` (UI Toolkit) + `StoreController`. Não acessível pelas demais cenas por enquanto; transição via `SceneManager.LoadScene("Store")`.

#### Sistema de itens (`Assets/Scripts/Items/`)

- **`ItemType.cs`:** enum `Attack`, `Defense`, `Consumable`.
- **`ItemId.cs`:** enum `SwordGold`, `ShieldSilver`, `HealthPotion`, `MagicAmulet`.
- **`ItemDefinition.cs`** + **`ItemCatalogData`:** classes `[Serializable]` com `id`, `displayName`, `description`, `type`, `price`, `icon`, `attackMultiplier`, `bonusHealth`, `consumableEffect`, `consumableValue`. Mapeamento `type` → `ItemType` e `id` → `ItemId` via switch expression.
- **`ItemCatalog.cs`:** classe estática; carrega `StreamingAssets/Items/item_catalog.json` (lazy, indexado por `id`). Expõe `All` e `Get(string id)`.
- **`PlayerItemStats.cs`:** `CalculateMaxHealth(SaveData)` = `100 + soma bonusHealth dos defense owned`; `CalculateDamageMultiplier(SaveData)` = `produto attackMultiplier dos attack owned`.
- **JSON:** `Assets/StreamingAssets/Items/item_catalog.json` com 4 itens de exemplo (Espada de Ouro, Escudo de Prata, Poção de Vida, Amuleto Mágico).

#### Save (`Assets/Scripts/Core/Save/SaveData.cs`)

- **Novo campo:** `List<string> ownedItemIds` — IDs dos itens comprados.
- Todos os itens owned são automaticamente ativos (sem equip slots por enquanto).
- **Na derrota:** `save.playerHealth = PlayerItemStats.CalculateMaxHealth(save)` (antes era hardcoded 100).

#### Duelist (`Assets/Scripts/Core/Blackjack/Core/Duelist.cs`)

- **Novos campos:** `MaxHealth` (int, default 100), `DamageMultiplier` (float, default 1f).
- **`BlackjackController.Awake`:** após carregar save, seta `Player.MaxHealth` e `Player.DamageMultiplier` via `PlayerItemStats`.
- **`RoundDamageResolver`:** quando dano é ao enemy, multiplica pelo `player.DamageMultiplier`.

#### Recompensas (`Assets/Scripts/Core/Blackjack/Rewards/BattleRewardResolver.cs`)

- **`GetCoinReward`:** Easy=10, Medium=50, Hard=100.
- **`ApplyVictoryRewards(SaveData, CombatEquationDifficulty)`:** soma moedas ao save. Retorna quantidade.
- Chamado em `BlackjackController.EndBattleRoutine` na vitória, **antes** de `SaveManager.Save`. UI mostra `+N moedas`.
- **Extensível:** ponto de adição para drops de itens consumíveis no futuro.

#### Loja (`Assets/Scripts/Store/StoreController.cs`)

- **Reescrito** para usar `ItemCatalog` (não mais JSON mock). Saldo vem de `SaveData.coins`.
- **Compra:** deduz moedas, adiciona `id` a `save.ownedItemIds`, aplica `bonusHealth` à vida do jogador (defense), salva. Itens já comprados ficam desabilitados.
- **Consumíveis:** botão com label "Em breve" (placeholder, `ItemType.Consumable` existe mas sem lógica de uso).
- **UI migrada:** `Assets/UI/Store/` (StoreView.uxml, StoreItemView.uxml, StoreStyles.uss, DefaultPanelSettings.asset). Fundo: `Assets/Art/Store/MagicDungeonBackground.png`. Ícones: `Assets/Resources/Icons/`.
- **Botão Voltar:** `SceneManager.LoadScene("Map")`.

#### Árvore de novos arquivos

```
Scripts/
├── Items/
│   ├── ItemId.cs
│   ├── ItemType.cs
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
├── Espada de Ouro.png
├── Escudo de Prata.png
├── Poção de Vida.png
├── Amuleto Mágico.png
└── CoinFrames/ (1-6.png)

Scenes/
└── Store.unity
```

#### Extensibilidade

- **Consumíveis:** `ItemType.Consumable` + `consumableEffect`/`consumableValue` no JSON; implementação futura remove do `ownedItemIds` e aplica efeito.
- **Drops de itens:** `BattleRewardResolver` pode ser estendido com tabela de drops por dificuldade.
- **Equip slots:** futuramente adicionar `equippedItemIds` ao save para separar posse de uso.
- **Novos itens:** adicionar entrada no `item_catalog.json` + novo valor no `ItemId` enum.
