using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Carrega e indexa todas as definições de itens a partir de StreamingAssets/Items/item_catalog.json.
    /// </summary>
    public static class ItemCatalog
    {
        private static Dictionary<string, ItemDefinition> _byId;
        private static ItemDefinition[] _all;

        public static ItemDefinition[] All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static ItemDefinition Get(string id)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out var def) ? def : null;
        }

        public static void Reload()
        {
            _byId = null;
            _all = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, ItemDefinition>();

            string path = Path.Combine(Application.streamingAssetsPath, "Items", "item_catalog.json");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ItemCatalog] Arquivo não encontrado: {path}");
                _all = System.Array.Empty<ItemDefinition>();
                return;
            }

            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var data = JsonUtility.FromJson<ItemCatalogData>(json);

                if (data?.items == null)
                {
                    Debug.LogWarning("[ItemCatalog] JSON inválido ou sem itens.");
                    _all = System.Array.Empty<ItemDefinition>();
                    return;
                }

                _all = data.items;
                foreach (var item in _all)
                {
                    if (string.IsNullOrEmpty(item.id))
                    {
                        Debug.LogWarning("[ItemCatalog] Item sem id ignorado.");
                        continue;
                    }
                    _byId[item.id] = item;
                }

                Debug.Log($"[ItemCatalog] {_byId.Count} itens carregados.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemCatalog] Falha ao carregar: {ex.Message}");
                _all = System.Array.Empty<ItemDefinition>();
            }
        }
    }
}
