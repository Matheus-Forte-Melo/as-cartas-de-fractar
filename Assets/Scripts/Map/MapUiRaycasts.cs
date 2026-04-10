using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Evita que drag no mapa e clique em nós consumam input quando o ponteiro está sobre UI (ex.: botão loja TEMP).
/// </summary>
public static class MapUiRaycasts
{
    public static bool IsScreenPositionOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }

    public static bool IsMouseOverUi()
    {
        if (Mouse.current == null)
            return false;
        return IsScreenPositionOverUi(Mouse.current.position.ReadValue());
    }
}
