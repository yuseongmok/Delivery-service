using DeliveryService.Mok;
using UnityEngine;
using UnityEngine.SceneManagement;

// 이전 씬에서 플레이어가 고른 무기만 기억해 다음 챕터로 넘긴다.
// 다른 씬의 스크립트나 진행 상태는 건드리지 않고, 장착된 무기를 읽기만 한다.
public static class SelectedWeaponCarry
{
    public static WeaponKind Weapon { get; private set; } = WeaponKind.None;
    public static int PistolAmmo { get; private set; }
    public static bool HasWeapon => Weapon != WeaponKind.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        Weapon = WeaponKind.None;
        PistolAmmo = 0;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateTracker()
    {
        var go = new GameObject("Selected Weapon Carry") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        go.AddComponent<Tracker>();
    }

    // 새 게임(로비/프롤로그)에서는 이전 선택을 지운다.
    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby" || scene.name == "BankPrologue")
        {
            Weapon = WeaponKind.None;
            PistolAmmo = 0;
        }
    }

    // 무기를 든 상태만 기록한다. 무기가 없는 씬에 들어와도 이전 선택이 지워지지 않는다.
    static void Record(PlayerWeapon weapon)
    {
        if (weapon == null || !weapon.isActiveAndEnabled || !weapon.HasWeapon) return;
        Weapon = weapon.Equipped;
        PistolAmmo = weapon.Equipped == WeaponKind.Pistol ? weapon.Ammo : 0;
    }

    sealed class Tracker : MonoBehaviour
    {
        PlayerWeapon cached;
        float nextSearch;

        void Update()
        {
            if (cached == null)
            {
                if (Time.unscaledTime < nextSearch) return;
                nextSearch = Time.unscaledTime + 1f;
                cached = FindFirstObjectByType<PlayerWeapon>();
                if (cached == null) return;
            }
            Record(cached);
        }
    }
}
