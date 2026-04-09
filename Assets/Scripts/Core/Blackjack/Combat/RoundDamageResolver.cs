using System;
using Blackjack.Decks;
using UnityEngine;

namespace Blackjack.Core
{
    public readonly struct RoundDamageOutcome
    {
        public int PlayerHandTotal { get; }
        public int EnemyHandTotal { get; }
        public int HandDifference { get; }
        public int RawGap { get; }
        public int DamageDealt { get; }
        public bool DamageToPlayer { get; }
        public bool DamageToEnemy { get; }
        public string RuleTag { get; }
        public int HealthPlayerBefore { get; }
        public int HealthEnemyBefore { get; }
        public int HealthPlayerAfter { get; }
        public int HealthEnemyAfter { get; }
        public int HandLimit { get; }
        public int RoundNumber { get; }
        public GameState TerminalState { get; }

        public RoundDamageOutcome(
            int playerHandTotal,
            int enemyHandTotal,
            int handDifference,
            int rawGap,
            int damageDealt,
            bool damageToPlayer,
            bool damageToEnemy,
            string ruleTag,
            int healthPlayerBefore,
            int healthEnemyBefore,
            int healthPlayerAfter,
            int healthEnemyAfter,
            int handLimit,
            int roundNumber,
            GameState terminalState)
        {
            PlayerHandTotal = playerHandTotal;
            EnemyHandTotal = enemyHandTotal;
            HandDifference = handDifference;
            RawGap = rawGap;
            DamageDealt = damageDealt;
            DamageToPlayer = damageToPlayer;
            DamageToEnemy = damageToEnemy;
            RuleTag = ruleTag;
            HealthPlayerBefore = healthPlayerBefore;
            HealthEnemyBefore = healthEnemyBefore;
            HealthPlayerAfter = healthPlayerAfter;
            HealthEnemyAfter = healthEnemyAfter;
            HandLimit = handLimit;
            RoundNumber = roundNumber;
            TerminalState = terminalState;
        }
    }

    /// <summary>
    /// Dano ao fim da rodada (GDD: distância da mão do perdedor ao limite × multiplicador).
    /// </summary>
    public static class RoundDamageResolver
    {
        public static RoundDamageOutcome ResolveAndApply(
            Duelist player,
            Duelist enemy,
            GameState terminalState,
            int roundNumber,
            int handLimit = 21,
            double damageMultiplier = 10.0)
        {
            if (!GameStateSemantics.IsRoundTerminal(terminalState))
            {
                Debug.LogWarning($"[RoundDamage] estado não terminal ignorado: {terminalState}");
                return default;
            }

            int pTotal = player.Hand.Value;
            int eTotal = enemy.Hand.Value;
            int diffHands = Math.Abs(pTotal - eTotal);
            int hpP = player.Health;
            int hpE = enemy.Health;

            if (terminalState == GameState.Push)
            {
                var push = new RoundDamageOutcome(
                    pTotal,
                    eTotal,
                    diffHands,
                    0,
                    0,
                    false,
                    false,
                    "Push",
                    hpP,
                    hpE,
                    hpP,
                    hpE,
                    handLimit,
                    roundNumber,
                    terminalState);
                LogOutcome(push);
                return push;
            }

            Duelist victim;
            string tag;

            switch (terminalState)
            {
                case GameState.PlayerBust:
                    victim = player;
                    tag = "PlayerBust";
                    break;
                case GameState.EnemyBust:
                    victim = enemy;
                    tag = "EnemyBust";
                    break;
                case GameState.PlayerWin:
                    victim = enemy;
                    tag = "Compare_PlayerWin";
                    break;
                case GameState.EnemyWin:
                    victim = player;
                    tag = "Compare_EnemyWin";
                    break;
                default:
                    Debug.LogWarning($"[RoundDamage] terminal sem regra de dano: {terminalState}");
                    return default;
            }

            int rawGap = Math.Abs(victim.Hand.Value - handLimit);
            if (rawGap == 0) rawGap = 1;
            int damage = (int)Math.Round(rawGap * damageMultiplier);

            bool toPlayer = ReferenceEquals(victim, player);
            if (!toPlayer && player.DamageMultiplier > 0f)
                damage = (int)Math.Round(damage * player.DamageMultiplier);

            if (toPlayer)
                player.ApplyDamage(damage);
            else
                enemy.ApplyDamage(damage);

            var outcome = new RoundDamageOutcome(
                pTotal,
                eTotal,
                diffHands,
                rawGap,
                damage,
                toPlayer,
                !toPlayer,
                tag,
                hpP,
                hpE,
                player.Health,
                enemy.Health,
                handLimit,
                roundNumber,
                terminalState);

            LogOutcome(outcome);
            return outcome;
        }

        private static void LogOutcome(RoundDamageOutcome o)
        {
            Debug.Log(
                $"[RoundDamage] round={o.RoundNumber} state={o.TerminalState} tag={o.RuleTag} " +
                $"mãos P={o.PlayerHandTotal} E={o.EnemyHandTotal} |mãos|={o.HandDifference} " +
                $"rawGap={o.RawGap} dano={o.DamageDealt} " +
                $"alvo={(o.DamageToPlayer ? "player" : o.DamageToEnemy ? "enemy" : "nenhum")} " +
                $"HP P {o.HealthPlayerBefore}→{o.HealthPlayerAfter} E {o.HealthEnemyBefore}→{o.HealthEnemyAfter}");
        }
    }
}
