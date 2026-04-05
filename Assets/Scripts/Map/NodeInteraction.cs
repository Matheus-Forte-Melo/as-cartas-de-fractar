using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NodeInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private MapVisualizer mapVisualizer;
    [SerializeField] private Camera mainCamera;

    [Header("Transition Message")]
    [SerializeField] private TMP_Text txtTransitionMessage;
    [SerializeField] private float messageDuration = 2.5f;

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

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isPressed = true;
            _pressPosition = mouse.position.ReadValue();
        }

        if (mouse.leftButton.wasReleasedThisFrame && _isPressed)
        {
            _isPressed = false;
            Vector2 releasePos = mouse.position.ReadValue();
            if (Vector2.Distance(_pressPosition, releasePos) < DragThreshold)
            {
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
            return node.Row == 0;

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
            foreach (var node in mapGenerator.Graph.Values)
            {
                if (node.Row == 0)
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

        SceneManager.LoadScene("Core");
    }

    private void HandleTransitionMessage()
    {
        if (txtTransitionMessage == null) return;

        switch (RunState.LastBattleResult)
        {
            case BattleResult.Won:
                txtTransitionMessage.text = "Você ganhou, pode prosseguir";
                txtTransitionMessage.gameObject.SetActive(true);
                StartCoroutine(HideMessageAfterDelay());
                break;

            case BattleResult.Lost:
                txtTransitionMessage.text = "Você perdeu, voltando ao início...";
                txtTransitionMessage.gameObject.SetActive(true);
                StartCoroutine(HideMessageAfterDelay());
                break;

            default:
                txtTransitionMessage.gameObject.SetActive(false);
                break;
        }

        RunState.LastBattleResult = BattleResult.None;
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        if (txtTransitionMessage != null)
            txtTransitionMessage.gameObject.SetActive(false);
    }
}
