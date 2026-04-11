using UnityEngine;
using TMPro;

namespace UI.Control
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Player Status UI")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI armorText;

        [Header("Weapon UI")]
        [SerializeField] private TextMeshProUGUI ammoText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Optional: DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Updates the health display.
        /// Example: "HP: 100 / 100"
        /// </summary>
        public void UpdateHealth(float current, float max)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }

        /// <summary>
        /// Updates the armor display.
        /// Example: "Armor: 50"
        /// </summary>
        public void UpdateArmor(float current)
        {
            if (armorText != null)
            {
                armorText.text = $"Armor: {Mathf.CeilToInt(current)}";
            }
        }

        /// <summary>
        /// Updates the ammo display string.
        /// Example Output: "24 / 30"
        /// </summary>
        public void UpdateAmmo(int current, int max)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{current} / {max}";
            }
        }
    }
}
