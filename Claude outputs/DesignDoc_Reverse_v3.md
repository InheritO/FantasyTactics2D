# 타일 기반 턴제 전략 게임 — 역기획서 (v3)

> 이 문서는 Cowork 세션에서 진행하던 작업을 일반 채팅으로 옮기기 위해, v2 이후 진행된 작업 내역을 반영해 다시 정리한 기획서입니다. v2 대비 달라진 점: 반격 시스템 UI 완성(방어태세), 재장전/이동력/막기 관련 버그 수정, 행동 패널 UX(자동 열기+토글), 코드 리뷰로 발견된 이슈 정리, 협업 작업 방식 메모가 추가/반영되었습니다.

---

## 1. 게임 개요

사각형 그리드 위에서 진행되는 턴제 전략 게임. 플레이어는 인간/엘프/드워프/오크 중 한 종족을 선택하고, 포인트 한도 안에서 유닛 수와 장비를 편성해 상대와 전투를 벌인다.

### 게임 모드
- **대전(Skirmish)** — 현재 구현 대상. 한 판의 전투에 집중.
- **캠페인(Campaign)** — 추후 개발 예정. 육성, 고용비/유지비, 장비 인벤토리, 부위별 방어구, 소모품 등 RPG 요소 추가 예정.

### 개발 환경
- Unity 6 (6000.x)
- 입력: 새 Input System (`GameControls`) — 액션 맵 `Gameplay`: `Click`, `Point`, `Move`, `Zoom`, `EndTurn`, `Cancel`, **`ToggleActions`(v3 신규)**
- UI: Unity UI(uGUI, Canvas/Button/TextMeshPro)
- 프로젝트 경로: `C:\Projects\Fantasy_Tactics\Fantasy_Tactics_2D`

---

## 2. 세력(Faction)과 종족(Race)의 분리

- **세력**: 편/색상/AI 여부 등 "고정 정체성" (`FactionData`)
- **종족**: 기본 스탯, 장비 접근권 (`RaceData`)
- **캠페인 모드**: 세력이 고정된 종족을 가짐 (`FactionData.race`)
- **대전 모드**: 매 판 종족을 다르게 선택 가능. `SkirmishParticipant`(세력+종족+로스터 묶음)라는 별도 개념으로 처리하며 캠페인 구조와 간섭하지 않음

---

## 3. UnitBase 아키텍처

`UnitBase`가 비대해져(약 600줄), 책임별로 분리했다. **위임 패턴**을 사용해 `UnitBase`의 public 인터페이스는 그대로 유지하면서 내부 구현만 분리했다.

```
UnitBase (MonoBehaviour) — 이동/전투/행동력 관리 핵심 로직
├── UnitEquipmentState (순수 클래스) — 무기/방패/방어구 슬롯, 파괴 상태, 장전 상태
└── UnitStatusEffectTracker (순수 클래스) — 출혈/기절 등 상태이상 목록 관리
```

`UnitBase`는 `#region`으로 섹션 구분: Events / Identity / Capabilities & Turn State / Equipment / **Universal Actions(v3 신규)** / Status Effects / Calculated Stats / Racial Traits / Action Availability / Lifecycle / Faction·Race Assignment / Movement / Action Point Consumption / Combat / Turn State.

**v3 추가**: `universalActions[]` 필드(+`UniversalActions` 프로퍼티) 추가 — 무기와 무관하게 항상 후보가 되는 행동(방어태세 등)을 유닛 프리팹마다 구성할 수 있음.

---

## 4. 유닛 스탯 (8종)

| 스탯 | 역할 |
|---|---|
| 체력 (MaxHealth) | 최대 생명력 |
| 이동력 (MoveRange) | 이동 가능 칸 수 |
| 민첩 (Agility) | 명중/회피 판정 전담 (공격자 민첩 vs 방어자 민첩) |
| 근접 기술 (MeleeSkill) | 근접 공격 시 막기 무력화 판정에 사용 |
| 원거리 기술 (RangedSkill) | 원거리 공격 시 막기 무력화 판정에 사용 |
| 힘 (Strength) | 힘 기반 무기의 데미지에 영향 |
| 맷집 (Constitution) | 데미지 감소(항상 적용), 상태이상 저항 판정 |
| 방어 기술 (DefenseSkill) | 방패로 막기 판정 시 사용 |

---

## 5. 전투 판정 구조 (5단계)

```
1단계 - 명중/회피: 공격자 Agility vs 방어자 Agility
        70% + (공격자민첩 - 방어자민첩) × 5%p + 무기 명중보정, 범위 5~95%

2단계 - 막기 (방패 보유 & 미파괴 시만 시도):
        공격자 (Melee/RangedSkill) vs 방어자 (DefenseSkill + 방패 blockSkillBonus)
        20% + (방어자 막기스킬 - 공격자 기술) × 2%p
        [v3 신규] 방어자가 방어태세(IsBraced) 중이면 +15%p(BracedBlockBonus) 추가
        범위 5~60%
        성공 시 데미지/치명타/상태이상 완전 무효(Blocked)

3단계 - 데미지:
        raw = 무기위력(기본+보너스) + 유효힘(힘기반 무기만, 근접 시 종족특성 반영)
        방어력 = 맷집 + max(0, [방어구+방패 보너스] - 관통력)
        기본 데미지 = max(1, raw - 방어력)

3-1단계 - 치명타 (막기 실패 시):
        확률 = max(무기기본치명타 + 공격보너스, 최소치 5%) — 방어자 저항 없음, 무기 고유 확률
        성공 시 데미지 × 1.5배

4단계 - 상태이상 (명중 + 막기 실패 시만 시도):
        40% + (disruption - 방어자 맷집) × 2%p, 범위 5~80%
        성공 시: 출혈/기절(임시, 갱신형) 또는 방어구파괴(즉시 영구, 방패 우선 파괴)
```

### 방어구 관통력의 원칙
관통력은 방어구+방패 보너스에서만 차감됨. **맷집은 관통 불가**.

### 치명타 설계 결정 과정 (참고)
처음엔 "disruption처럼 방어자 맷집과 대결"하는 방식을 검토했으나, **"맷집이 데미지감소+상태이상저항+치명타저항까지 3개 역할을 겸하는 것"이 과도하다고 판단**하여 **방어자 저항 없이 순수 무기 확률**로 단순화. 모든 무기가 최소 5%(`MinimumCritRating`)는 갖도록 `CombatResolver`에서 하한선을 강제.

### [v3 신규] 방어태세와 막기의 연동
`CombatResolver.TryResolveBlock()`에 `BracedBlockBonus`(상수, 기본 15%p) 추가. 방어자가 `IsBraced` 상태면 막기 확률 계산에 가산됨. 방패가 없으면 애초에 막기 판정 자체가 시도되지 않으므로, 자연스럽게 "방패를 든 유닛의 방어태세"에만 효과가 붙는다. 수치는 상수라 바로 튜닝 가능.

---

## 6. 행동력(Action Point) 시스템

```
HasMoved (bool) — 이동은 항상 최대 1회
ActionsUsedThisTurn (int) / MaxActionsPerTurn — 공격, 재장전, 방어태세 등
    "행동" 전체가 공유하는 통합 자원
```

- `ConsumeAction()`: 모든 "행동"(공격/재장전/방어태세)이 공통으로 호출하는 함수. 행동력 소모 + `HasMoved = true`(행동하면 이동도 봉인) + 소진 시 `OnActionsExhausted` 이벤트
- `CanAttack`(태생적 공격 가능 여부, 포탑 등에서 false 고정)은 **공격에만 적용**, 재장전/방어태세는 `HasActionsRemaining`만 체크
- `MaxActionsPerTurn`: 종족 특성(`ExtraAttackTrait.ModifyMaxActions`)으로 증가 가능. 엘프가 이 특성을 보유해 턴당 2회 행동 가능.
- **[v3]** `EnterBracedStance()`에 `IsStunned` 체크 추가 — 기존엔 `TryAttack`/`TryMoveTo`에는 있었는데 여기만 빠져 있던 버그.

---

## 7. UnitAction 계층

`WeaponAttack`(공격)과 "공격이 아닌 행동"(재장전, 방어태세 등)의 공통 부모.

```
UnitAction (추상 SO)
├── IsAvailable(actor, target) — 지금 이 행동을 선택할 수 있는지
├── Execute(actor, target) — 실행
│
├── WeaponAttack — 공격류 (베기, 찌르기 등)
├── ReloadAction — 재장전 (석궁 전용, WeaponData.reloadAction으로 참조)
└── StandGroundAction — "방어태세" 진입 [v3: 완성됨]
```

`AttackOptionProvider`가 `UnitAction` 후보 목록(무기의 attacks[] + 재장전 필요 시 reloadAction + **[v3] UnitBase.UniversalActions[]**)을 만들고, 각 `IsAvailable()`로 걸러서 UI 버튼으로 표시. 재장전이 필요한데 장전 안 된 무기는 `WeaponAttack.IsAvailable()`이 false를 반환해 공격 옵션 자체가 안 뜨고, 대신 `ReloadAction`만 노출됨.

### [v3] StandGroundAction (완성)
```csharp
[CreateAssetMenu(fileName = "Steady", menuName = "Strategy/Actions/Stand Ground (Steady)")]
public class StandGroundAction : UnitAction
{
    // Reset()에서 actionName 기본값 "방어태세" 자동 설정
    // IsAvailable: !actor.IsBraced && actor.HasActionsRemaining
    // Execute: actor.EnterBracedStance()
}
```
- `UnitBase.universalActions[]`에 등록해서 사용 (유닛 프리팹의 Inspector에서 구성).
- target 없이도(자기 자신 대상) 후보가 되므로, 적을 클릭하지 않은 상태에서도 선택 가능.
- 대상이 있을 때(적을 클릭해서 연 패널)도 후보에 포함되므로, 다른 공격 옵션과 동일하게 목록에 같이 나타남.

---

## 8. 반격 시스템 [v3: 완료]

```csharp
IsBraced (bool) — 방어태세 상태, ResetTurnState()에서 매턴 초기화
EnterBracedStance() — 태세 진입 + ConsumeAction() [v3: IsStunned 체크 추가]
TakeDamage(int amount, UnitBase attacker = null) — attacker가 있고 IsBraced && 사거리 안이면 자동 반격
PerformCounterattack(attacker) — CombatResolver.Resolve() 재사용, 반격은 attacker 인자 생략(재귀 반격 방지)
    [v3] 재장전 필요 무기가 장전 안 됐으면 반격 자체를 하지 않도록 가드 추가
    [v3] 반격도 발사인 건 마찬가지이므로 ConsumeAmmo() 호출 추가 (탄약 없는 석궁 무한 반격 버그 수정)
```

- 출혈 등 "공격자 없는" 데미지는 `attacker: null`로 호출되어 반격 발동 안 함
- 방어태세는 **자동 발동이 아니라 사전에 선택해야 하는 전략적 행동**으로 설계 (파이어엠블렘류의 무조건 반격과 차별화)
- **[v3] 방어태세는 막기 확률도 함께 올려준다** (5장 참고, `BracedBlockBonus`)
- **[v3] UI 완성**: 16장 참고 — 유닛 선택 시 자동으로 방어태세 옵션이 뜨고, R키/토글 버튼으로 패널을 껐다 켤 수 있음

---

## 9. 상태이상 시스템

| 종류 | 성격 | 처리 방식 |
|---|---|---|
| 출혈 (Bleed) | 지속 데미지 | `UnitStatusEffectTracker` 리스트, 턴 시작 시 데미지+지속시간 감소 |
| 기절 (Stun) | 이동/공격 불가 | 동일 리스트, `IsStunned` 플래그로 행동 차단 |
| 방어구파괴 (ArmorBreak) | 영구 디버프 | 리스트 밖. `ShieldBroken`/`ArmorBroken` 플래그, 방패 우선 파괴 |

**갱신 규칙**: 같은 상태이상 재적용 시 지속시간은 최신값, 데미지(Magnitude)는 기존/신규 중 큰 쪽 유지. 만료 조건은 `RemainingTurns < 0`.

---

## 10. 종족 특성 (RacialTrait)

`RacialTrait`(SO, abstract) 상속, `RaceData.traits[]`로 복수 조합 가능.

| 특성 클래스 | 훅 | 효과 |
|---|---|---|
| `TerrainAdaptationTrait`⚠️ | `ModifyMoveCost` | 지정 지형 이동비용 할인 (드워프-산) |
| `ExtraAttackTrait` | `ModifyMaxActions` | 턴당 최대 행동 횟수 증가 (엘프) |
| `MeleeStrengthBonusTrait` | `ModifyMeleeStrength` | 근접 공격 시 힘 보너스, 원거리 미적용 (오크) |
| `VersatileTrait` | (없음) | 효과 없음, 명시적 "보정 없음" 표기용 (인간) |

⚠️ **[v3 발견, 미수정]** `Units/Traits/RacialTraits/TerrainAdaptationTrait.cs` 파일의 실제 클래스명은 `TerrainAffinityTrait`. 파일명/기획서와 이름이 다름 (ScriptableObject라 컴파일 문제는 없지만 찾기 어려움 — 정리 필요).

**네이밍 규칙**: 재사용 SO 확장 클래스는 `Data` 접미사 없이(`WeaponAttack`, `RacialTrait`, `UnitAction`), 순수 값 저장 SO는 `[명사]Data`, 인터페이스는 `I[역할]`.

---

## 11. 장비 시스템

### 무기 (WeaponData) — 2단 구조

```
WeaponData (무기 자체)
├── handedness, slotType(MainHand/OffHand 플래그), isRanged, damageScaling, attackRangeOverride
├── basePower, baseArmorPenetration, baseAccuracyBonus, baseCritRating (기본 스펙)
└── attacks[]: WeaponAttack 참조 배열
    requiresReload, reloadAction (선택 사항)

WeaponAttack : UnitAction (공격 방식, 여러 무기가 공유 가능)
├── powerBonus, armorPenetrationBonus, accuracyBonusModifier, critRatingBonus (가산)
└── inflictedEffect, disruption, effectDuration, effectMagnitude (상태이상, 선택)
```

**원칙**: 사거리·근접/원거리·힘반영방식은 무기 레벨 고정. 위력·관통·명중·치명타·상태이상은 공격 방식마다 다를 수 있음.

**양손 vs 한손**: 양손 무기는 한손보다 위력·관통·명중 모두 확실히 우위 (대신 방패/보조무기 불가).

### 방어구(ArmorData) / 방패(ShieldData)
```
ArmorData: defenseBonus, moveRangePenalty, cost
ShieldData: defenseBonus, blockSkillBonus, moveRangePenalty, cost
```
방패는 무기와 별개 타입. `defenseBonus`(항상 적용) + `blockSkillBonus`(막기 판정용) 이중 효과.

**[v3 버그 수정]** `UnitBase.MoveRange` 계산에서 `EquippedShield.moveRangePenalty`가 빠져있던 버그 수정. 기존엔 방어구 페널티만 반영되고 방패 페널티는 무시됐음.

### 슬롯 규칙
양손 무기 장착 시 보조무기/방패 자동 해제. 보조무기(`OffHand` 플래그 무기만)와 방패는 같은 슬롯 경쟁. 보조무기 장착 시 `ExtraAttackAbility`(추가공격, 명중 70% 배율) 자동 부여.

⚠️ **[v3 발견, 미수정]** `ExtraAttackAbility.TryTrigger()`가 명중+데미지만 자체 계산하고 `CombatResolver.Resolve()`의 막기/치명타/상태이상 단계를 타지 않음. 방패 든 상대에게도 무조건 맞고, 도끼를 보조무기로 들어도 방어구파괴가 안 걸림.

---

## 12. 확정된 무기 목록

| 무기 | 손 | 사거리 | 기본(위력/관통/명중/치명) | 공격 | 상태이상/특이사항 |
|---|---|---|---|---|---|
| 양손검 | 양손 | 1 | 7/2/3/- | 베기, 찌르기 | 베기: 출혈 |
| 한손검 | 한손 | 1 | 4/0/0/- | 베기, 찌르기 | 베기: 출혈(약함) |
| 양손도끼 | 양손 | 1 | 9/2/0/- | 가르기 | 방어구파괴 |
| 한손도끼 | 한손 | 1 | 6/2/-3/- | 가르기 | 방어구파괴(약함) |
| 메이스 | 양손 | 1 | 8/2/0/- | 강타 | 기절 |
| 창 | 한손 | 2 | 5/0/0/- | 찌르기 | 없음(둘 다 없음 기조) |
| 활 | 양손 | 2 | 4/0/0/25 | 사격 | 없음, Strength기반+고점(치명타25%) |
| 석궁 | 양손 | 2 | 24/4/10/- | 발사 | 없음, Fixed(저점 보장)+**재장전 필요** |

**컨셉**: 베기=출혈, 찌르기=관통특화, 둔기=강력한 일격+상태이상. 활=고점(치명타 25%), 석궁=저점(항상 일정, 대신 재장전 필요).

**보류**: 엘프-활/드워프-석궁 선호는 종족별 무기 접근 제한으로 해결 예정, 미반영.

---

## 13. 확정된 종족표

| 스탯 | 인간 | 엘프 | 드워프 | 오크 |
|---|---|---|---|---|
| 비용 | 10 | 14 | 14 | 8 |
| 체력 | 40 | 38 | 44 | 42 |
| 이동력 | 6 | 8 | 5 | 6 |
| 민첩 | 20 | 26 | 14 | 15 |
| 근접 기술 | 20 | 24 | 20 | 13 |
| 원거리 기술 | 20 | 28 | 16 | 12 |
| 힘 | 20 | 16 | 22 | 25 |
| 맷집 | 20 | 19 | 24 | 22 |
| 방어 기술 | 20 | 18 | 28 | 15 |
| 종족 특성 | Versatile | ExtraAttack | TerrainAdaptation(산) | MeleeStrengthBonus |

**컨셉**: 인간=기준값, 엘프=빠르고 정확하지만 약함, 드워프=느리지만 최강 방어, 오크=강하지만 부정확한 물량형.
**포인트당 효율**: 인간16.0, 엘프/드워프12.0(의도적 통일), 오크18.0(물량형).

---

## 14. 턴 / 페이즈 / AI

### 페이즈
`BattlePhase`: `Placement` → `Battle` → `Ended` (열거형은 `BattlePhaseManager.cs`에 정의됨 — `Battle/BattlePhase.cs` 파일은 빈 채로 남아있는 정리 대상)

### 턴 관리 (`TurnManager`)
**세력 단위 순환**. `AddFaction`/`RemoveFaction`으로 난입/전멸 지원.

> ⚠️ **알려진 구조적 문제 (미해결)**: 세력 단위로 유닛 전체가 한 번에 행동하는 구조라 "선공 몰살(Alpha Strike)" 위험이 있음. 개별 유닛 턴제 또는 교번 행동으로 바꾸는 게 정석이나 규모가 매우 큼. 착수 여부 미결정.

### AI 편성 (`AIRosterGenerator`)
지정 종족 고정 편성, 무기 필수 장착 규칙. 전략(`AIRosterStrategy`): Standard/MeleeFocus/RangedFocus(80% 편중).

### AI 행동 (`IUnitAIBehavior`, 부대 단위 통일 적용)
| 클래스 | 성향 | 행동 |
|---|---|---|
| `AggressiveMoveTowardEnemy` | Aggressive | 최근접 적 접근 후 공격 |
| `DefensiveHoldPosition` | Defensive | 사거리 내 적에만 반응, 그 외 사수 |
| `KeepDistanceAndShoot` | RangedKiting | 원거리 유닛만 거리유지, 근접유닛은 Defensive로 대체 |

**[v3 확인 및 수정]** 재장전 처리 로직은 AI 세 클래스 모두에 이미 반영돼 있었음(v2 시점의 "재확인 요망" 항목 해소). 다만 `KeepDistanceAndShoot`의 "너무 가까움" 후퇴 분기에서 재장전 체크가 빠져있던 버그를 발견해 수정(재장전 필요 시 쏘지 않고 재장전하도록).

**[v3] 재장전/반격 체크 위치 정리**: `IsLoaded` 체크가 `WeaponAttack.IsAvailable()`(UI용)과 각 AI 클래스에 개별 중복 구현되어 있던 걸, `UnitBase.TryAttack()` 자체에 추가해서 호출 경로(플레이어 UI/AI/반격) 전체에서 한 곳에 강제되도록 정리.

공통 로직은 `AIQueryUtility`로 분리.

---

## 15. 대전(Skirmish) 모드 흐름

```
부대 편성 (RosterPhaseManager + RosterUIController)
  -> 종족/적종족/AI전략/AI성향 선택, 포인트 슬라이더로 총량 조정, 장비 순환 선택, 확정
그리드 배치
  -> PlayerDeploymentController(클릭), EnemyDeploymentController(자동, AIRosterGenerator 결과)
전투 (Battle)
결과 화면 (승/패) -> 부대 편성 복귀 (SkirmishFlowController가 전체 관리)
```

씬 분리 없이 한 씬 안 UI 패널/오브젝트 활성화로 흐름 제어.

---

## 16. UI [v3: 행동 패널 UX 정리]

- 유닛 머리 위 체력바 (`UnitHealthBar`, `OnDamaged` 구독 자동 갱신)
- 전투 시작/턴 종료 버튼, 승패 결과 화면
- `UnitActionVisual`: 행동완료 시 색 어둡게, 데미지 받으면 잠깐 흰색 플래시 후 원래 상태로 복귀
- 부대 편성 화면: 좌(Draft+목록)/우(적 설정+포인트) 좌우 분할

### [v3 신규] 공용 행동 패널(`BattleAttackPanel`) 동작 방식
여러 차례 논의 끝에 다음과 같이 확정:

1. **유닛을 선택하면 자동으로 패널이 열림** (대상 없이, 방어태세/재장전 등 무기 무관 행동만 후보). `UnitSelectionController.RefreshSelectionDisplay()`에서 `attackPanel.Show(selectedUnit, null, ...)` 자동 호출 — 이미 열려 있으면 다시 채우지 않음.
2. **적을 클릭하면** 기존처럼 `ShowAttackOptions(target)`가 target을 채워서 패널을 다시 열고, 무기 공격 옵션이 방어태세/재장전과 함께 표시됨.
3. **패널이 이동 가능 타일을 가릴 수 있어서**, 열림/닫힘을 토글하는 기능 추가:
   - `BattleAttackPanel.IsShown` 프로퍼티 신설.
   - `UnitSelectionController.ToggleActionsPanel()`(구 이름 `OpenActionsForSelectedUnit`) — 열려있으면 Hide, 닫혀있고 아직 행동 가능하면 target 없이 Show.
   - R키(Input Actions의 `ToggleActions`, 구 `OpenActions`에서 개명)와 `BattleUIController`의 토글 버튼(구 "방어태세" 버튼, 현재는 라벨 재검토 가능) 둘 다 `ToggleActionsPanel()`을 호출.
4. 옵션 버튼은 목록 순서대로 자동으로 숫자(1, 2, 3...)가 붙고 숫자키로도 선택 가능 (`BattleAttackPanel`의 `NumberKeys[]`) — 방어태세도 이 규칙을 그대로 따르므로 별도 구현 없이 숫자키 선택 가능.

### [v3 버그 수정] 공격 옵션 버튼이 비활성 상태로 생성되던 문제
- 원인: `AttackOptionButtonPrefab.prefab`의 루트 게임오브젝트가 프리팹 자체에 비활성(Active 꺼짐) 상태로 저장돼 있었음. `Instantiate()` 복제본도 똑같이 비활성으로 시작되는데 `BattleAttackPanel.Show()`에 `SetActive(true)` 호출이 없었음.
- 사용자가 프리팹(Active 켜기)과 `BattleAttackPanel.cs`(`Instantiate()` 직후 `SetActive(true)` 추가) 양쪽 모두 직접 수정 완료.

---

## 17. 폴더 구조

### 스크립트
```
Assets/Scripts/
├── Grid/                    GridManager, TileInstance, TileTypeData, MapGenerator, DeploymentZone
├── Units/                   UnitBase, RaceData, FactionData, WeaponData, ArmorData, ShieldData, UnitEquipmentState, UnitStatusEffectTracker
│   ├── Actions/              UnitAction, WeaponAttack, ReloadAction, StandGroundAction[v3], AttackOption, AttackOptionProvider
│   ├── Traits/RacialTraits/  RacialTrait, TerrainAdaptationTrait(파일명, 클래스명은 TerrainAffinityTrait⚠️), ExtraAttackTrait, MeleeStrengthBonusTrait, VersatileTrait
│   └── AI/                   IUnitAIBehavior, AggressiveMoveTowardEnemy, DefensiveHoldPosition, KeepDistanceAndShoot, AIQueryUtility, AICombatDisposition, EnemyAIController
├── Battle/                  BattlePhaseManager(BattlePhase enum 포함), TurnManager, BattleOutcomeManager, CombatResolver, CombatResult, StatusEffect/
│   ├── Abilities/             IWeaponAbility, ExtraAttackAbility
│   └── UI/                    BattleUIController, BattleAttackPanel, UnitHealthBar, UnitActionVisual, BattleResultPanel
├── Roster/                  RosterEntry, RosterBuilder, RosterPhaseManager, RosterUIController, SkirmishParticipant, AIRosterGenerator
├── Flow/                    SkirmishFlowController
├── Input/                   UnitSelectionController, PlayerDeploymentController, EnemyDeploymentController, SimpleCameraController
├── InputActions/            GameControls.inputactions(+자동생성 GameControls.cs)
├── Visualization/           TileVisualizer, MovementRangeVisualizer
├── Utility/                 UnitSpawner, MovementRangeCalculator, InspectorFieldValidator
├── Debug/                   CombatLogger, UnitStatusDebugDisplay
└── Test/                    TestUnitPlacer, UnitMoveTester, ReinforcementTestTrigger, PhaseTestTrigger, TestUnit

Assets/Prefabs/
├── Character/               TestUnit.prefab
└── UI/                      AttackOptionButtonPrefab.prefab, RaceButtonPrefab.prefab, RosterListItemPrefab.prefab
```

### 에셋
```
ScriptableObjects/
├── Races/, Traits/, Factions/, TileTypes/
├── Equipment/{Weapons, Armors, Shields}/
└── Actions/{Attacks, Special}/   <- StandGroundAction 에셋은 Special/에 저장 권장
```

**네이밍**: `Race_`, `Faction_`, `TileType_`, `Trait_`, `Weapon_`, `Armor_`, `Shield_`, `Attack_[무기명]_[공격명]`, `Action_`

---

## 18. 다른 SRPG와 비교해 식별된 격차

| 항목 | 규모 | 상태 |
|---|---|---|
| **반격(Counterattack)** | 중 | **[v3] 완료** — 로직+UI 모두 구현 |
| **선공 몰살 방지(턴 구조 개편)** | **매우 큼** | 미착수, 구조적 결함으로 인지만 함 |
| ZOC(통제구역)/기회공격 | 중 | 미착수 |
| 시야/엄폐(LoS/Cover) | 중~대 | 미착수. 지형 방어 보너스(TileTypeData에 필드는 있으나 CombatResolver 미반영)와 함께 처리 예정 |
| 방향성/측면 공격(Flanking) | 대 | 미착수 |
| 고저차(Height) | 매우 큼 | 미착수. 그리드가 순수 2D라 구조 변경 필요 |
| 오버워치/대기(Overwatch) | 중 | 미착수 |
| 피로도/행동력 자원 | - | 의도적 보류 |

**진행 순서 제안**: (반격 완료) → 선공 몰살 문제 검토 → ZOC/오버워치 순.

---

## 19. 알려진 제한사항 / 다음 세션 시작점

1. **`ExtraAttackAbility`가 막기/치명타/상태이상을 건너뛰는 문제** — 명중+데미지만 자체 계산하고 `CombatResolver.Resolve()`의 나머지 단계를 안 탐. `CombatResolver` 경로로 통합하는 리팩토링 필요.
2. **선공 몰살 문제(턴 구조) 대응 여부 결정** — 여전히 미해결, 착수 여부 결정 필요.
3. **ZOC, 오버워치 설계 착수 여부**
4. **지형 방어 보너스 연동** — `TileTypeData.defenseBonus`/`TileInstance.GetDefenseBonus()`는 있는데 `CombatResolver.CalculateDamage()`가 안 씀.
5. **엘프-활/드워프-석궁 종족별 무기 접근 제한** 미반영.
6. **월드맵↔배틀맵 전환, 캠페인 모드** 전부 미착수.
7. **이동/공격 불가 유닛(포탑 등)** 플래그(`canMoveInnately`/`canAttackInnately`)는 있으나 구체 클래스 미작성.
8. **정교한 경로탐색(우선순위 큐)** 미적용, 근사치(단순 큐 기반 BFS류)로 대체 중.
9. **[v3 신규] `TerrainAdaptationTrait.cs` 파일의 실제 클래스명이 `TerrainAffinityTrait`** — 네이밍 정리 필요(컴파일엔 문제없음).
10. **[v3 신규] `Battle/BattlePhase.cs` 빈 파일 정리** — 실제 enum은 `BattlePhaseManager.cs`에 있음.
11. **[v3 신규] 파일 인코딩(CP949/EUC-KR vs UTF-8) 불일치** — 손댄 파일들은 UTF-8로 통일됐으나, 나머지 다수 파일이 여전히 CP949. 점진적 통일 권장.
12. **[v3 신규] 배틀 UI의 토글 버튼 라벨** — 기존 "방어태세"에서 역할이 "패널 열기/닫기"로 바뀌었으므로 라벨 재검토(예: "행동 패널") 필요.

---

## 20. 작업 방식 메모 (AI 협업 시)

코드를 직접 수정하기 전에, 먼저 다음을 설명하고 사용자 확인을 받은 뒤에 실제 파일을 수정할 것:

1. **어디를 바꿔야 하는지** — 어떤 파일/클래스/함수인지 구체적으로
2. **왜 필요한지** — 어떤 문제를 해결하는지, 정말 필요한 수정인지
3. **구조가 어떻게 바뀌는지** — 새로 생기는 필드/클래스가 있는지, 기존 호출 흐름이 바뀌는지

사용자가 "설명 없이 바로 해도 된다"고 명시적으로 말한 경우는 예외.

배경: 코드/구조에 대한 이해와 통제권을 놓치고 싶지 않다는 게 이유. AI가 알아서 다 고쳐버리면 무엇이 왜 바뀌었는지 놓칠 수 있음.
