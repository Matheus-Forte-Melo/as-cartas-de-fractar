using UnityEditor;
using UnityEngine;

public static class SetBgFirstSibling
{
    [MenuItem("Tools/SetBgFirstSibling")]
    public static void DoIt()
    {
        var go = GameObject.Find("imgBackground");
        if (go != null)
        {
            go.transform.SetAsFirstSibling();
            Debug.Log("Set imgBackground as first sibling");
        }
    }
}
