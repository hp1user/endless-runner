# Advanced Acid Spit

Mesh-based boss acid for Unity 6000.3.4f1 / URP 17.3. No VFX Graph or compute dependencies.

## Assets and integration

- `Prefabs/AcidProjectile_Advanced.prefab`: main editable projectile prefab.
- `Resources/AcidSpitAdvanced/AcidProjectile_Advanced.prefab`: runtime **variant** of that prefab; inherits edits to the main prefab.
- `Prefabs/AcidImpact_Advanced.prefab`: animated radial liquid crown with 12 ballistic mesh particles.
- `Prefabs/AcidPuddle_Advanced.prefab`: opaque irregular residue, flowing hot regions and procedural bubble rings, with six maximum vapor particles.
- `Meshes/AcidDroplet.asset`: generated asymmetric 352-triangle droplet shared by the body and supporting droplets.
- `Shaders/AcidProjectileURP.shader`: opaque URP HLSL, vertex wobble/ripples, flowing toxic colors, toon lighting, gloss and rim.
- `Shaders/AcidPuddleURP.shader`: opaque bubbling/flowing residue with corrosive edge and lifecycle erosion.
- `Shaders/AcidVaporURP.shader`: sparse soft particles; the only transparent material in this effect.

`Assets/Scripts/Enemy/BossProjectile.cs` bridges the existing boss two-lane attack to the new reusable controller. The original `Assets/Prefabs/VFX/Boss_AcidOrb.prefab` and `Assets/Resources/Boss_AcidOrb.prefab` are preserved. Damage remains 25 per shot as supplied by EnemyController. Puddles and optional ground-skimming residue are cosmetic; they add no damage-over-time mechanic. The attack still leaves one safe lane.

The controller has no player dependency. `AcidProjectileController.Launch(prefab, origin, direction, speed, arcGravity, impactPlaneZ)` supports directional fire; `LaunchAt` supports target positions. Swept sphere casts use an undeformed collision radius. Subscribe to `Impact` after launching to handle gameplay damage. Layer masks should exclude the shooter. Boss integration supplies a swept endpoint plane so missed lanes still splash near the player, and injects the scrolling road velocity into surface effects.

`AcidPool` has hard per-prefab limits: eight projectiles, eight impacts, sixteen puddles. Saturated pools skip additional effects instead of growing. Optional ground-skimming requires a downward raycast within `skimHeight`, spaces patches by `patchSpacing`, and caps patches per projectile. It defaults off for the airborne boss attack.

## Editing and rebuilding

Use **Tools > VFX > Build Advanced Acid Spit** to regenerate meshes, materials, prefabs, and the isolated scene. This resets generated asset tuning; edit the builder to retain changes through regeneration. Do not run the old Boss Acid Orb builder to configure this effect.

Quality controls: `AcidProjectileVisual.dropletCount`, `rippleIntensity`, `trailDensity`; `AcidSurfaceEffect.bubbleCount` and `vaporEnabled`. Supporting fine droplets have a 12-particle cap. Shared materials expose the requested color, flow, gloss, rim, wobble and bubbling controls. Main meshes and particles share mesh/material assets; per-instance ripple and puddle lifecycle values use MaterialPropertyBlock.

## Preview and validation

Open `Scenes/AcidSpit_VFX_Test.unity` and Play. The launcher exposes speed, interval, intensity, arc gravity, ground-skimming and auto-fire; `Fire acid` is also available in its component context menu. The camera uses the runner's 56-degree FOV, 2.2m camera height and 5.5-degree pitch; the floor has no bloom or post-processing dependency.

Editor-only regression preview: call `launcher.StartCoroutine(launcher.Validate())` in Play Mode. It captures flight, splash and residue through the test camera at 720x1280, then validates endpoint impact, collision, cleanup, skimming and pool saturation. It finishes with timeScale zero; stop Play Mode to restore edit state.

Validated on 2026-11-14:

- No C# errors; all three new shaders render without shader errors in the Editor.
- Generated assets imported with assigned meshes, materials, impact and puddle references; runtime prefab verified as Variant.
- New projectile dependency graph has no `.vfx` or `.compute` assets.
- Exactly one endpoint impact; swept collider hit at 1400 units/second.
- Surface effects return inactive after lifetime; airborne shots leave no patches; low shots create spaced patches; puddle pool accepts exactly sixteen simultaneous instances.
- Flight and splash inspected through the **actual Main Camera in the real boss encounter**, including portrait 720x1280 captures. The boss attack routine was exercised, not just a Scene view preview.

Evidence in project-root `Captures/AcidSpitAdvanced/`: `gameplay-boss-flight-final.png`, `gameplay-boss-final.png`, `test-flight-final.png`, `test-impact-final.png`, `test-puddle-final.png`, `validation.txt`.

Not verified: Android device GPU timing, a 60 FPS target, thermal behavior, or a fresh Android player build. Editor rendering is not evidence of device performance.
