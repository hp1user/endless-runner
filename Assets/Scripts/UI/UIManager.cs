using UnityEngine;
using TMPro;

namespace UI.Control
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

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
        /// Updates the ammo display string.
        /// Example Output: "Bullets: 24 / 30"
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
