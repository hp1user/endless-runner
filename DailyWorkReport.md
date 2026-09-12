# Daily Work Report

Use this document to track daily progress, features implemented, and bugs fixed.

## Date: 2026-09-03

### Features Added
- **Boss Camera Transition**: Created `BossCameraController.cs` to smoothly toggle between standard and back-facing Cinemachine Virtual Cameras during the boss phase.
- **Project Architecture Documentation**: Compiled `ProjectArchitecture.md` as a living technical document outlining the codebase.
- **Daily Work Report**: Created this tracking document.

### Bugs Fixed & Improvements
- **Boss Spawn Timing**: Added a 2-second delay to `EnemyManager.SpawnBoss` to allow the camera and transition bridge to set up gracefully before the boss arrives.
- **Boss Defeat Logic**: Fixed a bug in `EnemyController.cs` where the boss would not properly end the phase upon dying. Implemented an `isBoss` flag to correctly invoke `GameManager.BossDefeated()`.
- **Boss Frequency Reverted**: Re-assigned `levelsBetweenBosses = 10` in `GameManager.cs` to restore standard gameplay pacing.

## Date: 2026-09-11

### Features Added
- **Boss Multi-Part Health System (1000 HP)**:
  - Created `BossBodyPart.cs` component for localized damage zones on the boss.
  - Divided total Boss health (1000 HP) across 3 parts: Head (600 HP), Left Leg (200 HP), and Right Leg (200 HP).
  - Updated `Bug Boss.prefab` and `EnemyDatabase.asset` to support the new combined health model.
- **Non-Lethal Leg Crippling & Half-Speed Chase**:
  - Boss no longer dies when both legs break.
  - Breaking both legs reduces movement speed to 50% (`moveSpeed * 0.5f`) while the boss continues to chase the player in a heavy crippled crawl locomotion (`Vertical = 2, Horizontal = 0`).
- **Boss Relentless Chase & Instakill**:
  - Removed artificial stopping distance in `EnemyController.cs`; boss chases the player indefinitely.
  - Boss reaching the player immediately calls `PlayerController.Kill()` to trigger Game Over.
- **Inverted Boss Fight Controls**:
  - Inverted lane swiping in `PlayerController.cs` while `isBossFightActive` is true to match the 180° flipped camera perspective.
- **Configurable Ultimate Damage**:
  - Added `damage` (500) and `bossDamage` (250) fields to `SkillData.cs` and `AOE.asset`.
  - Updated `PlayerController.ActivateUltimate()` so the Ultimate deals balanced damage against bosses instead of instant-killing with 9999 damage.
  - Added multi-collider deduplication (`HashSet<EnemyController>`) preventing the boss from being hit multiple times in a single activation.
- **Boss Checkpoint & Quick Battle Testing System**:
  - Created `BossCheckpointSystem.cs` to allow instant jumps to any boss level (Level 5, Level 10, etc.) without clearing wave stages.
  - Automatically simulates progression by awarding random upgrade cards (e.g. 5 cards for Level 5, 10 for Level 10) via `UpgradeManager.ApplyRandomUpgrades()`.
  - Automatically unlocks tier-appropriate weapons with ammo (Shotgun & SMG for Level 5; Assault Rifle, LMG, and Sniper for Level 10).
  - Cleans up active wave enemies via `EnemyManager.ClearAllActiveEnemies()` and immediately initiates the boss battle.
  - Added Inspector Context Menu actions (`Jump to Boss (Level 5)`, `Jump to Boss (Level 10)`) and an auto-start toggle on `GameManager.cs`.

### Bugs Fixed & Improvements
- **Broken Leg Damage Block**: Fixed a bug where `BossBodyPart.cs` returned `0f` damage once a leg was broken. Broken parts now continue passing full damage to the boss's total 1000 HP pool until the boss is defeated.
- **Premature Cripple Animation**: Fixed a bug where the cripple animation was playing on the first bullet hit while legs still had ~200 HP remaining. The boss now walks normally until a leg's durability reaches 0.
- **Cripple Animation Inversion**: Fixed an orientation mismatch in `EnemyController.cs` so shooting the Right Leg triggers Right Leg Cripple (`Horizontal = -1`), and Left Leg triggers Left Leg Cripple (`Horizontal = 1`).
- **UI Toolkit Verification**: Verified the `DataManagerWindow` editor tool is implemented with modern UI Toolkit (`CreateGUI`, UXML, USS, `TwoPaneSplitView`, and `InspectorElement` bindings).

## Date: 2026-09-12

### Features & Tuning
- **Boss Movement Speed Tuning & Progressive Leg Slow**:
  - Lowered Bug Boss base `moveSpeed` in `EnemyDatabase.asset` from `0.5` to `0.35` so the boss no longer rushes down the player too quickly.
  - Implemented progressive leg damage speed debuffs in `EnemyController.cs`:
    - 0 broken legs: 1.0x base speed (`moveSpeed`).
    - 1 broken leg (Left or Right): 0.75x speed (`moveSpeed * 0.75f`).
    - Both legs broken: 0.4x speed (`moveSpeed * 0.4f`).

- **EnemyDatabase Integration in Data Manager & Quick Boss Minions Tool**:
  - Integrated `EnemyDatabase` into `DataManagerWindow.cs` via UI Toolkit under a new `"Enemy / Boss"` tab.
  - Implemented color-coded category badges (`[STD]` in blue, `[ELITE]` in purple, `[BOSS]` in red) with inline renaming and deletion directly in the list view.
  - Built custom inspector for `EnemyEntry` handling General visuals, Spawn Rules, Chase settings, and Combat Stats with two-way Undo and dirty tracking.
  - Designed a **Quick Boss Minions Assigner**: dropdown list of pre-built enemies + `+ Add Minion` button that clones entries directly into the boss's `minionTypes` list, accompanied by mini cards displaying minion stats and removal buttons.
  - Added real-time 3D card preview rendering enemy model thumbnails directly via `AssetPreview.GetAssetPreview()` with category-themed background cards.
  - Added `EnemyEntry.Clone()` in `EnemyDatabase.cs` for clean copying and minion assignment.

### Bugs Fixed & Improvements
- **SMG & AR Bullet Offset and Leg Targeting Fix**:
  - **Reparenting Removal**: Removed `targetTransform.SetParent(hitTransform, true)` in `AimTargetController.cs`, which was corrupting `aimTarget`'s position and scale whenever a limb under the boss's 100x scaled hierarchy was targeted.
  - **Fallback Depth Correction**: Fixed `AimTargetController.cs` projection depth during boss fights. Corrected the negative `-25f` plane (which was behind the active camera at Z = -8) to positive `+20f` in front of the camera, preventing aim target dropouts when tracking moving limbs.
  - **SphereCast Fallback**: Added a 0.35f radius `SphereCast` fallback in `PlayerController.PerformRaycastHit()` for rapid-fire / full-auto weapons, ensuring continuous gunfire reliably hits moving limbs without missing through thin frame gaps.
  - **SMG Muzzle Alignment**: Updated `muzzlePosition` in `SMG.asset` from unconfigured `(0, 0, 0)` to `(0.28, 0, -0.07)` and `muzzleRotation` to `(0, 0, -90)` so bullet trails emit directly from the SMG barrel tip instead of the character's wrist.
  - **Boss Leg Collider Alignment in Prefab**: Repositioned and expanded `L_Leg` and `R_Leg` BoxColliders in `Bug Boss.prefab` to accurately encompass the animated leg bone sweep volume (`size: (1.2, 1.2, 1.5)` at `localPos: (±0.028, 0.015, -0.028)`), ensuring clicks on visual legs always connect with colliders.
