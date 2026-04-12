using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Player.Control;

public class WeaponWheelManager : MonoBehaviour
{
    public static WeaponWheelManager Instance;

    [Header("UI References")]
    public GameObject wheelUI;              // The main panel containing the wheel
    public Image[] slotIcons;               // Array of 8 Image components for the weapon icons
    public RectTransform selectorHighlight; // The "Pie Slice" highlight image
    public TextMeshProUGUI centerWeaponName;// Text in the middle of the wheel

    [Header("Data")]
    // Tracks which WeaponEntry is in which slot (0-7)
    private WeaponEntry[] slots = new WeaponEntry[8];
    private int selectedIndex = -1;

    private void Awake()
    {
        Instance = this;
        wheelUI.SetActive(false); // Make sure it's hidden on start
    }

    // --- FIXED SLOT MAPPING ---
    // Guarantees specific weapon types always appear in the exact same slot
    private int GetSlotIndexForCategory(WeaponCategory category)
    {
        switch (category)
        {
            case WeaponCategory.AssaultRifle: return 0; // Top
            case WeaponCategory.Sniper: return 1; // Top Right
            case WeaponCategory.Minigun: return 2; // Right
            case WeaponCategory.Grenade: return 3; // Bottom Right
            case WeaponCategory.Shotgun: return 4; // Bottom
            case WeaponCategory.RocketLauncher: return 5; // Bottom Left
            case WeaponCategory.SMG: return 6; // Left
            case WeaponCategory.Pistol: return 7; // Top Left 
            default: return 0;
        }
    }

    // --- ADDING WEAPONS ---
    public void AddWeaponToWheel(WeaponEntry newWeapon)
    {
        if (newWeapon == null) return;

        // Find the EXACT slot this weapon belongs in based on its category
        int targetSlot = GetSlotIndexForCategory(newWeapon.category);

        // Put it in that slot (overwriting whatever was there before)
        slots[targetSlot] = newWeapon;
        UpdateSlotUI(targetSlot);

        Debug.Log($"Added {newWeapon.weaponName} to Slot {targetSlot}");
    }

    private void UpdateSlotUI(int index)
    {
        if (slots[index] != null && slots[index].icon != null)
        {
            slotIcons[index].sprite = slots[index].icon;
            slotIcons[index].color = Color.white; // Make the icon visible
        }
    }

    // --- OPEN / CLOSE LOGIC ---
    public void OpenWheel()
    {
        wheelUI.SetActive(true);
        Time.timeScale = 0.1f; // Trigger slow-motion
    }

    public void CloseWheel()
    {
        wheelUI.SetActive(false);
        Time.timeScale = 1.0f; // Return to normal speed

        // Equip the weapon if they landed on a valid slot
        if (selectedIndex != -1 && slots[selectedIndex] != null)
        {
            EquipSelectedWeapon();
        }
    }

    private void EquipSelectedWeapon()
    {
        WeaponEntry chosenWeapon = slots[selectedIndex];
        Debug.Log($"<color=green>Equipping Weapon: {chosenWeapon.weaponName}</color>");

        // CALL THE PLAYER CONTROLLER
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EquipWeaponFromWheel(chosenWeapon);
        }
        else
        {
            Debug.LogError("PlayerController Instance is null! Make sure Awake() is setting it.");
        }
    }

    // --- WHEEL SELECTION MATH ---
    private void Update()
    {
        // Don't calculate math if the wheel is closed
        if (!wheelUI.activeSelf) return;

        // Find the direction from the center of the screen to the finger
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 dir = TouchManager.CurrentTouchPosition - screenCenter;

        // Apply a small deadzone so it doesn't freak out if the finger is perfectly in the middle
        if (dir.magnitude > 50f)
        {
            // PERFECT CLOCKWISE MATH
            // Converts raw angle into a 0-360 degree format where 0 is at the TOP
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float clockwiseAngle = 90f - angle;
            if (clockwiseAngle < 0) clockwiseAngle += 360f;

            // Offset by 22.5 degrees so the UI slice sits perfectly *over* the icon
            float offsetAngle = (clockwiseAngle + 22.5f) % 360f;

            // Divide by 45 degrees (360 degrees / 8 slots) to get the index (0-7)
            selectedIndex = Mathf.FloorToInt(offsetAngle / 45f);

            // Rotate the highlight UI graphic to match
            selectorHighlight.localRotation = Quaternion.Euler(0, 0, -selectedIndex * 45f);

            // Update the center text
            if (slots[selectedIndex] != null)
            {
                centerWeaponName.text = slots[selectedIndex].weaponName;
            }
            else
            {
                centerWeaponName.text = "Empty";
            }
        }
    }
}