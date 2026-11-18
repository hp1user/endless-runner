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

        // Foliage Material Swapper
        private Material foliageMaterial;
        private Material roadFoliageMaterial;
        private bool autoApplyFoliageToPreview = false;
        private bool isFoliageAppliedToPreview = false;
        private int previewChunkIndex = 0;
        private readonly Dictionary<Renderer, Material[]> originalMaterialsCache = new Dictionary<Renderer, Material[]>();

        // Decal Randomizer Settings
        private int minDecalsPerChunk = 3;
        private int maxDecalsPerChunk = 6;
        private bool includeDecalCracks = true;
        private bool includeDecalMud = true;
        private bool includeDecalMoss = true;

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

            if (foliageMaterial == null)
            {
                foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Foliage_Sample.mat");
            }
            if (roadFoliageMaterial == null)
            {
                roadFoliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Road_Foliage.mat");
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

            // Foliage / Post-Apocalyptic Material Quick Swapper
            EditorGUILayout.LabelField("🌿 Foliage / Overgrown Material (1-Click Preview)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            foliageMaterial = (Material)EditorGUILayout.ObjectField("Props Foliage Mat", foliageMaterial, typeof(Material), false);
            roadFoliageMaterial = (Material)EditorGUILayout.ObjectField("Road Foliage Mat", roadFoliageMaterial, typeof(Material), false);
            autoApplyFoliageToPreview = EditorGUILayout.Toggle(new GUIContent("Auto-Apply to New Previews", "Automatically swap preview chunk renderers to the foliage material whenever a preview is generated."), autoApplyFoliageToPreview);

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            previewChunkIndex = EditorGUILayout.IntSlider(new GUIContent("Preview Chunk Sequence (0-10)", "Simulates placing chunks sequentially along the highway (0m, 40m, 80m, 120m...). The overgrowth pattern evolves seamlessly across 3-4 chunks."), previewChunkIndex, 0, 10);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyChunkNoiseOffsetToPreview(GetTargetPreviewChunk(), previewChunkIndex * 40f);
            }

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = isFoliageAppliedToPreview ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("🌿 Apply Foliage Material", GUILayout.Height(32)))
            {
                ApplyFoliageMaterialToTarget(GetTargetPreviewChunk());
                ApplyChunkNoiseOffsetToPreview(GetTargetPreviewChunk(), previewChunkIndex * 40f);
            }

            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUILayout.Button("↩️ Revert to Original", GUILayout.Height(32), GUILayout.Width(150)))
            {
                RevertFoliageMaterialOnTarget(GetTargetPreviewChunk());
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            string statusText = isFoliageAppliedToPreview
                ? "Status: <color=#33dd33><b>🌿 Foliage Shader Active</b></color> (overgrown moss & debris visible in Preview)"
                : "Status: <color=#888888>Original Highway Materials Active</color>";
            EditorGUILayout.LabelField(statusText, new GUIStyle(EditorStyles.miniLabel) { richText = true });

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📦 Apply Foliage to All Baked Prefabs", GUILayout.Height(24)))
            {
                ApplyFoliageToAllBakedPrefabs();
            }
            GUI.backgroundColor = new Color(1f, 0.85f, 0.85f);
            if (GUILayout.Button("↩️ Revert All Baked Prefabs", GUILayout.Height(24)))
            {
                RevertAllBakedPrefabsToOriginal();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Hybrid Storytelling Decals (1-Click Stamper & Batch Randomizer)
            EditorGUILayout.LabelField("🎯 Storytelling Road Decals (Cracks, Mud & Moss)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.HelpBox("Project decals onto the road surface. Decals use Layer 'Ignore Raycast' and shallow vertical projection bounds so they never interfere with player vehicles or colliders.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            minDecalsPerChunk = EditorGUILayout.IntSlider("Min Decals / Chunk", minDecalsPerChunk, 0, 10);
            maxDecalsPerChunk = EditorGUILayout.IntSlider("Max Decals / Chunk", maxDecalsPerChunk, 1, 15);
            if (maxDecalsPerChunk < minDecalsPerChunk) maxDecalsPerChunk = minDecalsPerChunk;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            includeDecalCracks = EditorGUILayout.ToggleLeft("💥 Asphalt Cracks", includeDecalCracks, GUILayout.Width(130));
            includeDecalMud = EditorGUILayout.ToggleLeft("🪨 Mud & Debris", includeDecalMud, GUILayout.Width(130));
            includeDecalMoss = EditorGUILayout.ToggleLeft("🌿 Moss Patches", includeDecalMoss, GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // 1-Click Batch Buttons
            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("🎲 Spawn Random Decals in All Baked Prefabs (1-Click)", GUILayout.Height(30)))
            {
                SpawnRandomDecalsOnAllBakedPrefabs();
            }
            GUI.backgroundColor = new Color(0.6f, 1f, 0.7f);
            if (GUILayout.Button("🎲 Spawn Random Decals on Active Preview Chunk", GUILayout.Height(26)))
            {
                int count = Random.Range(minDecalsPerChunk, maxDecalsPerChunk + 1);
                SpawnRandomDecalsOnTarget(GetTargetPreviewChunk(), count);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.85f, 0.85f);
            if (GUILayout.Button("🧹 Clear Decals from All Baked Prefabs", GUILayout.Height(22)))
            {
                ClearDecalsFromAllBakedPrefabs();
            }
            if (GUILayout.Button("🧹 Clear Decals from Preview Chunk", GUILayout.Height(22)))
            {
                ClearDecalsOnPreview(GetTargetPreviewChunk());
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Manual 1-Click Stampers (Active Preview):", EditorStyles.miniBoldLabel);

            // Row 1: Cracks
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("💥 Cracked 1", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Cracked_1", new Vector3(Random.Range(-2.8f, 2.8f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("💥 Cracked 2", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Cracked_2", new Vector3(Random.Range(-2.8f, 2.8f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("💥 Road Crack", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_RoadCrack", new Vector3(Random.Range(-2.8f, 2.8f), 0.25f, Random.Range(-15f, 15f)));
            }
            EditorGUILayout.EndHorizontal();

            // Row 2: Mud
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🪨 Mud Decal 1", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Mud_Decal_1", new Vector3(Random.Range(-3.5f, 3.5f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("🪨 Mud Decal 2", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Mud_Decal_2", new Vector3(Random.Range(-3.5f, 3.5f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("🪨 Mud Debris", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_MudDebris", new Vector3(Random.Range(-3.5f, 3.5f), 0.25f, Random.Range(-15f, 15f)));
            }
            EditorGUILayout.EndHorizontal();

            // Row 3: Moss
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌿 Moss 1", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Moss_1", new Vector3((Random.value < 0.5f ? -1f : 1f) * Random.Range(2.5f, 4.2f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("🌿 Moss 2", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_Moss_2", new Vector3((Random.value < 0.5f ? -1f : 1f) * Random.Range(2.5f, 4.2f), 0.25f, Random.Range(-15f, 15f)));
            }
            if (GUILayout.Button("🌿 Moss Cluster", GUILayout.Height(22)))
            {
                StampDecalOnPreview("Decal_MossPatch", new Vector3(Random.Range(-4.2f, 4.2f), 0.25f, Random.Range(-15f, 15f)));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Batch Baking
            EditorGUILayout.LabelField("📦 1-Batch Prefab Baker", EditorStyles.boldLabel);
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

        private GameObject GetTargetPreviewChunk()
        {
            if (currentPreviewChunk != null) return currentPreviewChunk;
            GameObject existing = GameObject.Find("[PROD_CHUNK_PREVIEW]");
            if (existing != null) return existing;
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<ProceduralChunk>() != null)
            {
                return Selection.activeGameObject;
            }
            return null;
        }

        private void ApplyFoliageMaterialToTarget(GameObject target)
        {
            if (target == null)
            {
                EditorUtility.DisplayDialog("Foliage Preview", "No preview chunk found in scene. Generate a preview chunk first or select one in the hierarchy.", "OK");
                return;
            }

            if (foliageMaterial == null)
            {
                foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Foliage_Sample.mat");
            }
            if (roadFoliageMaterial == null)
            {
                roadFoliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Road_Foliage.mat");
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            int count = 0;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!originalMaterialsCache.ContainsKey(r))
                {
                    originalMaterialsCache[r] = r.sharedMaterials;
                }

                bool isRoad = IsRoadRenderer(r);
                Material targetMat = (isRoad && roadFoliageMaterial != null) ? roadFoliageMaterial : foliageMaterial;
                if (targetMat == null) continue;

                Material[] newMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < newMats.Length; i++)
                {
                    newMats[i] = targetMat;
                }
                r.sharedMaterials = newMats;
                count++;
            }

            isFoliageAppliedToPreview = true;
            SceneView.RepaintAll();
            Debug.Log($"<color=green>[ChunkStudio]</color> Applied Foliage Material to {count} renderers on '{target.name}'.");
        }

        private bool IsRoadRenderer(Renderer r)
        {
            if (r == null) return false;
            string objName = r.name;
            if (objName.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                objName.IndexOf("Path", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            Transform parent = r.transform.parent;
            while (parent != null && parent != r.transform.root)
            {
                if (parent.name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    parent.name.IndexOf("Path", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                parent = parent.parent;
            }

            if (r.sharedMaterial != null)
            {
                string matName = r.sharedMaterial.name;
                if (matName.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void RevertFoliageMaterialOnTarget(GameObject target)
        {
            if (target == null)
            {
                target = GetTargetPreviewChunk();
            }

            if (target == null)
            {
                EditorUtility.DisplayDialog("Foliage Preview", "No preview chunk found in scene.", "OK");
                return;
            }

            Material defaultRoad = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Road.mat");
            Material defaultProps = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Highway/Mats/Highway Props.mat");

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            int count = 0;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (originalMaterialsCache.TryGetValue(r, out Material[] cached) && cached != null && cached.Length > 0 && cached[0] != null)
                {
                    r.sharedMaterials = cached;
                    count++;
                }
                else
                {
                    Material fallback = GetOriginalMaterialForRenderer(r, defaultRoad, defaultProps);
                    Material[] newMats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = fallback;
                    }
                    r.sharedMaterials = newMats;
                    count++;
                }
            }

            originalMaterialsCache.Clear();
            isFoliageAppliedToPreview = false;
            SceneView.RepaintAll();
            Debug.Log($"<color=yellow>[ChunkStudio]</color> Reverted {count} renderers back to original highway materials.");
        }

        private Material GetOriginalMaterialForRenderer(Renderer r, Material defaultRoad, Material defaultProps)
        {
            if (r == null) return defaultProps;
            if (IsRoadRenderer(r)) return defaultRoad;

            string rName = r.name.ToLower();
            Transform parent = r.transform.parent;
            string pName = parent != null ? parent.name.ToLower() : "";

            if (rName.Contains("van") || pName.Contains("van"))
                return AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Vehicles/Materials/M_Van_Rusted.mat") ?? defaultProps;
            if (rName.Contains("bus") || pName.Contains("bus"))
                return AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Vehicles/Materials/M_Bus_Rusted.mat") ?? defaultProps;
            if (rName.Contains("suv") || pName.Contains("suv"))
                return AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Vehicles/Materials/M_SUV_Rusted.mat") ?? defaultProps;
            if (rName.Contains("car") || pName.Contains("car"))
                return AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Vehicles/Materials/M_Car_Rusted.mat") ?? defaultProps;

            return defaultProps;
        }

        private void RevertAllBakedPrefabsToOriginal()
        {
            if (string.IsNullOrEmpty(bakeFolder) || !Directory.Exists(bakeFolder))
            {
                EditorUtility.DisplayDialog("Revert Prefabs", $"Folder '{bakeFolder}' not found.", "OK");
                return;
            }

            Material defaultRoad = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Road.mat");
            Material defaultProps = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Highway/Mats/Highway Props.mat");

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { bakeFolder });
            int modifiedCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    Material fallback = GetOriginalMaterialForRenderer(r, defaultRoad, defaultProps);
                    Material[] newMats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = fallback;
                    }
                    r.sharedMaterials = newMats;
                }

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
                modifiedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=yellow>[ChunkStudio]</color> Reverted {modifiedCount} chunk prefabs in '{bakeFolder}' back to original highway materials.");
            EditorUtility.DisplayDialog("Revert Complete", $"Successfully reverted {modifiedCount} chunk prefabs in '{bakeFolder}' back to original highway materials!", "OK");
        }

        private void ApplyChunkNoiseOffsetToPreview(GameObject target, float offset)
        {
            if (target == null) return;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_NoiseOffset", offset);
                r.SetPropertyBlock(mpb);
            }
            SceneView.RepaintAll();
        }

        private void StampDecalOnPreview(string prefabName, Vector3 localPos)
        {
            GameObject target = GetTargetPreviewChunk();
            if (target == null)
            {
                EditorUtility.DisplayDialog("Decal Stamper", "Please generate a preview chunk in the scene first.", "OK");
                return;
            }

            GameObject decalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Decals/{prefabName}.prefab");
            if (decalPrefab == null)
            {
                EditorUtility.DisplayDialog("Decal Stamper", $"Prefab 'Assets/Prefabs/Decals/{prefabName}.prefab' not found.", "OK");
                return;
            }

            Transform decalsRoot = target.transform.Find("Decals");
            if (decalsRoot == null)
            {
                GameObject go = new GameObject("Decals");
                go.transform.SetParent(target.transform, false);
                decalsRoot = go.transform;
            }

            GameObject decalInstance = (GameObject)PrefabUtility.InstantiatePrefab(decalPrefab, decalsRoot);
            decalInstance.name = $"{prefabName}_{decalsRoot.childCount:D2}";
            decalInstance.transform.localPosition = localPos;
            decalInstance.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            float scale = Random.Range(0.85f, 1.35f);
            decalInstance.transform.localScale = new Vector3(scale, scale, 1.0f);

            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;
            decalInstance.layer = ignoreRaycastLayer;

            Collider col = decalInstance.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Selection.activeGameObject = decalInstance;
            SceneView.RepaintAll();
            Debug.Log($"<color=cyan>[ChunkStudio]</color> Stamped {decalInstance.name} at {localPos} (Layer: Ignore Raycast).");
        }

        private void SpawnRandomDecalsOnTarget(GameObject target, int count)
        {
            if (target == null) return;

            Transform decalsRoot = target.transform.Find("Decals");
            if (decalsRoot == null)
            {
                GameObject go = new GameObject("Decals");
                go.transform.SetParent(target.transform, false);
                decalsRoot = go.transform;
            }
            else
            {
                for (int i = decalsRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(decalsRoot.GetChild(i).gameObject);
                }
            }

            List<string> candidatePrefabs = new List<string>();
            if (includeDecalCracks)
            {
                candidatePrefabs.Add("Decal_Cracked_1");
                candidatePrefabs.Add("Decal_Cracked_2");
                candidatePrefabs.Add("Decal_RoadCrack");
            }
            if (includeDecalMud)
            {
                candidatePrefabs.Add("Decal_Mud_Decal_1");
                candidatePrefabs.Add("Decal_Mud_Decal_2");
                candidatePrefabs.Add("Decal_MudDebris");
            }
            if (includeDecalMoss)
            {
                candidatePrefabs.Add("Decal_Moss_1");
                candidatePrefabs.Add("Decal_Moss_2");
                candidatePrefabs.Add("Decal_MossPatch");
            }

            if (candidatePrefabs.Count == 0) return;

            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;

            for (int i = 0; i < count; i++)
            {
                string chosenName = candidatePrefabs[Random.Range(0, candidatePrefabs.Count)];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Decals/{chosenName}.prefab");
                if (prefab == null) continue;

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, decalsRoot);
                inst.name = $"{chosenName}_{i + 1:D2}";
                inst.layer = ignoreRaycastLayer;

                float posX = 0f;
                if (chosenName.Contains("Moss"))
                {
                    posX = (Random.value < 0.65f)
                        ? ((Random.value < 0.5f ? -1f : 1f) * Random.Range(2.5f, 4.3f))
                        : Random.Range(-1.2f, 1.2f);
                }
                else if (chosenName.Contains("Mud"))
                {
                    posX = Random.Range(-3.8f, 3.8f);
                }
                else
                {
                    posX = Random.Range(-3.2f, 3.2f);
                }

                float posZ = Random.Range(-24f, 24f);
                float posY = 0.25f;

                inst.transform.localPosition = new Vector3(posX, posY, posZ);
                inst.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.35f);
                inst.transform.localScale = new Vector3(scale, scale, 1.0f);

                Collider col = inst.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
            }
        }

        private void SpawnRandomDecalsOnAllBakedPrefabs()
        {
            if (string.IsNullOrEmpty(bakeFolder) || !Directory.Exists(bakeFolder))
            {
                EditorUtility.DisplayDialog("Spawn Decals", $"Folder '{bakeFolder}' not found.", "OK");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { bakeFolder });
            int modifiedCount = 0;
            int totalDecalsCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                int decalCount = Random.Range(minDecalsPerChunk, maxDecalsPerChunk + 1);
                SpawnRandomDecalsOnTarget(prefab, decalCount);
                totalDecalsCount += decalCount;

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
                modifiedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[ChunkStudio]</color> Spawned {totalDecalsCount} decals across {modifiedCount} chunk prefabs in '{bakeFolder}'.");
            EditorUtility.DisplayDialog("Decals Spawned", $"Successfully spawned {totalDecalsCount} random decals across {modifiedCount} chunk prefabs in '{bakeFolder}'!\n\nAll decals assigned to Layer 'Ignore Raycast' with zero physics collisions.", "OK");
        }

        private void ClearDecalsFromAllBakedPrefabs()
        {
            if (string.IsNullOrEmpty(bakeFolder) || !Directory.Exists(bakeFolder))
            {
                EditorUtility.DisplayDialog("Clear Decals", $"Folder '{bakeFolder}' not found.", "OK");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { bakeFolder });
            int modifiedCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                Transform decalsRoot = prefab.transform.Find("Decals");
                if (decalsRoot != null)
                {
                    DestroyImmediate(decalsRoot.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    modifiedCount++;
                }

                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=yellow>[ChunkStudio]</color> Cleared decals from {modifiedCount} chunk prefabs in '{bakeFolder}'.");
            EditorUtility.DisplayDialog("Decals Cleared", $"Successfully cleared decals from {modifiedCount} chunk prefabs in '{bakeFolder}'.", "OK");
        }

        private void ClearDecalsOnPreview(GameObject target)
        {
            if (target == null) return;
            Transform decalsRoot = target.transform.Find("Decals");
            if (decalsRoot != null)
            {
                Undo.DestroyObjectImmediate(decalsRoot.gameObject);
                SceneView.RepaintAll();
                Debug.Log("<color=yellow>[ChunkStudio]</color> Cleared decals from preview chunk.");
            }
        }

        private void ApplyFoliageToAllBakedPrefabs()
        {
            if (foliageMaterial == null)
            {
                foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Foliage_Sample.mat");
            }
            if (roadFoliageMaterial == null)
            {
                roadFoliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Road_Foliage.mat");
            }
            if (foliageMaterial == null)
            {
                EditorUtility.DisplayDialog("Foliage Batch Apply", "No Foliage Material assigned.", "OK");
                return;
            }

            if (!Directory.Exists(bakeFolder))
            {
                EditorUtility.DisplayDialog("Foliage Batch Apply", $"Folder '{bakeFolder}' not found.", "OK");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { bakeFolder });
            int modifiedCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    bool isRoad = IsRoadRenderer(r);
                    Material targetMat = (isRoad && roadFoliageMaterial != null) ? roadFoliageMaterial : foliageMaterial;
                    if (targetMat == null) continue;

                    Material[] newMats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++)
                    {
                        newMats[i] = targetMat;
                    }
                    r.sharedMaterials = newMats;
                }

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
                modifiedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[ChunkStudio]</color> Applied Foliage Material to {modifiedCount} chunk prefabs in '{bakeFolder}'.");
            EditorUtility.DisplayDialog("Foliage Batch Complete", $"Successfully applied Foliage Material to {modifiedCount} chunk prefabs in '{bakeFolder}'!", "OK");
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

            previewChunkIndex = (previewChunkIndex + 1) % 11;

            if (autoApplyFoliageToPreview)
            {
                ApplyFoliageMaterialToTarget(currentPreviewChunk);
                ApplyChunkNoiseOffsetToPreview(currentPreviewChunk, previewChunkIndex * 40f);
            }
            else
            {
                isFoliageAppliedToPreview = false;
            }

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
            originalMaterialsCache.Clear();
            isFoliageAppliedToPreview = false;
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

            // 7. Vehicles (Assets/Prefabs/Obstrucle/)
            profile.vehicles.vehiclePrefabs.Clear();
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Bus.prefab", profile.vehicles.vehiclePrefabs, 1f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Car.prefab", profile.vehicles.vehiclePrefabs, 1.2f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Car 1 Variant.prefab", profile.vehicles.vehiclePrefabs, 1f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Car 2 Variant.prefab", profile.vehicles.vehiclePrefabs, 1f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/SUV.prefab", profile.vehicles.vehiclePrefabs, 1f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Van.prefab", profile.vehicles.vehiclePrefabs, 1f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Obstrucle1.prefab", profile.vehicles.vehiclePrefabs, 0.8f);
            AddExactPrefabIfFound("Assets/Prefabs/Obstrucle/Obstrucle2.prefab", profile.vehicles.vehiclePrefabs, 0.8f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[ChunkStudio]</color> Auto-populated profile with Highway environment assets and Assets/Prefabs/Obstrucle vehicles!");
        }

        private void AddExactPrefabIfFound(string assetPath, List<CategorizedAssetItem> targetList, float weight = 1f)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go != null)
            {
                targetList.Add(new CategorizedAssetItem(go, weight, null, null, false));
            }
        }

        private void AddPrefabIfFound(string nameSearch, List<CategorizedAssetItem> targetList, float weight = 1f, Vector3? offset = null, Vector3? rot = null, bool isBroken = false)
        {
            string[] guids = AssetDatabase.FindAssets($"{nameSearch} t:Prefab");
            if (guids.Length == 0) guids = AssetDatabase.FindAssets($"{nameSearch} t:Model");

            if (guids.Length > 0)
            {
                // Prefer prefabs located in Assets/Prefabs/Obstrucle
                string selectedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (p.IndexOf("Assets/Prefabs/Obstrucle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        selectedPath = p;
                        break;
                    }
                }

                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
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
