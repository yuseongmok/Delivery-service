# YunCity 가로등 테스트

- 적용 씬: `Assets/Scenes/JJinCity.unity`. 테스트 복사본 `Assets/Scenes/YunCity.unity`도 유지합니다.
- 제어 오브젝트: `Yun Streetlights (16h)`
- 연결: 기존 `Time` 오브젝트의 `DayNightCycle.currentTime`을 읽습니다. 시간이나 영업 상태는 변경하지 않습니다.
- 영업 중 06:00 이상~09:00 미만 또는 16:00 이상이면 켜집니다. 09:00~16:00에는 꺼집니다. 마감하면 시간과 관계없이 꺼지고, 점등 시간대에 마감을 취소하면 다시 켜집니다. 다음 날 06:00 영업 시작 전에는 꺼져 있고 영업을 시작하면 켜집니다. 가로등과 오토바이 전조등이 동일한 조건을 사용합니다.
- JJinCity에는 검증된 조명 오브젝트와 해당 씬의 시간 연결만 이관했습니다. 기존 게임 스크립트, Synty/Bike 프리팹·머티리얼, 프로젝트 렌더링 설정은 수정하지 않았습니다. 다른 씬에 자동 설치하지 않습니다.

## 구성

프리팹 내부를 포함한 활성 가로등 91개에 조명 96개를 설치했습니다. 일반형 86개는 하향 Spot Light, 쌍등형 5개는 각각 Point Light 2개를 사용합니다. 등 표면에만 별도 발광 도형을 추가했으며 원래 공용 머티리얼은 유지합니다.

오토바이에는 `Bike` 모델 하위 `Yun Motorcycle Headlight`를 추가했습니다. 원본 `Headlight` 서브메시를 복제한 렌즈만 발광시키고, `Yun Motorcycle Beam` Spot Light가 앞길을 비춥니다. 빛은 모델의 이동·회전·기울기를 그대로 따릅니다. 원본 FBX 머티리얼 및 Bike 1 프리팹은 수정하지 않습니다. 빛의 초기값은 강도 35, 범위 24, 외곽각 65도/중심각 35도, 아래로 8도이며 벽을 관통하는 빛을 줄이기 위해 이 조명 하나에만 Hard Shadow를 사용합니다. 탑승 여부나 연료량은 점등 조건에 포함하지 않습니다.

조명은 따뜻한 색, 실시간 조명, 그림자 없음입니다. Spot 강도 65/범위 13, Point 강도 12/범위 9를 초기 테스트값으로 사용합니다. 모든 조명은 `Yun Streetlight` 이름의 자식 오브젝트이며 개별 Inspector에서 조정할 수 있습니다. 맵 전체의 실제 플레이 프레임 성능은 별도 측정하지 않았습니다.

## 직접 확인

1. YunCity를 열고 Play합니다.
2. PC에서 영업을 시작합니다. 06시에 점등되고 09시에 소등되며 16시에 다시 점등됩니다.
3. 빠르게 확인하려면 영업을 시작한 뒤 Play 중 `Time > DayNightCycle > Current Time`을 6, 8.99, 9, 15.99, 16으로 변경합니다. 6·8.99·16에서 켜지고 9·15.99에서 꺼지는지 확인합니다. Play 진입 시 기존 코드가 시간을 06:00으로 초기화하므로 실행 후 변경해야 합니다.
4. JJinCity에도 같은 기능이 적용되어 있습니다. 씬 간 조명 오브젝트는 별도이므로 이후 밝기/배치 변경은 각 씬에 개별 반영합니다.

## 검증 도구

`YunCityStreetlightVerifier.RunBatch`: 저장된 조명/시간 참조, 16시 경계·밤·다음 날·비활성화 상태, 원본 씬 불변 검사 및 야간 점등 전후 렌더링. 그래픽을 켠 Unity 배치 에디터에서 실행합니다. 렌더링은 점등 차이를 보기 위해 주변광을 고정하며 변경값을 씬에 저장하지 않습니다.

`YunCityStreetlightVerifier.RunPlayBatch`: 실제 Play Mode의 LateUpdate, OnEnable/OnDisable, 시간 초기화 후 재점등, 기존 영업 시간 Update의 16시 통과를 검사합니다. 사용자의 실제 발주 재고가 입고되는 부작용을 막기 위해 검사 중 DayNightCycle의 자동 Start/Update는 비활성화하고 시간 진행 함수만 수동 호출합니다. 테스트 씬을 다시 저장하지 않습니다.

`YunCityStreetlightSetup.BuildBatch`는 최초 복사/배치 도구이며 기존 YunCity가 있으면 덮어쓰지 않고 중단합니다. `TuneBatch`는 YunCity의 추가 조명 밝기만 초기 테스트값으로 맞춥니다.

`YunMotorcycleLightSetup.BuildBatch`는 YunCity에만 전조등을 설치하며 중복 설치를 차단합니다. `YunMotorcycleLightVerifier.RunPlayBatch`는 조명 97개의 시간 경계, 실제 마감/마감 취소 함수, 아침 초기화, 영업 종료 상태, 전조등만 비활성화/복구, 오토바이 이동·회전·기울기 추종을 검사합니다. `RenderBatch`는 전조등 점등 전후 화면을 저장하며 씬에는 저장하지 않습니다.

`JJinCityLightingMigration.RunBatch`는 부모 계층과 메시를 모두 확인한 뒤 조명/발광 표면만 복사하고 JJinCity의 DayNightCycle로 재연결합니다. 이미 이관되어 있으면 중복 생성을 거부합니다. `YunMotorcycleLightVerifier.RunJJinPlayBatch`는 JJinCity에서 조명 동작과 기존 Canvas 재료 텍스트 연결, PC 재개방 시 발주 묶음 수 초기화도 검사합니다.
