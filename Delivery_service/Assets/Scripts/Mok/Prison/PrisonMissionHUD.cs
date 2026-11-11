using UnityEngine;

namespace DeliveryService.Mok
{
    public sealed class PrisonMissionHUD : MonoBehaviour
    {
        public PrisonMission mission;
        GUIStyle label, subtitle;
        void OnGUI()
        {
            if (!mission || !mission.player || !mission.player.View) return;
            float scale = Screen.height / 900f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale,scale,1));
            float width = Screen.width / scale;
            label ??= new GUIStyle(GUI.skin.label) { font = MokUIFont.Font, fontSize = 23, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            subtitle ??= new GUIStyle(label) { fontSize = 24 };
            GUI.color = new Color(.04f,.07f,.1f,.88f); GUI.DrawTexture(new Rect(width/2-350,25,700,65),Texture2D.whiteTexture);
            GUI.color = new Color(1,.72f,.35f);
            string task = mission.dialogue.objectives[(int)mission.State];
            if (mission.State == PrisonMission.Phase.Deliver) task += $" ({mission.Delivered}/3)";
            GUI.Label(new Rect(width/2-340,30,680,55),task,label);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(width/2-2,448,4,4),Texture2D.whiteTexture);
            GUI.Label(new Rect(width/2-550,725,1100,110),mission.Subtitle,subtitle);
            if (mission.Target && !mission.Failing)
            {
                var screen = mission.player.View.WorldToScreenPoint(mission.Target.position + Vector3.up * 1.5f);
                if (screen.z > 0)
                {
                    float x = Mathf.Clamp(screen.x/scale,45,width-45), y = Mathf.Clamp((Screen.height-screen.y)/scale,110,680);
                    GUI.color = new Color(1,.5f,.12f); GUI.Label(new Rect(x-25,y-20,50,40),"◇",label); GUI.color=Color.white;
                }
            }
            if (Physics.Raycast(mission.player.View.transform.position, mission.player.View.transform.forward, out var hit,3,~0,QueryTriggerInteraction.Ignore))
            {
                string hint = "";
                var point = hit.collider.GetComponentInParent<PrisonMissionPoint>();
                if (point && point.transform == mission.Target) hint = point.prompt;
                else if (hit.collider.GetComponent<FoodCartController>()) hint = "카트 밀기 / 놓기";
                else if (hit.collider.GetComponentInParent<PrisonMealCart>()) hint = mission.State == PrisonMission.Phase.Load ? "식사 싣기" : "카트 밀기 / 놓기";
                else if (hit.collider.GetComponentInParent<PrisonDistraction>()) hint = "배관을 두드려 시선 돌리기";
                if (hint.Length>0) GUI.Label(new Rect(width/2-300,660,600,40),"[F] "+hint,label);
            }
            float suspicion=0;foreach(var guard in mission.guards)if(guard)suspicion=Mathf.Max(suspicion,guard.Suspicion);
            if(suspicion>0){GUI.color=new Color(.15f,.18f,.2f);GUI.DrawTexture(new Rect(width/2-100,105,200,8),Texture2D.whiteTexture);GUI.color=new Color(1,.45f,.15f);GUI.DrawTexture(new Rect(width/2-100,105,200*suspicion,8),Texture2D.whiteTexture);}
            if(mission.Progress>0){GUI.color=Color.white;GUI.DrawTexture(new Rect(width/2-100,700,200*mission.Progress,5),Texture2D.whiteTexture);}
            GUI.color=new Color(0,0,0,mission.Fade);GUI.DrawTexture(new Rect(0,0,width,900),Texture2D.whiteTexture);GUI.color=Color.white;
        }
    }
}
