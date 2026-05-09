using System.Collections.Generic;
using TMPro;
using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NodeInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapVisualizer mapVisualizer;
    [SerializeField] private Camera mainCamera;

    [Tooltip("Opcional. Se existir na cena, é desativado ao entrar no mapa (sem mensagens pós-batalha).")]
    [SerializeField] private TMP_Text txtTransitionMessage;

    private SaveData _save;
    private Vector2 _pressPosition;
    private bool _isPressed;
    private const float DragThreshold = 5f;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
                Debug.LogWarning("[NodeInteraction] mainCamera não atribuída no Inspector; usando Camera.main.");
            else
                Debug.LogError("[NodeInteraction] mainCamera e Camera.main são nulos — clique no mapa não funcionará.");
        }

        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (mapGenerator != null)
                Debug.LogWarning("[NodeInteraction] mapGenerator não atribuído no Inspector; usando FindFirstObjectByType<MapGenerator>().");
            else
                Debug.LogError("[NodeInteraction] Nenhum MapGenerator na cena.");
        }

        if (mapVisualizer == null)
        {
            mapVisualizer = FindFirstObjectByType<MapVisualizer>();
            if (mapVisualizer != null)
                Debug.LogWarning("[NodeInteraction] mapVisualizer não atribuído no Inspector; usando FindFirstObjectByType<MapVisualizer>().");
            else
                Debug.LogWarning("[NodeInteraction] Nenhum MapVisualizer na cena — opacidade de acessibilidade não será aplicada.");
        }

        _save = SaveManager.Load();
        HandleTransitionMessage();
        ApplyVisualAccessibility();
        AttachMapTutorialNodePulseIfNeeded();
    }

    /// <summary>Adiciona o pulso no nó (0,0) enquanto o jogador ainda não escolheu uma fase em <c>MapTutorial</c>.</summary>
    private void AttachMapTutorialNodePulseIfNeeded()
    {
        if (SceneManager.GetActiveScene().name != GameFlowScenes.MapTutorial)
            return;
        if (_save.playerRow != -1 || _save.playerCol != -1)
            return;
        if (mapVisualizer == null)
            return;
        if (!mapVisualizer.TryGetNodeTransform(0, 0, out Transform nodeTf) || nodeTf == null)
            return;
        if (nodeTf.GetComponent<MapTutorialNodePulse>() == null)
            nodeTf.gameObject.AddComponent<MapTutorialNodePulse>();
    }

    private void ApplyVisualAccessibility()
    {
        if (mapVisualizer == null) return;

        var accessible = GetAccessibleNodeKeys();
        mapVisualizer.ApplyAccessibility(accessible, _save.playerRow, _save.playerCol);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        TryDevTriggerBossOutroFlowFromMap();

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 press = mouse.position.ReadValue();
            if (MapUiRaycasts.IsScreenPositionOverUi(press))
                _isPressed = false;
            else
            {
                _isPressed = true;
                _pressPosition = press;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame && _isPressed)
        {
            _isPressed = false;
            Vector2 releasePos = mouse.position.ReadValue();
            if (Vector2.Distance(_pressPosition, releasePos) < DragThreshold)
            {
                if (MapUiRaycasts.IsScreenPositionOverUi(releasePos))
                    return;
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(releasePos.x, releasePos.y, 0f));
                TrySelectNode(new Vector2(worldPos.x, worldPos.y));
            }
        }
    }

    private void TrySelectNode(Vector2 worldPos)
    {
        MapNode closest = null;
        float closestDist = float.MaxValue;
        const float clickRadius = 0.5f;

        foreach (var node in mapGenerator.Graph.Values)
        {
            float dist = Vector2.Distance(worldPos, node.WorldPosition);
            if (dist < clickRadius && dist < closestDist)
            {
                closest = node;
                closestDist = dist;
            }
        }

        if (closest == null) return;

        if (!IsAccessible(closest))
        {
            Debug.Log($"[NodeInteraction] Node ({closest.Row}, {closest.Col}) não é acessível.");
            return;
        }

        SelectNode(closest);
    }

    public bool IsAccessible(MapNode node)
    {
        if (_save.playerRow == -1 && _save.playerCol == -1)
        {
            // No tutorial do mapa, só o nó (0,0) é selecionável na primeira escolha.
            if (SceneManager.GetActiveScene().name == GameFlowScenes.MapTutorial)
                return node.Row == 0 && node.Col == 0;
            return node.Row == 0;
        }

        return mapGenerator.ConnectionSet.Contains((
            _save.playerRow, _save.playerCol,
            node.Row, node.Col
        ));
    }

    public HashSet<(int row, int col)> GetAccessibleNodeKeys()
    {
        var accessible = new HashSet<(int row, int col)>();

        if (_save.playerRow == -1 && _save.playerCol == -1)
        {
            bool tutorial = SceneManager.GetActiveScene().name == GameFlowScenes.MapTutorial;
            foreach (var node in mapGenerator.Graph.Values)
            {
                if (node.Row != 0)
                    continue;
                if (tutorial && node.Col != 0)
                    continue;
                accessible.Add((node.Row, node.Col));
            }
        }
        else
        {
            foreach (var edge in mapGenerator.Connections)
            {
                if (edge.fromRow == _save.playerRow && edge.fromCol == _save.playerCol)
                    accessible.Add((edge.toRow, edge.toCol));
            }
        }

        return accessible;
    }

    private void SelectNode(MapNode node)
    {
        RunState.CurrentNodeType = node.Type;
        RunState.CurrentCombatDifficulty = node.Difficulty;

        _save.playerRow = node.Row;
        _save.playerCol = node.Col;
        SaveManager.Save(_save);

        // Permite ao TutorialManager (em MapTutorial) marcar a etapa de spotlight como concluída
        // antes da transição de cena.
        EventBridge.TriggerEvent(TutorialMapEventIds.NodeSelected);

        SceneManager.LoadScene(GameFlowScenes.CurrentCore);
    }

    private void HandleTransitionMessage()
    {
        if (txtTransitionMessage != null)
            txtTransitionMessage.gameObject.SetActive(false);
        RunState.LastBattleResult = BattleResult.None;
    }

    /// <summary>
    /// TODO(remover após testes): <b>Shift+F12</b> na cena Map — mesmo fluxo que vitória no
    /// boss (vídeo <c>Cutscenes/fim.mp4</c> + wipe + menu com agradecimento).
    /// </summary>
    private void TryDevTriggerBossOutroFlowFromMap()
    {
        if (SceneManager.GetActiveScene().name != GameFlowScenes.Map)
            return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (!kb.leftShiftKey.isPressed && !kb.rightShiftKey.isPressed) return;
        if (!kb.f12Key.wasPressedThisFrame) return;

        BossOutroFlow.BeginReturnToMenuWithOutroVideo("Cutscenes/fim.mp4", 1f);
    }

}
