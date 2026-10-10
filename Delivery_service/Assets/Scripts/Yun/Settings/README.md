# 설정 패널

JJinCity와 YunCity의 기존 HUD Canvas 아래 `Yun Settings/Settings Panel`에 저장되어 있습니다.
`Settings Panel`은 기본 비활성 상태이며, Hierarchy 체크박스나 `GameObject.SetActive`로 켜고 끌 수 있습니다.
Play 중 Canvas나 버튼을 생성하지 않습니다. `Yun Settings` 부모는 켜 두어야 숨겨진 동안에도 저장된 볼륨이 적용됩니다.

- 배경음: AudioManager의 Bgm 볼륨을 0~100%로 조절합니다. 음원이 없어도 값이 저장되고 이후 Bgm으로 재생할 음악에 적용됩니다.
- 효과음: Sfx, Ui, Ambience 볼륨을 함께 조절합니다. AudioManager 밖의 소리는 부모의 GameAudioSettings > Scene Effects 목록에 AudioSource를 지정하면 원래 볼륨에 비례해 조절됩니다. 현재 WeatherManager의 rainAudioSource는 두 씬 모두 미지정 상태입니다.
- 닫기: 같은 Settings Panel 오브젝트를 비활성화합니다.
- 로비로 돌아가기: 로비 미지정 시 안내만 표시합니다. 추후 로비를 Build Profiles의 Scene List에 넣고 Settings Panel의 GameSettingsPanel > Lobby Scene Path에 `Assets/Scenes/Lobby.unity` 같은 경로를 지정하면 됩니다.
- 게임 종료: 빌드에서는 Application.Quit, Unity 에디터에서는 Play 모드 종료입니다.

슬라이더 및 버튼 콜백은 Inspector의 On Value Changed / On Click에 저장되어 있습니다.
텍스트, 색상, 배치는 Play하지 않고 각 오브젝트의 Inspector에서 편집합니다. 새 UI 이미지 파일은 사용하지 않습니다.
설정이 열리면 기존 PC/정산 UI와 공유하는 입력 잠금으로 커서와 플레이어 이동·시점 조작을 관리합니다. 게임 시간은 정지하지 않습니다.
다른 창이 없을 때 ESC로 설정을 열고, 설정이 열려 있으면 ESC로 닫습니다. PC·정산·버튼/슬라이더/입력 필드가 있는 다른 UI가 표시 중이면 열지 않습니다. PC를 ESC로 닫는 프레임도 차단합니다. 시간·돈·재료·미니맵 등 표시용 HUD는 제외합니다. 버튼 없는 팝업을 추가하면 Yun Settings의 GameAudioSettings > Additional Blocking Panels에 등록하세요. 원하는 버튼에서 Settings Panel의 GameObject.SetActive(true)를 호출하는 것도 가능합니다.
볼륨은 Yun.Settings.BgmVolume / Yun.Settings.SfxVolume PlayerPrefs에 저장되며 패널 닫기, 앱 일시 중단, 종료 시 디스크에 저장합니다.

코드 변경은 Yun 폴더 안에서만 진행했습니다. GameSettingsSetup은 에디터에서 배치하는 도구이며 게임 실행 중에는 호출되지 않습니다.
