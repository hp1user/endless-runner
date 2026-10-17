using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EndlessRunner.Data;
using Player.Control;

namespace EndlessRunner.Editor
{
    public class CharacterStudioWindow : EditorWindow
    {
        private const string CHARACTERS_FOLDER = "Assets/ScriptableObjects/Characters";
        private const string PREFABS_EXPORT_FOLDER = "Assets/Prefabs/Characters";

        private List<CharacterData> characterList = new List<CharacterData>();
        private List<CharacterData> filteredList = new List<CharacterData>();
        private CharacterData selectedCharacter;
        private SerializedObject serializedSelected;

        private string searchFilter = "";
        private Vector2 listScrollPos;
        private Vector2 detailScrollPos;
        private int currentTab = 0;
        private readonly string[] tabHeaders = new string[] { "👤 Identity & Visuals", "⚔️ Combat Stats", "⚡ Skills & Loadout", "🎮 Live Scene Test" };

        private PlayerController targetScenePlayer;
        private bool isInitializingDefaults = false;

        [MenuItem("Tools/Endless Runner/Character Studio", false, 12)]
        [MenuItem("Window/Character Studio", false, 202)]
        public static void OpenWindow()
        {
            var win = GetWindow<CharacterStudioWindow>("Character Studio");
            win.minSize = new Vector2(780, 620);
            win.Show();
        }

        private void OnEnable()
        {
            RefreshCharacterList();
            FindScenePlayer();
        }

        private void RefreshCharacterList()
        {
            characterList.Clear();

            if (!Directory.Exists(CHARACTERS_FOLDER))
            {
                Directory.CreateDirectory(CHARACTERS_FOLDER);
                AssetDatabase.Refresh();
            }

            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var charData = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (charData != null)
                {
                    characterList.Add(charData);
                }
            }

            // Auto-create defaults if project has none
            if (characterList.Count == 0)
            {
                CreateDefaultProfiles();
            }

            CheckAndRepairDefaultProfiles();

            FilterList();

            if (selectedCharacter == null && characterList.Count > 0)
            {
                SelectCharacter(characterList[0]);
            }
            else if (selectedCharacter != null)
            {
                SelectCharacter(selectedCharacter);
            }
        }

        private void FilterList()
        {
            filteredList.Clear();
            string query = searchFilter.Trim().ToLowerInvariant();

            foreach (var c in characterList)
            {
                if (c == null) continue;
                if (string.IsNullOrEmpty(query) ||
                    c.characterName.ToLowerInvariant().Contains(query) ||
                    c.characterTitle.ToLowerInvariant().Contains(query) ||
                    c.characterID.ToLowerInvariant().Contains(query))
                {
                    filteredList.Add(c);
                }
            }
        }

        private void SelectCharacter(CharacterData character)
        {
            selectedCharacter = character;
            if (selectedCharacter != null)
            {
                serializedSelected = new SerializedObject(selectedCharacter);
            }
            else
            {
                serializedSelected = null;
            }
            GUI.FocusControl(null);
        }

        private void OnGUI()
        {
            DrawTopToolbar();

            EditorGUILayout.BeginHorizontal();

            // Left Sidebar: Character List
            DrawCharacterListSidebar();

            // Right Main Panel: Detail Inspector & Test Station
            DrawCharacterDetailPanel();

            EditorGUILayout.EndHorizontal();
        }

        #region Top Toolbar

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🎭 Character Studio", EditorStyles.boldLabel, GUILayout.Width(140));

            GUILayout.Space(10);

            // Search bar
            string newSearch = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(220));
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                FilterList();
            }

            if (!string.IsNullOrEmpty(searchFilter))
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    searchFilter = "";
                    FilterList();
                    GUI.FocusControl(null);
                }
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("➕ New Character", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                CreateNewCharacter();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton, GUILayout.Width(75)))
            {
                RefreshCharacterList();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Left Sidebar: Character List

        private void DrawCharacterListSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));

            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label($"Roster ({characterList.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Preload Defaults", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                CreateDefaultProfiles();
                FilterList();
                if (characterList.Count > 0 && selectedCharacter == null) SelectCharacter(characterList[0]);
            }
            EditorGUILayout.EndHorizontal();

            listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos, "box");

            if (filteredList.Count == 0)
            {
                EditorGUILayout.HelpBox("No characters found. Click '➕ New Character' to create one.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < filteredList.Count; i++)
                {
                    var c = filteredList[i];
                    if (c == null) continue;

                    bool isSelected = (c == selectedCharacter);

                    GUI.backgroundColor = isSelected ? new Color(0.35f, 0.65f, 1f) : new Color(0.95f, 0.95f, 0.95f);
                    EditorGUILayout.BeginVertical("box");
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.BeginHorizontal();

                    // Mini 3D preview / icon thumbnail
                    Texture2D thumb = null;
                    if (c.characterIcon != null) thumb = c.characterIcon.texture;
                    if (thumb == null && c.modelPrefab != null) thumb = AssetPreview.GetAssetPreview(c.modelPrefab);

                    if (thumb != null)
                    {
                        GUILayout.Label(thumb, GUILayout.Width(38), GUILayout.Height(38));
                    }
                    else
                    {
                        GUILayout.Box("👤", GUILayout.Width(38), GUILayout.Height(38));
                    }

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(c.characterName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    // Clone button
                    if (GUILayout.Button("⎘", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        CloneCharacter(c);
                        GUIUtility.ExitGUI();
                    }

                    // Delete button
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Character?", $"Are you sure you want to delete '{c.characterName}'?", "Yes", "No"))
                        {
                            DeleteCharacter(c);
                            GUIUtility.ExitGUI();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Label(string.IsNullOrEmpty(c.characterTitle) ? "Operative" : c.characterTitle, EditorStyles.miniLabel);

                    // Stats badges
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = new Color(0.5f, 1f, 0.5f);
                    GUILayout.Label($"HP:{c.baseHealth}", EditorStyles.miniLabel, GUILayout.Width(50));
                    GUI.color = new Color(0.5f, 0.8f, 1f);
                    GUILayout.Label($"ARM:{c.baseArmor}", EditorStyles.miniLabel, GUILayout.Width(55));
                    GUI.color = new Color(1f, 0.85f, 0.4f);
                    GUILayout.Label($"SPD:{c.movementSpeed}", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    // Click entire box to select
                    if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        SelectCharacter(c);
                        Event.current.Use();
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Right Detail Panel: Inspector & Test Station

        private void DrawCharacterDetailPanel()
        {
            EditorGUILayout.BeginVertical("box");

            if (selectedCharacter == null || serializedSelected == null)
            {
                EditorGUILayout.HelpBox("Select a character from the list on the left or click '➕ New Character' to begin.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            serializedSelected.Update();

            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            // 1. Hero Preview Banner
            DrawHeroPreviewCard();

            EditorGUILayout.Space(8);

            // 2. Tabs Selector
            currentTab = GUILayout.Toolbar(currentTab, tabHeaders, GUILayout.Height(28));
            EditorGUILayout.Space(6);

            // 3. Tab Contents
            switch (currentTab)
            {
                case 0:
                    DrawIdentityAndVisualsTab();
                    break;
                case 1:
                    DrawCombatStatsTab();
                    break;
                case 2:
                    DrawSkillsAndLoadoutTab();
                    break;
                case 3:
                    DrawLiveSceneTestTab();
                    break;
            }

            EditorGUILayout.Space(12);

            // 4. Global Action Footer (Always accessible)
            DrawFooterActionButtons();

            EditorGUILayout.EndScrollView();

            if (serializedSelected.hasModifiedProperties)
            {
                serializedSelected.ApplyModifiedProperties();
                EditorUtility.SetDirty(selectedCharacter);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHeroPreviewCard()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // 3D Preview Image
            Texture2D previewTex = null;
            if (selectedCharacter.modelPrefab != null)
            {
                previewTex = AssetPreview.GetAssetPreview(selectedCharacter.modelPrefab);
            }
            if (previewTex == null && selectedCharacter.characterIcon != null)
            {
                previewTex = selectedCharacter.characterIcon.texture;
            }

            if (previewTex != null)
            {
                GUILayout.Label(previewTex, GUILayout.Width(90), GUILayout.Height(90));
            }
            else
            {
                GUILayout.Box("🎭\nNo Model", GUILayout.Width(90), GUILayout.Height(90));
            }

            GUILayout.Space(10);

            // Character Info Banner
            EditorGUILayout.BeginVertical();
            GUILayout.Label(selectedCharacter.characterName, EditorStyles.boldLabel);
            GUILayout.Label(string.IsNullOrEmpty(selectedCharacter.characterTitle) ? "Operative" : selectedCharacter.characterTitle, EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(selectedCharacter.description))
            {
                GUILayout.Label(selectedCharacter.description, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(4);

            // Skill Chips
            EditorGUILayout.BeginHorizontal();
            if (selectedCharacter.activeSkill != null)
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                GUILayout.Box($"⚡ Skill: {selectedCharacter.activeSkill.skillName}", EditorStyles.miniButton);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUILayout.Box("⚡ No Skill Assigned", EditorStyles.miniLabel);
            }

            if (selectedCharacter.activeUltimate != null)
            {
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                GUILayout.Box($"💥 Ult: {selectedCharacter.activeUltimate.skillName}", EditorStyles.miniButton);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUILayout.Box("💥 No Ultimate Assigned", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawIdentityAndVisualsTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("👤 Character Identity & Model", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedSelected.FindProperty("characterName"), new GUIContent("Character Name"));
            EditorGUILayout.PropertyField(serializedSelected.FindProperty("characterID"), new GUIContent("Character ID"));
            EditorGUILayout.PropertyField(serializedSelected.FindProperty("characterTitle"), new GUIContent("Role / Title"));
            EditorGUILayout.PropertyField(serializedSelected.FindProperty("description"), new GUIContent("Bio / Description"));
            EditorGUILayout.PropertyField(serializedSelected.FindProperty("characterIcon"), new GUIContent("UI Portrait Icon"));

            EditorGUILayout.Space(8);
            GUILayout.Label("🎮 3D Model & Rigging Configuration", EditorStyles.boldLabel);

            SerializedProperty modelProp = serializedSelected.FindProperty("modelPrefab");
            EditorGUILayout.PropertyField(modelProp, new GUIContent("Model / Prefab Slot"));

            GameObject model = modelProp.objectReferenceValue as GameObject;
            if (model != null)
            {
                Avatar avatar = selectedCharacter.GetAvatar();
                EditorGUILayout.BeginHorizontal();
                if (avatar != null && avatar.isHuman)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"✓ Valid Humanoid Avatar: {avatar.name}", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(1f, 0.6f, 0.2f);
                    GUILayout.Label("⚠️ Model does not have a Humanoid Avatar", EditorStyles.boldLabel);
                    GUI.color = Color.white;

                    if (GUILayout.Button("⚡ Auto-Fix to Humanoid", GUILayout.Width(170)))
                    {
                        AutoFixModelImporterToHumanoid(model);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a 3D model or configured Character Prefab here.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(serializedSelected.FindProperty("customAnimatorController"), new GUIContent("Custom Animator (Optional)"));
            if (selectedCharacter.customAnimatorController == null)
            {
                EditorGUILayout.HelpBox("Using default 'Man Controller' (supports all 9 firearm animation layers and locomotion).", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCombatStatsTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("⚔️ Combat & Health Attributes", EditorStyles.boldLabel);

            SerializedProperty hpProp = serializedSelected.FindProperty("baseHealth");
            SerializedProperty armProp = serializedSelected.FindProperty("baseArmor");

            EditorGUILayout.Slider(hpProp, 50f, 1000f, new GUIContent("Base Health (HP)"));
            EditorGUILayout.Slider(armProp, 0f, 200f, new GUIContent("Base Armor"));

            EditorGUILayout.Space(8);
            GUILayout.Label("🏃 Locomotion & Lane Physics", EditorStyles.boldLabel);

            SerializedProperty spdProp = serializedSelected.FindProperty("movementSpeed");
            SerializedProperty multProp = serializedSelected.FindProperty("moveSpeedMultiplier");
            SerializedProperty smoothProp = serializedSelected.FindProperty("strafeAnimationSmoothing");
            SerializedProperty laneProp = serializedSelected.FindProperty("laneDistance");

            EditorGUILayout.Slider(spdProp, 5f, 30f, new GUIContent("Movement Speed (m/s)"));
            EditorGUILayout.Slider(multProp, 0.5f, 3.0f, new GUIContent("Speed Multiplier"));
            EditorGUILayout.Slider(smoothProp, 1f, 20f, new GUIContent("Strafe Smoothing"));
            EditorGUILayout.Slider(laneProp, 1f, 4f, new GUIContent("Lane Distance (X Spacing)"));

            EditorGUILayout.Space(8);
            // Quick Stat Presets
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡ Quick Stat Archetypes:", EditorStyles.miniBoldLabel, GUILayout.Width(150));
            if (GUILayout.Button("🛡️ Tank", EditorStyles.miniButton))
            {
                hpProp.floatValue = 220f;
                armProp.floatValue = 50f;
                spdProp.floatValue = 8.5f;
            }
            if (GUILayout.Button("⚡ Scout / Runner", EditorStyles.miniButton))
            {
                hpProp.floatValue = 90f;
                armProp.floatValue = 10f;
                spdProp.floatValue = 13.5f;
            }
            if (GUILayout.Button("🎯 Balanced", EditorStyles.miniButton))
            {
                hpProp.floatValue = 120f;
                armProp.floatValue = 25f;
                spdProp.floatValue = 10f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillsAndLoadoutTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("⚡ Tactical Skill & Ultimate Loadout", EditorStyles.boldLabel);

            SerializedProperty skillProp = serializedSelected.FindProperty("activeSkill");
            SerializedProperty ultProp = serializedSelected.FindProperty("activeUltimate");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(skillProp, new GUIContent("Primary Tactical Skill"));
            if (GUILayout.Button("Rapid Fire", EditorStyles.miniButton, GUILayout.Width(75)))
            {
                skillProp.objectReferenceValue = LoadSkillByName("RapidFire");
            }
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                skillProp.objectReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();

            if (selectedCharacter.activeSkill != null)
            {
                DrawSkillDetailsBox(selectedCharacter.activeSkill, "Tactical Skill Details");
            }

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ultProp, new GUIContent("Ultimate Ability"));
            if (GUILayout.Button("AOE Bomb", EditorStyles.miniButton, GUILayout.Width(75)))
            {
                ultProp.objectReferenceValue = LoadSkillByName("AOE");
            }
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(45)))
            {
                ultProp.objectReferenceValue = null;
            }
            EditorGUILayout.EndHorizontal();

            if (selectedCharacter.activeUltimate != null)
            {
                DrawSkillDetailsBox(selectedCharacter.activeUltimate, "Ultimate Details");
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.PropertyField(serializedSelected.FindProperty("starterWeaponID"), new GUIContent("Starting Weapon ID"));

            EditorGUILayout.EndVertical();
        }

        private void DrawSkillDetailsBox(SkillData skill, string header)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            if (skill.icon != null)
            {
                GUILayout.Label(skill.icon.texture, GUILayout.Width(30), GUILayout.Height(30));
            }
            EditorGUILayout.BeginVertical();
            GUILayout.Label($"<b>{header}: {skill.skillName}</b> ({skill.skillType})", EditorStyles.wordWrappedMiniLabel);
            string recharge = skill.isKillBasedRecharge ? $"{skill.rechargeRequirement} Kills" : $"{skill.rechargeRequirement}s Cooldown";
            GUILayout.Label($"Recharge: {recharge} | Damage: {skill.damage} (Boss: {skill.bossDamage}) | Duration: {skill.effectDuration}s", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawLiveSceneTestTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🎮 Live Scene Test & Prefab Export Station", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign this character directly to the Player in the active scene to test its model, animations, stats, and abilities in real-time!", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            targetScenePlayer = (PlayerController)EditorGUILayout.ObjectField("Scene Player Target", targetScenePlayer, typeof(PlayerController), true);
            if (GUILayout.Button("🔍 Find Player", GUILayout.Width(100)))
            {
                FindScenePlayer();
            }
            EditorGUILayout.EndHorizontal();

            if (targetScenePlayer == null)
            {
                EditorGUILayout.HelpBox("No PlayerController found in the open scene. Open 'GameView_EndlessRunner.unity' to test live.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.green;
                GUILayout.Label($"✓ Target Player Locked: {targetScenePlayer.gameObject.name}", EditorStyles.boldLabel);
                GUI.color = Color.white;

                if (GUILayout.Button("🎯 Ping in Hierarchy", GUILayout.Width(140)))
                {
                    EditorGUIUtility.PingObject(targetScenePlayer.gameObject);
                    Selection.activeGameObject = targetScenePlayer.gameObject;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();

            // Big Assign Button
            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.45f);
            if (GUILayout.Button($"🎮 Assign '{selectedCharacter.characterName}' to Player", GUILayout.Height(38)))
            {
                AssignCharacterToScenePlayer();
            }

            // Quick Play Test Button
            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
            if (GUILayout.Button(Application.isPlaying ? "⏹️ Stop Play Test" : "▶️ Quick Play Test", GUILayout.Height(38), GUILayout.Width(170)))
            {
                if (Application.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    AssignCharacterToScenePlayer();
                    EditorApplication.isPlaying = true;
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Export Prefab Button
            GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
            if (GUILayout.Button($"📦 Export '{selectedCharacter.characterName}' as Standalone Prefab", GUILayout.Height(30)))
            {
                ExportCharacterAsPrefab();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawFooterActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.45f);
            if (GUILayout.Button("🎮 Assign to Player", GUILayout.Height(32)))
            {
                AssignCharacterToScenePlayer();
            }

            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
            if (GUILayout.Button(Application.isPlaying ? "⏹️ Stop Play Test" : "▶️ Quick Play Test", GUILayout.Height(32), GUILayout.Width(150)))
            {
                if (Application.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    AssignCharacterToScenePlayer();
                    EditorApplication.isPlaying = true;
                }
            }

            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUILayout.Button("💾 Save Changes", GUILayout.Height(32), GUILayout.Width(110)))
            {
                EditorUtility.SetDirty(selectedCharacter);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>[CharacterStudio]</color> Saved changes to '{selectedCharacter.characterName}'.");
            }

            if (GUILayout.Button("🎯 Ping Asset", GUILayout.Height(32), GUILayout.Width(90)))
            {
                EditorGUIUtility.PingObject(selectedCharacter);
                Selection.activeObject = selectedCharacter;
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Operations & Application Logic

        private void CheckAndRepairDefaultProfiles()
        {
            bool modified = false;
            foreach (var c in characterList)
            {
                if (c == null) continue;
                if (c.characterID == "ch15_agent")
                {
                    if (c.modelPrefab == null)
                    {
                        c.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/CH15/Ch15_nonPBR.fbx");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                    if (c.activeSkill == null)
                    {
                        c.activeSkill = LoadSkillByName("RapidFire");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                    if (c.activeUltimate == null)
                    {
                        c.activeUltimate = LoadSkillByName("AOE");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                }
                else if (c.characterID == "mocap_runner")
                {
                    if (c.modelPrefab == null)
                    {
                        c.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/Rough/Man_T.fbx");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                    if (c.activeSkill == null)
                    {
                        c.activeSkill = LoadSkillByName("RapidFire");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                    if (c.activeUltimate == null)
                    {
                        c.activeUltimate = LoadSkillByName("AOE");
                        EditorUtility.SetDirty(c);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private void FindScenePlayer()
        {
#if UNITY_2023_1_OR_NEWER
            targetScenePlayer = UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
#else
            targetScenePlayer = GameObject.FindObjectOfType<PlayerController>();
#endif
            if (targetScenePlayer == null)
            {
                GameObject playerGo = GameObject.FindWithTag("Player");
                if (playerGo == null) playerGo = GameObject.Find("Man_Rig");
                if (playerGo != null) targetScenePlayer = playerGo.GetComponent<PlayerController>();
            }
        }

        private void AssignCharacterToScenePlayer()
        {
            if (selectedCharacter == null) return;

            // Auto-heal model if null
            if (selectedCharacter.modelPrefab == null)
            {
                if (selectedCharacter.characterID == "ch15_agent")
                {
                    selectedCharacter.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/CH15/Ch15_nonPBR.fbx");
                    EditorUtility.SetDirty(selectedCharacter);
                }
                else if (selectedCharacter.characterID == "mocap_runner")
                {
                    selectedCharacter.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/Rough/Man_T.fbx");
                    EditorUtility.SetDirty(selectedCharacter);
                }

                if (selectedCharacter.modelPrefab == null)
                {
                    EditorUtility.DisplayDialog("Model Missing", $"Character '{selectedCharacter.characterName}' has no 3D Model assigned.\nPlease assign an FBX model or prefab into the 'Model / Prefab Slot' first.", "OK");
                    return;
                }
            }

            if (targetScenePlayer == null)
            {
                FindScenePlayer();
            }

            if (targetScenePlayer == null)
            {
                EditorUtility.DisplayDialog("Player Not Found", "Could not find a PlayerController in the active scene.\nPlease make sure 'GameView_EndlessRunner.unity' is loaded.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(targetScenePlayer.gameObject, $"Assign Character: {selectedCharacter.characterName}");

            // Apply character data
            targetScenePlayer.ApplyCharacterData(selectedCharacter, true, true);

            // Mark scene dirty
            EditorUtility.SetDirty(targetScenePlayer);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(targetScenePlayer.gameObject.scene);
                SceneView.RepaintAll();
            }

            EditorGUIUtility.PingObject(targetScenePlayer.gameObject);
            Selection.activeGameObject = targetScenePlayer.gameObject;

            Debug.Log($"<color=green>[CharacterStudio]</color> <b>Successfully assigned '{selectedCharacter.characterName}' to Player!</b>\n" +
                      $"HP: {selectedCharacter.baseHealth} | Armor: {selectedCharacter.baseArmor} | Speed: {selectedCharacter.movementSpeed} | Skill: {selectedCharacter.activeSkill?.skillName} | Ult: {selectedCharacter.activeUltimate?.skillName}");
        }

        private void ExportCharacterAsPrefab()
        {
            if (selectedCharacter == null || selectedCharacter.modelPrefab == null)
            {
                EditorUtility.DisplayDialog("Model Missing", "Please assign a valid 3D Model / Prefab to this character before exporting.", "OK");
                return;
            }

            if (!Directory.Exists(PREFABS_EXPORT_FOLDER))
            {
                Directory.CreateDirectory(PREFABS_EXPORT_FOLDER);
                AssetDatabase.Refresh();
            }

            string safeName = selectedCharacter.characterName.Replace(" ", "_");
            string exportPath = $"{PREFABS_EXPORT_FOLDER}/{safeName}_Player.prefab";

            // Build temporary GameObject
            GameObject tempRoot = new GameObject(safeName + "_Player");
            tempRoot.tag = "Player";
            tempRoot.layer = LayerMask.NameToLayer("Player") != -1 ? LayerMask.NameToLayer("Player") : 6;

            // 1. Components
            var pc = tempRoot.AddComponent<PlayerController>();
            var aimCtrl = tempRoot.AddComponent<Animation.Tools.AimTargetController>();
            var col = tempRoot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.899f, 0);
            col.radius = 0.29f;
            col.height = 1.687f;
            col.isTrigger = true;

            var rb = tempRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            tempRoot.AddComponent<AudioSource>();

            // 2. Aim Target
            GameObject aimGo = new GameObject("Aim");
            aimGo.transform.SetParent(tempRoot.transform, false);
            aimGo.transform.localPosition = new Vector3(0, 1.618f, 1.144f);

            // 3. Model
            GameObject modelInst = Instantiate(selectedCharacter.modelPrefab, tempRoot.transform);
            modelInst.name = "Character_Model";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;

            Animator childAnim = modelInst.GetComponent<Animator>();
            if (childAnim != null) DestroyImmediate(childAnim);

            // 4. Weapon Socket
            Transform rHand = null;
            foreach (Transform t in modelInst.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "mixamorig:RightHand") { rHand = t; break; }
            }

            Transform socketTrans = null;
            if (rHand != null)
            {
                GameObject socketGo = new GameObject("Weapon_socket");
                socketGo.transform.SetParent(rHand, false);
                socketGo.transform.localPosition = Vector3.zero;
                socketGo.transform.localRotation = Quaternion.identity;
                socketTrans = socketGo.transform;
            }

            // 5. Animator on Root
            var rootAnim = tempRoot.AddComponent<Animator>();
            Avatar avatar = selectedCharacter.GetAvatar();
            if (avatar != null) rootAnim.avatar = avatar;

            var defaultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/3d/Characters/Rough/Animations/Controller/Man Controller.controller");
            rootAnim.runtimeAnimatorController = selectedCharacter.customAnimatorController != null ? selectedCharacter.customAnimatorController : defaultController;
            rootAnim.applyRootMotion = false;

            // 6. Bind PlayerController & AimTargetController
            pc.currentCharacterData = selectedCharacter;
            pc.weaponSocket = socketTrans;
            pc.aimTarget = aimGo.transform;
            pc.equippedSkill = selectedCharacter.activeSkill;
            pc.equippedUltimate = selectedCharacter.activeUltimate;

            aimCtrl.targetTransform = aimGo.transform;
            aimCtrl.rayOriginOverride = socketTrans;

            // Save Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempRoot, exportPath);
            DestroyImmediate(tempRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"<color=green>[CharacterStudio]</color> Exported character prefab to: {exportPath}");
            EditorUtility.DisplayDialog("Export Complete", $"Successfully exported standalone player prefab to:\n{exportPath}", "OK");
        }

        private void AutoFixModelImporterToHumanoid(GameObject model)
        {
            string path = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrEmpty(path)) return;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
                Debug.Log($"<color=green>[CharacterStudio]</color> Successfully configured '{model.name}' as Humanoid with auto-generated Avatar!");
            }
        }

        private void CreateNewCharacter()
        {
            if (!Directory.Exists(CHARACTERS_FOLDER))
            {
                Directory.CreateDirectory(CHARACTERS_FOLDER);
            }

            int index = characterList.Count + 1;
            string assetPath = $"{CHARACTERS_FOLDER}/New_Character_{index}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            var newChar = ScriptableObject.CreateInstance<CharacterData>();
            newChar.characterName = $"Character {index}";
            newChar.characterID = $"char_{index}";
            newChar.characterTitle = "Operative";
            newChar.baseHealth = 120f;
            newChar.baseArmor = 20f;
            newChar.movementSpeed = 10f;
            newChar.laneDistance = 2.0f;
            newChar.activeSkill = LoadSkillByName("RapidFire");
            newChar.activeUltimate = LoadSkillByName("AOE");

            AssetDatabase.CreateAsset(newChar, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshCharacterList();
            SelectCharacter(newChar);
            Debug.Log($"<color=green>[CharacterStudio]</color> Created new character profile at: {assetPath}");
        }

        private void CloneCharacter(CharacterData source)
        {
            if (source == null) return;

            string path = AssetDatabase.GetAssetPath(source);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path.Replace(".asset", "_Copy.asset"));

            var clone = Instantiate(source);
            clone.characterName = source.characterName + " (Copy)";
            clone.characterID = source.characterID + "_copy";

            AssetDatabase.CreateAsset(clone, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshCharacterList();
            SelectCharacter(clone);
            Debug.Log($"<color=green>[CharacterStudio]</color> Cloned '{source.characterName}' to '{clone.characterName}'.");
        }

        private void DeleteCharacter(CharacterData character)
        {
            if (character == null) return;
            string path = AssetDatabase.GetAssetPath(character);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshCharacterList();
        }

        private void CreateDefaultProfiles()
        {
            if (isInitializingDefaults) return;
            isInitializingDefaults = true;

            try
            {
                if (!Directory.Exists(CHARACTERS_FOLDER))
                {
                    Directory.CreateDirectory(CHARACTERS_FOLDER);
                }

                // 1. Ch15 Cyber Agent
                string ch15Path = $"{CHARACTERS_FOLDER}/Ch15_Agent.asset";
                if (!File.Exists(ch15Path))
                {
                    var ch15 = ScriptableObject.CreateInstance<CharacterData>();
                    ch15.characterID = "ch15_agent";
                    ch15.characterName = "Ch15 Cyber Agent";
                    ch15.characterTitle = "Vanguard Infiltrator";
                    ch15.description = "Agile cybernetic operative equipped with high-tech armor and rapid tactical firearms.";
                    ch15.baseHealth = 125f;
                    ch15.baseArmor = 30f;
                    ch15.movementSpeed = 11f;
                    ch15.laneDistance = 2.0f;
                    ch15.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/CH15/Ch15_nonPBR.fbx");
                    ch15.activeSkill = LoadSkillByName("RapidFire");
                    ch15.activeUltimate = LoadSkillByName("AOE");
                    AssetDatabase.CreateAsset(ch15, ch15Path);
                    if (!characterList.Contains(ch15)) characterList.Add(ch15);
                }
                else
                {
                    var ch15 = AssetDatabase.LoadAssetAtPath<CharacterData>(ch15Path);
                    if (ch15 != null && !characterList.Contains(ch15)) characterList.Add(ch15);
                }

                // 2. Mocap Veteran
                string mocapPath = $"{CHARACTERS_FOLDER}/Mocap_Runner.asset";
                if (!File.Exists(mocapPath))
                {
                    var mocap = ScriptableObject.CreateInstance<CharacterData>();
                    mocap.characterID = "mocap_runner";
                    mocap.characterName = "Mocap Veteran";
                    mocap.characterTitle = "Combat Specialist";
                    mocap.description = "Seasoned combat runner with balanced health, standard tactical gear, and versatile firearm handling.";
                    mocap.baseHealth = 100f;
                    mocap.baseArmor = 0f;
                    mocap.movementSpeed = 10f;
                    mocap.laneDistance = 2.0f;
                    mocap.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3d/Characters/Rough/Man_T.fbx");
                    mocap.activeSkill = LoadSkillByName("RapidFire");
                    mocap.activeUltimate = LoadSkillByName("AOE");
                    AssetDatabase.CreateAsset(mocap, mocapPath);
                    if (!characterList.Contains(mocap)) characterList.Add(mocap);
                }
                else
                {
                    var mocap = AssetDatabase.LoadAssetAtPath<CharacterData>(mocapPath);
                    if (mocap != null && !characterList.Contains(mocap)) characterList.Add(mocap);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=green>[CharacterStudio]</color> Initialized default character profiles (Ch15 Cyber Agent & Mocap Veteran).");
            }
            finally
            {
                isInitializingDefaults = false;
            }
        }

        private SkillData LoadSkillByName(string nameSearch)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameSearch} t:SkillData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<SkillData>(path);
            }
            return null;
        }

        #endregion
    }
}
