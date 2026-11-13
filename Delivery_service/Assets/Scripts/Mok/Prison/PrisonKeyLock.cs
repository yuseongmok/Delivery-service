using System.Collections;
using UnityEngine;
namespace DeliveryService.Mok
{
    // 열쇠 선택과 문 회전을 분리해 추후 잠금장치 연출만 교체할 수 있다.
    public sealed class PrisonKeyLock:MonoBehaviour
    {
        public Transform door;
        public GameObject[] keyModels;
        public int correctKey=2;
        public float turnSeconds=1.1f;
        public int Selected{get;private set;}
        public float Progress{get;private set;}
        public bool Opened{get;private set;}
        Quaternion closed;
        void Awake(){closed=door.localRotation;Show(false);}
        public void Select(int direction){Selected=(Selected+direction+keyModels.Length)%keyModels.Length;Progress=0;Show(true);}
        public void Show(bool value){for(int i=0;i<keyModels.Length;i++)keyModels[i].SetActive(value&&i==Selected);}
        public int Turn(float seconds){Progress+=seconds/turnSeconds;keyModels[Selected].transform.localRotation=Quaternion.Euler(0,0,-90*Mathf.Clamp01(Progress));if(Progress<1)return 0;Progress=0;return Selected==correctKey?1:-1;}
        public void Cancel(){Progress=0;Show(false);}
        public IEnumerator Open(){Opened=true;Show(false);var obstacle=door.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(obstacle)obstacle.enabled=false;for(float t=0;t<.8f;t+=Time.deltaTime){door.localRotation=closed*Quaternion.Euler(0,Mathf.SmoothStep(0,105,t/.8f),0);yield return null;}door.localRotation=closed*Quaternion.Euler(0,105,0);}
        public void ResetDoor(){Opened=false;Selected=0;Progress=0;door.localRotation=closed;var obstacle=door.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(obstacle)obstacle.enabled=true;Show(false);}
    }
}
