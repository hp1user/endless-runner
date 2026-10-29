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
- **Weapon Categories:** Pistol, Assault Rifle, Shotgun, SMG, Sniper, Heavy/LMG.
- **Weapon Wheel Toolkit (`WeaponWheelToolkitManager.cs`):**
  - Built with modern UI Toolkit (`WeaponWheel.uxml`, `WeaponWheel.uss`).
  - Uses modular 1024x1024 sprite sheet (`Assets/UI/Sprite/WeaponWheel/`) divided into 6 distinct sectors (4 weapon slots + Skill + Ultimate) and center plate.
  - Slices highlight dynamically with neon amber/cyan glow on touch or mouse hover.
  - **Dynamic Pickup Only:** Unacquired weapons are hidden and dimmed until collected via loot drops (`PlayerController.ApplyWeaponPickup()`).
  - Center hub displays weapon name, category, and real-time ammo counter.
  - Long-press (0.35s) on character or dedicated hotkeys open the wheel.

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
  - Card rarities: Common, Rare, Epic, Legendary (`CardRarityDatabase.cs`).
  - Stat bonuses: Damage Multiplier, Max HP, Armor capacity, Movement Speed, Reload Speed, Fire Rate.
  - Level-up triggers a modal 3-card selection popup (`CardsPanel`).
- **Skills & Ultimates:**
  - Buff abilities (e.g. Rapid Fire, Shields) and area damage ultimates (e.g. AOE blast with multi-collider deduplication preventing boss multi-hit exploits).

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
  8. Maintained master documentation in `ProjectSummary.md` and `DailyWorkReport.md`.
- **Next Potential Steps / Open Tasks:**
  - Play-testing in Unity Editor to evaluate in-game feel of the 20% alpha shield overlay and road chunk boss transition.
  - Testing boss combat on road chunks using `BossCheckpointSystem.JumpToBossCheckpoint(5)`.
  - Adding audio/visual particle effects when armor breaks or magazine hits low ammo warning.
