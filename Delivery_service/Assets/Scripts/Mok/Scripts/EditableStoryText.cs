using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DeliveryService.Mok {
[DefaultExecutionOrder(10001),DisallowMultipleComponent,RequireComponent(typeof(Text))]
public sealed class EditableStoryText:MonoBehaviour {
 Text label;string source,last;int revision=-1;
 void Awake(){label=GetComponent<Text>();}
 void LateUpdate(){if(!label)return;if(label.text!=last)source=label.text;else if(revision==GameDialogue.Revision)return;revision=GameDialogue.Revision;last=GameDialogue.Resolve(source);label.text=last;}
 public static void Bind(MonoBehaviour owner){if(!owner)return;foreach(var field in owner.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance)){if(field.FieldType==typeof(Text))Attach(field.GetValue(owner) as Text);else if(field.FieldType==typeof(Text[])){var texts=field.GetValue(owner) as Text[];if(texts!=null)foreach(var t in texts)Attach(t);}}}
 static void Attach(Text label){if(label&&!label.GetComponent<StorySpeakerColors>()&&!label.GetComponent<EditableStoryText>())label.gameObject.AddComponent<EditableStoryText>();}
 static bool StoryOwner(MonoBehaviour m){if(!m)return false;var n=m.GetType().Name;return n=="BankPrologue"||n=="StoryTutorial"||n=="BrokerMission"||n=="SpecialOrderMission"||n=="BackdoorMission"||n=="MechanicMission"||n=="VenueFinale"||n=="StoryPhoneInbox"||n=="StoryPhoneApp"||n=="FirstWantedLesson";}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Loaded(default,default);}
 static void Loaded(Scene scene,LoadSceneMode mode){foreach(var m in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(StoryOwner(m))Bind(m);}
}
}
