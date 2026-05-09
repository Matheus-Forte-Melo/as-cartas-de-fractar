using System;
using System.Collections.Generic;
using System.IO;
using Blackjack.Decks;
using UnityEngine;

namespace Tutorial.CoreTutorial
{
    [Serializable]
    public class CoreTutorialCardSpec
    {
        public string equation;
        public bool ace;
    }

    /// <summary>
    /// Configuração forçada de **uma** rodada do <c>CoreTutorial</c>.
    /// As listas <see cref="playerOpening"/> e <see cref="enemyOpening"/> precisam ter exatamente 2 cartas cada.
    /// <see cref="drawStack"/> é a sequência de compras (topo → base) usada por jogador e inimigo após a abertura.
    /// </summary>
    [Serializable]
    public class CoreTutorialRoundSpec
    {
        public string label;
        public CoreTutorialCardSpec[] playerOpening = Array.Empty<CoreTutorialCardSpec>();
        public CoreTutorialCardSpec[] enemyOpening = Array.Empty<CoreTutorialCardSpec>();
        public CoreTutorialCardSpec[] drawStack = Array.Empty<CoreTutorialCardSpec>();
    }

    [Serializable]
    public class CoreTutorialRoundsConfig
    {
        public int playerMaxHealth = 100;
        public int enemyMaxHealth = 100;
        /// <summary>HP restaurado quando o combate entra em modo sandbox (depois das rodadas guiadas).</summary>
        public int sandboxHealth = 1000;
        public int enemyStandThreshold = 17;
        public float damageMultiplier = 10f;
        public CoreTutorialRoundSpec[] rounds = Array.Empty<CoreTutorialRoundSpec>();
    }

    /// <summary>
    /// Carrega a configuração de rodadas forçadas do tutorial de combate.
    /// </summary>
    public static class CoreTutorialRoundsLoader
    {
        public static CoreTutorialRoundsConfig LoadFromStreamingPath(string absolutePath)
        {
            var fallback = new CoreTutorialRoundsConfig();
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
            {
                Debug.LogError($"[CoreTutorialRoundsLoader] Arquivo inexistente: {absolutePath}. "
                               + "O tutorial do combate precisa deste JSON para cartas determinísticas.");
                return fallback;
            }

            try
            {
                string json = File.ReadAllText(absolutePath, System.Text.Encoding.UTF8);
                var cfg = JsonUtility.FromJson<CoreTutorialRoundsConfig>(json);
                if (cfg == null)
                {
                    Debug.LogError($"[CoreTutorialRoundsLoader] JsonUtility retornou null para {absolutePath}.");
                    return fallback;
                }
                cfg.rounds ??= Array.Empty<CoreTutorialRoundSpec>();

                Debug.Log($"[CoreTutorialRoundsLoader] JSON carregado: {absolutePath} | "
                          + $"playerHP={cfg.playerMaxHealth} enemyHP={cfg.enemyMaxHealth} sandboxHP={cfg.sandboxHealth} "
                          + $"standThreshold={cfg.enemyStandThreshold} damageMult={cfg.damageMultiplier} "
                          + $"rounds={cfg.rounds.Length}");

                for (int i = 0; i < cfg.rounds.Length; i++)
                {
                    var r = cfg.rounds[i];
                    string po = FormatCards(r?.playerOpening);
                    string eo = FormatCards(r?.enemyOpening);
                    string ds = FormatCards(r?.drawStack);
                    Debug.Log($"[CoreTutorialRoundsLoader]   Rodada #{i + 1} ({r?.label}) | "
                              + $"player=[{po}] enemy=[{eo}] drawStack=[{ds}]");
                }

                if (cfg.rounds.Length == 0)
                    Debug.LogError("[CoreTutorialRoundsLoader] rounds vazio — o tutorial vai cair em sandbox (aleatório) logo de cara!");

                return cfg;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoreTutorialRoundsLoader] Erro ao ler JSON: {ex.Message}");
                return fallback;
            }
        }

        private static string FormatCards(CoreTutorialCardSpec[] specs)
        {
            if (specs == null || specs.Length == 0) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < specs.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var s = specs[i];
                sb.Append(s == null ? "null" : s.equation);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Constrói a sequência ordenada (topo → base) de cartas a entregar nesta rodada:
        /// [P0, E0, P1, E1, drawStack...]. Cada elemento é uma <see cref="Card"/> nova.
        /// </summary>
        public static List<Card> BuildOrderedDeckFor(CoreTutorialRoundSpec spec)
        {
            var list = new List<Card>(8);
            if (spec == null)
                return list;

            if (spec.playerOpening == null || spec.playerOpening.Length < 2
                || spec.enemyOpening == null || spec.enemyOpening.Length < 2)
            {
                Debug.LogError(
                    "[CoreTutorialRoundsLoader] Rig inválido: playerOpening e enemyOpening precisam de pelo menos 2 cartas cada.");
                return list;
            }

            list.Add(BuildCard(spec.playerOpening[0]));
            list.Add(BuildCard(spec.enemyOpening[0]));
            list.Add(BuildCard(spec.playerOpening[1]));
            list.Add(BuildCard(spec.enemyOpening[1]));

            if (spec.drawStack != null)
            {
                foreach (var c in spec.drawStack)
                    list.Add(BuildCard(c));
            }

            return list;
        }

        private static Card BuildCard(CoreTutorialCardSpec spec)
        {
            if (spec == null)
                return new Card("0", false);
            string eq = spec.equation ?? "0";
            return new Card(eq, spec.ace);
        }
    }
}
