using UnityEngine;
namespace DeliveryService.Mok {
// 결투 전용 잔상과 비유혈 타격 입자. 미션 판정에는 관여하지 않는다.
public sealed class PrisonDuelEffects : MonoBehaviour {
 PrisonMedicineDuel duel; TrailRenderer[] trails; ParticleSystem sparks; Material material;
 public void Initialize(PrisonMedicineDuel owner,Animator actor){duel=owner;if(trails!=null)return;material=new Material(Shader.Find("Sprites/Default"));trails=new TrailRenderer[2];for(int i=0;i<2;i++){var hand=actor.GetBoneTransform(i==0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);var go=new GameObject("주먹 바람 잔상");go.transform.SetParent(hand,false);var t=go.AddComponent<TrailRenderer>();t.sharedMaterial=material;t.time=.22f;t.startWidth=.065f;t.endWidth=0;t.startColor=new Color(.88f,.95f,1,.48f);t.endColor=new Color(.8f,.95f,1,0);t.minVertexDistance=.015f;t.emitting=false;trails[i]=t;}var hit=new GameObject("주먹 명중 불꽃");hit.transform.SetParent(transform,false);sparks=hit.AddComponent<ParticleSystem>();sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=sparks.main;main.loop=false;main.playOnAwake=false;main.startLifetime=.16f;main.startSpeed=1.8f;main.startSize=.035f;main.startColor=new Color(1,.83f,.42f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=24;var emission=sparks.emission;emission.enabled=false;var shape=sparks.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.045f;hit.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;}
 public void Hit(){if(!sparks)return;sparks.transform.position=duel.boss.position+Vector3.up*1.45f+duel.boss.forward*.15f;sparks.Emit(12);}
 void LateUpdate(){if(trails==null)return;for(int i=0;i<2;i++)trails[i].emitting=Time.timeScale>0&&duel.Active&&duel.Current==PrisonMedicineDuel.Phase.Strike&&(duel.DangerSide<0?i==0:i==1);}
 void OnDestroy(){if(material)Destroy(material);}
}
}

