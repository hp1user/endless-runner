using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EndlessRunner.LevelGen;

namespace EndlessRunner.LevelGen.Editor
{
    public class ProceduralChunkEditorWindow : EditorWindow
    {
        private int currentTab = 0;
        private readonly string[] tabTitles = new string[] { "🛣️ Highway Chunk Studio", "🚧 Obstacle Manager & Pool" };

        // Tab 0: Chunk Studio
        private ProceduralEnvironmentProfile profile;
        private Vector2 scrollPos;
        private int bakeCount = 10;
        private string bakeFolder = "Assets/Prefabs/Chunks/Generated";
        private LevelThemeData targetThemeToAssign;
        private GameObject currentPreviewChunk;
        private SerializedObject serializedProfile;

        // Tab 1: Obstacle Manager
        private ObstacleManager sceneObstacleManager;
        private SerializedObject serializedObstacleManager;
        private Vector2 obstacleScrollPos;
        private GameObject dropPrefabToSetup;
        private float newObstacleDamage = 25f;

        [MenuItem("Tools/Endless Runner/Procedural Chunk Studio", false, 10)]
        [MenuItem("Window/Procedural Chunk Studio", false, 200)]
        public static void OpenWindow()
        {
            var win = GetWindow<ProceduralChunkEditorWindow>("Chunk & Obstacle Studio");
            win.minSize = new Vector2(520, 650);
            win.Show();
        }

        private void OnEnable()
        {
            // Auto-load profile if in project
            if (profile == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:ProceduralEnvironmentProfile");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    profile = AssetDatabase.LoadAssetAtPath<ProceduralEnvironmentProfile>(path);
                }
            }

            if (profile != null)
            {
                serializedProfile = new SerializedObject(profile);
            }

            FindSceneObstacleManager();
        }

        private void FindSceneObstacleManager()
        {
            sceneObstacleManager = Object.FindObjectOfType<ObstacleManager>();
            if (sceneObstacleManager != null)
            {
                serializedObstacleManager = new SerializedObject(sceneObstacleManager);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            DrawHeader();

            EditorGUILayout.Space(4);
            currentTab = GUILayout.Toolbar(currentTab, tabTitles, GUILayout.Height(30));

            EditorGUILayout.Space(6);

            switch (currentTab)
            {
                case 0:
                    DrawChunkStudioTab();
                    break;
                case 1:
                    DrawObstacleManagerTab();
                    break;
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🛣️ Procedural Studio & Obstacle Hub", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(75)))
            {
                FindSceneObstacleManager();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Label("Design procedural highway chunks, live preview layouts, and easily manage gameplay ObstacleManager pools.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        #region TAB 0: CHUNK STUDIO

        private void DrawChunkStudioTab()
        {
            DrawProfileSelector();

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Select or create a ProceduralEnvironmentProfile asset to begin designing chunks.", MessageType.Info);
                if (GUILayout.Button("✨ Create New Procedural Profile", GUILayout.Height(30)))
                {
                    CreateNewProfile();
                }
                return;
            }

            if (serializedProfile == null || serializedProfile.targetObject != profile)
            {
                serializedProfile = new SerializedObject(profile);
            }

            serializedProfile.Update();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(6);
            DrawQuickPopulateSection();

            EditorGUILayout.Space(8);
            DrawProfileSettings();

            EditorGUILayout.Space(12);
            DrawActionButtons();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();

            if (serializedProfile.hasModifiedProperties)
            {
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
            }
        }

        private void DrawProfileSelector()
        {
            EditorGUILayout.BeginHorizontal();
            var newProfile = (ProceduralEnvironmentProfile)EditorGUILayout.ObjectField("Active Profile", profile, typeof(ProceduralEnvironmentProfile), false);
            if (newProfile != profile)
            {
                profile = newProfile;
                if (profile != null) serializedProfile = new SerializedObject(profile);
            }

            if (GUILayout.Button("New", GUILayout.Width(50)))
            {
                CreateNewProfile();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuickPopulateSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡ Auto-Detect & Populate Highway Assets", EditorStyles.boldLabel);
            if (GUILayout.Button("Auto-Fill Highway Pack", GUILayout.Width(170)))
            {
                AutoPopulateHighwayAssets();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Automatically maps Side Railings, Dividers 1-4, Street Lights, Signs, Debris, and Road meshes from Assets/3d/Environment/Highway into categorized slots.", MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawProfileSettings()
        {
            if (serializedProfile == null) return;

            // Base dimensions
            EditorGUILayout.LabelField("1. Base & Road Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(serializedProfile.FindProperty("profileName"));
            EditorGUILayout.PropertyField(serializedProfile.FindProperty("roadBasePrefab"));
            EditorGUILayout.PropertyField(serializedProfile.FindProperty("roadBaseOffset"));
            EditorGUILayout.PropertyField(serializedProfile.FindProperty("chunkLength"));
            EditorGUILayout.PropertyField(serializedProfile.FindProperty("roadWidth"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Dual Highway (Twin Roadways)
            DrawCategorySection("🛣️ Dual Highway (Parallel Visual Road)", "dualHighway");

            EditorGUILayout.Space(6);

            // Side Railings
            DrawCategorySection("2. Side Railings & Barriers", "sideRailings");

            EditorGUILayout.Space(6);

            // Center Dividers & Lights
            DrawCategorySection("3. Center Dividers & Center Lights", "centerDividers");

            EditorGUILayout.Space(6);

            // Street Lights & Small Poles
            DrawCategorySection("4. Street Lights & Poles (Side Shoulders)", "streetLights");

            EditorGUILayout.Space(6);

            // Overhead Signs
            DrawCategorySection("5. Overhead / Big Sign Boards", "overheadSigns");

            EditorGUILayout.Space(6);

            // Debris & Clutter
            DrawCategorySection("6. Debris, Clutter & Road Props", "debrisAndClutter");

            EditorGUILayout.Space(6);

            // Vehicles & Obstacles
            DrawCategorySection("7. Vehicles & Heavy Obstacles", "vehicles");
        }

        private void DrawCategorySection(string title, string propertyName)
        {
            SerializedProperty prop = serializedProfile.FindProperty(propertyName);
            if (prop == null) return;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(prop, new GUIContent(title), true);
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("🛠️ Generation & Baking Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // Live Scene Preview
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("🎲 Preview Random Chunk in Scene", GUILayout.Height(32)))
            {
                GenerateScenePreview();
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Clear Preview", GUILayout.Height(32), GUILayout.Width(110)))
            {
                ClearScenePreview();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Batch Baking
            EditorGUILayout.LabelField("📦 1-Click Batch Prefab Baker", EditorStyles.boldLabel);
            bakeCount = EditorGUILayout.IntSlider("Number of Prefabs", bakeCount, 1, 30);
            bakeFolder = EditorGUILayout.TextField("Save Folder", bakeFolder);
            targetThemeToAssign = (LevelThemeData)EditorGUILayout.ObjectField("Assign to Theme (Optional)", targetThemeToAssign, typeof(LevelThemeData), false);

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button($"🚀 Bake {bakeCount} Unique Chunk Prefabs", GUILayout.Height(36)))
            {
                BakeChunkPrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);

            // Create Master Dynamic Prefab
            if (GUILayout.Button("⚡ Create Runtime Dynamic Chunk Prefab", GUILayout.Height(28)))
            {
                CreateRuntimeDynamicPrefab();
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateScenePreview()
        {
            if (profile == null) return;

            ClearScenePreview();

            currentPreviewChunk = new GameObject("[PROD_CHUNK_PREVIEW]");
            currentPreviewChunk.transform.position = Vector3.zero;

            ProceduralChunk proc = currentPreviewChunk.AddComponent<ProceduralChunk>();
            proc.profile = profile;
            proc.GenerateRandomLayout();

            Selection.activeGameObject = currentPreviewChunk;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("<color=cyan>[ChunkStudio]</color> Generated live preview chunk in scene view.");
        }

        private void ClearScenePreview()
        {
            GameObject existing = GameObject.Find("[PROD_CHUNK_PREVIEW]");
            if (existing != null)
            {
                DestroyImmediate(existing);
            }
            currentPreviewChunk = null;
        }

        private void BakeChunkPrefabs()
        {
            if (profile == null) return;

            if (!Directory.Exists(bakeFolder))
            {
                Directory.CreateDirectory(bakeFolder);
                AssetDatabase.Refresh();
            }

            List<GameObject> bakedPrefabs = new List<GameObject>();

            for (int i = 1; i <= bakeCount; i++)
            {
                string chunkName = $"{profile.profileName.Replace(" ", "")}_Chunk_{i}";
                GameObject tempGO = new GameObject(chunkName);
                tempGO.transform.position = Vector3.zero;

                ProceduralChunk proc = tempGO.AddComponent<ProceduralChunk>();
                proc.profile = profile;
                proc.GenerateRandomLayout(UnityEngine.Random.Range(1000, 999999));

                DestroyImmediate(proc);

                string prefabPath = $"{bakeFolder}/{chunkName}.prefab";
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
                bakedPrefabs.Add(savedPrefab);

                DestroyImmediate(tempGO);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (targetThemeToAssign != null)
            {
                Undo.RecordObject(targetThemeToAssign, "Assign Baked Chunks to Theme");
                targetThemeToAssign.chunkVariants = bakedPrefabs.ToArray();
                EditorUtility.SetDirty(targetThemeToAssign);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>[ChunkStudio]</color> Successfully baked {bakeCount} prefabs and assigned them to {targetThemeToAssign.themeName}!");
            }
            else
            {
                Debug.Log($"<color=green>[ChunkStudio]</color> Successfully baked {bakeCount} prefabs to {bakeFolder}!");
            }

            EditorUtility.DisplayDialog("Bake Complete", $"Successfully generated and saved {bakeCount} chunk prefabs to:\n{bakeFolder}", "OK");
        }

        private void CreateRuntimeDynamicPrefab()
        {
            if (profile == null) return;

            if (!Directory.Exists(bakeFolder))
            {
                Directory.CreateDirectory(bakeFolder);
                AssetDatabase.Refresh();
            }

            string name = $"{profile.profileName.Replace(" ", "")}_DynamicChunk";
            GameObject tempGO = new GameObject(name);
            ProceduralChunk proc = tempGO.AddComponent<ProceduralChunk>();
            proc.profile = profile;
            proc.generateOnStart = true;

            string prefabPath = $"{bakeFolder}/{name}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGO, prefabPath);
            DestroyImmediate(tempGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(savedPrefab);
            Debug.Log($"<color=cyan>[ChunkStudio]</color> Created dynamic runtime chunk prefab at: {prefabPath}");
        }

        private void CreateNewProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Procedural Theme Profile", "New Highway Profile", "asset", "Save Procedural Profile");
            if (!string.IsNullOrEmpty(path))
            {
                ProceduralEnvironmentProfile newProf = ScriptableObject.CreateInstance<ProceduralEnvironmentProfile>();
                AssetDatabase.CreateAsset(newProf, path);
                AssetDatabase.SaveAssets();
                profile = newProf;
                serializedProfile = new SerializedObject(profile);
                AutoPopulateHighwayAssets();
            }
        }

        private void AutoPopulateHighwayAssets()
        {
            if (profile == null) return;

            Undo.RecordObject(profile, "Auto Populate Highway Assets");

            // 1. Road Base
            string[] roadGuids = AssetDatabase.FindAssets("Path_1 t:Model");
            if (roadGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(roadGuids[0]);
                profile.roadBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                profile.roadBaseOffset = new Vector3(0f, 0f, 5f);
                profile.chunkLength = 40f;
                profile.roadWidth = 10f;
            }

            // 2. Side Railings
            profile.sideRailings.leftX = -4.95f;
            profile.sideRailings.rightX = 4.95f;
            profile.sideRailings.segmentLength = 2.58f;
            profile.sideRailings.spawnChance = 1.0f;
            profile.sideRailings.railingPrefabs.Clear();
            AddPrefabIfFound("Side Railing", profile.sideRailings.railingPrefabs, 1f, new Vector3(0, 0, 0), new Vector3(0, 0, 0));

            // 3. Dividers
            profile.centerDividers.centerX = 0f;
            profile.centerDividers.segmentLength = 5.5f;
            profile.centerDividers.minIntactBetweenBroken = 2;
            profile.centerDividers.dividerPrefabs.Clear();
            AddPrefabIfFound("Divider 1", profile.centerDividers.dividerPrefabs, 4f, null, null, false);
            AddPrefabIfFound("Divider 2", profile.centerDividers.dividerPrefabs, 1f, null, null, true);
            AddPrefabIfFound("Divider 3", profile.centerDividers.dividerPrefabs, 1f, null, null, true);
            AddPrefabIfFound("Divider 4", profile.centerDividers.dividerPrefabs, 1f, null, null, true);

            // Center Lights
            profile.centerDividers.centerLightPrefabs.Clear();
            profile.centerDividers.centerLightInterval = 20f;
            AddPrefabIfFound("Stree Light", profile.centerDividers.centerLightPrefabs, 1f, new Vector3(0, 0, 0), new Vector3(0, 90, 0));

            // 4. Street Lights & Small Poles
            profile.streetLights.leftX = -5.2f;
            profile.streetLights.rightX = 5.2f;
            profile.streetLights.intervalZ = 18f;
            profile.streetLights.lightPrefabs.Clear();
            AddPrefabIfFound("Stree Light", profile.streetLights.lightPrefabs, 1.5f);
            AddPrefabIfFound("Small Pole 1", profile.streetLights.lightPrefabs, 1f);
            AddPrefabIfFound("Small Pole 2", profile.streetLights.lightPrefabs, 1f);
            AddPrefabIfFound("Small Pole 3", profile.streetLights.lightPrefabs, 1f);
            AddPrefabIfFound("Small Pole 4", profile.streetLights.lightPrefabs, 1f);

            // 5. Overhead Signs
            profile.overheadSigns.signPrefabs.Clear();
            AddPrefabIfFound("Big Sign Board", profile.overheadSigns.signPrefabs, 1f);

            // 6. Debris & Clutter
            profile.debrisAndClutter.debrisPrefabs.Clear();
            AddPrefabIfFound("Debris", profile.debrisAndClutter.debrisPrefabs, 1.5f);
            AddPrefabIfFound("Cone", profile.debrisAndClutter.debrisPrefabs, 1f);
            AddPrefabIfFound("block 1", profile.debrisAndClutter.debrisPrefabs, 1f);
            AddPrefabIfFound("block 2", profile.debrisAndClutter.debrisPrefabs, 1f);

            // 7. Vehicles
            profile.vehicles.vehiclePrefabs.Clear();
            AddPrefabIfFound("Bus", profile.vehicles.vehiclePrefabs, 1f);
            AddPrefabIfFound("Car", profile.vehicles.vehiclePrefabs, 1f);
            AddPrefabIfFound("SUV", profile.vehicles.vehiclePrefabs, 1f);
            AddPrefabIfFound("Van", profile.vehicles.vehiclePrefabs, 1f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[ChunkStudio]</color> Auto-populated profile with Highway environment assets!");
        }

        private void AddPrefabIfFound(string nameSearch, List<CategorizedAssetItem> targetList, float weight = 1f, Vector3? offset = null, Vector3? rot = null, bool isBroken = false)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameSearch} t:Prefab");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets($"{nameSearch} t:Model");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    targetList.Add(new CategorizedAssetItem(go, weight, offset, rot, isBroken));
                }
            }
        }

        #endregion

        #region TAB 1: OBSTACLE MANAGER

        private void DrawObstacleManagerTab()
        {
            if (sceneObstacleManager == null)
            {
                FindSceneObstacleManager();
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎯 Active Scene ObstacleManager Target", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            var newManager = (ObstacleManager)EditorGUILayout.ObjectField("Target Manager", sceneObstacleManager, typeof(ObstacleManager), true);
            if (newManager != sceneObstacleManager)
            {
                sceneObstacleManager = newManager;
                if (sceneObstacleManager != null) serializedObstacleManager = new SerializedObject(sceneObstacleManager);
            }

            if (GUILayout.Button("🔍 Find in Scene", GUILayout.Width(110)))
            {
                FindSceneObstacleManager();
            }
            EditorGUILayout.EndHorizontal();

            if (sceneObstacleManager == null)
            {
                EditorGUILayout.HelpBox("No ObstacleManager found in the current active scene.", MessageType.Warning);
                if (GUILayout.Button("➕ Create New ObstacleManager in Scene", GUILayout.Height(30)))
                {
                    CreateSceneObstacleManager();
                }
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎯 Ping in Hierarchy", GUILayout.Height(22)))
            {
                EditorGUIUtility.PingObject(sceneObstacleManager.gameObject);
                Selection.activeGameObject = sceneObstacleManager.gameObject;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (serializedObstacleManager == null || serializedObstacleManager.targetObject != sceneObstacleManager)
            {
                serializedObstacleManager = new SerializedObject(sceneObstacleManager);
            }

            serializedObstacleManager.Update();

            obstacleScrollPos = EditorGUILayout.BeginScrollView(obstacleScrollPos);

            EditorGUILayout.Space(8);

            // 1. Quick Add Obstacles from Project
            DrawQuickAddProjectObstaclesSection();

            EditorGUILayout.Space(8);

            // 2. Active Obstacle Pool List
            DrawObstaclePoolSection();

            EditorGUILayout.Space(8);

            // 3. Spawning & Movement Settings
            DrawObstacleSpawnerSettingsSection();

            EditorGUILayout.Space(8);

            // 4. Quick Convert / Setup Obstacle Prefab
            DrawConvertObstaclePrefabSection();

            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();

            if (serializedObstacleManager.hasModifiedProperties)
            {
                serializedObstacleManager.ApplyModifiedProperties();
                EditorUtility.SetDirty(sceneObstacleManager);
            }
        }

        private HashSet<string> dismissedDetectedPaths = new HashSet<string>();
        private bool showDismissedDetected = false;

        private void DrawQuickAddProjectObstaclesSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("⚡ Quick-Add Detected Obstacles & Vehicles", EditorStyles.boldLabel);

            if (dismissedDetectedPaths.Count > 0)
            {
                showDismissedDetected = GUILayout.Toggle(showDismissedDetected, $"Show Dismissed ({dismissedDetectedPaths.Count})", "Button", GUILayout.Width(140));
            }

            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            if (GUILayout.Button("➕ Add All Detected", GUILayout.Width(130)))
            {
                AddAllDetectedObstaclesToPool();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Click '+ Add' to assign into active pool, or '⊘ Dismiss' to remove unwanted candidates from this detected list.", MessageType.None);

            string[] detectedPaths = FindCandidateObstaclePrefabs();
            if (detectedPaths.Length == 0)
            {
                EditorGUILayout.LabelField("No candidate obstacle prefabs found in project.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (string path in detectedPaths)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    bool alreadyInPool = sceneObstacleManager.obstaclePool != null && sceneObstacleManager.obstaclePool.Contains(prefab);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false, GUILayout.Width(180));

                    Obstacle obs = prefab.GetComponent<Obstacle>();
                    Collider col = prefab.GetComponent<Collider>();

                    if (obs != null && col != null)
                    {
                        string restLabel = obs.laneRestriction == Obstacle.ObstacleLaneRestriction.SidesOnly ? "Sides Only (L/R)" : obs.laneRestriction.ToString();
                        Color badgeCol = obs.laneRestriction == Obstacle.ObstacleLaneRestriction.SidesOnly ? new Color(0.3f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);
                        GUI.color = badgeCol;
                        GUILayout.Label($"[{restLabel}]", EditorStyles.miniBoldLabel, GUILayout.Width(100));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUILayout.Label("⚠️ Needs Setup", EditorStyles.miniLabel, GUILayout.Width(100));
                    }

                    if (alreadyInPool)
                    {
                        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
                        if (GUILayout.Button("✕ Remove", GUILayout.Width(75)))
                        {
                            RemovePrefabFromObstaclePool(prefab);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
                        if (GUILayout.Button("+ Add", GUILayout.Width(75)))
                        {
                            if (obs == null || col == null)
                            {
                                SetupPrefabAsObstacle(prefab, 25f);
                            }
                            AddPrefabToObstaclePool(prefab);
                        }
                        GUI.backgroundColor = Color.white;
                    }

                    if (dismissedDetectedPaths.Contains(path))
                    {
                        if (GUILayout.Button("↩ Restore", GUILayout.Width(65)))
                        {
                            dismissedDetectedPaths.Remove(path);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("⊘ Dismiss", GUILayout.Width(65)))
                        {
                            dismissedDetectedPaths.Add(path);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawObstaclePoolSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"📦 Active Obstacle Pool ({sceneObstacleManager.obstaclePool?.Count ?? 0} Prefabs)", EditorStyles.boldLabel);

            if (GUILayout.Button("➕ Add Slot", GUILayout.Width(85)))
            {
                if (sceneObstacleManager.obstaclePool == null) sceneObstacleManager.obstaclePool = new List<GameObject>();
                Undo.RecordObject(sceneObstacleManager, "Add Obstacle Pool Slot");
                sceneObstacleManager.obstaclePool.Add(null);
                EditorUtility.SetDirty(sceneObstacleManager);
            }

            GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
            if (GUILayout.Button("✨ Deduplicate", GUILayout.Width(95)))
            {
                RemoveDuplicatesFromObstaclePool();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🧹 Clear", GUILayout.Width(65)))
            {
                if (EditorUtility.DisplayDialog("Clear Obstacle Pool?", "Are you sure you want to remove all prefabs from the ObstacleManager pool?", "Yes", "No"))
                {
                    Undo.RecordObject(sceneObstacleManager, "Clear Obstacle Pool");
                    sceneObstacleManager.obstaclePool?.Clear();
                    EditorUtility.SetDirty(sceneObstacleManager);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (sceneObstacleManager.obstaclePool == null || sceneObstacleManager.obstaclePool.Count == 0)
            {
                EditorGUILayout.HelpBox("Obstacle pool is empty! Add prefabs using the Quick-Add list above or drag & drop below.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < sceneObstacleManager.obstaclePool.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label($"[{i + 1}]", GUILayout.Width(26));

                    GameObject current = sceneObstacleManager.obstaclePool[i];
                    GameObject updated = (GameObject)EditorGUILayout.ObjectField(current, typeof(GameObject), false);
                    if (updated != current)
                    {
                        Undo.RecordObject(sceneObstacleManager, "Change Obstacle Prefab");
                        sceneObstacleManager.obstaclePool[i] = updated;
                        EditorUtility.SetDirty(sceneObstacleManager);
                    }

                    if (updated != null)
                    {
                        Obstacle obs = updated.GetComponent<Obstacle>();
                        Collider col = updated.GetComponent<Collider>();

                        if (obs != null && col != null)
                        {
                            // Lane Restriction Badge / Quick Selector
                            Obstacle.ObstacleLaneRestriction newRest = (Obstacle.ObstacleLaneRestriction)EditorGUILayout.EnumPopup(obs.laneRestriction, GUILayout.Width(95));
                            if (newRest != obs.laneRestriction)
                            {
                                string path = AssetDatabase.GetAssetPath(updated);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                                    {
                                        Obstacle rootObs = scope.prefabContentsRoot.GetComponent<Obstacle>();
                                        if (rootObs != null) rootObs.laneRestriction = newRest;
                                    }
                                    AssetDatabase.SaveAssets();
                                }
                            }

                            GUI.color = Color.green;
                            GUILayout.Label("✓", EditorStyles.boldLabel, GUILayout.Width(16));
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUI.color = new Color(1f, 0.7f, 0.2f);
                            GUILayout.Label("⚠️ Setup", EditorStyles.miniLabel, GUILayout.Width(50));
                            GUI.color = Color.white;

                            if (GUILayout.Button("Fix", GUILayout.Width(40)))
                            {
                                SetupPrefabAsObstacle(updated, 25f);
                            }
                        }
                    }

                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("✕", GUILayout.Width(26)))
                    {
                        Undo.RecordObject(sceneObstacleManager, "Remove Obstacle Prefab");
                        sceneObstacleManager.obstaclePool.RemoveAt(i);
                        EditorUtility.SetDirty(sceneObstacleManager);
                        GUIUtility.ExitGUI();
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawObstacleSpawnerSettingsSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("⚙️ Obstacle Spawner & Movement Settings", EditorStyles.boldLabel);

            SerializedProperty spawnIntervalProp = serializedObstacleManager.FindProperty("spawnIntervalRange");
            SerializedProperty lanePositionsProp = serializedObstacleManager.FindProperty("lanePositions");
            SerializedProperty spawnYProp = serializedObstacleManager.FindProperty("spawnYPosition");
            SerializedProperty obstacleDirProp = serializedObstacleManager.FindProperty("obstacleDirection");
            SerializedProperty despawnThresholdProp = serializedObstacleManager.FindProperty("obstacleDespawnThreshold");

            if (spawnIntervalProp != null) EditorGUILayout.PropertyField(spawnIntervalProp, new GUIContent("Spawn Interval (Min/Max s)"));
            if (lanePositionsProp != null) EditorGUILayout.PropertyField(lanePositionsProp, new GUIContent("Playable Lanes X"), true);
            if (spawnYProp != null) EditorGUILayout.PropertyField(spawnYProp, new GUIContent("Spawn Height Y"));
            if (obstacleDirProp != null) EditorGUILayout.PropertyField(obstacleDirProp, new GUIContent("Obstacle Move Direction"));
            if (despawnThresholdProp != null) EditorGUILayout.PropertyField(despawnThresholdProp, new GUIContent("Despawn Distance Threshold"));

            EditorGUILayout.EndVertical();
        }

        private bool setupRandomizeYaw = true;
        private bool setupAllowFlip180 = true;
        private bool setupFullRandom360 = false;
        private float setupMaxYawVariation = 15f;
        private Obstacle.ObstacleCategory setupCategory = Obstacle.ObstacleCategory.Standard;
        private Obstacle.ObstacleLaneRestriction setupLaneRestriction = Obstacle.ObstacleLaneRestriction.AnyLane;

        private void DrawConvertObstaclePrefabSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🛠️ 1-Click Obstacle Prefab Setup Helper", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Drop any 3D model or prefab here to automatically configure a BoxCollider trigger, classification, and lane restriction rules.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            dropPrefabToSetup = (GameObject)EditorGUILayout.ObjectField("Source Prefab / Model", dropPrefabToSetup, typeof(GameObject), false);
            newObstacleDamage = EditorGUILayout.FloatField("Damage", newObstacleDamage, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            setupCategory = (Obstacle.ObstacleCategory)EditorGUILayout.EnumPopup("Category", setupCategory);
            setupLaneRestriction = (Obstacle.ObstacleLaneRestriction)EditorGUILayout.EnumPopup("Lane Rule", setupLaneRestriction);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            setupRandomizeYaw = EditorGUILayout.ToggleLeft("Randomize Yaw", setupRandomizeYaw, GUILayout.Width(110));
            setupAllowFlip180 = EditorGUILayout.ToggleLeft("Flip 0/180°", setupAllowFlip180, GUILayout.Width(90));
            setupFullRandom360 = EditorGUILayout.ToggleLeft("Full 360°", setupFullRandom360, GUILayout.Width(80));
            setupMaxYawVariation = EditorGUILayout.FloatField("± Angle", setupMaxYawVariation, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (dropPrefabToSetup != null)
            {
                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                if (GUILayout.Button($"✨ Configure '{dropPrefabToSetup.name}' as Obstacle & Add to Pool", GUILayout.Height(30)))
                {
                    SetupPrefabAsObstacle(dropPrefabToSetup, newObstacleDamage, setupRandomizeYaw, setupAllowFlip180, setupFullRandom360, setupMaxYawVariation, setupCategory, setupLaneRestriction);
                    AddPrefabToObstaclePool(dropPrefabToSetup);
                    dropPrefabToSetup = null;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateSceneObstacleManager()
        {
            GameObject go = new GameObject("ObstacleManager");
            sceneObstacleManager = go.AddComponent<ObstacleManager>();
            sceneObstacleManager.spawnIntervalRange = new Vector2(4f, 10f);
            sceneObstacleManager.lanePositions = new float[] { -2f, 0f, 2f };
            sceneObstacleManager.obstacleDirection = Obstacle.MoveDirection.Forward;
            sceneObstacleManager.obstacleDespawnThreshold = 20f;
            sceneObstacleManager.obstaclePool = new List<GameObject>();

            AddAllDetectedObstaclesToPool();

            Undo.RegisterCreatedObjectUndo(go, "Create ObstacleManager");
            serializedObstacleManager = new SerializedObject(sceneObstacleManager);
            Selection.activeGameObject = go;
            Debug.Log("<color=green>[ObstacleStudio]</color> Created new ObstacleManager in scene!");
        }

        private string[] FindCandidateObstaclePrefabs()
        {
            Dictionary<string, string> uniqueCandidates = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLower();

                // Check for obstacles, vehicles, debris
                if (lower.Contains("obstruc") || lower.Contains("obstacle") || lower.Contains("bus") || lower.Contains("car") || lower.Contains("suv") || lower.Contains("van") || lower.Contains("block") || lower.Contains("cone") || lower.Contains("debris"))
                {
                    // Exclude UI cards and procedural chunk root variants
                    if (!lower.Contains("card") && !lower.Contains("ui") && !lower.Contains("generated") && !lower.Contains("dynamicchunk"))
                    {
                        string assetName = Path.GetFileNameWithoutExtension(path);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;

                        bool isConfiguredObstacle = prefab.GetComponent<Obstacle>() != null && prefab.GetComponent<Collider>() != null;

                        if (uniqueCandidates.TryGetValue(assetName, out string existingPath))
                        {
                            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(existingPath);
                            bool existingConfigured = existing != null && existing.GetComponent<Obstacle>() != null && existing.GetComponent<Collider>() != null;

                            // Prefer configured obstacle over unconfigured model/prefab
                            if (isConfiguredObstacle && !existingConfigured)
                            {
                                uniqueCandidates[assetName] = path;
                            }
                            else if (lower.Contains("obstrucle") && !existingPath.ToLower().Contains("obstrucle"))
                            {
                                uniqueCandidates[assetName] = path;
                            }
                        }
                        else
                        {
                            uniqueCandidates[assetName] = path;
                        }
                    }
                }
            }

            List<string> filtered = new List<string>();
            foreach (var kvp in uniqueCandidates)
            {
                if (showDismissedDetected || !dismissedDetectedPaths.Contains(kvp.Value))
                {
                    filtered.Add(kvp.Value);
                }
            }

            return filtered.ToArray();
        }

        private void RemovePrefabFromObstaclePool(GameObject prefab)
        {
            if (sceneObstacleManager == null || prefab == null || sceneObstacleManager.obstaclePool == null) return;

            Undo.RecordObject(sceneObstacleManager, "Remove Obstacle from Pool");
            sceneObstacleManager.obstaclePool.RemoveAll(p => p == prefab);
            EditorUtility.SetDirty(sceneObstacleManager);
            Debug.Log($"<color=orange>[ObstacleStudio]</color> Removed {prefab.name} from ObstacleManager pool.");
        }

        private void RemoveDuplicatesFromObstaclePool()
        {
            if (sceneObstacleManager == null || sceneObstacleManager.obstaclePool == null) return;

            Undo.RecordObject(sceneObstacleManager, "Remove Obstacle Pool Duplicates");
            HashSet<GameObject> seen = new HashSet<GameObject>();
            List<GameObject> unique = new List<GameObject>();

            foreach (var item in sceneObstacleManager.obstaclePool)
            {
                if (item != null)
                {
                    if (seen.Add(item))
                    {
                        unique.Add(item);
                    }
                }
            }

            int removed = sceneObstacleManager.obstaclePool.Count - unique.Count;
            sceneObstacleManager.obstaclePool = unique;
            EditorUtility.SetDirty(sceneObstacleManager);
            Debug.Log($"<color=green>[ObstacleStudio]</color> Cleaned ObstacleManager pool (removed {removed} duplicate/empty slots)!");
        }

        private void AddPrefabToObstaclePool(GameObject prefab)
        {
            if (sceneObstacleManager == null || prefab == null) return;
            if (sceneObstacleManager.obstaclePool == null) sceneObstacleManager.obstaclePool = new List<GameObject>();

            if (!sceneObstacleManager.obstaclePool.Contains(prefab))
            {
                Undo.RecordObject(sceneObstacleManager, "Add Obstacle to Pool");
                sceneObstacleManager.obstaclePool.Add(prefab);
                EditorUtility.SetDirty(sceneObstacleManager);
                Debug.Log($"<color=green>[ObstacleStudio]</color> Added {prefab.name} to ObstacleManager pool.");
            }
        }

        private void AddAllDetectedObstaclesToPool()
        {
            if (sceneObstacleManager == null) return;
            string[] paths = FindCandidateObstaclePrefabs();
            int count = 0;

            foreach (string path in paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    if (sceneObstacleManager.obstaclePool == null) sceneObstacleManager.obstaclePool = new List<GameObject>();
                    if (!sceneObstacleManager.obstaclePool.Contains(prefab))
                    {
                        // Ensure it has Obstacle and Collider
                        if (prefab.GetComponent<Obstacle>() == null || prefab.GetComponent<Collider>() == null)
                        {
                            SetupPrefabAsObstacle(prefab, 25f);
                        }

                        Undo.RecordObject(sceneObstacleManager, "Add Obstacle to Pool");
                        sceneObstacleManager.obstaclePool.Add(prefab);
                        count++;
                    }
                }
            }

            EditorUtility.SetDirty(sceneObstacleManager);
            Debug.Log($"<color=green>[ObstacleStudio]</color> Added {count} obstacle prefabs into ObstacleManager pool!");
        }

        private void SetupPrefabAsObstacle(GameObject prefab, float damage, bool randomizeYaw = true, bool allowFlip180 = true, bool full360 = false, float maxYawVariation = 15f, Obstacle.ObstacleCategory? categoryOverride = null, Obstacle.ObstacleLaneRestriction? restrictionOverride = null)
        {
            if (prefab == null) return;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) return;

            bool isHeavy = prefab.name.ToLower().Contains("bus") || prefab.name.ToLower().Contains("van");
            Obstacle.ObstacleCategory cat = categoryOverride ?? (isHeavy ? Obstacle.ObstacleCategory.HeavyWide : Obstacle.ObstacleCategory.Standard);
            Obstacle.ObstacleLaneRestriction restriction = restrictionOverride ?? (isHeavy ? Obstacle.ObstacleLaneRestriction.SidesOnly : Obstacle.ObstacleLaneRestriction.AnyLane);

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;

                // 1. BoxCollider
                BoxCollider col = root.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = root.AddComponent<BoxCollider>();
                    Renderer[] rends = root.GetComponentsInChildren<Renderer>();
                    if (rends.Length > 0)
                    {
                        Bounds b = rends[0].bounds;
                        foreach (var r in rends) b.Encapsulate(r.bounds);
                        col.center = root.transform.InverseTransformPoint(b.center);
                        col.size = b.size;
                    }
                }
                col.isTrigger = true;

                // 2. Obstacle component
                Obstacle obs = root.GetComponent<Obstacle>();
                if (obs == null)
                {
                    obs = root.AddComponent<Obstacle>();
                }
                obs.category = cat;
                obs.laneRestriction = restriction;
                obs.damageAmount = damage;
                obs.worldMoveSpeed = 15f;
                obs.randomizeYaw = randomizeYaw;
                obs.allowFlip180 = allowFlip180;
                obs.fullRandom360 = full360;
                obs.maxRandomYawVariation = maxYawVariation;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[ObstacleStudio]</color> Configured {prefab.name} as {cat} [{restriction}] (Damage: {damage}).");
        }

        #endregion
    }
}
