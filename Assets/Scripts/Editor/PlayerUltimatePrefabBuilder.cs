using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Player.Control;

namespace VFX.EditorTool
{
    [InitializeOnLoad]
    public static class PlayerUltimatePrefabBuilder
    {
        private const string PREFAB_PATH = "Assets/Prefabs/VFX/PlayerUltimate_EMP.prefab";
        private const string RESOURCES_PREFAB_PATH = "Assets/Resources/PlayerUltimate_EMP.prefab";
        private const string SOURCE_BURST_PREFAB = "Assets/GabrielAguiarProductions/Prefabs/ElectricOrbs/vfx_Electricity_Burst_v2.prefab";
        private const string AOE_ASSET_PATH = "Assets/ScriptableObjects/Player/Skill & Ultimate/AOE.asset";
        private const string BUILD_KEY = "PlayerUltimate_EMP_Built_v2";

        static PlayerUltimatePrefabBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(BUILD_KEY, false))
                {
                    SessionState.SetBool(BUILD_KEY, true);
                    BuildPrefab();
                }
            };
        }

        [MenuItem("Tools/VFX/Build or Rebuild Player Ultimate EMP Prefab")]
        public static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VFX"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            GameObject burstSource = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_BURST_PREFAB);
            if (burstSource == null)
            {
                Debug.LogError($"[PlayerUltimatePrefabBuilder] Could not find burst prefab at {SOURCE_BURST_PREFAB}");
                return;
            }

            // 1. Root GameObject
            GameObject rootEMP = new GameObject("PlayerUltimate_EMP");
            rootEMP.transform.position = Vector3.zero;

            // 2. Child EMP Discharge Effect (instantiated from vfx_Electricity_Burst_v2)
            GameObject burstInstance = Object.Instantiate(burstSource, rootEMP.transform);
            burstInstance.name = "EMP_RadialBurst";
            burstInstance.transform.localPosition = new Vector3(0f, 1.2f, 0f); // Torso level
            burstInstance.transform.localRotation = Quaternion.identity;
            burstInstance.transform.localScale = Vector3.one * 0.5f; // Starts small, grown by EMPScaleGrowth

            VisualEffect vfx = burstInstance.GetComponentInChildren<VisualEffect>();
            if (vfx != null)
            {
                vfx.SetFloat("ElectricityRate", 50f);
                vfx.SetFloat("SparksRate", 35f);
                vfx.SetFloat("SparksLifetime", 0.5f);
                vfx.SetFloat("ElectricityLifetime", 0.4f);
                vfx.SetBool("Loop", true); // Looped during the growth phase so lightning arcs erupt continuously
            }

            // 3. Child Point Light for dynamic flash illumination
            GameObject lightObj = new GameObject("EMP_PointLight");
            lightObj.transform.SetParent(rootEMP.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            Light flashLight = lightObj.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(0.1f, 0.75f, 1f, 1f); // Electric Cyan
            flashLight.intensity = 6f;
            flashLight.range = 3f;
            flashLight.shadows = LightShadows.None;

            // 4. Attach EMPScaleGrowth controller to animate gradual radial expansion
            EMPScaleGrowth growthCtrl = rootEMP.AddComponent<EMPScaleGrowth>();
            
            // Use SerializedObject to wire private serialized fields cleanly
            SerializedObject so = new SerializedObject(growthCtrl);
            so.FindProperty("visualTransform").objectReferenceValue = burstInstance.transform;
            so.FindProperty("startScale").floatValue = 0.5f;
            so.FindProperty("targetScale").floatValue = 5.0f; // Covers 10m road width
            so.FindProperty("growthDuration").floatValue = 1.2f; // Smooth, visible gradual expansion
            so.FindProperty("visualEffect").objectReferenceValue = vfx;
            so.FindProperty("flashLight").objectReferenceValue = flashLight;
            so.FindProperty("maxLightIntensity").floatValue = 6f;
            so.FindProperty("maxLightRange").floatValue = 16f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 5. Save Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootEMP, PREFAB_PATH);
            PrefabUtility.SaveAsPrefabAsset(rootEMP, RESOURCES_PREFAB_PATH);
            Object.DestroyImmediate(rootEMP);

            // 6. Assign to AOE.asset
            SkillData aoeSkill = AssetDatabase.LoadAssetAtPath<SkillData>(AOE_ASSET_PATH);
            if (aoeSkill != null)
            {
                aoeSkill.visualEffectPrefab = savedPrefab;
                aoeSkill.effectDuration = 2.0f; // 1.2s growth + 0.8s spark dissipation
                EditorUtility.SetDirty(aoeSkill);
                Debug.Log($"<color=cyan>[PlayerUltimatePrefabBuilder]</color> Successfully assigned {PREFAB_PATH} to AOE.asset (effectDuration = 2.0s, radius = {aoeSkill.effectRadius}m)");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[PlayerUltimatePrefabBuilder]</color> Successfully built Player Ultimate EMP Prefab with Gradual Growth at {PREFAB_PATH}");
        }
    }
}
