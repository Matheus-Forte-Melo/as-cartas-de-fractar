using UnityEngine;

namespace Items
{
    /// <summary>
    /// Calcula stats agregados do jogador com base nos itens possuídos no save.
    /// </summary>
    public static class PlayerItemStats
    {
        public const int BaseHealth = 100;

        public static int CalculateMaxHealth(SaveData save)
        {
            int bonus = 0;

            if (save.ownedItemIds == null) return BaseHealth;

            foreach (string id in save.ownedItemIds)
            {
                var item = ItemCatalog.Get(id);
                if (item == null) continue;
                if (item.ItemType == ItemType.Defense)
                    bonus += item.bonusHealth;
            }

            return BaseHealth + bonus;
        }

        public static float CalculateDamageMultiplier(SaveData save)
        {
            float multiplier = 1f;

            if (save.ownedItemIds == null) return multiplier;

            foreach (string id in save.ownedItemIds)
            {
                var item = ItemCatalog.Get(id);
                if (item == null) continue;
                if (item.ItemType == ItemType.Attack)
                    multiplier *= item.attackMultiplier;
            }

            return multiplier;
        }
    }
}
