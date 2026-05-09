using System.IO;
using Tutorial;
using Tutorial.Onboarding;
using UnityEngine;

namespace Tutorial.CoreTutorial
{
    /// <summary>
    /// Configura o <see cref="TutorialManager"/> da cena <c>CoreTutorial</c>: define o id do tutorial
    /// (<c>core_onboarding</c>), aplica o painel expandido, carrega passos/strings de StreamingAssets
    /// e injeta no manager antes do <c>Awake</c>.
    /// </summary>
    /// <remarks>
    /// Execution order definido no .meta para correr antes do <see cref="TutorialManager"/>.
    /// </remarks>
    public sealed class CoreTutorialGuidedSession : MonoBehaviour
    {
        [SerializeField] private string _stepsJsonFileName = TutorialContentPaths.CoreSteps;
        [SerializeField] private string _stringsJsonFileName = TutorialContentPaths.CoreStrings;
        [SerializeField] private TutorialManager _tutorialManager;

        private void Awake()
        {
            SaveManager.EnsureMainTutorialCompletedKeyInSaveFile();

            if (_tutorialManager == null)
                _tutorialManager = GetComponent<TutorialManager>();
            if (_tutorialManager == null)
                _tutorialManager = GetComponentInChildren<TutorialManager>(true);

            if (_tutorialManager == null)
            {
                // Quando o manager é criado em runtime (sem prefab) usamos o flow de pendentes.
                var canvasRt = FindCanvasRoot();
                var steps = TutorialOnboardingJsonLoader.Load(
                    Path.Combine(Application.streamingAssetsPath, _stepsJsonFileName),
                    Path.Combine(Application.streamingAssetsPath, _stringsJsonFileName),
                    canvasRt);
                TutorialManager.RegisterPendingBootstrap(TutorialIds.CoreOnboarding, steps);
                _tutorialManager = gameObject.AddComponent<TutorialManager>();
                return;
            }

            string stepsPath = Path.Combine(Application.streamingAssetsPath, _stepsJsonFileName);
            string stringsPath = Path.Combine(Application.streamingAssetsPath, _stringsJsonFileName);
            _tutorialManager.LoadStepsFromStreamingAssets(stepsPath, stringsPath, FindCanvasRoot());
        }

        private RectTransform FindCanvasRoot()
        {
            // O Canvas da cena é irmão deste GameObject.
            var t = transform.parent != null ? transform.parent.Find("Canvas") : null;
            if (t == null)
            {
                var canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null) t = canvas.transform;
            }
            return t as RectTransform;
        }
    }
}
