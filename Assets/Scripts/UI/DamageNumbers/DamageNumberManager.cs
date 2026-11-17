using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Enemy.Control;

namespace UI.DamageNumbers
{
    /// <summary>
    /// Central manager and object pool for floating 3D worldspace damage numbers.
    /// Handles pool prewarming, styling for regular enemies vs boss body parts, and camera offsets.
    /// All size, offset, color, and speed parameters can be controlled via DamageNumberSettings in the Data Manager.
    /// </summary>
    public class DamageNumberManager : MonoBehaviour
    {
        private static DamageNumberManager instance;
        public static DamageNumberManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DamageNumberManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DamageNumberManager");
                        instance = go.AddComponent<DamageNumberManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Configuration Asset")]
        [Tooltip("Settings asset defining sizes, offsets, colors, and motion. Loaded from Resources/DamageNumberSettings if null.")]
        [SerializeField] private DamageNumberSettings settings;
        public DamageNumberSettings Settings { get => settings; set => settings = value; }

        [Header("Pool Configuration")]
        [Tooltip("Initial number of damage text instances to prewarm in memory.")]
        [SerializeField] private int initialPoolSize = 35;
        [Tooltip("Hard cap on active pooled instances to avoid runaway allocations during shotgun bursts.")]
        [SerializeField] private int maxPoolCap = 75;

        [Header("Fallback Defaults (Used if Settings Asset is absent)")]
        [SerializeField] private float defaultFontSize = 5.2f;
        [SerializeField] private Color normalColor = new Color(1f, 0.96f, 0.65f, 1f);       // Bright warm white/yellow
        [SerializeField] private Color bossHeadColor = new Color(1f, 0.22f, 0.22f, 1f);     // Fiery red/crimson
        [SerializeField] private Color bossLegColor = new Color(1f, 0.62f, 0.05f, 1f);      // Vibrant amber/orange
        [SerializeField] private Color bossGeneralColor = new Color(1f, 0.85f, 0.25f, 1f);  // Boss body gold
        [SerializeField] private Color bossBrokenColor = new Color(0.72f, 0.72f, 0.72f, 0.9f); // Dim silver
        [SerializeField] private Color critColor = new Color(0.75f, 0.35f, 1.0f, 1f);        // Vibrant neon purple

        private Queue<DamageNumber> availablePool = new Queue<DamageNumber>();
        private List<DamageNumber> inUseList = new List<DamageNumber>();
        private Material sharedOverlayMaterial;
        private TMP_FontAsset activeFont;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadSettings();
            InitializeFontAndMaterial();
            PrewarmPool(initialPoolSize);
        }

        public void LoadSettings()
        {
            if (settings == null)
            {
                settings = Resources.Load<DamageNumberSettings>("DamageNumberSettings");
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<DamageNumberSettings>();
                }
            }
        }

        private void InitializeFontAndMaterial()
        {
            if (settings != null && settings.customFont != null)
            {
                activeFont = settings.customFont;
            }
            else
            {
                activeFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (activeFont == null)
                {
                    activeFont = TMP_Settings.defaultFontAsset;
                }
            }

            // Create a high-priority Overlay material so damage numbers render on top of 3D meshes without clipping
            Shader overlayShader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");
            if (overlayShader == null) overlayShader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (overlayShader == null && activeFont != null && activeFont.material != null)
            {
                overlayShader = activeFont.material.shader;
            }

            if (overlayShader != null && activeFont != null)
            {
                sharedOverlayMaterial = new Material(overlayShader);
                if (activeFont.atlasTexture != null)
                {
                    sharedOverlayMaterial.mainTexture = activeFont.atlasTexture;
                }
                // Crisp black outline for readability against light and dark backdrops
                sharedOverlayMaterial.SetColor("_OutlineColor", Color.black);
                sharedOverlayMaterial.SetFloat("_OutlineWidth", 0.22f);
                sharedOverlayMaterial.SetFloat("_OutlineSoftness", 0.1f);
            }
        }

        private void PrewarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                DamageNumber dn = CreateNewInstance();
                dn.gameObject.SetActive(false);
                availablePool.Enqueue(dn);
            }
        }

        private DamageNumber CreateNewInstance()
        {
            GameObject obj = new GameObject("DamageNumber");
            obj.transform.SetParent(this.transform);

            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            if (activeFont != null) tmp.font = activeFont;
            if (sharedOverlayMaterial != null) tmp.fontMaterial = sharedOverlayMaterial;

            tmp.fontSize = (settings != null) ? settings.baseFontSize : defaultFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = new Vector4(0, 0, 0, 0);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 500; // Render above world geometry
            }

            DamageNumber dn = obj.AddComponent<DamageNumber>();
            dn.Initialize(tmp);
            return dn;
        }

        public DamageNumber GetInstance()
        {
            DamageNumber dn = null;

            while (availablePool.Count > 0 && dn == null)
            {
                dn = availablePool.Dequeue();
            }

            if (dn == null)
            {
                if (inUseList.Count < maxPoolCap)
                {
                    dn = CreateNewInstance();
                }
                else
                {
                    // Recycle oldest active instance if cap is reached
                    dn = inUseList[0];
                    inUseList.RemoveAt(0);
                    dn.Stop();
                }
            }

            inUseList.Add(dn);
            return dn;
        }

        public void ReturnToPool(DamageNumber dn)
        {
            if (dn == null) return;
            inUseList.Remove(dn);
            if (!availablePool.Contains(dn))
            {
                availablePool.Enqueue(dn);
            }
        }

        /// <summary>
        /// Spawns a floating damage number for regular enemies above their head using configured height offset.
        /// </summary>
        public void SpawnEnemyDamage(float damage, Vector3 enemyRootPosition, bool isCrit = false)
        {
            if (damage <= 0.05f) return;

            float yOffset = (settings != null) ? settings.enemyHeadHeightOffset : 1.25f;
            float spread = (settings != null) ? settings.randomSpreadRadius : 0.25f;

            Vector3 spawnPos = enemyRootPosition + Vector3.up * yOffset;
            spawnPos += new Vector3(Random.Range(-spread, spread), Random.Range(0f, spread * 0.75f), 0f);

            string dmgStr = FormatDamageString(damage);
            string text = isCrit 
                ? $"<size=75%><color=#E9D5FF><b>CRIT!</b></color></size>\n<size=125%><b>{dmgStr}</b></size>" 
                : dmgStr;

            Color c = isCrit 
                ? ((settings != null) ? settings.critColor : critColor)
                : ((settings != null) ? settings.normalColor : normalColor);

            float scale = isCrit
                ? ((settings != null) ? settings.critScale : 1.55f)
                : ((settings != null) ? settings.normalScale : 1.0f);

            float dur = (settings != null) ? settings.duration : 0.70f;
            if (isCrit) dur *= 1.15f;
            float floatSpd = (settings != null) ? settings.floatUpSpeed : 1.8f;
            if (isCrit) floatSpd *= 1.15f;
            float driftSpd = (settings != null) ? settings.driftSpeed : 0.45f;

            DamageNumber dn = GetInstance();
            if (dn != null)
            {
                dn.Play(text, spawnPos, c, scale, dur, floatSpd, driftSpd);
            }
        }

        /// <summary>
        /// Spawns a floating damage number for the Boss at the specific body part or hit location.
        /// Formats text and color based on part type (Head, Left Leg, Right Leg, Broken) and critical strike.
        /// </summary>
        public void SpawnBossDamage(float damage, Vector3 hitPoint, BossBodyPartType partType, bool isBroken, bool isCrit = false, bool isArmored = false)
        {
            if (damage <= 0.05f) return;

            string dmgStr = FormatDamageString(damage);
            string formattedText;
            Color color;
            float scaleMult;

            Color pCritColor = (settings != null) ? settings.critColor : critColor;
            float pCritScale = (settings != null) ? settings.critScale : 1.55f;

            if (isArmored)
            {
                if (isCrit)
                {
                    formattedText = $"<size=70%><color=#94A3B8><b>ARMOR [CRIT]</b></color></size>\n<b>{dmgStr}</b>";
                    color = pCritColor * 0.85f;
                    scaleMult = (settings != null ? settings.bossBrokenScale : 0.95f) * 1.1f;
                }
                else
                {
                    formattedText = $"<size=70%><color=#94A3B8><b>ARMOR</b></color></size>\n{dmgStr}";
                    color = new Color(0.60f, 0.65f, 0.72f);
                    scaleMult = (settings != null ? settings.bossBrokenScale : 0.95f);
                }
            }
            else if (isCrit)
            {
                color = pCritColor;
                if (isBroken)
                {
                    formattedText = $"<size=70%><color=#E9D5FF><b>CRIT BROKEN</b></color></size>\n<b>{dmgStr}</b>";
                    scaleMult = (settings != null ? settings.bossBrokenScale : 0.95f) * 1.15f;
                }
                else
                {
                    switch (partType)
                    {
                        case BossBodyPartType.Head:
                            formattedText = $"<size=80%><color=#E9D5FF><b>CRIT WEAKPOINT!</b></color></size>\n<size=140%><b>{dmgStr}</b></size>";
                            scaleMult = (settings != null ? settings.bossHeadScale : 1.45f) * 1.3f;
                            break;
                        case BossBodyPartType.LeftLeg:
                            formattedText = $"<size=75%><color=#E9D5FF><b>CRIT L-LEG!</b></color></size>\n<size=125%><b>{dmgStr}</b></size>";
                            scaleMult = (settings != null ? settings.bossLegScale : 1.25f) * 1.25f;
                            break;
                        case BossBodyPartType.RightLeg:
                            formattedText = $"<size=75%><color=#E9D5FF><b>CRIT R-LEG!</b></color></size>\n<size=125%><b>{dmgStr}</b></size>";
                            scaleMult = (settings != null ? settings.bossLegScale : 1.25f) * 1.25f;
                            break;
                        default:
                            formattedText = $"<size=75%><color=#E9D5FF><b>CRIT!</b></color></size>\n<size=125%><b>{dmgStr}</b></size>";
                            scaleMult = pCritScale;
                            break;
                    }
                }
            }
            else
            {
                if (isBroken)
                {
                    formattedText = $"<size=70%><color=#888888>BROKEN</color></size>\n{dmgStr}";
                    color = (settings != null) ? settings.bossBrokenColor : bossBrokenColor;
                    scaleMult = (settings != null) ? settings.bossBrokenScale : 0.95f;
                }
                else
                {
                    switch (partType)
                    {
                        case BossBodyPartType.Head:
                            formattedText = $"<size=75%><color=#FFCC00><b>WEAKPOINT!</b></color></size>\n<size=130%><b>{dmgStr}</b></size>";
                            color = (settings != null) ? settings.bossHeadColor : bossHeadColor;
                            scaleMult = (settings != null) ? settings.bossHeadScale : 1.45f;
                            break;
                        case BossBodyPartType.LeftLeg:
                            formattedText = $"<size=75%><color=#FFA500><b>L-LEG</b></color></size>\n<b>{dmgStr}</b>";
                            color = (settings != null) ? settings.bossLegColor : bossLegColor;
                            scaleMult = (settings != null) ? settings.bossLegScale : 1.25f;
                            break;
                        case BossBodyPartType.RightLeg:
                            formattedText = $"<size=75%><color=#FFA500><b>R-LEG</b></color></size>\n<b>{dmgStr}</b>";
                            color = (settings != null) ? settings.bossLegColor : bossLegColor;
                            scaleMult = (settings != null) ? settings.bossLegScale : 1.25f;
                            break;
                        default:
                            formattedText = $"<b>{dmgStr}</b>";
                            color = (settings != null) ? settings.bossGeneralColor : bossGeneralColor;
                            scaleMult = (settings != null) ? settings.bossGeneralScale : 1.15f;
                            break;
                    }
                }
            }

            float duration = (settings != null) ? settings.duration : 0.75f;
            if (isCrit) duration *= 1.2f;
            float floatSpd = (settings != null) ? settings.floatUpSpeed : 1.8f;
            if (isCrit) floatSpd *= 1.15f;
            float driftSpd = (settings != null) ? settings.driftSpeed : 0.45f;
            float camOffset = (settings != null) ? settings.bossCameraOffset : 0.25f;
            float vertOffset = (settings != null) ? settings.bossVerticalOffset : 0.20f;
            float spread = (settings != null) ? settings.randomSpreadRadius : 0.25f;

            Vector3 cameraDir = Vector3.back;
            if (Camera.main != null)
            {
                cameraDir = (Camera.main.transform.position - hitPoint).normalized;
            }
            Vector3 spawnPos = hitPoint + cameraDir * camOffset + Vector3.up * vertOffset;
            spawnPos += new Vector3(Random.Range(-spread, spread), Random.Range(-spread * 0.5f, spread * 0.5f), 0f);

            DamageNumber dn = GetInstance();
            if (dn != null)
            {
                dn.Play(formattedText, spawnPos, color, scaleMult, duration, floatSpd, driftSpd);
            }
        }

        /// <summary>
        /// General damage spawner supporting customizable styles, scale, and labels.
        /// </summary>
        public void SpawnDamage(float damage, Vector3 worldPos, DamageStyle style, string customLabel = null)
        {
            if (damage <= 0.05f) return;

            string dmgStr = FormatDamageString(damage);
            string formattedText = dmgStr;

            Color color = (settings != null) ? settings.normalColor : normalColor;
            float scaleMult = (settings != null) ? settings.normalScale : 1.0f;
            float duration = (settings != null) ? settings.duration : 0.75f;
            float floatSpd = (settings != null) ? settings.floatUpSpeed : 1.8f;
            float driftSpd = (settings != null) ? settings.driftSpeed : 0.45f;
            float camOffset = (settings != null) ? settings.bossCameraOffset : 0.25f;
            float vertOffset = (settings != null) ? settings.bossVerticalOffset : 0.20f;
            float spread = (settings != null) ? settings.randomSpreadRadius : 0.25f;

            // Offset slightly toward camera and upwards so it floats cleanly in front of hit mesh
            Vector3 cameraDir = Vector3.back;
            if (Camera.main != null)
            {
                cameraDir = (Camera.main.transform.position - worldPos).normalized;
            }
            Vector3 spawnPos = worldPos + cameraDir * camOffset + Vector3.up * vertOffset;
            spawnPos += new Vector3(Random.Range(-spread, spread), Random.Range(-spread * 0.5f, spread * 0.5f), 0f);

            switch (style)
            {
                case DamageStyle.BossHead:
                    formattedText = $"<size=75%><color=#FFCC00><b>HEAD</b></color></size>\n<size=125%><b>{dmgStr}</b></size>";
                    color = (settings != null) ? settings.bossHeadColor : bossHeadColor;
                    scaleMult = (settings != null) ? settings.bossHeadScale : 1.45f;
                    duration = (settings != null) ? settings.duration * 1.15f : 0.85f;
                    break;

                case DamageStyle.BossLeftLeg:
                    formattedText = $"<size=75%><color=#FFA500><b>L-LEG</b></color></size>\n<b>{dmgStr}</b>";
                    color = (settings != null) ? settings.bossLegColor : bossLegColor;
                    scaleMult = (settings != null) ? settings.bossLegScale : 1.25f;
                    duration = (settings != null) ? settings.duration * 1.05f : 0.78f;
                    break;

                case DamageStyle.BossRightLeg:
                    formattedText = $"<size=75%><color=#FFA500><b>R-LEG</b></color></size>\n<b>{dmgStr}</b>";
                    color = (settings != null) ? settings.bossLegColor : bossLegColor;
                    scaleMult = (settings != null) ? settings.bossLegScale : 1.25f;
                    duration = (settings != null) ? settings.duration * 1.05f : 0.78f;
                    break;

                case DamageStyle.BossBroken:
                    formattedText = $"<size=70%><color=#888888>BROKEN</color></size>\n{dmgStr}";
                    color = (settings != null) ? settings.bossBrokenColor : bossBrokenColor;
                    scaleMult = (settings != null) ? settings.bossBrokenScale : 0.95f;
                    duration = (settings != null) ? settings.duration * 0.9f : 0.65f;
                    break;

                case DamageStyle.BossGeneral:
                    formattedText = $"<b>{dmgStr}</b>";
                    color = (settings != null) ? settings.bossGeneralColor : bossGeneralColor;
                    scaleMult = (settings != null) ? settings.bossGeneralScale : 1.15f;
                    duration = (settings != null) ? settings.duration : 0.72f;
                    break;

                case DamageStyle.Critical:
                    formattedText = $"<size=75%><color=#E9D5FF><b>CRIT!</b></color></size>\n<size=125%><b>{dmgStr}</b></size>";
                    color = (settings != null) ? settings.critColor : critColor;
                    scaleMult = (settings != null) ? settings.critScale : 1.55f;
                    duration = (settings != null) ? settings.duration * 1.2f : 0.85f;
                    break;

                case DamageStyle.Normal:
                default:
                    if (!string.IsNullOrEmpty(customLabel))
                    {
                        formattedText = $"{customLabel}\n{dmgStr}";
                    }
                    else
                    {
                        formattedText = dmgStr;
                    }
                    color = (settings != null) ? settings.normalColor : normalColor;
                    scaleMult = (settings != null) ? settings.normalScale : 1.0f;
                    duration = (settings != null) ? settings.duration : 0.70f;
                    break;
            }

            DamageNumber dn = GetInstance();
            if (dn != null)
            {
                dn.Play(formattedText, spawnPos, color, scaleMult, duration, floatSpd, driftSpd);
            }
        }

        private string FormatDamageString(float damage)
        {
            if (damage < 10f && (damage % 1f) > 0.05f)
            {
                return damage.ToString("F1");
            }
            return Mathf.RoundToInt(damage).ToString();
        }
    }
}
