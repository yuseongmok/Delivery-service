# 업적 시스템

JJinCity와 YunCity에 저장된 Canvas 오브젝트로 구성되어 있습니다. 실행 중 UI 오브젝트를 만들지 않습니다.

## 사용

- ESC → 설정의 **업적** 버튼 → 업적 목록. 업적 화면의 뒤로 가기 또는 ESC는 설정으로 돌아갑니다. 설정에서 ESC를 다시 누르면 게임으로 돌아갑니다.
- 첫 피자 완성: 영업 중 **구운 피자를 포장**하면 최초 1회 달성합니다. 오븐 완료만으로는 달성하지 않습니다. 영업 전에 포장한 피자를 나중에 픽업/적재하는 행동과 굽지 않은 피자의 포장은 제외합니다.
- 시간 표시 바로 아래에 `업적 1개 달성됨!`이 실제 시간 5초 동안 표시됩니다. 표시 중 추가 달성이 들어오면 개수를 합산하고 마지막 알림부터 5초를 다시 셉니다.
- 달성 여부는 PlayerPrefs의 `Yun.Achievements.first_packaged_pizza`에 저장됩니다. 날짜 전환과 재실행 후에도 유지하며, 저장된 달성 기록을 불러올 때는 알리지 않습니다.
- 보상은 미정으로 표시하며 실제 지급하지 않습니다.

## UI 편집

- 기존 설정 Canvas의 `Settings Panel/Window/Achievements`: 진입 버튼.
- `Settings Panel/Achievements Page`: 목록, 업적 이름·설명·상태·보상·뒤로 가기 버튼.
- DayNightCycle의 `timeText` 오브젝트 아래 `Yun Achievement Popup`: 시간 아래 알림. Image/Text의 색·폰트·크기·배치는 Inspector에서 바꿀 수 있습니다.
- HUD의 `Yun Achievements`: 상태 저장, 감지, 알림 컴포넌트. 항상 켜 두세요.
- 설정창과 업적창은 작은 화면에서만 축소해 화면 안에 맞춥니다. 레퍼런스 적용 시 저장된 오브젝트를 편집하면 됩니다.
- `Tools > Delivery Service > Yun > Setup Achievements`: 미배치 씬에 최초 배치. 이미 업적이 있는 씬은 다시 만들지 않아 디자인을 보존합니다.
- Play 모드에서 `Preview Achievement Notification` 메뉴로 업적 기록을 변경하지 않고 알림을 미리 볼 수 있습니다.

## 기존 코드 연결

Min/Woong/Mok 코드를 수정하지 않습니다. `PizzaAchievementBridge`는 Inspector에 연결된 픽업대·인벤토리·오토바이의 PizzaData를 관찰하며, 같은 데이터의 이동은 새 제작으로 세지 않습니다. 영업 상태는 `DayNightCycle.isShopOpen`, 굽기 여부는 `PizzaData.isBaked`를 읽습니다.

포장 이벤트/공개 조회 API가 없어 픽업대의 `packagedDataList`와 오토바이의 `storedPizzas`만 캐시한 리플렉션으로 읽습니다. `link.xml`은 IL2CPP 빌드에서 이 필드가 제거되지 않게 보존합니다. 팀원이 필드 이름을 변경하면 이 연결부도 함께 갱신해야 합니다. 목록에 쓰지는 않습니다.

기존 Update 상호작용 뒤 LateUpdate에서 판정합니다. 향후 한 프레임 안에 포장부터 배달/삭제까지 모두 처리하는 자동화 기능을 추가하거나 런타임에 새로운 픽업대를 생성한다면, 명시적인 포장 이벤트나 새 감지 대상 연결이 필요합니다. 현재 씬의 플레이어 상호작용 및 같은 프레임의 픽업/적재를 지원합니다.

추가 업적을 만들 때는 AchievementManager의 ID/저장 로딩/허용 ID와 목록 항목, 해당 조건 연결을 추가하세요. 알림은 `Show(int)`로 여러 개를 합산할 수 있으며 같은 업적 중복 호출은 Manager에서 차단합니다.

## 검증

저장된 JJinCity에서 `Tools > Delivery Service > Yun > Verify Achievements`를 실행합니다. 빈 테스트 씬과 임시 저장 키를 사용하고, 끝나면 원래 씬으로 돌아갑니다. 결과는 `Temp/YunAchievementVerification.txt`에 기록됩니다.

실제 Oven/PizzaPacking/PizzaPickUpZone/인벤토리/오토바이 메서드를 사용해 영업 전·미완성 제외, 포장 달성, 동일 프레임 이동, 중복 방지, 저장 재로드, 알림 개수 합산, timeScale 0에서 5초 표시, ESC 단계별 복귀를 확인합니다. 게임 전체를 수동으로 플레이하는 테스트를 대신하지는 않습니다.
