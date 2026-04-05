using Blackjack.Decks;
using UnityEngine;

namespace Blackjack.Core
{
    /// <summary>
    /// Valida as mãos iniciais (2+2 cartas). Se bust ou total 21 em qualquer lado, reembaralha e redistribui.
    /// </summary>
    public class RoundStartBalance : RoundBalance
    {
        public const int DefaultMaxRedealAttempts = 32;

        /// <summary>
        /// Limpa mãos, reconstrói baralho e distribui até ambas válidas ou esgotar tentativas.
        /// </summary>
        public void Apply(Duelist player, Enemy enemy, Deck deck, int roundNumber, int maxAttempts = DefaultMaxRedealAttempts)
        {
            Debug.Log($"[RoundStartBalance] acionado | round={roundNumber} | início validação");

            bool success = false;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                player.Hand.Clear();
                enemy.Hand.Clear();
                deck.Reset();

                player.Hand.Add(deck.Draw());
                enemy.Hand.Add(deck.Draw());
                player.Hand.Add(deck.Draw());
                enemy.Hand.Add(deck.Draw());

                int pv = player.Hand.Value;
                int ev = enemy.Hand.Value;

                if (attempt > 0)
                {
                    string reason = DescribeInvalidReason(pv, ev);
                    Debug.Log($"[RoundStartBalance] redeal | tentativa={attempt + 1} | player={pv} enemy={ev} | motivo={reason}");
                }
                else
                {
                    Debug.Log($"[RoundStartBalance] tentativa={attempt + 1} | player={pv} enemy={ev}");
                }

                if (IsOpeningValid(pv, ev))
                {
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                Debug.LogError($"[RoundStartBalance] esgotou {maxAttempts} tentativas; mantendo último deal (round={roundNumber}).");
            }
        }

        private static bool IsOpeningValid(int playerValue, int enemyValue) =>
            playerValue is > 0 and < 21 && enemyValue is > 0 and < 21;

        private static string DescribeInvalidReason(int pv, int ev)
        {
            if (pv > 21 || ev > 21) return "bust_inicial";
            if (pv == 21 || ev == 21) return "total_21_inicial";
            return "desconhecido";
        }
    }
}
