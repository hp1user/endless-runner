using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.LevelGen
{
    [Serializable]
    public class CategorizedAssetItem
    {
        public string assetName = "Asset";
        public GameObject prefab;
        [Range(0.01f, 10f)]
        public float weight = 1f;
        [Tooltip("Marks this item as damaged/broken so the generator maintains spacing between broken pieces")]
        public bool isBroken = false;
        public Vector3 offset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public Vector3 scaleMultiplier = Vector3.one;

        public CategorizedAssetItem() { }

        public CategorizedAssetItem(GameObject prefab, float weight = 1f, Vector3? offset = null, Vector3? rot = null, bool isBroken = false)
        {
            this.prefab = prefab;
            this.assetName = prefab != null ? prefab.name : "Asset";
            this.weight = weight;
            this.isBroken = isBroken;
            this.offset = offset ?? Vector3.zero;
            this.rotationOffset = rot ?? Vector3.zero;
            this.scaleMultiplier = Vector3.one;
        }
    }

    [Serializable]
    public class SideRailingRule
    {
        public bool enabled = true;
        public List<CategorizedAssetItem> railingPrefabs = new List<CategorizedAssetItem>();
        [Tooltip("Minimum number of intact/normal pieces required between broken pieces")]
        [Range(0, 10)]
        public int minIntactBetweenBroken = 2;
        [Tooltip("Length of each railing segment along the Z axis (e.g. 5m or 10m)")]
        public float segmentLength = 8f;
        [Tooltip("Left side X position")]
        public float leftX = -5.5f;
        [Tooltip("Right side X position")]
        public float rightX = 5.5f;
        [Range(0f, 1f)]
        public float spawnChance = 1.0f;
    }

    [Serializable]
    public class CenterDividerRule
    {
        public bool enabled = true;
        public List<CategorizedAssetItem> dividerPrefabs = new List<CategorizedAssetItem>();
        [Tooltip("Minimum number of intact/normal pieces required between broken pieces")]
        [Range(0, 10)]
        public int minIntactBetweenBroken = 2;
        [Tooltip("Spacing between consecutive divider segments")]
        public float segmentLength = 6f;
        public float centerX = 0f;
        [Range(0f, 1f)]
        public float spawnChance = 0.85f;

        [Header("Center Lighting")]
        public bool includeCenterLights = true;
        public List<CategorizedAssetItem> centerLightPrefabs = new List<CategorizedAssetItem>();
        public float centerLightInterval = 20f;
    }

    [Serializable]
    public class StreetLightRule
    {
        public enum PlacementMode { BothSides, Alternating, LeftOnly, RightOnly }

        public bool enabled = true;
        public List<CategorizedAssetItem> lightPrefabs = new List<CategorizedAssetItem>();
        public float intervalZ = 20f;
        public float leftX = -5.8f;
        public float rightX = 5.8f;
        public PlacementMode placementMode = PlacementMode.Alternating;
        [Range(0f, 1f)]
        public float spawnChance = 0.9f;
    }

    [Serializable]
    public class OverheadSignRule
    {
        public bool enabled = true;
        public List<CategorizedAssetItem> signPrefabs = new List<CategorizedAssetItem>();
        [Range(0f, 1f)]
        [Tooltip("Chance that this chunk contains an overhead sign board")]
        public float spawnChancePerChunk = 0.35f;
        public float centerX = 0f;
        public float heightY = 0f;
    }

    [Serializable]
    public class DebrisClutterRule
    {
        public bool enabled = true;
        [Tooltip("If false, no debris will spawn on the playable runner highway (keeps lanes clean for dynamic gameplay)")]
        public bool spawnOnPlayableRoad = false;
        public List<CategorizedAssetItem> debrisPrefabs = new List<CategorizedAssetItem>();
        public int minItemsPerChunk = 1;
        public int maxItemsPerChunk = 4;
        public float minX = -5f;
        public float maxX = 5f;
        public bool randomizeYaw = true;
    }

    [Serializable]
    public class VehicleObstacleRule
    {
        public bool enabled = true;
        [Tooltip("If false, no vehicles will spawn on the playable runner lanes (keeps lanes clean for dynamic gameplay)")]
        public bool spawnOnPlayableRoad = false;
        public List<CategorizedAssetItem> vehiclePrefabs = new List<CategorizedAssetItem>();
        [Range(0f, 1f)]
        public float spawnChancePerChunk = 0.4f;
        public int maxVehiclesPerChunk = 2;
        public float[] spawnLanesX = new float[] { -2f, 0f, 2f, -5f, 5f };
        public bool randomizeYaw = true;
    }

    [Serializable]
    public class DualHighwaySettings
    {
        [Tooltip("Enable a parallel visual-only secondary highway running alongside the playable highway")]
        public bool enableSecondaryHighway = true;
        [Tooltip("Offset of the parallel visual highway from the playable highway")]
        public Vector3 secondaryRoadOffset = new Vector3(-10.5f, 0f, 5f);
        [Tooltip("Position for the center median divider between both highways")]
        public float medianX = -5.25f;
        [Tooltip("Spawn randomized background props (debris, obstacles, cars) on the visual highway")]
        public bool populateVisualHighwayProps = true;
        [Tooltip("Density multiplier for clutter & debris on the visual highway vs playable road (e.g. 2.0 = 2x more)")]
        [Range(1f, 5f)]
        public float visualObstacleMultiplier = 2.0f;
        [Tooltip("Chance per chunk to spawn vehicle traffic on visual road")]
        [Range(0f, 1f)]
        public float visualVehicleSpawnChance = 0.9f;
        [Tooltip("Minimum vehicles on visual highway")]
        public int minVisualVehicles = 2;
        [Tooltip("Maximum vehicles on visual highway")]
        public int maxVisualVehicles = 6;
    }

    [CreateAssetMenu(fileName = "New Procedural Theme Profile", menuName = "Game Data/Procedural Theme Profile")]
    public class ProceduralEnvironmentProfile : ScriptableObject
    {
        [Header("Base Settings")]
        public string profileName = "Highway Procedural";
        public GameObject roadBasePrefab;
        public Vector3 roadBaseOffset = new Vector3(0f, 0f, 5f);
        public float chunkLength = 40f;
        public float roadWidth = 10f;

        [Header("Dual Highway (Twin Roadway) Settings")]
        public DualHighwaySettings dualHighway = new DualHighwaySettings();

        [Header("Categorized Placement Rules")]
        public SideRailingRule sideRailings = new SideRailingRule
        {
            leftX = -4.95f,
            rightX = 4.95f,
            segmentLength = 2.58f,
            spawnChance = 1.0f
        };
        public CenterDividerRule centerDividers = new CenterDividerRule();
        public StreetLightRule streetLights = new StreetLightRule
        {
            leftX = -5.2f,
            rightX = 5.2f,
            intervalZ = 18f
        };
        public OverheadSignRule overheadSigns = new OverheadSignRule
        {
            centerX = 10.85f, // Centers the Big Sign Board spanning across both highways
            spawnChancePerChunk = 0.35f
        };
        public DebrisClutterRule debrisAndClutter = new DebrisClutterRule();
        public VehicleObstacleRule vehicles = new VehicleObstacleRule();

        /// <summary>
        /// Picks a random item from a list based on weighted distribution, with optional broken-item filtering.
        /// </summary>
        public static CategorizedAssetItem PickWeightedItem(List<CategorizedAssetItem> items, bool allowBroken = true)
        {
            if (items == null || items.Count == 0) return null;

            float totalWeight = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null && item.prefab != null)
                {
                    if (!allowBroken && item.isBroken) continue;
                    totalWeight += item.weight;
                }
            }

            if (totalWeight <= 0f)
            {
                // Fallback: Pick first non-broken item if allowBroken is false, otherwise first available item
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].prefab != null)
                    {
                        if (!allowBroken && items[i].isBroken) continue;
                        return items[i];
                    }
                }
                return items[0];
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentSum = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null && item.prefab != null)
                {
                    if (!allowBroken && item.isBroken) continue;
                    currentSum += item.weight;
                    if (randomValue <= currentSum) return item;
                }
            }

            return items[0];
        }
    }
}
