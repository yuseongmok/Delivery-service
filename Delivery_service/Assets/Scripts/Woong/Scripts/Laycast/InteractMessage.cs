using UnityEngine;

public class InteractMessage : MonoBehaviour
{
    [Header("엑셀파일 번호 입력하면 메세지뜸")]
    //이 파일을 MessageManager이랑 연동된다 보시면 되요 
    // 이 파일을 이제 제가 만든 엑셀파일에서 번호를 찾고 입력하면 
    // 그 오브젝트에 다가가서 레이캐스트로 쏘면 플레이어 화면에서 메세지가 떠요
    public int messageID = 1;
}