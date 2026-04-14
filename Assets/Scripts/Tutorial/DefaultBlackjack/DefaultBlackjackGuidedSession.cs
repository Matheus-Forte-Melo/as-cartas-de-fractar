using System.IO;
using Tutorial.Onboarding;
using UnityEngine;

namespace Tutorial.DefaultBlackjack
{
    /// <summary>
    /// Aplica mesa forçada e injeta passos do onboarding a partir de StreamingAssets antes do <see cref="TutorialManager"/> montar o canvas.
    /// Execution order definido no .meta (antes do <see cref="TutorialManager"/>).
    /// </summary>
    public sealed class DefaultBlackjackGuidedSession : MonoBehaviour
    {
        [SerializeField] private string _forcedTableJsonFileName = "tutorial_blackjack_guided_table.json";
        [SerializeField] private string _stepsJsonFileName = "tutorial_blackjack_guided_steps.json";
        [SerializeField] private string _stringsJsonFileName = "tutorial_blackjack_guided_strings.json";
        [SerializeField] private TutorialManager _tutorialManager;

        private void Awake()
        {
            SaveManager.EnsureMainTutorialCompletedKeyInSaveFile();

            if (!string.IsNullOrEmpty(_forcedTableJsonFileName))
                DefaultBlackjackSession.TableJsonFileNameOverride = _forcedTableJsonFileName;

            if (GetComponent<GuidedBlackjackNarrative>() == null)
                gameObject.AddComponent<GuidedBlackjackNarrative>();

            if (_tutorialManager == null)
                _tutorialManager = GetComponent<TutorialManager>();

            if (_tutorialManager == null)
                _tutorialManager = GetComponentInChildren<TutorialManager>(true);

            if (_tutorialManager == null)
            {
                Debug.LogWarning("[DefaultBlackjackGuidedSession] TutorialManager não encontrado.");
                return;
            }

            var canvasRt = transform.Find("Canvas") as RectTransform;

            string dir = Application.streamingAssetsPath;
            string stepsPath = Path.Combine(dir, _stepsJsonFileName);
            string stringsPath = Path.Combine(dir, _stringsJsonFileName);
            _tutorialManager.LoadStepsFromStreamingAssets(stepsPath, stringsPath, canvasRt);
        }

        private void OnDestroy() =>
            DefaultBlackjackSession.ClearOverrides();
    }
}
