using UnityEditor;
using UnityEngine;
using TMPro;

public class CheckTMPStatus
{
    [MenuItem("Tools/Check TMP Status")]
    public static void Check()
    {
        TMP_Text[] sceneTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var t in sceneTexts) {
            if (count < 20) {
                Debug.Log($"TMP '{t.name}' (Active: {t.gameObject.activeInHierarchy}): Font='{(t.font != null ? t.font.name : "null")}', Mat='{(t.fontSharedMaterial != null ? t.fontSharedMaterial.name : "null")}', Color={t.color}, Scale={t.transform.localScale}, Size={t.fontSize}, Text='{t.text}'");
            }
            count++;
        }
        Debug.Log($"Total TMP found: {count}");
    }
}
