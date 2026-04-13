using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Configuração opcional da mesa: mãos iniciais e fila de compras forçada (para ensaios de tutorial).
    /// </summary>
    [Serializable]
    public class TutorialTableRigConfig
    {
        public bool useForcedTable;
        public bool shuffleRemaining = true;
        public TableCardSpec[] playerOpening = Array.Empty<TableCardSpec>();
        public TableCardSpec[] dealerOpening = Array.Empty<TableCardSpec>();
        public TableCardSpec[] drawStack = Array.Empty<TableCardSpec>();
    }

    [Serializable]
    public class TableCardSpec
    {
        /// <summary>Nome do naipe igual ao <c>label</c> do grupo no deck (ex.: Copas).</summary>
        public string suit;
        public int value;
        public bool ace;
    }

    public static class TutorialTableRigJsonLoader
    {
        public static TutorialTableRigConfig LoadFromStreamingPath(string absolutePath)
        {
            var fallback = new TutorialTableRigConfig
            {
                useForcedTable = false,
                shuffleRemaining = true,
                playerOpening = Array.Empty<TableCardSpec>(),
                dealerOpening = Array.Empty<TableCardSpec>(),
                drawStack = Array.Empty<TableCardSpec>()
            };

            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                Debug.LogWarning($"[TutorialTableRigJsonLoader] Arquivo inexistente: {absolutePath}");
                return fallback;
            }

            try
            {
                string json = File.ReadAllText(absolutePath, System.Text.Encoding.UTF8);
                var cfg = JsonUtility.FromJson<TutorialTableRigConfig>(json);
                if (cfg == null)
                    return fallback;
                cfg.playerOpening ??= Array.Empty<TableCardSpec>();
                cfg.dealerOpening ??= Array.Empty<TableCardSpec>();
                cfg.drawStack ??= Array.Empty<TableCardSpec>();
                return cfg;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TutorialTableRigJsonLoader] Erro ao ler JSON: {ex.Message}");
                return fallback;
            }
        }
    }

    /// <summary>
    /// Monta a pilha de compra e distribui aberturas conforme o rig (ou modo aleatório).
    /// </summary>
    public static class TutorialBlackjackRoundSetup
    {
        private static readonly System.Random Rng = new System.Random();

        public static void SetupRound(
            List<TutorialPlayingCard> fullDeckInExpansionOrder,
            TutorialTableRigConfig rig,
            TutorialBlackjackHand player,
            TutorialBlackjackHand dealer,
            TutorialDeck stock)
        {
            player.Clear();
            dealer.Clear();
            stock.Clear();

            var pool = new List<TutorialPlayingCard>(fullDeckInExpansionOrder);

            if (rig != null && rig.useForcedTable)
            {
                bool openingsOk = rig.playerOpening != null && rig.playerOpening.Length >= 2
                                  && rig.dealerOpening != null && rig.dealerOpening.Length >= 2;
                if (!openingsOk)
                {
                    Debug.LogWarning(
                        "[TutorialBlackjackRoundSetup] useForcedTable exige pelo menos 2 cartas em playerOpening e em dealerOpening. Usando modo aleatório.");
                    ApplyRandom(pool, player, dealer, stock);
                    return;
                }

                try
                {
                    ApplyForced(pool, rig, player, dealer, stock);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TutorialBlackjackRoundSetup] Rig inválido: {ex.Message}. Usando modo aleatório.");
                    pool = new List<TutorialPlayingCard>(fullDeckInExpansionOrder);
                    ApplyRandom(pool, player, dealer, stock);
                }

                return;
            }

            ApplyRandom(pool, player, dealer, stock);
        }

        private static void ApplyRandom(List<TutorialPlayingCard> pool, TutorialBlackjackHand player,
            TutorialBlackjackHand dealer, TutorialDeck stock)
        {
            ShuffleInPlace(pool);

            player.Add(DrawFromPoolEnd(pool));
            dealer.Add(DrawFromPoolEnd(pool));
            player.Add(DrawFromPoolEnd(pool));
            dealer.Add(DrawFromPoolEnd(pool));

            for (int i = 0; i < pool.Count; i++)
                stock.PushTop(pool[i]);
        }

        private static void ApplyForced(
            List<TutorialPlayingCard> pool,
            TutorialTableRigConfig rig,
            TutorialBlackjackHand player,
            TutorialBlackjackHand dealer,
            TutorialDeck stock)
        {
            foreach (var spec in rig.playerOpening)
                player.Add(TakeFirstMatching(pool, spec));

            foreach (var spec in rig.dealerOpening)
                dealer.Add(TakeFirstMatching(pool, spec));

            var forcedDraw = new List<TutorialPlayingCard>();
            if (rig.drawStack != null)
            {
                foreach (var spec in rig.drawStack)
                    forcedDraw.Add(TakeFirstMatching(pool, spec));
            }

            if (rig.shuffleRemaining)
                ShuffleInPlace(pool);
            // pool = cartas que não foram para mãos nem para drawStack forçado

            for (int i = 0; i < pool.Count; i++)
                stock.PushTop(pool[i]);

            for (int i = forcedDraw.Count - 1; i >= 0; i--)
                stock.PushTop(forcedDraw[i]);
        }

        private static TutorialPlayingCard TakeFirstMatching(List<TutorialPlayingCard> pool, TableCardSpec spec)
        {
            string suit = spec.suit ?? "";
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].MatchesSpec(suit, spec.value, spec.ace))
                {
                    var c = pool[i];
                    pool.RemoveAt(i);
                    return c;
                }
            }

            throw new InvalidOperationException(
                $"[TutorialBlackjackRoundSetup] Carta não encontrada no baralho: suit={suit} value={spec.value} ace={spec.ace}");
        }

        private static TutorialPlayingCard DrawFromPoolEnd(List<TutorialPlayingCard> pool)
        {
            int n = pool.Count;
            if (n == 0)
                throw new InvalidOperationException("[TutorialBlackjackRoundSetup] Baralho vazio ao distribuir.");

            var c = pool[n - 1];
            pool.RemoveAt(n - 1);
            return c;
        }

        private static void ShuffleInPlace(List<TutorialPlayingCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
