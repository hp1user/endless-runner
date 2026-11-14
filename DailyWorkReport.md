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

## Date: 2026-10-17

### Features & Model Replacement
- **Player Character Model Upgrade (`Ch15_nonPBR`)**:
  - Replaced the placeholder `Man_T` character model with the newly imported high-quality sci-fi character `Ch15_nonPBR`.
  - Configured `Ch15_nonPBR.fbx` importer with `ModelImporterAnimationType.Human` and `avatarSetup = CreateFromThisModel` to generate a fully validated Humanoid Avatar (`Ch15_nonPBRAvatar`).
  - Tuned URP Lit materials (`Ch15_body.mat`, `Ch15_body1.mat`) with Specular Workflow (`_WorkflowMode = 1`), assigning Diffuse, Normal, Specular (`Ch15_1001/1002_Specular`), and Emission maps.
  - Updated player prefab (`Assets/3d/Characters/Rough/Prefab/Man_T.prefab`) and created a dedicated copy (`Assets/3d/Characters/CH15/Ch15_Player.prefab`):
    - Rigged with `Ch15_nonPBR` SkinnedMeshRenderer (65 Mixamo bones).
    - Linked `Weapon_socket` to `mixamorig:RightHand` with clean local transforms.
    - Bound `Animator` with `Ch15_nonPBRAvatar` and existing `Man Controller` (all 9 layers: Base, Aim, Rifle, Pistol, SMG, Shotgun, Sniper, Rocket, LMG working seamlessly).
    - Bound `PlayerController` (`weaponSocket`, `aimTarget`), `AimTargetController` (`targetTransform`, `rayOriginOverride`), and `CapsuleCollider`.
  - Updated the active scene `GameView_EndlessRunner.unity` where the player instance `Man_Rig` immediately displays `Ch15_nonPBR` with 0 console errors.
- **Character Creation & Testing Studio Tool Window (`CharacterStudioWindow.cs`)**:
  - Created a comprehensive editor tool accessible via `Tools > Endless Runner > Character Studio` and `Window > Character Studio`.
  - Designed with an intuitive two-pane layout adhering to the aesthetic and workflow standards of `DataManagerWindow` and `ProceduralChunkEditorWindow`.
  - **Interactive Character Roster Sidebar**:
    - Scans and lists all `CharacterData` ScriptableObjects in `Assets/ScriptableObjects/Characters/`.
    - Live search filter across character name, role/title, and ID.
    - Visual list entries featuring 3D model asset preview thumbnails, role labels, and color-coded stat badges (HP, Armor, Speed).
    - 1-click clone (`⎘`) and delete (`✕`) actions with confirmation dialogs.
    - Toolbar with `➕ New Character`, `🔄 Refresh`, and `Preload Defaults` buttons.
  - **Hero Preview & Tabbed Configuration Panel**:
    - **Hero Preview Card**: Displays real-time 3D model render via `AssetPreview`, title, description, and active skill badge chips.
    - **Tab 1: 👤 Identity & Visuals**:
      - Name, ID, Title, Bio, and UI Portrait Icon.
      - 3D Model / Prefab slot with automatic Humanoid Avatar detection.
      - Integrated 1-click **`⚡ Auto-Fix to Humanoid`** button for newly imported FBXs.
      - Optional custom Animator Controller slot (defaults to `Man Controller`).
    - **Tab 2: ⚔️ Combat Stats**:
      - Sliders for Base Health (50-1000 HP), Base Armor (0-200), Movement Speed (5-30 m/s), Speed Multiplier, Strafe Smoothing, and Lane Distance.
      - 1-click Archetype Presets: `🛡️ Tank` (220 HP, 50 Arm, 8.5 Spd), `⚡ Scout / Runner` (90 HP, 10 Arm, 13.5 Spd), `🎯 Balanced` (120 HP, 25 Arm, 10 Spd).
    - **Tab 3: ⚡ Skills & Loadout**:
      - Primary Tactical Skill & Ultimate Ability object fields with 1-click quick-assign buttons (`Rapid Fire`, `AOE Bomb`, `Clear`).
      - Inline skill summary cards rendering icon, type, cooldown/kill recharge requirement, base damage, boss damage, and duration.
      - Starting Weapon ID selector (default: `D Eagle`).
    - **Tab 4: 🎮 Live Scene Test & Standalone Export**:
      - Scene Player detection with 1-click lock and `🎯 Ping in Hierarchy`.
      - **`🎮 Assign to Current Player (Test Now)`**: Immediately hot-swaps the character model, automatically re-parents `Weapon_socket` under `mixamorig:RightHand`, rebinds `AimTargetController`, updates avatar/animator, and updates player stats/skills in real-time with full Undo support.
      - **`📦 Export Standalone Prefab`**: Automatically compiles and bakes a complete, game-ready character prefab to `Assets/Prefabs/Characters/` with all required runtime components (`PlayerController`, `CapsuleCollider`, `Rigidbody`, `AimTargetController`, `Animator`).
- **Core Runtime Architecture (`CharacterData.cs`, `PlayerController.cs`)**:
  - Created `CharacterData.cs` ScriptableObject holding character identity, model prefab, animator controller, stats, skill slots, and `GetAvatar()` helper.
  - Extended `PlayerController.cs` with `currentCharacterData`, `ApplyCharacterData()`, and `SwapCharacterModel()` with automatic prefab instance unpacking in Edit Mode.
- **Preconfigured Character Profiles**:
  - `Ch15_Agent.asset`: Ch15 Cyber Agent (Vanguard Infiltrator, 125 HP, 30 Armor, 11 Speed, Rapid Fire + AOE, `Ch15_nonPBR.fbx`).
  - `Mocap_Runner.asset`: Mocap Veteran (Combat Specialist, 100 HP, 0 Armor, 10 Speed, Rapid Fire + AOE, `Man_T.fbx`).

## Date: 2026-10-17 (Part 2)

### Features & UI Overhaul
- **Stylized Sci-Fi Health, Armor & Ammo UI HUD**:
  - Replaced legacy plain TextMeshPro HUD text with a sleek, futuristic high-tech HUD inspired by modern sci-fi shooter aesthetics and user reference.
  - **Custom High-Resolution Vector/2D UI Sprites (`Assets/UI/Sprite/HUD/`)**:
    - `HUD_Panel_Bg.png`: Dark translucent glassmorphism container with rounded cyber-corners and neon cyan perimeter glow. 9-sliced (`border: 20`) for responsive mobile layouts.
    - `HUD_Bar_Track.png`: Dark sunken channel track with beveled edges and subtle sci-fi border framing.
    - `HUD_Bar_Fill.png`: Rounded glowing capsule bar fill used for smooth horizontal fill effects.
    - `Icon_Heart.png`: Neon emerald green heart icon with ambient drop-glow for the Health system.
    - `Icon_Shield.png`: Electric cyan tech crest shield icon with highlight bevel for the Armor system.
    - `HUD_Ammo_Panel.png`: Chamfered high-tech ammo card container for top-right weapon HUD.
    - `Icon_Bullet.png`: Stylized angled kinetic bullet icon.
    - All textures configured with `textureType: 8` (Sprite 2D and UI), 9-slice borders, and alpha transparency.
  - **Top-Left Player Status HUD Card (`PlayerStatusCard`)**:
    - **Health Row**: Glowing emerald heart icon, background track, red damage-lag / ghost bar, bright neon green health bar fill, and right-aligned bold numeric HP counter (`100`).
    - **Armor Row**: Electric cyan shield icon, background track, light cyan ghost bar, bright cyan armor fill, and right-aligned bold numeric armor counter (`100`).
    - Automatically hides or dims the armor row when the player has 0 armor.
  - **Top-Right Weapon & Ammo HUD Card (`AmmoHUDCard`)**:
    - **Weapon Display**: Dynamically displays currently equipped gun silhouette/icon (`WeaponData.icon`), along with stylized weapon category header (e.g., `ASSAULT RIFLE`, `D EAGLE`, `SHOTGUN`).
    - **Dynamic Ammo Counter**:
      - Current loaded ammo in prominent 34pt bold white text.
      - Sleek divider `/`.
      - Reserve ammo in 26pt amber/gold accent text (`∞` for infinite weapons).
    - **Combat Feedback & Warnings**:
      - Turns current ammo amber on low magazine (≤ 25%).
      - Turns ammo red and flashes a pulsing `⚡ RELOAD` indicator when the magazine is empty.
      - Heart icon pulses rhythmically when player health is critical (≤ 25%).
  - **Juice & AAA Combat Game-Feel (`UIManager.cs`)**:
    - Smooth horizontal bar animations using delta-time lerping.
    - Dual-layer Damage Lag ("Ghost Bar"): Upon taking damage, the primary bar shrinks instantly, while a red ghost bar holds for `0.4s` before smoothly catching up, providing punchy and readable damage feedback.
  - **Dynamic Player Integration (`PlayerController.cs`)**:
    - Wired `UpdatePlayerUI()` to feed current health, max health, current armor, and max armor from `currentCharacterData` or `playerStats`.
    - Wired `UpdateAmmoUI()` to automatically feed active weapon loaded ammo, reserve ammo, and `UpdateWeapon(currentWeaponData)` upon switching or firing.
  - **Automated HUD Builder Tool (`SciFiHUDBuilder.cs`)**:
    - 1-Click Builder tool accessible via `Tools > Endless Runner > HUD > Build All HUD Variants`.
    - Equipped with `[InitializeOnLoad]` and automatic scene verification, seamlessly instantiating the UI hierarchy, adjusting `CanvasScaler` to 1080x1920 portrait reference resolution, binding all component references to `UIManager`, and saving the scene.

- **Dual-Variant HUD System & Top-Center Boss Health Bar**:
  - **Fixed Top Bar HUD Ammo Misalignment**:
    - Corrected local coordinate anchors and offsets in `AmmoHUDCard_Top`.
    - Dedicated left column for weapon silhouette (`84x50`) and weapon title badge.
    - Dedicated right column for big current ammo (`38pt`), divider, reserve ammo (`26pt`), and centered reload prompt (`⚡ RELOAD`), eliminating any overflow below the card boundary.
  - **New Side Wings HUD Layout (`SideWings_HUD`)**:
    - Created a sleek side-docked HUD layout aligned with the left and right screen borders, keeping the entire top center open.
    - **Left Wing (`PlayerStatusCard_Side` - Vertical Vitals)**:
      - Redesigned into a sleek vertical capsule panel (`160x490`) at `(32, -80)` with cyan glowing border (`HUD_Vertical_Panel.png`).
      - Centered `VITALS` header badge at the top.
      - **Side-by-side vertical meters** with sunken channel tracks (`HUD_Vertical_Track.png`, `32x335`) and vertical fills (`HUD_Vertical_Fill.png`, bottom-to-top):
        - Left meter: Emerald Heart icon (`32x32`), red damage-lag ghost bar, neon green health fill, and horizontal `100` numeric text below.
        - Right meter: Cyan Shield icon (`32x32`), cyan ghost bar, electric cyan armor fill, and horizontal `100` numeric text below.
      - Reoriented numeric text to clean horizontal orientation under each respective bar for instant readability.
    - **Right Wing (`AmmoHUDCard_Side` - Amber Weapon Card)**:
      - Positioned at `(-32, -80)` (`300x190`) with amber border (`HUD_Ammo_Card.png`).
      - Gold `ASSAULT RIFLE` header right-aligned in top corner.
      - Centered weapon silhouette (`120x60`).
      - High-contrast ammo row at bottom: `42pt` bold white current ammo, divider, and `28pt` gold reserve ammo (`30 / 120`).
      - Pulsing red `⚡ RELOAD` indicator centered underneath.
  - **Top-Center Boss Health Bar (`Boss_HUD`)**:
    - Centered at `(0.5, 1)` at `(0, -32)` (`620x108`) fitting into the top center header (blue box in reference).
    - 9-sliced crimson/obsidian hazard frame (`HUD_Boss_Panel.png`) and glowing cyber-skull crest (`Icon_Boss_Skull.png`).
    - Glowing Boss Name header (`⚠️ BUG BOSS [LEVEL 5]`), real-time HP percentage readout (`1000 / 1000 (100%)`).
    - Fiery crimson health fill and yellow-gold damage lag ghost bar.
    - Seamlessly hooked into `EnemyManager.SpawnBossRoutine()`, `EnemyController.OnPartDamaged()`, `EnemyController.Die()`, and `GameManager.SkipBossPhase()`. Automatically appears when boss arrives on the bridge and vanishes when defeated.
  - **Instant 1-Click Variant Switcher**:
    - Users can toggle between **Side Wings HUD (Vertical Vitals)** and **Top Bar HUD** anytime without losing either setup!
    - Available via top menu:
      - `Tools > Endless Runner > HUD > Switch to Side Wings HUD (Vertical)`
      - `Tools > Endless Runner > HUD > Switch to Top Bar HUD`
      - `Tools > Endless Runner > HUD > Toggle Test Boss Health Bar`

## Date: 2026-10-17

### Bugs Fixed & Improvements
- **Smooth & Natural Lane Changing (`PlayerController.cs`)**:
  - Replaced abrupt linear lane snap (`Mathf.MoveTowards`) with natural ease-in / ease-out smooth damping (`Mathf.SmoothDamp`) tuned to ~0.30s–0.35s duration.
  - Balanced `movementSpeed` default from `10f`/`11f` down to `6.0f` across `PlayerController.cs`, `CharacterData.cs`, `PlayerDatabase.cs`, `PlayerDatabase.asset`, `Ch15_Agent.asset`, `Mocap_Runner.asset`, and `GameView_EndlessRunner.unity`.
  - Lane change now allows the character's strafe blend tree tilting animation to cleanly blend in and out during movement without abrupt snapping.

- **Weapon Wheel Hold & UI Raycast Overlap Fixes (`TouchManager.cs`, `WeaponWheelToolkitManager.cs`, `UIManager.cs`)**:
  - **Root Cause 1 (`UIToolkit` Hierarchy Disabled)**: The `UIToolkit` root GameObject in `GameView_EndlessRunner.unity` had been deactivated (`m_IsActive: 0`), preventing `UIDocument` and `WeaponWheelToolkitManager` from executing `Awake()`. As a result, `WeaponWheelToolkitManager.Instance` was `null`, throwing `NullReferenceException` inside `TouchManager.Update()`.
    - **Fix**: Re-activated `UIToolkit` GameObject and children in `GameView_EndlessRunner.unity`.
    - **Resilience**: Added lazy discovery and hierarchy auto-activation to `WeaponWheelToolkitManager.Instance` property getter and `EnsureInitialized()` guard so the wheel can never fail to initialize even if disabled in the scene.
  - **Root Cause 2 (Hard Reject on Upper Body)**: `TouchManager.requireTouchOnBottomHalf` was `true`, rejecting any click on the player character's chest, arms, shoulders, or head (above $Y = 50\%$).
    - **Fix**: Set `requireTouchOnBottomHalf = false` in both code and scene serialization.
    - Lowered `holdTimeToOpenWheel` from `0.5f` to `0.35f` for responsive long-press feel.
  - **Root Cause 3 (UI Raycast Blockers)**: 67 UI graphics under `UIManager` had `raycastTarget = true`, blocking touch inputs from reaching the 3D scene.
    - **Fix**: Implemented `UIManager.MakeHUDClickThrough()`, disabling `raycastTarget` on all decorative HUD images, tracks, cards, and texts while leaving only interactive buttons enabled (2 targets remaining).
  - **Root Cause 4 (Trigger Collider & Tagging)**: Player GameObject `Man_Rig` was `Untagged` and used a narrow trigger capsule collider ($r = 0.29\text{m}$).
    - **Fix**: Tagged `Man_Rig` with `"Player"`.
    - Upgraded `TouchManager.DidTouchPlayer()` to a 3-tier detection system:
      1. Raycast with `QueryTriggerInteraction.Collide` checking `PlayerController`, `"Player"` tag, and player layer.
      2. 0.45m radius `Physics.SphereCastAll` to capture touches around limbs and edges.
      3. Screen-space proximity fallback to the player's screen position.
- **Portrait Camera Framing & Lane Spacing Calibration (`GameView_EndlessRunner.unity`)**:
  - **Lane Distance Verification**: Confirmed `laneDistance = 2.0f` is 100% correct across all game systems:
    - Highway chunks use a 10m roadway width (`-5m` to `+5m`).
    - Obstacle vehicles spawn across lanes `{-2, 0, 2}` in `ProceduralChunk.cs` and `New Highway Profile.asset`.
    - Minion runner lanes and Boss attack projectiles use lanes `{-2, 0, 2}` in `EnemyManager.cs` and `EnemyController.cs`.
  - **Root Cause (Portrait Aspect Frustum Narrowing)**:
    - Standard camera `Cam` was positioned at `Z = 8.0m`, `Y = 1.49m`, looking completely horizontal (`rot = 0`), with Vertical FOV = `50°`.
    - In 9:16 portrait orientation (1080x1920), vertical FOV 50° yields a horizontal FOV of only `29.4°`.
    - At `Z = 8.0m`, the visible screen width was only `4.20m` (half-width `2.10m`).
    - When moving to the left or right lane (`X = ±2.0m`), the character was positioned at `viewport = 0.023` and `0.977` (pixel 25 and 1055 out of 1080), causing the character's outer body, arm, and weapon to be sliced off by the bezel.
  - **Camera Tuning & Elevation**:
    - `Cam` (Standard Front Camera): Pulled back to `pos = (0, 2.20, 10.50)` with natural `5.5°` downward pitch (`rot = 5.5, 180, 0`) and `Lens.FieldOfView = 56°`.
    - `Cam_1` (Boss Back Camera): Symmetrically updated to `pos = (0, 2.20, -10.50)`, `rot = (5.5, 0, 0)`, and `Lens.FieldOfView = 56°`.
    - `Main Camera`: Updated to `pos = (0, 2.20, 10.50)`, `rot = (5.5, 180, 0)`, `fieldOfView = 56°`.
    - Expands the visible road width at the player from `4.20m` to `6.15m`, bringing the left/right lanes to comfortable `18%` and `82%` screen positions (with ~180px safe margin from the screen borders) without clipping the character or overlapping HUD cards.

## Date: 2026-10-19

### Features Added
- **Weapon Wheel Sprite Customizer Tool (`WeaponWheelSpriteCustomizerWindow.cs`)**:
  - Built an Editor Tool window (`Tools > Endless Runner > Weapon Wheel > Sprite Customizer`) using UI Toolkit.
  - Allows quick previewing, assigning, and swapping of weapon category silhouettes, icons, and wheel slice assets without having to inspect raw ScriptableObjects.

### Bugs Fixed & Improvements
- **Level Transition Spawn Delay (`EnemyManager.cs`)**:
  - Resolved the lack of delay when transitioning into new levels.
  - Added a paced delay window between wave completions so that enemies no longer spawn instantly on top of the player when a new level starts.

- **Assault Rifle Tap Fire vs. Auto-Fire Hold (`PlayerController.cs`, `TouchManager.cs`)**:
  - **Single Tap Accuracy**: Single tap/touch on Assault Rifle now fires strictly 1 single round on release, eliminating ammo wastage (previously fired 2–3 rounds or failed to register single taps).
  - **Auto-Fire Hold Delay**: Continuous automatic fire now engages only when the touch/hold is held past `autoFireHoldDelay` (0.22s).
  - Maintained full weapon fire-rate capability once auto-fire is engaged.

- **Instant Lane Switching & Shooting in Transit (`PlayerController.cs`, `TouchManager.cs`)**:
  - **Restored Previous Fast & Fluid Feel**: Removed distance transit locks (`Mathf.Abs(targetX - transform.position.x) > 0.15f`) and cooldown timers (`laneChangeCooldownTimer = 0.35f`) in `MoveLeft()` and `MoveRight()`. Rapid consecutive swipes and quick dodges against boss orbs/obstacles are instant once again.
  - **Shooting While Changing Lanes**: Decoupled physical character movement from trigger lockout by removing `IsChangingLane` from blocking weapon fire. Players can now freely tap or hold to shoot enemies while their character is sliding across lanes.
  - **Zero Fire on Swipe**: Active swipe gestures (`TouchManager.IsSwiping`) strictly block weapon firing so that flicks to change lanes or reload never discharge accidental shots during drag or upon finger release.

- **Character Proximity Swipe Deadzone Fix (`TouchManager.cs`)**:
  - **Root Cause**: `DidTouchPlayer()` previously assigned `isHoldingOnPlayer = true` when touch began on or near the player's 3D model, causing the hold logic to intercept and ignore all swipe gesture evaluations in that screen area.
  - **Fix**: Re-architected gesture prioritization so swipe detection is evaluated first across the entire screen.
  - Swiping left or right to switch lanes, or swiping up to reload, now works seamlessly even when starting directly on or near the player character.
  - Stationary long-press (0.35s) on the character continues to open the Weapon Wheel smoothly.

## Date: 2026-10-25

### Features Added & UI Overhaul
- **Modular Weapon Wheel Sprite Sheet Integration (`WeaponWheelToolkitManager.cs`, `WeaponWheel.uxml`, `WeaponWheel.uss`)**:
  - Fully integrated user's custom modular 1024x1024 sprite sheet from `Assets/UI/Sprite/WeaponWheel/`.
  - Replaced procedural canvas vector highlight arcs with 6 independent slice layers (`slice-slot-1` through `slice-slot-4`, `slice-skill`, `slice-ultimate`) and `slice-center` (`center_plate.png`).
  - Seamlessly swaps each slice's background between its `_normal.png` texture and `_selected.png` texture (with built-in neon amber/cyan/orange glow) on hover with zero arc bleeding, distortion, or seam issues.
  - Symmetrical 8-slot angular mapping:
    - Slot 1 (Pistol): 270° – 315° (45° span, top-left)
    - Slot 2 (Assault Rifle / SMG): 315° – 360° (45° span, top-center-left)
    - Slot 3 (Shotgun): 0° – 45° (45° span, top-center-right)
    - Slot 4 (Minigun / Heavy): 45° – 90° (45° span, mid-right)
    - Ultimate: 90° – 180° (90° span, bottom-right)
    - Skill: 180° – 270° (90° span, bottom-left)
  - Anchored weapon icons and ability labels to mathematically exact sector centroids.

### Bugs Fixed & Improvements
- **Typo Fix (`Pistal.asset`)**:
  - Corrected `weaponName: Pistal` to `weaponName: Pistol` in `Pistal.asset`.
- **Eliminated Highlight Bleed & Arc Overlap**:
  - Replaced `LineCap.Round` procedural arc with native sprite slices, eliminating highlight overflow into adjacent Skill/Ultimate slots.
- **Removed Baked Icons & Duplicate Text from Abilities**:
  - Cleaned up duplicate dynamic text overlays (`RapidFire`, `AOE`) over baked icons on Skill and Ultimate segments.
- **Center Info Hub UI**:
  - Enhanced center hub styling with high-contrast weapon preview title, category styling, and weapon descriptions.
- **Weapon Pickup-Only Visibility in Weapon Wheel**:
  - **Unpicked Weapons Hidden**: Removed default fallback icons from empty slots. Slices for unacquired weapons now remain completely empty without showing gun icons until the player physically collects that weapon.
  - **Empty Slot Dimming & Lock**: Slices for unpicked weapons are dimmed (`opacity: 0.35`) and cannot be selected or equipped.
  - **Center Hub Lock Status**: Hovering over an empty weapon slot displays "Locked" / "No weapon acquired yet." without previewing unowned guns.
  - **Dynamic Unlock**: When a new weapon is picked up via loot drops (`PlayerController.ApplyWeaponPickup()`), the corresponding slot immediately activates, brightens to full opacity, displays the weapon's icon, and allows equipping.

## Date: 2026-10-29

### Features Added & System Overhauls

- **Road Chunk Boss Arena Fallback (`LevelManager.cs`, `City Ruin.asset`, `City Ruin 2.asset`)**:
  - **Bridge Model Replacement**: Because a final 3D bridge asset is not yet available, adapted `LevelManager` to dynamically generate regular highway road chunks (`currentTheme.chunkVariants`) as the boss battle arena.
  - **Toggleable Settings**:
    - `useRoadChunksForBridge = true` (default): Enables road chunk boss fight generation across all themes without requiring dedicated bridge prefabs.
    - `customBossRoadChunk`: Optional field allowing developers to specify a dedicated flat or uncluttered road chunk specifically for boss arenas.
    - `roadTransitionDuration = 15f`: Maximum fallback duration for the cinematic approach run.
  - **Smooth Pacing**: Player approaches the boss arena along an empty road stretch for ~15 seconds while upcoming chunks travel into position; once the horizon chunk reaches the player, the Boss Camera engages, the Boss spawns, and the combat phase begins.
  - **Asset Cleanliness**: Disconnected the placeholder cube blockout `Bridge_1.prefab` from `City Ruin.asset` and `City Ruin 2.asset`. When the final bridge model arrives, simply assigning it to `transitionBridge` and disabling `useRoadChunksForBridge` will restore the physical bridge arena instantly.

- **Single Overlapping Bar HUD Architecture (`SciFiHUDBuilder.cs`, `GameView_EndlessRunner.unity`)**:
  - **Overhaul from 4 Bars to 2 Single Overlapping Bars**: Replaced the previous 4-column side-wings HUD (2 parallel slender bars on each dock) with 1 single, beautifully proportioned centered bar per dock with layered depth:
    - **Left Dock (Vitals)**:
      - **Base Layer**: Health Bar Fill (`#1EF294` Neon Green, full 26px track width) + Health Damage Ghost Lag.
      - **Overlapping Layer**: Armor Bar Fill (`#26D9FF` Electric Cyan, sleek 4px inset) + Armor Ghost Lag, wrapped inside an `ArmorColumn` CanvasGroup.
      - **Visual Reading**: Players see full health with an electric cyan armor core when both are full. As damage is taken, armor depletes first, visually unveiling the green health bar underneath.
      - **Header & Footer**: Symmetrical `Icon_Heart` and `Icon_Shield` side-by-side above the single track; side-by-side numeric readouts (`100` HP in green, `100` Armor in cyan) below.
    - **Right Dock (Ammo)**:
      - **Base Layer**: Reserve / Max Ammo Bar Fill (`#D98214` Dark Amber, full 26px track width).
      - **Overlapping Layer**: Loaded Magazine Ammo Bar Fill (`#FFC425` Bright Electric Yellow/Amber, sleek 4px inset) + Fire Ghost Lag.
      - **Visual Reading**: The outer bar indicates total reserve capacity, while the inner bright bar tracks current rounds remaining in the active magazine.
      - **Header & Footer**: Symmetrical `Icon_Bullet` and `Icon_Reserve` side-by-side above the single track; side-by-side numeric readouts (`30` Mag in white, `120` Reserve in amber) below.
  - **Backups Created**:
    - Script backups: `SciFiHUDBuilder.cs.backup` and `UIManager.cs.backup`.
    - Scene GameObject backup: `SideWings_HUD_4Bars_Backup` safely duplicated and preserved inactive under `Ui Manager` in `GameView_EndlessRunner.unity`.

- **Wuthering Waves Overlapping Shield & Compact 80px Dock Refinement (`SciFiHUDBuilder.cs`, `UIManager.cs`, `Default_SciFi_Cyan.asset`)**:
  - **Compact 80px Dock Width**: Decreased dock width on both Left (`PlayerStatusCard_Side`) and Right (`AmmoHUDCard_Side`) docks from 110px down to **80px**. Centered all elements symmetrically at $X = 40\text{px}$ with a sleek track, freeing up 60px of screen space for improved action visibility.
  - **Wuthering Waves Luminous 20% Alpha Overlays & Dynamic Framing Brackets**:
    - **20% Alpha Transparency**: Increased transparency on overlapping top layers to exactly `alpha = 0.20f` (`armorOverlayCol` and `ammoOverlayCol`) so underlying solid health (neon green `#10F080`) and reserve ammo (dark amber `#D97214`) shine through clearly without color muddying.
    - **Wider Overlapping Layer Architecture**: Inner base bars (`Health_Fill`, `Reserve_Fill`) set to 16px wide (`insetX = 4px` inside 24px track). Overlapping top bars (`Armor_Fill`, `Ammo_Fill`) set to full 24px width (`insetX = 0px`), visibly overlapping and wrapping around the base bar like Wuthering Waves.
    - **Dynamic Border Highlight Framing (`Armor_Frame`, `Ammo_Frame`)**:
      - Created custom 9-sliced sprite `HUD_Bar_Bracket.png` (`spriteBorder: {x: 6, y: 6, z: 6, w: 6}`) featuring crisp 2px glowing borders with transparent center.
      - Integrated `Armor_Frame` and `Ammo_Frame` into `UIManager.cs` and `HUDElementBinding`.
      - Extended `UIManager.UpdateCapPosition()` to dynamically resize `frame.sizeDelta = new Vector2(24f, yPos)` in real time. As shield or ammo is consumed, the glowing framing bracket and glowing cap line shrink in lockstep down the bar.
    - **Dual Cap Bloom Lines**: Maintained 26px x 4px glowing horizontal cap indicators (`Armor_Cap` in luminous cyan, `Ammo_Cap` in luminous gold) at the fill boundary for intense edge bloom.
  - **Weapon Wheel Color Palette Harmonization**:
    - **Gunmetal Slate Docks**: Updated dock background sprites (`HUD_Vertical_Dock_Left.png`, `HUD_Vertical_Dock_Right.png`) to deep metallic gunmetal slate `[18, 24, 32, 230]` matching the weapon wheel slice body and backdrop.
    - **Skill Cyan Accent (`#6CE8FF`)**: Left dock outer rim and "VITALS" header matched to Weapon Wheel `.skill-title-color` (`#6CE8FF`).
    - **Weapon Gold / Amber Accent (`#FFAA33` / `#FF8855`)**: Right dock outer rim and "AMMO" header matched to Weapon Wheel `.ult-title-color` and weapon slice highlights (`#FFAA33`).
    - **Typography**: Ammo current magazine readout matched to pure white (`#FFFFFF`, same as weapon wheel `.center-title`); armor text matched to `#6CE8FF`, reserve ammo matched to `#FFAA33`.
  - **Master Continuity & Context Preservation**:
    - Overhauled and updated `ProjectSummary.md` and `DailyWorkReport.md`.
    - Future sessions can immediately reconstruct full design rationale, file locations, and state from these documents even if chat transcripts are cleared.

- **Non-Neon Grounded Colors Overhaul for HUD Docks & Health/Armor (`SciFiHUDBuilder.cs`, `Default_SciFi_Cyan.asset`, Dock Sprites)**:
  - **Eliminated Neon Dock Border Glare**:
    - Replaced the high-saturation neon cyan and neon orange borders on `HUD_Vertical_Dock_Left.png` and `HUD_Vertical_Dock_Right.png` with **refined metallic cool-steel borders (`[95, 110, 128]` to `[125, 140, 160]`, `#6A7B90`)** over a matte charcoal slate chassis.
    - Docks now look like sleek, high-grade hardware bezels that recede naturally into the scene rather than glowing fluorescent borders.
  - **Grounded, Non-Neon Health & Armor Colors**:
    - **Health**: Shifted from radioactive neon lime (`#10F080`) to **rich grounded Emerald Green (`#27AE60` / `RGB(39, 174, 96)`)**, a classic, eye-friendly medical green.
    - **Armor**: Shifted from electric neon cyan (`#6CE8FF`) to **tactical Sapphire Blue (`#3182CE` / `RGB(49, 130, 206)`)**. The 20% alpha layer provides a calm, sophisticated azure glaze over the emerald health bar.
    - **Armor Framing Bracket & Cap**: Shifted from glowing neon bloom to **clean Steel Blue (`#4A90E2`)**, providing a crisp, non-distracting 2px boundary highlight.
    - **Ammo & Reserve**: Shifted to **warm natural Golden Brass (`#D4973B`)** and **dark grounded Bronze Amber (`#8C4F1A`)**, removing all fluorescent yellow glare.
    - **Headers & Icons**: "VITALS" header and "AMMO" header matched to muted metallic slate (`#B3C7DC` and `#D1B899`); Heart and Shield icons tinted to match the emerald green and tactical sapphire blue.
  - **Backups Preserved**: `Default_SciFi_Cyan.asset.backup2`, `SciFiHUDBuilder.cs.backup2`, `UIManager.cs.backup2`.

- **First Test Gameplay Mechanics: Armor on Kill, Droppable Weapons, and Smart Ammo System (`UpgradeCard.cs`, `PlayerController.cs`, `GameManager.cs`, `LootManager.cs`, Scene Prefabs)**:
  - **1. Armor Gain on Kill System**:
    - **Enum Extension**: Added `ArmorPerKill` to `UpgradeType` in `UpgradeCard.cs`.
    - **Player Logic**: Added `armorPerKill` tracking field to `PlayerController.cs`. Updated `ApplyUpgrade` to accumulate armor-per-kill bonuses and wired `HandleEnemyKilled()` to invoke `AddArmor(armorPerKill)` on every enemy defeat.
    - **GameManager Event Decoupling**: Updated `GameManager.RegisterEnemyKill()` so that `OnEnemyKilled?.Invoke()` broadcasts across all combat states (including during boss fights and minions) while preserving quota checks.
    - **2 New Armor Upgrade Cards**:
      - `Armor Siphon.asset` (**Rare**): Grants **+2 Armor per kill** (`upgradeType = ArmorPerKill`, `upgradeValue = 2`).
      - `Armor Matrix.asset` (**Epic**): Grants **+5 Armor per kill** (`upgradeType = ArmorPerKill`, `upgradeValue = 5`).
      - Registered both cards into `UpgradeManager.allAvailableCards` and added `Armor Matrix` to `bossDropPool`.
  - **2. Universal Weapon Drops & Rare Weapon Unlock Cards**:
    - **Full Weapon Drop Prefab Suite**: Created drop prefabs with triggers, pooling, and `LootItem` (`type: Weapon`):
      - `Weapon_SMG.prefab` (SMG, ID `smg1`, 30 rounds)
      - `Weapon_Shotgun.prefab` (Shotgun, ID `shotgun1`, 16 rounds)
      - `Weapon_Sniper.prefab` (Sniper, ID `AWM`, 10 rounds)
      - `Weapon_RPG.prefab` (Rocket Launcher, ID `GrandeLauncher`, 4 rounds)
      - `Weapon_Minigun.prefab` (Minigun, ID `machineGun`, 60 rounds)
      - `Weapon_Grenade.prefab` (Grenade, ID `Grenade`, 8 rounds)
      - Plus existing `AR.prefab` (Assault Rifle, ID `M4`, 30 rounds).
    - Added all 7 weapon prefabs to `LootManager.weaponLootPool` on `ItemManager` in `GameView_EndlessRunner.unity`.
    - **Rare Weapon Unlock Upgrade Cards**:
      - `WeaponUnlock_Minigun.asset` (**Epic**): Unlocks Minigun (`machineGun`) into arsenal and weapon wheel.
      - `WeaponUnlock_RPG.asset` (**Legendary**): Unlocks Rocket Launcher (`GrandeLauncher`).
      - `WeaponUnlock_Sniper.asset` (**Rare**): Unlocks Sniper (`AWM`).
      - `WeaponUnlock_Shotgun.asset` (**Rare**): Unlocks Shotgun (`shotgun1`).
      - `WeaponUnlock_SMG.asset` (**Uncommon**): Unlocks SMG (`smg1`).
      - Registered cards in `UpgradeManager.allAvailableCards` and `bossDropPool`.
    - **Weapon Unlock Starting Ammo**: Updated `PlayerController.UnlockWeapon()` so newly unlocked weapons automatically receive 2 full magazines of starting reserve ammo upon first acquisition.
  - **3. Smart Ammo Drops for Unlocked Weapon Wheel Weapons**:
    - **Excluded Pistol Ammo**: In `LootManager.DropSupplyCrate()`, explicitly excluded `WeaponCategory.Pistol` from drop rolls until pistol variants are implemented.
    - **Dynamic Owned Weapon Detection**: Integrated `PlayerController.Instance.GetOwnedWeaponCategories()` so ammo only drops for weapons currently unlocked in the player's Weapon Wheel (e.g., if player only has AR and SMG, only AR and SMG ammo will drop).
    - **Complete Ammo Prefab Suite**: Created ammo prefabs for all weapon categories:
      - `Ammo_SMG.prefab` (SMG, 45 rounds)
      - `Ammo_Shotgun.prefab` (Shotgun, 16 shells)
      - `Ammo_Sniper.prefab` (Sniper, 10 rounds)
      - `Ammo_Rocket.prefab` (Rocket Launcher, 4 rockets)
      - `Ammo_Minigun.prefab` (Minigun, 60 rounds)
      - `Ammo_Grenade.prefab` (Grenade, 8 grenades)
      - Plus existing `Ammo_AR.prefab` (Assault Rifle, 30 rounds).
    - **Armor Consumable Drop**: Created `Armor.prefab` (+25 Armor) and populated `LootManager.itemLootPool` alongside `Health.prefab`.
    - Populated all 7 ammo prefabs into `LootManager.ammoLootPool` in `GameView_EndlessRunner.unity`.
  - **Backups Preserved**: `UpgradeCard.cs.backup`, `PlayerController.cs.backup`, `GameManager.cs.backup`, `LootManager.cs.backup`.

- **Boss-Exclusive Rare Weapon Card Drops & Weighted Chance System (`UpgradeManager.cs`, `PlayerController.cs`, `GameView_EndlessRunner.unity`)**:
  - **Card Deck Segregation**:
    - Completely removed all `WeaponUnlock` cards from `allAvailableCards` on `UpgradeManager`. Normal level-ups now strictly offer stat progression cards (Health, Damage, Speed, ArmorPerKill, etc.) and will never drop weapon unlock cards.
  - **Boss Drop Pool Configuration (`bossDropPool`)**:
    - Rare weapon unlock cards are now exclusively acquired from Boss kills:
      - `WeaponUnlock_Sniper.asset` (**Rare**, AWM, Weight: 25)
      - `WeaponUnlock_Minigun.asset` (**Epic**, Machine Gun, Weight: 10)
      - `WeaponUnlock_RPG.asset` (**Legendary**, GrandeLauncher, Weight: 4)
      - Plus `Armor Matrix.asset` (**Epic**, +5 Armor/kill, Weight: 10)
  - **Weighted Chance & Duplicate Prevention**:
    - Preserved existing weighted boss drop math in `GetWeightedBossDrop()`, allowing chance-based selection where Legendary (RPG) is rarest, followed by Epic (Minigun / Armor Matrix) and Rare (Sniper).
    - Added duplicate card prevention in `DrawCards()`, ensuring the 3 cards offered on level up or boss defeat are distinct.
  - **Smart Unowned Weapon Prioritization**:
    - Added `HasWeapon(string weaponID)` in `PlayerController.cs`.
    - Added `IsWeaponCardAlreadyOwned(UpgradeCard card)` in `UpgradeManager.cs`. When rolling `GetWeightedBossDrop()`, already-owned weapon cards are filtered out in favor of unowned boss rewards so players don't receive duplicate weapon unlock offers while unearned weapons remain.
  - **Backups Preserved**: `UpgradeManager.cs.backup`, `PlayerController.cs.backup`.

- **Bug Fix: Compilation Namespace & Camera Viewport Rendering (`UpgradeManager.cs`, `GameView_EndlessRunner.unity`)**:
  - **Namespace Resolution (`CS0103`)**: Resolved compiler error in `UpgradeManager.cs` lines 308/318 by fully qualifying `Player.Control.PlayerController.Instance` without conflicting with existing root namespace references.
  - **Camera Viewport Restoration**: Fixed a YAML formatting anomaly in `GameView_EndlessRunner.unity` where extraneous CRLF empty lines disrupted component deserialization. Cleanly re-applied `LootManager` and `UpgradeManager` pool serializations.
  - **Verification**: Verified Unity console is at **0 errors**, and captured live Game View screenshot confirming the main camera, highway environment, skybox, and character are rendering cleanly.






- **Bug Fix: Moving Weapon Supply Crates & Reliable Ammo Pickup Trigger Detection (LootItem.cs, Weapon Prefabs, Ammo Prefabs)**:
  - **1. Moving Weapon Supply Crates (Fixed Frozen Horizon Spawns)**:
    - **Root Cause**: In Weapon_SMG.prefab, Weapon_Shotgun.prefab, Weapon_Sniper.prefab, Weapon_RPG.prefab, Weapon_Minigun.prefab, and Weapon_Grenade.prefab, LootItem and BoxCollider were attached to child GameObjects rather than root. In LootManager.DropSupplyCrate(), droppedItem.GetComponent<LootItem>() returned 
ull, leaving Initialize() uncalled. Movement defaulted to Backward (Vector3.back), and LootItem.Update() moved only the child transform while the root object (with the crate mesh) stayed permanently anchored at  = -20$.
    - **Prefab Architecture Standardization**: Rebuilt all 6 weapon prefabs to duplicate the exact root component architecture of AR.prefab:
      - Root GameObject holds Transform, BoxCollider (isTrigger = true, .5\text{m} \times 2.5\text{m} \times 2.5\text{m}$), and LootItem (	ype = 3 Weapon, with respective weapon IDs and ammo categories).
      - Visual crate mesh (AmmoBoxLarge) is attached cleanly as a child Transform without conflicting colliders.
    - **Dynamic Track Speed Synchronization**: Updated LootItem.Update() to dynamically match LevelManager.Instance speed (worldMoveSpeed = 15f, or 	ransitionSpeed = 4.5f during bridge approach). Crates now travel synchronously with the road and obstacles towards the player without sliding or stalling.
    - **Pool Reactivation Safety**: Added OnEnable() { isCollected = false; } in LootItem.cs so pooled crates and items cleanly reset their state when retrieved from PoolManager.
  - **2. Reliable Ammo & Item Pickup Collection**:
    - **Root Cause**: In all Ammo_*.prefab files, BoxCollider had m_Size: {x: 1, y: 1, z: 1} with root scale (0.2, 0.2, 0.1). The resulting world trigger was only .2\text{m} \times 0.2\text{m} \times 0.1\text{m}$ resting at  = 0$. Player capsule collider starts at  = +0.055\text{m}$, creating a minuscule vertical intersection zone (< 4.5 cm) easily missed during runner gameplay. Furthermore, at 15 m/s, discrete physics detection could tunnel past the 10 cm depth slice between frames.
    - **Generous Trigger Dimensions**: Updated BoxCollider on all 7 ammo prefabs to m_Size: {x: 10, y: 10, z: 10} and m_Center: {x: 0, y: 5, z: 0}. In world coordinates, this creates a generous .0\text{m} \times 2.0\text{m} \times 1.0\text{m}$ trigger centered at  = 1.0\text{m}$, fully encompassing the running lane and character height.
    - **Armor & Health Pickup Triggers**: Configured Armor.prefab and Health.prefab to m_Size: {x: 1.5, y: 1.5, z: 2} and m_Center: {x: 0, y: 0.5, z: 0} (.0\text{m} \times 3.0\text{m} \times 2.0\text{m}$ world trigger).
    - **Bulletproof Trigger Detection**: Enhanced LootItem.OnTriggerEnter to check other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>() ?? (PlayerController.Instance != null && (other.transform.root == PlayerController.Instance.transform || other.transform.IsChildOf(PlayerController.Instance.transform)) ? PlayerController.Instance : null), completely eliminating layer-mask and child-collider detection failures.
  - **Backups Preserved**: LootItem.cs.backup.

- **Drop Pool Refinement: Removed RPG and Grenade from Drop Lists (LootManager, UpgradeManager, GameView_EndlessRunner.unity)**:
  - **LootManager Weapon Crate Pool**: Removed Weapon_RPG.prefab and Weapon_Grenade.prefab from weaponLootPool. Active drop pool now focuses on core combat weapons: Assault Rifle (AR.prefab), Submachine Gun (Weapon_SMG.prefab), Shotgun (Weapon_Shotgun.prefab), Sniper Rifle (Weapon_Sniper.prefab), and Minigun (Weapon_Minigun.prefab).
  - **UpgradeManager Boss Drop Pool**: Removed WeaponUnlock_RPG.asset from ossDropPool. Boss kills now offer WeaponUnlock_Sniper.asset, WeaponUnlock_Minigun.asset, and Armor Matrix.asset.
  - **Verification**: Verified Unity console has 0 errors.

- **Weapon Slot Architecture & Option C (Direct Category / Slot Replacement) Implemented (PlayerController.cs, WeaponWheelToolkitManager.cs)**:
  - **4-Slot Weapon Wheel Alignment**:
    - **Slot 1 (Index 0)**: Sidearm / Pistol (starterPistol - Desert Eagle).
    - **Slot 2 (Index 1)**: Rapid-Fire / Precision Rifles (AssaultRifle, SMG, Sniper).
    - **Slot 3 (Index 2)**: CQB / Scatter (Shotgun).
    - **Slot 4 (Index 3)**: Heavy Ordnance (Minigun, RocketLauncher, Grenade).
  - **Direct Slot Replacement Logic (PlayerController.UnlockWeapon)**:
    - When acquiring a new weapon:
      1. **Duplicate Gun Check**: If the player already owns the exact weapon, converts drop to ammo (+2 full reserve magazines).
      2. **Occupied Slot Detection**: Identifies target slot using WeaponWheelToolkitManager.GetSlotIndexForCategory(). If a weapon already occupies that slot (e.g., Slot 2 already has an SMG, and player picks up an Assault Rifle), the old weapon is cleanly discarded and replaced by the new weapon.
      3. **Empty Slot Acquisition**: If the slot is empty, the new weapon fills the slot directly without discarding anything.
      4. **Starting Ammo & UI Refresh**: Grants at least 2 full magazines of starting reserve ammo for the new weapon category, updates WeaponWheelToolkitManager slice visuals, and automatically equips the newly acquired weapon.
  - **1:1 Slot Keyboard Hotkey Alignment**:
    - Added SwitchToSlot(int slotIndex) in PlayerController.cs.
    - Key 1: Slot 1 (Pistol)
    - Key 2: Slot 2 (Rifle / SMG / Sniper)
    - Key 3: Slot 3 (Shotgun)
    - Key 4: Slot 4 (Heavy)
    - Q / E: Cycle through currently active weapons.
  - **Smart Ammo Drop Harmony**:
    - Discarded weapons are removed from unlockedWeapons, so GetOwnedWeaponCategories() immediately stops dropping ammo for the old weapon and starts dropping ammo for the new weapon.
  - **Backups Preserved**: PlayerController.cs.backup3, WeaponWheelToolkitManager.cs.backup.

## Date: 2026-11-13

### Features Added: Bug Death Splash & Hit VFX System + Data Manager Tool Update
- **VFX Assets Integration (`Assets/GabrielAguiarProductions/`)**:
  - Imported ToonLiquids VFX suite (prefabs, VFX Graphs, URP Shader Graphs, textures) from VFXAssets into `Assets/GabrielAguiarProductions/`.
  - Added `"com.unity.visualeffectgraph": "17.3.0"` and `"com.unity.shadergraph": "17.3.0"` to `Packages/manifest.json`.
  - Wrapped legacy camera controller in `#if ENABLE_LEGACY_INPUT_MANAGER` for New Input System compatibility.
- **Configured Death Splash & Hit VFX Matrix (`EnemyDatabase.asset`)**:
  - **Beetle / Fly Bug Death Splash**: Configured with `vfx_RadioactiveWaste_Burst_v2.prefab` (primary) and `vfx_RadioactiveWaste_Burst_v3.prefab` (variant), automatically alternating with 50/50 randomized splash bursts on death.
  - **Ground Bug Death Splash**: Configured with `vfx_RadioactiveWaste_Burst_v4.prefab`.
  - **Hit Effect for All Bugs & Boss**: Configured with `vfx_RadioactiveWaste_Burst_v5.prefab`, spawning localized radioactive burst VFX at the exact bullet raycast impact coordinate (`hit.point`, `hit.normal`).
- **Core Gameplay Integration (`EnemyController.cs`, `BossBodyPart.cs`, `PlayerController.cs`, `PoolManager.cs`)**:
  - Extended `EnemyEntry` in `EnemyDatabase.cs` with `deathVFX`, `deathVFX_Variant`, `hitVFX`, `deathVFXLifetime`, and `hitVFXLifetime`. Updated `Clone()` to pass VFX settings to spawned minions.
  - Added `SpawnDeathVFX()` and `SpawnHitVFX(Vector3 hitPoint, Vector3 hitNormal)` in `EnemyController.cs`, hooked into `Die()` and `TakeDamage()`.
  - Updated `BossBodyPart.TakeDamage()` to accept impact coordinates and trigger localized hit VFX on the Bug Boss.
  - Updated `PlayerController.PerformRaycastHit()` to forward `hit.point` and `hit.normal` to `bodyPart.TakeDamage()` and `enemy.TakeDamage()`.
  - Enhanced `PoolManager.SpawnFromPool()` to automatically call `vfx.Reinit()` / `vfx.Play()` on recycled `VisualEffect` instances.
- **Editor Tool Enhancement (`DataManagerWindow.cs`)**:
  - Added a dedicated **"VFX & Combat Effects"** section in `BuildEnemyInspector()` under the Enemy tab in `DataManagerWindow`.
  - Provides visual `ObjectField` selectors for Death VFX, Death VFX Variant, Hit VFX, and lifetime float fields with full Undo/Redo and serialized dirty tracking.
- **VFX Duplication & 25% Scaled Variants for Minions (`vfx_RadioactiveWaste_Burst_*_Small.prefab`)**:
  - Duplicated `v2`, `v3`, `v4`, and `v5` into `_Small` variants (`vfx_RadioactiveWaste_Burst_v2_Small.prefab`, `v3_Small.prefab`, `v4_Small.prefab`, `v5_Small.prefab`).
  - Scaled Transform hierarchy and VFX Graph property sheets (`BloodSize`, `GroundBloodSize`, `GroundBloodScale`, `BloodParticlesSize`, `ImpactSize`, `BloodVelocity`) by 0.75x (25% reduction).
  - Assigned `_Small` variants to standard enemies (Beetle, Bug, and Boss minion swarms) in `EnemyDatabase.asset`.
  - Preserved the full-size `v4` and `v5` prefabs for the Bug Boss encounter.
- **Track-Speed Movement Matching for Ground Splashes & Corpses (`VFXTrackMover.cs`, `vfxgraph_Liquid_v1.vfx`, `EnemyController.cs`)**:
  - Converted particle systems in `vfxgraph_Liquid_v1.vfx` to Local Space (`m_Space: 0`) so that emitted particles, liquid bursts, and ground splatters simulate relative to the GameObject Transform.
  - Created `VFXTrackMover.cs` component that dynamically reads `LevelManager.Instance` speed (`worldMoveSpeed = 15f` or `transitionSpeed = 4.5f`) and chunk direction, translating the splash backwards synchronously with the road.
  - Updated `EnemyController.SpawnDeathVFX()` to ensure `VFXTrackMover` is attached and enabled on spawned death splashes.
  - Updated `EnemyController.Update()` during death (`isDead`) so the falling corpse also slides backward synchronously with the track speed rather than moving at slow bug speed, keeping corpse and ground blood puddle anchored to the moving asphalt until they travel out of view.
- **Boss Acid Spit Attack VFX Overhaul (`BossProjectile.cs`, `EnemyController.cs`)**:
  - Replaced the placeholder primitive sphere with a multi-layered toxic **Acid Spit Orb**:
    - **Pulsating Liquid Core**: Smooth glossy orb with dynamic organic liquid wobble (`Mathf.Sin` scale pulsation) and axial tumbling rotation.
    - **High-Gloss Wet Slime Material**: URP Lit material with intense HDR neon emission (`#48FF0A`), high smoothness (0.95), and low metallic (0.05).
    - **Glowing Toxic Corona**: Camera-facing billboard quad using `Glow_02.png` with additive blending.
    - **Boiling Droplets Particle System**: Integrated particle emitter spraying sizzling green bubbles and corrosive droplets trailing behind in the -Z direction.
    - **Viscous Slime Trail**: Wide tapered `TrailRenderer` (1.3m to 0.05m) with toxic lime gradient.
    - **Detonation & Ground Splash**: When the acid orb crosses the player lane or impacts, it triggers `vfx_RadioactiveWaste_Burst_v4` on the road surface, which travels backward synchronously with the track via `VFXTrackMover`.
- **Boss Acid Orb Refinement with ToonLiquids Loop VFX (`Boss_AcidOrb.prefab`, `BossProjectile.cs`, `vfx_RadioactiveWaste_Loop_v1.prefab`)**:
  - **Removed Physical Droplets & TrailRenderer**: Removed the child sphere meshes (`SpillDroplet_L`, `SpillDroplet_R`), removed the `TrailRenderer`, and removed the custom particle emitter.
  - **Integrated `vfx_RadioactiveWaste_Loop_v1` for Dripping Droplets**:
    - Imported and configured `Assets/GabrielAguiarProductions/Prefabs/ToonLiquids/vfx_RadioactiveWaste_Loop_v1.prefab`.
    - Slowed down burst rate from `64` to `12` (`Rate: 12`, `BloodVelocity: (1.5, 3.5, 1.5)`) to produce intermittent dripping droplets that fall from the flying orb into the air.
  - **Flat Ground Effect (`Ground_Acid_Trail`)**:
    - Attached a ground-level instance of `vfx_RadioactiveWaste_Loop_v1` positioned directly on the road surface (`Y = 0`) laying flat under the moving orb.
    - Configured with `Rate: 16`, `GroundBloodScale: 4.5`, and `GroundBloodLifetime: 3.5s`, continuously laying expanding green radioactive acid puddles along the highway asphalt as the orb passes.
  - **VFX Graph Lifecycle & Rotation Decoupling (`Boss_AcidOrb.prefab`, `BossProjectile.cs`, `BossAcidOrbVisual.cs`)**:
    - Decoupled orb visual tumbling rotation from child VFX emitters by reorganizing the prefab hierarchy:
      - Root `Boss_AcidOrb` handles only trajectory translation along the highway.
      - Child `Visual_OrbMesh` contains the Sphere mesh and `BossAcidOrbVisual.cs`, isolating wobble and tumbling spin strictly to the 3D sphere.
      - Child `Dripping_Droplets` and `Ground_Acid_Trail` are independent sibling children of the root, maintaining stable, non-spinning downward and flat road orientations.
    - Updated `BossProjectile.cs` to cache `VisualEffect` instances at spawn.
    - When fading out, it hides the core orb mesh, calls `vfx.Stop()` to cease emission, and waits for the 3.5s ground puddles to finish their natural dissolution before destroying the projectile container.
  - **Restored World Space Simulation on `vfxgraph_Liquid_v1.vfx` for Moving Particle Trails**:
    - Identified why moving the orb in Editor did not leave particle trails: `vfxgraph_Liquid_v1.vfx` had previously been switched to Local Space (`m_Space: 0`), forcing all emitted particles to follow the moving transform rather than staying behind in the scene.
    - Restored `vfxgraph_Liquid_v1.vfx` to **World Space (`m_Space: 1`)** matching `VFXAssets`, enabling particles and ground puddles to remain fixed in world coordinates for 5–6 seconds as the emitter moves.
    - Preserved Local Space simulation for enemy death splashes by branching `vfxgraph_Liquid_Local.vfx` (`m_Space: 0`) for the burst prefabs (`v2` - `v5`), maintaining synchronized track scrolling via `VFXTrackMover`.
    - Preserved user's custom prefab adjustments (color overrides, custom rotations) across `Assets/Prefabs/VFX/Boss_AcidOrb.prefab` and `Assets/Resources/Boss_AcidOrb.prefab`.
- **Player Ultimate Attack VFX - Cyber-EMP Radial Energy Discharge (`PlayerUltimate_EMP.prefab`, `AOE.asset`)**:
  - **Identified & Imported VFX Assets**:
    - Analyzed the external `VFXAssets` project for AoE effects matching the user's EMP / radial energy discharge theme.
    - Selected the `ElectricOrbs` series (`vfx_Electricity_Burst_v2`), which provides intense electric cyan/blue/violet radial arcs, sizzling sparks, and central energy flash.
    - Imported dependencies cleanly without unnecessary asset bloat:
      - `Assets/GabrielAguiarProductions/Shaders/ElectricityShader.shadergraph`
      - `Assets/GabrielAguiarProductions/Prefabs/VFXGraphs/vfxgraph_Electricity_v1.vfx`
      - `Assets/GabrielAguiarProductions/Prefabs/VFXGraphs/vfxgraph_Electricity_v2_simple.vfx`
      - `Assets/GabrielAguiarProductions/Prefabs/ElectricOrbs/` prefabs.
  - **Created `PlayerUltimate_EMP.prefab`**:
    - Configured root `PlayerUltimate_EMP` at torso level (`Y = 1.2m`) with a 4.5x scale multiplier (`(4.5, 4.5, 4.5)`) to cover all 3 highway lanes, matching `effectRadius = 10m` on `AOE.asset`.
    - Tuned `vfxgraph_Electricity` properties for a dense, punchy blast: `ElectricityRate: 50`, `SparksRate: 35`, `SparksLifetime: 0.5s`, `ElectricityLifetime: 0.35s`, `Loop: false`.
    - Created lightweight `EMPFlashLight.cs` component driving a dynamic point light flash (`#00E5FF`, range 15m, intensity 6) that rapidly fades over 0.5s to dynamically illuminate the roadway and environment during detonation.
    - Created automated Editor utility `PlayerUltimatePrefabBuilder.cs` under `[MenuItem("Tools/VFX/Build or Rebuild Player Ultimate EMP Prefab")]`.
  - **Hooked into Player Ultimate Configuration (`AOE.asset`)**:
    - Assigned `PlayerUltimate_EMP.prefab` to `visualEffectPrefab` on `Assets/ScriptableObjects/Player/Skill & Ultimate/AOE.asset`.
    - Adjusted `effectDuration` from `1.0s` to `2.0s` and added `EMPScaleGrowth.cs` driving a smooth 1.2s ease-in-out radial scale expansion from torso to 10m road width with continuous crackling arcs (`Loop: true`).
- **Complete Weapon Archetypes Overhaul: Auto-Fire Fix, Shotgun Multi-Target Spread & Sniper Piercing (`WeaponData.cs`, `PlayerController.cs`, `Weapons/*.asset`)**:
  - **Auto-Fire Mechanics Fix (`PlayerController.cs`)**:
    - Removed the artificial `autoFireHoldDelay = 0.22f` stutter pause that was causing SMG and other automatic weapons to pause for nearly a quarter of a second after shot 1, making them feel like sluggish single-shot weapons.
    - Automatic weapons (`WeaponFireMode.Auto`) now fire continuously at their native `fireRate` as long as the trigger/screen is held down, while `TouchManager.IsSwiping` continues to safely protect against accidental firing during lane changes.
  - **SMG & LMG Archetype Realignment (`SMG.asset`, `LMG.asset`)**:
    - **SMG**: Upgraded magazine capacity from `15` to `40` rounds, increased bullet damage from `10` to `16`, and set fire rate to `18` shots/sec with 60 ammo per crate pickup.
    - **LMG / Minigun**: Fixed `fireMode` from `0` (`Single`) to `2` (`Auto`), replaced single-shot `100` damage with high-DPS `24` damage/bullet, expanded magazine from `10` to `80` rounds, and set cyclic rate to `16` shots/sec with 80 ammo per crate pickup.
  - **Sniper Line Piercing Damage (`WeaponData.cs`, `PlayerController.cs`, `Sniper.asset`)**:
    - Added `public bool isPiercing` property to `WeaponData`. Enabled on `Sniper.asset` (`AWM`).
    - Overhauled `PerformRaycastHit` for piercing weapons using `Physics.RaycastAll` / `Physics.SphereCastAll` sorted by distance.
    - The sniper bullet punches completely through multiple enemies and boss parts in a straight line, dealing full `260` damage (was 100) to every target penetrated and drawing a continuous high-caliber piercing trail to the horizon.
    - Adjusted Sniper mag capacity to `5` rounds with `1.2` fire rate.
  - **Shotgun Multi-Target Pellet Spread (`WeaponData.cs`, `PlayerController.cs`, `Shotgun.asset`)**:
    - Added `public int pelletCount` and `public float spreadAngle` to `WeaponData`. Configured on `Shotgun.asset` (`shotgun1`) with `pelletCount = 8` and `spreadAngle = 8.0f`.
    - `PerformRaycastHit` fires an 8-pellet cone covering multiple highway lanes, enabling simultaneous multi-target damage against swarming bugs and spreading localized hits across boss body parts.
    - Adjusted Shotgun damage to `22` damage per pellet (176 max point-blank damage) with an `8` shell magazine and `1.8` fire rate.
  - **Bug Fixes**:
    - Fixed `BossCheckpointSystem.cs` line 78 where `machineGun` (Minigun category) was granting ammo to `WeaponCategory.AssaultRifle` instead of `WeaponCategory.Minigun`.
- **Roguelike Upgrade Cards Descriptions & Stat Normalization (`Assets/ScriptableObjects/Cards/*.asset`)**:
  - Replaced all placeholder `"What does this card do?"` texts across the card deck with concise, clear in-game descriptions:
    - **Damage Boost (Rare)**: *"Increases all weapon damage output by +10%."* (`upgradeValue: 10`).
    - **Damage Boost (Epic)**: *"Increases all weapon damage output by +20%."* (`upgradeValue: 20`).
    - **Vitality Boost (Common)**: *"Increases Maximum Health by +10 and heals 10 HP immediately."* (`upgradeValue: 10`).
    - **Vitality Boost (Uncommon)**: *"Increases Maximum Health by +15 and heals 15 HP immediately."* (`upgradeValue: 15`).
    - **Vitality Boost (Rare)**: *"Increases Maximum Health by +25 and heals 25 HP immediately."* (`upgradeValue: 25`).
    - **Swift Reflexes (Common)**: *"Increases lane-switching speed by +10% for snappier dodging."* (`upgradeValue: 0.6`).
    - **Swift Reflexes (Rare)**: *"Increases lane-switching speed by +25% for lightning-fast dodging."* (`upgradeValue: 1.5`).
    - **Sidearm: Desert Eagle**: *"Unlocks the heavy Desert Eagle sidearm with high-caliber single-target stopping power."* (`weaponID: D Eagle`).
- **Boss Health Buff (+70%) & Gun Damage Rebalance (`Bug Boss.prefab`, `EnemyDatabase.asset`, `BossBodyPart.cs`, `EnemyController.cs`, `Weapons/*.asset`)**:
  - **Increased Boss Health to 1700 HP (+70% boost)**:
    - **Head**: Scaled from `600 HP` $\rightarrow$ **`1000 HP`**.
    - **Left Leg**: Scaled from `200 HP` $\rightarrow$ **`350 HP`**.
    - **Right Leg**: Scaled from `200 HP` $\rightarrow$ **`350 HP`**.
    - Total Health Pool: **1700 HP** (was 1000 HP).
    - Updated across `Bug Boss.prefab`, `EnemyDatabase.asset` (`maxHealth: 1700`), `BossBodyPart.cs` default values, and `EnemyController.cs` formulas.
  - **Rebalanced & Nerfed Gun Damage**:
    - **Sniper**: Nerfed from `250` $\rightarrow$ **`110 damage`** (takes 3-4 clean shots to break a 350 HP leg, preventing 1-shot chunking while retaining multi-enemy penetration).
    - **LMG / Minigun**: Nerfed from `24` $\rightarrow$ **`12 damage`** at 15 shots/sec (reduces DPS from 384 down to 180, rewarding sustained suppressive fire without instant melting).
    - **Shotgun**: Nerfed from `22` $\rightarrow$ **`11 damage per pellet`** (8 pellets $\times$ 11 = 88 max point-blank damage, requires 4 point-blank blasts to break a leg).
    - **SMG**: Adjusted from `16` $\rightarrow$ **`10 damage`** at 17 shots/sec (170 DPS).
    - **Assault Rifle**: Adjusted from `22` $\rightarrow$ **`14 damage`** at 13 shots/sec (182 DPS).
    - **Pistol**: Adjusted from `18` $\rightarrow$ **`12 damage`**.
    - **Rocket / Grenade**: Nerfed to `160` and `135 damage`.
- **Rarity System Overhaul for Heavy/Power Weapons (`LootManager.cs`, `PlayerController.cs`, `GameView_EndlessRunner.unity`, `WeaponUnlock_*.asset`)**:
  - **Loot Drop Rate & Weapon Weighting**:
    - Reduced `weaponDropChance` in `GameView_EndlessRunner.unity` from test override `59.4%` down to balanced `18%`.
    - Implemented archetype-weighted weapon drop tables in `LootManager.cs`:
      - Standard/Common weapons (`AR`: weight 40, `SMG`: weight 40).
      - Rare/Power weapons (`Shotgun`: weight 10, `Sniper`: weight 6, `Minigun`: weight 4).
      - Minigun is now a 4% chance on weapon drops (1 in 25 weapon crates) instead of 20% (1 in 5).
  - **Smart Ammo Drop Weighting**:
    - Weighted consumable and ammo drops so heavy weapon ammo does not flood the player:
      - Health & Armor consumables: weight 30.
      - Standard ammo (`AR`, `SMG`): weight 30.
      - Power ammo (`Shotgun`: 10, `Sniper`: 6, `Minigun`: 5, `Launcher`: 5).
      - Heavy ammo now only rolls with ~5-10% relative weight even when owned.
  - **Duplicate Heavy Weapon Pickup & Initial Unlock Ammo Balancing**:
    - In `PlayerController.UnlockWeapon`, duplicate heavy weapon pickups (`Shotgun`, `Sniper`, `Minigun`, `Rocket`, `Grenade`) now reward 1 magazine instead of 2 magazines.
    - Initial weapon unlock starting reserve ammo granted is 1 magazine for heavy weapons instead of 2 magazines.
  - **Upgrade Card Rarity Escalation**:
    - `WeaponUnlock_Shotgun.asset`: Elevated to **Rare** (Rarity: 2).
    - `WeaponUnlock_Sniper.asset`: Elevated to **Epic** (Rarity: 3) with matching Epic frame artwork and violet tint.
    - `WeaponUnlock_Minigun.asset`: Elevated to **Legendary** (Rarity: 4) with matching Legendary frame artwork and gold tint.
    - Directly links to `UpgradeManager`'s weighted boss and level-up card draw systems.
  - **Weapon Crate Drop Rate Restored to 59.4% & Standard Weapon Dominance**:
    - Restored `weaponDropChance` in `GameView_EndlessRunner.unity` and `LootManager.cs` to `59.4%` so weapon crates drop at the original high frequency.
    - Assigned `commonWeaponWeight = 50` (AR and SMG) vs `shotgunWeight = 8`, `sniperWeight = 4`, `minigunWeight = 2`.
    - AR and SMG make up **87.8% of all weapon crates** (~52% of all crates dropped overall), ensuring players regularly receive standard weapon drops and ammo refills early in runs while heavy weapons remain rare.
  - **Damage Multiplier Audit & Broken Card Rebalancing**:
    - Identified source of `37.1` damage from `14` base AR damage: `PlayerController.damageMultiplier` was at `2.65x` ($14 \times 2.65 = 37.1$).
    - Root cause: `Dead Mans Switch.asset` was granting an exorbitant `+150%` damage (`upgradeValue: 150`), and `Tactical Severance.asset` was granting `+40%` flat damage.
    - Rebalanced high-stat cards:
      - `Dead Mans Switch.asset`: Reduced from `+150%` down to `+30%` (`upgradeValue: 30`).
      - `Tactical Severance.asset`: Reduced from `+40%` down to `+20%` (`upgradeValue: 20`).
      - `Singularity Chambers.asset`: Reduced from `+25%` down to `+15%` (`upgradeValue: 15`).
    - Fixed `PlayerController.cs`: `damageMultiplier = 1.0f` and `armorPerKill = 0f` are now explicitly reset upon `InitializeStats()` and `ApplyCharacterData()`.
    - Tuned `BossCheckpointSystem.cs`: Capped simulation cards granted on checkpoint jumps to `Mathf.Clamp(targetLevel / 2, 2, 5)` instead of awarding 10-15 stacked cards.
    - Enhanced `BossBodyPart.cs` debug logging to clearly display incoming damage and part multipliers.
- **Configurable Enemy Damage Reduction System (Boss & Elite Armor/Resistance) (`EnemyDatabase.cs`, `EnemyController.cs`, `BossBodyPart.cs`, `DataManagerWindow.cs`)**:
  - **Mechanic**:
    - Added `damageReduction` (percentage 0-100%) to `EnemyEntry` in `EnemyDatabase.cs`.
    - Applied dynamically across all damage calculation pathways: $\text{effectiveDamage} = \text{damage} \times (1 - \frac{\text{damageReduction}}{100})$.
    - **Boss**: Default `40%` reduction. Base AR 14 damage deals $14 \times 0.60 = \mathbf{8.4\text{ damage}}$. Buffed 34 damage deals $34 \times 0.60 = \mathbf{20.4\text{ damage}}$.
    - **Elite**: Default `20%` reduction ($20\%$ less damage).
    - **Standard**: `0%` reduction (full damage taken).
  - **Data Manager UI Integration**:
    - Added interactive **"Damage Reduction (%)"** slider (0–100%) with live float input in `DataManagerWindow.cs` under the "Combat Stats" section.
    - Category dropdown automatically assigns default reductions (Boss $\rightarrow$ 40%, Elite $\rightarrow$ 20%) while allowing custom overrides.
    - Added live damage reduction metric to the Data Manager item preview card.
    - Saved `[Bug]Boss` in `EnemyDatabase.asset` with `damageReduction: 40`.
- **Sci-Fi UI & Typography Alignment (`MainMenu.unity`, `GameView_EndlessRunner.unity`, `UpgradeManager.cs`)**:
  - **Main Menu UI Overhaul (`MainMenu.unity`)**:
    - **Start Button Text**: Upgraded to all-caps `START` with bold tracking (`fontSize: 40`, `fontStyle: Bold`, `characterSpacing: 16`), rendered in vibrant emissive cyan (`#00E5FF`).
    - **Start Button Chassis**: Replaced raw gray box with high-tech dark slate/navy chassis (`rgba(20, 36, 56, 0.95)`), featuring dynamic cyan glow highlights on hover and active click states.
    - **Loading Panel**: Styled backdrop to deep space navy (`rgba(10, 18, 30, 0.98)`), with electric cyan progress fill and sleek dark track chassis.
  - **Next Level & Progression Banner (`GameView_EndlessRunner.unity`, `UpgradeManager.cs`)**:
    - **Level-Up Banner Text**: Replaced raw unstyled black text (`rgba(0,0,0,1)`) with high-tech sci-fi typography (`fontSize: 54`, `fontStyle: Bold`, `characterSpacing: 18`, middle-centered alignment).
    - **Dynamic Palette Formatting in `UpgradeManager.cs`**:
      - Boss Defeat: Radiant amber/gold banner `<color=#FFB830>★ BOSS DEFEATED ★</color>`.
      - Level Progression: Electric cyber cyan banner `<color=#00E5FF>LEVEL {level} REACHED</color>`.
    - **Banner Panel Backdrop**: Replaced transparent canvas with sleek rounded sci-fi chassis card (`rgba(10, 20, 36, 0.92)`, sliced rect `820x160`).
  - **Confirm Button in Card Selection (`GameView_EndlessRunner.unity`)**:
    - **Confirm Text**: Upgraded from unstyled black text to bold uppercase `CONFIRM` (`fontSize: 40`, `fontStyle: Bold`, `characterSpacing: 16`, electric cyan `#00E5FF`).
    - **Button Frame**: Replaced pure white placeholder with compact high-tech chassis (`520x90`, dark slate `rgba(20, 36, 56, 0.95)` with cyan hover glow).
  - **Game Over & Restart Polish (`GameView_EndlessRunner.unity`)**:
    - Upgraded `Game Over ..` to bold tracked `MISSION FAILED` in warning coral red (`#FF4059`).
    - Upgraded Restart button to bold tracked `RESTART` in dark chassis with cyan highlights.
  - **Upgrade Card UI Preserved**: Upgrade card prefabs and layout components remained untouched as requested.

- **Responsive Upgrade Card Layout for Portrait & Landscape (`UpgradeCardResponsiveLayout.cs`, `UpgradeManager.cs`, `GameView_EndlessRunner.unity`)**:
  - **Problem Solved**: In vertical (portrait) mode, 3 upgrade cards fit when stacked vertically. However, in horizontal (landscape) mode, fixed vertical stacking (`VerticalLayoutGroup` with 1564 total height) overflowed off the top and bottom of the screen, with the `CONFIRM` button overlapping directly on top of the bottom card.
  - **Implemented `UpgradeCardResponsiveLayout.cs` (Legacy uGUI)**:
    - **Dynamic Orientation Switching**:
      - **Vertical / Portrait (`canvasWidth <= canvasHeight`)**:
        - Keeps `VerticalLayoutGroup` enabled on `CardContainer`.
        - Stacked 1 column top-to-bottom with 36px spacing.
        - Automatically computes scale (`scale = 0.60–1.0`) based on available screen height so cards never overflow even on shorter screens.
        - Positions `CONFIRM` button cleanly below the cards (`confirmSize: 520x90`).
      - **Horizontal / Landscape (`canvasWidth > canvasHeight`)**:
        - Keeps `VerticalLayoutGroup` enabled on `CardContainer` (does NOT turn off VerticalLayoutGroup).
        - Applies compact spacing (`landscapeSpacing: 18px`).
        - Dynamically scales `CardContainer` (`scale = 0.38–0.62`) so the vertical 3-card stack fits cleanly within the horizontal screen height without overflowing off top or bottom.
        - Automatically positions `CONFIRM` button centered directly below the cards with clean vertical spacing (`confirmGap: 16px`) and zero overlap.
    - **Sub-Canvas & Sorting Order Fix (`sortingOrder: 500`)**:
      - Added a nested `Canvas` component with `overrideSorting = true` and `sortingOrder = 500` plus a `GraphicRaycaster` to `CardsPanel`.
      - Moved `CardsPanel` to the end of the `UI Manager` Canvas hierarchy (`transform.SetAsLastSibling()`).
      - Resolves the issue where `CardsPanel` was being rendered behind the HUD (`SideWings_HUD`, `TopBar_HUD`, `Boss_HUD`) and UI Toolkit panels. `CardsPanel` is now guaranteed to render on top of all HUD and UI elements.
    - **`[ExecuteAlways]` Support**: Runs live in the Unity Editor / Simulator when toggling between Portrait and Landscape via `OnRectTransformDimensionsChange()` and `Update()`.
    - **Full Canvas Stretch for `CardsPanel`**: Configured `CardsPanel` to stretch full-screen (`anchorMin: (0, 0), anchorMax: (1, 1), sizeDelta: (0, 0)`), providing an accurate responsive anchor context.
    - **`UpgradeManager.cs` Integration**: Added immediate responsive layout update hook in `DisplayCardsOnScreen()`.
  - **Camera Build Zoom Note**: Analyzed Cinemachine camera settings (`gateFitMode: Horizontal`, `FOV: 56`). Documented why vertical FOV adapts on different mobile screen aspect ratios (19.5:9 / 20:9 vs 16:9).

- **Post-Apocalyptic Abandoned Highway Vehicle Textures & Materials (`Car.prefab`, `Bus.prefab`, `SUV.prefab`, `Van.prefab`)**:
  - **Context & Diagnosis**:
    - The 4 highway obstacle vehicle prefabs (`Car.prefab`, `Bus.prefab`, `SUV.prefab`, `Van.prefab`) were rendering flat white without textures because they shared an untextured material (`Vehicles.mat`).
    - Furthermore, `Bus.fbx`, `SUV.fbx`, and `Van.fbx` lacked UV coordinate layers in their source FBX exports.
  - **UV Generation & Alignment**:
    - Enabled `generateSecondaryUV: 1` and `swapUVChannels: 1` on `Bus.fbx.meta`, `SUV.fbx.meta`, and `Van.fbx.meta`.
    - Unity automatically calculates unwrapped lightmap UV charts for all submesh parts and swaps them to primary UV channel 0 (`mesh.uv`), allowing seamless 2D texture sampling across every vehicle part (body, doors, windows, wheels, bumpers).
  - **Tailored Procedural Vehicle Textures (`Assets/3d/Environment/Vehicles/Textures/`)**:
    - Generated 4 high-resolution (1024x1024) rusted/broken apocalyptic textures featuring multi-octave noise, surface metal oxidation, peeling lacquer blister borders, vertical acid rain rust drip streaks, road grime gradients, and gouge scratches:
      - `T_Car_Rusted.png`: Weathered crimson passenger sedan with deep rust blooms, oxidized metal scratches, blistered lacquer borders, and road grime along lower quarter panels.
      - `T_Bus_Rusted.png`: Highway transit yellow with heavy vertical rust driplines descending from window seals, worn charcoal skirt trim, and dark grime on lower wheel skirts.
      - `T_SUV_Rusted.png`: Rugged matte olive drab / coyote tan with dried mud splatter cakes along lower rocker panels, deep gouges, and oxidized steel brush-guard wear.
      - `T_Van_Rusted.png`: Chalky oxidized delivery white / utility grey with orange rust blooms spreading along flat cargo sheet-metal panels, door seams, and water-channel rust.
  - **Dedicated Curved World Materials (`Assets/3d/Environment/Vehicles/Materials/`)**:
    - Created 4 dedicated materials mapped to the project'\''s curved roadway shader (`Shader Graphs/World Bend`, GUID `061a183c1e894ae4b8dbc427879d7e72`):
      - `M_Car_Rusted.mat` $\rightarrow$ `T_Car_Rusted.png`
      - `M_Bus_Rusted.mat` $\rightarrow$ `T_Bus_Rusted.png`
      - `M_SUV_Rusted.mat` $\rightarrow$ `T_SUV_Rusted.png`
      - `M_Van_Rusted.mat` $\rightarrow$ `T_Van_Rusted.png`
    - Maintained exact highway curvature compatibility (`_Curvature: -0.0015`, `_DistanceOffset: -30`) so the vehicles bend naturally with the procedural roadway.
    - Configured realistic physical response (`_Metallic: 0.15`, `_Smoothness: 0.25`).
    - Remapped FBX material slots (`Car.fbx.meta`, `Bus.fbx.meta`, `SUV.fbx.meta`, `Van.fbx.meta`) to automatically assign each vehicle's dedicated rusted material across all prefab instances.
    - Updated fallback `Vehicles.mat` to reference `T_Car_Rusted.png`.

## Date: 2026-11-14

### Features Added & Mobile Performance Optimizations
- **Mobile VFX Hardware Detection & Dynamic Throttling (`VFXPerformanceManager.cs`)**:
  - **Context & Diagnosis**:
    - Low-end mobile devices like the Samsung Galaxy Tab A7 Lite (SM-T225, Android 11) run on the PowerVR GE8320 GPU and 3GB RAM. VFX Graph computes liquid droplets, ground splatters, and bursts using GPU Compute Shaders (`DispatchCompute`). The PowerVR GE8320 architecture lacks dedicated compute throughput, dropping frame rates to single digits during heavy combat.
  - **Dynamic Hardware Profiler (`VFXPerformanceManager.cs`)**:
    - Automatically identifies low-end hardware profiles at runtime:
      - System memory $\le 3200\text{MB}$.
      - PowerVR GPUs (e.g. Rogue GE8300 / GE8320 / GE8322).
      - Entry-tier Mali GPUs (Mali-400/450/T-series, Mali-G31/G51/G52).
    - Exposes `VFXPerformanceManager.IsVFXEnabled`:
      - Automatically initializes to `false` on low-end hardware, bypassing GPU compute passes entirely and ensuring a rock-solid, smooth frame rate.
      - Automatically initializes to `true` on mid/high-tier devices (e.g., Google Pixel 7a, modern flagships).
      - Persisted via `PlayerPrefs` (`VFX_ENABLED_KEY`), allowing user settings menu toggles.
  - **Per-Target Hit VFX Rate Limiting**:
    - High fire-rate weapons (Minigun at 15 rps, Assault Rifle at 13 rps, SMG at 17 rps) previously spawned a separate VFX Graph instance on every single bullet impact, resulting in up to 30–50 concurrent compute shader instances.
    - Implemented `CanSpawnHitVFX(targetInstanceId)` with `MIN_HIT_VFX_INTERVAL = 0.10f`, capping hit VFX to a maximum of 10 bursts per second per target.
    - Added automatic dictionary cleanup via `ClearTarget(targetInstanceId)` hooked into `EnemyController.Die()`, `Despawn()`, and `ForceDespawn()`.

- **First-Hit Stutter Elimination & PSO / Compute Shader Pre-Warming (`PoolManager.cs`, `EnemyManager.cs`)**:
  - **Root Cause (Pixel 7a Stutter)**:
    - On Android devices utilizing the Vulkan or OpenGL ES graphics backend, shaders and compute kernels are compiled lazily upon the very first draw call or dispatch. Spawning the first hit or death VFX during active combat caused a 1-second pipeline compilation stall (PSO compilation freeze).
  - **Runtime Shader & Compute Pre-Warming (`PoolManager.PrewarmPool`)**:
    - Implemented `PrewarmPool(GameObject prefab, int count, bool warmupShaders)` in `PoolManager.cs`.
    - Instantiates VFX instances off-screen (`Vector3.down * 500f`) during initial scene loading and executes a single simulation frame (`vfx.Simulate(0.016f)`).
    - Forces the GPU driver to compile all vertex, fragment, and compute pipelines ahead of time while the loading screen or level begins, completely eliminating runtime first-hit stutter.
    - Enforced a hard pool safety cap (`MAX_POOL_CAP_PER_PREFAB = 12`) to prevent unbounded memory growth during intense swarming.
  - **Automated Scene Pre-Warming Integration (`EnemyManager.PrewarmVFX`)**:
    - Extended `EnemyManager.Start()` with `PrewarmVFX()`: dynamically aggregates all unique `hitVFX`, `deathVFX`, and `deathVFX_Variant` across all regular enemies and bosses in `EnemyDatabase.asset` and pre-warms their pools before waves begin.

- **Boss Fight Freeze & Crash Fixes (`BossProjectile.cs`, `BossAttackTelegraph.cs`, `EnemyController.cs`)**:
  - **Boss Attack Telegraph Material Leak Fix (`BossAttackTelegraph.cs`)**:
    - Discovered that each boss telegraph warning was executing `new Material(Shader.Find(...))` without destroying the previous instance, leaking hundreds of native material instances into memory across repeated attacks until the application ran out of graphics memory and crashed.
    - Refactored `BossAttackTelegraph.cs` to utilize a single static, lazy-initialized `sharedTelegraphMat` across all telegraph instances.
  - **Boss Projectile Pooling (`BossProjectile.cs`)**:
    - Converted dynamic `Instantiate(cachedOrbPrefab)` in `BossProjectile.cs` to `PoolManager.Instance.SpawnFromPool`.
    - In `FadeAndCleanupRoutine`, recycled expired orbs back into the pool via `PoolManager.Instance.ReturnToPool` instead of invoking `Destroy()`.
    - Guarded boss projectile detonation splash instantiation with `VFXPerformanceManager.IsVFXEnabled`.
  - **Multi-Part Hit Deduping**:
    - Prevented cascading hit-VFX triggers across multi-part boss colliders during piercing or shotgun blasts.

- **Orientation & Surface Loss Prevention (`ProjectSettings/ProjectSettings.asset`)**:
  - Disabled `allowedAutorotateToPortraitUpsideDown: 0` in project settings. Prevents accidental 180° screen flips which trigger Android Vulkan surface recreation (`onSurfaceDestroyed` / `onSurfaceCreated`) while active compute shaders are in-flight, which was causing catastrophic freezes and OS watchdog crashes.

