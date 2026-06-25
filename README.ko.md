# AI Bridge for Unity

[English](README.md) | [简体中文](README.zh-CN.md) | **한국어**

**AI와 대화하며 Unity 게임을 만드세요.** AI Bridge는 로컬 AI 어시스턴트(예:
[Claude Code](https://claude.com/claude-code))를 열려 있는 Unity 에디터에 연결합니다 — 그래서 AI가
씬을 보고, 오브젝트를 생성·수정하고, UI를 배치하고, 여러분의 아트를 사용하고, 애니메이션을 만들고,
게임 로직을 작성하고, 심지어 게임을 실행하는 것까지 모두 채팅에서 처리합니다.
서버도, 포트도, MCP 설정도 필요 없습니다 — 패키지만 설치하면 됩니다.

> 상태: 초기 단계이고 계속 발전 중이지만, 이미 충분히 쓸 만합니다. **Unity 6**의 **2D 모바일**
> 워크플로를 위해 만들어졌습니다.

## 할 수 있는 것

- 👀 **씬 보기** — 데이터로도, 실제 스크린샷으로도(UI 포함, 그리고 게임 실행 중 화면까지).
- 🐛 **콘솔 읽기** — 로그·경고·컴파일 오류를 읽어, 여러분이 읽어 주지 않아도 스스로 디버깅하고 자신의 실수를 고칩니다.
- 🧱 **만들기** — GameObject, UGUI(캔버스/버튼/텍스트), 그리드, 프리팹.
- ✏️ **프리팹 비파괴 편집** — 한 번에 요소/필드 하나씩(텍스트·스프라이트·색상·레이아웃·복제) 바꾸고 prefab 전체를 다시 쓰지 않아, 손수 맞춘 레이아웃이 절대 덮어쓰이지 않습니다.
- 🎨 **아트 사용** — 이미지를 스프라이트로 임포트해 오브젝트에 붙입니다.
- 🎞️ **애니메이션** — 키프레임 애니메이션(이동/스케일/색상), 그리고 선택적 **Spine** 스켈레탈 애니메이션.
- 🔊 **사운드 & ✨ 이펙트** — 오디오 소스와 파티클 이펙트를 추가합니다.
- 🧠 **게임 로직 작성** — C#을 작성하고, 직접 컴파일하고, 자신의 오류를 읽습니다.
- ▶️ **게임 실행** — Play 모드에 진입해 지켜봅니다.
- 👉 **"이것 / 여기" 이해** — Unity에서 오브젝트나 한 지점을 클릭하면, AI가 정확히 무엇을 가리키는지 압니다.

## 요구 사항

- **Unity 6**(6000.x).
- 프로젝트의 파일을 읽고 쓸 수 있는 **로컬 AI 에이전트** — 예: **Claude Code**.
  Unity와 같은 컴퓨터에서 실행되어야 합니다(둘은 프로젝트 안의 폴더를 통해 통신합니다).

## 동작 방식

```
당신 + 로컬 AI 에이전트(예: Claude Code)
        │  에이전트가 프로젝트 안의 폴더(.aibridge)를 읽고 씁니다
        ▼
AI Bridge 패키지  ──  Unity 에디터 내부에서 실행되며 요청을 처리합니다
        ▼
당신의 Unity 프로젝트(실제로 열려 있는 에디터)
```

두 부분으로 구성됩니다: **(1)** 프로젝트에 설치되어 에디터와 함께 자동 시작되는 이 Unity 패키지,
그리고 **(2)** 동봉된 skill로부터 조작 방법을 배우는 로컬 AI 에이전트. 백그라운드 프로세스도,
네트워크도 없습니다 — 브리지는 그저 폴더 하나를 지켜볼 뿐입니다.

## 설치

**1. Unity 패키지 추가.** Unity에서: **Window → Package Manager → + → Add package from git URL**, 붙여넣기:

```text
https://github.com/hi5jeff/Aibridge4Unity.git?path=/com.aibridge.unity
```

프로젝트를 열면 Console에 `[AIBridge] Bridge ready …`가 보이고 **Tools ▸ AI Bridge** 메뉴가 생깁니다.

**2. AI 에이전트 설정 — 원클릭.** Unity에서: **Tools ▸ AI Bridge ▸ Configure Claude Code**.
이 메뉴는 `unity-bridge` skill을 `.claude/skills/unity-bridge/SKILL.md`에 설치하고, **또한** 프로젝트의
`CLAUDE.md`에 짧은 상시 안내문을 추가합니다. 그래서 Claude Code가 스크린샷을 찍거나 MCP 서버를
찾는 대신, 브리지를 통해 안정적으로 Unity를 조작하게 됩니다.

그런 다음 프로젝트에서 **Claude Code를 재시작**하세요(skill과 `CLAUDE.md`는 세션 시작 시 읽힙니다).
이제 그냥 요청하면 됩니다.

> **에이전트가 자동으로 인식하지 못하면**, 한 번만 이렇게 말하세요:
> *"`.claude/skills/unity-bridge/SKILL.md`를 읽고 그걸 사용해 Unity를 조작해줘."*
> 그 파일에서 파일 채널을 배운 뒤로는 해당 세션 내내 동작합니다. (Claude Desktop과 Claude Code 모두 동일합니다.)

## 새 버전으로 업데이트

Unity의 Package Manager는 git 패키지를 **설치 시점의 commit에 고정**합니다(`Packages/packages-lock.json`에 기록). 그래서 이 저장소에 새 커밋이 올라와도 **자동으로 업데이트되지 않습니다**. 최신 버전을 받으려면:

- **가장 쉬움(Package Manager)**: Window → Package Manager → **In Project** → *AI Bridge for Unity* → **Update** 버튼이 보이면 클릭하세요. 없으면 **Remove** 후 **Add package from git URL**로 다시 추가하면 최신 commit으로 다시 받습니다. `BridgeConfig` 에셋과 `.aibridge` 채널은 프로젝트에 있으므로 사라지지 않습니다.
- **버전 고정(권장)**: git URL 뒤에 태그를 붙여 예측 가능하게 업데이트하세요:
  ```text
  https://github.com/hi5jeff/Aibridge4Unity.git?path=/com.aibridge.unity#v0.26.0
  ```
  `#v…` 태그를 올리면 새 버전으로 이동합니다. 버전 목록은 저장소 **Releases**/태그를, 현재 버전은 `com.aibridge.unity/package.json`과 `CHANGELOG.md`를 참고하세요.
- **업데이트 후**, **Tools ▸ AI Bridge ▸ Configure Claude Code**를 한 번 다시 실행해 번들된 `unity-bridge` skill과 새 규칙(`PRESENTATION_RULES.md` 등)을 갱신하세요.

## 사용법 — 예시

평소처럼 AI 에이전트와 대화하면, 에이전트가 대신 Unity를 조작합니다. 예를 들어:

| 당신이 말하면… | AI가 합니다… |
|---|---|
| *"씬에 뭐가 있어?"* | 모든 오브젝트의 위치, 컴포넌트, UI 앵커를 읽습니다 |
| *"게임 화면 보여줘"* | 실제 Game 뷰(UI 포함)를 캡처해 살펴봅니다 |
| *"가운데에 파란색 **Play** 버튼 추가해"* | 캔버스 + 버튼 + 라벨을 만들고 앵커를 맞춥니다 |
| *"`hero.png`를 플레이어 스프라이트로 써"* | 이미지를 스프라이트로 임포트해 할당합니다 |
| *"코인이 위아래로 둥실거리게 해"* | 반복 키프레임 애니메이션을 만들어 붙입니다 |
| *"이걸 프리팹으로 만들고 5개 생성해"* | 프리팹을 저장하고 복제본을 인스턴스화합니다 |
| *"`PlayerHealth` 스크립트 추가해서 플레이어에 붙여"* | C#을 작성하고 컴파일하고 붙입니다 |
| *"왜 오류가 나? 고쳐줘."* | Console 오류를 읽고 원인을 찾아 고칩니다 |
| *"코인 획득 사운드 추가해"* / *"여기 스파크 터뜨려"* | AudioSource / 파티클 이펙트를 추가합니다 |
| *"spineboy 캐릭터 생성하고 'walk' 재생해"* | Spine 스켈레톤을 인스턴스화하고 해당 애니메이션을 재생합니다 |
| *"**이걸** 여기로 옮겨"*(오브젝트를 클릭한 뒤 지점을 클릭) | 당신의 선택/지점을 읽어 옮깁니다 |
| *"실행해"* | Play 모드에 진입해 게임을 지켜볼 수 있게 합니다 |

### 설명 대신 '가리키기'

**Tools ▸ AI Bridge ▸ AI Reference**를 열어 *가리키세요*: 오브젝트를 선택(또는 Scene 뷰에서 한 지점을
클릭)하고 핀으로 고정하면 — AI가 당신이 의도한 것을 정확히 읽어, 좌표를 열 번씩 설명할 필요가 없습니다.

## 설정

조정 가능한 모든 항목은 `BridgeConfig` 에셋에 있습니다(채널 폴더, 언어 `en`/`zh-CN`, 켜기/끄기…).
**Tools ▸ AI Bridge ▸ Create Config Asset**로 하나 만들거나, 그냥 기본값을 사용하세요.

## 라이선스

[MIT](LICENSE).
