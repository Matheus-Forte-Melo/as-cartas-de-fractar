using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Carrega o deck do tutorial (ex.: <c>TUTORIAL/defaultblackjack/tutorial_default_blackjack_deck.json</c>) —
    /// grupos = naipes, cartas com <c>value</c> inteiro e <c>ace</c> opcional.
    /// </summary>
    public static class TutorialDeckJsonLoader
    {
        [Serializable]
        private class CardEntry
        {
            public int value;
            public bool ace;
        }

        [Serializable]
        private class GroupEntry
        {
            public string label;
            public CardEntry[] cards;
        }

        [Serializable]
        private class DeckRoot
        {
            public GroupEntry[] groups;
        }

        /// <summary>Expansão estável: ordem dos grupos no JSON, depois ordem das cartas em cada grupo.</summary>
        public static List<TutorialPlayingCard> ExpandDeckFromJsonText(string json)
        {
            var list = new List<TutorialPlayingCard>();
            if (string.IsNullOrWhiteSpace(json))
                return list;

            DeckRoot root;
            try
            {
                root = JsonUtility.FromJson<DeckRoot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TutorialDeckJsonLoader] JSON inválido: {ex.Message}");
                return list;
            }

            if (root?.groups == null)
                return list;

            foreach (var group in root.groups)
            {
                if (group == null || string.IsNullOrEmpty(group.label) || group.cards == null)
                    continue;

                foreach (var entry in group.cards)
                {
                    if (entry == null)
                        continue;

                    bool ace = entry.ace;
                    int v = entry.value;
                    if (!ace && (v < 2 || v > 10))
                    {
                        Debug.LogWarning($"[TutorialDeckJsonLoader] Valor fora de 2–10 ignorado: {v} em {group.label}");
                        continue;
                    }

                    list.Add(new TutorialPlayingCard(group.label, v, ace));
                }
            }

            return list;
        }

        public static List<TutorialPlayingCard> LoadDeckFromStreamingPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                Debug.LogWarning($"[TutorialDeckJsonLoader] Arquivo inexistente: {absolutePath}");
                return new List<TutorialPlayingCard>();
            }

            string json = File.ReadAllText(absolutePath, System.Text.Encoding.UTF8);
            return ExpandDeckFromJsonText(json);
        }
    }
}
