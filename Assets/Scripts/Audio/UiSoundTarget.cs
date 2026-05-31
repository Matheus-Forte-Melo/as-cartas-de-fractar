using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover + click SFX num botão uGUI. Adicionado automaticamente pelo <see cref="UiSoundDirector"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class UiSoundTarget : MonoBehaviour, IPointerEnterHandler
{
    private string _entryId;
    private Button _button;
    private bool _clickHooked;

    public string EntryId => _entryId;

    public void Wire(string entryId)
    {
        if (string.IsNullOrEmpty(entryId) || _entryId == entryId)
            return;

        _entryId = entryId;

        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null && !_clickHooked)
        {
            _button.onClick.AddListener(PlayClick);
            _clickHooked = true;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enabled)
            return;
        if (_button != null && !_button.interactable)
            return;

        UiSoundWiring.PlayEntryHover(_entryId);
    }

    private void PlayClick() => UiSoundWiring.PlayEntryClick(_entryId);

    private void OnDestroy()
    {
        if (_button != null && _clickHooked)
            _button.onClick.RemoveListener(PlayClick);
    }
}
