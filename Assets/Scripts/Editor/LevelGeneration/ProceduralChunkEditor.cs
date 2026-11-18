using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EndlessRunner.LevelGen;

namespace EndlessRunner.LevelGen.Editor
{
    [CustomEditor(typeof(ProceduralChunk))]
    public class ProceduralChunkEditor : UnityEditor.Editor
    {
        private static Material foliageMaterial;
        private static Material roadFoliageMaterial;
        private static readonly Dictionary<Renderer, Material[]> originalMaterialsCache = new Dictionary<Renderer, Material[]>();
        private bool isFoliageActive = false;

        private void OnEnable()
        {
            if (foliageMaterial == null)
            {
                foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Foliage_Sample.mat");
            }
            if (roadFoliageMaterial == null)
            {
                roadFoliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Road_Foliage.mat");
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProceduralChunk chunk = (ProceduralChunk)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🌿 Foliage / Post-Apocalyptic Material Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            foliageMaterial = (Material)EditorGUILayout.ObjectField("Props Foliage Mat", foliageMaterial, typeof(Material), false);
            roadFoliageMaterial = (Material)EditorGUILayout.ObjectField("Road Foliage Mat", roadFoliageMaterial, typeof(Material), false);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = isFoliageActive ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("🌿 Apply Foliage Material", GUILayout.Height(30)))
            {
                ApplyFoliageMaterial(chunk.gameObject);
            }

            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f);
            if (GUILayout.Button("↩️ Revert Original", GUILayout.Height(30), GUILayout.Width(130)))
            {
                RevertOriginalMaterial(chunk.gameObject);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            string status = isFoliageActive 
                ? "Status: <color=#33dd33><b>🌿 Foliage Shader Active (Triplanar & Debris Blended)</b></color>" 
                : "Status: <color=#888888>Original Highway Materials Active</color>";
            EditorGUILayout.LabelField(status, new GUIStyle(EditorStyles.miniLabel) { richText = true });

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎲 Layout Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎲 Regenerate Layout", GUILayout.Height(28)))
            {
                chunk.GenerateRandomLayout();
                if (isFoliageActive)
                {
                    ApplyFoliageMaterial(chunk.gameObject);
                }
            }
            if (GUILayout.Button("🧹 Clear Layout", GUILayout.Height(28), GUILayout.Width(100)))
            {
                chunk.ClearLayout();
                originalMaterialsCache.Clear();
                isFoliageActive = false;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyFoliageMaterial(GameObject targetGo)
        {
            if (foliageMaterial == null)
            {
                foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Foliage_Sample.mat");
            }
            if (roadFoliageMaterial == null)
            {
                roadFoliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/M_PostApoc_Road_Foliage.mat");
            }

            Renderer[] renderers = targetGo.GetComponentsInChildren<Renderer>(true);
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

            isFoliageActive = true;
            SceneView.RepaintAll();
            Debug.Log($"<color=green>[ProceduralChunk]</color> Applied Foliage Material to {count} renderers on '{targetGo.name}'.");
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

        private void RevertOriginalMaterial(GameObject targetGo)
        {
            if (targetGo == null) return;

            Material defaultRoad = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Road.mat");
            Material defaultProps = AssetDatabase.LoadAssetAtPath<Material>("Assets/3d/Environment/Highway/Mats/Highway Props.mat");

            Renderer[] renderers = targetGo.GetComponentsInChildren<Renderer>(true);
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
            isFoliageActive = false;
            SceneView.RepaintAll();
            Debug.Log($"<color=yellow>[ProceduralChunk]</color> Reverted {count} renderers back to original highway materials.");
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
    }
}
