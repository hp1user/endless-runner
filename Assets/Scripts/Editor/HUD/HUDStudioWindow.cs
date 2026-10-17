using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;
using UI.Control;
using EndlessRunner.Data;

namespace EndlessRunner.Editor
{
    public class HUDStudioWindow : EditorWindow
    {
        private const string THEMES_FOLDER = "Assets/ScriptableObjects/HUDThemes";
        private const string SPRITES_PATH = "Assets/UI/Sprite/HUD";
        private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private List<HUDThemeProfile> themeList = new List<HUDThemeProfile>();
        private int selectedThemeIndex = 0;
        private HUDThemeProfile selectedTheme;
        private SerializedObject serializedTheme;

        private int activeTab = 0;
        private readonly string[] tabLabels = new string[]
        {
            "📱 Side Wings (Vertical)",
            "🖥️ Top Bar HUD",
            "💀 Boss Health Bar",
            "🎨 Colors & Fonts",
            "📐 Layout & Dimensions"
        };

        private Vector2 scrollPos;
        private UIManager sceneUIManager;

        [MenuItem("Tools/Endless Runner/HUD/HUD Sprite Studio", false, 15)]
        [MenuItem("Window/Endless Runner/HUD Sprite Studio", false, 201)]
        public static void OpenWindow()
        {
            var window = GetWindow<HUDStudioWindow>("HUD Sprite Studio");
            window.minSize = new Vector2(760, 640);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void AutoEnsureDefaultTheme()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying) return;
                string[] guids = AssetDatabase.FindAssets("t:HUDThemeProfile");
                if (guids.Length == 0)
                {
                    if (!Directory.Exists(THEMES_FOLDER))
                    {
                        Directory.CreateDirectory(THEMES_FOLDER);
                    }
                    string path = $"{THEMES_FOLDER}/Default_SciFi_Cyan.asset";
                    HUDThemeProfile profile = CreateInstance<HUDThemeProfile>();
                    profile.themeName = "Default Sci-Fi Cyan";
                    PopulateDefaultsStatic(profile);
                    AssetDatabase.CreateAsset(profile, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log("<color=cyan>[HUD Studio]</color> Auto-created default theme profile at " + path);
                }
            };
        }

        private void OnEnable()
        {
            EnsureThemesDirectory();
            RefreshThemeList();
            FindSceneUIManager();
        }

        private void FindSceneUIManager()
        {
            sceneUIManager = UnityEngine.Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        }

        private void EnsureThemesDirectory()
        {
            if (!Directory.Exists(THEMES_FOLDER))
            {
                Directory.CreateDirectory(THEMES_FOLDER);
                AssetDatabase.Refresh();
            }
        }

        private void RefreshThemeList()
        {
            themeList.Clear();
            string[] guids = AssetDatabase.FindAssets("t:HUDThemeProfile");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<HUDThemeProfile>(path);
                if (profile != null && !themeList.Contains(profile))
                {
                    themeList.Add(profile);
                }
            }

            // If no theme exists, create a default one
            if (themeList.Count == 0)
            {
                CreateNewTheme("Default_SciFi_Cyan");
            }
            else
            {
                if (selectedThemeIndex >= themeList.Count) selectedThemeIndex = 0;
                SelectTheme(themeList[selectedThemeIndex]);
            }
        }

        private void SelectTheme(HUDThemeProfile profile)
        {
            selectedTheme = profile;
            if (selectedTheme != null)
            {
                serializedTheme = new SerializedObject(selectedTheme);
                selectedThemeIndex = themeList.IndexOf(selectedTheme);
            }
            else
            {
                serializedTheme = null;
            }
        }

        private void OnGUI()
        {
            if (sceneUIManager == null) FindSceneUIManager();

            DrawHeader();
            DrawToolbar();
            DrawSceneStatusBar();

            if (selectedTheme == null || serializedTheme == null)
            {
                EditorGUILayout.HelpBox("No HUD Theme Profile selected. Click '➕ New Theme' to create one.", MessageType.Info);
                return;
            }

            serializedTheme.Update();

            EditorGUILayout.Space(6);
            activeTab = GUILayout.Toolbar(activeTab, tabLabels, GUILayout.Height(30));
            EditorGUILayout.Space(8);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (activeTab)
            {
                case 0:
                    DrawSideWingsTab();
                    break;
                case 1:
                    DrawTopBarTab();
                    break;
                case 2:
                    DrawBossHUDTab();
                    break;
                case 3:
                    DrawColorsAndFontsTab();
                    break;
                case 4:
                    DrawLayoutDimensionsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            if (serializedTheme.hasModifiedProperties)
            {
                serializedTheme.ApplyModifiedProperties();
                EditorUtility.SetDirty(selectedTheme);
            }

            DrawBottomActionBar();
        }

        #region Header & Toolbars

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.2f, 0.85f, 1f) }
            };

            GUILayout.Label("⚡ HUD SPRITE & THEME STUDIO", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Endless Runner UI Toolkit", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Visually swap HUD sprites, icons, fill bars, and colors for Side Wings, Top Bar, and Boss health with 1-click scene sync.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Active Theme Profile:", EditorStyles.miniLabel, GUILayout.Width(130));

            // Dropdown of profiles
            string[] themeNames = new string[themeList.Count];
            for (int i = 0; i < themeList.Count; i++)
            {
                themeNames[i] = themeList[i] != null ? themeList[i].themeName : $"Theme {i}";
            }

            int newIndex = EditorGUILayout.Popup(selectedThemeIndex, themeNames, EditorStyles.toolbarPopup, GUILayout.Width(220));
            if (newIndex != selectedThemeIndex && newIndex >= 0 && newIndex < themeList.Count)
            {
                selectedThemeIndex = newIndex;
                SelectTheme(themeList[selectedThemeIndex]);
            }

            EditorGUILayout.Space(8);

            if (GUILayout.Button("➕ New Theme", EditorStyles.toolbarButton, GUILayout.Width(95)))
            {
                CreateNewTheme();
            }

            if (GUILayout.Button("⎘ Duplicate", EditorStyles.toolbarButton, GUILayout.Width(85)))
            {
                if (selectedTheme != null) DuplicateTheme(selectedTheme);
            }

            if (GUILayout.Button("💾 Save", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                if (selectedTheme != null)
                {
                    EditorUtility.SetDirty(selectedTheme);
                    AssetDatabase.SaveAssets();
                    ShowNotification(new GUIContent("Theme Saved!"));
                }
            }

            GUILayout.FlexibleSpace();

            if (selectedTheme != null)
            {
                if (GUILayout.Button("Ping Asset", EditorStyles.toolbarButton, GUILayout.Width(75)))
                {
                    EditorGUIUtility.PingObject(selectedTheme);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSceneStatusBar()
        {
            EditorGUILayout.BeginHorizontal("box");

            if (sceneUIManager != null)
            {
                string variantStr = sceneUIManager.activeVariant == HUDLayoutVariant.SideWings 
                    ? "<color=#00ffff>Side Wings (Vertical Vitals & Ammo)</color>" 
                    : "<color=#ffaa00>Top Bar (Top-Left / Top-Right)</color>";

                GUILayout.Label($"Active Scene HUD: <b>{variantStr}</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = (sceneUIManager.activeVariant == HUDLayoutVariant.SideWings) ? new Color(0.2f, 0.8f, 1f) : Color.white;
                if (GUILayout.Button("📱 Side Wings", EditorStyles.miniButtonLeft, GUILayout.Width(95)))
                {
                    Undo.RecordObject(sceneUIManager, "Switch to Side Wings");
                    sceneUIManager.SwitchVariant(HUDLayoutVariant.SideWings);
                    EditorUtility.SetDirty(sceneUIManager);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneUIManager.gameObject.scene);
                }

                GUI.backgroundColor = (sceneUIManager.activeVariant == HUDLayoutVariant.TopBar) ? new Color(1f, 0.7f, 0.2f) : Color.white;
                if (GUILayout.Button("🖥️ Top Bar", EditorStyles.miniButtonMid, GUILayout.Width(85)))
                {
                    Undo.RecordObject(sceneUIManager, "Switch to Top Bar");
                    sceneUIManager.SwitchVariant(HUDLayoutVariant.TopBar);
                    EditorUtility.SetDirty(sceneUIManager);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneUIManager.gameObject.scene);
                }

                GUI.backgroundColor = (sceneUIManager.bossHealthContainer != null && sceneUIManager.bossHealthContainer.activeSelf) ? new Color(1f, 0.3f, 0.3f) : Color.white;
                if (GUILayout.Button("💀 Test Boss Bar", EditorStyles.miniButtonRight, GUILayout.Width(110)))
                {
                    sceneUIManager.ToggleTestBossHealth();
                    EditorUtility.SetDirty(sceneUIManager);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneUIManager.gameObject.scene);
                }

                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.6f, 0.3f);
                GUILayout.Label("⚠️ No UIManager found in active scene. Load 'GameView_EndlessRunner.unity' to live-test.", EditorStyles.miniLabel);
                GUI.color = Color.white;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Find UIManager", EditorStyles.miniButton, GUILayout.Width(110)))
                {
                    FindSceneUIManager();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 0: Side Wings HUD

        private void DrawSideWingsTab()
        {
            DrawSectionHeader("📱 Vertical Side Docks (Flush Bezel Frame)", "The left dock hosts vertical health & armor meters; the right dock hosts vertical current & reserve ammo meters.");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("dockLeftPanel", "Left Dock Panel Frame", "Background frame for the left status card (110x520 slim vertical dock flush with left screen edge).", "110x520");
            DrawSpriteCard("verticalTrack", "Vertical Bar Track", "Sunken background channel for health, armor, and ammo bars.", "20x340");
            DrawSpriteCard("heartIcon", "Health Meter Icon", "Heart crest shown above the vertical health fill bar.", "32x32");
            DrawSpriteCard("bulletIcon", "Loaded Ammo Icon", "Bullet crest shown above the current ammo fill bar.", "28x28");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("dockRightPanel", "Right Dock Panel Frame", "Background frame for the right ammo card (110x520 slim vertical dock flush with right screen edge).", "110x520");
            DrawSpriteCard("verticalFill", "Vertical Bar Fill", "Bottom-to-top glowing fill sprite for health, armor, and ammo meters.", "20x340");
            DrawSpriteCard("shieldIcon", "Armor Meter Icon", "Shield crest shown above the vertical armor fill bar.", "32x32");
            DrawSpriteCard("ammoBoxIcon", "Reserve Ammo Icon", "Ammo box crest shown above the reserve ammo bar.", "28x28");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawSpriteCard("defaultWeaponIcon", "Default Weapon Silhouette", "Default gun silhouette displayed when no custom weapon is loaded.", "84x50");
        }

        #endregion

        #region Tab 1: Top Bar HUD

        private void DrawTopBarTab()
        {
            DrawSectionHeader("🖥️ Top Bar HUD (Classic Top-Left & Top-Right)", "Horizontal health & armor bars on the top-left; weapon card and ammo counter on the top-right.");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("topStatusPanel", "Top-Left Status Card", "Background panel for player health & armor bars (classic top-left card).", "400x120");
            DrawSpriteCard("horizontalTrack", "Horizontal Bar Track", "Horizontal track sprite for health and armor bars.", "280x24");
            DrawSpriteCard("heartIcon", "Health Icon", "Heart icon displayed on the left of the health bar.", "32x32");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("topAmmoPanel", "Top-Right Ammo Card", "Background panel for weapon silhouette and ammo count (top-right card).", "340x120");
            DrawSpriteCard("horizontalFill", "Horizontal Bar Fill", "Horizontal fill sprite for health and armor bars.", "280x24");
            DrawSpriteCard("shieldIcon", "Armor Icon", "Shield icon displayed on the left of the armor bar.", "32x32");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 2: Boss Health Bar

        private void DrawBossHUDTab()
        {
            DrawSectionHeader("💀 Boss Health HUD (Top Center)", "Top-center hazard banner with glowing cyber-skull crest, boss name, and health readout.");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("bossPanel", "Boss Frame Panel", "Obsidian and hazard striped border banner (620x108).", "620x108");
            DrawSpriteCard("bossSkullIcon", "Boss Skull Crest", "Glowing cyber-skull crest displayed on the left side of the boss banner.", "48x48");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * 0.48f));
            DrawSpriteCard("bossTrack", "Boss Bar Track", "Horizontal channel track for the boss health bar.", "440x26");
            DrawSpriteCard("bossFill", "Boss Bar Fill", "Crimson health fill and yellow damage-lag ghost bar for the boss.", "440x26");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tab 3: Colors & Fonts

        private void DrawColorsAndFontsTab()
        {
            DrawSectionHeader("🎨 Color Palettes & Visual Tints", "Customize the glowing theme colors for vitals, ammo, and boss bars.");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("healthColor"), new GUIContent("Health Meter Color", "Color for primary health fill bar (Neon Emerald recommended)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("armorColor"), new GUIContent("Armor Meter Color", "Color for primary armor fill bar (Electric Cyan recommended)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("ammoColor"), new GUIContent("Ammo Meter Color", "Color for loaded & reserve ammo fills (Amber Gold recommended)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("ghostDamageColor"), new GUIContent("Damage Lag Ghost Color", "Color for delayed damage ghost lag bar (Crimson Red recommended)."));
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("bossHealthColor"), new GUIContent("Boss Health Color", "Color for boss health fill bar."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("bossGhostColor"), new GUIContent("Boss Lag Ghost Color", "Color for boss damage lag bar (Gold recommended)."));
            EditorGUILayout.EndVertical();

            DrawSectionHeader("🔤 Typography & Font Asset", "TextMesh Pro font asset used across all HUD numeric readouts and labels.");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("fontAsset"), new GUIContent("HUD Font Asset", "TMP Font Asset (LiberationSans SDF recommended)."));
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Tab 4: Layout & Dimensions

        private void DrawLayoutDimensionsTab()
        {
            DrawSectionHeader("📐 Side Wings Dock Geometry & Positioning", "Fine-tune the size and screen-edge positioning of the vertical side wings.");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("dockWidth"), new GUIContent("Dock Width (px)", "Width of the slim vertical dock (110px recommended for flush edges)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("dockHeight"), new GUIContent("Dock Height (px)", "Height of the slim vertical dock (490px - 520px recommended)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("dockOffsetY"), new GUIContent("Vertical Offset Y (px)", "Top anchor Y offset (-80px recommended to clear top status bar)."));
            EditorGUILayout.PropertyField(serializedTheme.FindProperty("dockMarginX"), new GUIContent("Edge Margin X (px)", "0 = Flush with screen glass bezels. Increase if margin is desired."));
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Reusable GUI Card Helper

        private void DrawSectionHeader(string title, string subtitle)
        {
            EditorGUILayout.Space(4);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
            GUILayout.Label(title, headerStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                GUILayout.Label(subtitle, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.Space(2);
        }

        private void DrawSpriteCard(string propertyName, string title, string tooltip, string recommendedRes)
        {
            SerializedProperty prop = serializedTheme.FindProperty(propertyName);
            if (prop == null) return;

            Sprite currentSprite = prop.objectReferenceValue as Sprite;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // Thumbnail preview (48x48)
            Texture2D thumb = null;
            if (currentSprite != null)
            {
                thumb = AssetPreview.GetAssetPreview(currentSprite);
                if (thumb == null) thumb = currentSprite.texture;
            }

            Rect thumbRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
            EditorGUI.DrawRect(thumbRect, new Color(0.12f, 0.12f, 0.15f, 1f));

            if (thumb != null)
            {
                GUI.DrawTexture(thumbRect, thumb, ScaleMode.ScaleToFit);
            }
            else
            {
                GUIStyle placeholderStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(thumbRect, "None", placeholderStyle);
            }

            EditorGUILayout.Space(6);

            // Details and ObjectField
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(recommendedRes))
            {
                GUI.color = new Color(0.6f, 0.85f, 1f);
                GUILayout.Label($"Rec: {recommendedRes}", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(prop, GUIContent.none);

            EditorGUILayout.BeginHorizontal();
            if (currentSprite != null)
            {
                string info = $"{currentSprite.rect.width:F0}x{currentSprite.rect.height:F0} px";
                GUILayout.Label(info, EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = Color.gray;
                GUILayout.Label("Unassigned", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();

            if (currentSprite != null)
            {
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(48)))
                {
                    EditorGUIUtility.PingObject(currentSprite);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Bottom Action Bar

        private void DrawBottomActionBar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // Apply Sprites to Active Scene (Instant Live Sync)
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);
            if (GUILayout.Button("⚡ Apply Sprites to Active Scene", GUILayout.Height(34)))
            {
                if (selectedTheme != null)
                {
                    SciFiHUDBuilder.ApplyThemeToScene(selectedTheme, silent: false);
                    ShowNotification(new GUIContent("HUD Sprites Synchronized!"));
                }
            }

            // Full Rebuild HUD
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f);
            if (GUILayout.Button("🔨 Full Rebuild HUD", GUILayout.Height(34), GUILayout.Width(160)))
            {
                if (EditorUtility.DisplayDialog("Rebuild Scene HUD?", 
                    "This will completely reconstruct the HUD hierarchy in the active scene using the current theme profile.\n\nContinue?", "Yes", "Cancel"))
                {
                    SciFiHUDBuilder.BuildHUDInternal(silent: false, selectedTheme);
                    ShowNotification(new GUIContent("HUD Rebuilt Successfully!"));
                }
            }

            // Reset to default assets
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUILayout.Button("🔄 Defaults", GUILayout.Height(34), GUILayout.Width(85)))
            {
                if (EditorUtility.DisplayDialog("Reset to Default Sprites?", 
                    "Reset all sprite slots in this theme profile to the standard project assets?", "Yes", "Cancel"))
                {
                    ResetCurrentThemeToDefaults();
                    ShowNotification(new GUIContent("Reset to Defaults"));
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Preset Management Logic

        private void CreateNewTheme(string suggestedName = "New_HUD_Theme")
        {
            EnsureThemesDirectory();

            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{THEMES_FOLDER}/{suggestedName}.asset");
            HUDThemeProfile newProfile = CreateInstance<HUDThemeProfile>();
            newProfile.themeName = Path.GetFileNameWithoutExtension(uniquePath).Replace("_", " ");

            PopulateDefaults(newProfile);

            AssetDatabase.CreateAsset(newProfile, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshThemeList();
            SelectTheme(newProfile);
            EditorGUIUtility.PingObject(newProfile);
        }

        private void DuplicateTheme(HUDThemeProfile source)
        {
            EnsureThemesDirectory();

            string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{THEMES_FOLDER}/{source.name}_Copy.asset");
            var clone = Instantiate(source);
            clone.themeName = source.themeName + " (Copy)";

            AssetDatabase.CreateAsset(clone, copyPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshThemeList();
            SelectTheme(clone);
            EditorGUIUtility.PingObject(clone);
        }

        private void ResetCurrentThemeToDefaults()
        {
            if (selectedTheme == null) return;
            Undo.RecordObject(selectedTheme, "Reset Theme To Defaults");
            PopulateDefaults(selectedTheme);
            EditorUtility.SetDirty(selectedTheme);
            AssetDatabase.SaveAssets();
            serializedTheme = new SerializedObject(selectedTheme);
        }

        private void PopulateDefaults(HUDThemeProfile profile)
        {
            PopulateDefaultsStatic(profile);
        }

        public static void PopulateDefaultsStatic(HUDThemeProfile profile)
        {
            profile.dockLeftPanel = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Vertical_Dock_Left.png");
            profile.dockRightPanel = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Vertical_Dock_Right.png");
            profile.verticalTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Track_Slim.png");
            profile.verticalFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Fill_Slim.png");
            profile.heartIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Heart.png");
            profile.shieldIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Shield.png");
            profile.bulletIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Bullet.png");
            profile.ammoBoxIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Ammo_Box.png");
            profile.defaultWeaponIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprite/AR.png");

            profile.topStatusPanel = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Panel_Bg.png");
            profile.topAmmoPanel = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Ammo_Panel.png");
            profile.horizontalTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Track.png");
            profile.horizontalFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Fill.png");

            profile.bossPanel = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Boss_Panel.png");
            profile.bossSkullIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/Icon_Boss_Skull.png");
            profile.bossTrack = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Track.png");
            profile.bossFill = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/HUD_Bar_Fill.png");

            profile.dockWidth = 110f;
            profile.dockHeight = 490f;
            profile.dockOffsetY = -80f;
            profile.dockMarginX = 0f;

            profile.healthColor = new Color(0.12f, 0.95f, 0.58f, 1f);
            profile.armorColor = new Color(0.15f, 0.85f, 1.0f, 1f);
            profile.ammoColor = new Color(1.0f, 0.72f, 0.12f, 1f);
            profile.ghostDamageColor = new Color(0.95f, 0.22f, 0.22f, 1f);
            profile.bossHealthColor = new Color(0.95f, 0.15f, 0.20f, 1f);
            profile.bossGhostColor = new Color(1.0f, 0.78f, 0.15f, 1f);

            profile.fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        }

        #endregion
    }
}
