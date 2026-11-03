using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace DeliveryService.Mok {
[CreateAssetMenu(menuName="Delivery Service/게임 대사 모음",fileName="GameDialogue")]
public sealed class GameDialogue:ScriptableObject {
 [Serializable]public sealed class Entry {public string id,group;[TextArea(2,8)]public string original;[TextArea(2,8)]public string text;}
 public List<Entry> entries=new();
 static GameDialogue current;static Entry[] edits;public static int Revision{get;private set;}
 public static GameDialogue Current=>current?current:current=Resources.Load<GameDialogue>("GameDialogue");
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){current=null;edits=null;Revision++;}
 public static void Invalidate(){edits=null;Revision++;}
 void OnValidate(){Invalidate();}
 // 원래 문장을 진행 판정에 남겨 두고, 최종 표시할 때만 수정 문장을 적용한다.
 public static string Resolve(string source){
  if(string.IsNullOrEmpty(source)||!Current)return source;
  if(edits==null)edits=Current.entries.Where(e=>!string.IsNullOrEmpty(e.original)&&e.text!=e.original).OrderByDescending(e=>e.original.Length).ToArray();
  if(edits.Length==0)return source;
  foreach(var entry in edits)if(source==entry.original)return entry.text??"";
  // 합쳐진 여러 화자 대사·동적 안내도 치환한다. 수정한 문장을 다시 치환하지 않는다.
  var result=new StringBuilder();int position=0;
  while(position<source.Length){Entry match=null;int first=source.Length;foreach(var entry in edits){int found=source.IndexOf(entry.original,position,StringComparison.Ordinal);if(found>=0&&found<first){first=found;match=entry;}}
   if(match==null){result.Append(source,position,source.Length-position);break;}
   result.Append(source,position,first-position);result.Append(match.text??"");position=first+match.original.Length;
  }return result.ToString();
 }
}
}
