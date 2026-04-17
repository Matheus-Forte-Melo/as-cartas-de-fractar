using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TEMPORÁRIO — botão na cena Map que abre a loja. Não faz parte do GDD (loja na run está cortada).
/// Remover ao integrar a loja no fluxo real: apague este script + componente na cena Map.
/// Documentado em context.md §11 — "ATENÇÃO — Loja temporária no mapa".
/// </summary>
public class TemporaryMapStoreAccess : MonoBehaviour
{
    private const string RuntimeUiChildName = "TEMP_MapShopButtonRoot";
    private static string StoreSceneName => GameFlowScenes.CurrentStore;

    [Tooltip("Se falso, não cria UI; útil para testar o mapa sem o atalho.")]
    [SerializeField]
    private bool createRuntimeUi = true;

    private void Start()
    {
        if (!createRuntimeUi)
            return;

        Canvas canvas = FindTransitionCanvasOrFirst();
        if (canvas == null)
        {
            Debug.LogWarning("[TemporaryMapStoreAccess] Nenhum Canvas na cena Map — botão da loja não criado.");
            return;
        }

        if (canvas.transform.Find(RuntimeUiChildName) != null)
            return;

        BuildShopButton(canvas.transform);
    }

    private static Canvas FindTransitionCanvasOrFirst()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == "TransitionCanvas")
                return c;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void BuildShopButton(Transform canvasTransform)
    {
        var root = new GameObject(RuntimeUiChildName, typeof(RectTransform));
        root.transform.SetParent(canvasTransform, false);

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(200f, 44f);

        var image = root.AddComponent<Image>();
        image.color = new Color(0.25f, 0.18f, 0.32f, 0.94f);

        var button = root.AddComponent<Button>();
        button.onClick.AddListener(GoToStore);

        var textGo = new GameObject("Caption", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text = "Loja [TEMP]";
        text.fontSize = 16;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary> Também pode ligar um Button do Inspector a este método, se no futuro houver UI estática. </summary>
    public void GoToStore() => SceneManager.LoadScene(StoreSceneName);
}
