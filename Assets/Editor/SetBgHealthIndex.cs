using UnityEngine;
using UnityEditor;

public class SetBgHealthIndex : MonoBehaviour
{
    [MenuItem("Tools/Set Bg Health Index")]
    public static void Run()
    {
        var bgPlayerHealth = GameObject.Find("bgPlayerHealth");
        var txtPlayerHealth = GameObject.Find("txtPlayerHealth");
        if (bgPlayerHealth != null && txtPlayerHealth != null)
        {
            bgPlayerHealth.transform.SetSiblingIndex(txtPlayerHealth.transform.GetSiblingIndex());
        }

        var bgEnemyHealth = GameObject.Find("bgEnemyHealth");
        var txtEnemyHealth = GameObject.Find("txtEnemyHealth");
        if (bgEnemyHealth != null && txtEnemyHealth != null)
        {
            bgEnemyHealth.transform.SetSiblingIndex(txtEnemyHealth.transform.GetSiblingIndex());
        }
    }
}
