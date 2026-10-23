using UnityEngine;
namespace DeliveryService.Mok {
// 저장된 총구 메시와 음원을 재사용하며 사격 순간에만 표시한다.
public sealed class PrologueGunFeedback : MonoBehaviour {
    public static event System.Action<Vector3> ShotHeard;
    public Transform flame;
    public Light flash;
    public AudioClip shot;
    public bool playerGun;
    public int ShotCount {get;private set;}
    AudioSource source;
    float remaining;
    Vector3 size;
    void Awake(){
        size=flame.localScale;flame.gameObject.SetActive(false);flash.enabled=false;
        source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;
        source.spatialBlend=playerGun?0:1;source.minDistance=4;source.maxDistance=65;
        source.rolloffMode=AudioRolloffMode.Linear;source.volume=playerGun?.65f:.45f;
    }
    public void Fire(){
        ShotHeard?.Invoke(transform.position);
        ShotCount++;remaining=.055f;
        flame.localScale=size*Random.Range(.8f,1.18f);
        flame.localRotation=Quaternion.Euler(0,0,Random.Range(0,360));
        flame.gameObject.SetActive(true);flash.enabled=true;
        source.pitch=Random.Range(.94f,1.06f);if(shot)source.PlayOneShot(shot);
    }
    void Update(){
        remaining-=Time.deltaTime;
        if(remaining<=0){flame.gameObject.SetActive(false);flash.enabled=false;}
    }
    void OnDisable(){if(flame)flame.gameObject.SetActive(false);if(flash)flash.enabled=false;}
}
}
