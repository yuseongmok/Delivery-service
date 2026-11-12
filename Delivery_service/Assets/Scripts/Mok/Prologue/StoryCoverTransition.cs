using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok {
// 첫 배달의 마무리와 실장의 연락 사이에만 사용하는 시간 경과 연출.
public sealed class StoryCoverTransition : MonoBehaviour {
    public StoryTutorial tutorial;
    [TextArea(2, 5)] public string settlingLine = "민기: 진짜 배달도 해야 의심을 안 받지. 당분간은 평범한 배달원처럼 지내자. 실장과 연락이 닿을 때까지.";
    public string timeCaption = "며칠 뒤";
    [TextArea(2, 5)] public string passageLine = "피자 가게에서 신분을 숨긴 채 지내던 어느 저녁.";
    [TextArea(2, 5)] public string invitation = "나 실장이야. 우리 조직 움직임이 이상해. 놈들이 날 없애려고 해.\n지금 표시한 장소로 와. 민기와 너 말고는 아무에게도 말하지 말고.";
    public bool Playing { get; private set; }
    CanvasGroup screen;
    PlayerMove move;
    CameraMove look;
    bool moveAllowed, lookAllowed;

    public IEnumerator Play() {
        if (Playing) yield break;
        Playing = true;
        move = tutorial.inventory.GetComponent<PlayerMove>();
        look = tutorial.inventory.GetComponentInChildren<CameraMove>();
        if (!look) look = FindFirstObjectByType<CameraMove>();
        if (move) { moveAllowed = move.isControllable; move.isControllable = false; }
        if (look) { lookAllowed = look.isControllable; look.isControllable = false; }
        FindFirstObjectByType<PhoneController>()?.ClosePhone();
        var root = new GameObject("Story time passage", typeof(Canvas), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
        screen = root.GetComponent<CanvasGroup>(); screen.alpha = 0;
        var background = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(root.transform, false);
        var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = Color.black;
        var label = new GameObject("Time caption", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(root.transform, false);
        label.rectTransform.anchorMin = new Vector2(.1f, .35f); label.rectTransform.anchorMax = new Vector2(.9f, .65f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = tutorial.subtitle.font; label.fontSize = 34; label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1, .91f, .74f); label.raycastTarget = false;
        yield return Fade(0, 1);
        tutorial.ui.SetActive(false);
        tutorial.day.currentTime = 18;
        label.text = GameDialogue.Resolve(timeCaption) + "\n\n<size=22>" + GameDialogue.Resolve(passageLine) + "</size>";
        yield return new WaitForSeconds(3.5f);
        label.text = "";
        yield return Fade(1, 0);
        Release();
    }

    IEnumerator Fade(float from, float to) {
        for (float t = 0; t < 1; t += Time.deltaTime / .8f) { screen.alpha = Mathf.Lerp(from, to, t); yield return null; }
        screen.alpha = to;
    }
    void Release() {
        if (move) move.isControllable = moveAllowed;
        if (look) look.isControllable = lookAllowed;
        if (screen) Destroy(screen.gameObject);
        Playing = false;
    }
    void OnDisable() { if (Playing) Release(); }
}
}
