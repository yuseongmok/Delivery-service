using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Yun.ShopUI
{
    // Bind logic to the existing scene canvas; never generate a UI at runtime.
    public static class ShopPcUiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PendingStockText.Install(scene);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (ShopPC pc in root.GetComponentsInChildren<ShopPC>(true))
            {
                if (pc.GetComponent<ShopPcUiHost>() == null)
                    pc.gameObject.AddComponent<ShopPcUiHost>();
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class ShopPcUiHost : MonoBehaviour
    {
        private ShopPC pc;
        private GameObject originalPanel;
        private GameObject viewObject;

        private void Awake()
        {
            pc = GetComponent<ShopPC>();
            if (pc == null) return;
            originalPanel = pc.pcUIPanel;
            var view = originalPanel != null ? originalPanel.GetComponent<ShopPcUiView>() : null;
            if (view == null) return; // Scenes without a baked PC UI keep their original UI.
            viewObject = view.gameObject;
            viewObject.SetActive(false);
            view.Initialize(pc);
            var business = gameObject.AddComponent<ShopBusinessUiBridge>();
            business.Bind(pc, view, originalPanel);
            gameObject.AddComponent<ShopSupplyUiBridge>().Bind(view);
        }

        private void OnDestroy()
        {
            if (pc != null && pc.pcUIPanel == viewObject) pc.pcUIPanel = originalPanel;
            // The scene owns the canvas lifetime.
        }
    }
}
