using UnityEngine;
using UnityEngine.UIElements;
using Player.Control;
using System.Collections.Generic;

public class WeaponWheelToolkitManager : MonoBehaviour
{
    public static WeaponWheelToolkitManager Instance;

    [Header("UI Document")]
    public UIDocument document;

    [Header("Colors & Styling")]
    [Tooltip("The main highlight color when hovering over slots.")]
    public Color highlightColor = new Color(1f, 1f, 1f, 0.8f);
    public Color weaponSliceColor = new Color(1f, 0.2f, 0.2f, 0.5f);
    public Color skillSliceColor = new Color(0.2f, 1f, 0.2f, 0.5f);
    public Color ultimateSliceColor = new Color(0.2f, 0.2f, 1f, 0.5f);
    public Color baseLineColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    [Header("Background & Icons")]
    [Tooltip("Optional background image for the wheel. If empty, uses the CSS default.")]
    public Texture2D wheelBackgroundImage;
    
    [Tooltip("Default icons to show when no weapon is equipped in a slot.")]
    public Texture2D defaultPistolIcon;
    public Texture2D defaultARIcon;
    public Texture2D defaultSMGIcon;
    public Texture2D defaultHeavyIcon;
    public Texture2D defaultSkillIcon;
    public Texture2D defaultUltimateIcon;

    private VisualElement wheelContainer;
    private VisualElement wheelBackground;
    private VisualElement baseLinesLayer;
    private VisualElement highlightLayer;
    private Label centerText;

    private VisualElement slotSMG, slotHeavy, slotUltimate, slotSkill, slotPistol, slotAR;
    private VisualElement iconSMG, iconHeavy, iconPistol, iconAR, iconSkill, iconUltimate;

    private int selectedIndex = -1;
    // 0: SMG, 1: Heavy, 2: Ultimate, 3: Skill, 4: Pistol, 5: AR
    
    private WeaponData[] weaponSlots = new WeaponData[6];

    public bool IsOpen => wheelContainer != null && wheelContainer.style.display == DisplayStyle.Flex;

    private void Awake()
    {
        Instance = this;
        if (document == null) document = GetComponent<UIDocument>();
        
        var root = document.rootVisualElement;
        wheelContainer = root.Q<VisualElement>("wheel-container");
        wheelBackground = root.Q<VisualElement>("wheel-background");
        baseLinesLayer = root.Q<VisualElement>("base-lines-layer");
        highlightLayer = root.Q<VisualElement>("highlight-layer");
        centerText = root.Q<Label>("center-text");

        slotSMG = root.Q<VisualElement>("slot-smg");
        slotHeavy = root.Q<VisualElement>("slot-heavy");
        slotUltimate = root.Q<VisualElement>("slot-ultimate");
        slotSkill = root.Q<VisualElement>("slot-skill");
        slotPistol = root.Q<VisualElement>("slot-pistol");
        slotAR = root.Q<VisualElement>("slot-ar");

        iconSMG = slotSMG?.Q<VisualElement>("icon");
        iconHeavy = slotHeavy?.Q<VisualElement>("icon");
        iconPistol = slotPistol?.Q<VisualElement>("icon");
        iconAR = slotAR?.Q<VisualElement>("icon");
        iconSkill = slotSkill?.Q<VisualElement>("icon");
        iconUltimate = slotUltimate?.Q<VisualElement>("icon");

        if (wheelBackgroundImage == null)
        {
#if UNITY_EDITOR
            wheelBackgroundImage = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/Sprite/WeaponWheel.png");
#endif
        }

        if (wheelBackground != null && wheelBackgroundImage != null)
        {
            wheelBackground.style.backgroundImage = new StyleBackground(wheelBackgroundImage);
            // Ensure background has no generic CSS styling if we are using the detailed sprite
            wheelBackground.style.backgroundColor = new StyleColor(Color.clear);
            wheelBackground.style.borderTopWidth = 0;
            wheelBackground.style.borderBottomWidth = 0;
            wheelBackground.style.borderLeftWidth = 0;
            wheelBackground.style.borderRightWidth = 0;
        }

        // Initialize slots with default icons
        UpdateSlotUI(0);
        UpdateSlotUI(1);
        UpdateSlotUI(2); // Ultimate
        UpdateSlotUI(3); // Skill
        UpdateSlotUI(4);
        UpdateSlotUI(5);

        if (baseLinesLayer != null)
        {
            baseLinesLayer.generateVisualContent += DrawBaseWheel;
        }

        if (highlightLayer != null)
        {
            highlightLayer.generateVisualContent += DrawHighlightSlice;
            highlightLayer.generateVisualContent += DrawCooldownSlices;
        }

        if (wheelContainer != null)
        {
            wheelContainer.style.display = DisplayStyle.None; // Hide on start
        }
    }

    // --- FIXED SLOT MAPPING ---
    private int GetSlotIndexForCategory(WeaponCategory category)
    {
        switch (category)
        {
            case WeaponCategory.SMG: return 0;
            case WeaponCategory.Minigun: return 1; // Heavy Gun
            case WeaponCategory.RocketLauncher: return 1; // Heavy Gun
            case WeaponCategory.Pistol: return 4;
            case WeaponCategory.AssaultRifle: return 5;
            case WeaponCategory.Sniper: return 5; // Group with AR
            case WeaponCategory.Shotgun: return 0; // Group with SMG
            default: return 4;
        }
    }

    public void AddWeaponToWheel(WeaponData newWeapon)
    {
        if (newWeapon == null) return;
        int targetSlot = GetSlotIndexForCategory(newWeapon.category);
        weaponSlots[targetSlot] = newWeapon;
        UpdateSlotUI(targetSlot);
    }

    private void UpdateSlotUI(int index)
    {
        VisualElement iconTarget = null;
        Texture2D defaultIcon = null;

        if (index == 0) { iconTarget = iconSMG; defaultIcon = defaultSMGIcon; }
        else if (index == 1) { iconTarget = iconHeavy; defaultIcon = defaultHeavyIcon; }
        else if (index == 2) { iconTarget = iconUltimate; defaultIcon = defaultUltimateIcon; }
        else if (index == 3) { iconTarget = iconSkill; defaultIcon = defaultSkillIcon; }
        else if (index == 4) { iconTarget = iconPistol; defaultIcon = defaultPistolIcon; }
        else if (index == 5) { iconTarget = iconAR; defaultIcon = defaultARIcon; }

        if (iconTarget != null)
        {
            if (weaponSlots[index] != null && weaponSlots[index].icon != null)
            {
                // Use the equipped weapon's icon
                iconTarget.style.backgroundImage = new StyleBackground(weaponSlots[index].icon);
            }
            else if (index == 2 && PlayerController.Instance != null && PlayerController.Instance.equippedUltimate != null && PlayerController.Instance.equippedUltimate.icon != null)
            {
                iconTarget.style.backgroundImage = new StyleBackground(PlayerController.Instance.equippedUltimate.icon);
            }
            else if (index == 3 && PlayerController.Instance != null && PlayerController.Instance.equippedSkill != null && PlayerController.Instance.equippedSkill.icon != null)
            {
                iconTarget.style.backgroundImage = new StyleBackground(PlayerController.Instance.equippedSkill.icon);
            }
            else if (defaultIcon != null)
            {
                // Fallback to the default inspector-assigned icon
                iconTarget.style.backgroundImage = new StyleBackground(defaultIcon);
            }
        }
    }

    public void OpenWheel()
    {
        if (wheelContainer == null) return;
        wheelContainer.style.display = DisplayStyle.Flex;
        Time.timeScale = 0.1f;
        selectedIndex = -1; // Reset selection
        if (highlightLayer != null) highlightLayer.MarkDirtyRepaint();
    }

    public void CloseWheel()
    {
        if (wheelContainer == null) return;
        wheelContainer.style.display = DisplayStyle.None;
        Time.timeScale = 1.0f;

        if (selectedIndex != -1)
        {
            EquipSelected();
        }
    }

    private void EquipSelected()
    {
        if (PlayerController.Instance == null) return;

        if (selectedIndex == 2)
        {
            PlayerController.Instance.ActivateUltimate();
        }
        else if (selectedIndex == 3)
        {
            PlayerController.Instance.ActivateSkill();
        }
        else if (weaponSlots[selectedIndex] != null)
        {
            PlayerController.Instance.EquipWeaponFromWheel(weaponSlots[selectedIndex]);
        }
    }

    private void Update()
    {
        if (wheelContainer == null || wheelContainer.style.display == DisplayStyle.None) return;

        // Force repaint of the highlight layer to animate cooldown slices
        if (highlightLayer != null) highlightLayer.MarkDirtyRepaint();

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = TouchManager.CurrentTouchPosition - screenCenter;

        if (dir.magnitude > 50f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float clockwiseAngle = 90f - angle;
            if (clockwiseAngle < 0) clockwiseAngle += 360f;

            int oldIndex = selectedIndex;

            if (clockwiseAngle >= 0 && clockwiseAngle < 45f) selectedIndex = 0;
            else if (clockwiseAngle >= 45f && clockwiseAngle < 90f) selectedIndex = 1;
            else if (clockwiseAngle >= 90f && clockwiseAngle < 180f) selectedIndex = 2; // Ultimate
            else if (clockwiseAngle >= 180f && clockwiseAngle < 270f) selectedIndex = 3; // Skill
            else if (clockwiseAngle >= 270f && clockwiseAngle < 315f) selectedIndex = 4;
            else if (clockwiseAngle >= 315f && clockwiseAngle < 360f) selectedIndex = 5;

            if (oldIndex != selectedIndex)
            {
                UpdateCenterText();
                if (highlightLayer != null) highlightLayer.MarkDirtyRepaint(); // Trigger redraw of Vector API
            }
        }
    }

    private void UpdateCenterText()
    {
        if (centerText == null) return;

        if (selectedIndex == 2) centerText.text = "ULTIMATE";
        else if (selectedIndex == 3) centerText.text = "SKILL";
        else if (selectedIndex >= 0 && selectedIndex < 6 && weaponSlots[selectedIndex] != null) 
            centerText.text = weaponSlots[selectedIndex].weaponName;
        else centerText.text = "";
    }

    // --- VECTOR API DRAWING ---
    private void DrawHighlightSlice(MeshGenerationContext mgc)
    {
        if (selectedIndex == -1) return;

        float startAngle = 0f;
        float endAngle = 0f;
        Color sliceColor = highlightColor;

        switch (selectedIndex)
        {
            case 0: startAngle = 0f; endAngle = 45f; sliceColor = weaponSliceColor; break;
            case 1: startAngle = 45f; endAngle = 90f; sliceColor = weaponSliceColor; break;
            case 2: startAngle = 90f; endAngle = 180f; sliceColor = ultimateSliceColor; break;
            case 3: startAngle = 180f; endAngle = 270f; sliceColor = skillSliceColor; break;
            case 4: startAngle = 270f; endAngle = 315f; sliceColor = weaponSliceColor; break;
            case 5: startAngle = 315f; endAngle = 360f; sliceColor = weaponSliceColor; break;
        }

        var painter = mgc.painter2D;
        painter.lineCap = LineCap.Butt;
        painter.lineJoin = LineJoin.Miter;

        float centerX = highlightLayer.layout.width / 2f;
        float centerY = highlightLayer.layout.height / 2f;
        float radius = 280f; // adjust to match the wheel size

        // UI Toolkit Angle conversion: 0 is Right, 90 is Bottom. 
        // Our map: 0 is Top. So Top is -90 in UI Toolkit.
        float uiStart = -90f + startAngle;
        float uiEnd = -90f + endAngle;

        // Calculate the donut dimensions
        // The background image is 600x600 (Radius 300). The center hole is roughly radius 100.
        // We draw the stroke halfway between the inner and outer edge.
        float innerRadius = 100f;
        float outerRadius = 290f; // slightly inside the edge
        float strokeCenter = (innerRadius + outerRadius) / 2f;
        float strokeWidth = outerRadius - innerRadius;

        // Draw the main highlight color base using a very thick stroke
        painter.strokeColor = sliceColor;
        painter.lineWidth = strokeWidth;
        painter.BeginPath();
        painter.Arc(new Vector2(centerX, centerY), strokeCenter, uiStart, uiEnd);
        painter.Stroke();

        // Draw a glowing outline effect on the inner and outer edges
        Color glowColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.8f);
        painter.strokeColor = glowColor;
        painter.lineWidth = 4f;

        // Outer glow
        painter.BeginPath();
        painter.Arc(new Vector2(centerX, centerY), outerRadius, uiStart, uiEnd);
        painter.Stroke();

        // Inner glow
        painter.BeginPath();
        painter.Arc(new Vector2(centerX, centerY), innerRadius, uiStart, uiEnd);
        painter.Stroke();
    }

    private void DrawBaseWheel(MeshGenerationContext mgc)
    {
        var painter = mgc.painter2D;
        painter.lineCap = LineCap.Round;
        painter.lineJoin = LineJoin.Round;
        painter.strokeColor = baseLineColor;
        painter.lineWidth = 4f;

        float centerX = baseLinesLayer.layout.width / 2f;
        float centerY = baseLinesLayer.layout.height / 2f;
        float innerRadius = 100f;
        float outerRadius = 290f;

        // Draw inner circle border
        painter.BeginPath();
        painter.Arc(new Vector2(centerX, centerY), innerRadius, 0f, 360f);
        painter.Stroke();

        // Draw outer circle border
        painter.BeginPath();
        painter.Arc(new Vector2(centerX, centerY), outerRadius, 0f, 360f);
        painter.Stroke();

        // The dividing lines between slices
        // In UI Toolkit, 0 is Right, 90 is Bottom. 
        // Our dividing clock angles: 0, 45, 90, 180, 270, 315
        float[] dividingAngles = new float[] { -90f, -45f, 0f, 90f, 180f, 225f };

        foreach (float angle in dividingAngles)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector2 innerPoint = new Vector2(centerX + Mathf.Cos(rad) * innerRadius, centerY + Mathf.Sin(rad) * innerRadius);
            Vector2 outerPoint = new Vector2(centerX + Mathf.Cos(rad) * outerRadius, centerY + Mathf.Sin(rad) * outerRadius);

            painter.BeginPath();
            painter.MoveTo(innerPoint);
            painter.LineTo(outerPoint);
            painter.Stroke();
        }
    }

    private void DrawCooldownSlices(MeshGenerationContext mgc)
    {
        if (PlayerController.Instance == null) return;

        var painter = mgc.painter2D;
        painter.lineCap = LineCap.Butt;
        painter.lineJoin = LineJoin.Miter;

        float innerRadius = 100f;
        float outerRadius = 290f;
        float strokeCenter = (innerRadius + outerRadius) / 2f;
        float strokeWidth = outerRadius - innerRadius;

        float centerX = highlightLayer.layout.width / 2f;
        float centerY = highlightLayer.layout.height / 2f;

        // Skill Cooldown (Index 3, Left slice -> 180 to 270 degrees in clock math)
        // UI Toolkit rotation: 0 is Top, so -90 is Top, right is 0. Wait, in UI toolkit, top is -90.
        // My slices:
        // 0: start 0, end 45
        // 1: start 45, end 90
        // 2 (Ultimate): start 90, end 180
        // 3 (Skill): start 180, end 270
        float skillRatio = PlayerController.Instance.GetSkillRechargeRatio();
        if (skillRatio < 1f)
        {
            float sweepAngle = 90f * (1f - skillRatio);
            float startAngle = 180f; 
            float uiStart = -90f + startAngle;
            float uiEnd = uiStart + sweepAngle;

            painter.strokeColor = new Color(0f, 0f, 0f, 0.7f);
            painter.lineWidth = strokeWidth;
            painter.BeginPath();
            painter.Arc(new Vector2(centerX, centerY), strokeCenter, uiStart, uiEnd);
            painter.Stroke();
        }

        // Ultimate Cooldown (Index 2, Bottom slice -> 90 to 180)
        float ultRatio = PlayerController.Instance.GetUltimateRechargeRatio();
        if (ultRatio < 1f)
        {
            float sweepAngle = 90f * (1f - ultRatio);
            float startAngle = 90f;
            float uiStart = -90f + startAngle;
            float uiEnd = uiStart + sweepAngle;

            painter.strokeColor = new Color(0f, 0f, 0f, 0.7f);
            painter.lineWidth = strokeWidth;
            painter.BeginPath();
            painter.Arc(new Vector2(centerX, centerY), strokeCenter, uiStart, uiEnd);
            painter.Stroke();
        }
    }
}
