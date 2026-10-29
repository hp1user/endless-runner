import os

hud_path = 'Assets/Scripts/Editor/HUD/SciFiHUDBuilder.cs'
with open(hud_path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. In BuildHUDInternal, load barBracket
s1 = '''            Sprite bulletIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Bullet.png");
            Sprite ammoBoxIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Ammo_Box.png");'''

r1 = '''            Sprite bulletIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Bullet.png");
            Sprite ammoBoxIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Ammo_Box.png");
            Sprite barBracket = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Bracket.png");
            if (barBracket == null) barBracket = slimTrack;'''

if s1 in code:
    code = code.replace(s1, r1, 1)
    print("1. Replaced barBracket loading in BuildHUDInternal")
else:
    print("1. barBracket loading already present or s1 not found")

# 2. Call BuildVerticalSideWingsLayout with barBracket
s2 = '''            HUDElementBinding sideBinding = BuildVerticalSideWingsLayout(sideWingsRootGo.transform, dockLeft, dockRight, slimTrack, slimFill, heartIcon, shieldIcon, bulletIcon, ammoBoxIcon, defaultGunIcon, font, theme);'''
r2 = '''            HUDElementBinding sideBinding = BuildVerticalSideWingsLayout(sideWingsRootGo.transform, dockLeft, dockRight, slimTrack, slimFill, barBracket, heartIcon, shieldIcon, bulletIcon, ammoBoxIcon, defaultGunIcon, font, theme);'''

if s2 in code:
    code = code.replace(s2, r2, 1)
    print("2. Replaced BuildVerticalSideWingsLayout call in BuildHUDInternal")
else:
    print("2. BuildVerticalSideWingsLayout call already updated or s2 not found")

# 3. Wire armorFrame and ammoFrame in BuildHUDInternal
s3 = '''            uiManager.armorCap = sideBinding.armorCap;
            uiManager.armorText = sideBinding.armorText;'''
r3 = '''            uiManager.armorCap = sideBinding.armorCap;
            uiManager.armorFrame = sideBinding.armorFrame;
            uiManager.armorText = sideBinding.armorText;'''

if s3 in code:
    code = code.replace(s3, r3, 1)
    print("3. Wired armorFrame in BuildHUDInternal")

s4 = '''            uiManager.ammoCap = sideBinding.ammoCap;
            uiManager.ammoCurrentText = sideBinding.ammoCurrentText;'''
r4 = '''            uiManager.ammoCap = sideBinding.ammoCap;
            uiManager.ammoFrame = sideBinding.ammoFrame;
            uiManager.ammoCurrentText = sideBinding.ammoCurrentText;'''

if s4 in code:
    code = code.replace(s4, r4, 1)
    print("4. Wired ammoFrame in BuildHUDInternal")

# 4. In ApplyThemeToScene, update alpha and colors
s_apply1 = '''                        Transform aFill = armorCol.Find("Armor_Fill");
                        if (aFill == null && aTrack != null) aFill = aTrack.Find("Armor_Fill");
                        if (aFill != null)
                        {
                            var img = aFill.GetComponent<Image>();
                            if (img != null)
                            {
                                if (theme.verticalFill != null) img.sprite = theme.verticalFill;
                                img.color = theme.armorColor;
                            }
                        }'''

r_apply1 = '''                        Transform aFill = armorCol.Find("Armor_Fill");
                        if (aFill == null && aTrack != null) aFill = aTrack.Find("Armor_Fill");
                        if (aFill != null)
                        {
                            var img = aFill.GetComponent<Image>();
                            if (img != null)
                            {
                                if (theme.verticalFill != null) img.sprite = theme.verticalFill;
                                img.color = new Color(theme.armorColor.r, theme.armorColor.g, theme.armorColor.b, 0.20f);
                            }
                        }
                        Transform aCap = armorCol.Find("Armor_Cap");
                        if (aCap != null)
                        {
                            var img = aCap.GetComponent<Image>();
                            if (img != null) img.color = new Color(theme.armorColor.r, theme.armorColor.g, theme.armorColor.b, 1.0f);
                        }
                        Transform aFrame = armorCol.Find("Armor_Frame");
                        if (aFrame != null)
                        {
                            var img = aFrame.GetComponent<Image>();
                            if (img != null) img.color = new Color(theme.armorColor.r, theme.armorColor.g, theme.armorColor.b, 1.0f);
                        }'''

if s_apply1 in code:
    code = code.replace(s_apply1, r_apply1, 1)
    print("5. Updated armor colors in ApplyThemeToScene")

s_apply2 = '''                        Transform ammoFill = ammoTrack.Find("Ammo_Fill");
                        if (ammoFill != null)
                        {
                            var img = ammoFill.GetComponent<Image>();
                            if (img != null)
                            {
                                if (theme.verticalFill != null) img.sprite = theme.verticalFill;
                                img.color = theme.ammoColor;
                            }
                        }'''

r_apply2 = '''                        Transform ammoFill = ammoTrack.Find("Ammo_Fill");
                        if (ammoFill != null)
                        {
                            var img = ammoFill.GetComponent<Image>();
                            if (img != null)
                            {
                                if (theme.verticalFill != null) img.sprite = theme.verticalFill;
                                img.color = new Color(theme.ammoColor.r, theme.ammoColor.g, theme.ammoColor.b, 0.20f);
                            }
                        }
                        Transform ammoCap = ammoTrack.Find("Ammo_Cap");
                        if (ammoCap != null)
                        {
                            var img = ammoCap.GetComponent<Image>();
                            if (img != null) img.color = new Color(theme.ammoColor.r, theme.ammoColor.g, theme.ammoColor.b, 1.0f);
                        }
                        Transform ammoFrame = ammoTrack.Find("Ammo_Frame");
                        if (ammoFrame != null)
                        {
                            var img = ammoFrame.GetComponent<Image>();
                            if (img != null) img.color = new Color(theme.ammoColor.r, theme.ammoColor.g, theme.ammoColor.b, 1.0f);
                        }'''

if s_apply2 in code:
    code = code.replace(s_apply2, r_apply2, 1)
    print("6. Updated ammo colors in ApplyThemeToScene")

# 5. Overhaul BuildVerticalSideWingsLayout with 20% alpha, wider WuWa brackets and Weapon Wheel palette
old_layout_start = "#region Vertical Side Wings HUD Builder (Flush Edge Bezel Docks)"
old_layout_end = "#endregion\n\n        #region Top Bar HUD Builder"

start_idx = code.find(old_layout_start)
end_idx = code.find(old_layout_end)
assert start_idx != -1 and end_idx != -1, "Could not find BuildVerticalSideWingsLayout boundaries"

new_layout = '''#region Vertical Side Wings HUD Builder (Flush Edge Bezel Docks)
        private static HUDElementBinding BuildVerticalSideWingsLayout(Transform parent, Sprite dockLeft, Sprite dockRight, Sprite slimTrack, Sprite slimFill, Sprite barBracket, Sprite heartIcon, Sprite shieldIcon, Sprite bulletIcon, Sprite ammoBoxIcon, Sprite gunIcon, TMP_FontAsset font, HUDThemeProfile theme = null)
        {
            var binding = new HUDElementBinding();
            binding.root = parent.gameObject;

            // Compact 80px dock width as requested
            float cardWidth = (theme != null && theme.dockWidth > 0) ? theme.dockWidth : 80f;
            float cardHeight = (theme != null && theme.dockHeight > 0) ? theme.dockHeight : 490f;
            float dockMarginX = (theme != null) ? theme.dockMarginX : 0f;
            float dockOffsetY = (theme != null) ? theme.dockOffsetY : -80f;

            float centerX = cardWidth * 0.5f; // Exactly 40px
            float barWidth = 24f;             // Sleek centered track (24px)
            float barHeight = Mathf.Clamp(cardHeight - 150f, 150f, 500f);
            float textPosY = -(barHeight + 96f);
            float reloadPosY = -(barHeight + 124f);

            // Base colors matching Weapon Wheel palette
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
            Color ammoCapCol = new Color(1.0f, 0.95f, 0.50f, 1.0f);                         // Glowing gold cap bloom

            // =========================================================================
            // 1. LEFT CARD: VERTICAL PLAYER VITALS (Wuthering Waves Overlapping Health + Armor)
            // =========================================================================
            GameObject leftCardGo = new GameObject("PlayerStatusCard_Side", typeof(RectTransform), typeof(Image));
            leftCardGo.transform.SetParent(parent, false);
            RectTransform leftRt = leftCardGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 1f);
            leftRt.anchorMax = new Vector2(0f, 1f);
            leftRt.pivot = new Vector2(0f, 1f);
            leftRt.anchoredPosition = new Vector2(dockMarginX, dockOffsetY);
            leftRt.sizeDelta = new Vector2(cardWidth, cardHeight);

            Image leftBg = leftCardGo.GetComponent<Image>();
            leftBg.sprite = dockLeft;
            leftBg.type = Image.Type.Sliced;
            leftBg.color = Color.white;
            leftBg.raycastTarget = false;

            // VITALS Header (Weapon Wheel Skill Cyan #6CE8FF)
            CreateTMPText("Vitals_Header", leftCardGo.transform, font, "VITALS", 9, FontStyles.Bold, new Color(0.42f, 0.91f, 1.0f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));

            // Dual Icons above the bar: Heart & Shield side-by-side
            GameObject heartGo = CreateImageObject("Icon_Heart", leftCardGo.transform, heartIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            GameObject shieldGo = CreateImageObject("Icon_Shield", leftCardGo.transform, shieldIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));

            // Single Vitals Track (Centered at X = 40)
            GameObject hTrackGo = CreateImageObject("Health_Track", leftCardGo.transform, slimTrack, new Vector2(centerX, -68f), new Vector2(barWidth, barHeight), new Vector2(0.5f, 1f));
            hTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            // BASE LAYER: Health Ghost & Solid Health Fill (Inner 16px width - Green shines clearly through 20% armor!)
            GameObject hGhostGo = CreateVerticalFillBar("Health_Ghost", hTrackGo.transform, slimFill, ghostCol, insetX: 4f);
            GameObject hFillGo = CreateVerticalFillBar("Health_Fill", hTrackGo.transform, slimFill, healthCol, insetX: 4f);

            // OVERLAPPING LAYER: Armor Container (Wider 24px width overlapping and framing health bar)
            GameObject armorColGo = new GameObject("ArmorColumn", typeof(RectTransform), typeof(CanvasGroup));
            armorColGo.transform.SetParent(hTrackGo.transform, false);
            RectTransform armorColRt = armorColGo.GetComponent<RectTransform>();
            armorColRt.anchorMin = Vector2.zero;
            armorColRt.anchorMax = Vector2.one;
            armorColRt.offsetMin = Vector2.zero;
            armorColRt.offsetMax = Vector2.zero;

            // Armor Fill: Translucent Electric Cyan (20% alpha) covering full 24px width!
            GameObject aGhostGo = CreateVerticalFillBar("Armor_Ghost", armorColGo.transform, slimFill, new Color(armorBase.r, armorBase.g, armorBase.b, 0.35f), insetX: 0f);
            GameObject aFillGo = CreateVerticalFillBar("Armor_Fill", armorColGo.transform, slimFill, armorOverlayCol, insetX: 0f);

            // Armor Frame Bracket: Wuthering Waves style luminous cyan 2px border highlight dynamically sizing to armor height!
            GameObject aFrameGo = CreateImageObject("Armor_Frame", armorColGo.transform, barBracket, new Vector2(0f, 0f), new Vector2(barWidth, 0f), new Vector2(0.5f, 0f));
            RectTransform aFrameRt = aFrameGo.GetComponent<RectTransform>();
            aFrameRt.anchorMin = new Vector2(0.5f, 0f);
            aFrameRt.anchorMax = new Vector2(0.5f, 0f);
            aFrameRt.pivot = new Vector2(0.5f, 0f);
            Image aFrameImg = aFrameGo.GetComponent<Image>();
            aFrameImg.type = Image.Type.Sliced;
            aFrameImg.color = armorFrameCol;

            // Armor Cap Indicator: Luminous glowing cyan cap line marking the shield boundary!
            GameObject aCapGo = CreateImageObject("Armor_Cap", armorColGo.transform, slimFill, new Vector2(0f, 0f), new Vector2(barWidth + 2f, 4f), new Vector2(0.5f, 0.5f));
            RectTransform aCapRt = aCapGo.GetComponent<RectTransform>();
            aCapRt.anchorMin = new Vector2(0.5f, 0f);
            aCapRt.anchorMax = new Vector2(0.5f, 0f);
            aCapRt.pivot = new Vector2(0.5f, 0.5f);
            Image aCapImg = aCapGo.GetComponent<Image>();
            aCapImg.color = armorCapCol;

            // Values below bar: Health (Green) on left, Armor (Cyan) on right
            GameObject hTextGo = CreateTMPText("Health_Text", leftCardGo.transform, font, "100", 14, FontStyles.Bold, healthCol,
                TextAlignmentOptions.Center, new Vector2(centerX - 15f, textPosY), new Vector2(30f, 20f), new Vector2(0.5f, 0.5f));

            GameObject aTextGo = CreateTMPText("Armor_Text", leftCardGo.transform, font, "100", 14, FontStyles.Bold, armorBase,
                TextAlignmentOptions.Center, new Vector2(centerX + 15f, textPosY), new Vector2(30f, 20f), new Vector2(0.5f, 0.5f));


            // =========================================================================
            // 2. RIGHT CARD: VERTICAL WEAPON & AMMO (Wuthering Waves Overlapping Reserve + Mag)
            // =========================================================================
            GameObject rightCardGo = new GameObject("AmmoHUDCard_Side", typeof(RectTransform), typeof(Image));
            rightCardGo.transform.SetParent(parent, false);
            RectTransform rightRt = rightCardGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 1f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 1f);
            rightRt.anchoredPosition = new Vector2(-dockMarginX, dockOffsetY);
            rightRt.sizeDelta = new Vector2(cardWidth, cardHeight);

            Image rightBg = rightCardGo.GetComponent<Image>();
            rightBg.sprite = dockRight;
            rightBg.type = Image.Type.Sliced;
            rightBg.color = Color.white;
            rightBg.raycastTarget = false;

            // AMMO Header (Weapon Wheel Warm Gold/Orange #FFAA33)
            GameObject wNameGo = CreateTMPText("Weapon_Name", rightCardGo.transform, font, "AMMO", 9, FontStyles.Bold, new Color(1.0f, 0.67f, 0.20f, 0.95f),
                TextAlignmentOptions.Center, new Vector2(centerX, -16f), new Vector2(70f, 16f), new Vector2(0.5f, 0.5f));

            // Compact Weapon Silhouette Icon at Top
            GameObject wIconGo = CreateImageObject("Weapon_Icon", rightCardGo.transform, gunIcon, new Vector2(centerX, -32f), new Vector2(38f, 18f), new Vector2(0.5f, 0.5f));
            Image wImg = wIconGo.GetComponent<Image>();
            wImg.preserveAspect = true;

            // Dual Icons above the bar: Bullet & Reserve Box side-by-side
            GameObject bIconGo = CreateImageObject("Icon_Bullet", rightCardGo.transform, bulletIcon, new Vector2(centerX - 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));
            GameObject boxIconGo = CreateImageObject("Icon_Reserve", rightCardGo.transform, ammoBoxIcon, new Vector2(centerX + 13f, -46f), new Vector2(20f, 20f), new Vector2(0.5f, 0.5f));

            // Single Ammo Track (Centered at X = 40)
            GameObject ammoTrackGo = CreateImageObject("Ammo_Track", rightCardGo.transform, slimTrack, new Vector2(centerX, -68f), new Vector2(barWidth, barHeight), new Vector2(0.5f, 1f));
            ammoTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            // BASE LAYER: Reserve / Max Ammo Bar (Inner 16px width Dark Amber - visible below mag ammo overlay!)
            GameObject resFillGo = CreateVerticalFillBar("Reserve_Fill", ammoTrackGo.transform, slimFill, reserveCol, insetX: 4f);

            // OVERLAPPING LAYER: Magazine Current Ammo (Wider 24px width, 20% alpha translucent gold!)
            GameObject ammoGhostGo = CreateVerticalFillBar("Ammo_Ghost", ammoTrackGo.transform, slimFill, new Color(1f, 0.4f, 0.2f, 0.35f), insetX: 0f);
            GameObject ammoFillGo = CreateVerticalFillBar("Ammo_Fill", ammoTrackGo.transform, slimFill, ammoOverlayCol, insetX: 0f);

            // Ammo Frame Bracket: Wuthering Waves style luminous gold 2px border highlight dynamically sizing to ammo height!
            GameObject ammoFrameGo = CreateImageObject("Ammo_Frame", ammoTrackGo.transform, barBracket, new Vector2(0f, 0f), new Vector2(barWidth, 0f), new Vector2(0.5f, 0f));
            RectTransform ammoFrameRt = ammoFrameGo.GetComponent<RectTransform>();
            ammoFrameRt.anchorMin = new Vector2(0.5f, 0f);
            ammoFrameRt.anchorMax = new Vector2(0.5f, 0f);
            ammoFrameRt.pivot = new Vector2(0.5f, 0f);
            Image ammoFrameImg = ammoFrameGo.GetComponent<Image>();
            ammoFrameImg.type = Image.Type.Sliced;
            ammoFrameImg.color = ammoFrameCol;

            // Ammo Cap Indicator: Luminous glowing gold cap line marking the magazine ammo boundary!
            GameObject ammoCapGo = CreateImageObject("Ammo_Cap", ammoTrackGo.transform, slimFill, new Vector2(0f, 0f), new Vector2(barWidth + 2f, 4f), new Vector2(0.5f, 0.5f));
            RectTransform ammoCapRt = ammoCapGo.GetComponent<RectTransform>();
            ammoCapRt.anchorMin = new Vector2(0.5f, 0f);
            ammoCapRt.anchorMax = new Vector2(0.5f, 0f);
            ammoCapRt.pivot = new Vector2(0.5f, 0.5f);
            Image ammoCapImg = ammoCapGo.GetComponent<Image>();
            ammoCapImg.color = ammoCapCol;

            // Values below bar: Current Mag Ammo (White) on left, Reserve Ammo (Amber) on right
            GameObject ammoCurGo = CreateTMPText("Ammo_Current", rightCardGo.transform, font, "30", 14, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Center, new Vector2(centerX - 15f, textPosY), new Vector2(30f, 20f), new Vector2(0.5f, 0.5f));

            GameObject ammoResGo = CreateTMPText("Ammo_Reserve", rightCardGo.transform, font, "120", 14, FontStyles.Bold, ammoBase,
                TextAlignmentOptions.Center, new Vector2(centerX + 15f, textPosY), new Vector2(30f, 20f), new Vector2(0.5f, 0.5f));

            // Reload Prompt at Bottom of Right Dock
            GameObject reloadGo = CreateTMPText("Reload_Prompt", rightCardGo.transform, font, "⚡ RELOAD", 10, FontStyles.Bold, new Color(1f, 0.25f, 0.25f, 1f),
                TextAlignmentOptions.Center, new Vector2(centerX, reloadPosY), new Vector2(80f, 16f), new Vector2(0.5f, 0.5f));
            reloadGo.SetActive(false);

            // Populate Binding
            binding.healthBarFill = hFillGo.GetComponent<Image>();
            binding.healthBarGhost = hGhostGo.GetComponent<Image>();
            binding.healthText = hTextGo.GetComponent<TextMeshProUGUI>();
            binding.healthIcon = heartGo.GetComponent<Image>();

            binding.armorContainer = armorColGo;
            binding.armorBarFill = aFillGo.GetComponent<Image>();
            binding.armorBarGhost = aGhostGo.GetComponent<Image>();
            binding.armorCap = aCapRt;
            binding.armorFrame = aFrameRt;
            binding.armorText = aTextGo.GetComponent<TextMeshProUGUI>();
            binding.armorIcon = shieldGo.GetComponent<Image>();

            binding.weaponIcon = wImg;
            binding.weaponNameText = wNameGo.GetComponent<TextMeshProUGUI>();
            binding.ammoBarFill = ammoFillGo.GetComponent<Image>();
            binding.ammoBarGhost = ammoGhostGo.GetComponent<Image>();
            binding.ammoReserveBarFill = resFillGo.GetComponent<Image>();
            binding.ammoCap = ammoCapRt;
            binding.ammoFrame = ammoFrameRt;
            binding.ammoCurrentText = ammoCurGo.GetComponent<TextMeshProUGUI>();
            binding.ammoReserveText = ammoResGo.GetComponent<TextMeshProUGUI>();
            binding.ammoDividerText = null;
            binding.reloadPrompt = reloadGo;

            return binding;
        }
'''

code = code[:start_idx] + new_layout + code[end_idx:]

with open(hud_path, 'w', encoding='utf-8', newline='\n') as f:
    f.write(code)

print("SciFiHUDBuilder.cs successfully updated!")

