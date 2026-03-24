# As Cartas de Fractar — GDD Atualizado (v2.0)
> Atualizado com base no Replanejamento de Escopo (fev/2026). Itens **cortados** foram removidos. Itens despriorizados foram mantidos com marcação `[BAIXA PRIORIDADE]`.

---

## Regras para o assistente de código

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

> **O total da mão não é exibido.** O jogador calcula sozinho — esse é o núcleo pedagógico do jogo.

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

- **Cartas do jogador:** exibidas na parte inferior da mesa. Equações mostradas sem resultado.
- **Cartas do inimigo:** parte superior. Uma carta pode iniciar virada.
- **Pontuação:** o total da mão **não é exibido** — o jogador calcula sozinho.
- **Limite (21):** exibido centralmente, adjacente ao cristal fractal.
- **Vida do jogador:** barra horizontal abaixo da mesa.
- **Vida do inimigo:** barra horizontal acima da mesa.
- **Botões de ação:** "Comprar Carta", "Jogar Habilidade", "Passar Turno" — parte inferior da tela.
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

- Refatorado `Assets/Scripts/Core/Duelist.cs`: removido campo privado `_health` e simplificado para auto-property `Health`; adicionado `HasStood` para estado de turno.
- Criado `Assets/Scripts/Core/Enemy.cs`: nova classe `Enemy : Duelist` com `StandThreshold`, `IsFirstCardHidden` e método `ShouldHit(int handLimit)`.
- Refatorado `Assets/Scripts/Core/BlackjackGame.cs`: removidas flags locais de "stand" e migrada lógica para `Player.HasStood` / `Enemy.HasStood`; `Enemy` agora é tipado como classe própria e decisão de compra usa `Enemy.ShouldHit(...)`.
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
  - `SaveData.cs` — dados persistidos: `currentRun`, `playerRow`/`playerCol`, `coins`, `currentSeed`, `playerHealth`, `seedHistory`.
  - `SeedHistoryEntry.cs` — registro de seed por run (estrutura pronta para futura UI de replay).
  - `SaveManager.cs` — classe estática com `Load()`, `Save()`, `Delete()`. Cria defaults seguros no primeiro load (run=1, pos=(-1,-1), seed aleatória, health=100).
- Criado `RunState.cs` — classe estática para transporte de dados voláteis entre cenas (`CurrentNodeType`, `LastBattleResult`).
- `MapGenerator.cs` — inicialização movida para `Awake()` em play mode; seed carregada do `SaveData`; adicionados `Seed` property, `GenerateWithSeed()`, `ConnectionSet`.
- `BlackjackController.cs` — carregamento de deck por tipo de node via mapeamento de prefixos (`combat_add_cards.json`, etc.) com fallback para `config_cards.json`; vida do player carregada do save; detecção de fim de batalha (health ≤ 0) com lógica de morte/reset de run ou vitória; transição automática de volta à cena Map.
- Criado `NodeInteraction.cs` — seleção de nodes no mapa via clique (com threshold para distinguir de drag); acessibilidade: pos (-1,-1) habilita qualquer node da row 0, senão apenas nodes conectados; ao selecionar, salva posição e carrega cena Core; exibe mensagem de transição ao retornar do Core.
- `MapVisualizer.cs` — adicionado `ApplyAccessibility()` que reduz opacidade (alpha ~0.3) de nodes e conexões inacessíveis; destaque visual do node atual do jogador.