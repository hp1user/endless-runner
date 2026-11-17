using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using EndlessRunner.Audio;

namespace EndlessRunner.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        [Header("HUD Trigger Button (Top-Right)")]
        [SerializeField] private Button pauseHUDButton;

        [Header("Pause Modal Overlay")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private TextMeshProUGUI stageInfoText;

        [Header("Modal Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button reviewBuffsButton;
        [SerializeField] private Button restartButton;

        public bool IsPaused => pauseOverlay != null && pauseOverlay.activeSelf;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureUIHierarchy();
        }

        private void Start()
        {
            if (pauseOverlay != null) pauseOverlay.SetActive(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
                {
                    if (IsPaused) ResumeGame();
                    else PauseGame();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (IsPaused) ResumeGame();
                else PauseGame();
            }
#endif
        }

        public void PauseGame()
        {
            // Do not pause if upgrade sequence is running or active buffs modal is open
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsSelectionOpen) return;
            if (ActiveBuffsModalUI.Instance != null && ActiveBuffsModalUI.Instance.IsOpen) return;

            TouchManager.IsInputBlocked = true;
            Time.timeScale = 0f;

            AudioManager.Instance?.PlayUISound(UISoundType.CardPopup);

            if (pauseOverlay != null) pauseOverlay.SetActive(true);

            UpdateStageInfo();
        }

        public void ResumeGame()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);

            if (pauseOverlay != null) pauseOverlay.SetActive(false);

            TouchManager.IsInputBlocked = false;
            Time.timeScale = 1f;
        }

        public void OpenBuffsFromPause()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);
            if (pauseOverlay != null) pauseOverlay.SetActive(false);

            if (ActiveBuffsModalUI.Instance != null)
            {
                ActiveBuffsModalUI.Instance.OpenModal();
            }
        }

        public void RestartLevel()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);

            TouchManager.IsInputBlocked = false;
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void UpdateStageInfo()
        {
            if (stageInfoText == null) return;

            int level = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
            bool isBoss = GameManager.Instance != null && GameManager.Instance.IsBossLevel(level);

            stageInfoText.text = isBoss
                ? $"<color=#FF5252>★ BOSS ENCOUNTER ★</color>\n<size=70%>LEVEL {level}</size>"
                : $"<color=#00E5FF>STAGE / SECTOR {level}</color>";
        }

        private void EnsureUIHierarchy()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform canvasTr = canvas.transform;

            // Remove previous instances if any existed so we cleanly rebuild with updated alignment
            Transform oldBtn = canvasTr.Find("PauseHUDButton");
            if (oldBtn != null)
            {
                oldBtn.name = "PauseHUDButton_Old";
                oldBtn.gameObject.SetActive(false);
                Destroy(oldBtn.gameObject);
            }

            Transform oldModal = canvasTr.Find("PauseModalOverlay");
            if (oldModal != null)
            {
                oldModal.name = "PauseModalOverlay_Old";
                oldModal.gameObject.SetActive(false);
                Destroy(oldModal.gameObject);
            }

            // 1. Setup Pause HUD Button (Top Right, centered at -40 inside 80px wing, centered vertically at -40)
            var s = BuffModalSettings.Instance;
            float btnSize = s != null ? s.hudButtonSize : 54f;

            GameObject btnObj = new GameObject("PauseHUDButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(canvasTr, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            rect.sizeDelta = new Vector2(btnSize, btnSize);

            Image img = btnObj.GetComponent<Image>();
            img.color = s != null ? s.hudButtonColor : new Color(0.10f, 0.13f, 0.18f, 0.95f);

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.30f, 0.40f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (s != null && s.pauseButtonSprite != null)
            {
                img.sprite = s.pauseButtonSprite;
                img.color = Color.white;
                img.preserveAspect = true;
                if (outline != null) outline.enabled = false;
            }
            else
            {
                // Procedural Dual Pause Bars (sharp vector look, no font glyph issues)
                GameObject bar1 = new GameObject("BarLeft", typeof(RectTransform), typeof(Image));
                bar1.transform.SetParent(btnObj.transform, false);
                RectTransform b1Rect = bar1.GetComponent<RectTransform>();
                b1Rect.anchorMin = new Vector2(0.5f, 0.5f);
                b1Rect.anchorMax = new Vector2(0.5f, 0.5f);
                b1Rect.pivot = new Vector2(0.5f, 0.5f);
                b1Rect.anchoredPosition = new Vector2(-5f, 0f);
                b1Rect.sizeDelta = new Vector2(5f, 18f);
                Image b1Img = bar1.GetComponent<Image>();
                b1Img.color = Color.white;
                b1Img.raycastTarget = false;

                GameObject bar2 = new GameObject("BarRight", typeof(RectTransform), typeof(Image));
                bar2.transform.SetParent(btnObj.transform, false);
                RectTransform b2Rect = bar2.GetComponent<RectTransform>();
                b2Rect.anchorMin = new Vector2(0.5f, 0.5f);
                b2Rect.anchorMax = new Vector2(0.5f, 0.5f);
                b2Rect.pivot = new Vector2(0.5f, 0.5f);
                b2Rect.anchoredPosition = new Vector2(5f, 0f);
                b2Rect.sizeDelta = new Vector2(5f, 18f);
                Image b2Img = bar2.GetComponent<Image>();
                b2Img.color = Color.white;
                b2Img.raycastTarget = false;
            }

            pauseHUDButton = btnObj.GetComponent<Button>();
            pauseHUDButton.onClick.AddListener(PauseGame);

            // 2. Setup Pause Modal Overlay
            pauseOverlay = CreateProceduralPauseModal(canvasTr);
        }

        private GameObject CreateProceduralPauseModal(Transform parentCanvas)
        {
            // Dark Backdrop
            GameObject overlay = new GameObject("PauseModalOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(parentCanvas, false);
            RectTransform ovRect = overlay.GetComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero;
            ovRect.anchorMax = Vector2.one;
            ovRect.offsetMin = Vector2.zero;
            ovRect.offsetMax = Vector2.zero;
            Image ovImg = overlay.GetComponent<Image>();
            ovImg.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);

            // Dialog Window
            GameObject dialog = new GameObject("DialogWindow", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(overlay.transform, false);
            RectTransform diaRect = dialog.GetComponent<RectTransform>();
            diaRect.anchorMin = new Vector2(0.5f, 0.5f);
            diaRect.anchorMax = new Vector2(0.5f, 0.5f);
            diaRect.pivot = new Vector2(0.5f, 0.5f);
            diaRect.sizeDelta = new Vector2(760f, 760f);
            dialog.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(dialog.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -40f);
            tRect.sizeDelta = new Vector2(660f, 65f);
            TextMeshProUGUI tText = titleObj.GetComponent<TextMeshProUGUI>();
            tText.fontSize = 44;
            tText.fontStyle = FontStyles.Bold;
            tText.alignment = TextAlignmentOptions.Center;
            tText.text = "<color=#FFB830>GAME PAUSED</color>";

            // Stage Info Subtitle
            GameObject subObj = new GameObject("StageInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(dialog.transform, false);
            RectTransform sRect = subObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -115f);
            sRect.sizeDelta = new Vector2(660f, 50f);
            stageInfoText = subObj.GetComponent<TextMeshProUGUI>();
            stageInfoText.fontSize = 24;
            stageInfoText.alignment = TextAlignmentOptions.Center;
            stageInfoText.text = "STAGE 1";

            // Buttons Container
            float btnY = -210f;
            float btnSpacing = 110f;

            // 1. Resume Button
            resumeButton = CreateModalButton(dialog.transform, "ResumeButton", "RESUME", new Color(0.0f, 0.75f, 0.85f, 1f), Color.black, btnY);
            resumeButton.onClick.AddListener(ResumeGame);

            // 2. Review Buff Cards Button
            btnY -= btnSpacing;
            reviewBuffsButton = CreateModalButton(dialog.transform, "ReviewBuffsButton", "REVIEW ACTIVE BUFFS", new Color(0.75f, 0.35f, 1.0f, 1f), Color.white, btnY);
            reviewBuffsButton.onClick.AddListener(OpenBuffsFromPause);

            // 3. Restart Level Button
            btnY -= btnSpacing;
            restartButton = CreateModalButton(dialog.transform, "RestartButton", "RESTART LEVEL", new Color(0.25f, 0.28f, 0.35f, 1f), new Color(1f, 0.4f, 0.4f, 1f), btnY);
            restartButton.onClick.AddListener(RestartLevel);

            overlay.SetActive(false);
            return overlay;
        }

        private Button CreateModalButton(Transform parent, string name, string label, Color bgColor, Color textColor, float posY)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, posY);
            rect.sizeDelta = new Vector2(500f, 75f);
            btnObj.GetComponent<Image>().color = bgColor;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.text = label;

            return btnObj.GetComponent<Button>();
        }
    }
}
