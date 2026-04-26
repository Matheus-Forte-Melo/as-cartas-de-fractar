using System.Collections.Generic;
using Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Store
{
    public class StoreController : MonoBehaviour
    {
        public const int MaxConsumableSlots = 3;

        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Templates")]
        [SerializeField] private VisualTreeAsset storeItemTemplate;

        private VisualElement root;
        private VisualElement itemsGrid;
        private Label balanceLabel;
        private Label consumableSlotsLabel;
        private VisualElement modalOverlay;
        private Label modalTitle;
        private Label modalMessage;
        private Button modalCancelBtn;
        private Button modalConfirmBtn;

        private SaveData _save;
        private ItemDefinition _pendingItem;
        private Button _pendingBuyButton;

        private List<Label> titleChars = new();
        private List<VisualElement> _coinIcons = new();
        private float titleTimeElapsed;
        private float currentCoinRotation;

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument != null && storeItemTemplate != null)
            {
                _save = SaveManager.Load();
                EnsureConsumableList();
                InitializeUI();
                UpdateBalanceDisplay();
                UpdateConsumableSlotsDisplay();
                LoadStoreData();
            }
            else
            {
                Debug.LogError("[StoreController] UIDocument ou storeItemTemplate não referenciados!");
            }
        }

        private static void EnsureConsumableList(SaveData save)
        {
            if (save.consumableSlots == null)
                save.consumableSlots = new List<string>();
        }

        private void EnsureConsumableList() => EnsureConsumableList(_save);

        private int ConsumableSlotCount => _save.consumableSlots?.Count ?? 0;

        /// <summary>
        /// Consumível: já está num slot. Ataque/defesa: id já em <c>ownedItemIds</c>.
        /// </summary>
        private bool IsItemAlreadyOwned(ItemDefinition item)
        {
            if (item.ItemType == ItemType.Consumable)
            {
                EnsureConsumableList();
                return _save.consumableSlots.Contains(item.id);
            }

            _save.ownedItemIds ??= new List<string>();
            return _save.ownedItemIds.Contains(item.id);
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;
            itemsGrid = root.Q<VisualElement>("ItemsGrid");

            Button backButton = root.Q<Button>("BackButton");
            if (backButton != null)
                backButton.clicked += OnBackButtonClicked;

            balanceLabel = root.Q<Label>("PlayerBalance");
            consumableSlotsLabel = root.Q<Label>("ConsumableSlotsLabel");

            modalOverlay = root.Q<VisualElement>("ConfirmationModal");
            modalTitle = root.Q<Label>("ModalTitle");
            modalMessage = root.Q<Label>("ModalMessage");
            modalCancelBtn = root.Q<Button>("ModalCancelBtn");
            modalConfirmBtn = root.Q<Button>("ModalConfirmBtn");

            if (modalCancelBtn != null)
                modalCancelBtn.clicked += CloseModal;
            if (modalConfirmBtn != null)
                modalConfirmBtn.clicked += ConfirmPurchase;

            VisualElement titleContainer = root.Q<VisualElement>("StoreTitleContainer");
            if (titleContainer != null)
            {
                titleContainer.Clear();
                titleChars.Clear();
                string titleText = "Loja de Itens";

                for (int i = 0; i < titleText.Length; i++)
                {
                    Label charLabel = new Label(titleText[i].ToString());
                    charLabel.AddToClassList("wave-char");
                    if (titleText[i] == ' ')
                        charLabel.style.width = 15;
                    titleContainer.Add(charLabel);
                    titleChars.Add(charLabel);
                }
            }

            RebuildCoinIconCache();
            root.schedule.Execute(AnimateLoop).Every(100);
        }

        private void RebuildCoinIconCache()
        {
            _coinIcons.Clear();
            root.Query<VisualElement>(className: "coin-icon").ForEach(el => _coinIcons.Add(el));
        }

        private void UpdateConsumableSlotsDisplay()
        {
            if (consumableSlotsLabel != null)
                consumableSlotsLabel.text = $"Consumíveis: {ConsumableSlotCount}/{MaxConsumableSlots}";
        }

        private void AnimateLoop()
        {
            const float dt = 0.1f;

            if (titleChars.Count > 0)
            {
                titleTimeElapsed += dt;
                for (int i = 0; i < titleChars.Count; i++)
                {
                    float t = titleTimeElapsed - (i * 0.15f);
                    float y = -10f + 10f * Mathf.Cos(t * Mathf.PI);
                    titleChars[i].style.translate = new StyleTranslate(new Translate(0, y, 0));
                }
            }

            currentCoinRotation += 15f;
            if (currentCoinRotation >= 360f) currentCoinRotation -= 360f;

            float rad = currentCoinRotation * Mathf.Deg2Rad;
            float scaleX = Mathf.Abs(Mathf.Cos(rad));
            if (scaleX < 0.1f) scaleX = 0.1f;

            var scale = new StyleScale(new Scale(new Vector3(scaleX, 1f, 1f)));
            foreach (var coinIcon in _coinIcons)
                coinIcon.style.scale = scale;
        }

        private void UpdateBalanceDisplay()
        {
            if (balanceLabel != null)
                balanceLabel.text = _save.coins.ToString();
        }

        private void LoadStoreData()
        {
            itemsGrid.Clear();

            foreach (var item in ItemCatalog.All)
                InstantiateItem(item);

            RebuildCoinIconCache();
        }

        private void InstantiateItem(ItemDefinition itemData)
        {
            TemplateContainer itemInstance = storeItemTemplate.Instantiate();

            VisualElement cardRoot = itemInstance.Q<VisualElement>(className: "store-item");
            Label itemNameLabel = itemInstance.Q<Label>("ItemName");
            Label itemPriceLabel = itemInstance.Q<Label>("ItemPrice");
            VisualElement itemIcon = itemInstance.Q<VisualElement>("ItemIcon");
            Button buyButton = itemInstance.Q<Button>("BuyButton");
            Button infoButton = itemInstance.Q<Button>("InfoButton");
            Label itemDescription = itemInstance.Q<Label>("ItemDescription");

            if (itemNameLabel != null) itemNameLabel.text = itemData.displayName;
            if (itemPriceLabel != null) itemPriceLabel.text = itemData.price.ToString();
            if (itemDescription != null) itemDescription.text = itemData.description;

            Sprite iconSprite = Resources.Load<Sprite>(itemData.icon);
            if (iconSprite != null && itemIcon != null)
                itemIcon.style.backgroundImage = new StyleBackground(iconSprite);

            bool isConsumable = itemData.ItemType == ItemType.Consumable;

            if (buyButton != null)
            {
                if (isConsumable)
                {
                    if (ConsumableSlotCount >= MaxConsumableSlots)
                        SetButtonPurchased(buyButton, "Inventário cheio (3/3)");
                    else if (IsItemAlreadyOwned(itemData))
                        SetButtonPurchased(buyButton, "Já no inventário");
                    else
                        buyButton.clicked += () => OnBuyItemClicked(itemData, buyButton);
                }
                else if (IsItemAlreadyOwned(itemData))
                    SetButtonPurchased(buyButton, "Já adquirido");
                else
                    buyButton.clicked += () => OnBuyItemClicked(itemData, buyButton);
            }

            if (infoButton != null && cardRoot != null)
            {
                infoButton.clicked += () =>
                {
                    cardRoot.ToggleInClassList("flipped");
                    infoButton.text = cardRoot.ClassListContains("flipped") ? "x" : "i";
                };
            }

            itemsGrid.Add(itemInstance);
        }

        private void SetButtonPurchased(Button btn, string label)
        {
            Label btnLabel = btn.Q<Label>("BuyButtonText");
            if (btnLabel != null) btnLabel.text = label;
            btn.SetEnabled(false);
            btn.AddToClassList("purchased");
        }

        private void OnBuyItemClicked(ItemDefinition itemData, Button button)
        {
            _pendingItem = itemData;
            _pendingBuyButton = button;
            OpenModal(itemData);
        }

        private void OpenModal(ItemDefinition itemData)
        {
            if (modalOverlay == null) return;

            modalOverlay.style.display = DisplayStyle.Flex;

            bool isConsumable = itemData.ItemType == ItemType.Consumable;
            if (isConsumable && ConsumableSlotCount >= MaxConsumableSlots)
            {
                modalTitle.text = "Inventário cheio";
                modalMessage.text =
                    "Você já tem 3 consumíveis. Use um na batalha antes de comprar outro.";
                modalMessage.AddToClassList("error");
                modalCancelBtn.style.display = DisplayStyle.Flex;
                modalConfirmBtn.style.display = DisplayStyle.None;
                modalCancelBtn.text = "Fechar";
                return;
            }

            if (IsItemAlreadyOwned(itemData))
            {
                modalTitle.text = isConsumable ? "Já no inventário" : "Já adquirido";
                modalMessage.text = isConsumable
                    ? "Você já possui este consumível. Use-o numa batalha antes de comprar outro."
                    : "Este item já foi adquirido na loja.";
                modalMessage.AddToClassList("error");
                modalCancelBtn.style.display = DisplayStyle.Flex;
                modalConfirmBtn.style.display = DisplayStyle.None;
                modalCancelBtn.text = "Fechar";
                return;
            }

            if (_save.coins >= itemData.price)
            {
                modalTitle.text = "Confirmar Compra";
                modalMessage.text = $"Deseja realmente comprar:\n\n<b>{itemData.displayName}</b>\npor {itemData.price} moedas?";
                modalMessage.RemoveFromClassList("error");
                modalCancelBtn.style.display = DisplayStyle.Flex;
                modalConfirmBtn.style.display = DisplayStyle.Flex;
                modalConfirmBtn.text = "Confirmar";
                modalCancelBtn.text = "Cancelar";
            }
            else
            {
                modalTitle.text = "Saldo Insuficiente";
                modalMessage.text = $"Você não tem <b>{itemData.price} moedas</b> para comprar\n\n<b>{itemData.displayName}</b>.\nFaltam {itemData.price - _save.coins} moedas.";
                modalMessage.AddToClassList("error");
                modalCancelBtn.style.display = DisplayStyle.Flex;
                modalConfirmBtn.style.display = DisplayStyle.None;
                modalCancelBtn.text = "Fechar";
            }
        }

        private void CloseModal()
        {
            if (modalOverlay != null)
                modalOverlay.style.display = DisplayStyle.None;
            _pendingItem = null;
            _pendingBuyButton = null;
        }

        private void ConfirmPurchase()
        {
            if (_pendingItem == null) return;

            if (_pendingItem.ItemType == ItemType.Consumable)
            {
                EnsureConsumableList();
                if (ConsumableSlotCount >= MaxConsumableSlots || IsItemAlreadyOwned(_pendingItem))
                {
                    Debug.LogWarning("[Store] Compra de consumível rejeitada (inventário cheio ou item já possuído).");
                    CloseModal();
                    return;
                }

                _save.coins -= _pendingItem.price;
                _save.consumableSlots.Add(_pendingItem.id);
                SaveManager.Save(_save);
                UpdateBalanceDisplay();
                UpdateConsumableSlotsDisplay();
                Debug.Log($"[Store] Consumível: {_pendingItem.displayName}. Slots: {ConsumableSlotCount}/{MaxConsumableSlots}");

                CloseModal();
                LoadStoreData();
                return;
            }

            if (IsItemAlreadyOwned(_pendingItem))
            {
                Debug.LogWarning("[Store] Compra rejeitada — item permanente já adquirido.");
                CloseModal();
                return;
            }

            _save.coins -= _pendingItem.price;
            _save.ownedItemIds ??= new List<string>();
            _save.ownedItemIds.Add(_pendingItem.id);

            if (_pendingItem.ItemType == ItemType.Defense)
                _save.playerHealth += _pendingItem.bonusHealth;

            SaveManager.Save(_save);
            UpdateBalanceDisplay();

            Debug.Log($"[Store] Comprou: {_pendingItem.displayName} por {_pendingItem.price}. Saldo: {_save.coins}");

            CloseModal();
            LoadStoreData();
        }

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene(GameFlowScenes.CurrentMap);
        }
    }
}
