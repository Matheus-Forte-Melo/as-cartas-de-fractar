using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blackjack.Decks
{
    public readonly struct CardDefinition
    {
        public readonly string Equation;
        public readonly bool IsAce;

        public CardDefinition(string equation, bool isAce)
        {
            Equation = equation;
            IsAce = isAce;
        }
    }

    public class DeckConfig
    {
        public List<CardDefinition> Cards { get; } = new();

        public static DeckConfig Load(string json)
        {
            var config = new DeckConfig();

            try
            {
                var root = JsonUtility.FromJson<ConfigRoot>(json);
                if (root?.groups == null)
                {
                    Debug.LogWarning("[DeckConfig] JSON inválido ou vazio.");
                    return config;
                }

                foreach (var group in root.groups)
                {
                    if (group?.cards == null) continue;
                    foreach (var entry in group.cards)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.equation)) continue;
                        config.Cards.Add(new CardDefinition(entry.equation, entry.ace));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeckConfig] Erro ao parsear JSON: {ex.Message}");
            }

            return config;
        }

        /// <summary>
        /// Fallback: 52 cartas com equações numéricas simples (sem JSON).
        /// </summary>
        public static DeckConfig CreateDefault()
        {
            var config = new DeckConfig();
            int[] values = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10, 10 };

            for (int group = 0; group < 4; group++)
            {
                foreach (int v in values)
                    config.Cards.Add(new CardDefinition(v.ToString(), false));
                config.Cards.Add(new CardDefinition("?", true));
            }

            return config;
        }

        #region DTOs para JsonUtility

        [Serializable]
        private class CardEntry
        {
            public string equation;
            public bool ace;
        }

        [Serializable]
        private class GroupEntry
        {
            public string label;
            public CardEntry[] cards;
        }

        [Serializable]
        private class ConfigRoot
        {
            public GroupEntry[] groups;
        }

        #endregion
    }
}
