# 타일 기반 턴제 전략 게임 — 역기획서 (v2)

---

## 1. 게임 개요

사각형 그리드 위에서 진행되는 턴제 전략 게임. 플레이어는 인간/엘프/드워프/오크 중 한 종족을 선택하고, 포인트 한도 안에서 유닛 수와 장비를 편성해 상대와 전투를 벌인다.

### 게임 모드
- **대전(Skirmish)** — 현재 구현 대상. 한 판의 전투에 집중.
- **캠페인(Campaign)** — 추후 개발 예정. 육성, 고용비/유지비, 장비 인벤토리, 부위별 방어구, 소모품 등 RPG 요소 추가 예정.

### 개발 환경
- Unity 6 (6000.x)
- 입력: 새 Input System (`GameControls`) — 액션 맵 `Gameplay`: `Click`, `Point`, `Move`, `Zoom`, `EndTurn`, `Cancel`
- UI: Unity UI(uGUI, Canvas/Button/TextMeshPro)

---

## 2. 세력(Faction)과 종족(Race)의 분리

- **세력**: 편/색상/AI 여부 등 "고정 정체성" (`FactionData`)
- **종족**: 기본 스탯, 장비 접근권 (`RaceData`)
- **캠페인 모드**: 세력이 고정된 종족을 가짐 (`FactionData.race`)
- **대전 모드**: 매 판 종족을 다르게 선택 가능. `SkirmishParticipant`(세력+종족+로스터 묶음)라는 별도 개념으로 처리하며 캠페인 구조와 간섭하지 않음

---

## 3. UnitBase 아키텍처 (리팩토링 반영)

`UnitBase`가 비대해져(약 600줄), 책임별로 분리했다. **위임 패턴**을 사용해 `UnitBase`의 public 인터페이스는 그대로 유지하면서 내부 구현만 분리했다.

```
UnitBase (MonoBehaviour) — 이동/전투/행동력 관리 핵심 로직
├── UnitEquipmentState (순수 클래스) — 무기/방패/방어구 슬롯, 파괴 상태, 장전 상태
└── UnitStatusEffectTracker (순수 클래스) — 출혈/기절 등 상태이상 목록 관리
```

`UnitBase`는 `#region`으로 섹션 구분: Events / Identity / Capabilities & Turn State / Equipment / Status Effects / Calculated Stats / Racial Traits / Action Availability / Lifecycle / Faction·Race Assignment / Movement / Action Point Consumption / Combat / Turn State.

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
        20% + (방어자 막기스킬 - 공격자 기술) × 2%p, 범위 5~60%
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
처음엔 "disruption처럼 방어자 맷집과 대결"하는 방식을 검토했으나, **"맷집이 데미지감소+상태이상저항+치명타저항까지 3개 역할을 겸하는 것"이 과도하다고 판단**하여 **방어자 저항 없이 순수 무기 확률**로 단순화. 모든 무기가 최소 5%(`MinimumCritRating`)는 갖도록 `CombatResolver`에서 하한선을 강제(에셋 수정 없이 코드 한 곳만 고치면 전체 반영됨).

---

## 6. 행동력(Action Point) 시스템

### 설계 변경 이력
기존에는 `HasMoved`(bool, 이동) + `AttacksUsedThisTurn`(int, 공격횟수)로 분리되어 있었으나, 재장전/반격태세 등 "공격이 아닌 행동"이 늘어나며 `AttacksUsedThisTurn`을 억지로 공유하는 문제가 발생. **절충안**으로 재정리:

```
HasMoved (bool) — 이동은 항상 최대 1회 (기존 유지)
ActionsUsedThisTurn (int) / MaxActionsPerTurn — 공격, 재장전, 방어태세 등
    "행동" 전체가 공유하는 통합 자원
```

- `ConsumeAction()`: 모든 "행동"(공격/재장전/방어태세)이 공통으로 호출하는 함수. 행동력 소모 + `HasMoved = true`(행동하면 이동도 봉인, 기존 규칙 유지) + 소진 시 `OnActionsExhausted` 이벤트
- `CanAttack`(태생적 공격 가능 여부, 포탑 등에서 false 고정)은 **공격에만 적용**, 재장전/방어태세는 `HasActionsRemaining`만 체크 (별개 축으로 분리)
- `MaxActionsPerTurn`: 종족 특성(`ExtraAttackTrait.ModifyMaxActions`, 옛 이름 `ModifyMaxAttacks`에서 역할 확장에 맞춰 개명)으로 증가 가능. 엘프가 이 특성을 보유해 턴당 2회 행동(공격/재장전/방어태세 무엇이든) 가능.

---

## 7. UnitAction 계층 (신규 — 재장전/방어태세를 위한 공통 뼈대)

`WeaponAttack`(공격)과 "공격이 아닌 행동"(재장전, 방어태세 등)이 늘어날 것을 대비해 공통 부모를 도입.

```
UnitAction (추상 SO)
├── IsAvailable(actor, target) — 지금 이 행동을 선택할 수 있는지
├── Execute(actor, target) — 실행
│
├── WeaponAttack — 공격류 (베기, 찌르기 등)
├── ReloadAction — 재장전 (석궁 전용, WeaponData.reloadAction으로 참조)
└── (설계는 됐으나 파일 미생성) StandGroundAction — "Steady", 방어태세 진입
```

`AttackOptionProvider`가 `UnitAction` 후보 목록(무기의 attacks[] + 재장전 필요 시 reloadAction)을 만들고, 각 `IsAvailable()`로 걸러서 UI 버튼으로 표시. **재장전이 필요한데 장전 안 된 무기는 `WeaponAttack.IsAvailable()`이 false를 반환**해 공격 옵션 자체가 안 뜨고, 대신 `ReloadAction`만 노출됨.

### ⚠️ 미완성 항목
- **`StandGroundAction`(Steady) 파일이 아직 생성되지 않음.** `UnitBase`에는 `IsBraced`, `EnterBracedStance()`, 반격 로직(`PerformCounterattack`)까지는 구현되어 있으나, **플레이어가 UI에서 "방어태세"를 선택할 방법이 없음** (직접 `EnterBracedStance()`를 호출하는 경로 미연결).
- `UnitBase`에 "무기와 무관한 공용 행동 목록"(`universalActions[]` 같은 필드)이 아직 없음. `AttackOptionProvider`가 이걸 후보에 포함하도록 하는 작업도 미완료.
- **다음 세션에서 가장 먼저 처리할 항목.**

---

## 8. 반격 시스템 (핵심 로직 구현 완료, UI 연결 대기)

```csharp
IsBraced (bool) — 방어태세 상태, ResetTurnState()에서 매턴 초기화
EnterBracedStance() — 태세 진입 + ConsumeAction()
TakeDamage(int amount, UnitBase attacker = null) — attacker가 있고 IsBraced && 사거리 안이면 자동 반격
PerformCounterattack(attacker) — CombatResolver.Resolve() 재사용, 반격은 attacker 인자 생략(재귀 반격 방지)
```

- 출혈 등 "공격자 없는" 데미지는 `attacker: null`로 호출되어 반격 발동 안 함
- 방어태세는 **자동 발동이 아니라 사전에 선택해야 하는 전략적 행동**으로 설계 (파이어엠블렘류의 무조건 반격과 차별화)

---

## 9. 상태이상 시스템

| 종류 | 성격 | 처리 방식 |
|---|---|---|
| 출혈 (Bleed) | 지속 데미지 | `UnitStatusEffectTracker` 리스트, 턴 시작 시 데미지+지속시간 감소 |
| 기절 (Stun) | 이동/공격 불가 | 동일 리스트, `IsStunned` 플래그로 행동 차단 |
| 방어구파괴 (ArmorBreak) | 영구 디버프 | 리스트 밖. `ShieldBroken`/`ArmorBroken` 플래그, 방패 우선 파괴 |

**갱신 규칙**: 같은 상태이상 재적용 시 지속시간은 최신값, 데미지(Magnitude)는 기존/신규 중 큰 쪽 유지(완전 중첩 방지). 만료 조건은 `RemainingTurns < 0`(1턴짜리가 최소 한 턴은 온전히 유지되도록).

---

## 10. 종족 특성 (RacialTrait)

`RacialTrait`(SO, abstract) 상속, `RaceData.traits[]`로 복수 조합 가능.

| 특성 클래스 | 훅 | 효과 |
|---|---|---|
| `TerrainAdaptationTrait` | `ModifyMoveCost` | 지정 지형 이동비용 할인 (드워프-산) |
| `ExtraAttackTrait` | `ModifyMaxActions` | 턴당 최대 행동 횟수 증가 (엘프) |
| `MeleeStrengthBonusTrait` | `ModifyMeleeStrength` | 근접 공격 시 힘 보너스, 원거리 미적용 (오크) |
| `VersatileTrait` | (없음) | 효과 없음, 명시적 "보정 없음" 표기용 (인간) |

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

**양손 vs 한손**: 양손 무기는 한손보다 위력·관통·명중 모두 확실히 우위 (대신 방패/보조무기 불가). 한손 무기는 "관통+명중페널티 있음" 또는 "둘 다 없음" 중 하나로 통일.

### 방어구(ArmorData) / 방패(ShieldData)
```
ArmorData: defenseBonus, moveRangePenalty, cost
ShieldData: defenseBonus, blockSkillBonus, cost
```
방패는 무기와 별개 타입. `defenseBonus`(항상 적용) + `blockSkillBonus`(막기 판정용) 이중 효과.

### 슬롯 규칙
양손 무기 장착 시 보조무기/방패 자동 해제. 보조무기(`OffHand` 플래그 무기만)와 방패는 같은 슬롯 경쟁. 보조무기 장착 시 `ExtraAttackAbility`(추가공격, 명중 70% 배율) 자동 부여.

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

**컨셉**: 베기=출혈, 찌르기=관통특화(상태이상 없음), 둔기=강력한 일격+상태이상. 활=고점(치명타 25%), 석궁=저점(항상 일정, 대신 격발 후 재장전 필요).

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

**컨셉**: 인간=기준값, 엘프=빠르고 정확하지만 약함(비용14), 드워프=느리지만 최강 방어(비용14), 오크=강하지만 부정확한 물량형(비용8).
**포인트당 효율**: 인간16.0, 엘프/드워프12.0(의도적 통일), 오크18.0(물량형).

---

## 14. 턴 / 페이즈 / AI

### 페이즈
`BattlePhase`: `Placement` → `Battle` → `Ended`

### 턴 관리 (`TurnManager`)
**세력 단위 순환**. `AddFaction`/`RemoveFaction`으로 난입/전멸 지원.

> ⚠️ **알려진 구조적 문제 (다른 SRPG와 비교 분석에서 발견, 미해결)**: 세력 단위로 유닛 전체가 한 번에 행동하는 구조라 "선공 몰살(Alpha Strike)" 위험이 있음 — 선공 세력이 전 병력을 연속 행동시켜 상대가 한 번도 못 움직이고 전력을 잃을 수 있음. 개별 유닛 턴제(민첩 기반 순서) 또는 교번 행동(1유닛씩 번갈아)으로 바꾸는 게 정석이나, **핵심 구조 리팩토링이라 규모가 매우 큼**. 반격 시스템 완성 후 별도로 착수 여부 결정 예정.

### AI 편성 (`AIRosterGenerator`)
지정 종족 고정 편성(무작위 아님), 무기 필수 장착 규칙. 전략(`AIRosterStrategy`): Standard/MeleeFocus/RangedFocus(80% 편중).

### AI 행동 (`IUnitAIBehavior`, 부대 단위 통일 적용)
| 클래스 | 성향 | 행동 |
|---|---|---|
| `AggressiveMoveTowardEnemy` | Aggressive | 최근접 적 접근 후 공격 |
| `DefensiveHoldPosition` | Defensive | 사거리 내 적에만 반응, 그 외 사수 |
| `KeepDistanceAndShoot` | RangedKiting | 원거리 유닛만 거리유지, 근접유닛은 Defensive로 대체 |

공통 로직은 `AIQueryUtility`로 분리. **재장전 필요 무기를 든 AI의 재장전 처리 로직은 각 AI 클래스에 추가 필요 여부 재확인 요망** (설계는 논의됐으나 최종 반영 확인 안 됨).

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

씬 분리 없이 한 씬 안 UI 패널/오브젝트 활성화로 흐름 제어. 세력/AI 관련 정보는 `RosterPhaseManager`에 통합 관리, `OnValidate`로 설정 오류 즉시 검증.

---

## 16. UI

- 유닛 머리 위 체력바 (`UnitHealthBar`, `OnDamaged` 구독 자동 갱신)
- 공격 시 `UnitAction` 후보(공격+재장전 등) 개수만큼 옵션 버튼, 숫자키 단축
- 전투 시작/턴 종료 버튼, 승패 결과 화면
- `UnitActionVisual`: 행동완료 시 색 어둡게, 데미지 받으면 잠깐 흰색(또는 지정색) 플래시 후 "현재 있어야 할 상태"(행동완료 여부에 따라 밝은/어두운 색)로 복귀
- 부대 편성 화면: 좌(Draft+목록)/우(적 설정+포인트) 좌우 분할

---

## 17. 폴더 구조 (정리 완료)

### 스크립트
```
_Project/Scripts/
├── Grid/                    GridManager, TileInstance, TileTypeData, MapGenerator, DeploymentZone
├── Units/                   UnitBase, RaceData, FactionData, WeaponData, ArmorData, ShieldData, UnitEquipmentState, UnitStatusEffectTracker
│   ├── Actions/              UnitAction, WeaponAttack, ReloadAction, AttackOption, AttackOptionProvider
│   ├── Traits/               RacialTrait, TerrainAdaptationTrait, ExtraAttackTrait, MeleeStrengthBonusTrait, VersatileTrait
│   └── AI/                   IUnitAIBehavior, AggressiveMoveTowardEnemy, DefensiveHoldPosition, KeepDistanceAndShoot, AIQueryUtility
├── Battle/                  BattlePhase(Manager), TurnManager, BattleOutcomeManager, CombatResolver, CombatResult, StatusEffectType/Instance
│   ├── Abilities/             IWeaponAbility, ExtraAttackAbility
│   └── UI/                    BattleUIController, BattleAttackPanel, UnitHealthBar, UnitActionVisual, BattleResultPanel
├── Roster/                  RosterEntry, RosterBuilder, RosterPhaseManager, RosterUIController, SkirmishParticipant, AIRosterGenerator
├── Flow/                    SkirmishFlowController
├── Input/                   UnitSelectionController, PlayerDeploymentController, EnemyDeploymentController, SimpleCameraController, GameControls(자동생성)
├── Visualization/           TileVisualizer, MovementRangeVisualizer
├── Utility/                 UnitSpawner, MovementRangeCalculator, InspectorFieldValidator
└── Debug/                   CombatLogger, UnitStatusDebugDisplay

_Sandbox/Scripts/            TestUnitPlacer, UnitMoveTester, ReinforcementTestTrigger
```

### 에셋
```
ScriptableObjects/
├── Races/, Traits/, Factions/, TileTypes/
├── Equipment/{Weapons, Armors, Shields}/
└── Actions/{Attacks, Special}/
```

**네이밍**: `Race_`, `Faction_`, `TileType_`, `Trait_`, `Weapon_`, `Armor_`, `Shield_`, `Attack_[무기명]_[공격명]`, `Action_`

---

## 18. 다른 SRPG(배틀 브라더스, 택틱스 오우거, 인투 더 브리치 등)와 비교해 식별된 격차

우선순위와 규모를 함께 정리 (작업 규모: 소/중/대/매우큼):

| 항목 | 규모 | 상태 |
|---|---|---|
| **반격(Counterattack)** | 중 | 핵심 로직 구현 완료, UI(Steady 선택) 연결 대기 |
| **선공 몰살 방지(턴 구조 개편)** | **매우 큼** | 미착수, 구조적 결함으로 인지만 함 |
| ZOC(통제구역)/기회공격 | 중 | 미착수 |
| 시야/엄폐(LoS/Cover) | 중~대 | 미착수. 지형 방어 보너스(TileTypeData에 필드는 있으나 미사용)와 함께 처리 예정 |
| 방향성/측면 공격(Flanking) | 대 | 미착수. "유닛이 바라보는 방향" 개념 자체가 없음 |
| 고저차(Height) | 매우 큼 | 미착수. 그리드가 순수 2D라 구조 변경 필요 |
| 오버워치/대기(Overwatch) | 중 | 미착수. 반격과 함께 다음 후보 |
| 피로도/행동력 자원 | - | 의도적 보류 (상태이상 시스템 자리잡은 뒤 재검토) |

**포트폴리오 볼륨을 고려해 시야/방향성/고저차는 우선순위 낮음으로 판단.** 반격 → (선공 몰살 문제 검토) → ZOC/오버워치 순으로 진행 예정.

---

## 19. 알려진 제한사항 / 다음 세션 시작점

1. **[최우선] StandGroundAction(Steady) 생성 및 AttackOptionProvider에 공용 행동(universalActions) 통합** — 반격 시스템의 마지막 퍼즐
2. AI 클래스들의 재장전 처리 로직 최종 반영 여부 확인
3. 선공 몰살 문제(턴 구조) 대응 여부 결정
4. ZOC, 오버워치 설계 착수 여부
5. 지형 방어 보너스(TileTypeData 필드는 있으나 CombatResolver 미반영) 연동
6. 엘프-활/드워프-석궁 종족별 무기 접근 제한 미반영
7. 보조무기 추가공격(`ExtraAttackAbility`)이 치명타/상태이상 판정과 완전히 연동됐는지 재확인 필요
8. 월드맵↔배틀맵 전환, 캠페인 모드 전부 미착수
9. 이동/공격 불가 유닛(포탑 등)은 플래그(`canMoveInnately`/`canAttackInnately`)로 대응 가능하나 구체 클래스 미작성
10. 정교한 경로탐색(우선순위 큐) 미적용, 근사치로 대체 중
