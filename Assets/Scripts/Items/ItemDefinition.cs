using System;

namespace Items
{
    [Serializable]
    public class ItemDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public string type;
        public int price;
        public string icon;
        public float attackMultiplier = 1f;
        public int bonusHealth;
        public string consumableEffect;
        public int consumableValue;

        public ItemType ItemType => type switch
        {
            "attack" => ItemType.Attack,
            "defense" => ItemType.Defense,
            "consumable" => ItemType.Consumable,
            _ => ItemType.Consumable
        };

        public ItemId ItemId => id switch
        {
            "sword_gold" => ItemId.SwordGold,
            "shield_silver" => ItemId.ShieldSilver,
            "health_potion" => ItemId.HealthPotion,
            "magic_amulet" => ItemId.MagicAmulet,
            _ => ItemId.None
        };

        public ConsumableActionType ParseConsumableAction()
        {
            if (string.IsNullOrWhiteSpace(consumableEffect))
                return ConsumableActionType.None;
            return consumableEffect.Trim().ToLowerInvariant() switch
            {
                "heal" => ConsumableActionType.Heal,
                _ => ConsumableActionType.None
            };
        }
    }

    [Serializable]
    public class ItemCatalogData
    {
        public ItemDefinition[] items;
    }
}
