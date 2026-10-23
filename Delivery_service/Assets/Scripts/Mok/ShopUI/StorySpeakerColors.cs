using UnityEngine;
using UnityEngine.UI;
namespace DeliveryService.Mok {
// 대사 원문은 그대로 편집하고, 화면에 표시할 때 화자별 색을 입힌다.
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Text))]
public sealed class StorySpeakerColors : MonoBehaviour {
 Text label;string displayed;
 void Awake(){label=GetComponent<Text>();label.supportRichText=true;label.color=Color.white;}
 public static string Format(string source){
  string color="FFF0D4";var lines=source.Split('\n');
  for(int i=0;i<lines.Length;i++){
   string s=lines[i];if(s.StartsWith("나:")||s.StartsWith("플레이어:"))color="9DDAFF";
   else if(s.StartsWith("민기:")||s.StartsWith("공범:"))color="FFD18B";
   else if(s.StartsWith("담당자:"))color="A6E5DF";
   else if(s.StartsWith("박진웅:"))color="F3A6A3";
   else if(s.StartsWith("연락책:"))color="C7B8E5";
   else if(s.StartsWith("정비사:"))color="C0DCA3";
   else if(s.StartsWith("실장:"))color="BFE5B5";
   else if(s.StartsWith("경비:"))color="FFCA83";
   else if(s.StartsWith("조직원:")||s.StartsWith("경찰:"))color="FFACA0";
   else if(s.StartsWith("손님:"))color="FFF0D4";
   lines[i]="<color=#"+(s.StartsWith("[")?"BCC6CD":color)+">"+s+"</color>";
  }return string.Join("\n",lines);
 }
 void LateUpdate(){if(!label||label.text==displayed)return;displayed=Format(label.text);label.text=displayed;}
}
}
