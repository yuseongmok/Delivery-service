using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeliveryService.Yun.ShopUI
{
    // Only runtime instances are changed. Existing scene/prefab assets remain untouched.
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
            viewObject = new GameObject("Yun Shop PC UI", typeof(RectTransform));
            viewObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(viewObject, gameObject.scene);
            var view = viewObject.AddComponent<ShopPcUiView>();
            view.Build(pc);
            if (originalPanel != null) originalPanel.SetActive(false);
            pc.pcUIPanel = viewObject;
            var business = gameObject.AddComponent<ShopBusinessUiBridge>();
            business.Bind(pc, view, originalPanel);
            gameObject.AddComponent<ShopSupplyUiBridge>().Bind(view);
        }

        private void OnDestroy()
        {
            if (pc != null && pc.pcUIPanel == viewObject) pc.pcUIPanel = originalPanel;
            if (viewObject != null) Destroy(viewObject);
        }
    }
}
