using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UGSManager : MonoBehaviour
{
    public static UGSManager Instance { get; private set; }

    public string PlayerID { get; private set; }

    private void Awake()
    {
        // Standard Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep this alive across all scenes
    }

    private async void Start()
    {
        await InitializeUnityServicesAsync();
    }

    private async Task InitializeUnityServicesAsync()
    {
        try
        {
            // 1. Initialize the Core Services
            await UnityServices.InitializeAsync();
            Debug.Log("<color=green>UGS Initialized Successfully!</color>");

            // 2. Setup Authentication Event Listeners
            SetupAuthenticationEvents();

            // 3. Sign the player in anonymously
            await SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing UGS: {e.Message}");
        }
    }

    private void SetupAuthenticationEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            PlayerID = AuthenticationService.Instance.PlayerId;
            Debug.Log($"<color=cyan>Player Signed In! ID: {PlayerID}</color>");

            // NOTE: This is where we will eventually load their Cloud Save data!
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError($"Sign-in failed: {err}");
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player Signed Out.");
        };
    }

    private async Task SignInAnonymouslyAsync()
    {
        try
        {
            // If they aren't already logged in, log them in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Authentication Exception: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request Failed: {ex.Message}");
        }
    }
}