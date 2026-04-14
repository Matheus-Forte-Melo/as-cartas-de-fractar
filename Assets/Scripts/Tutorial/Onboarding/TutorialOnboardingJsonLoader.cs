using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Tutorial.Onboarding
{
    /// <summary>
    /// Carrega passos e textos de arquivos JSON em StreamingAssets (textos editáveis separados dos passos).
    /// </summary>
    public static class TutorialOnboardingJsonLoader
    {
        [Serializable]
        private class StepJson
        {
            public string stepId;
            public string targetPath;
            public string stringKey;
            public int advanceMode;
            public string advanceWhenEventId;
            public float autoAdvanceDelaySeconds;
            public float spotlightPadding;
            public float tooltipOffsetX;
            public float tooltipOffsetY;
        }

        [Serializable]
        private class StepsRoot
        {
            public StepJson[] steps;
        }

        [Serializable]
        private class StringEntry
        {
            public string key;
            public string text;
        }

        [Serializable]
        private class StringsRoot
        {
            public StringEntry[] strings;
        }

        public static List<TutorialStepDefinition> Load(
            string stepsAbsolutePath,
            string stringsAbsolutePath,
            RectTransform canvasRoot)
        {
            var list = new List<TutorialStepDefinition>();
            var texts = LoadStrings(stringsAbsolutePath);

            if (!File.Exists(stepsAbsolutePath))
            {
                Debug.LogError($"[TutorialOnboardingJsonLoader] Passos não encontrados: {stepsAbsolutePath}");
                return list;
            }

            string stepsJson = File.ReadAllText(stepsAbsolutePath, Encoding.UTF8);
            var root = JsonUtility.FromJson<StepsRoot>(stepsJson);
            if (root?.steps == null || root.steps.Length == 0)
                return list;

            foreach (var sj in root.steps)
            {
                var def = new TutorialStepDefinition
                {
                    stepId = sj.stepId ?? "",
                    target = ResolveTarget(canvasRoot, sj.targetPath),
                    tooltipText = Lookup(texts, sj.stringKey),
                    advanceMode = (TutorialAdvanceMode)Mathf.Clamp(sj.advanceMode, 0, 2),
                    advanceWhenEventId = sj.advanceWhenEventId ?? "",
                    autoAdvanceDelaySeconds = sj.autoAdvanceDelaySeconds,
                    spotlightPadding = sj.spotlightPadding > 0 ? sj.spotlightPadding : 8f,
                    tooltipOffset = new Vector2(sj.tooltipOffsetX, sj.tooltipOffsetY)
                };
                list.Add(def);
            }

            return list;
        }

        private static Dictionary<string, string> LoadStrings(string absolutePath)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[TutorialOnboardingJsonLoader] Strings não encontradas: {absolutePath}");
                return map;
            }

            try
            {
                string json = File.ReadAllText(absolutePath, Encoding.UTF8);
                var root = JsonUtility.FromJson<StringsRoot>(json);
                if (root?.strings == null)
                    return map;
                foreach (var e in root.strings)
                {
                    if (e == null || string.IsNullOrEmpty(e.key))
                        continue;
                    map[e.key] = e.text ?? "";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TutorialOnboardingJsonLoader] Erro ao ler strings: {ex.Message}");
            }

            return map;
        }

        private static string Lookup(Dictionary<string, string> map, string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";
            return map.TryGetValue(key, out string t) ? t : $"[{key}]";
        }

        private static RectTransform ResolveTarget(RectTransform canvasRoot, string path)
        {
            if (canvasRoot == null || string.IsNullOrWhiteSpace(path))
                return null;

            var t = FindDeep(canvasRoot, path.Trim());
            return t as RectTransform;
        }

        private static Transform FindDeep(Transform root, string path)
        {
            if (path.Contains('/'))
            {
                var parts = path.Split('/');
                Transform cur = root;
                foreach (var p in parts)
                {
                    if (string.IsNullOrEmpty(p))
                        continue;
                    if (cur == null)
                        return null;
                    var next = cur.Find(p);
                    if (next == null)
                        return null;
                    cur = next;
                }

                return cur;
            }

            return FindRecursive(root, path);
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                var r = FindRecursive(c, name);
                if (r != null)
                    return r;
            }

            return null;
        }
    }
}
