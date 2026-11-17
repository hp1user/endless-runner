using UnityEngine;
using UnityEngine.UIElements;
using Player.Control;
using System.Collections.Generic;

public class WeaponWheelToolkitManager : MonoBehaviour
{
    private static WeaponWheelToolkitManager _instance;
    public static WeaponWheelToolkitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<WeaponWheelToolkitManager>(FindObjectsInactive.Include);
                if (_instance != null)
                {
                    if (!_instance.gameObject.activeInHierarchy)
                    {
                        if (_instance.transform.parent != null && !_instance.transform.parent.gameObject.activeSelf)
                        {
                            _instance.transform.parent.gameObject.SetActive(true);
                        }
                        _instance.gameObject.SetActive(true);
                    }
                    _instance.EnsureInitialized();
                }
            }
            return _instance;
        }
        set => _instance = value;
    }

    [Header("UI Document")]
    public UIDocument document;

    [Header("Modular Slices - Normal State")]
    public Texture2D weapon01Normal;
    public Texture2D weapon02Normal;
    public Texture2D weapon03Normal;
    public Texture2D weapon04Normal;
    public Texture2D skillNormal;
    public Texture2D ultimateNormal;
    public Texture2D centerPlate;

    [Header("Modular Slices - Selected/Glowing State")]
    public Texture2D weapon01Selected;
    public Texture2D weapon02Selected;
    public Texture2D weapon03Selected;
    public Texture2D weapon04Selected;
    public Texture2D skillSelected;
    public Texture2D ultimateSelected;

    [Header("Default Fallback Weapon Icons")]
    public Texture2D defaultPistolIcon;
    public Texture2D defaultARIcon;
    public Texture2D defaultShotgunIcon;
    public Texture2D defaultHeavyIcon;

    // Visual Elements
    private VisualElement wheelContainer;
    private VisualElement wheelRoot;
    private VisualElement cooldownLayer;

    // Slice Elements (User Sprite Sheet Layers)
    private VisualElement sliceSlot1;
    private VisualElement sliceSlot2;
    private VisualElement sliceSlot3;
    private VisualElement sliceSlot4;
    private VisualElement sliceSkill;
    private VisualElement sliceUltimate;
    private VisualElement sliceCenter;

    // Slot Icons (1: Top Left - Pistol, 2: Top Center-Left - AR, 3: Top Center-Right - Shotgun, 4: Mid Right - Heavy)
    private VisualElement iconSlot1;
    private VisualElement iconSlot2;
    private VisualElement iconSlot3;
    private VisualElement iconSlot4;
    private VisualElement pipsSlot1;
    private VisualElement pipsSlot2;
    private VisualElement pipsSlot3;
    private VisualElement pipsSlot4;
    private Label skillSubtext;
    private Label ultimateSubtext;

    // Center Info Hub
    private VisualElement centerHub;
    private VisualElement centerHeroIcon;
    private Label centerTitle;
    private Label centerDesc;

    // Floating Sci-Fi Mini-Dock Popup
    private VisualElement miniDockContainer;
    private VisualElement miniDockChassis;

    // Selected Slot Index:
    // 0: Slot 1 (Top Left - Pistol: 270° – 315°)
    // 1: Slot 2 (Top Center-Left - AR / SMG / Sniper: 315° – 360°)
    // 2: Slot 3 (Top Center-Right - Shotgun: 0° – 45°)
    // 3: Slot 4 (Mid Right - Minigun / Heavy: 45° – 90°)
    // 4: Ultimate (Bottom Right: 90° – 180°)
    // 5: Skill (Bottom Left: 180° – 270°)
    private int selectedIndex = -1;
    private WeaponData[] weaponSlots = new WeaponData[4];
    private List<WeaponData>[] slotWeapons = new List<WeaponData>[4]
    {
        new List<WeaponData>(),
        new List<WeaponData>(),
        new List<WeaponData>(),
        new List<WeaponData>()
    };
    private int[] activeWeaponIndices = new int[4];
    private bool isInitialized = false;

    public bool IsOpen => wheelContainer != null && wheelContainer.style.display == DisplayStyle.Flex;

    private void Awake()
    {
        _instance = this;
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (isInitialized && wheelContainer != null) return;

        if (document == null) document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null) return;

        var root = document.rootVisualElement;
        wheelContainer = root.Q<VisualElement>("wheel-container");
        wheelRoot = root.Q<VisualElement>("wheel-root");
        cooldownLayer = root.Q<VisualElement>("cooldown-layer");

        sliceSlot1 = root.Q<VisualElement>("slice-slot-1");
        sliceSlot2 = root.Q<VisualElement>("slice-slot-2");
        sliceSlot3 = root.Q<VisualElement>("slice-slot-3");
        sliceSlot4 = root.Q<VisualElement>("slice-slot-4");
        sliceSkill = root.Q<VisualElement>("slice-skill");
        sliceUltimate = root.Q<VisualElement>("slice-ultimate");
        sliceCenter = root.Q<VisualElement>("slice-center");

        iconSlot1 = root.Q<VisualElement>("icon-1");
        iconSlot2 = root.Q<VisualElement>("icon-2");
        iconSlot3 = root.Q<VisualElement>("icon-3");
        iconSlot4 = root.Q<VisualElement>("icon-4");

        pipsSlot1 = root.Q<VisualElement>("pips-1");
        pipsSlot2 = root.Q<VisualElement>("pips-2");
        pipsSlot3 = root.Q<VisualElement>("pips-3");
        pipsSlot4 = root.Q<VisualElement>("pips-4");

        miniDockContainer = root.Q<VisualElement>("mini-dock-container");
        miniDockChassis = root.Q<VisualElement>("mini-dock-chassis");

        skillSubtext = root.Q<Label>("skill-subtext");
        ultimateSubtext = root.Q<Label>("ultimate-subtext");

        centerHub = root.Q<VisualElement>("center-hub");
        centerHeroIcon = root.Q<VisualElement>("center-hero-icon");
        centerTitle = root.Q<Label>("center-title");
        centerDesc = root.Q<Label>("center-desc");

        LoadSpritesIfNeeded();

        // Assign initial textures
        if (sliceCenter != null && centerPlate != null)
            sliceCenter.style.backgroundImage = new StyleBackground(centerPlate);

        UpdateSliceVisuals();

        // Initialize weapon icons
        for (int i = 0; i < 4; i++)
        {
            UpdateSlotUI(i);
        }

        UpdateSkillAndUltimateLabels();

        if (cooldownLayer != null)
        {
            cooldownLayer.generateVisualContent -= DrawCooldownSlices;
            cooldownLayer.generateVisualContent += DrawCooldownSlices;
        }

        if (wheelContainer != null)
        {
            wheelContainer.style.display = DisplayStyle.None;
        }

        isInitialized = true;
    }

    private void LoadSpritesIfNeeded()
    {
        // 1. Runtime load from Resources (Ensures textures load on Android builds & Standalone)
        if (weapon01Normal == null) weapon01Normal = Resources.Load<Texture2D>("WeaponWheel/weapon_01_normal");
        if (weapon01Selected == null) weapon01Selected = Resources.Load<Texture2D>("WeaponWheel/weapon_01_selected");
        if (weapon02Normal == null) weapon02Normal = Resources.Load<Texture2D>("WeaponWheel/weapon_02_normal");
        if (weapon02Selected == null) weapon02Selected = Resources.Load<Texture2D>("WeaponWheel/weapon_02_selected");
        if (weapon03Normal == null) weapon03Normal = Resources.Load<Texture2D>("WeaponWheel/weapon_03_normal");
        if (weapon03Selected == null) weapon03Selected = Resources.Load<Texture2D>("WeaponWheel/weapon_03_selected");
        if (weapon04Normal == null) weapon04Normal = Resources.Load<Texture2D>("WeaponWheel/weapon_04_normal");
        if (weapon04Selected == null) weapon04Selected = Resources.Load<Texture2D>("WeaponWheel/weapon_04_selected");
        if (skillNormal == null) skillNormal = Resources.Load<Texture2D>("WeaponWheel/skill_normal");
        if (skillSelected == null) skillSelected = Resources.Load<Texture2D>("WeaponWheel/skill_selected");
        if (ultimateNormal == null) ultimateNormal = Resources.Load<Texture2D>("WeaponWheel/ultimate_normal");
        if (ultimateSelected == null) ultimateSelected = Resources.Load<Texture2D>("WeaponWheel/ultimate_selected");
        if (centerPlate == null) centerPlate = Resources.Load<Texture2D>("WeaponWheel/center_plate");

#if UNITY_EDITOR
        // 2. Editor AssetDatabase fallback
        string folder = "Assets/UI/Sprite/WeaponWheel/";
        if (weapon01Normal == null) weapon01Normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_01_normal.png");
        if (weapon01Selected == null) weapon01Selected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_01_selected.png");
        if (weapon02Normal == null) weapon02Normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_02_normal.png");
        if (weapon02Selected == null) weapon02Selected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_02_selected.png");
        if (weapon03Normal == null) weapon03Normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_03_normal.png");
        if (weapon03Selected == null) weapon03Selected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_03_selected.png");
        if (weapon04Normal == null) weapon04Normal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_04_normal.png");
        if (weapon04Selected == null) weapon04Selected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "weapon_04_selected.png");
        if (skillNormal == null) skillNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "skill_normal.png");
        if (skillSelected == null) skillSelected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "skill_selected.png");
        if (ultimateNormal == null) ultimateNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "ultimate_normal.png");
        if (ultimateSelected == null) ultimateSelected = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "ultimate_selected.png");
        if (centerPlate == null) centerPlate = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "center_plate.png");

        if (defaultPistolIcon == null) defaultPistolIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprite/WeaponIcon/pistol.png");
        if (defaultARIcon == null) defaultARIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprite/WeaponIcon/assault-rifle.png");
        if (defaultShotgunIcon == null) defaultShotgunIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprite/WeaponIcon/shotgun.png");
        if (defaultHeavyIcon == null) defaultHeavyIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprite/WeaponIcon/machine-gun.png");
#endif
    }

    private void Start()
    {
        SyncUnlockedWeapons();
    }

    public void SyncUnlockedWeapons()
    {
        for (int i = 0; i < 4; i++)
        {
            slotWeapons[i].Clear();
            weaponSlots[i] = null;
        }

        if (PlayerController.Instance != null)
        {
            var unlocked = PlayerController.Instance.GetUnlockedWeapons();
            if (unlocked != null)
            {
                foreach (var weapon in unlocked)
                {
                    if (weapon == null) continue;
                    int targetSlot = GetSlotIndexForCategory(weapon.category);
                    if (targetSlot >= 0 && targetSlot < 4)
                    {
                        if (!slotWeapons[targetSlot].Contains(weapon))
                        {
                            slotWeapons[targetSlot].Add(weapon);
                        }
                    }
                }
            }

            // Sync active index with currently equipped weapon if applicable
            WeaponData equipped = PlayerController.Instance.CurrentWeaponData;
            if (equipped != null)
            {
                int eqSlot = GetSlotIndexForCategory(equipped.category);
                if (eqSlot >= 0 && eqSlot < 4)
                {
                    int idx = slotWeapons[eqSlot].IndexOf(equipped);
                    if (idx >= 0)
                    {
                        activeWeaponIndices[eqSlot] = idx;
                    }
                }
            }
        }

        // Clamp indices and sync weaponSlots
        for (int i = 0; i < 4; i++)
        {
            if (slotWeapons[i].Count == 0) activeWeaponIndices[i] = 0;
            else activeWeaponIndices[i] = Mathf.Clamp(activeWeaponIndices[i], 0, slotWeapons[i].Count - 1);
            weaponSlots[i] = GetActiveWeaponForSlot(i);
            UpdateSlotUI(i);
        }

        UpdateSliceVisuals();
    }

    public static int GetSlotIndexForCategory(WeaponCategory category)
    {
        switch (category)
        {
            case WeaponCategory.Pistol: return 0; // Slot 1: Sidearm (Top-Left)
            case WeaponCategory.AssaultRifle:
            case WeaponCategory.SMG:
            case WeaponCategory.Sniper:
                return 1; // Slot 2: Rapid / Rifles (Top Center-Left)
            case WeaponCategory.Shotgun:
                return 2; // Slot 3: CQB / Shotguns (Top Center-Right)
            case WeaponCategory.Minigun:
            case WeaponCategory.RocketLauncher:
            case WeaponCategory.Grenade:
                return 3; // Slot 4: Heavy Weapons (Mid-Right)
            default:
                return 1;
        }
    }

    public void AddWeaponToWheel(WeaponData newWeapon)
    {
        if (newWeapon == null) return;
        int targetSlot = GetSlotIndexForCategory(newWeapon.category);
        if (targetSlot >= 0 && targetSlot < 4)
        {
            if (!slotWeapons[targetSlot].Contains(newWeapon))
            {
                slotWeapons[targetSlot].Add(newWeapon);
            }
            activeWeaponIndices[targetSlot] = slotWeapons[targetSlot].IndexOf(newWeapon);
            weaponSlots[targetSlot] = newWeapon;
            UpdateSlotUI(targetSlot);
            UpdateSliceVisuals();
        }
    }

    public WeaponData GetActiveWeaponForSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        if (slotWeapons[slotIndex].Count == 0) return null;
        int activeIdx = Mathf.Clamp(activeWeaponIndices[slotIndex], 0, slotWeapons[slotIndex].Count - 1);
        return slotWeapons[slotIndex][activeIdx];
    }

    public void SelectWeaponInSlot(int slotIndex, int weaponIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        if (weaponIndex < 0 || weaponIndex >= slotWeapons[slotIndex].Count) return;

        if (activeWeaponIndices[slotIndex] != weaponIndex)
        {
            activeWeaponIndices[slotIndex] = weaponIndex;
            weaponSlots[slotIndex] = GetActiveWeaponForSlot(slotIndex);
            EndlessRunner.Audio.AudioManager.Instance?.PlayUISound(EndlessRunner.Audio.UISoundType.WeaponWheelSelect);
        }

        UpdateSlotUI(slotIndex);
        UpdateCenterHub(slotIndex);
        UpdateMiniDock(slotIndex);
    }

    private void UpdateSlotUI(int index)
    {
        VisualElement targetIconElement = null;
        VisualElement targetPipsElement = null;

        switch (index)
        {
            case 0: targetIconElement = iconSlot1; targetPipsElement = pipsSlot1; break;
            case 1: targetIconElement = iconSlot2; targetPipsElement = pipsSlot2; break;
            case 2: targetIconElement = iconSlot3; targetPipsElement = pipsSlot3; break;
            case 3: targetIconElement = iconSlot4; targetPipsElement = pipsSlot4; break;
        }

        WeaponData activeWeapon = GetActiveWeaponForSlot(index);

        if (targetIconElement != null)
        {
            if (activeWeapon != null && activeWeapon.icon != null)
            {
                targetIconElement.style.backgroundImage = new StyleBackground(activeWeapon.icon);
                targetIconElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                targetIconElement.style.backgroundImage = null;
                targetIconElement.style.display = DisplayStyle.None;
            }
        }

        if (targetPipsElement != null)
        {
            targetPipsElement.Clear();
            int count = slotWeapons[index].Count;
            if (count > 1)
            {
                targetPipsElement.style.display = DisplayStyle.Flex;
                int activeIdx = Mathf.Clamp(activeWeaponIndices[index], 0, count - 1);
                for (int p = 0; p < count; p++)
                {
                    VisualElement dot = new VisualElement();
                    dot.AddToClassList("slot-pip-dot");
                    if (p == activeIdx) dot.AddToClassList("slot-pip-dot-active");
                    targetPipsElement.Add(dot);
                }
            }
            else
            {
                targetPipsElement.style.display = DisplayStyle.None;
            }
        }
    }

    public void UpdateSkillAndUltimateLabels()
    {
        if (PlayerController.Instance != null)
        {
            if (PlayerController.Instance.equippedSkill != null && skillSubtext != null)
            {
                skillSubtext.text = PlayerController.Instance.equippedSkill.skillName;
            }
            if (PlayerController.Instance.equippedUltimate != null && ultimateSubtext != null)
            {
                ultimateSubtext.text = PlayerController.Instance.equippedUltimate.skillName;
            }
        }
    }

    public void OpenWheel()
    {
        EnsureInitialized();
        if (wheelContainer == null)
        {
            Debug.LogWarning("<color=yellow>[WeaponWheelToolkit]</color> wheelContainer not found in UIDocument!");
            return;
        }

        SyncUnlockedWeapons();
        UpdateSkillAndUltimateLabels();

        wheelContainer.style.display = DisplayStyle.Flex;
        Time.timeScale = 0.1f;
        EndlessRunner.Audio.AudioManager.Instance?.PlayUISound(EndlessRunner.Audio.UISoundType.WeaponWheelOpen);

        // Default hovered slot: currently equipped weapon
        int currentEquippedSlot = -1;
        if (PlayerController.Instance != null && PlayerController.Instance.CurrentWeaponData != null)
        {
            currentEquippedSlot = GetSlotIndexForCategory(PlayerController.Instance.CurrentWeaponData.category);
        }

        selectedIndex = currentEquippedSlot;
        UpdateSliceVisuals();
        UpdateCenterHub(selectedIndex);
        UpdateMiniDock(selectedIndex);

        if (cooldownLayer != null) cooldownLayer.MarkDirtyRepaint();
    }

    public void CloseWheel()
    {
        if (wheelContainer == null) return;
        wheelContainer.style.display = DisplayStyle.None;
        if (miniDockContainer != null) miniDockContainer.style.display = DisplayStyle.None;

        Time.timeScale = 1.0f;
        EndlessRunner.Audio.AudioManager.Instance?.PlayUISound(EndlessRunner.Audio.UISoundType.WeaponWheelClose);

        if (selectedIndex != -1)
        {
            EquipSelected();
        }
    }

    private void EquipSelected()
    {
        if (PlayerController.Instance == null) return;

        if (selectedIndex == 4)
        {
            PlayerController.Instance.ActivateUltimate();
        }
        else if (selectedIndex == 5)
        {
            PlayerController.Instance.ActivateSkill();
        }
        else if (selectedIndex >= 0 && selectedIndex <= 3)
        {
            WeaponData chosen = GetActiveWeaponForSlot(selectedIndex);
            if (chosen != null)
            {
                PlayerController.Instance.EquipWeaponFromWheel(chosen);
            }
        }
    }

    private void Update()
    {
        if (wheelContainer == null || wheelContainer.style.display == DisplayStyle.None) return;

        if (cooldownLayer != null) cooldownLayer.MarkDirtyRepaint();

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = TouchManager.CurrentTouchPosition - screenCenter;

        if (dir.magnitude > 35f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float clockwiseAngle = 90f - angle;
            if (clockwiseAngle < 0f) clockwiseAngle += 360f;

            int oldIndex = selectedIndex;

            // Mathematical sector mapping matching user's custom sprite sheet:
            // Slot 1 (Pistol): 270° – 315° (Top Left, 45° span)
            // Slot 2 (Assault Rifle / SMG / Sniper): 315° – 360° (Top Center-Left, 45° span)
            // Slot 3 (Shotgun): 0° – 45° (Top Center-Right, 45° span)
            // Slot 4 (Minigun / Heavy): 45° – 90° (Mid Right, 45° span)
            // Ultimate: 90° – 180° (Bottom Right, 90° span)
            // Skill: 180° – 270° (Bottom Left, 90° span)
            if (clockwiseAngle >= 270f && clockwiseAngle < 315f)
                selectedIndex = 0; // Slot 1: Pistol
            else if (clockwiseAngle >= 315f && clockwiseAngle <= 360f)
                selectedIndex = 1; // Slot 2: Assault Rifle / SMG / Sniper
            else if (clockwiseAngle >= 0f && clockwiseAngle < 45f)
                selectedIndex = 2; // Slot 3: Shotgun
            else if (clockwiseAngle >= 45f && clockwiseAngle < 90f)
                selectedIndex = 3; // Slot 4: Minigun / Heavy
            else if (clockwiseAngle >= 90f && clockwiseAngle < 180f)
                selectedIndex = 4; // Ultimate
            else if (clockwiseAngle >= 180f && clockwiseAngle < 270f)
                selectedIndex = 5; // Skill

            if (oldIndex != selectedIndex)
            {
                UpdateSliceVisuals();
                UpdateCenterHub(selectedIndex);
                UpdateMiniDock(selectedIndex);
            }
            else if (selectedIndex >= 0 && selectedIndex <= 3 && slotWeapons[selectedIndex].Count > 1 && dir.magnitude > 175f)
            {
                // Sub-sector radial drag selection for multi-weapon slots
                float startAng = 0f;
                float spanAng = 45f;
                if (selectedIndex == 0) startAng = 270f;
                else if (selectedIndex == 1) startAng = 315f;
                else if (selectedIndex == 2) startAng = 0f;
                else if (selectedIndex == 3) startAng = 45f;

                int count = slotWeapons[selectedIndex].Count;
                float step = spanAng / count;
                float rel = clockwiseAngle - startAng;
                if (rel < 0f) rel += 360f;
                int subIdx = Mathf.Clamp((int)(rel / step), 0, count - 1);
                if (subIdx != activeWeaponIndices[selectedIndex])
                {
                    SelectWeaponInSlot(selectedIndex, subIdx);
                }
            }
        }

        HandleScrollInput();
    }

    private void HandleScrollInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.1f && selectedIndex >= 0 && selectedIndex <= 3 && slotWeapons[selectedIndex].Count > 1)
            {
                int count = slotWeapons[selectedIndex].Count;
                int scrollDir = scroll > 0 ? -1 : 1;
                int newIdx = (activeWeaponIndices[selectedIndex] + scrollDir + count) % count;
                SelectWeaponInSlot(selectedIndex, newIdx);
            }
        }
#endif
    }

    private void UpdateSliceVisuals()
    {
        // Slot 1 (Pistol)
        if (sliceSlot1 != null && weapon01Normal != null && weapon01Selected != null)
        {
            bool isUnlocked = slotWeapons[0].Count > 0;
            sliceSlot1.style.backgroundImage = new StyleBackground((selectedIndex == 0 && isUnlocked) ? weapon01Selected : weapon01Normal);
            sliceSlot1.style.opacity = isUnlocked ? 1.0f : 0.35f;
        }

        // Slot 2 (AR / SMG / Sniper)
        if (sliceSlot2 != null && weapon02Normal != null && weapon02Selected != null)
        {
            bool isUnlocked = slotWeapons[1].Count > 0;
            sliceSlot2.style.backgroundImage = new StyleBackground((selectedIndex == 1 && isUnlocked) ? weapon02Selected : weapon02Normal);
            sliceSlot2.style.opacity = isUnlocked ? 1.0f : 0.35f;
        }

        // Slot 3 (Shotgun)
        if (sliceSlot3 != null && weapon03Normal != null && weapon03Selected != null)
        {
            bool isUnlocked = slotWeapons[2].Count > 0;
            sliceSlot3.style.backgroundImage = new StyleBackground((selectedIndex == 2 && isUnlocked) ? weapon03Selected : weapon03Normal);
            sliceSlot3.style.opacity = isUnlocked ? 1.0f : 0.35f;
        }

        // Slot 4 (Minigun / Heavy)
        if (sliceSlot4 != null && weapon04Normal != null && weapon04Selected != null)
        {
            bool isUnlocked = slotWeapons[3].Count > 0;
            sliceSlot4.style.backgroundImage = new StyleBackground((selectedIndex == 3 && isUnlocked) ? weapon04Selected : weapon04Normal);
            sliceSlot4.style.opacity = isUnlocked ? 1.0f : 0.35f;
        }

        if (sliceUltimate != null && ultimateNormal != null && ultimateSelected != null)
            sliceUltimate.style.backgroundImage = new StyleBackground(selectedIndex == 4 ? ultimateSelected : ultimateNormal);

        if (sliceSkill != null && skillNormal != null && skillSelected != null)
            sliceSkill.style.backgroundImage = new StyleBackground(selectedIndex == 5 ? skillSelected : skillNormal);
    }

    private void UpdateMiniDock(int slotIndex)
    {
        if (miniDockContainer == null || miniDockChassis == null) return;

        if (slotIndex < 0 || slotIndex >= 4 || slotWeapons[slotIndex].Count <= 1)
        {
            miniDockContainer.style.display = DisplayStyle.None;
            return;
        }

        miniDockContainer.RemoveFromClassList("dock-pos-0");
        miniDockContainer.RemoveFromClassList("dock-pos-1");
        miniDockContainer.RemoveFromClassList("dock-pos-2");
        miniDockContainer.RemoveFromClassList("dock-pos-3");
        miniDockContainer.AddToClassList($"dock-pos-{slotIndex}");

        miniDockChassis.Clear();

        var weaponsInSlot = slotWeapons[slotIndex];
        int activeIdx = Mathf.Clamp(activeWeaponIndices[slotIndex], 0, weaponsInSlot.Count - 1);

        for (int i = 0; i < weaponsInSlot.Count; i++)
        {
            int capturedSlot = slotIndex;
            int capturedIdx = i;
            var w = weaponsInSlot[i];
            bool isActive = (i == activeIdx);

            VisualElement chip = new VisualElement();
            chip.AddToClassList("mini-dock-chip");
            if (isActive) chip.AddToClassList("mini-dock-chip-active");

            // Weapon icon
            VisualElement chipIcon = new VisualElement();
            chipIcon.AddToClassList("mini-dock-chip-icon");
            if (w.icon != null) chipIcon.style.backgroundImage = new StyleBackground(w.icon);
            chip.Add(chipIcon);

            // Weapon Label
            Label chipLabel = new Label(w.weaponName.ToUpper());
            chipLabel.AddToClassList("mini-dock-chip-label");
            if (isActive) chipLabel.AddToClassList("mini-dock-chip-label-active");
            chip.Add(chipLabel);

            // Ammo readout
            int reserve = PlayerController.Instance != null ? PlayerController.Instance.GetReserveAmmo(w.category) : 0;
            Label chipAmmo = new Label(w.category == WeaponCategory.Pistol ? "∞" : $"{reserve}");
            chipAmmo.AddToClassList("mini-dock-chip-ammo");
            if (isActive) chipAmmo.AddToClassList("mini-dock-chip-ammo-active");
            chip.Add(chipAmmo);

            // Pointer/Click callbacks
            chip.RegisterCallback<PointerDownEvent>(evt => {
                SelectWeaponInSlot(capturedSlot, capturedIdx);
                evt.StopPropagation();
            });
            chip.RegisterCallback<ClickEvent>(evt => {
                SelectWeaponInSlot(capturedSlot, capturedIdx);
                evt.StopPropagation();
            });

            miniDockChassis.Add(chip);
        }

        miniDockContainer.style.display = DisplayStyle.Flex;
    }

    private void UpdateCenterHub(int index)
    {
        if (centerTitle == null || centerDesc == null) return;

        if (index == 4) // Ultimate (Bottom Right)
        {
            string ultName = "Ultimate";
            if (PlayerController.Instance != null && PlayerController.Instance.equippedUltimate != null)
            {
                ultName = PlayerController.Instance.equippedUltimate.skillName;
                centerDesc.text = PlayerController.Instance.equippedUltimate.description;
            }
            else
            {
                centerDesc.text = "Meteor Strike";
            }
            centerTitle.text = ultName;
            centerTitle.style.color = new StyleColor(new Color(1f, 0.53f, 0.33f)); // Fiery Orange matching HUD
            if (centerHeroIcon != null) centerHeroIcon.style.display = DisplayStyle.None;
        }
        else if (index == 5) // Skill (Bottom Left)
        {
            string skillName = "Skill";
            if (PlayerController.Instance != null && PlayerController.Instance.equippedSkill != null)
            {
                skillName = PlayerController.Instance.equippedSkill.skillName;
                centerDesc.text = PlayerController.Instance.equippedSkill.description;
            }
            else
            {
                centerDesc.text = "Dash Forward";
            }
            centerTitle.text = skillName;
            centerTitle.style.color = new StyleColor(new Color(0.42f, 0.91f, 1f)); // Electric Cyan matching HUD
            if (centerHeroIcon != null) centerHeroIcon.style.display = DisplayStyle.None;
        }
        else if (index >= 0 && index <= 3) // Weapons (Slots 1, 2, 3, 4)
        {
            WeaponData weapon = GetActiveWeaponForSlot(index);

            if (weapon != null)
            {
                centerTitle.text = weapon.weaponName;
                centerTitle.style.color = new StyleColor(new Color(1f, 0.88f, 0.25f)); // Warm Amber/Gold
                centerDesc.text = GetWeaponDescription(weapon);
                if (centerHeroIcon != null && weapon.icon != null)
                {
                    centerHeroIcon.style.backgroundImage = new StyleBackground(weapon.icon);
                    centerHeroIcon.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                centerTitle.text = "Locked";
                centerTitle.style.color = new StyleColor(new Color(0.5f, 0.55f, 0.62f));
                centerDesc.text = "No weapon acquired yet.";
                if (centerHeroIcon != null) centerHeroIcon.style.display = DisplayStyle.None;
            }
        }
        else
        {
            centerTitle.text = "";
            centerDesc.text = "";
            if (centerHeroIcon != null) centerHeroIcon.style.display = DisplayStyle.None;
        }
    }

    private string GetWeaponDescription(WeaponData weapon)
    {
        if (weapon == null) return "";
        switch (weapon.category)
        {
            case WeaponCategory.Pistol:
                return "Rapid fire handgun good for close range.";
            case WeaponCategory.AssaultRifle:
                return "Versatile automatic rifle with high accuracy.";
            case WeaponCategory.Shotgun:
                return "High spread pellets with devastating burst damage.";
            case WeaponCategory.Minigun:
            case WeaponCategory.RocketLauncher:
                return "Rapid suppression firearm with massive capacity.";
            default:
                return $"{weapon.fireMode} weapon with {weapon.baseDamage} damage.";
        }
    }

    private void DrawCooldownSlices(MeshGenerationContext mgc)
    {
        if (PlayerController.Instance == null || cooldownLayer == null) return;

        var painter = mgc.painter2D;
        painter.lineCap = LineCap.Butt;
        painter.lineJoin = LineJoin.Miter;

        float centerX = cooldownLayer.layout.width / 2f;
        float centerY = cooldownLayer.layout.height / 2f;

        // Radii scaled for 680px wheel (center hub 153px, outer rim 316px)
        float innerRadius = 153f;
        float outerRadius = 316f;
        float strokeCenter = (innerRadius + outerRadius) / 2f;
        float strokeWidth = outerRadius - innerRadius;

        // Skill Cooldown (Index 5, Bottom-Left -> 180° to 270°, 90° span)
        float skillRatio = PlayerController.Instance.GetSkillRechargeRatio();
        if (skillRatio < 1f)
        {
            float totalSpan = 90f;
            float sweepAngle = totalSpan * (1f - skillRatio);
            float startAngle = 180f;
            float uiStart = startAngle - 90f;
            float uiEnd = uiStart + sweepAngle;

            painter.strokeColor = new Color(0f, 0f, 0f, 0.72f);
            painter.lineWidth = strokeWidth;
            painter.BeginPath();
            painter.Arc(new Vector2(centerX, centerY), strokeCenter, uiStart, uiEnd);
            painter.Stroke();
        }

        // Ultimate Cooldown (Index 4, Bottom-Right -> 90° to 180°, 90° span)
        float ultRatio = PlayerController.Instance.GetUltimateRechargeRatio();
        if (ultRatio < 1f)
        {
            float totalSpan = 90f;
            float sweepAngle = totalSpan * (1f - ultRatio);
            float startAngle = 90f;
            float uiStart = startAngle - 90f;
            float uiEnd = uiStart + sweepAngle;

            painter.strokeColor = new Color(0f, 0f, 0f, 0.72f);
            painter.lineWidth = strokeWidth;
            painter.BeginPath();
            painter.Arc(new Vector2(centerX, centerY), strokeCenter, uiStart, uiEnd);
            painter.Stroke();
        }
    }
}
