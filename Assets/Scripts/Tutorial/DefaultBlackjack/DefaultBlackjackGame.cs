using System.Collections.Generic;
using UnityEngine;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Blackjack simplificado (jogador vs mesa). Sem duelista, vida ou dano.
    /// Naturals (21 com exatamente duas cartas): empate se ambos; senão vitória de quem tem natural.
    /// </summary>
    public sealed class DefaultBlackjackGame
    {
        public const int HandLimit = 21;

        public TutorialBlackjackHand Player { get; } = new();
        public TutorialBlackjackHand Dealer { get; } = new();
        public TutorialDeck Stock { get; } = new();

        public DefaultBlackjackState State { get; private set; } = DefaultBlackjackState.PlayerTurn;

        /// <summary>Primeira carta do dealer oculta na UI até o fim da fase do jogador.</summary>
        public bool DealerHoleHidden { get; private set; }

        public int CurrentRoundNumber { get; private set; }

        private List<TutorialPlayingCard> _fullDeckExpansion;
        private TutorialTableRigConfig _rig;

        public void ConfigureDeckSource(List<TutorialPlayingCard> fullDeckInExpansionOrder, TutorialTableRigConfig rig)
        {
            _fullDeckExpansion = fullDeckInExpansionOrder ?? new List<TutorialPlayingCard>();
            _rig = rig ?? new TutorialTableRigConfig { useForcedTable = false };
        }

        public void NewRound()
        {
            CurrentRoundNumber++;
            State = DefaultBlackjackState.PlayerTurn;
            DealerHoleHidden = true;

            if (_fullDeckExpansion == null || _fullDeckExpansion.Count == 0)
            {
                Debug.LogError("[DefaultBlackjackGame] Baralho não configurado.");
                State = DefaultBlackjackState.Push;
                return;
            }

            try
            {
                TutorialBlackjackRoundSetup.SetupRound(_fullDeckExpansion, _rig, Player, Dealer, Stock);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DefaultBlackjackGame] Falha ao montar rodada: {ex.Message}");
                State = DefaultBlackjackState.Push;
                return;
            }

            if (TryResolveNaturalsAfterDeal())
                return;

            if (Player.Value == HandLimit)
                BeginDealerPhaseAfterPlayerFinished();
        }

        /// <returns>true se a rodada terminou (natural ou empate natural).</returns>
        private bool TryResolveNaturalsAfterDeal()
        {
            bool pNat = IsNatural(Player);
            bool dNat = IsNatural(Dealer);

            if (pNat && dNat)
            {
                State = DefaultBlackjackState.Push;
                DealerHoleHidden = false;
                return true;
            }

            if (pNat)
            {
                State = DefaultBlackjackState.PlayerWin;
                DealerHoleHidden = false;
                return true;
            }

            if (dNat)
            {
                State = DefaultBlackjackState.DealerWin;
                DealerHoleHidden = false;
                return true;
            }

            return false;
        }

        private static bool IsNatural(TutorialBlackjackHand hand) =>
            hand.Cards.Count == 2 && hand.Value == HandLimit;

        public void PlayerHit()
        {
            if (State != DefaultBlackjackState.PlayerTurn)
                return;

            Player.Add(Stock.Draw());

            if (Player.Value > HandLimit)
            {
                State = DefaultBlackjackState.PlayerBust;
                DealerHoleHidden = false;
                return;
            }

            if (Player.Value == HandLimit)
                BeginDealerPhaseAfterPlayerFinished();
        }

        public void PlayerStand()
        {
            if (State != DefaultBlackjackState.PlayerTurn)
                return;

            BeginDealerPhaseAfterPlayerFinished();
        }

        private void BeginDealerPhaseAfterPlayerFinished()
        {
            DealerHoleHidden = false;
            State = DefaultBlackjackState.DealerTurn;
        }

        /// <summary>Uma compra ou parada do dealer; use em loop na UI com pausa entre chamadas.</summary>
        /// <returns>true se ainda em <see cref="DefaultBlackjackState.DealerTurn"/> e outra ação será necessária.</returns>
        public bool TryDealerStep()
        {
            if (State != DefaultBlackjackState.DealerTurn)
                return false;

            if (Dealer.Value > HandLimit)
            {
                State = DefaultBlackjackState.DealerBust;
                return false;
            }

            if (Dealer.Value >= 17)
            {
                ResolveComparison();
                return false;
            }

            Dealer.Add(Stock.Draw());

            if (Dealer.Value > HandLimit)
            {
                State = DefaultBlackjackState.DealerBust;
                return false;
            }

            if (Dealer.Value >= 17)
            {
                ResolveComparison();
                return false;
            }

            return true;
        }

        /// <summary>Executa todo o turno do dealer sem pausa (testes ou jogo sem animação).</summary>
        public void RunDealerToCompletion()
        {
            while (TryDealerStep()) { }
        }

        private void ResolveComparison()
        {
            int pv = Player.Value;
            int dv = Dealer.Value;

            if (pv > dv)
                State = DefaultBlackjackState.PlayerWin;
            else if (pv < dv)
                State = DefaultBlackjackState.DealerWin;
            else
                State = DefaultBlackjackState.Push;
        }
    }
}
