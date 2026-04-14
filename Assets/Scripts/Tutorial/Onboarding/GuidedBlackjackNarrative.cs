using System;
using System.Collections;
using Tutorial.DefaultBlackjack;
using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Passos especiais do blackjack guiado: esconde o overlay no passo <c>dealer_watch</c> e força a mesa a jogar.
    /// </summary>
    public sealed class GuidedBlackjackNarrative : MonoBehaviour
    {
        public const string StepIdDealerWatch = "dealer_watch";

        [SerializeField] private TutorialManager _tutorialManager;
        [SerializeField] private DefaultBlackjackController _blackjack;

        private Coroutine _dealerWatchRoutine;

        private void Awake()
        {
            if (_tutorialManager == null)
                _tutorialManager = GetComponent<TutorialManager>();
            if (_tutorialManager == null)
                _tutorialManager = GetComponentInChildren<TutorialManager>(true);
            if (_blackjack == null)
                _blackjack = GetComponent<DefaultBlackjackController>();
            if (_blackjack == null)
                _blackjack = GetComponentInChildren<DefaultBlackjackController>(true);
        }

        private void OnEnable()
        {
            if (_tutorialManager != null)
                _tutorialManager.StepAppliedAfterUi += OnStepAppliedAfterUi;
        }

        private void OnDisable()
        {
            if (_tutorialManager != null)
                _tutorialManager.StepAppliedAfterUi -= OnStepAppliedAfterUi;
            if (_dealerWatchRoutine != null)
            {
                StopCoroutine(_dealerWatchRoutine);
                _dealerWatchRoutine = null;
            }
        }

        private void OnStepAppliedAfterUi(int index, TutorialStepDefinition step)
        {
            if (step == null || step.stepId != StepIdDealerWatch)
                return;
            if (_blackjack == null || _tutorialManager == null)
                return;
            if (_dealerWatchRoutine != null)
                StopCoroutine(_dealerWatchRoutine);
            _dealerWatchRoutine = StartCoroutine(DealerWatchSequence());
        }

        private IEnumerator DealerWatchSequence()
        {
            _tutorialManager.SetTutorialLayersVisible(false);
            bool done = false;
            void OnBridge(string id)
            {
                if (id == TutorialBlackjackEventIds.DealerTurnVisualDone)
                    done = true;
            }

            EventBridge.TutorialEvent += OnBridge;
            // O jogador já deve ter usado Parar; só força se ainda estiver na fase do jogador.
            _blackjack.ForceStandForGuidedTutorial();
            yield return new WaitUntil(() => done);
            EventBridge.TutorialEvent -= OnBridge;
            _dealerWatchRoutine = null;
        }
    }
}
