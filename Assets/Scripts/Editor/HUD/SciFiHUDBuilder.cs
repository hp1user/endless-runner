using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UI.Control;

namespace EndlessRunner.Editor
{
    [InitializeOnLoad]
    public static class SciFiHUDBuilder
    {
        private const string SPRITES_PATH = "Assets/UI/Sprite/HUD";
        private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static SciFiHUDBuilder()
        {
            EditorApplication.delayCall += AutoRunIfPending;
            EditorApplication.update += CheckAndBuildOnce;
        }

        private static void CheckAndBuildOnce()
        {
            EditorApplication.update -= CheckAndBuildOnce;
            AutoRunIfPending();
        }

        private static void AutoRunIfPending()
        {
            if (Application.isPlaying) return;
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager == null) return;

            Transform sideRoot = uiManager.transform.Find("SideWings_HUD");
            Transform topRoot = uiManager.transform.Find("TopBar_HUD");
            Transform bossRoot = uiManager.transform.Find("Boss_HUD");

            // Check if vertical slim dock is already built and matches latest dimensions (width 110f, flush edge bezels)
            if (sideRoot != null && topRoot != null && bossRoot != null &&
                uiManager.sideWingsHUD != null && uiManager.sideWingsHUD.ammoBarFill != null &&
                uiManager.sideWingsHUD.healthBarFill != null &&
                uiManager.sideWingsHUD.healthBarFill.fillMethod == Image.FillMethod.Vertical)
            {
                RectTransform leftRt = sideRoot.Find("PlayerStatusCard_Side") as RectTransform;
                RectTransform rightRt = sideRoot.Find("AmmoHUDCard_Side") as RectTransform;
                if (leftRt != null && rightRt != null &&
                    Mathf.Abs(leftRt.sizeDelta.x - 110f) < 0.5f &&
                    Mathf.Abs(leftRt.anchoredPosition.x) < 0.5f &&
                    Mathf.Abs(rightRt.sizeDelta.x - 110f) < 0.5f &&
                    Mathf.Abs(rightRt.anchoredPosition.x) < 0.5f)
                {
                    return; // Already configured to latest sleek vertical layout
                }
            }

            BuildHUDInternal(silent: true);
        }

        [MenuItem("Tools/Endless Runner/HUD/Build All HUD Variants", false, 20)]
        [MenuItem("Window/Endless Runner/HUD/Build All HUD Variants", false, 200)]
        public static void BuildHUD()
        {
            BuildHUDInternal(silent: false);
        }

        [MenuItem("Tools/Endless Runner/HUD/Switch to Side Wings HUD (Vertical)", false, 21)]
        public static void MenuSwitchToSideWings()
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager != null)
            {
                Undo.RecordObject(uiManager, "Switch to Side Wings HUD");
                uiManager.SwitchVariant(HUDLayoutVariant.SideWings);
                EditorUtility.SetDirty(uiManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
                Debug.Log("<color=cyan>[SciFiHUD]</color> Active HUD switched to: <b>Side Wings (Vertical Vitals & Vertical Ammo)</b>");
            }
        }

        [MenuItem("Tools/Endless Runner/HUD/Switch to Top Bar HUD", false, 22)]
        public static void MenuSwitchToTopBar()
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager != null)
            {
                Undo.RecordObject(uiManager, "Switch to Top Bar HUD");
                uiManager.SwitchVariant(HUDLayoutVariant.TopBar);
                EditorUtility.SetDirty(uiManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
                Debug.Log("<color=cyan>[SciFiHUD]</color> Active HUD switched to: <b>Top Bar (Top-Left & Top-Right)</b>");
            }
        }

        [MenuItem("Tools/Endless Runner/HUD/Toggle Test Boss Health Bar", false, 23)]
        public static void MenuToggleBossHealth()
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager != null)
            {
                uiManager.ToggleTestBossHealth();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
            }
        }

        public static void BuildHUDInternal(bool silent = false)
        {
            var uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (uiManager == null)
            {
                if (!silent)
                {
                    EditorUtility.DisplayDialog("UI Manager Not Found", "Could not find a UIManager in the active scene.\nPlease make sure 'GameView_EndlessRunner.unity' is loaded.", "OK");
                }
                return;
            }

            GameObject canvasGo = uiManager.gameObject;
            Undo.RegisterFullObjectHierarchyUndo(canvasGo, "Build All Sci-Fi HUD Variants");

            // 1. Setup CanvasScaler for crisp Portrait layout (1080x1920)
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
            }

            // 2. Load Sprites & Font Assets
            Sprite panelBg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Panel_Bg.png");
            Sprite barTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Track.png");
            Sprite barFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Fill.png");
            Sprite heartIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Heart.png");
            Sprite shieldIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Shield.png");
            Sprite ammoPanelBg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Ammo_Panel.png");
            Sprite defaultGunIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprite/AR.png");
            Sprite bossPanelBg = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Boss_Panel.png");
            Sprite bossSkullIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Boss_Skull.png");

            // Slim Vertical Dock Sprites (Flush against screen edges, reduced width)
            Sprite dockLeft = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Vertical_Dock_Left.png");
            Sprite dockRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Vertical_Dock_Right.png");
            Sprite slimTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Track_Slim.png");
            Sprite slimFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Fill_Slim.png");
            Sprite bulletIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Bullet.png");
            Sprite ammoBoxIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Ammo_Box.png");

            if (dockLeft == null) dockLeft = panelBg;
            if (dockRight == null) dockRight = panelBg;
            if (slimTrack == null) slimTrack = barTrack;
            if (slimFill == null) slimFill = barFill;
            if (bulletIcon == null) bulletIcon = defaultGunIcon;
            if (ammoBoxIcon == null) ammoBoxIcon = bulletIcon;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            Transform canvasTrans = canvasGo.transform;

            // 3. Hide legacy unstyled raw text
            HideOldRawText(canvasTrans, "Health");
            HideOldRawText(canvasTrans, "Armour");
            HideOldRawText(canvasTrans, "ammoText");

            // Remove previous standalone cards if present
            DestroyChildIfFound(canvasTrans, "PlayerStatusCard");
            DestroyChildIfFound(canvasTrans, "AmmoHUDCard");

            // 4. Build Variant 1: Side Wings HUD Root (Vertical Vitals & Vertical Ammo - Flush Edge Bezels)
            DestroyChildIfFound(canvasTrans, "SideWings_HUD");
            GameObject sideWingsRootGo = new GameObject("SideWings_HUD", typeof(RectTransform));
            sideWingsRootGo.transform.SetParent(canvasTrans, false);
            RectTransform sideWingsRt = sideWingsRootGo.GetComponent<RectTransform>();
            StretchFull(sideWingsRt);
            Undo.RegisterCreatedObjectUndo(sideWingsRootGo, "Create Side Wings HUD");

            HUDElementBinding sideBinding = BuildVerticalSideWingsLayout(sideWingsRootGo.transform, dockLeft, dockRight, slimTrack, slimFill, heartIcon, shieldIcon, bulletIcon, ammoBoxIcon, defaultGunIcon, font);

            // 5. Build Variant 2: Top Bar HUD Root (Classic Top-Left / Top-Right)
            DestroyChildIfFound(canvasTrans, "TopBar_HUD");
            GameObject topBarRootGo = new GameObject("TopBar_HUD", typeof(RectTransform));
            topBarRootGo.transform.SetParent(canvasTrans, false);
            RectTransform topBarRt = topBarRootGo.GetComponent<RectTransform>();
            StretchFull(topBarRt);
            Undo.RegisterCreatedObjectUndo(topBarRootGo, "Create Top Bar HUD");

            HUDElementBinding topBinding = BuildTopBarLayout(topBarRootGo.transform, panelBg, ammoPanelBg, barTrack, barFill, heartIcon, shieldIcon, defaultGunIcon, font);

            // 6. Build Boss Health HUD (Top Center - blue box in reference)
            DestroyChildIfFound(canvasTrans, "Boss_HUD");
            GameObject bossHudRootGo = new GameObject("Boss_HUD", typeof(RectTransform));
            bossHudRootGo.transform.SetParent(canvasTrans, false);
            RectTransform bossHudRt = bossHudRootGo.GetComponent<RectTransform>();
            StretchFull(bossHudRt);
            Undo.RegisterCreatedObjectUndo(bossHudRootGo, "Create Boss HUD");

            var bossRefs = BuildBossHealthBar(bossHudRootGo.transform, bossPanelBg, barTrack, barFill, bossSkullIcon, font);

            // 7. Wire all references to UIManager
            uiManager.sideWingsHUD = sideBinding;
            uiManager.topBarHUD = topBinding;

            uiManager.bossHealthContainer = bossRefs.container;
            uiManager.bossBarFill = bossRefs.fill;
            uiManager.bossBarGhost = bossRefs.ghost;
            uiManager.bossNameText = bossRefs.nameText;
            uiManager.bossHealthText = bossRefs.healthText;
            uiManager.bossSkullIcon = bossRefs.icon;

            // Direct fallback pointers point to side wings (active variant)
            uiManager.healthBarFill = sideBinding.healthBarFill;
            uiManager.healthBarGhost = sideBinding.healthBarGhost;
            uiManager.healthText = sideBinding.healthText;
            uiManager.healthIcon = sideBinding.healthIcon;
            uiManager.armorContainer = sideBinding.armorContainer;
            uiManager.armorBarFill = sideBinding.armorBarFill;
            uiManager.armorBarGhost = sideBinding.armorBarGhost;
            uiManager.armorText = sideBinding.armorText;
            uiManager.armorIcon = sideBinding.armorIcon;
            uiManager.weaponIcon = sideBinding.weaponIcon;
            uiManager.weaponNameText = sideBinding.weaponNameText;
            uiManager.ammoCurrentText = sideBinding.ammoCurrentText;
            uiManager.ammoReserveText = sideBinding.ammoReserveText;
            uiManager.ammoDividerText = sideBinding.ammoDividerText;
            uiManager.reloadPrompt = sideBinding.reloadPrompt;

            uiManager.activeVariant = HUDLayoutVariant.SideWings;
            uiManager.ApplyVariantActiveState();

            EditorUtility.SetDirty(uiManager);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGo.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                SceneView.RepaintAll();
            }

            Debug.Log("<color=green>[SciFiHUDBuilder]</color> <b>Successfully built and wired Vertical Side Wings HUD, Top Bar HUD, and Boss Health Bar!</b>");
            if (!silent)
            {
                EditorUtility.DisplayDialog("Sci-Fi HUD Built", 
                    "Successfully built and wired all HUD elements:\n\n" +
                    "• Vertical Side Wings HUD (Flush bezel docks, slender 115px width, vertical Vitals & vertical Ammo)\n" +
                    "• Top Bar HUD (Classic layout with fixed text spacing)\n" +
                    "• Boss Health Bar (Top-Center Blue Box)\n\n" +
                    "You can switch between them anytime via Tools > Endless Runner > HUD!", "OK");
            }
        }

        #region Vertical Side Wings HUD Builder (Flush Edge Bezel Docks)
        private static HUDElementBinding BuildVerticalSideWingsLayout(Transform parent, Sprite dockLeft, Sprite dockRight, Sprite slimTrack, Sprite slimFill, Sprite heartIcon, Sprite shieldIcon, Sprite bulletIcon, Sprite ammoBoxIcon, Sprite gunIcon, TMP_FontAsset font)
        {
            var binding = new HUDElementBinding();
            binding.root = parent.gameObject;

            const float CARD_WIDTH = 110f;
            const float CARD_HEIGHT = 490f;
            const float COL_LEFT_X = 36f;
            const float COL_RIGHT_X = 74f;
            const float BAR_WIDTH = 20f;
            const float BAR_HEIGHT = 340f;

            // =========================================================================
            // 1. LEFT CARD: VERTICAL PLAYER VITALS (Flush with Left Screen Edge)
            // =========================================================================
            GameObject leftCardGo = new GameObject("PlayerStatusCard_Side", typeof(RectTransform), typeof(Image));
            leftCardGo.transform.SetParent(parent, false);
            RectTransform leftRt = leftCardGo.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 1f);
            leftRt.anchorMax = new Vector2(0f, 1f);
            leftRt.pivot = new Vector2(0f, 1f);
            leftRt.anchoredPosition = new Vector2(0f, -80f); // 0f = Flush against left screen bezel!
            leftRt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            Image leftBg = leftCardGo.GetComponent<Image>();
            leftBg.sprite = dockLeft;
            leftBg.type = Image.Type.Sliced;
            leftBg.color = Color.white;
            leftBg.raycastTarget = false;

            // VITALS Header
            CreateTMPText("Vitals_Header", leftCardGo.transform, font, "VITALS", 10, FontStyles.Bold, new Color(0.4f, 0.85f, 1f, 0.9f),
                TextAlignmentOptions.Center, new Vector2(CARD_WIDTH * 0.5f, -18f), new Vector2(90f, 18f), new Vector2(0.5f, 0.5f));

            // --- HEALTH COLUMN (Left side: X = 36) ---
            GameObject heartGo = CreateImageObject("Icon_Heart", leftCardGo.transform, heartIcon, new Vector2(COL_LEFT_X, -50f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));

            GameObject hTrackGo = CreateImageObject("Health_Track", leftCardGo.transform, slimTrack, new Vector2(COL_LEFT_X, -72f), new Vector2(BAR_WIDTH, BAR_HEIGHT), new Vector2(0.5f, 1f));
            hTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject hGhostGo = CreateVerticalFillBar("Health_Ghost", hTrackGo.transform, slimFill, new Color(1f, 0.3f, 0.3f, 0.75f));
            GameObject hFillGo = CreateVerticalFillBar("Health_Fill", hTrackGo.transform, slimFill, new Color(0f, 1f, 0.65f, 1f));

            GameObject hTextGo = CreateTMPText("Health_Text", leftCardGo.transform, font, "100", 16, FontStyles.Bold, new Color(0f, 1f, 0.65f, 1f),
                TextAlignmentOptions.Center, new Vector2(COL_LEFT_X, -438f), new Vector2(40f, 22f), new Vector2(0.5f, 0.5f));

            // --- ARMOR COLUMN (Right side: X = 74) ---
            GameObject armorColGo = new GameObject("ArmorColumn", typeof(RectTransform));
            armorColGo.transform.SetParent(leftCardGo.transform, false);
            RectTransform armorColRt = armorColGo.GetComponent<RectTransform>();
            armorColRt.anchorMin = Vector2.zero;
            armorColRt.anchorMax = Vector2.one;
            armorColRt.offsetMin = Vector2.zero;
            armorColRt.offsetMax = Vector2.zero;
            armorColGo.AddComponent<CanvasGroup>();

            GameObject shieldGo = CreateImageObject("Icon_Shield", armorColGo.transform, shieldIcon, new Vector2(COL_RIGHT_X, -50f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));

            GameObject aTrackGo = CreateImageObject("Armor_Track", armorColGo.transform, slimTrack, new Vector2(COL_RIGHT_X, -72f), new Vector2(BAR_WIDTH, BAR_HEIGHT), new Vector2(0.5f, 1f));
            aTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject aGhostGo = CreateVerticalFillBar("Armor_Ghost", aTrackGo.transform, slimFill, new Color(0.5f, 0.85f, 1f, 0.75f));
            GameObject aFillGo = CreateVerticalFillBar("Armor_Fill", aTrackGo.transform, slimFill, new Color(0f, 0.9f, 1f, 1f));

            GameObject aTextGo = CreateTMPText("Armor_Text", armorColGo.transform, font, "100", 16, FontStyles.Bold, new Color(0f, 0.9f, 1f, 1f),
                TextAlignmentOptions.Center, new Vector2(COL_RIGHT_X, -438f), new Vector2(40f, 22f), new Vector2(0.5f, 0.5f));


            // =========================================================================
            // 2. RIGHT CARD: VERTICAL WEAPON & AMMO (Flush with Right Screen Edge)
            // =========================================================================
            GameObject rightCardGo = new GameObject("AmmoHUDCard_Side", typeof(RectTransform), typeof(Image));
            rightCardGo.transform.SetParent(parent, false);
            RectTransform rightRt = rightCardGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 1f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 1f);
            rightRt.anchoredPosition = new Vector2(0f, -80f); // 0f = Flush against right screen bezel!
            rightRt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

            Image rightBg = rightCardGo.GetComponent<Image>();
            rightBg.sprite = dockRight;
            rightBg.type = Image.Type.Sliced;
            rightBg.color = Color.white;
            rightBg.raycastTarget = false;

            // AMMO Header
            GameObject wNameGo = CreateTMPText("Weapon_Name", rightCardGo.transform, font, "AMMO", 10, FontStyles.Bold, new Color(1f, 0.75f, 0.15f, 0.9f),
                TextAlignmentOptions.Center, new Vector2(CARD_WIDTH * 0.5f, -18f), new Vector2(90f, 18f), new Vector2(0.5f, 0.5f));

            // Compact Weapon Silhouette Icon at Top
            GameObject wIconGo = CreateImageObject("Weapon_Icon", rightCardGo.transform, gunIcon, new Vector2(CARD_WIDTH * 0.5f, -34f), new Vector2(48f, 24f), new Vector2(0.5f, 0.5f));
            Image wImg = wIconGo.GetComponent<Image>();
            wImg.preserveAspect = true;

            // --- MAGAZINE COLUMN (Left side: X = 36) ---
            GameObject bIconGo = CreateImageObject("Icon_Bullet", rightCardGo.transform, bulletIcon, new Vector2(COL_LEFT_X, -50f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));

            GameObject ammoTrackGo = CreateImageObject("Ammo_Track", rightCardGo.transform, slimTrack, new Vector2(COL_LEFT_X, -72f), new Vector2(BAR_WIDTH, BAR_HEIGHT), new Vector2(0.5f, 1f));
            ammoTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject ammoGhostGo = CreateVerticalFillBar("Ammo_Ghost", ammoTrackGo.transform, slimFill, new Color(1f, 0.4f, 0.2f, 0.75f));
            GameObject ammoFillGo = CreateVerticalFillBar("Ammo_Fill", ammoTrackGo.transform, slimFill, new Color(1f, 0.82f, 0.1f, 1f));

            GameObject ammoCurGo = CreateTMPText("Ammo_Current", rightCardGo.transform, font, "30", 16, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Center, new Vector2(COL_LEFT_X, -438f), new Vector2(40f, 22f), new Vector2(0.5f, 0.5f));

            // --- RESERVE COLUMN (Right side: X = 74) ---
            GameObject boxIconGo = CreateImageObject("Icon_Reserve", rightCardGo.transform, ammoBoxIcon, new Vector2(COL_RIGHT_X, -50f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));

            GameObject resTrackGo = CreateImageObject("Reserve_Track", rightCardGo.transform, slimTrack, new Vector2(COL_RIGHT_X, -72f), new Vector2(BAR_WIDTH, BAR_HEIGHT), new Vector2(0.5f, 1f));
            resTrackGo.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject resFillGo = CreateVerticalFillBar("Reserve_Fill", resTrackGo.transform, slimFill, new Color(0.85f, 0.6f, 0.1f, 0.85f));

            GameObject ammoResGo = CreateTMPText("Ammo_Reserve", rightCardGo.transform, font, "120", 16, FontStyles.Bold, new Color(1f, 0.75f, 0.15f, 1f),
                TextAlignmentOptions.Center, new Vector2(COL_RIGHT_X, -438f), new Vector2(40f, 22f), new Vector2(0.5f, 0.5f));

            // Reload Prompt at Bottom of Right Dock
            GameObject reloadGo = CreateTMPText("Reload_Prompt", rightCardGo.transform, font, "⚡ RELOAD", 11, FontStyles.Bold, new Color(1f, 0.25f, 0.25f, 1f),
                TextAlignmentOptions.Center, new Vector2(CARD_WIDTH * 0.5f, -468f), new Vector2(96f, 16f), new Vector2(0.5f, 0.5f));
            reloadGo.SetActive(false);

            // Populate Binding
            binding.healthBarFill = hFillGo.GetComponent<Image>();
            binding.healthBarGhost = hGhostGo.GetComponent<Image>();
            binding.healthText = hTextGo.GetComponent<TextMeshProUGUI>();
            binding.healthIcon = heartGo.GetComponent<Image>();

            binding.armorContainer = armorColGo;
            binding.armorBarFill = aFillGo.GetComponent<Image>();
            binding.armorBarGhost = aGhostGo.GetComponent<Image>();
            binding.armorText = aTextGo.GetComponent<TextMeshProUGUI>();
            binding.armorIcon = shieldGo.GetComponent<Image>();

            binding.weaponIcon = wImg;
            binding.weaponNameText = wNameGo.GetComponent<TextMeshProUGUI>();
            binding.ammoBarFill = ammoFillGo.GetComponent<Image>();
            binding.ammoBarGhost = ammoGhostGo.GetComponent<Image>();
            binding.ammoReserveBarFill = resFillGo.GetComponent<Image>();
            binding.ammoCurrentText = ammoCurGo.GetComponent<TextMeshProUGUI>();
            binding.ammoReserveText = ammoResGo.GetComponent<TextMeshProUGUI>();
            binding.ammoDividerText = null;
            binding.reloadPrompt = reloadGo;

            return binding;
        }
        #endregion

        #region Top Bar HUD Builder (Fixed Coordinates)
        private static HUDElementBinding BuildTopBarLayout(Transform parent, Sprite panelBg, Sprite ammoPanelBg, Sprite barTrack, Sprite barFill, Sprite heartIcon, Sprite shieldIcon, Sprite gunIcon, TMP_FontAsset font)
        {
            var binding = new HUDElementBinding();
            binding.root = parent.gameObject;

            // --- Top-Left Player Status Card ---
            GameObject statusCardGo = new GameObject("PlayerStatusCard_Top", typeof(RectTransform), typeof(Image));
            statusCardGo.transform.SetParent(parent, false);
            RectTransform statusRect = statusCardGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(0f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);
            statusRect.anchoredPosition = new Vector2(32f, -32f);
            statusRect.sizeDelta = new Vector2(440f, 136f);

            Image statusBgImg = statusCardGo.GetComponent<Image>();
            statusBgImg.sprite = panelBg;
            statusBgImg.type = Image.Type.Sliced;
            statusBgImg.color = Color.white;
            statusBgImg.raycastTarget = false;

            // Health Row
            GameObject healthRowGo = CreateRowContainer("HealthRow", statusCardGo.transform, new Vector2(0, -12), new Vector2(440, 54));
            GameObject heartGo = CreateImageObject("Icon_Heart", healthRowGo.transform, heartIcon, new Vector2(18, 0), new Vector2(36, 36), new Vector2(0f, 0.5f));
            GameObject healthTrackGo = CreateImageObject("Health_Track", healthRowGo.transform, barTrack, new Vector2(64, 0), new Vector2(270, 22), new Vector2(0f, 0.5f));
            healthTrackGo.GetComponent<Image>().type = Image.Type.Sliced;
            GameObject healthGhostGo = CreateFillBar("Health_Bar_Ghost", healthTrackGo.transform, barFill, new Color(1f, 0.32f, 0.32f, 0.75f));
            GameObject healthFillGo = CreateFillBar("Health_Bar_Fill", healthTrackGo.transform, barFill, new Color(0f, 1f, 0.65f, 1f));
            GameObject healthTextGo = CreateTMPText("Health_Text", healthRowGo.transform, font, "100", 22, FontStyles.Bold, new Color(0f, 1f, 0.65f, 1f),
                TextAlignmentOptions.Right, new Vector2(344, 0), new Vector2(80, 28), new Vector2(0f, 0.5f));

            // Armor Row
            GameObject armorRowGo = CreateRowContainer("ArmorRow", statusCardGo.transform, new Vector2(0, -70), new Vector2(440, 54));
            armorRowGo.AddComponent<CanvasGroup>();
            GameObject shieldGo = CreateImageObject("Icon_Shield", armorRowGo.transform, shieldIcon, new Vector2(18, 0), new Vector2(36, 36), new Vector2(0f, 0.5f));
            GameObject armorTrackGo = CreateImageObject("Armor_Track", armorRowGo.transform, barTrack, new Vector2(64, 0), new Vector2(270, 22), new Vector2(0f, 0.5f));
            armorTrackGo.GetComponent<Image>().type = Image.Type.Sliced;
            GameObject armorGhostGo = CreateFillBar("Armor_Bar_Ghost", armorTrackGo.transform, barFill, new Color(0.5f, 0.85f, 1f, 0.75f));
            GameObject armorFillGo = CreateFillBar("Armor_Bar_Fill", armorTrackGo.transform, barFill, new Color(0f, 0.9f, 1f, 1f));
            GameObject armorTextGo = CreateTMPText("Armor_Text", armorRowGo.transform, font, "100", 22, FontStyles.Bold, new Color(0f, 0.9f, 1f, 1f),
                TextAlignmentOptions.Right, new Vector2(344, 0), new Vector2(80, 28), new Vector2(0f, 0.5f));

            // --- Top-Right Ammo HUD Card (Padded and Fixed Coordinates) ---
            GameObject ammoCardGo = new GameObject("AmmoHUDCard_Top", typeof(RectTransform), typeof(Image));
            ammoCardGo.transform.SetParent(parent, false);
            RectTransform ammoRect = ammoCardGo.GetComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(1f, 1f);
            ammoRect.anchorMax = new Vector2(1f, 1f);
            ammoRect.pivot = new Vector2(1f, 1f);
            ammoRect.anchoredPosition = new Vector2(-32f, -32f);
            ammoRect.sizeDelta = new Vector2(360f, 136f);

            Image ammoBgImg = ammoCardGo.GetComponent<Image>();
            ammoBgImg.sprite = ammoPanelBg;
            ammoBgImg.type = Image.Type.Sliced;
            ammoBgImg.color = Color.white;
            ammoBgImg.raycastTarget = false;

            // Left Section: Weapon Icon & Name
            GameObject weaponIconGo = CreateImageObject("Weapon_Icon", ammoCardGo.transform, gunIcon, new Vector2(64, -48), new Vector2(84, 50), new Vector2(0.5f, 0.5f));
            Image wIconImg = weaponIconGo.GetComponent<Image>();
            wIconImg.preserveAspect = true;

            GameObject weaponNameGo = CreateTMPText("Weapon_Name", ammoCardGo.transform, font, "ASSAULT RIFLE", 12, FontStyles.Bold, new Color(0.5f, 0.85f, 1f, 0.9f),
                TextAlignmentOptions.Center, new Vector2(64, -100), new Vector2(110, 20), new Vector2(0.5f, 0.5f));

            // Right Section: Ammo Numbers & Reload Prompt
            GameObject ammoCurrentGo = CreateTMPText("Ammo_Current", ammoCardGo.transform, font, "30", 38, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Right, new Vector2(230, -52), new Vector2(84, 45), new Vector2(1f, 0.5f));

            GameObject ammoDividerGo = CreateTMPText("Ammo_Divider", ammoCardGo.transform, font, "/", 24, FontStyles.Bold, new Color(0.45f, 0.55f, 0.65f, 1f),
                TextAlignmentOptions.Center, new Vector2(246, -52), new Vector2(16, 36), new Vector2(0.5f, 0.5f));

            GameObject ammoReserveGo = CreateTMPText("Ammo_Reserve", ammoCardGo.transform, font, "120", 26, FontStyles.Bold, new Color(1f, 0.78f, 0.15f, 1f),
                TextAlignmentOptions.Left, new Vector2(262, -56), new Vector2(85, 36), new Vector2(0f, 0.5f));

            GameObject reloadPromptGo = CreateTMPText("Reload_Prompt", ammoCardGo.transform, font, "⚡ RELOAD", 13, FontStyles.Bold, new Color(1f, 0.3f, 0.3f, 1f),
                TextAlignmentOptions.Center, new Vector2(246, -100), new Vector2(170, 20), new Vector2(0.5f, 0.5f));
            reloadPromptGo.SetActive(false);

            // Populate Binding
            binding.healthBarFill = healthFillGo.GetComponent<Image>();
            binding.healthBarGhost = healthGhostGo.GetComponent<Image>();
            binding.healthText = healthTextGo.GetComponent<TextMeshProUGUI>();
            binding.healthIcon = heartGo.GetComponent<Image>();

            binding.armorContainer = armorRowGo;
            binding.armorBarFill = armorFillGo.GetComponent<Image>();
            binding.armorBarGhost = armorGhostGo.GetComponent<Image>();
            binding.armorText = armorTextGo.GetComponent<TextMeshProUGUI>();
            binding.armorIcon = shieldGo.GetComponent<Image>();

            binding.weaponIcon = wIconImg;
            binding.weaponNameText = weaponNameGo.GetComponent<TextMeshProUGUI>();
            binding.ammoCurrentText = ammoCurrentGo.GetComponent<TextMeshProUGUI>();
            binding.ammoReserveText = ammoReserveGo.GetComponent<TextMeshProUGUI>();
            binding.ammoDividerText = ammoDividerGo.GetComponent<TextMeshProUGUI>();
            binding.reloadPrompt = reloadPromptGo;

            return binding;
        }
        #endregion

        #region Boss Health Bar Builder (Top Center Blue Box)
        private struct BossRefs
        {
            public GameObject container;
            public Image fill;
            public Image ghost;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI healthText;
            public Image icon;
        }

        private static BossRefs BuildBossHealthBar(Transform parent, Sprite panelBg, Sprite barTrack, Sprite barFill, Sprite skullIcon, TMP_FontAsset font)
        {
            BossRefs refs = new BossRefs();

            GameObject bossCardGo = new GameObject("BossHUDCard", typeof(RectTransform), typeof(Image));
            bossCardGo.transform.SetParent(parent, false);
            RectTransform bossRt = bossCardGo.GetComponent<RectTransform>();
            bossRt.anchorMin = new Vector2(0.5f, 1f);
            bossRt.anchorMax = new Vector2(0.5f, 1f);
            bossRt.pivot = new Vector2(0.5f, 1f);
            bossRt.anchoredPosition = new Vector2(0f, -32f); // Spans top center
            bossRt.sizeDelta = new Vector2(620f, 108f);

            Image bossBg = bossCardGo.GetComponent<Image>();
            bossBg.sprite = panelBg;
            bossBg.type = Image.Type.Sliced;
            bossBg.color = Color.white;
            bossBg.raycastTarget = false;

            // Boss Skull Icon (Left)
            GameObject skullGo = CreateImageObject("Icon_Boss_Skull", bossCardGo.transform, skullIcon, new Vector2(40, -54), new Vector2(56, 56), new Vector2(0.5f, 0.5f));
            refs.icon = skullGo.GetComponent<Image>();

            // Boss Name Header
            GameObject nameGo = CreateTMPText("Boss_Name", bossCardGo.transform, font, "⚠️ BUG BOSS", 16, FontStyles.Bold, new Color(1f, 0.25f, 0.35f, 1f),
                TextAlignmentOptions.Left, new Vector2(80, -28), new Vector2(300, 22), new Vector2(0f, 0.5f));
            refs.nameText = nameGo.GetComponent<TextMeshProUGUI>();

            // Boss Health Text (1000 / 1000 (100%))
            GameObject hpTextGo = CreateTMPText("Boss_Health_Text", bossCardGo.transform, font, "1000 / 1000 (100%)", 14, FontStyles.Bold, new Color(1f, 0.85f, 0.2f, 1f),
                TextAlignmentOptions.Right, new Vector2(595, -28), new Vector2(210, 22), new Vector2(1f, 0.5f));
            refs.healthText = hpTextGo.GetComponent<TextMeshProUGUI>();

            // Boss Health Track
            GameObject trackGo = CreateImageObject("Boss_Track", bossCardGo.transform, barTrack, new Vector2(80, -68), new Vector2(516, 26), new Vector2(0f, 0.5f));
            trackGo.GetComponent<Image>().type = Image.Type.Sliced;

            // Ghost damage lag fill
            GameObject ghostGo = CreateFillBar("Boss_Ghost_Fill", trackGo.transform, barFill, new Color(1f, 0.8f, 0.2f, 0.8f));
            refs.ghost = ghostGo.GetComponent<Image>();

            // Primary crimson health fill
            GameObject fillGo = CreateFillBar("Boss_Bar_Fill", trackGo.transform, barFill, new Color(1f, 0.15f, 0.25f, 1f));
            refs.fill = fillGo.GetComponent<Image>();

            refs.container = bossCardGo;
            bossCardGo.SetActive(false); // Inactive until Boss phase

            return refs;
        }
        #endregion

        #region Helpers
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static GameObject CreateRowContainer(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateFillBar(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1.0f;
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateVerticalFillBar(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.fillAmount = 1.0f;
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateTMPText(string name, Transform parent, TMP_FontAsset font, string text, float fontSize,
            FontStyles fontStyle, Color color, TextAlignmentOptions alignment, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return go;
        }

        private static void HideOldRawText(Transform canvasTrans, string childName)
        {
            Transform t = canvasTrans.Find(childName);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }

        private static void DestroyChildIfFound(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
        #endregion
    }
}
