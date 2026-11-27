using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 미션이 끝난 뒤 화면을 검게 덮고 다음 챕터 씬을 불러온다.
// 다음 씬(YamajakiChapter)은 검은 화면에서 스스로 페이드인하므로 여기서는 암전까지만 맡는다.
public static class StorySceneFade
{
    static bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession() => busy = false;

    public static bool Busy => busy;

    /// <summary>화면을 seconds 동안 암전한 뒤 scene을 불러온다. 암전 중에는 게임 시간을 멈춘다.</summary>
    public static void FadeOutAndLoad(string scene, float seconds = 1.2f)
    {
        if (busy || string.IsNullOrEmpty(scene)) return;
        busy = true;
        var go = new GameObject("Story Scene Fade");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // 휴대폰·자막·HUD 위를 모두 덮는다.
        var image = new GameObject("Blackout", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(go.transform, false);
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.color = new Color(0, 0, 0, 0);
        image.raycastTarget = true;
        go.AddComponent<Runner>().StartCoroutine(Run(image, scene, Mathf.Max(.1f, seconds)));
    }

    static IEnumerator Run(Image image, string scene, float seconds)
    {
        float previousScale = Time.timeScale;
        Time.timeScale = 0f; // 암전 중 플레이어·적·차량이 움직이지 않게 한다.
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        {
            image.color = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, t / seconds));
            yield return null;
        }
        image.color = Color.black;
        yield return null; // 완전히 검은 한 프레임을 보여 준 뒤 전환한다.
        Time.timeScale = previousScale > 0 ? previousScale : 1f;
        busy = false;
        SceneManager.LoadScene(scene);
    }

    sealed class Runner : MonoBehaviour { }
}
