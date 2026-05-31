using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

/// <summary>
/// Liga hover/click SFX a botões UI Toolkit conforme ui_sounds_config.json.
/// </summary>
public static class UiSoundToolkitBinder
{
    private static readonly Dictionary<VisualElement, EventCallback<PointerEnterEvent>> HoverHandlers = new();
    private static readonly Dictionary<VisualElement, Action> ClickHandlers = new();
    private static readonly Dictionary<VisualElement, string> EntryIds = new();

    public static void Bind(
        VisualElement root,
        string documentHint,
        IReadOnlyList<UiSoundEntry> entries)
    {
        if (root == null || string.IsNullOrEmpty(documentHint) || entries == null)
            return;

        foreach (UiSoundEntry entry in entries)
        {
            if (entry?.match == null
                || !string.Equals(entry.match.ui, "uitoolkit", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(entry.match.documentHint)
                && !string.Equals(entry.match.documentHint, documentHint, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(entry.match.elementName))
            {
                root.Query<Button>(name: entry.match.elementName).ForEach(btn =>
                    RegisterButton(btn, entry.id));
            }

            if (!string.IsNullOrEmpty(entry.match.elementNamePattern))
            {
                root.Query<Button>().ForEach(btn =>
                {
                    if (MatchesElementName(btn.name, entry.match.elementNamePattern))
                        RegisterButton(btn, entry.id);
                });
            }
        }
    }

    private static void RegisterButton(Button btn, string entryId)
    {
        if (btn == null || string.IsNullOrEmpty(entryId))
            return;

        if (HoverHandlers.TryGetValue(btn, out EventCallback<PointerEnterEvent> oldHover))
        {
            btn.UnregisterCallback(oldHover);
            HoverHandlers.Remove(btn);
        }

        if (ClickHandlers.TryGetValue(btn, out Action oldClick))
        {
            btn.clicked -= oldClick;
            ClickHandlers.Remove(btn);
        }

        EntryIds.Remove(btn);

        void OnEnter(PointerEnterEvent _) => UiSoundWiring.PlayEntryHover(entryId);
        void OnClick() => UiSoundWiring.PlayEntryClick(entryId);

        btn.RegisterCallback<PointerEnterEvent>(OnEnter);
        btn.clicked += OnClick;
        HoverHandlers[btn] = OnEnter;
        ClickHandlers[btn] = OnClick;
        EntryIds[btn] = entryId;
    }

    private static bool MatchesElementName(string name, string pattern)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
            return false;

        if (pattern.Contains('|'))
        {
            string[] parts = pattern.Split('|');
            foreach (string part in parts)
            {
                if (MatchesElementName(name, part.Trim()))
                    return true;
            }

            return false;
        }

        if (pattern.Contains('*'))
        {
            string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(name, regex);
        }

        return string.Equals(name, pattern, StringComparison.Ordinal);
    }
}
