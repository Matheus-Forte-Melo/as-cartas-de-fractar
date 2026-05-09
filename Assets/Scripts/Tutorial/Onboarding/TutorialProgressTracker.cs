using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Persistência por <paramref name="tutorialId"/>: blackjack (passo em <c>main_tutorial_step_index</c>),
    /// mapa (<c>map_onboarding_step_index</c>); flags no perfil.
    /// </summary>
    public sealed class TutorialProgressTracker
    {
        private readonly string _tutorialId;

        public TutorialProgressTracker(string tutorialId)
        {
            _tutorialId = string.IsNullOrEmpty(tutorialId) ? TutorialIds.BlackjackOnboarding : tutorialId;
        }

        public int LoadCurrentStepIndex()
        {
            if (_tutorialId == TutorialIds.MapOnboarding)
                return SaveManager.LoadMapOnboardingStepIndex();
            if (_tutorialId == TutorialIds.CoreOnboarding)
                return SaveManager.LoadCoreOnboardingStepIndex();
            return SaveManager.LoadTutorialStepIndex();
        }

        public bool LoadCompleted()
        {
            var p = SaveManager.LoadProfile();
            if (_tutorialId == TutorialIds.MapOnboarding)
                return p.map_onboarding_completed;
            if (_tutorialId == TutorialIds.CoreOnboarding)
                return p.core_onboarding_completed;
            if (_tutorialId == TutorialIds.BlackjackOnboarding)
                return p.guided_blackjack_completed;
            return p.main_tutorial_completed;
        }

        public void SaveStepIndex(int index)
        {
            if (_tutorialId == TutorialIds.MapOnboarding)
                SaveManager.SaveMapOnboardingStepIndex(index);
            else if (_tutorialId == TutorialIds.CoreOnboarding)
                SaveManager.SaveCoreOnboardingStepIndex(index);
            else
                SaveManager.SaveTutorialStepIndex(index);
        }

        /// <summary>Chamado ao terminar ou pular o fluxo (último passo ou skip).</summary>
        public void PersistCompletionAfterFinish()
        {
            if (_tutorialId == TutorialIds.MapOnboarding)
            {
                if (SaveManager.LoadProfile().map_onboarding_completed)
                    return;
                SaveManager.SetMapOnboardingCompleted(true);
                SaveManager.ClearMapOnboardingStepIndexAfterCompletion();
                return;
            }

            if (_tutorialId == TutorialIds.CoreOnboarding)
            {
                if (SaveManager.LoadProfile().core_onboarding_completed)
                    return;
                SaveManager.SetCoreOnboardingCompleted(true);
                SaveManager.SetMainTutorialCompleted(true);
                SaveManager.ClearCoreOnboardingStepIndexAfterCompletion();
                return;
            }

            if (_tutorialId == TutorialIds.BlackjackOnboarding)
            {
                if (SaveManager.LoadProfile().guided_blackjack_completed)
                    return;
                SaveManager.SetGuidedBlackjackCompleted(true);
                SaveManager.ClearTutorialStepIndexAfterCompletion();
                return;
            }

            if (SaveManager.LoadProfile().main_tutorial_completed)
                return;
            SaveManager.SetMainTutorialCompleted(true);
            SaveManager.ClearTutorialStepIndexAfterCompletion();
        }

        /// <summary>Reinicia passo e flags (dev / reset).</summary>
        public void ResetProgress()
        {
            if (_tutorialId == TutorialIds.MapOnboarding)
            {
                SaveManager.SetMapOnboardingCompleted(false);
                SaveManager.SaveMapOnboardingStepIndex(0);
                return;
            }

            if (_tutorialId == TutorialIds.CoreOnboarding)
            {
                SaveManager.SetCoreOnboardingCompleted(false);
                SaveManager.SaveCoreOnboardingStepIndex(0);
                return;
            }

            if (_tutorialId == TutorialIds.BlackjackOnboarding)
            {
                SaveManager.SetGuidedBlackjackCompleted(false);
                SaveManager.SaveTutorialStepIndex(0);
                return;
            }

            SaveManager.SetMainTutorialCompleted(false);
            SaveManager.SaveTutorialStepIndex(0);
        }
    }
}
