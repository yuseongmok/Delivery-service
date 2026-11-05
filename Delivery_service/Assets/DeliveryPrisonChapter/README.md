# 다음 챕터 감옥 씬

## 열기
`Scenes/PrisonChapter.unity`를 연다. 기존 씬 및 Build Settings는 변경하지 않았다.
Synty와 Fonts는 프로젝트에 이미 있는 원본 파일을 참조한다. 이 폴더 전체와 .meta를 함께 커밋한다.

## 공간
- 약 110 × 92m의 교정시설 부지, 4개 감시탑과 외벽
- 중앙 2층 수감동: 24개 감방, 이층침대, 열린 철창문, 양끝 계단과 상층 난간
- 동쪽 식당/주방: 식탁, 조리대, 식재료와 배식 공간
- 서쪽 간수실/면회실/의무실
- 북쪽 운동장: 농구 코트, 휴식 벤치, 운동 기구
- 북동쪽 작업장/창고
- 주방 뒤 정비 통로와 외곽 반출 지점

모든 정적 공간과 소품은 씬에 저장되어 Play 전에도 편집할 수 있다.
Synty 메시와 텍스처를 이용하며, 재질은 이 폴더에 전용 복사본을 만들었다. Synty 원본 재질을 수정하지 않는다.

## 둘러보기
Play → WASD 이동 / 마우스 시점 / Shift 달리기 / Space 점프 / Esc 커서 해제 / 좌클릭 다시 시점 조작.
`PreviewPlayer`는 이 씬 전용이며 기존 플레이어 스크립트를 건드리지 않는다.
실제 챕터 플레이어를 연결할 때 PreviewPlayer를 비활성화해 카메라가 중복되지 않게 한다.

## 향후 미션 연결 위치
Hierarchy에서 다음 이름을 검색한다.
- ChapterStart, ArrivalSpawn: 챕터 도착
- PlayerCell, MingiCell: 플레이어/민기 감방
- CookingStation, ServingStation: 조리/배식
- GuardOffice, InfirmarySpawn: 간수실/의무실
- YardMeeting, WorkStation: 운동장 대화/작업
- MaintenanceEntry, EscapeExit: 탈옥 동선
- GuardPatrol_Cellblock_01 / 02: 수감동 순찰 기준점

## 현재 범위
공간 제작과 독립 둘러보기 단계다. 3일차 진행, 조리 상호작용, NPC/간수 AI, 문 잠금, 탈옥 성공 조건, NavMesh 및 기존 챕터 전환은 아직 연결하지 않았다.
기존 씬·공유 프리팹·프로젝트 설정에 자동으로 내용을 추가하는 스크립트는 없다.
