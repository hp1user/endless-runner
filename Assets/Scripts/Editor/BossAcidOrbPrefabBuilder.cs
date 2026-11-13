using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using VFX;

namespace VFX.EditorTool
{
    public static class BossAcidOrbPrefabBuilder
    {
        private const string PREFAB_PATH = "Assets/Prefabs/VFX/Boss_AcidOrb.prefab";
        private const string RESOURCES_PREFAB_PATH = "Assets/Resources/Boss_AcidOrb.prefab";
        private const string CORE_MAT_PATH = "Assets/Materials/VFX/M_AcidOrb_Core.mat";
        private const string LOOP_VFX_PATH = "Assets/GabrielAguiarProductions/Prefabs/ToonLiquids/vfx_RadioactiveWaste_Loop_v1.prefab";

        [MenuItem("Tools/VFX/Build or Rebuild Boss Acid Orb Prefab")]
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

            Material coreMat = AssetDatabase.LoadAssetAtPath<Material>(CORE_MAT_PATH);
            GameObject loopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LOOP_VFX_PATH);

            // 1. Root Container (does NOT rotate, only translates along lane)
            GameObject rootOrb = new GameObject("Boss_AcidOrb");

            // 2. Child Visual Mesh (Sphere that wobbles & spins in place independently)
            GameObject meshChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshChild.name = "Visual_OrbMesh";
            meshChild.transform.SetParent(rootOrb.transform);
            meshChild.transform.localPosition = Vector3.zero;
            meshChild.transform.localScale = new Vector3(1.35f, 1.35f, 1.45f);

            Collider col = meshChild.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            if (coreMat != null)
            {
                meshChild.GetComponent<Renderer>().sharedMaterial = coreMat;
            }

            // Wobble & tumbling spin attached ONLY to the visual mesh child
            BossAcidOrbVisual orbVisual = meshChild.AddComponent<BossAcidOrbVisual>();
            orbVisual.wobbleSpeed = 13.5f;
            orbVisual.wobbleAmount = 0.16f;
            orbVisual.spinVelocity = new Vector3(35f, 55f, 180f);
            orbVisual.baseScale = new Vector3(1.35f, 1.35f, 1.45f);

            // 3. Child Dripping Droplets Effect (Sibling of Visual_OrbMesh, unaffected by its spin)
            if (loopPrefab != null)
            {
                GameObject dripChild = Object.Instantiate(loopPrefab, rootOrb.transform);
                dripChild.name = "Dripping_Droplets";
                dripChild.transform.localPosition = Vector3.zero;
                dripChild.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                VisualEffect dripVfx = dripChild.GetComponentInChildren<VisualEffect>();
                if (dripVfx != null)
                {
                    dripVfx.SetFloat("Rate", 12f); // Slowed down burst rate for dripping droplets
                    dripVfx.SetFloat("BloodLifetime", 1.2f);
                    dripVfx.SetFloat("GroundBloodScale", 3.5f);
                    dripVfx.SetFloat("GroundBloodLifetime", 3.0f);
                }

                // 4. Ground-Level Liquid Effect (Sibling of Visual_OrbMesh, laying flat on the ground)
                GameObject groundChild = Object.Instantiate(loopPrefab, rootOrb.transform);
                groundChild.name = "Ground_Acid_Trail";
                groundChild.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                groundChild.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                VisualEffect groundVfx = groundChild.GetComponentInChildren<VisualEffect>();
                if (groundVfx != null)
                {
                    groundVfx.SetFloat("Rate", 16f);
                    groundVfx.SetFloat("GroundBloodScale", 4.5f);
                    groundVfx.SetFloat("GroundBloodLifetime", 3.5f);
                    groundVfx.SetVector3("BloodVelocity", new Vector3(1.0f, 1.5f, 1.0f));
                }
            }

            // 5. Save as Prefab Asset in both Prefabs/VFX and Resources/
            PrefabUtility.SaveAsPrefabAsset(rootOrb, PREFAB_PATH);
            PrefabUtility.SaveAsPrefabAsset(rootOrb, RESOURCES_PREFAB_PATH);
            Object.DestroyImmediate(rootOrb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[VFX Builder]</color> Successfully built separated Boss Acid Orb prefab at: {PREFAB_PATH} and {RESOURCES_PREFAB_PATH}");
        }
    }
}
