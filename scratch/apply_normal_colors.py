hud_path = 'Assets/Scripts/Editor/HUD/SciFiHUDBuilder.cs'
with open(hud_path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update colors in BuildVerticalSideWingsLayout
old_colors = '''            // Base colors matching Weapon Wheel palette
            Color healthCol = (theme != null) ? theme.healthColor : new Color(0.06f, 0.94f, 0.50f, 1f); // Vibrant Neon Green
            Color ghostCol = (theme != null) ? theme.ghostDamageColor : new Color(0.95f, 0.22f, 0.22f, 0.75f);
            Color reserveCol = new Color(0.85f, 0.45f, 0.08f, 0.90f); // Base Dark Amber

            // Wuthering Waves Style Overlapping Overlays & Weapon Wheel Theme
            // Skill Cyan (#6CE8FF)
            Color armorBase = (theme != null) ? theme.armorColor : new Color(0.42f, 0.91f, 1.0f, 1f);
            Color armorOverlayCol = new Color(armorBase.r, armorBase.g, armorBase.b, 0.20f); // Exactly 20% alpha translucent cyan!
            Color armorFrameCol = new Color(armorBase.r, armorBase.g, armorBase.b, 1.0f);   // 100% luminous cyan border highlight
            Color armorCapCol = new Color(0.65f, 0.95f, 1.0f, 1.0f);                        // Glowing cyan cap bloom

            // Weapon/Ult Gold (#FFAA33 / #FF8855)
            Color ammoBase = (theme != null) ? theme.ammoColor : new Color(1.0f, 0.67f, 0.20f, 1f);
            Color ammoOverlayCol = new Color(ammoBase.r, ammoBase.g, ammoBase.b, 0.20f); // Exactly 20% alpha translucent gold!
            Color ammoFrameCol = new Color(ammoBase.r, ammoBase.g, ammoBase.b, 1.0f);    // 100% luminous gold/amber border highlight
            Color ammoCapCol = new Color(1.0f, 0.95f, 0.50f, 1.0f);                         // Glowing gold cap bloom'''

new_colors = '''            // Grounded normal game colors (Calm, non-neon palette)
            Color healthCol = (theme != null) ? theme.healthColor : new Color(0.15f, 0.68f, 0.38f, 1f); // Rich Emerald/Jade Green (#27AE60)
            Color ghostCol = (theme != null) ? theme.ghostDamageColor : new Color(0.75f, 0.22f, 0.17f, 0.75f);
            Color reserveCol = new Color(0.55f, 0.31f, 0.10f, 0.90f); // Grounded Bronze Amber (#8C4F1A)

            // Tactical Armor (Deep Sapphire Blue #3182CE - non-neon)
            Color armorBase = (theme != null) ? theme.armorColor : new Color(0.19f, 0.51f, 0.81f, 1f);
            Color armorOverlayCol = new Color(armorBase.r, armorBase.g, armorBase.b, 0.20f); // 20% alpha calm azure glaze
            Color armorFrameCol = new Color(0.29f, 0.56f, 0.89f, 1.0f);   // Clean steel-blue 2px border (#4A90E2)
            Color armorCapCol = new Color(0.42f, 0.64f, 0.91f, 1.0f);     // Clean steel-blue cap line

            // Ammo (Warm Golden Brass #D4973B - non-neon)
            Color ammoBase = (theme != null) ? theme.ammoColor : new Color(0.83f, 0.59f, 0.23f, 1f);
            Color ammoOverlayCol = new Color(ammoBase.r, ammoBase.g, ammoBase.b, 0.20f); // 20% alpha warm brass glaze
            Color ammoFrameCol = new Color(ammoBase.r, ammoBase.g, ammoBase.b, 1.0f);    // Clean golden brass border
            Color ammoCapCol = new Color(0.90f, 0.70f, 0.35f, 1.0f);     // Clean brass cap line'''

assert old_colors in code, 'old_colors not found'
code = code.replace(old_colors, new_colors, 1)

# 2. Update Vitals Header Color
old_header_l = '''            // VITALS Header (Weapon Wheel Skill Cyan #6CE8FF)
            CreateTMPText("Vitals_Header", leftCardGo.transform, font, "VITALS", 9, FontStyles.Bold, new Color(0.42f, 0.91f, 1.0f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));'''

new_header_l = '''            // VITALS Header (Clean metallic steel-slate #B3C7DC matching dock bezel)
            CreateTMPText("Vitals_Header", leftCardGo.transform, font, "VITALS", 9, FontStyles.Bold, new Color(0.70f, 0.78f, 0.86f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));'''

assert old_header_l in code, 'old_header_l not found'
code = code.replace(old_header_l, new_header_l, 1)

# 3. Update Ammo Header Color
old_header_r = '''            // AMMO Header (Weapon Wheel Warm Gold/Orange #FFAA33)
            GameObject wNameGo = CreateTMPText("Weapon_Name", rightCardGo.transform, font, "AMMO", 9, FontStyles.Bold, new Color(1.0f, 0.67f, 0.20f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));'''

new_header_r = '''            // AMMO Header (Clean warm bronze-slate #D1B899 matching dock bezel)
            GameObject wNameGo = CreateTMPText("Weapon_Name", rightCardGo.transform, font, "AMMO", 9, FontStyles.Bold, new Color(0.82f, 0.72f, 0.60f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));'''

assert old_header_r in code, 'old_header_r not found'
code = code.replace(old_header_r, new_header_r, 1)

# 4. Tint icons with grounded colors
old_icons_l = '''            // Dual Icons above the bar: Heart & Shield side-by-side
            GameObject heartGo = CreateImageObject("Icon_Heart", leftCardGo.transform, heartIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            GameObject shieldGo = CreateImageObject("Icon_Shield", leftCardGo.transform, shieldIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));'''

new_icons_l = '''            // Dual Icons above the bar: Heart & Shield side-by-side
            GameObject heartGo = CreateImageObject("Icon_Heart", leftCardGo.transform, heartIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            heartGo.GetComponent<Image>().color = healthCol;
            GameObject shieldGo = CreateImageObject("Icon_Shield", leftCardGo.transform, shieldIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            shieldGo.GetComponent<Image>().color = armorBase;'''

assert old_icons_l in code, 'old_icons_l not found'
code = code.replace(old_icons_l, new_icons_l, 1)

old_icons_r = '''            // Dual Icons above the bar: Bullet & Reserve Box side-by-side
            GameObject bIconGo = CreateImageObject("Icon_Bullet", rightCardGo.transform, bulletIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            GameObject boxIconGo = CreateImageObject("Icon_Reserve", rightCardGo.transform, ammoBoxIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));'''

new_icons_r = '''            // Dual Icons above the bar: Bullet & Reserve Box side-by-side
            GameObject bIconGo = CreateImageObject("Icon_Bullet", rightCardGo.transform, bulletIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            bIconGo.GetComponent<Image>().color = ammoBase;
            GameObject boxIconGo = CreateImageObject("Icon_Reserve", rightCardGo.transform, ammoBoxIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            boxIconGo.GetComponent<Image>().color = ammoBase;'''

assert old_icons_r in code, 'old_icons_r not found'
code = code.replace(old_icons_r, new_icons_r, 1)

with open(hud_path, 'w', encoding='utf-8', newline='\n') as f:
    f.write(code)

print('SciFiHUDBuilder.cs successfully updated with normal colors!')

