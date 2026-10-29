player_path = 'Assets/Scripts/Player/PlayerController.cs'
with open(player_path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add armorPerKill variable
s1 = '        public float damageMultiplier = 1.0f;\n'
r1 = '        public float damageMultiplier = 1.0f;\n        public float armorPerKill = 0f;\n'
assert s1 in code, 's1 not found'
code = code.replace(s1, r1, 1)

# 2. Add armor on kill logic to HandleEnemyKilled
s2 = '''        private void HandleEnemyKilled()
        {
            if (equippedUltimate != null && equippedUltimate.isKillBasedRecharge)'''

r2 = '''        private void HandleEnemyKilled()
        {
            if (armorPerKill > 0f && !isDead)
            {
                AddArmor(armorPerKill);
                if (debugMode) Debug.Log($"<color=cyan>[Armor Siphon]</color> +{armorPerKill} Armor on kill! Current Armor: {currentArmor}");
            }

            if (equippedUltimate != null && equippedUltimate.isKillBasedRecharge)'''

assert s2 in code, 's2 not found'
code = code.replace(s2, r2, 1)

# 3. Add reserve ammo grant when weapon is first unlocked
s3 = '''            unlockedWeapons.Add(newGun);

            if (WeaponWheelToolkitManager.Instance != null)'''

r3 = '''            unlockedWeapons.Add(newGun);

            // Grant starting reserve ammo for the newly unlocked weapon (2 full magazines)
            if (!reserveAmmo.ContainsKey(newGun.category) || reserveAmmo[newGun.category] == 0)
            {
                reserveAmmo[newGun.category] = newGun.magSize * 2;
            }

            if (WeaponWheelToolkitManager.Instance != null)'''

assert s3 in code, 's3 not found'
code = code.replace(s3, r3, 1)

# 4. Add UpgradeType.ArmorPerKill to ApplyUpgrade switch
s4 = '''                    case UpgradeType.WeaponUnlock:
                        if (!string.IsNullOrEmpty(effect.weaponID))
                        {
                            UnlockWeapon(effect.weaponID);
                        }
                        else
                        {
                            Debug.LogWarning("WeaponUnlock card effect is missing a Weapon ID!");
                        }
                        break;
                }'''

r4 = '''                    case UpgradeType.WeaponUnlock:
                        if (!string.IsNullOrEmpty(effect.weaponID))
                        {
                            UnlockWeapon(effect.weaponID);
                        }
                        else
                        {
                            Debug.LogWarning("WeaponUnlock card effect is missing a Weapon ID!");
                        }
                        break;

                    case UpgradeType.ArmorPerKill:
                        armorPerKill += effect.upgradeValue;
                        if (debugMode) Debug.Log($"<color=cyan>[Upgrade]</color> Armor Per Kill increased by +{effect.upgradeValue}! Total: {armorPerKill}/kill");
                        break;
                }'''

assert s4 in code, 's4 not found'
code = code.replace(s4, r4, 1)

with open(player_path, 'w', encoding='utf-8', newline='\n') as f:
    f.write(code)

print('PlayerController.cs updated successfully!')

