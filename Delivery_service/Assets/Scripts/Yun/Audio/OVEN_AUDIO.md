# 오븐 가동음

- 음원: 사용자가 제공한 `freesound_community-oven-01-29740.mp3`를 편집 없이 `Clips/oven-operation.mp3`로 복사했습니다.
- 설정 위치: `Assets/Scripts/Yun/SoundLibrary.asset`의 **피자/오븐 가동 반복음 (OvenLoop, 306)**.
- 초기 설정: Sfx, 볼륨 0.6, 피치 1, 반복 재생, 3D, 최소 거리 1.5m / 최대 거리 12m. 효과음 설정 슬라이더도 적용됩니다.
- 굽기 시작 시 오븐 위치에서 재생하고, 굽기 완료·오븐 비활성화·파괴 시 정지합니다. ESC 일시정지 중에는 소리도 정지하며, 돌아오면 굽는 중인 오븐만 클립 처음부터 재생합니다.
- 씬 로드 시 기존 Oven에 Yun의 GameplayAudio 공통 연결 컴포넌트를 자동 부착합니다. UI나 시각 오브젝트는 생성하지 않습니다. 기존 AudioManager와 SoundLibrary를 사용하며 별도 AudioSource를 만들지 않습니다.
- Min의 Oven 코드는 수정하지 않습니다. 공개 상태/이벤트가 없어 `isBaking`, `currentPizza`를 읽기만 합니다. 필드가 변경되면 연결부를 갱신해야 하며, link.xml로 빌드에서 필드를 보존합니다.
- 볼륨/거리/음원은 라이브러리에서 조정하세요. 원본의 시작·끝 부분과 반복 연결감은 아직 편집하지 않았습니다.
- 배치 검증 진입점: `DeliveryService.Yun.Editor.OvenAudioVerifier.RunBatch`. 빈 Play 씬에서 실제 Oven.Interact를 사용하며 게임 저장은 건드리지 않습니다.
