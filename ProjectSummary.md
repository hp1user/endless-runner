# Endless Runner Project Summary & Master Reference

This document serves as the master technical blueprint and living knowledge base for the **Endless Runner** project. It is structured to provide full architectural and gameplay context for developers and AI programming assistants resuming work across sessions.

---

## 📌 Quick Orientation for AI Assistants (Session Resume Guide)
> **If starting a new session or after a chat history reset:**
> 1. Read **Section 9 (Current Active Focus & Work in Progress)** below to immediately know what is currently being worked on.
> 2. Read the latest entries in [`DailyWorkReport.md`](file:///DailyWorkReport.md) for detailed day-by-day implementation notes.
> 3. Main scene: `Assets/Scenes/GameView_EndlessRunner.unity`.
> 4. Key singletons: `GameManager.Instance`, `PlayerController.Instance`, `LevelManager.Instance`, `UIManager.Instance`, `PoolManager.Instance`, `WeaponDatabase.Instance`.

---

## 1. Project Overview & Core Gameplay Loop
- **Genre:** 3D Sci-Fi / Cyberpunk Endless Runner + Shooter hybrid with Roguelike card upgrade progression.
- **Orientation & Camera:** Portrait (9:16 aspect, 1080x1920 reference). Standard Front Camera (`Cam`) is set at `Z = 10.5m, Y = 2.20m, Pitch = 5.5°, FOV = 56°` with seamless Cinemachine flipping to Boss Back Camera (`Cam_1`) during boss encounters.
- **Lanes:** 3-lane highway (`X = -2.0m, 0.0m, +2.0m`) with smooth ease-in/ease-out lane transition damping and responsive dodging.
- **Combat:** Multi-weapon shooting with raycast/spherecast detection, magazine reloading, touch/hold auto-fire, and skills/ultimates.
- **Wave Progression:** Enemies spawn according to level quotas (`GameManager.Instance.GetRequiredKillsForCurrentLevel()`). Clearing quotas advances levels, triggers card upgrade selection popups, and initiates boss encounters every 5 or 10 levels.

---

## 2. Weapon System & Modular Weapon Wheel
- **Architecture:** ScriptableObject-based weapons (`WeaponData.cs` stored in `Assets/Resources/Weapons/`). Master list managed by `WeaponDatabase.cs` Singleton.
- **Weapon Categories:** Pistol, Assault Rifle, Shotgun, SMG, Sniper, Heavy/LMG/Minigun, Rocket Launcher, Grenade.
- **Weapon Wheel Toolkit (`WeaponWheelToolkitManager.cs`):**
  - Built with modern UI Toolkit (`WeaponWheel.uxml`, `WeaponWheel.uss`).
  - Uses modular 1024x1024 sprite sheet (`Assets/UI/Sprite/WeaponWheel/`) divided into 6 distinct sectors (4 weapon slots + Skill + Ultimate) and center plate.
  - Slices highlight dynamically on touch or mouse hover.
  - **Dynamic Pickup & Wheel Activation:** Unacquired weapons remain hidden until collected via drops or unlocked via cards (`PlayerController.ApplyWeaponPickup()`, `PlayerController.UnlockWeapon()`).
  - Center hub displays weapon name, category, and real-time ammo counter.
  - Long-press (0.35s) on character or dedicated hotkeys open the wheel.
- **Droppable Weapons Suite (`LootManager.cs`):**
  - Complete drop prefab collection in `Assets/Prefabs/Items/Weapon/`: `AR.prefab` (M4), `Weapon_SMG.prefab` (smg1), `Weapon_Shotgun.prefab` (shotgun1), `Weapon_Sniper.prefab` (AWM), `Weapon_RPG.prefab` (GrandeLauncher), `Weapon_Minigun.prefab` (machineGun), `Weapon_Grenade.prefab` (Grenade).
  - Configured into `LootManager.weaponLootPool`. Collecting an already-owned weapon automatically converts into 2 magazines of reserve ammo.
- **Weapon Unlock Upgrade Cards & Boss-Exclusive Drops:**
  - Dedicated cards in `Assets/ScriptableObjects/Cards/`: `WeaponUnlock_Sniper` (Rare), `WeaponUnlock_Minigun` (Epic), `WeaponUnlock_RPG` (Legendary).
  - **Boss-Exclusive Drop Logic (`bossDropPool`):** Weapon unlock cards are completely removed from `allAvailableCards` to ensure regular level-ups only roll stat progression. They drop exclusively from Boss defeats via weighted chance (`GetWeightedBossDrop()`: Legendary RPG weight 4, Epic Minigun weight 10, Rare Sniper weight 25).
  - **Smart Unowned Prioritization:** If a player already owns a weapon, `GetWeightedBossDrop()` prioritizes unowned rare weapons/boss cards.
  - Unlocking grants the weapon directly into the weapon wheel with 2 full magazines of starting reserve ammo.
- **Smart Ammo Drop System:**
  - Supply crates dynamically inspect the player's owned weapons (`PlayerController.Instance.GetOwnedWeaponCategories()`).
  - Only ammo matching weapons currently in the player's Weapon Wheel will drop (`Ammo_AR`, `Ammo_SMG`, `Ammo_Shotgun`, `Ammo_Sniper`, `Ammo_Rocket`, `Ammo_Minigun`, `Ammo_Grenade`).
  - **Pistol Ammo Exclusion:** Pistol ammo is explicitly excluded from crate drops until pistol variants are introduced.
  - Consumable `Armor.prefab` (+25 armor) and `Health.prefab` drop from item loot pools.

---

## 3. Level Generation & Highway Chunks
- **Chunk System (`LevelManager.cs`, `ProceduralChunk.cs`):**
  - Chunks scroll along Z axis using constant pooled recycling (`PoolManager.cs`).
  - Active screen typically holds 3 chunks (Chunk 0 under player, Chunk 1 ahead, Chunk 2 at horizon).
  - 20 procedural highway chunk prefabs (`HighwayProcedural_Chunk_1` through `20`) configured via `New Highway Profile.asset`.
  - High-performance animated props and crowds powered by Vertex Animation Textures (`VATBakerWindow.cs`, `VATInstanceController.cs`).

---

## 4. Boss Battles & Road Chunk Arena Fallback
- **Multi-Part Boss Destruction (`BossBodyPart.cs`, `EnemyController.cs`):**
  - Total 1000 HP distributed across Head (600 HP), Left Leg (200 HP), and Right Leg (200 HP).
  - Progressive crippling: Breaking 1 leg slows boss to 75% speed; breaking both legs slows to 40% speed with heavy crawl locomotion without killing the boss.
  - Boss reaches the player triggers lethal contact damage.
- **Road Chunk Boss Arena (`LevelManager.cs`):**
  - When boss transition starts, the game uses regular road chunks (`currentTheme.chunkVariants`) as the combat arena instead of requiring a separate 3D bridge model.
  - Controlled via `useRoadChunksForBridge = true` on `LevelManager.cs`.
  - Cinematic 15-second approach run along the road builds anticipation before the 180° camera flip and boss spawn occur.
  - When the boss is defeated, regular road generation seamlessly resumes.
  - Optional `customBossRoadChunk` slot available on `LevelManager` if a dedicated flat chunk is preferred.
  - When a final 3D bridge model is created, it can simply be plugged into `transitionBridge` on `LevelThemeData` and `useRoadChunksForBridge` toggled to `false`.

---

## 5. Sci-Fi HUD Architecture (Side Wings HUD)
- **Builder Tool:** `SciFiHUDBuilder.cs` in `Assets/Scripts/Editor/HUD/` creates and wires the entire Canvas UI.
- **Compact 80px Dock Dimensions:** Reduced dock width from 110px to **80px** (`dockWidth: 80` in `Default_SciFi_Cyan.asset`), centered around $X = 40\text{px}$ with a sleek 24px track, leaving maximal field-of-view for runner gameplay.
- **Wuthering Waves Single Overlapping Bar System (Updated 2026-10-29):**
  - **Left Dock (Vitals):**
    - Single centered vertical track (24px width).
    - **Base Layer:** Solid Health Bar Fill (**Rich Grounded Emerald Green `#27AE60`**, 100% opaque, 16px inner width) + Health Ghost Damage Lag (`#C0392B`).
    - **Overlapping Layer:** Translucent Armor Bar Fill (**Tactical Sapphire Blue `#3182CE`**, **20% alpha**, full 24px width) allowing the emerald green health bar below to clearly show through while visibly framing and overlapping it.
    - **Dynamic Armor Frame (Border Highlight):** Sliced 9-slice bracket (`Armor_Frame`, `HUD_Bar_Bracket.png`) with a clean 2px steel-blue border (`#4A90E2`) that dynamically tracks fill height via `UIManager.UpdateCapPosition()`, framing the active shield section just like Wuthering Waves.
    - **Dynamic Armor Cap:** Clean steel-blue horizontal cap indicator (`Armor_Cap`, 26px width x 4px height, `#6BA4E8`) marking the shield boundary.
    - Symmetrical `Icon_Heart` (emerald green) and `Icon_Shield` (tactical blue) above; dual numbers (`100` HP in green, `100` Armor in blue) below.
  - **Right Dock (Ammo):**
    - Single centered vertical track (24px width).
    - **Base Layer:** Max / Reserve Ammo Fill (**Grounded Dark Bronze Amber `#8C4F1A`**, 16px inner width).
    - **Overlapping Layer:** Translucent Magazine Ammo Fill (**Warm Golden Brass `#D4973B`**, **20% alpha**, full 24px width) allowing the darker bronze reserve bar below to remain visible.
    - **Dynamic Ammo Frame (Border Highlight):** Sliced 9-slice bracket (`Ammo_Frame`, `HUD_Bar_Bracket.png`) with a clean 2px brass border (`#D4973B`) dynamically resizing to loaded bullet height.
    - **Dynamic Ammo Cap:** Clean brass horizontal cap indicator (`Ammo_Cap`, 26px width x 4px height) tracking remaining magazine rounds.
    - Symmetrical `Icon_Bullet` and `Icon_Reserve` (warm brass) above; dual numbers (`30` Mag in pure white `#FFFFFF`, `120` Reserve in warm brass `#D4973B`) below.
- **Dock Finish & De-Neonized Metallic Borders:**
  - Dock background panels (`HUD_Vertical_Dock_Left.png`, `HUD_Vertical_Dock_Right.png`) upgraded to matte charcoal slate chassis with **refined metallic cool-steel borders (`#6A7B90` / `[95, 110, 128]`)**, completely eliminating fluorescent neon glare on the dock perimeters.
  - Vitals header matched to clean metallic slate (`#B3C7DC`), Ammo header matched to warm bronze-slate (`#D1B899`).
- **Safety Backups:**
  - Script backups: `SciFiHUDBuilder.cs.backup2`, `UIManager.cs.backup2`, `Default_SciFi_Cyan.asset.backup2`.
  - Sprite backups: `HUD_Vertical_Dock_Left.png.backup`, `HUD_Vertical_Dock_Right.png.backup`.
  - Scene GameObject backup: `SideWings_HUD_4Bars_Backup` preserved inactive in `GameView_EndlessRunner.unity`.

---

## 6. Roguelike Card Upgrades & Skills
- **Upgrade System (`UpgradeManager.cs`, `UpgradeCard.cs`, `UpgradeCardUI.cs`):**
  - Card rarities: Common, Uncommon, Rare, Epic, Legendary (`CardRarityDatabase.cs`).
  - Stat bonuses: Damage Multiplier, Max HP, Armor capacity, Movement Speed, Reload Speed, Fire Rate, Weapon Unlocks, and **Armor Per Kill (`ArmorPerKill`)**.
  - **Armor-Gain Upgrade Cards:**
    - `Armor Siphon.asset` (**Rare**): Grants **+2 Armor per enemy kill**.
    - `Armor Matrix.asset` (**Epic**): Grants **+5 Armor per enemy kill** (also available in `bossDropPool`).
  - **Weapon Unlock Cards (Boss-Exclusive Drops):**
    - Unlocks rare weapons (Sniper, Minigun, RPG) directly into the weapon wheel with starting ammo.
    - Excluded from `allAvailableCards` and housed exclusively in `bossDropPool` with weighted drop chances and duplicate prevention.
  - Level-up triggers a modal 3-card selection popup (`CardsPanel`).
- **Skills & Ultimates:**
  - Buff abilities (e.g. Rapid Fire, Shields) and area damage ultimates (e.g. AOE blast with multi-collider deduplication preventing boss multi-hit exploits).
  - Enemy kills invoke `GameManager.OnEnemyKilled`, rewarding armor-on-kill and charging player ultimate meters even during boss fights.

---

## 7. Controls & Input Architecture
- **Touch Manager (`TouchManager.cs`):**
  - Full touch, swipe, and mouse support via Unity Input System.
  - Left/Right swipe: Immediate lane change (usable while shooting without cooldown locks).
  - Up swipe: Manual reload.
  - Tap / Short touch: Single-tap precision fire (1 bullet per tap for semi/burst/AR).
  - Hold (> 0.22s): Full-auto continuous fire.
  - Long-press on character (0.35s): Opens Weapon Wheel.
  - Swipes starting on the character model are prioritized as lane gestures rather than wheel triggers.
  - HUD elements are set to `raycastTarget = false` (`UIManager.MakeHUDClickThrough()`) so UI frames never block touch input.

---

## 8. Custom Editor Tools Suite
- **HUD Studio (`HUDStudioWindow.cs`):** Live theming, palette swaps, and dimension tuning for Sci-Fi HUD profiles (`HUDThemeProfile.cs`).
- **Data Manager Window (`DataManagerWindow.cs`):** UI Toolkit visual editor for Cards, Weapons, and Enemy/Boss databases.
- **Procedural Chunk Studio (`ProceduralChunkEditorWindow.cs`):** Visual generator and obstacle previewer for highway layouts.
- **Character Studio (`CharacterStudioWindow.cs`):** In-editor runner character customization and stats configuration.
- **VAT Baker (`VATBakerWindow.cs`):** Vertex animation texture generator for crowd meshes.
- **Sprite Customizer (`WeaponWheelSpriteCustomizerWindow.cs`):** Fast icon and sprite swapping for weapon wheel segments.
- **Boss Checkpoint System (`BossCheckpointSystem.cs`):** Instant play-mode jump to Level 5 or Level 10 boss fights with pre-granted cards, weapons, and ammo.

---

## 9. Current Active Focus & Work in Progress
- **Latest Work Done (2026-10-29):**
  1. Converted bridge requirement to use procedural **road chunks for boss battles** (`LevelManager.cs`).
  2. Overhauled HUD from 4 separate bars to **2 single overlapping bars** (Health base + Armor overlay; Reserve base + Mag ammo overlay).
  3. Reduced dock width to **80px** flush against screen edges.
  4. Implemented **Wuthering Waves style 20% alpha overlays** with wider 24px top bars wrapping around 16px inner base bars.
  5. Added **dynamic 9-sliced border highlight frames (`Armor_Frame`, `Ammo_Frame`)** and cap bloom lines resizing in real-time with fill ratios.
  6. **Grounded Normal Colors Overhaul**: Eliminated all loud fluorescent/neon glare:
     - Dock borders converted from neon cyan/orange to **sleek metallic cool-steel borders (`#6A7B90`)** over matte charcoal chassis.
     - Health shifted from radioactive neon lime to **rich grounded Emerald Green (`#27AE60`)**.
     - Armor shifted from electric neon cyan to **tactical Sapphire Blue (`#3182CE`)** with steel-blue frame (`#4A90E2`).
     - Ammo shifted to **warm natural Golden Brass (`#D4973B`)** and **grounded Bronze Amber (`#8C4F1A`)**.
  7. Created complete safety backups (script files, sprites, and in-scene `SideWings_HUD_4Bars_Backup`).
  8. **First Test Gameplay Systems Implemented**:
     - **Armor Gain on Kill**: Added `ArmorPerKill` upgrade type, wired `PlayerController` and `GameManager`, created `Armor Siphon` (+2 Rare) and `Armor Matrix` (+5 Epic).
     - **Universal Droppable Weapons Suite**: Created drop prefabs for all 7 weapons in `weaponLootPool` (SMG, Shotgun, Sniper, RPG, Minigun, Grenade, AR) with starting ammo conversion on duplicates.
     - **Smart Ammo Drops**: `LootManager` filters dynamic drops to match weapons currently owned in the player's Weapon Wheel. Explicitly excluded pistol ammo drops. Created consumable `Armor.prefab` (+25 armor) for item drops.
  9. **Boss-Exclusive Rare Weapon Drops & Weighted Chance**:
     - Removed weapon unlock cards from `allAvailableCards` so normal level-up progression never drops weapons.
     - Configured `UpgradeManager.bossDropPool` with `WeaponUnlock_Sniper` (Rare, wt 25), `WeaponUnlock_Minigun` (Epic, wt 10), `WeaponUnlock_RPG` (Legendary, wt 4), and `Armor Matrix` (Epic, wt 10).
     - Enhanced `DrawCards()` with duplicate card prevention in 3-card hand and smart unowned weapon prioritization.
  10. **Loot Crate Movement & Reliable Ammo Collection Overhaul**:
     - Fixed weapon supply crates frozen at the horizon (Z = -20) by standardizing all 6 weapon prefabs (Weapon_SMG, Weapon_Shotgun, Weapon_Sniper, Weapon_RPG, Weapon_Minigun, Weapon_Grenade) to have LootItem and BoxCollider directly on the root GameObject matching AR.prefab.
     - Synchronized LootItem.Update() speed dynamically with LevelManager.Instance track speed (worldMoveSpeed = 15f, transitionSpeed = 4.5f) so crates travel synchronously with the road down towards the player.
     - Fixed ammo pickup failures by expanding BoxCollider triggers on all 7 ammo prefabs to 2.0m x 2.0m x 1.0m (centered at Y = 1.0m), eliminating vertical misses and fast-frame tunneling.
     - Hardened LootItem.OnTriggerEnter with deep parent/root player checks and added OnEnable() { isCollected = false; } for clean pool reactivation.
  11. **Refined Drop Pools**: Removed RPG and Grenade from LootManager.weaponLootPool and UpgradeManager.bossDropPool for focused test gameplay (AR, SMG, Shotgun, Sniper, Minigun).
  12. **Option C (Direct Slot Replacement System) Implemented**:
     - Standardized 4 active weapon wheel slots: Slot 1 (Pistol), Slot 2 (Rifle/SMG/Sniper), Slot 3 (Shotgun), Slot 4 (Heavy: Minigun/RPG/Grenade).
     - When receiving a new weapon for an occupied slot, it cleanly replaces the existing weapon in that slot (with 2 full magazines of starting ammo), updates the weapon wheel, and equips the new gun.
     - Aligned keyboard hotkeys 1-4 directly to Slots 1-4 via SwitchToSlot().
  13. Maintained master documentation in ProjectSummary.md and DailyWorkReport.md.
- **Next Potential Steps / Open Tasks:**
  - Play-testing in Unity Editor to evaluate gameplay balance for weapon drops, ammo replenishment, and armor siphon sustainability.
  - Testing boss combat on road chunks using `BossCheckpointSystem.JumpToBossCheckpoint(5)`.
  - Adding audio/visual particle effects when armor breaks or magazine hits low ammo warning.
