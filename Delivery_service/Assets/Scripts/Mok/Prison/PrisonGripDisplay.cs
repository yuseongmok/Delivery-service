using UnityEngine;
namespace DeliveryService.Mok
{
    // 조작 설명 없이 남은 악력만 보여 주는 작은 분할 게이지.
    public static class PrisonGripDisplay
    {
        static Texture2D capsule;
        static void Ensure(){if(capsule)return;capsule=new Texture2D(64,16,TextureFormat.RGBA32,false);var pixels=new Color[1024];for(int y=0;y<16;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(7.5f-x,x-55.5f),dy=y-7.5f;float a=dx<=0?1:Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*64+x]=new Color(1,1,1,a);}capsule.SetPixels(pixels);capsule.Apply();}
        public static void Draw(float grip){
            Ensure();float s=Mathf.Min(Screen.width/1920f,Screen.height/1080f);var old=GUI.color;var r=new Rect(Screen.width*.5f-154*s,Screen.height*.96f,308*s,24*s);
            GUI.color=new Color(.025f,.04f,.045f,.85f);GUI.DrawTexture(new Rect(r.x-5*s,r.y-5*s,r.width+10*s,r.height+10*s),capsule);
            GUI.color=new Color(.32f,.38f,.39f,.8f);GUI.DrawTexture(r,capsule);
            GUI.color=new Color(.055f,.08f,.09f,.95f);GUI.DrawTexture(new Rect(r.x+2*s,r.y+2*s,r.width-4*s,r.height-4*s),capsule);
            float pulse=.7f+.3f*Mathf.Sin(Time.unscaledTime*9);var fill=grip<.25f?new Color(1,.27f,.12f,pulse):Color.Lerp(new Color(1,.68f,.27f),new Color(.85f,.91f,.84f),grip);
            for(int i=0;i<18;i++){float amount=Mathf.Clamp01(grip*18-i);if(amount<=0)continue;GUI.color=fill;GUI.DrawTexture(new Rect(r.x+(8+i*16.4f)*s,r.y+7*s,13.4f*amount*s,10*s),capsule);}
            GUI.color=old;
        }
    }
}
