using System;

/// <summary>
/// Configuração em StreamingAssets/Audio/ui_sounds_config.json: mapeia botões para clips em Resources (sem extensão).
/// </summary>
[Serializable]
public class UiSoundConfigRoot
{
    public int schemaVersion;
    public UiSoundDefaults defaults;
    public UiSoundEntry[] entries;
    public UiGameplayEvent[] gameplayEvents;
}

[Serializable]
public class UiGameplayEvent
{
    public string id;
    public string notes;
    public UiGameplayMatch match;
    public string[] resourcePaths;
}

[Serializable]
public class UiGameplayMatch
{
    public string eventId;
}

[Serializable]
public class UiSoundDefaults
{
    public string hoverResourcePath;
    public string clickResourcePath;
    public string[] hoverResourcePaths;
    public string[] clickResourcePaths;
}

[Serializable]
public class UiSoundEntry
{
    public string id;
    public string notes;
    public UiSoundMatch match;
    public string hoverResourcePath;
    public string clickResourcePath;
    public string[] hoverResourcePaths;
    public string[] clickResourcePaths;
}

[Serializable]
public class UiSoundMatch
{
    public string ui;
    public string objectName;
    public string sceneName;
    public string parentHint;
    public string prefabHint;
    public string elementName;
    public string elementNamePattern;
    public string documentHint;
}
