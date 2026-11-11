# 프롤로그 코드 안내

## 수정할 파일 찾기

| 변경할 내용 | 담당 파일 |
|---|---|
| 진행 단계, 돈 수집 완료, 탑승 조건, 운전 완료, 일시정지·실패·재시작·본편 전환 | `BankPrologue.cs` |
| 첫 교전 → 숨 돌리기 → 증원 → 마지막 압박, 화면 밖 증원 후보 선택 | `PrologueCombatDirector.cs` |
| 오프닝 카메라, 컷씬/플레이 카메라 전환, 1인칭 탑승 연결, 도시 진입·매장 도착·로고 | `PrologueCinematics.cs` |
| 금고 경보음 반복·쉬는 시간·일시정지·수집 완료 시 종료 | `PrologueAlarm.cs` |

주 진행 클래스는 `BankPrologue`다. 세 담당 클래스는 일반 C# 클래스이며 `Awake`에서 생성한다. 씬에 새 컴포넌트를 붙일 필요는 없다. 기존 필드 이름, 대사 연결, 단계 순서와 외부 호출 함수는 유지했다.

## 기존 동작 담당자

- `PrologueAccomplice`: 민기 이동과 수집 진행
- `ProloguePartnerPerformance`: 금고 해제, 돈 넣기, 가방 메기와 차량 탑승 동작
- `PrologueEscapePerformance`: 1인칭 탑승과 탈출 연출 보강
- `PrologueDrive`: 직접 운전
- `PrologueShopArrival`: 차량 안 대화와 매장 도착 동작
- `PrologueStory`: 수정 가능한 대사와 연출 시간

대사를 바꿀 때는 씬에 연결된 `PrologueStory` 에셋을 수정한다. 이미 분리된 동작·운전 기능은 주 진행 파일에 다시 합치지 않는다.

## 진행과 컷씬의 관계

오프닝 → 돈을 챙기는 동안 전투 → 민기와 도보 탈출 → 차량 탑승 컷씬 → 직접 운전 → 도시 진입·매장 도착 컷씬 → 본편 튜토리얼

`PrologueCinematics`의 코루틴은 `BankPrologue`에서 실행한다. 따라서 기존 `StopAllCoroutines`, 일시정지와 컷씬 건너뛰기 제어가 계속 적용된다. 탑승 컷씬이 끝난 뒤 실제 운전을 시작하고, 도착 컷씬이 끝난 뒤 본편으로 전환하는 결정은 주 진행 클래스에 남겼다.

## 검사

Unity 메뉴 `Tools > Delivery Service > Mok > Verify Bank Prologue`로 기존 Play Mode 검사를 실행할 수 있다. 열린 씬 변경 사항을 먼저 저장해야 한다. 결과는 프로젝트의 `Logs/BankPrologue-check.txt`에 남는다.

검사는 실제 씬에서 금고·수집·공범 이동·탑승·운전·추격·매장 도착 연결을 확인하되, 전투와 이동 조건 일부는 테스트 코드가 직접 준비한다. 수동으로 모든 입력을 수행하는 플레이 테스트와는 구분한다.

이번 정리 후 진행 검사 77개, 경보·발사음 검사 14개, 키 입력 검사 21개가 통과했다. 결과 사본은 `Logs/Prologue-refactor-flow-check.txt`, `Logs/Prologue-refactor-alarm-check.txt`, `Logs/Prologue-refactor-input-check.txt`에 있다. 진행 검사는 본편 로드 직전까지 확인하며 전체 튜토리얼 완주를 포함하지 않는다.
