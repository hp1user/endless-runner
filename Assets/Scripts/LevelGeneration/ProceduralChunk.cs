using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.LevelGen
{
    [ExecuteAlways]
    public class ProceduralChunk : MonoBehaviour
    {
        [Header("Configuration")]
        public ProceduralEnvironmentProfile profile;
        public bool generateOnStart = false;
        public bool useFixedSeed = false;
        public int fixedSeed = 12345;

        [Header("Hierarchy Containers (Auto-managed)")]
        [SerializeField] private Transform roadBaseContainer;
        [SerializeField] private Transform leftRailContainer;
        [SerializeField] private Transform rightRailContainer;
        [SerializeField] private Transform centerContainer;
        [SerializeField] private Transform lightsContainer;
        [SerializeField] private Transform signsContainer;
        [SerializeField] private Transform debrisContainer;
        [SerializeField] private Transform vehiclesContainer;

        private void Start()
        {
            if (Application.isPlaying && generateOnStart)
            {
                GenerateRandomLayout(useFixedSeed ? fixedSeed : (int?)null);
            }
        }

        public void EnsureContainers()
        {
            roadBaseContainer = GetOrCreateContainer("RoadBase");
            leftRailContainer = GetOrCreateContainer("LeftRails");
            rightRailContainer = GetOrCreateContainer("RightRails");
            centerContainer = GetOrCreateContainer("CenterDividers");
            lightsContainer = GetOrCreateContainer("StreetLights");
            signsContainer = GetOrCreateContainer("OverheadSigns");
            debrisContainer = GetOrCreateContainer("DebrisClutter");
            vehiclesContainer = GetOrCreateContainer("Vehicles");
        }

        private Transform GetOrCreateContainer(string name)
        {
            Transform found = transform.Find(name);
            if (found == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(this.transform, false);
                found = go.transform;
            }
            return found;
        }

        public void ClearLayout()
        {
            EnsureContainers();
            ClearContainer(roadBaseContainer);
            ClearContainer(leftRailContainer);
            ClearContainer(rightRailContainer);
            ClearContainer(centerContainer);
            ClearContainer(lightsContainer);
            ClearContainer(signsContainer);
            ClearContainer(debrisContainer);
            ClearContainer(vehiclesContainer);
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        public void GenerateRandomLayout(int? seed = null)
        {
            if (profile == null)
            {
                Debug.LogWarning($"[ProceduralChunk] No ProceduralEnvironmentProfile assigned on {gameObject.name}");
                return;
            }

            if (seed.HasValue)
            {
                Random.InitState(seed.Value);
            }

            ClearLayout();
            EnsureContainers();

            float halfLength = profile.chunkLength * 0.5f;

            // 1. Road Base Mesh (Primary Playable Road + Optional Visual Secondary Road)
            if (profile.roadBasePrefab != null && roadBaseContainer.childCount == 0)
            {
                // Playable Highway
                InstantiateProp(profile.roadBasePrefab, profile.roadBaseOffset, Quaternion.identity, roadBaseContainer);

                // Parallel Visual Highway
                if (profile.dualHighway.enableSecondaryHighway)
                {
                    InstantiateProp(profile.roadBasePrefab, profile.dualHighway.secondaryRoadOffset, Quaternion.identity, roadBaseContainer);
                }
            }

            bool isDual = profile.dualHighway.enableSecondaryHighway;
            float secX = profile.dualHighway.secondaryRoadOffset.x;
            float medianX = isDual ? profile.dualHighway.medianX : profile.centerDividers.centerX;

            float outerLeftRailX = profile.sideRailings.leftX;
            float outerRightRailX = profile.sideRailings.rightX;

            float outerLeftLightX = profile.streetLights.leftX;
            float outerRightLightX = profile.streetLights.rightX;

            if (isDual)
            {
                if (secX < 0f)
                {
                    // Visual Road is on the Left, Playable Road is on the Right
                    outerLeftRailX = secX + profile.sideRailings.leftX;
                    outerLeftLightX = secX + profile.streetLights.leftX;
                    outerRightRailX = profile.sideRailings.rightX;
                    outerRightLightX = profile.streetLights.rightX;
                }
                else
                {
                    // Playable Road is on the Left, Visual Road is on the Right
                    outerLeftRailX = profile.sideRailings.leftX;
                    outerLeftLightX = profile.streetLights.leftX;
                    outerRightRailX = secX + profile.sideRailings.rightX;
                    outerRightLightX = secX + profile.streetLights.rightX;
                }
            }

            // 2. Side Railings (Outer Left & Outer Right Boundaries)
            if (profile.sideRailings.enabled && profile.sideRailings.railingPrefabs.Count > 0)
            {
                if (Random.value <= profile.sideRailings.spawnChance)
                {
                    float segmentLen = Mathf.Max(profile.sideRailings.segmentLength, 2f);
                    int minIntact = Mathf.Max(0, profile.sideRailings.minIntactBetweenBroken);
                    int leftIntactCount = minIntact; // Start allowed to pick broken
                    int rightIntactCount = minIntact;

                    for (float z = -halfLength + (segmentLen * 0.5f); z < halfLength; z += segmentLen)
                    {
                        // Outer Left Railing
                        bool allowLeftBroken = leftIntactCount >= minIntact;
                        var leftItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.sideRailings.railingPrefabs, allowLeftBroken);
                        if (leftItem != null && leftItem.prefab != null)
                        {
                            if (leftItem.isBroken) leftIntactCount = 0;
                            else leftIntactCount++;

                            Vector3 pos = new Vector3(outerLeftRailX, 0f, z) + leftItem.offset;
                            Quaternion rot = Quaternion.Euler(leftItem.rotationOffset);
                            InstantiateProp(leftItem.prefab, pos, rot, leftRailContainer, leftItem.scaleMultiplier);
                        }

                        // Outer Right Railing (Keep rotation aligned with 0 deg)
                        bool allowRightBroken = rightIntactCount >= minIntact;
                        var rightItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.sideRailings.railingPrefabs, allowRightBroken);
                        if (rightItem != null && rightItem.prefab != null)
                        {
                            if (rightItem.isBroken) rightIntactCount = 0;
                            else rightIntactCount++;

                            Vector3 pos = new Vector3(outerRightRailX, 0f, z) + new Vector3(-rightItem.offset.x, rightItem.offset.y, rightItem.offset.z);
                            Quaternion rot = Quaternion.Euler(rightItem.rotationOffset);
                            InstantiateProp(rightItem.prefab, pos, rot, rightRailContainer, rightItem.scaleMultiplier);
                        }
                    }
                }
            }

            // 3. Center Median Dividers & Center Lights (Between the Two Highways)
            if (profile.centerDividers.enabled && profile.centerDividers.dividerPrefabs.Count > 0)
            {
                if (Random.value <= profile.centerDividers.spawnChance)
                {
                    float segLen = Mathf.Max(profile.centerDividers.segmentLength, 2f);
                    int minIntact = Mathf.Max(0, profile.centerDividers.minIntactBetweenBroken);
                    int intactCount = minIntact; // Start allowed to pick broken

                    for (float z = -halfLength + (segLen * 0.5f); z < halfLength; z += segLen)
                    {
                        bool allowBroken = intactCount >= minIntact;
                        var dividerItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.centerDividers.dividerPrefabs, allowBroken);
                        if (dividerItem != null && dividerItem.prefab != null)
                        {
                            if (dividerItem.isBroken) intactCount = 0;
                            else intactCount++;

                            Vector3 pos = new Vector3(medianX, 0f, z) + dividerItem.offset;
                            Quaternion rot = Quaternion.Euler(dividerItem.rotationOffset);
                            InstantiateProp(dividerItem.prefab, pos, rot, centerContainer, dividerItem.scaleMultiplier);
                        }
                    }
                }

                // Center Lights along Median
                if (profile.centerDividers.includeCenterLights && profile.centerDividers.centerLightPrefabs.Count > 0)
                {
                    float lightInterval = Mathf.Max(profile.centerDividers.centerLightInterval, 10f);
                    for (float z = -halfLength + (lightInterval * 0.5f); z < halfLength; z += lightInterval)
                    {
                        var lightItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.centerDividers.centerLightPrefabs);
                        if (lightItem != null && lightItem.prefab != null)
                        {
                            Vector3 pos = new Vector3(medianX, 0f, z) + lightItem.offset;
                            Quaternion rot = Quaternion.Euler(lightItem.rotationOffset);
                            InstantiateProp(lightItem.prefab, pos, rot, centerContainer, lightItem.scaleMultiplier);
                        }
                    }
                }
            }

            // 4. Street Lights / Small Poles (Shoulders)
            if (profile.streetLights.enabled && profile.streetLights.lightPrefabs.Count > 0)
            {
                float lightInterval = Mathf.Max(profile.streetLights.intervalZ, 8f);
                int index = 0;

                for (float z = -halfLength + (lightInterval * 0.5f); z < halfLength; z += lightInterval)
                {
                    if (Random.value > profile.streetLights.spawnChance) { index++; continue; }

                    bool spawnLeft = false;
                    bool spawnRight = false;

                    switch (profile.streetLights.placementMode)
                    {
                        case StreetLightRule.PlacementMode.BothSides:
                            spawnLeft = true;
                            spawnRight = true;
                            break;
                        case StreetLightRule.PlacementMode.Alternating:
                            if (index % 2 == 0) spawnLeft = true;
                            else spawnRight = true;
                            break;
                        case StreetLightRule.PlacementMode.LeftOnly:
                            spawnLeft = true;
                            break;
                        case StreetLightRule.PlacementMode.RightOnly:
                            spawnRight = true;
                            break;
                    }

                    if (spawnLeft)
                    {
                        var item = ProceduralEnvironmentProfile.PickWeightedItem(profile.streetLights.lightPrefabs);
                        if (item != null && item.prefab != null)
                        {
                            Vector3 pos = new Vector3(outerLeftLightX, 0f, z) + item.offset;
                            // Rotate +90 deg so lamp arm overhangs inwards toward +X (the road)
                            Quaternion rot = Quaternion.Euler(item.rotationOffset.x, item.rotationOffset.y + 90f, item.rotationOffset.z);
                            InstantiateProp(item.prefab, pos, rot, lightsContainer, item.scaleMultiplier);
                        }
                    }

                    if (spawnRight)
                    {
                        var item = ProceduralEnvironmentProfile.PickWeightedItem(profile.streetLights.lightPrefabs);
                        if (item != null && item.prefab != null)
                        {
                            Vector3 pos = new Vector3(outerRightLightX, 0f, z) + new Vector3(-item.offset.x, item.offset.y, item.offset.z);
                            // Rotate -90 deg so lamp arm overhangs inwards toward -X (the road)
                            Quaternion rot = Quaternion.Euler(item.rotationOffset.x, item.rotationOffset.y - 90f, item.rotationOffset.z);
                            InstantiateProp(item.prefab, pos, rot, lightsContainer, item.scaleMultiplier);
                        }
                    }

                    index++;
                }
            }

            // 5. Overhead Sign Boards (Spanning Across Both Highways)
            if (profile.overheadSigns.enabled && profile.overheadSigns.signPrefabs.Count > 0)
            {
                if (Random.value <= profile.overheadSigns.spawnChancePerChunk)
                {
                    var signItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.overheadSigns.signPrefabs);
                    if (signItem != null && signItem.prefab != null)
                    {
                        float randomZ = Random.Range(-halfLength * 0.5f, halfLength * 0.5f);
                        Vector3 pos = new Vector3(profile.overheadSigns.centerX, profile.overheadSigns.heightY, randomZ) + signItem.offset;
                        Quaternion rot = Quaternion.Euler(signItem.rotationOffset);
                        InstantiateProp(signItem.prefab, pos, rot, signsContainer, signItem.scaleMultiplier);
                    }
                }
            }

            // 6. Debris & Roadside Clutter
            if (profile.debrisAndClutter.enabled && profile.debrisAndClutter.debrisPrefabs.Count > 0)
            {
                // Playable Road Debris (Kept clean if spawnOnPlayableRoad is false)
                int playableDebrisCount = 0;
                if (profile.debrisAndClutter.spawnOnPlayableRoad)
                {
                    playableDebrisCount = Random.Range(profile.debrisAndClutter.minItemsPerChunk, profile.debrisAndClutter.maxItemsPerChunk + 1);
                    for (int i = 0; i < playableDebrisCount; i++)
                    {
                        var debrisItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.debrisAndClutter.debrisPrefabs);
                        if (debrisItem != null && debrisItem.prefab != null)
                        {
                            float randX = Random.Range(profile.debrisAndClutter.minX, profile.debrisAndClutter.maxX);
                            float randZ = Random.Range(-halfLength * 0.9f, halfLength * 0.9f);
                            float yaw = profile.debrisAndClutter.randomizeYaw ? Random.Range(0f, 360f) : debrisItem.rotationOffset.y;

                            Vector3 pos = new Vector3(randX, 0f, randZ) + debrisItem.offset;
                            Quaternion rot = Quaternion.Euler(debrisItem.rotationOffset.x, yaw, debrisItem.rotationOffset.z);
                            InstantiateProp(debrisItem.prefab, pos, rot, debrisContainer, debrisItem.scaleMultiplier);
                        }
                    }
                }

                // Visual Highway Debris (2x Density)
                if (isDual && profile.dualHighway.populateVisualHighwayProps)
                {
                    int baseCount = Mathf.Max(playableDebrisCount, Random.Range(1, 4));
                    int visualDebrisCount = Mathf.RoundToInt(baseCount * profile.dualHighway.visualObstacleMultiplier);
                    float visualMinX = secX - 4.5f;
                    float visualMaxX = secX + 4.5f;

                    for (int i = 0; i < visualDebrisCount; i++)
                    {
                        var debrisItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.debrisAndClutter.debrisPrefabs);
                        if (debrisItem != null && debrisItem.prefab != null)
                        {
                            float randX = Random.Range(visualMinX, visualMaxX);
                            float randZ = Random.Range(-halfLength * 0.9f, halfLength * 0.9f);
                            float yaw = profile.debrisAndClutter.randomizeYaw ? Random.Range(0f, 360f) : debrisItem.rotationOffset.y;

                            Vector3 pos = new Vector3(randX, 0f, randZ) + debrisItem.offset;
                            Quaternion rot = Quaternion.Euler(debrisItem.rotationOffset.x, yaw, debrisItem.rotationOffset.z);
                            InstantiateProp(debrisItem.prefab, pos, rot, debrisContainer, debrisItem.scaleMultiplier);
                        }
                    }
                }
            }

            // 7. Vehicles & Obstacles
            if (profile.vehicles.enabled && profile.vehicles.vehiclePrefabs.Count > 0)
            {
                // Playable Road Vehicles (Kept clean if spawnOnPlayableRoad is false)
                if (profile.vehicles.spawnOnPlayableRoad && Random.value <= profile.vehicles.spawnChancePerChunk)
                {
                    int vehicleCount = Random.Range(1, profile.vehicles.maxVehiclesPerChunk + 1);
                    for (int v = 0; v < vehicleCount; v++)
                    {
                        var vehicleItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.vehicles.vehiclePrefabs);
                        if (vehicleItem != null && vehicleItem.prefab != null)
                        {
                            float laneX = profile.vehicles.spawnLanesX.Length > 0
                                ? profile.vehicles.spawnLanesX[Random.Range(0, profile.vehicles.spawnLanesX.Length)]
                                : 0f;

                            float randZ = Random.Range(-halfLength * 0.8f, halfLength * 0.8f);
                            float yaw = profile.vehicles.randomizeYaw ? (Random.value > 0.5f ? 0f : 180f) + Random.Range(-10f, 10f) : vehicleItem.rotationOffset.y;

                            Vector3 pos = new Vector3(laneX, 0f, randZ) + vehicleItem.offset;
                            Quaternion rot = Quaternion.Euler(vehicleItem.rotationOffset.x, yaw, vehicleItem.rotationOffset.z);
                            InstantiateProp(vehicleItem.prefab, pos, rot, vehiclesContainer, vehicleItem.scaleMultiplier);
                        }
                    }
                }

                // Visual Highway Vehicles (2x+ Traffic & Atmospheric Pileups)
                if (isDual && profile.dualHighway.populateVisualHighwayProps)
                {
                    if (Random.value <= profile.dualHighway.visualVehicleSpawnChance)
                    {
                        int visualCount = Random.Range(profile.dualHighway.minVisualVehicles, profile.dualHighway.maxVisualVehicles + 1);
                        float[] visualLanes = new float[] {
                            secX - 3.2f, // Left shoulder / visual lane 1
                            secX - 1.6f, // Visual lane 2
                            secX,        // Visual center lane 3
                            secX + 1.6f, // Visual lane 4
                            secX + 3.2f  // Right shoulder / visual lane 5
                        };

                        for (int v = 0; v < visualCount; v++)
                        {
                            var vehicleItem = ProceduralEnvironmentProfile.PickWeightedItem(profile.vehicles.vehiclePrefabs);
                            if (vehicleItem != null && vehicleItem.prefab != null)
                            {
                                float laneX = visualLanes[Random.Range(0, visualLanes.Length)] + Random.Range(-0.3f, 0.3f);
                                float randZ = Random.Range(-halfLength * 0.85f, halfLength * 0.85f);
                                
                                // Variety of rotations: straight, reverse, or crash angles (20-90 deg)
                                float baseAngle = Random.value < 0.6f ? (Random.value > 0.5f ? 0f : 180f) : Random.Range(-90f, 90f);
                                float yaw = profile.vehicles.randomizeYaw ? baseAngle + Random.Range(-15f, 15f) : vehicleItem.rotationOffset.y;

                                Vector3 pos = new Vector3(laneX, 0f, randZ) + vehicleItem.offset;
                                Quaternion rot = Quaternion.Euler(vehicleItem.rotationOffset.x, yaw, vehicleItem.rotationOffset.z);
                                InstantiateProp(vehicleItem.prefab, pos, rot, vehiclesContainer, vehicleItem.scaleMultiplier);
                            }
                        }
                    }
                }
            }
        }

        private GameObject InstantiateProp(GameObject prefab, Vector3 localPos, Quaternion localRot, Transform parent, Vector3? scale = null)
        {
            if (prefab == null) return null;

            GameObject instance;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance != null)
                {
                    instance.transform.localPosition = localPos;
                    instance.transform.localRotation = localRot;
                    if (scale.HasValue) instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale.Value);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Procedural Prop Spawn");
                    return instance;
                }
            }
#endif
            instance = Instantiate(prefab, parent);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = localRot;
            if (scale.HasValue) instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale.Value);
            return instance;
        }
    }
}
