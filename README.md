# 타일 기반 턴제 전략 게임 — 역기획서 (v4)
---

## 1. 게임 개요

사각형 그리드 위에서 진행되는 턴제 전략 게임. 플레이어는 인간/엘프/드워프/오크 중 한 종족을 선택하고, 포인트 한도 안에서 유닛 수와 장비를 편성해 상대와 전투를 벌인다.

### 게임 모드
- **대전(Skirmish)** — 구현 대상. 메인 메뉴 → 부대 편성 → 배치 → 전투 → 결과까지 전체 루프 완성.
- **캠페인(Campaign)** — 추후 개발 예정. 육성, 고용비/유지비, 장비 인벤토리, 부위별 방어구, 소모품 등 RPG 요소 추가 예정. **전부 미착수** (포트폴리오 우선순위상 대전 모드 완성도를 우선하기로 재확인함).

### 개발 환경
- Unity 6 (6000.x)
- 입력: 새 Input System (`GameControls`) — 액션 맵 `GamePlay`: `Click`, `Point`, `Move`, `Zoom`, `EndTurn`, `Cancel`, **`ToggleActions`(추가)**
- UI: Unity UI(uGUI, Canvas/Button/TextMeshPro), **Screen Space - Overlay**
- 키 리바인딩(`EndTurn`/`Cancel`/`ToggleActions`) 및 마스터 볼륨(AudioMixer) 설정 지원

---

## 2. 세력(Faction)과 종족(Race)의 분리

- **세력**: 편/색상/AI 여부 등 "고정 정체성" (`FactionData`)
- **종족**: 기본 스탯, 장비 접근권, **비주얼(몸/머리 스프라이트, 체형 배율, 비무장 기본 장비)** (`RaceData`)
- **캠페인 모드**: 세력이 고정된 종족을 가짐 (`FactionData.race`)
- **대전 모드**: 매 판 종족을 다르게 선택 가능. `SkirmishParticipant`(세력+종족+로스터 묶음)라는 별도 개념으로 처리하며 캠페인 구조와 간섭하지 않음
- 세력 식별은 이제 유닛 스프라이트 색칠이 아니라 **발밑 `UnitFactionMarker`**(별도 원형 마커)로 표시 (7장 참고)

---

## 3. UnitBase 아키텍처 (리팩토링 반영)

`UnitBase`가 비대해져(약 600줄), 책임별로 분리했다. **위임 패턴**을 사용해 `UnitBase`의 public 인터페이스는 그대로 유지하면서 내부 구현만 분리했다.

```
UnitBase (MonoBehaviour) — 이동/전투/행동력 관리 핵심 로직
├── UnitEquipmentState (순수 클래스) — 무기/방패/방어구 슬롯, 파괴 상태, 장전 상태
└── UnitStatusEffectTracker (순수 클래스) — 출혈/기절 등 상태이상 목록 관리
```

`UnitBase`는 `#region`으로 섹션 구분: Events / Identity / Capabilities & Turn State / Equipment / Status Effects / Calculated Stats / Racial Traits / Action Availability / Lifecycle / Faction·Race Assignment / Movement / Action Point Consumption / Combat / Turn State.

**비주얼은 `UnitBase`가 직접 안 들고 있음** — 자체 `SpriteRenderer`/기본 사각형 스프라이트 생성 코드는 전부 제거됨. 실제 렌더링은 외부 `UnitVisualController`(7장)가 전담하고, `UnitBase`는 `Race`/`MainHandWeapon`/`EquippedArmor`/`EquippedShield`/`FacingDirection` 등 **그 컴포넌트가 읽어갈 데이터만** 노출.

**추가된 것**: `FacingDirection`(enum: Up/Down/Left/Right) — 이동(`TryMoveTo`)과 공격(`TryAttack`) 시 자동 갱신. Flanking 작업의 전제조건.

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

(변경 없음)

---

## 5. 전투 판정 구조 (5단계 + 위치 기반 보정)

```
1단계 - 명중/회피: 공격자 Agility vs 방어자 Agility
        70% + (공격자민첩 - 방어자민첩) × 5%p + 무기 명중보정, 범위 5~95%
        + 엄폐 보정(추가): 원거리 공격이고 방어자 인접 4칸 중 엄폐 제공원이 있으면 -20%p

2단계 - 막기 (방패 보유 & 미파괴 시만 시도):
        공격자 (Melee/RangedSkill) vs 방어자 (DefenseSkill + 방패 blockSkillBonus)
        20% + (방어자 막기스킬 - 공격자 기술) × 2%p, 범위 5~60%
        성공 시 데미지/치명타/상태이상 완전 무효(Blocked)

3단계 - 데미지:
        raw = 무기위력(기본+보너스) + 유효힘(힘기반 무기만, 근접 시 종족특성 반영)
        방어력 = 맷집 + max(0, [방어구+방패 보너스] - 관통력)
        기본 데미지 = max(1, raw - 방어력)
        (지형 방어 보너스는 인프라만 준비됨, 아직 미반영 — 19장 참고)

3-1단계 - 치명타 (막기 실패 시):
        확률 = max(무기기본치명타 + 공격보너스, 최소치 5%) — 방어자 저항 없음, 무기 고유 확률
        성공 시 데미지 × 1.5배

4단계 - 상태이상 (명중 + 막기 실패 시만 시도):
        40% + (disruption - 방어자 맷집) × 2%p, 범위 5~80%
        성공 시: 출혈/기절(임시, 갱신형) 또는 방어구파괴(즉시 영구, 방패 우선 파괴)
```

`CombatResolver.Resolve()`에 `accuracyMultiplier`(기본값 1) 파라미터가 추가됨 — 보조무기 추가공격처럼 "약화된 확률의 공격"을 표현하는 데 사용(11장 참고).

### 방어구 관통력의 원칙
관통력은 방어구+방패 보너스에서만 차감됨. **맷집은 관통 불가**. (지형 방어 보너스도 반영 시 같은 원칙 적용 예정)

### 치명타 설계 결정 과정 (참고, v2와 동일)
방어자 저항 없이 순수 무기 확률로 단순화. 모든 무기가 최소 5%(`MinimumCritRating`)는 갖도록 `CombatResolver`에서 하한선을 강제.

---

## 6. 행동력(Action Point) 시스템

```
HasMoved (bool) — 이동은 항상 최대 1회
ActionsUsedThisTurn (int) / MaxActionsPerTurn — 공격, 재장전, 방어태세 등 "행동" 전체가 공유하는 통합 자원
```

- `ConsumeAction()`: 모든 "행동"이 공통으로 호출. 행동력 소모 + `HasMoved = true` + 소진 시 `OnActionsExhausted` 이벤트
- **반격/기회공격은 행동력을 소모하지 않는 반응(reaction)** — `ConsumeAction()`을 안 거침(자기 턴이 아닐 때 발동하는 것이라 당연한 설계)
- `CanAttack`은 공격에만 적용, 재장전/방어태세는 `HasActionsRemaining`만 체크
- `MaxActionsPerTurn`: 엘프의 `ExtraAttackTrait.ModifyMaxActions`로 턴당 2회 행동 가능

---

## 7. UnitAction 계층 및 비주얼 시스템

### 7-1. UnitAction (완료)

```
UnitAction (추상 SO)
├── IsAvailable(actor, target)
├── Execute(actor, target)
│
├── WeaponAttack — 공격류
├── ReloadAction — 재장전 (석궁 전용)
└── StandGroundAction — "Steady", 방어태세 진입 (완료)
```

`UnitBase.universalActions[]`(무기와 무관한 공용 행동 목록)이 추가되어, `AttackOptionProvider`가 무기 공격 + 재장전 + 공용 행동(방어태세 등)을 한 목록으로 합쳐서 UI에 노출. **v2에서 최우선 미완성으로 남아있던 부분, 전부 해결됨.**

UX: 유닛 선택 시 공용 행동 패널이 자동으로 열리고(대상 없이), R키(`ToggleActions`)로 토글. 적 클릭 시 뜨는 공격 옵션 패널과 같은 패널(`BattleAttackPanel`)을 공유.

### 7-2. 캐릭터 비주얼 (신규 — 페이퍼돌)

Universal LPC Spritesheet 기반. **"애니메이션은 동작 패턴(몸의 포즈), 무기/장비는 별도 레이어"**라는 원칙으로 설계.

```
UnitBase (게임 로직, 그리드 좌표) — 스케일 항상 1
└── VisualRoot (종족별 visualScale 적용 — 체형 차이 표현, 예: 드워프)
    ├── BodyRenderer   (Race.visualSet)
    ├── HeadRenderer   (Armor.headVisualSet 있으면 우선, 없으면 Race.headVisualSet)
    ├── ArmorRenderer  (EquippedArmor.visualSet — 항상 존재, null 아님. 비무장도 전용 ArmorData)
    └── WeaponRenderer (MainHandWeapon.visualSet, OffHandSlot은 보조무기/방패 중 있는 쪽)

FactionMarker / UnitHealthBar — VisualRoot 밖(유닛 루트 직속), 종족 스케일 영향 안 받음
```

`CharacterAnimationSet`(SO): `idle`/`combatIdle`/`walk`/`slash`/`thrust`/`shoot` × 4방향(Up/Left/Down/Right). `WeaponData`/`ArmorData`/`ShieldData`/`RaceData`에 각각 `visualSet` 필드.

**현재 범위**: idle만 재생(4방향, 정지). walk/공격 애니메이션 트리거는 미착수(19장 참고). 보조무기 페이퍼돌은 LPC 에셋 한계로 미지원(양손 무기용 모션만 있어 쌍수 표현 불가) — 해당 종족의 보조무기 후보를 비워 선택 자체를 막아둠.

에셋 파이프라인은 두 에디터 도구(`BatchGridSlicer`, `AnimationSetBuilder`)로 자동화됨 — 17장 참고.

---

## 8. 반격 시스템 (완료 — 설계 변경됨)

**v2/v3 설계(방어태세=반격 게이트)에서 최종적으로 분리됨.**

```csharp
IsBraced (bool) — 방어태세 상태, 막기 확률 보너스(BracedBlockBonus)에만 관여. 반격과 무관
WeaponData.grantsCounterattack (bool) — 이 무기를 들고 있으면 근접 사거리 내 피격 시 자동 반격
TakeDamage(int amount, UnitBase attacker = null)
    — !IsStunned && MainHandWeapon.grantsCounterattack && IsInAttackRange(attacker) 면 반격
PerformCounterattack(attacker) — CombatResolver.Resolve() 재사용, attacker 인자 생략(재귀 방지)
```

- 반격 가능 무기: 현재 **한손검/양손검만** `grantsCounterattack = true` (검류가 "정석적으로 안정적인 무기"라는 컨셉과 부합)
- ZOC 기회공격(14장)도 같은 `Resolve()` 경로를 재사용하되 `attacker: null`로 넘겨 반격 연쇄를 막음
- 방어태세는 여전히 "사전에 선택해야 하는 전략적 행동"이지만, 이제 그 효과는 순수하게 막기 확률 상승뿐

---

## 9. 상태이상 시스템

(v2와 동일, 변경 없음)

| 종류 | 성격 | 처리 방식 |
|---|---|---|
| 출혈 (Bleed) | 지속 데미지 | `UnitStatusEffectTracker` 리스트, 턴 시작 시 데미지+지속시간 감소 |
| 기절 (Stun) | 이동/공격 불가 | 동일 리스트, `IsStunned` 플래그로 행동 차단 |
| 방어구파괴 (ArmorBreak) | 영구 디버프 | 리스트 밖. `ShieldBroken`/`ArmorBroken` 플래그, 방패 우선 파괴 |

---

## 10. 종족 특성 (RacialTrait)

`RacialTrait`(SO, abstract) 상속, `RaceData.traits[]`로 복수 조합 가능.

| 특성 클래스 | 훅 | 효과 |
|---|---|---|
| `TerrainAdaptationTrait` | `ModifyMoveCost` | 지정 지형 이동비용 할인 (드워프-산) |
| `ExtraAttackTrait` | `ModifyMaxActions` | 턴당 최대 행동 횟수 증가 (엘프) |
| `MeleeStrengthBonusTrait` | `ModifyMeleeStrength` | 근접 공격 시 힘 보너스, 원거리 미적용 (오크) |
| `VersatileTrait` | (없음) | 효과 없음, 명시적 "보정 없음" 표기용 (인간) |

`TerrainAdaptationTrait.cs`의 파일명/클래스명 불일치(`TerrainAffinityTrait`) 수정 완료 — 클래스명을 파일명과 일치하도록 변경, 다른 참조처가 없어 안전하게 처리됨.

---

## 11. 장비 시스템

### 무기 (WeaponData) — 2단 구조

```
WeaponData
├── handedness, slotType, isRanged, damageScaling, attackRangeOverride
├── basePower, baseArmorPenetration, baseAccuracyBonus, baseCritRating
├── grantsCounterattack (신규) — 근접 피격 시 자동 반격 여부
├── visualSet (신규) — CharacterAnimationSet 참조
└── attacks[]: WeaponAttack 참조 배열
    requiresReload, reloadAction

WeaponAttack : UnitAction
├── powerBonus, armorPenetrationBonus, accuracyBonusModifier, critRatingBonus
└── inflictedEffect, disruption, effectDuration, effectMagnitude
```

`ExtraAttackAbility`(보조무기 추가공격)가 자체 명중/데미지 계산 대신 **`CombatResolver.Resolve()`를 `accuracyMultiplier`(기본 0.7)와 함께 호출**하도록 수정됨 — 막기/치명타/상태이상을 스킵하던 버그 해결.

### 방어구(ArmorData) / 방패(ShieldData)
```
ArmorData: defenseBonus, moveRangePenalty, cost, visualSet(신규), headVisualSet(신규, 투구 있는 갑옷용)
ShieldData: defenseBonus, blockSkillBonus, cost, visualSet(신규)
```

**비무장 처리**: `RaceData.unarmoredArmor`(ArmorData 참조, `availableArmors[]` 배열 밖에 별도 보관)로 "갑옷 안 고름" 상태를 표현. 무기/방패와 같은 "-1 = 특수 상태" 순환 관례를 유지하되, 그 특수 상태가 가리키는 값이 `null`이 아니라 실제 데이터. `RosterUIController.ResetDraft()`/`CycleArmor()` 양쪽 다 이 필드를 참조해야 하며, 한쪽만 반영하면 로스터 화면 재진입 시 상태가 꼬이는 버그가 있었음(수정 완료).

### 슬롯 규칙
(v2와 동일) 양손 무기 장착 시 보조무기/방패 자동 해제. 보조무기와 방패는 같은 슬롯 경쟁. 보조무기 장착 시 `ExtraAttackAbility` 자동 부여.

---

## 12. 확정된 무기 목록

| 무기 | 손 | 사거리 | 기본(위력/관통/명중/치명) | 공격 | 상태이상/특이사항 | 반격 |
|---|---|---|---|---|---|---|
| 양손검 | 양손 | 1 | 7/2/3/- | 베기, 찌르기 | 베기: 출혈 | O |
| 한손검 | 한손 | 1 | 4/0/0/- | 베기, 찌르기 | 베기: 출혈(약함) | O |
| 양손도끼 | 양손 | 1 | 9/2/0/- | 가르기 | 방어구파괴 | - |
| 한손도끼 | 한손 | 1 | 6/2/-3/- | 가르기 | 방어구파괴(약함) | - |
| 메이스 | 양손 | 1 | 8/2/0/- | 강타 | 기절 | - |
| 창 | 한손 | 2 | 5/0/0/- | 찌르기 | 없음 | - |
| 활 | 양손 | 2 | 4/0/0/25 | 사격 | 없음, Strength기반+고점 | - |
| 석궁 | 양손 | 2 | 24/4/10/- | 발사 | 없음, Fixed+재장전 필요 | - |

페이퍼돌 비주얼: 한손도끼는 전용 에셋이 없어 양손도끼(`Waraxe`)와 시각 자원을 공유(게임 수치는 독립).

**보류**: 엘프-활/드워프-석궁 종족별 무기 접근 제한 — 여전히 미반영.

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

**컨셉**: (v2와 동일) 인간=기준값, 엘프=빠르고 정확하지만 약함, 드워프=느리지만 최강 방어, 오크=강하지만 부정확한 물량형.

**추가된 데이터**: 각 종족 `RaceData`에 `visualSet`/`headVisualSet`/`unarmoredArmor`/`visualScale`(체형 배율, 드워프만 1보다 작게) 설정.

---

## 14. 턴 / 페이즈 / AI

### 페이즈
`BattlePhase`: `Placement` → `Battle` → `Ended` (변경 없음)

### 턴 관리 (`TurnManager`)
세력 단위 순환. (변경 없음)

> **선공 몰살(Alpha Strike) 문제 — 검토 완료, 구조는 유지하기로 결정.** 다른 SRPG(XCOM 2)를 조사한 결과 정확히 같은 턴 구조("자기 세력 전체가 아무 순서로나 행동 후 상대에게 턴 넘김")였음을 확인. 알파 스트라이크는 구조 자체가 아니라 "움직임에 대가가 없는 것"이 문제라고 재정의하고, **반격 + ZOC(기회공격) + 엄폐/장애물** 조합으로 완화하는 방향으로 확정(XCOM의 "반응사격+커버" 조합과 유사). 턴 구조 리팩토링은 착수하지 않음.

### ZOC(Zone of Control) / 기회공격 (신규, 완료)
`ZoneOfControlResolver`(순수 계산) + `UnitBase.TryMoveTo()` 통합. 이동 시작 지점엔 인접했던 적이 도착 지점엔 인접하지 않게 되면(=벗어나면) 기회공격 발동. 위협 조건은 반격과 동일(근접 사거리, 비기절, 장전됨). 맞아도 즉사만 아니면 이동은 완료.

### 지형 · 엄폐 · 장애물 (신규, 완료)
- **엄폐**: `TileTypeData.providesCover`(파괴 불가 지형) 또는 `UnitBase.ProvidesCover`(파괴 가능 장애물 유닛) — 원거리 공격에만 적용, 방어자 인접 4칸 중 하나라도 있으면 명중률 -20%p
- **장애물 유닛**(`ObstacleUnit : UnitBase`): `canMoveInnately`/`canAttackInnately` 플래그로 이동/공격 불가 구현(v2에서 예고됐던 "포탑용 플래그" 개념을 실제로 처음 사용한 사례). `Faction = null`로 세력 무소속 — 기존 null-safe 로직들 덕분에 코드 수정 없이 안전하게 동작
- **절차적 배치**: `ObstaclePlacementCalculator`가 노이즈로 후보를 고르고 BFS로 연결성(고립 지역 없음)을 보장. 배치 구역은 제외
- **지형 방어 보너스**(`TileTypeData.defenseBonus`): 코드 인프라는 준비됐지만 실제로 쓰는 지형이 없어 아직 `CombatResolver`에 미반영(의도적 보류)

### AI 편성 (`AIRosterGenerator`)
(v2와 동일) 비무장 선택지도 `race.unarmoredArmor`를 참조하도록 함께 수정됨.

### AI 행동 (`IUnitAIBehavior`)
| 클래스 | 성향 | 행동 |
|---|---|---|
| `AggressiveMoveTowardEnemy` | Aggressive | 최근접 적 접근 후 공격 |
| `DefensiveHoldPosition` | Defensive | 사거리 내 적에만 반응, 그 외 사수 |
| `KeepDistanceAndShoot` | RangedKiting | 원거리 유닛만 거리유지, 근접유닛은 Defensive로 대체 |

> **AI가 ZOC/엄폐를 전혀 인식하지 못함.** 반격 시스템까지는 AI도 자연스럽게 영향을 받지만(피격당하면 자동 발동이라 AI 행동 코드 수정 불필요), ZOC·엄폐는 AI가 "이동 경로/위치를 고를 때" 고려해야 하는 능동적 요소라 관련 AI 클래스 수정이 필요함. **다음 세션 최우선 후보** (검토 착수했으나 중단된 상태).

AI 재장전 처리 로직은 `KeepDistanceAndShoot`에 반영 완료 확인됨(v2에서 미확인이었던 항목).

### 오버워치(Overwatch) — 검토 후 보류 결정
XCOM도 근접 유닛엔 잘 안 주는 기능이고, 이 게임 로스터가 근접 위주(8종 중 6종)라 수혜 대상이 좁음 + ZOC보다 구현 난이도가 큼(사거리 내 전체 이동을 매번 검사)을 근거로 **착수하지 않기로 결정**. 원거리 유닛 비중이 커지거나 궁수 무력함에 대한 밸런스 불만이 실제로 나오면 재검토.

---

## 15. 대전(Skirmish) 모드 흐름

```
메인 메뉴 (신규)
  -> 시작 / 옵션 / 가이드 / 종료
부대 편성 (RosterPhaseManager + RosterUIController)
  -> 종족/적종족/AI전략/AI성향 선택, 포인트 슬라이더로 총량 조정, 장비 순환 선택, 확정
  -> [메인 메뉴로] 버튼 (신규, 로스터 유지된 채로 돌아감)
그리드 배치
  -> PlayerDeploymentController(클릭), EnemyDeploymentController(자동)
전투 (Battle)
  -> [옵션] [전투 중단](확인 후, 신규 — 로스터 리셋되고 메인 메뉴로) 버튼 추가
결과 화면 (승/패) -> 부대 편성 복귀
```

씬 분리 없이 한 씬 안 UI 패널/오브젝트 활성화로 흐름 제어(변경 없음). `SkirmishFlowController`에 `CleanupBattle()`(유닛 파괴/턴·페이즈 리셋/맵 재생성)을 공용 메서드로 분리해 "결과 화면에서 돌아가기"와 "전투 중단" 둘 다 재사용.

---

## 16. UI

### 기존 (v2, 변경 없음)
- 유닛 머리 위 체력바 (`UnitHealthBar`)
- 공격 시 `UnitAction` 후보 개수만큼 옵션 버튼, 숫자키 단축
- 부대 편성 화면: 좌(Draft+목록)/우(적 설정+포인트) 좌우 분할

### 신규
- **메인 메뉴 / 팝업 시스템**: `UIPopup`(CanvasGroup 기반 열기/닫기·애니메이션) + `PopupCoordinator`(싱글톤, 상호배제·백드롭·`IsAnyPopupOpen` 관리). 옵션(AudioMixer 볼륨, 3개 키 리바인딩), 플레이 방법(스크롤+고정 닫기 버튼) 팝업 구현
- **`UnitActionVisual`**: 세력색 틴트 로직 제거(→ `UnitFactionMarker`로 이전), 행동완료 시 어두워짐/피격 시 흰색 플래시는 유지하되 대상 렌더러를 `UnitVisualController.bodyRenderer`로 변경
- **SortingLayer 체계**: `Background - Terrain - Highlight - Units - UnitUI` 5단계(문자열은 `SortingLayers` 정적 클래스로 통일 관리)
- **재사용 UI 컴포넌트**: `LabeledButton`(라벨+버튼 쌍), `RemovableListItem`(배경+텍스트+제거버튼)
- **타일 비주얼**: 지형 아이콘 적용, 배치구역 틴트를 아이콘 위에도 적용하도록 수정, 격자선 전용 오버레이(`TileGridOverlay`) 분리, 서브픽셀 렌더링으로 인한 타일 틈 깜빡임을 타일 살짝 오버사이즈(1.02배)로 완화

---

## 17. 폴더 구조 (v2 대비 추가/변경)

### 스크립트
```
_Project/Scripts/
├── Grid/
│   ├── GridManager, TileInstance, TileTypeData, MapGenerator, DeploymentZone (기존)
│   └── TileGridOverlay.cs                (신규)
├── Units/
│   ├── UnitBase, RaceData, FactionData, WeaponData, ArmorData, ShieldData,
│   │   UnitEquipmentState, UnitStatusEffectTracker (기존, 필드 추가됨 - 11장 참고)
│   ├── ObstacleUnit.cs                   (신규)
│   ├── ZoneOfControlResolver.cs          (신규)
│   ├── Actions/                          UnitAction, WeaponAttack, ReloadAction,
│   │                                     StandGroundAction(완료), AttackOption, AttackOptionProvider
│   ├── Traits/                           (기존, TerrainAdaptationTrait 클래스명 수정됨)
│   └── AI/                               (기존, ZOC/엄폐 인식 못 함 - 14장 참고)
├── Battle/                               BattlePhase(Manager), TurnManager, BattleOutcomeManager,
│   │                                     CombatResolver(accuracyMultiplier 추가), CombatResult
│   ├── Abilities/                        IWeaponAbility, ExtraAttackAbility(수정됨)
│   └── UI/                               BattleUIController(옵션/전투중단 버튼 추가), BattleAttackPanel,
│                                          UnitHealthBar, UnitActionVisual(수정), BattleResultPanel
├── Roster/                               RosterEntry, RosterBuilder, RosterPhaseManager,
│                                          RosterUIController(수정), SkirmishParticipant, AIRosterGenerator(수정)
├── Flow/
│   ├── SkirmishFlowController.cs         (CleanupBattle 등 추가)
│   ├── MainMenuController.cs             (신규)
│   ├── OptionsPanelController.cs         (신규)
│   ├── KeyRebindController.cs            (신규)
│   ├── ConfirmationPopup.cs              (신규)
│   └── PopupCoordinator.cs               (신규)
├── UI/ (공용 팝업/비주얼 컴포넌트, 신규 디렉토리)
│   ├── UIPopup.cs
│   ├── LabeledButton.cs
│   ├── RemovableListItem.cs
│   ├── UnitFactionMarker.cs
│   └── UnitVisualController.cs
├── Input/                                UnitSelectionController(가드 추가), PlayerDeploymentController,
│                                          EnemyDeploymentController, SimpleCameraController(페이즈 가드 추가),
│                                          GameControls, InputBindingUtility.cs(신규)
├── Visualization/                        TileVisualizer(아이콘/틴트 수정), MovementRangeVisualizer(레이어 수정)
├── Utility/                              UnitSpawner(수정), MovementRangeCalculator, InspectorFieldValidator,
│                                          ObstaclePlacementCalculator.cs(신규), ProceduralSpriteUtility.cs(신규)
├── Editor/                               (신규 디렉토리, 빌드 제외)
│   ├── BatchGridSlicer.cs
│   └── AnimationSetBuilder.cs
└── Debug/                                CombatLogger, UnitStatusDebugDisplay
```

### 에셋
```
ScriptableObjects/
├── Races/, Traits/, Factions/, TileTypes/
├── Equipment/{Weapons, Armors, Shields}/     (각 SO에 visualSet 참조 추가됨)
├── Visuals/                                  CharacterAnimationSet 에셋들 (신규)
└── Actions/{Attacks, Special}/
```

---

## 18. 다른 SRPG와 비교해 식별된 격차 (갱신)

| 항목 | 규모 | 상태 |
|---|---|---|
| **반격(Counterattack)** | 중 | 완료 (무기 속성 기반으로 최종 확정) |
| **ZOC(통제구역)/기회공격** | 중 | 완료 |
| **엄폐(Cover)** | 중 | 완료 (타일+유닛 둘 다 지원, 간이 판정) |
| **절차적 장애물 배치** | 중 | 완료 (연결성 보장) |
| **선공 몰살 방지(턴 구조 개편)** | 매우 큼 | 검토 완료 - **구조 유지, 착수 안 함**으로 결정 |
| **오버워치/대기(Overwatch)** | 중 | 검토 완료 - **보류**로 결정(로스터 구성상 수혜 좁음) |
| AI의 ZOC/엄폐 인식 | 중~대 | 검토 중단, **다음 세션 최우선 후보** |
| 이동 보간(트윈) | 중 | 미착수. walk 애니메이션 활성화의 전제조건 |
| 공격 애니메이션 트리거 | 중 | 미착수 |
| 시야/차단(LoS) 자체(엄폐와 별개) | 대 | 미착수, 낮은 우선순위 유지 |
| 방향성/측면 공격(Flanking) | 대 | 전제조건(`FacingDirection`) 갖춰짐, 판정 로직은 미착수 |
| 고저차(Height) | 매우 큼 | 미착수, 낮은 우선순위 유지 |
| 피로도/행동력 자원 | - | 의도적 보류 |

---

## 19. 알려진 제한사항 / 다음 세션 시작점

1. **[최우선] AI가 ZOC/엄폐를 인식하도록 수정** — `AggressiveMoveTowardEnemy`/`DefensiveHoldPosition`/`KeepDistanceAndShoot`가 이 시스템들 도입 전 로직 그대로라, 사람 플레이어만 활용 가능한 비대칭 상태
2. 이동 보간(트윈) 구현 — walk 애니메이션 및 이동 연출의 전제조건
3. 공격 애니메이션(slash/thrust/shoot) 트리거 설계 및 구현
4. Flanking(측면 공격) 판정 로직 — `FacingDirection`은 준비됨
5. 지형 방어 보너스(`TileTypeData.defenseBonus`) `CombatResolver` 반영 — 실제로 쓰는 지형 타입이 생기면 착수
6. 엘프-활/드워프-석궁 종족별 무기 접근 제한 — 데이터만 수정하면 되는 작업, 보류 중
7. 보조무기 페이퍼돌 미지원 — LPC 에셋 한계. "그림 없이 게임 규칙만 살리는" 절충안 검토 가능
8. 남은 자잘한 정리 — 인코딩(CP949) 일부 파일 잔존 여부 재확인
9. 월드맵↔배틀맵 전환, 캠페인 모드 — 전부 미착수, 우선순위 낮음 재확인
10. 정교한 경로탐색(우선순위 큐) 미적용, 근사치로 대체 중 (v2에서 이어짐, 미해결)
