# Endless Runner - Project Architecture & Summary

This document serves as a comprehensive overview of the project's codebase, architecture, and systems. It acts as a reference for future development to ensure context is maintained without needing repetitive explanations.

---

## 1. Core Systems & Managers

- **GameManager (`GameManager.cs`)**: The central brain of the game. It tracks the current level, speed, and enemy kills. It handles the wave progression (requiring X enemies killed per phase) and manages global events like `OnLevelCompleted`, `OnBossFightStarted`, `OnBossDefeated`, and `OnEnemyKilled`.
- **LevelManager (`LevelManager.cs`)**: Handles the endless runner environment. Instead of the player moving forward, `LevelManager` spawns environment "chunks" (from `LevelThemeData`) and moves them backwards along the Z-axis. It listens to `GameManager` events to dynamically switch to a `transitionBridge` chunk when a boss fight begins, creating an infinite arena.
- **PoolManager (`PoolManager.cs`)**: Handles object pooling for optimization. Used extensively for enemies, bullets, VFX, and environment chunks to avoid expensive `Instantiate` and `Destroy` calls during gameplay.
- **TouchManager (`TouchManager.cs`)**: Centralizes mobile touch inputs (swipes for movement, taps/holds for shooting).

## 2. Player Mechanics & Combat

- **PlayerController (`PlayerController.cs`)**: Handles player movement across 3 lanes (left, center, right), aiming, shooting, reloading, and health/armor tracking. It uses an Animator with complex Blend Trees for strafing and aiming. It also manages Roguelike active skills and ultimates.
- **Weapon System**:
  - **WeaponDatabase (`WeaponDatabase.cs`)**: A global singleton that loads all `WeaponData` ScriptableObjects from the Resources folder.
  - **WeaponData (`WeaponData.cs`)**: Holds stats for individual weapons (Damage, Fire Rate, Ammo, Range, Prefabs).
  - **Ammo Management**: The player has a `reserveAmmo` backpack (categorized by weapon type) and a `loadedAmmo` memory for each specific gun. Auto-reloads and auto-switches when empty.
- **AimTargetController (`AimTargetController.cs`)**: Uses `Camera.main` (driven by Cinemachine) to perform screen-to-world raycasts based on touch input. It moves an IK target for the player's weapon/arms to track, ensuring bullets accurately hit where the player taps.

## 3. Enemy & Boss Architecture

- **EnemyManager (`EnemyManager.cs`)**: Handles spawning regular enemies from the pool based on the current level's quota. It applies difficulty scaling (health, damage, speed multipliers per level). It also listens for `OnBossFightStarted` to spawn the level's specific Boss.
- **EnemyController (`EnemyController.cs`)**: The AI attached to every enemy. It calculates paths towards the player on an interval (for performance) and runs towards them. Contains the `isBoss` flag which ensures that when a boss is defeated, it triggers `GameManager.BossDefeated()` instead of a standard kill registration.
- **EnemyDatabase (`EnemyDatabase.cs`) & EnemyEntry**: ScriptableObjects defining enemy stats, visual prefabs, categories (Standard, Elite, Boss), and spawn constraints (min/max levels, specific boss target levels).

## 4. Upgrades & Roguelike Elements

- **UpgradeManager (`UpgradeManager.cs`) & UpgradeCard (`UpgradeCard.cs`)**: Manages the roguelike progression. Players receive randomized upgrade cards (stat boosts, new weapons, etc.) when certain conditions are met.
- **LootManager (`LootManager.cs`)**: Handles dropping physical loot (health, ammo crates) in the world.

## 5. User Interface (UI)

- **UIManager (`UIManager.cs`)**: Manages the HUD, displaying Player Health, Armor, and current/reserve Ammo.
- **WeaponWheelToolkitManager (`WeaponWheelToolkitManager.cs`)**: Handles the UI Toolkit implementation of the dynamic weapon selection wheel.
- **Editor Tools (`DataManagerWindow.cs`)**: A custom Unity Editor window that unifies the creation and modification of `UpgradeCards` and `WeaponData` in a single interface.

## 6. Camera & Visuals
- **Cinemachine integration**: The game uses a Cinemachine camera setup. Raycasting for shooting (`AimTargetController`) relies on `Camera.main` which represents the Cinemachine brain. 
- **VAT (Vertex Animation Textures)**: Enemies utilize `VATInstanceController.cs` for highly performant, GPU-instanced animations instead of standard Unity Animators, allowing hundreds of enemies on screen.
