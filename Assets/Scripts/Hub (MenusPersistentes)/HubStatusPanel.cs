using Items;
using Store;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Painel Status do Hub: stats do save com layout fixo em pixels (não escala com a tela).
/// </summary>
[DisallowMultipleComponent]
public sealed class HubStatusPanel : MonoBehaviour
{
    [Header("Consumíveis — ícones por slot (RefreshConsumableSlots)")]
    [SerializeField] private float consumableIconPadding = 5f;

    [Header("Painel — posição/tamanho fixos (ref. 800×600)")]
    [SerializeField] private Vector2 panelSize = new Vector2(300f, 168f);
    [SerializeField] private Vector2 panelOffset = new Vector2(0f, 28f);
    [SerializeField] private Vector2 panelPadding = new Vector2(16f, 12f);
    [SerializeField] private bool counterCanvasScaler = true;

    [Header("Coluna única — BuildLayout")]
    [SerializeField] private float rowSpacing = 8f;

    [Header("Células fixas (px) — não auto-ajustam")]
    [SerializeField] private Vector2 labelSize = new Vector2(185f, 28f);
    [SerializeField] private Vector2 valueCellSize = new Vector2(68f, 28f);
    [SerializeField] private Vector2 consumableSlotSize = new Vector2(55f, 28f);
    [SerializeField] private float columnGap = 15f;

    [Header("Cores — backdrops")]
    [SerializeField] private Color panelBackdropColor = new Color(0f, 0f, 0f, 0.58f);
    [SerializeField] private Color valueBackdropColor = new Color(0.06f, 0.06f, 0.08f, 0.78f);
    [SerializeField] private Color consumableSlotColor = new Color(0.06f, 0.06f, 0.08f, 0.5f);

    [Header("Tipografia fixa")]
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private float fontSize = 16f;
    [SerializeField] private Color labelTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color valueTextColor = new Color(1f, 0.96f, 0.78f, 1f);

    [Header("Referências (auto após BuildLayout)")]
    [SerializeField] private TextMeshProUGUI textoMoedas;
    [SerializeField] private TextMeshProUGUI textoVidaMaxima;
    [SerializeField] private TextMeshProUGUI textoDanoMultiplicador;

    private Image _panelBackdrop;
    private Canvas _rootCanvas;
    private bool _built;
    private static Sprite _whiteSprite;
    private readonly Image[] _consumableSlotIcons = new Image[StoreController.MaxConsumableSlots];

    private void Start()
    {
        CaptureFontFromLegacyLabels();
        BuildLayout();
        ApplyFixedLayout();
        ApplyConstantPixelScale();
        RefreshFromSave();
    }

    private void OnEnable()
    {
        if (_built)
        {
            ApplyConstantPixelScale();
            RefreshFromSave();
        }
    }

    private void LateUpdate()
    {
        if (counterCanvasScaler && _built)
            ApplyConstantPixelScale();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled || !_built)
            return;

        ApplyFixedLayout();
        ApplyConstantPixelScale();
        RefreshFromSave();
    }
#endif

    /// <summary>Carrega save e preenche moedas, vida máxima e multiplicador de dano.</summary>
    public void RefreshFromSave()
    {
        SaveData save = SaveManager.Load();

        if (textoMoedas != null)
            textoMoedas.text = save.coins.ToString();

        if (textoVidaMaxima != null)
            textoVidaMaxima.text = PlayerItemStats.CalculateMaxHealth(save).ToString();

        if (textoDanoMultiplicador != null)
        {
            float mult = PlayerItemStats.CalculateDamageMultiplier(save);
            textoDanoMultiplicador.text = PlayerItemStats.FormatDamageMultiplier(mult);
        }

        RefreshConsumableSlots(save);
    }

    /// <summary>Preenche ícones dos slots a partir de <see cref="SaveData.consumableSlots"/>.</summary>
    private void RefreshConsumableSlots(SaveData save)
    {
        var slots = save.consumableSlots;

        for (int i = 0; i < StoreController.MaxConsumableSlots; i++)
        {
            Image icon = _consumableSlotIcons[i];
            if (icon == null)
                continue;

            string itemId = slots != null && i < slots.Count ? slots[i] : null;
            if (string.IsNullOrEmpty(itemId))
            {
                icon.enabled = false;
                icon.sprite = null;
                continue;
            }

            ItemDefinition def = ItemCatalog.Get(itemId);
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.icon))
                sprite = Resources.Load<Sprite>(def.icon);

            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }

    private void CaptureFontFromLegacyLabels()
    {
        if (fontAsset != null)
            return;

        foreach (TextMeshProUGUI legacy in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (legacy.font != null)
            {
                fontAsset = legacy.font;
                break;
            }
        }
    }

    private void BuildLayout()
    {
        if (_built)
            return;

        ClearLegacyChildren();

        _panelBackdrop = GetComponent<Image>();
        if (_panelBackdrop == null)
            _panelBackdrop = gameObject.AddComponent<Image>();
        EnsureImageSprite(_panelBackdrop);
        _panelBackdrop.raycastTarget = false;
        _panelBackdrop.color = panelBackdropColor;

        int rowCount = 4;
        float rowH = Mathf.Max(labelSize.y, consumableSlotSize.y);
        float step = rowH + rowSpacing;
        float statRowWidth = labelSize.x + columnGap + valueCellSize.x;
        float slotsWidth = consumableSlotSize.x * StoreController.MaxConsumableSlots
                           + columnGap * (StoreController.MaxConsumableSlots - 1);
        float consumableRowWidth = labelSize.x + columnGap + slotsWidth;
        float contentWidth = Mathf.Max(statRowWidth, consumableRowWidth);
        float contentHeight = rowCount * rowH + (rowCount - 1) * rowSpacing;

        panelSize = new Vector2(contentWidth + panelPadding.x * 2f, contentHeight + panelPadding.y * 2f);

        BuildStatRow("Row_Moedas", "Moedas:", RowCenterY(0, rowCount, rowH), out textoMoedas);
        BuildStatRow("Row_VidaMaxima", "Vida Máxima:", RowCenterY(1, rowCount, rowH), out textoVidaMaxima);
        BuildStatRow("Row_DanoMultiplicador", "Dmg. Mult.:", RowCenterY(2, rowCount, rowH), out textoDanoMultiplicador);
        BuildConsumableRow(RowCenterY(3, rowCount, rowH));

        _built = true;
    }

    /// <summary>Posição Y da linha i (0 = topo) centrada no painel.</summary>
    private float RowCenterY(int index, int rowCount, float rowHeight)
    {
        float total = rowCount * rowHeight + (rowCount - 1) * rowSpacing;
        float top = total * 0.5f - rowHeight * 0.5f;
        return top - index * (rowHeight + rowSpacing);
    }

    private void ClearLegacyChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    private void BuildStatRow(string rowName, string labelText, float rowCenterY, out TextMeshProUGUI valueText)
    {
        var row = CreateRect(rowName, transform);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, rowCenterY);
        row.sizeDelta = new Vector2(labelSize.x + columnGap + valueCellSize.x, labelSize.y);

        var label = CreateLabel("Label", row, labelText, labelSize, TextAlignmentOptions.MidlineLeft, labelTextColor, FontStyles.Bold);
        label.anchoredPosition = new Vector2(-valueCellSize.x * 0.5f - columnGap * 0.5f, 0f);

        var valueCell = CreateBackdropCell("ValueCell", row, valueCellSize, valueBackdropColor);
        valueCell.anchoredPosition = new Vector2(labelSize.x * 0.5f + columnGap * 0.5f, 0f);

        valueText = CreateValueText("ValueText", valueCell);
    }

    private void BuildConsumableRow(float rowCenterY)
    {
        int slotCount = StoreController.MaxConsumableSlots;
        float slotsWidth = consumableSlotSize.x * slotCount + columnGap * (slotCount - 1);
        float rowWidth = labelSize.x + columnGap + slotsWidth;

        var row = CreateRect("Row_Consumiveis", transform);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, rowCenterY);
        row.sizeDelta = new Vector2(rowWidth, consumableSlotSize.y);

        var label = CreateLabel("Label", row, "Consumíveis:", labelSize, TextAlignmentOptions.MidlineLeft, labelTextColor, FontStyles.Bold);
        label.anchoredPosition = new Vector2(-slotsWidth * 0.5f - columnGap * 0.5f, 0f);

        var slotsRoot = CreateRect("Slots", row);
        slotsRoot.sizeDelta = new Vector2(slotsWidth, consumableSlotSize.y);
        slotsRoot.anchoredPosition = new Vector2(labelSize.x * 0.5f + columnGap * 0.5f, 0f);

        for (int i = 0; i < slotCount; i++)
        {
            var slot = CreateBackdropCell($"Slot{i + 1}", slotsRoot, consumableSlotSize, consumableSlotColor);
            float x = -slotsWidth * 0.5f + consumableSlotSize.x * 0.5f + i * (consumableSlotSize.x + columnGap);
            slot.anchoredPosition = new Vector2(x, 0f);
            _consumableSlotIcons[i] = CreateSlotIcon(slot);
        }
    }

    private Image CreateSlotIcon(RectTransform slot)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(slot, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        float pad = consumableIconPadding;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);

        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.enabled = false;
        return img;
    }

    /// <summary>Anchor bottom-center + tamanho fixo do painel (pixels de referência).</summary>
    private void ApplyFixedLayout()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = panelSize;
        rt.anchoredPosition = panelOffset;

        if (_panelBackdrop != null)
        {
            EnsureImageSprite(_panelBackdrop);
            _panelBackdrop.color = panelBackdropColor;
        }
    }

    /// <summary>Unity 6 não desenha Image sem sprite — garante quad branco 1×1.</summary>
    private static void EnsureImageSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Simple;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        _whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _whiteSprite;
    }

    /// <summary>Cancela escala do CanvasScaler para manter px constantes na tela.</summary>
    private void ApplyConstantPixelScale()
    {
        if (!counterCanvasScaler)
        {
            transform.localScale = Vector3.one;
            return;
        }

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        if (_rootCanvas == null)
            return;

        float s = _rootCanvas.scaleFactor;
        if (s <= 0.0001f)
            return;

        float inv = 1f / s;
        transform.localScale = new Vector3(inv, inv, 1f);
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private RectTransform CreateLabel(string name, RectTransform parent, string text, Vector2 size, TextAlignmentOptions align, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        ConfigureFixedText(tmp, text, align, color, style);
        return rt;
    }

    private TextMeshProUGUI CreateValueText(string name, RectTransform cell)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(cell, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        ConfigureFixedText(tmp, string.Empty, TextAlignmentOptions.Center, valueTextColor, FontStyles.Bold);
        return tmp;
    }

    private void ConfigureFixedText(TextMeshProUGUI tmp, string text, TextAlignmentOptions align, Color color, FontStyles style)
    {
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontSizeMin = fontSize;
        tmp.fontSizeMax = fontSize;
        tmp.enableAutoSizing = false;
        tmp.font = fontAsset;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    private RectTransform CreateBackdropCell(string name, RectTransform parent, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        EnsureImageSprite(img);
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }
}
