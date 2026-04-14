using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Persistência do onboarding principal em <see cref="SaveData"/> (<c>save.json</c>): passo e conclusão.
    /// </summary>
    public sealed class TutorialProgressTracker
    {
        public TutorialProgressTracker(string tutorialId)
        {
            _ = tutorialId;
        }

        public int LoadCurrentStepIndex()
        {
            var save = SaveManager.Load();
            return Mathf.Max(0, save.main_tutorial_step_index);
        }

        public bool LoadCompleted() => SaveManager.Load().main_tutorial_completed;

        public void SaveStepIndex(int index)
        {
            var save = SaveManager.Load();
            save.main_tutorial_step_index = Mathf.Max(0, index);
            SaveManager.Save(save);
        }

        public void SaveCompleted()
        {
            var save = SaveManager.Load();
            save.main_tutorial_completed = true;
            SaveManager.Save(save);
        }

        /// <summary>Reinicia passo e flag de conclusão no save (ex.: testar o fluxo de novo).</summary>
        public void ResetProgress()
        {
            var save = SaveManager.Load();
            save.main_tutorial_step_index = 0;
            save.main_tutorial_completed = false;
            SaveManager.Save(save);
        }
    }
}
