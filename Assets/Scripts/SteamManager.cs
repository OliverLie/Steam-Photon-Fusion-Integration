using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager instance;
    public static SteamManager Instance => instance;

    public static bool IsInitialized { get; private set; }
    public CSteamID SteamID { get; private set; }
    public string PlayerName { get; private set; }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSteam();
    }

    void InitializeSteam()
    {
        try
        {
            if (!SteamAPI.Init())
            {
                Debug.LogError("Steam API initialization failed!");
                return;
            }

            IsInitialized = true;
            SteamID = SteamUser.GetSteamID();
            PlayerName = SteamFriends.GetPersonaName();

            Debug.Log($"Steam initialized! Player: {PlayerName} (ID: {SteamID})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Steam initialization error: {e.Message}");
        }
    }

    void Update()
    {
        if (IsInitialized)
        {
            // Required to receive Steam callbacks
            SteamAPI.RunCallbacks();
        }
    }

    void OnApplicationQuit()
    {
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
        }
    }
}