using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuCreditos : MonoBehaviour
{
    [SerializeField] private string VoltarParaMenu;

    private void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        foreach (Button btn in canvas.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<MenuButtonHover>() != null) continue;
            btn.gameObject.AddComponent<MenuButtonHover>();
        }
    }

    public void voltar()
    {
        SceneManager.LoadScene(VoltarParaMenu);
    }
}
