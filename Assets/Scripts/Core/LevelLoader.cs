using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for changing scenes
using UnityEngine.UI;              // Required if you want a loading bar

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("Loading Screen UI")]
    [Tooltip("The Canvas Panel containing your loading graphics.")]
    public GameObject loadingScreenPanel;

    [Tooltip("A UI Slider to show loading progress (Optional).")]
    public Slider loadingSlider;

    private void Awake()
    {
        // Standard Singleton so you can call it from anywhere
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Keeps this manager alive when the scene changes
        DontDestroyOnLoad(gameObject);

        // Ensure the loading screen is hidden when the game starts
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Call this from your Main Menu "Play" button!
    /// Example: LevelLoader.Instance.LoadLevel("GameScene");
    /// </summary>
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadSceneAsynchronously(sceneName));
    }

    private IEnumerator LoadSceneAsynchronously(string sceneName)
    {
        // 1. Turn on the loading screen UI
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }

        // 2. Tell Unity to start loading the scene in the background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 3. Update the loading bar while we wait
        while (!operation.isDone)
        {
            // Unity's progress stops at 0.9. This math normalizes it to a perfect 0.0 to 1.0 scale.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            yield return null; // Wait until the next frame before looping again
        }

        // 4. Once loaded, turn the loading screen back off
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }
}