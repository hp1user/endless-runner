using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EndlessRunner.LevelGen;

namespace EndlessRunner.LevelGen.Editor
{
    public class ProceduralChunkEditorWindow : EditorWindow
    {
        private ProceduralEnvironmentProfile profile;
        private Vector2 scrollPos;
        private int bakeCount = 10;
        private string bakeFolder = "Assets/Prefabs/Chunks/Generated";
        private LevelThemeData targetThemeToAssign;

        private GameObject currentPreviewChunk;
        private SerializedObject serializedProfile;

        [MenuItem("Tools/Endless Runner/Procedural Chunk Studio", false, 10)]
        [MenuItem("Window/Procedural Chunk Studio", false, 200)]
        public static void OpenWindow()
        {
            var win = GetWindow<ProceduralChunkEditorWindow>("Chunk Studio");
            win.minSize = new Vector2(480, 600);
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
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            DrawHeader();

            EditorGUILayout.Space(4);
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

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🛣️ Procedural Chunk Studio", EditorStyles.boldLabel);
            GUILayout.Label("Categorize highway assets, preview layouts in scene, and batch-bake randomized chunk prefabs.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
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

                // Clean up ProceduralChunk script from baked static prefab so it's clean and lightweight
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
                profile.roadBaseOffset = new Vector3(0f, 0f, 5f); // Centers Path_1 exactly from -20m to +20m
                profile.chunkLength = 40f;
                profile.roadWidth = 10f;
            }

            // 2. Side Railings
            profile.sideRailings.leftX = -4.95f;
            profile.sideRailings.rightX = 4.95f;
            profile.sideRailings.segmentLength = 2.58f; // Exact length of Side Railing.prefab mesh
            profile.sideRailings.spawnChance = 1.0f;
            profile.sideRailings.railingPrefabs.Clear();
            AddPrefabIfFound("Side Railing", profile.sideRailings.railingPrefabs, 1f, new Vector3(0, 0, 0), new Vector3(0, 0, 0));

            // 3. Dividers
            profile.centerDividers.centerX = 0f;
            profile.centerDividers.segmentLength = 5.5f;
            profile.centerDividers.dividerPrefabs.Clear();
            AddPrefabIfFound("Divider 1", profile.centerDividers.dividerPrefabs, 1f);
            AddPrefabIfFound("Divider 2", profile.centerDividers.dividerPrefabs, 1f);
            AddPrefabIfFound("Divider 3", profile.centerDividers.dividerPrefabs, 1f);
            AddPrefabIfFound("Divider 4", profile.centerDividers.dividerPrefabs, 1f);

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
            AddPrefabIfFound("Obstrucle1", profile.vehicles.vehiclePrefabs, 1f);
            AddPrefabIfFound("Obstrucle2", profile.vehicles.vehiclePrefabs, 1f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[ChunkStudio]</color> Auto-populated profile with Highway environment assets!");
        }

        private void AddPrefabIfFound(string nameSearch, List<CategorizedAssetItem> targetList, float weight = 1f, Vector3? offset = null, Vector3? rot = null)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameSearch} t:Prefab");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets($"{nameSearch} t:Model");

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    targetList.Add(new CategorizedAssetItem(go, weight, offset, rot));
                }
            }
        }
    }
}
