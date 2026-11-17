using UnityEngine;
namespace DeliveryService.Mok
{
    // 감옥 첫날에만 사용하는 조명 전환. 공유 하늘 재질은 복제해 원본을 보존한다.
    public sealed class PrisonEvening : MonoBehaviour
    {
        Light sun; Material originalSky, sky;
        Quaternion rotation; Color sunColor, ambientSky, ambientEquator, ambientGround, fog;
        float intensity; bool saved;
        public bool IsEvening { get; private set; }
        public void SetEvening()
        {
            if(!saved){sun=RenderSettings.sun;originalSky=RenderSettings.skybox;if(originalSky){sky=new Material(originalSky);RenderSettings.skybox=sky;}if(sun){rotation=sun.transform.rotation;sunColor=sun.color;intensity=sun.intensity;}ambientSky=RenderSettings.ambientSkyColor;ambientEquator=RenderSettings.ambientEquatorColor;ambientGround=RenderSettings.ambientGroundColor;fog=RenderSettings.fogColor;saved=true;}
            if(sun){sun.transform.rotation=Quaternion.Euler(8,265,0);sun.color=new Color(1,.48f,.24f);sun.intensity=.75f;}
            if(sky){if(sky.HasProperty("_Zenith"))sky.SetColor("_Zenith",new Color(.10f,.12f,.27f));if(sky.HasProperty("_Horizon"))sky.SetColor("_Horizon",new Color(.8f,.35f,.22f));}
            RenderSettings.ambientSkyColor=new Color(.25f,.26f,.40f);RenderSettings.ambientEquatorColor=new Color(.4f,.28f,.25f);RenderSettings.ambientGroundColor=new Color(.13f,.14f,.19f);RenderSettings.fogColor=new Color(.38f,.24f,.26f);IsEvening=true;DynamicGI.UpdateEnvironment();
        }
        public void RestoreDay(){if(!saved)return;if(sun){sun.transform.rotation=rotation;sun.color=sunColor;sun.intensity=intensity;}RenderSettings.skybox=originalSky;RenderSettings.ambientSkyColor=ambientSky;RenderSettings.ambientEquatorColor=ambientEquator;RenderSettings.ambientGroundColor=ambientGround;RenderSettings.fogColor=fog;IsEvening=false;DynamicGI.UpdateEnvironment();}
        void OnDestroy(){RestoreDay();if(sky)Destroy(sky);}
    }
}
