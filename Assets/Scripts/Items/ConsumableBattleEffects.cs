using System;
using Blackjack.Core;

namespace Items
{
    public static class ConsumableBattleEffects
    {
        public static bool TryApply(ItemDefinition def, Duelist player, out string resultMessage)
        {
            resultMessage = null;
            if (def == null)
                return false;

            switch (def.ParseConsumableAction())
            {
                case ConsumableActionType.Heal:
                    int amount = Math.Max(0, def.consumableValue);
                    int before = player.Health;
                    int cap = player.MaxHealth;
                    player.Health = Math.Min(player.Health + amount, cap);
                    int gained = player.Health - before;
                    resultMessage = gained > 0
                        ? $"+{gained} vida (máx. {cap})."
                        : "Vida já está no máximo.";
                    return true;
                default:
                    return false;
            }
        }
    }
}
