using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryService.Mok
{
    // 미션 원문은 보존하고, 가까운 목적지에서 표시할 안내만 선택한다.
    [DefaultExecutionOrder(11500)]
    public sealed class StoryInteractionCue : MonoBehaviour
    {
        private static readonly List<StoryInteractionCue> cues = new List<StoryInteractionCue>();
        private Text marker, objective, prompt;
        private bool promptEnabled;
        private Vector2 originalSize;
        private float distance;
        private bool onScreen;
        private int placedFrame = -10;
        private string distantText, movedHint, action;
        public bool ShowingAction { get; private set; }
        public string Action => action;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>cues.Clear();

        public static void Bind(Text marker, Text objective, Text prompt=null)
        {
            if(GameModeSession.IsChallenge || !marker || !objective || marker.GetComponent<StoryInteractionCue>())return;
            var cue=marker.gameObject.AddComponent<StoryInteractionCue>();
            cue.marker=marker;cue.objective=objective;cue.prompt=prompt;
            StoryCutsceneHUD.Track(marker.gameObject);
            cue.promptEnabled=prompt&&prompt.enabled;cue.originalSize=marker.rectTransform.sizeDelta;cues.Add(cue);
        }
        public void Record(float distance,bool onScreen,string distantText)
        {
            this.distance=distance;this.onScreen=onScreen;this.distantText=distantText;placedFrame=Time.frameCount;
        }
        private void LateUpdate()
        {
            ShowingAction=false;movedHint="";action="";
            if(prompt)prompt.enabled=promptEnabled;
            if(!marker||!objective||GameModeSession.IsChallenge||placedFrame!=Time.frameCount)return;
            marker.text=distantText;marker.rectTransform.sizeDelta=originalSize;
            if(!marker.gameObject.activeInHierarchy || !onScreen || distance>3.8f)return;
            string raw=objective.text??"";
            string hint=raw.Split('\n').FirstOrDefault(x=>x.Contains("[F]"))??"";
            bool fromPrompt=prompt&&prompt.gameObject.activeInHierarchy&&prompt.text.StartsWith("F  ");
            if(fromPrompt)hint="[F] "+prompt.text.Substring(3).Trim();
            if(string.IsNullOrWhiteSpace(hint))
            {
                StoryObjectiveCard.Split(raw.Split('\n')[0],"",out _,out action);
                if(string.IsNullOrWhiteSpace(action))return;
            }
            else action=ResolveAction(raw,hint);
            marker.text=action;marker.rectTransform.sizeDelta=new Vector2(Mathf.Max(360,originalSize.x),Mathf.Max(100,originalSize.y));ShowingAction=true;movedHint=hint;
            if(fromPrompt)prompt.enabled=false;
        }
        public static string ResolveAction(string objective,string hint)
        {
            if(hint.Trim()=="[F] 상호작용")
            {
                string first=objective.Split('\n')[0];
                StoryObjectiveCard.Split(first,"",out _,out string goal);
                if(goal=="납품 출입구")goal="행사장으로 들어가기";
                else if(goal=="서비스 테이블")goal="피자 놓기";
                return "[F] "+goal;
            }
            if(hint==objective.Split('\n')[0]){StoryObjectiveCard.Split(hint,"",out _,out string inline);return inline;}
            return hint.Trim(); // 길게 누르기/바라보기/정차 조건은 그대로 전달한다.
        }
        public static string WithoutDuplicate(Text source,string instruction)
        {
            var cue=cues.Find(c=>c&&c.objective==source&&c.ShowingAction&&c.marker&&c.marker.gameObject.activeInHierarchy&&c.placedFrame==Time.frameCount);
            if(!cue)return instruction;
            StoryObjectiveCard.Split(cue.movedHint,"",out _,out string shortenedHint);
            string actionWithoutKey=cue.action.Replace("[F]","").Trim();
            return string.Join("\n",instruction.Split('\n').Where(line=>line.Trim()!=cue.movedHint.Trim()&&line.Trim()!=shortenedHint&&line.Trim()!=actionWithoutKey));
        }
        private void OnDisable(){ShowingAction=false;if(marker)marker.rectTransform.sizeDelta=originalSize;if(prompt)prompt.enabled=promptEnabled;}
        private void OnDestroy(){cues.Remove(this);if(prompt)prompt.enabled=promptEnabled;}
    }
}
