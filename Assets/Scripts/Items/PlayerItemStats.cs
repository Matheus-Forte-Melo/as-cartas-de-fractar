using System.Globalization;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Calcula stats agregados do jogador com base nos itens possuídos no save.
    /// </summary>
    public static class PlayerItemStats
    {
        public const int BaseHealth = 100;

        /// <summary>Formato exibido no Hub e no resumo de combate (ex.: ×1,5).</summary>
        public static string FormatDamageMultiplier(float multiplier)
        {
            if (multiplier <= 0f)
                return "×0";
            return "×" + multiplier.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Vida máxima = base + soma de <c>bonusHealth</c> de cada entrada defensiva em <c>ownedItemIds</c>
        /// (o mesmo id pode repetir-se para acumular).
        /// </summary>
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

        /// <summary>
        /// Bónus de dano cumulativo: cada item de ataque em <c>ownedItemIds</c> (incluindo repetições do mesmo id)
        /// contribui com <c>attackMultiplier - 1</c>; o total é <c>1 + soma</c>. Ex.: 1,15 + 1,35 + 1,50 → ×2,0.
        /// </summary>
        public static float CalculateDamageMultiplier(SaveData save)
        {
            float bonusSum = 0f;

            if (save.ownedItemIds == null) return 1f;

            foreach (string id in save.ownedItemIds)
            {
                var item = ItemCatalog.Get(id);
                if (item == null) continue;
                if (item.ItemType != ItemType.Attack) continue;
                float m = item.attackMultiplier;
                if (m > 1f) bonusSum += m - 1f;
            }

            return 1f + bonusSum;
        }
    }
}
