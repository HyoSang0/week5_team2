# AGENTS.md

## 0. Git

- 커밋 메세지 제안만 가능
- git cli 수행 금지

## 1. Comment

- Unity `MonoBehaviour`의 Lifecycle 및 Callback 메서드를 제외한 모든 메서드의 상단에 XML `summary` 주석을 작성한다.
- `summary`에는 다음 내용을 포함한다.
  - 메서드가 수행하는 동작
  - 메서드가 사용하는 주요 입력값
  - 메서드가 반환하는 값 또는 변경하는 주요 상태
- 내부 주석은 짧은 평문 문장으로 작성하며, 2~3문장을 초과하지 않는다.
- 내부 주석에는 이모지와 Markdown 문법을 사용하지 않는다.

```csharp
/// <summary>
/// 이동 입력을 받아 현재 속도를 목표 속도로 변경한다.
/// moveInput을 사용하며, 변경된 속도를 _velocity에 저장한다.
/// </summary>
private void ProcessMovement(Vector2 moveInput)
{
    Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

    // 지면 이동에서는 입력 방향을 즉시 속도에 반영하지 않고 가속도로 처리한다.
    _velocity = Vector3.MoveTowards(
        _velocity,
        direction * _moveSpeed,
        _moveAcceleration * Time.fixedDeltaTime);
}
```

---

## 2. Field

### Inspector 설정값

- Inspector에 노출해야 하는 값만 `[SerializeField]`를 사용한다.
- `[SerializeField] public`은 사용하지 않는다.
- `[SerializeField]`가 필요하지 않은 필드는 `private`으로 선언한다.

### 외부 공개 상태

- 클래스 외부에서 값을 참조해야할 경우에만 `public` 프로퍼티를 사용한다.

---

## 3. Field 순서

### 역할 별 1차 분류

필드는 역할에 따라 [Header("")] attribute를 사용하여 그룹화한다.

그룹은 다음 순서로 배치한다.

1. `const`
2. `static`
3. `[Header("")]`
4. ...

### 접근 제한자 별 2차 분류

각 [Header("")] 그룹 내부의 필드는 다음 순서로 배치한다.

1. `[SerializeField] private`
2. `private/protected`
3. `public`

---

## 4. Access Modifier

### Field

- 모든 필드는 접근 제한자를 명시한다.

### Method

- 모든 메서드는 접근 제한자를 명시한다.
- 단, Unity Lifecycle, Callback 메서드는 접근 제한자를 생략한다.
- 단, Unity Lifecycle, Callback 메서드에 virtual, override, abstract 등 Method Modifier을 사용하는 경우에는 접근 제한자를 명시한다.

---

## 5. Using

`using`은 다음 순서로 작성한다.

1. `System`
2. `UnityEngine`
3. Unity 패키지
4. 외부 패키지
5. 프로젝트 namespace

각 그룹 사이에는 한 줄을 넣는다.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;

using Cinemachine;

using Game.Player;
using Game.System;
```

- 사용하지 않는 `using`은 제거한다.
- 같은 그룹 안에서는 알파벳 순서로 정렬한다.

---

## 6. Naming

- private 필드: `_camelCase`
- public 프로퍼티: `PascalCase`
- 메서드: `PascalCase`
- 지역 변수: `camelCase`
- 상수: `SCREAMING_SNAKE_CASE`
- enum 타입: `PascalCase`
- enum 값: `PascalCase`

---

## 7. Scope

- 요청받은 기능과 직접 관련된 코드만 수정한다.
- 요청받지 않은 클래스, 메서드, 변수의 이름을 변경하지 않는다.
- 요청받지 않은 리팩터링을 하지 않는다.
- 기존 동작을 변경하는 코드는 요청된 경우에만 수정한다.
- 테스트 코드 작성 금지

## 8. Code Structure

- Unity Lifecycle 순서와 Inspector 설정을 고려하여 불필요한 `null` check를 추가하지 않는다.

---

## 9. Unity CLI

실행 중인 Editor는 Unity CLI(`com.unity.pipeline` 패키지, 127.0.0.1:7800)로 조회·검증한다.

### 명령

- 형식: `unity cmd <command> --no-banner --json`
- 상태 확인: `editor_status`, `list_open_scenes`(isDirty 확인), `unity pipeline list`(서버 연결 확인)
- 컴파일 검증: `recompile` 실행 후 `recompile_status`가 `completed`가 될 때까지 조회하고 `compilationFailed`, `errors`를 확인한다.
- 콘솔: `console`에는 이전 에러도 남아 있으므로 `timestampUtc`로 최신 항목만 판단한다.
- 조회: `eval`(조회용 코드만), `get_*_settings`, `get_scene_hierarchy`, `find_assets`, `get_serialized_fields`, `package_list`
- 프리팹·에셋 생성: `eval` 또는 에디터 스크립트에서 `PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset` → `UnloadPrefabContents`를 사용한다. 새 오브젝트가 필요하면 `EditorSceneManager.NewPreviewScene()` 안에서 만들고 닫는다.

### 금지

- 요청 없이 Play 모드 진입, `open_scene`, `save_scene`, 씬(`.unity`) 수정을 하지 않는다. Editor는 사용자가 작업 중이다.
- 런타임 동작 확인은 사용자가 Play로 한다.

### 주의

- Git Bash에서는 Windows 경로를 슬래시로 쓴다(`C:/Gits/week5_team2`). 백슬래시는 이스케이프되어 경로가 깨진다.
- 모든 명령이 `Main thread operation timed out`이면 Editor에 모달 창이 떠 있는지 먼저 확인한다(예: git pull 후 씬 외부 변경 알림). 창 선택은 사용자에게 맡긴다.

---

## 10. Orca Orchestration

작업은 Orca 오케스트레이션과 pi 워커로 진행한다.

### 워커 배치

- 담당 파일이 겹쳐 충돌 위험이 있으면 워커 1개로 순차 진행한다.
- 담당 파일이 겹치지 않으면 병렬로 진행한다.
- 사용자가 모델을 지정하면 그 모델만 사용한다.

### 실행 순서

1. `orca skills get orchestration`으로 현재 버전 가이드를 확인한다.
2. `orca orchestration run-create --objective "<목표>"`
3. `orca terminal create --worktree path:C:/Gits/week5_team2 --title <이름> --command "pi --approve --model <모델>"` 후 `orca terminal wait --for tui-idle`
4. `orca orchestration worker-start --run <run> --terminal <handle> --worktree path:C:/Gits/week5_team2 --spec "<지시서>"`
5. `orca orchestration check --run <run> --wait --types "worker_done,escalation,question"`로 대기하고, 처리한 delivery는 `--ack`한다.
6. 완료 보고를 코드와 Editor 측정으로 검증한 뒤 `worker-release --dispatch <id>`, `orca terminal close --terminal <handle>`.

- 진행 중 추가 지시: `orca orchestration send --to dispatch:<id>`
- 워커 질문·에스컬레이션 답변: `orca orchestration reply --id <message_id>`

### 모델

- `dgx-spark/qwen3.8-flash-next`: 첫 호출은 모델 로드로 2분 이상 걸릴 수 있다.
- `zai/glm-5.3-flash`: OpenRouter가 아닌 zai provider로 사용한다.

### 지시서

- 담당 파일 목록(그 외 수정 금지), 다른 워커가 쓰는 공개 API 시그니처, 수용 기준을 명시한다.
- 이 문서(AGENTS.md)의 코드 규칙과 git 금지를 따르게 한다.
- 워커 보고서의 이슈를 수정 대상으로 올리기 전에 사용자가 이미 내린 결정과 대조한다.
- 워커가 컴파일 성공을 보고해도 코디네이터가 `recompile_status`로 다시 확인한다.