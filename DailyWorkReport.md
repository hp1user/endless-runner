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

## Date: 2026-09-13

### Features Added
- **15-Second City-to-Bridge Environment Transition**:
  - Configured `bossTransitionDuration = 15f` in `LevelManager.cs`.
  - When the boss transition starts, the player runs forward through the city in the front-facing camera for 15 seconds.
  - At ~11.5s into the transition, `Bridge_1` is queued and rolls under the player at the 15-second mark, triggering `OnBridgeReached`.
- **Bridge Arrival Detection & Boss Camera Switch**:
  - `LevelManager.CheckBridgeArrival()` detects when the bridge chunk's leading edge reaches the player ($Z = 0$).
  - Once on the bridge, `GameManager.OnBridgeReached()` fires `OnBossFightStarted`.
  - `BossCameraController` switches from `standardFrontCamera` (`Cam`) to `bossBackCamera` (`Cam_1`) looking down the bridge.
  - Converts preceding chunks in front of the player to `Bridge_1` so the entire combat arena is on the bridge.
  - `EnemyManager` spawns the Boss onto the bridge at $Z = +25$, beginning combat.
- **Bridge-to-City Exit Transition**:
  - Added `PrepareCityTransition()` in `LevelManager.cs` called upon boss defeat (`HandleBossDefeated`).
  - Swaps upcoming chunks behind the player back to city ruins (`currentTheme.chunkVariants`) so the bridge ends and the city road seamlessly returns.
- **Flexible Boss Schedule System**:
  - Replaced rigid modulo formula (`currentLevel % levelsBetweenBosses == 0`) with a configurable list in `GameManager.cs`: `bossLevels = [5, 15, 20, 25, 40]`.
  - Added `EnemyDatabase.GetBossForLevel()` fallback so that if an exact level boss isn't mapped, it automatically scales an available boss instead of skipping and leaving the camera stuck.
- **Weapon Switching Fix (AR / Pistol & Hotkeys)**:
  - Fixed initialization race condition in `WeaponWheelToolkitManager.cs` by syncing `GetUnlockedWeapons()` in `Start()` and on `OpenWheel()`, ensuring slot 4 (Pistol) is always populated and selectable.
  - Updated `PlayerController.EquipWeaponFromWheel()` to immediately assign `currentWeaponData`, spawn the model, and update ammo UI.
  - Added keyboard hotkeys (`1`, `2`, `3`, `4`, `5`, `Q`, `E`) in `PlayerController.cs` for instant weapon switching at any time, even while holding AR with remaining ammo.

### Bugs Fixed & Improvements
- **Camera Lock on Level 10 Fix**: Updated `GameManager.SkipBossPhase()` to invoke `OnBossDefeated`, ensuring `BossCameraController` and `LevelManager` properly reset back to standard front camera and city theme.
- **Data Manager UI Cleanup**: Removed nested submenu in `DataManagerWindow` toolbar (changed `"Enemy / Boss"` to `"Enemy"`) and purged uninitialized dummy minion entry from `EnemyDatabase.asset`.

## Date: 2026-09-18

### Features Added
- **Boss Both Legs Crippled Animation State (`Criple Both`)**:
  - Connected the new 2D Blend Tree state in `Bug Boss.controller` (`Criple Both` at `Horizontal = 0, Vertical = 2`).
  - Updated `EnemyController.UpdateLegCrippleState()`:
    - Both Legs Broken: plays `Criple Both` (`Horizontal = 0, Vertical = 2`) with speed reduced to 0.4x (`moveSpeed * 0.4f`).
    - Left Leg Broken: plays `Cripple_L` (`Horizontal = -1, Vertical = 2`) with speed reduced to 0.75x.
    - Right Leg Broken: plays `Cripple R` (`Horizontal = 1, Vertical = 2`) with speed reduced to 0.75x.
    - Neither Broken: plays `Walk` (`Horizontal = 0, Vertical = 1`) at 1.0x base speed.
- **New Roguelike Upgrade Cards Added**:
  - Extended `UpgradeCard.cs` with `category`, `flavorText`, and `visualConcept` fields.
  - Created 8 distinct ScriptableObject card assets in `Assets/ScriptableObjects/Cards/` spanning all rarities:
    1. **Shredder Rounds** (Common, Weapon / Firepower): +15% weapon damage, -1 piercing.
    2. **Kinetic Dampeners** (Common, Mobility / Survival): +20% lane-switch speed, 0.5s shield.
    3. **Acidic Payload** (Rare, Elemental / Bullet Effects): Corrosive DoT, armor melting.
    4. **Tactical Severance** (Rare, Precision / Boss Encounters): +40% critical damage on boss limbs/joints.
    5. **Adrenaline Engine** (Epic, Skill / Ultimate Synergies): 10 streak kills reduce Ultimate cooldown by 15%.
    6. **Scrap-Shield Protocol** (Epic, Mobility / Survival): Emergency shockwave explosion at <30% HP.
    7. **Singularity Chambers** (Legendary, Elemental / High-Reward): Every 10th shot fires micro-black hole.
    8. **Dead Man's Switch** (Legendary, High-Risk / High-Reward): Caps Max HP at 50%, +150% Base Damage, heals on limb sever.
  - Added all 8 cards to `UpgradeManager.allAvailableCards` deck in `GameView_EndlessRunner.unity`.
  - Enhanced `DataManagerWindow.cs` to show color-coded rarity badges (`[COMMON]`, `[RARE]`, `[EPIC]`, `[LEGENDARY]`) in the list view and render Card Category, Effect values, and italicized Flavor Text in the 2D Card Preview.

### Bugs Fixed & Improvements
- **Bridge Transition Timing & Queue Bloat Fix**:
  - **Identified Root Cause**: The previous setup allowed 4–5 chunks to spawn, queuing the bridge chunk up to 160–200 meters away (well beyond the 100m camera far clip plane), forcing a ~20s delay before spawning and a ~60s wait before reaching the player.
  - **Immediate Horizon Placement**: `LevelManager.HandleBossTransitionStarted()` now immediately sets the horizon chunk (Chunk 2, ~60–80m away) as the `transitionBridge` chunk while keeping Chunk 0 (under player) and Chunk 1 (road ahead) as solid City. The bridge is instantly visible in the distance without popping.
  - **Dynamic Transition Speed**: Configured `transitionSpeed = 4.5f` during the bridge approach, ensuring the city sprint down towards the bridge takes a balanced ~12–15 seconds before crossing and starting the boss battle.
  - **Strict Despawn Recycling**: Clamped despawn threshold to `chunkLength + 5f` (45m) so chunks despawn promptly behind the camera, keeping exactly 3 visible chunks active at all times with zero queue bloat.

## Date: 2026-09-21

### Features & Balancing
- **Boss Staged Minion Intro Phase (12–15s)**:
  - Updated `EnemyManager.SpawnBossRoutine()`: When entering the bridge, 3 waves of swarm minions spawn across runner lanes over the first ~12 seconds before the Boss emerges at $Z = +30\text{m}$, creating pacing, combat buildup, and tension.
- **Boss 2-Lane Projectile & Danger Telegraph Attack**:
  - Every 4–8 seconds, the boss immediately fires a 2-lane wide acid barrage down the bridge (with a danger trajectory telegraph), then completely stops moving for **4.5 seconds** in post-shot recovery before resuming chase. This keeps the boss at range and gives the player uninterrupted windows to shoot limbs and clear minions.
- **Boss Health & Speed Rebalance**:
  - Configured custom speed curve in `EnemyController.cs` and `EnemyDatabase.asset`:
    - Undamaged Base speed: **0.35 m/s**
    - 1 Leg Broken: **0.25 m/s**
    - Both Legs Broken: **0.10 m/s**
  - Scaled Boss health pool to **4500 HP** (Head: 2700 HP, Left Leg: 900 HP, Right Leg: 900 HP) in `EnemyDatabase.asset`.
- **Boss Minion Forward Spawning**:
  - Updated `EnemyController.SpawnMinion()`: Mid-combat minions spawn in front of the boss, midway between the boss and player (`playerZ + (bossZ - playerZ) * 0.5f`), cleanly distributed across runner lanes (`X = -2, 0, +2`) with high charge speed (`moveSpeed = 1.5`).

### Bugs Fixed & Improvements
- **Boss Leg Cripple Animation Inversion Fix**:
  - Swapped blend tree horizontal parameter values in `EnemyController.UpdateLegCrippleState()`: shooting the Left Leg now triggers the Left Leg Cripple (`Horizontal = 1`), and shooting the Right Leg triggers the Right Leg Cripple (`Horizontal = -1`).
- **Loot & Supply Drop Clarification**:
  - Confirmed and documented that periodic Supply Crates (`LootManager.cs`) with Smart Ammo detection is the final intended design. Updated [`ProjectStatusReport.md`](file:///g:/Unity/Unity%20Project/endless-runner/ProjectStatusReport.md) accordingly.

## Date: 2026-09-25

### Bugs Fixed & Controls Tuning
- **Lane Movement Unblocked**:
  - Removed the `if (TouchManager.IsShooting) return;` block from `PlayerController.MoveLeft()` and `MoveRight()`. When a swipe gesture is recognized, `TouchManager` sets `IsShooting = false` and executes lane movement immediately.
  - Added dynamic resolution-adaptive swipe threshold (`Mathf.Clamp(Screen.width * swipeScreenPercentage, 40f, 200f)`), ensuring responsive swiping across both mouse and mobile touchscreens.
- **Single-Click Multi-Shot Bug Fix Across All Weapons**:
  - **Identified Root Cause**: In `PlayerController.HandleActions()`, `shotsFiredThisTriggerPull` was being continuously reset to `0` on every frame when the mouse/touch was not held. When clicking, the trigger release cleared the counter while the Animator's `Fire` state and transition were still playing/blending, allowing trailing animation event markers to fire 2–3 duplicate shots.
  - **Leading-Edge Trigger Reset**: `shotsFiredThisTriggerPull` is now reset exclusively on the leading edge of a new trigger press (`if (shootingInput && !wasShootingLastFrame)`).
  - **Trailing Event Suppression**: Full-auto/burst weapons immediately reject animation events once the user has released the trigger (`!shootingInput`), preventing phantom bullets during the exit transition.
  - **Fire Rate Interval Limiter**: Added minimum shot interval safety check (`minShotInterval = 1f / (activeFireRate * 1.5f)`) preventing animation event overlap from double-firing bullets faster than the weapon's design fire rate.
- **Swipe-Up Reload Reliability on Mobile/Touch**:
  - **Identified Root Causes**:
    1. When swiping up from the bottom of the screen, the initial touch often landed over the player runner model, setting `isHoldingOnPlayer = true`. Previously, `isHoldingOnPlayer` completely bypassed swipe detection unless held for 0.5s to open the wheel, dropping swipe gestures.
    2. The swipe duration window was restricted to $\le 0.3\text{s}$, causing standard thumb flick motions (~0.35–0.4s) on mobile devices to be dropped.
    3. `PlayerController` had a redundant secondary hold-time check discarding reload requests if input was held $\ge 0.4\text{s}$.
  - **Player Touch Cancellation on Drag**: If a touch starts on the player model but moves $\ge \text{swipeThreshold}$, `isHoldingOnPlayer` is automatically cancelled and the swipe event (Swipe Up Reload or Lane Swap) is executed immediately.
  - **Dynamic Swipe Timing & Cleanup**: Expanded mobile flick detection window to $\le 0.45\text{s}$ and removed redundant hold checks in `PlayerController.HandleActions()`. Reloading now activates promptly for all weapons (Pistol, AR, Shotgun, SMG, Sniper).
- **Drag-to-Aim & Full-Auto Lane Switching Isolation**:
  - In `TouchManager.cs`, flicks within $0.45\text{s}$ trigger lane shifts or reloads. Holding beyond $0.45\text{s}$ is treated exclusively as an aim-drag for continuous shooting, ensuring dragging to aim with AR/SMG never causes unwanted lane shifts.

## Date: 2026-09-26

### Features & Tools
- **Procedural Highway Chunk Studio & Generator (`ProceduralChunkEditorWindow.cs`)**:
  - Created an interactive Unity Editor Tool accessible via `Tools > Endless Runner > Procedural Chunk Studio`.
  - Added 1-Click **"Auto-Fill Highway Pack"** button that automatically scans `Assets/3d/Environment/Highway/Prefabs/` and maps assets into organized categories:
    - Side Railings & Barriers (`Side Railing.prefab`)
    - Center Dividers & Center Lights (`Divider 1-4.prefab`, `Stree Light.prefab`)
    - Street Lights & Small Poles (`Stree Light.prefab`, `Small Pole 1-4.prefab`)
    - Overhead / Big Sign Boards (`Big Sign Board.prefab`)
    - Debris, Clutter & Road Props (`Debris.prefab`, `Cone.fbx`, `block 1-2.fbx`)
    - Vehicles & Heavy Obstacles (`Obstrucle1-2.prefab`)
  - **Live Scene Preview**: Added a `🎲 Preview Random Chunk in Scene` button to instantly visualize generated chunks at $(0, 0, 0)$ in the Scene View with instant cleanup.
  - **1-Click Batch Prefab Baker**: Generates $N$ unique, clean, optimized chunk prefabs saved to `Assets/Prefabs/Chunks/Generated/` and optionally binds them directly to active `LevelThemeData` ScriptableObjects.
  - **Runtime Dynamic Chunk Support**: Added `ProceduralChunk.cs` and hooked it into `LevelManager.SpawnNextChunk()`, allowing runtime dynamic randomization on-the-fly when spawned/recycled from `PoolManager`.
- **Dual Highway (Twin Roadway) Generation**:
  - Implemented `DualHighwaySettings` in `ProceduralEnvironmentProfile.cs` and `ProceduralChunk.cs`.
  - Configured parallel background visual highway on the right side of the screen/camera view (`secondaryRoadOffset = (-10.5, 0, 5)`) alongside the playable road (`X = 0`).
  - Positioned the center median dividers/lights directly in the middle between both highways (`medianX = -5.25`).
  - Configured outer boundaries: outer left railing at $X = -15.45$, outer right railing at $X = +4.95$, and center dividers at $X = -5.25$.
  - Integrated newly pulled vehicle models (`Bus.fbx`, `Car.fbx`, `SUV.fbx`, `Van.fbx`) into the procedural clutter pool.
  - Implemented **2x Obstacle Density on Visual Highway**:
    - Debris and clutter on the visual road are generated at $2\times$ the rate of the playable highway (`visualObstacleMultiplier = 2.0f`).
    - Visual highway vehicle traffic spawn rate increased (`visualVehicleSpawnChance = 0.95f`, `minVisualVehicles = 3`, `maxVisualVehicles = 6`) with multi-lane spread across 5 visual lanes and randomized crash/parking angles to simulate a dense, abandoned highway gridlock.
    - Added dedicated **`Spawn On Playable Road`** toggles to both Debris and Vehicle rules (default: `false`), allowing the playable runner highway to remain 100% clean for dynamic obstacle/enemy spawn managers while the visual background highway populates full 2x traffic and clutter.
- **Dedicated Obstacle Manager & Pool Tab (`ProceduralChunkEditorWindow.cs`)**:
  - Added a second tab **"🚧 Obstacle Manager & Pool"** to the Procedural Studio tool.
  - Features:
    - Auto-detection and direct target binding to the scene's active `ObstacleManager`.
    - **1-Click "Add All Detected"**: Scans project assets and populates candidate obstacle prefabs (`Obstrucle1`, `Obstrucle2`, `Bus`, `Car`, `SUV`, `Van`, `Debris`, etc.) with 1 click.
    - **Active Pool Management**: Visual list with component validation badges (`✓ Ready` / `⚠️ Setup Req`), damage amounts, slot reordering, individual removal buttons (`✕`), and a 1-click **`✨ Deduplicate`** button to eliminate repeated entries.
    - **Smart Candidate Deduplication & Dismiss Filter**: Automatically filters out unconfigured raw models and visual chunk duplicates by asset name (prioritizing ready-to-use prefabs in `Assets/Prefabs/Obstrucle/`), with dedicated `⊘ Dismiss` buttons to hide unwanted candidates from the detection list.
    - **Dynamic Obstacle Rotation System (`Obstacle.cs`, `ObstacleManager.cs`)**:
      - Added configurable spawn rotation rules to `Obstacle.cs` (`baseRotation`, `randomizeYaw`, `allowFlip180`, `maxRandomYawVariation`, `fullRandom360`).
      - Vehicles (`Bus`, `Car`, `SUV`, `Van`) spawn with dynamic traffic alignment (flipping between forward $0^\circ$ and oncoming $180^\circ \pm 15^\circ$ swerve angles).
      - Debris and road clutter spawn with full $360^\circ$ randomized rotations.
      - Integrated rotation settings and toggles directly into the Studio Setup Helper.
    - **Divider & Street Light Rotation Fixes (`ProceduralChunk.cs`)**:
      - Fixed Right Side Railings / Dividers so they no longer flip $180^\circ$ and stay cleanly aligned at $0^\circ$.
      - Corrected Street Light orientations so their horizontal lamp arms overhang perpendicular towards the roadway ($+90^\circ$ on outer left shoulder and $-90^\circ / 270^\circ$ on outer right shoulder) rather than pointing straight down the road.
      - Fixed Center Median Lights so they strictly use their configured `rotationOffset` without double-adding $+90^\circ$, eliminating unwanted $180^\circ$ spins.
    - **Obstacle Classification & Lane Restriction System (`Obstacle.cs`, `ObstacleManager.cs`, `ProceduralChunkEditorWindow.cs`)**:
      - Added `ObstacleCategory` (`Standard` vs `HeavyWide`) and `ObstacleLaneRestriction` (`AnyLane`, `SidesOnly`, `CenterOnly`, `LeftOnly`, `RightOnly`) to `Obstacle.cs`.
      - Restricted heavy & wide obstacles (`Bus`, `Van`) to `SidesOnly` (Left lane $X = -2.0$ or Right lane $X = +2.0$), strictly preventing them from spawning in the Center lane ($X = 0.0$) so the player is never boxed in without an escape route.
      - Updated `ObstacleManager.SpawnObstacle()` with intelligent candidate retry logic and `IsLaneAllowed()` filtering that guarantees restricted obstacles only spawn in valid, safe lanes.
      - Enhanced the Obstacle Studio Editor tab with inline lane restriction dropdowns (`[Sides Only]`, `[Any Lane]`), category tags, and preset auto-assignment for large vehicles.
    - **Broken Divider Spacing & Anti-Clustering Rule (`ProceduralEnvironmentProfile.cs`, `ProceduralChunk.cs`)**:
      - Added `isBroken` boolean to `CategorizedAssetItem` so individual divider and railing prefabs (`Divider 2`, `Divider 3`, etc.) can be marked as damaged/broken variants.
      - Added configurable `minIntactBetweenBroken` (default: `2`) to `CenterDividerRule` and `SideRailingRule`.
      - Updated `ProceduralChunk.cs` and `ProceduralEnvironmentProfile.PickWeightedItem()` so that once a broken divider spawns, at least $N$ (default: 2) intact/normal dividers (`Divider 1`) MUST spawn before another broken piece is permitted, preventing two broken pieces from ever generating consecutively side-by-side.
