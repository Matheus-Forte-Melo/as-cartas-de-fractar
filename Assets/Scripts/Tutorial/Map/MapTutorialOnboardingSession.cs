using System.Collections;
using System.IO;
using Tutorial.Onboarding;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Onboarding do mapa só em <see cref="GameFlowScenes.MapTutorial"/> (fluxo tutorial clonado).
/// </summary>
[DisallowMultipleComponent]
public sealed class MapTutorialOnboardingSession : MonoBehaviour
{
    private const string StepsFile = "tutorial_map_steps.json";
    private const string StringsFile = "tutorial_map_strings.json";

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != GameFlowScenes.MapTutorial)
            yield break;

        if (SaveManager.LoadProfile().map_onboarding_completed)
            yield break;

        yield return null;

        var generator = GetComponent<MapGenerator>();
        if (generator == null)
            yield break;

        if (!generator.Graph.TryGetValue((0, 0), out MapNode highlightNode))
        {
            Debug.LogWarning("[MapTutorialOnboardingSession] Nó (0,0) ausente no grafo; verifique MapGenerator no fluxo MapTutorial.");
            yield break;
        }

        string dir = Application.streamingAssetsPath;
        string stepsPath = Path.Combine(dir, StepsFile);
        string stringsPath = Path.Combine(dir, StringsFile);

        var steps = TutorialOnboardingJsonLoader.Load(stepsPath, stringsPath, null);
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning("[MapTutorialOnboardingSession] Passos vazios ou ficheiros em falta.");
            yield break;
        }

        // Substitui o texto do passo final usando os dados reais do nó (0,0) do mapa tutorial.
        // O passo continua centrado (sem alvo) e avança via botão Continuar; a restrição de clique
        // ao nó (0,0) durante MapTutorial é tratada em NodeInteraction.
        foreach (var step in steps)
        {
            if (step.stepId != "map_spotlight")
                continue;
            step.tooltipText = BuildSpotlightText(highlightNode);
            break;
        }

        TutorialManager.RegisterPendingBootstrap(TutorialIds.MapOnboarding, steps);

        var host = new GameObject("MapTutorialTutorialRoot");
        host.transform.SetParent(transform, false);
        host.AddComponent<TutorialManager>();
    }

    private static string BuildSpotlightText(MapNode node)
    {
        string diff = node.Difficulty switch
        {
            CombatEquationDifficulty.Easy => "fácil",
            CombatEquationDifficulty.Medium => "média",
            CombatEquationDifficulty.Hard => "difícil",
            _ => node.Difficulty.ToString()
        };

        string eq = node.Type switch
        {
            MapNodeType.Combat_Add => "adição",
            MapNodeType.Combat_Sub => "subtração",
            MapNodeType.Combat_Multi => "multiplicação",
            MapNodeType.Combat_Div => "divisão",
            _ => node.Type.ToString()
        };

        return
            "Vamos começar pela <b>primeira casa</b> da base. " +
            "Esta fase é de <b>" + eq + "</b>, dificuldade <b>" + diff + "</b> — " +
            "é a única clicável agora. Pressiona <b>Continuar</b> e depois clica nela para entrar.";
    }
}
