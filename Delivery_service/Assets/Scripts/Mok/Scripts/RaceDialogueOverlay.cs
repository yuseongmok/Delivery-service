using UnityEngine;

namespace DeliveryService.Mok
{
    // The race keeps its dialogue state and editable lines; only presentation is shared.
    public sealed class RaceDialogueOverlay : MonoBehaviour
    {
        RacerNPCInteraction racer;
        CanvasGroup hiddenPanel;
        float oldAlpha;
        bool oldBlocks;
        public static bool Active { get; private set; }

        void Awake() { racer = GetComponent<RacerNPCInteraction>(); }
        void LateUpdate()
        {
            var race = RaceManager.Instance;
            bool result = race && race.ShowingResult;
            bool talking = racer && racer.IsDialogueActive;
            if (!talking && !result) { Restore(); return; }
            Active = true;
            var panel = result ? race.ResultPanel : racer.DialoguePanel;
            if (!panel) return;
            if (hiddenPanel && hiddenPanel.gameObject != panel) Restore();
            if (!hiddenPanel)
            {
                hiddenPanel = panel.GetComponent<CanvasGroup>();
                if (!hiddenPanel) hiddenPanel = panel.AddComponent<CanvasGroup>();
                oldAlpha = hiddenPanel.alpha; oldBlocks = hiddenPanel.blocksRaycasts;
            }
            hiddenPanel.alpha = 0; hiddenPanel.blocksRaycasts = false;
        }

        void Restore()
        {
            if (hiddenPanel) { hiddenPanel.alpha = oldAlpha; hiddenPanel.blocksRaycasts = oldBlocks; }
            hiddenPanel = null; Active = false;
        }
        void OnDisable() { Restore(); }

        void OnGUI()
        {
            var race = RaceManager.Instance;
            bool result = race && race.ShowingResult;
            if ((!racer || !racer.IsDialogueActive) && !result || Time.timeScale == 0) return;
            string line = result ? race.ResultLine : racer.CurrentLine;
            if (!line.Contains(":")) line = "레이서: " + line;
            var previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1920 * scale) * .5f, 0, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            var text = new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 32, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var rect = new Rect(210, 840, 1500, 110);
            text.normal.textColor = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), line, text);
            text.normal.textColor = line.StartsWith("나:") ? new Color(.55f, .82f, 1) : new Color(1, .94f, .8f);
            GUI.Label(rect, line, text);
            text.fontSize = 20; text.normal.textColor = new Color(1, 1, 1, .7f);
            if (result || racer.MissionDialogue)
                GUI.Label(new Rect(510, 972, 900, 42), result ? "[F] 계속" : "[F] 계속   ·   [Space] 건너뛰기", text);
            else
            {
                var button = new GUIStyle(GUI.skin.button) { font = MokUIFont.Font, fontSize = 25 };
                if (GUI.Button(new Rect(680, 970, 260, 50), "레이스 시작", button)) racer.OnClick_Yes();
                if (GUI.Button(new Rect(980, 970, 260, 50), "다음에", button)) racer.OnClick_No();
            }
            GUI.matrix = previous;
        }
    }
}
