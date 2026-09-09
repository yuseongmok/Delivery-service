# Yun Audio

기존 게임 코드, 씬, 프리팹을 수정하지 않는 독립 사운드 기반이다. 지금은 빈 슬롯과 중앙 재생기만 제공하며, 실제 게임 동작과의 연결은 나중에 한다.

## 설치

1. Unity에서 `Assets/Scripts/Yun` 스크립트가 컴파일될 때까지 기다린다.
2. Project 창 빈 곳에서 `Create > Delivery Service > Yun > Sound Library (All Slots)` 를 선택한다. 모든 `SoundId` 빈 슬롯이 들어 있는 `SoundLibrary` 에셋이 생긴다.
3. 직접 `Create > Delivery Service > Yun > Sound Library` 로 빈 에셋을 만든 경우에는 Inspector의 **없는 SoundId 빈 슬롯 채우기** 버튼을 누른다. 이미 넣은 음원과 설정은 덮어쓰지 않는다.
4. 새 루트 GameObject를 만들고 `Add Component > Delivery Service/Yun/Audio Manager` 를 추가한다. 기존 씬 오브젝트를 찾아 붙이지 말고, 나중에 쓸 전용 오브젝트를 쓰는 것이 안전하다.
5. Audio Manager의 Library 필드에 방금 만든 `SoundLibrary` 를 넣는다.
6. 필요하면 Master / BGM / SFX / UI / Ambience 볼륨과 음소거만 조정한다. 저장 기능과 AudioMixer 에셋은 없다.

Audio Manager는 중복 인스턴스를 버리고, 루트 오브젝트로 분리한 뒤 씬 전환 시에도 유지한다.

## 음원 할당

1. `SoundLibrary` 에셋을 선택한다.
2. 각 슬롯의 `Clips` 크기를 늘리고 AudioClip을 넣는다. 여러 개를 넣으면 재생 시 비어 있지 않은 클립 중 하나를 고른다.
3. 카테고리, 기본 볼륨, 피치 범위, 반복, 2D/3D, 3D 거리를 항목마다 맞춘다.
4. 클립이 없어도 슬롯은 유효하다. 그 ID를 재생하면 아무 일도 하지 않고 무효 핸들만 돌려준다.
5. 중복 ID, 뒤집힌 피치 범위, 잘못된 3D 거리는 Inspector 경고로만 보여 준다. 콘솔을 반복 출력하지 않는다.

예약 슬롯(`온기 부족`, `경찰 사이렌` 등)도 ID와 설정 자리만 있다. 해당 게임 기능은 이 작업에서 만들지 않았다.

## 사용 예시

나중에 게임 코드에서 연결할 때의 호출 형태다. 지금은 기존 스크립트에 넣지 않는다.

```csharp
using DeliveryService.Yun.Audio;
using UnityEngine;

public class ExampleSoundCalls : MonoBehaviour
{
    [SerializeField] Transform motorcycle;
    [SerializeField] Vector3 brakePoint;

    void Examples()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
        {
            return;
        }

        // 2D. UI/BGM처럼 위치와 무관한 소리.
        audio.Play(SoundId.UiClick);

        // 지정한 월드 위치의 3D 소리.
        audio.PlayAt(SoundId.MotorcycleBrake, brakePoint);

        // Transform을 따라가는 반복음. 풀 오브젝트는 대상의 자식이 되지 않는다.
        SoundHandle engine = audio.PlayFollow(SoundId.MotorcycleEngineLoop, motorcycle);

        // 개별 정지, 볼륨, 피치.
        engine.SetVolume(0.6f);
        engine.SetPitch(1.15f);
        engine.Stop();

        // 같은 ID 전체, 또는 모든 소리 정지.
        audio.Stop(SoundId.OvenLoop);
        audio.StopAll();

        audio.SetCategoryVolume(SoundCategory.Sfx, 0.5f);
        audio.SetCategoryMuted(SoundCategory.Bgm, true);
        audio.MasterVolume = 0.8f;
    }
}
```

재생이 실패하면 예외를 던지지 않고 `SoundHandle.Invalid` 를 반환한다. 실패한 핸들의 `Stop` / `SetVolume` / `SetPitch` 는 무시된다.

## 풀 동작

- AudioSource는 Audio Manager 아래 `Voices` 자식으로만 두고 재사용한다.
- 기본 최대 동시 재생 수는 32, 상한은 64다.
- 빈 슬롯이 있으면 그것을 먼저 쓴다.
- 원샷이 끝나면 자동으로 슬롯을 돌려준다.
- 풀이 가득이고 원샷이 하나라도 있으면, **가장 오래된 원샷만** 끊고 그 슬롯을 재사용한다. 이전 핸들은 무효가 되어 새 소리를 제어하지 못한다.
- 풀이 반복음만으로 가득이면 새 재생을 건너뛴다. 반복음을 자동으로 빼앗지 않는다.
- 따라가던 Transform이 파괴되면 해당 재생만 정리한다.
- 재사용 전에 AudioSource 설정을 초기화한다.

## 이번에 연결하지 않은 것

기존 클래스 참조, 이벤트 구독, 입력 훅, 씬/프리팹 자동 부착은 하지 않았다. 나중에 연결할 후보는 다음과 같다.

- 플레이어 걷기/달리기/점프/착지 (`PlayerMove`)
- 주문서, 수락/거절, 배달 성공/거부, UI (`OrderUI`, `OrderManager`, `Counter`, `CurrentOrderUI`)
- 피자 제작/오븐/포장/쓰레기 (`PizzaMaker`, `Oven`, `PizzaPacking`, `TrashCan` 등)
- 오토바이 탑승/엔진/충돌/고장 (`MotorcycleController`, `MotorcycleAccident`, `MotorcycleDurability`)
- 주유 (`FuelNozzle`, `FuelCap`, `MotorcycleFuel`)
- NPC 발소리/사고, 차량 엔진 (`NPCControl`, `CarMov`)
- 가게/낮밤 환경음 (`DayNightCycle`)
- 수입/지출/구매/정산 (`MoneyManager`, `PurchaseTrigger`, `SettlementUI`)
- 온기, 배달 제한시간, 영업 시작/마감, 월세, 파산, 수리, 합의/도주, 소매치기, 경찰/추격

고급 연출(BGM 전환, 덕킹)도 포함하지 않는다.

## 검증

Unity 메뉴 `Tools > Delivery Service > Yun > Run Audio System Verification` 으로 정적 검사를 실행할 수 있다. Play Mode에서 실행하면 동시 재생, 반복음 정지, 핸들 재사용, 대상 파괴, 볼륨 변경까지 확인한다.
