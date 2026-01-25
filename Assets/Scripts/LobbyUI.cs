using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Fusion;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private int mainMenu = 0;
    private SteamLobbyManager lobbyManager;
    private int currentSceneIndex = -1;

    [Header("Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject PlayerListPanel;

    [Header("Player List")]
    [SerializeField] private string playerListContainerName = "Content"; // Navn på Content GameObject
    [SerializeField] private LobbyMemberUI memberPrefab; // Dette overlever da det er en prefab asset

    private Transform playerListContainer; // Ikke serialized - finder den runtime
    private List<LobbyMemberUI> activeMemberUIs = new List<LobbyMemberUI>();
    private bool isInitialized = false;

    void Start()
    {
        Debug.Log("[UI] Setting up LobbyUI...");

        EnsureSingleEventSystem();

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitializeUI();
    }

    private void InitializeUI()
    {
        Debug.Log("[UI] Initializing UI...");

        // Find SteamLobbyManager
        FindLobbyManager();

        // Find PlayerListContainer
        FindPlayerListContainer();

        if (lobbyManager == null)
        {
            Debug.LogError("[UI] Cannot find SteamLobbyManager!");
            return;
        }

        if (playerListContainer == null)
        {
            Debug.LogError("[UI] Cannot find playerListContainer!");
            return;
        }

        Debug.Log($"[UI] playerListContainer found: {playerListContainer.name}");
        Debug.Log($"[UI] memberPrefab is null: {memberPrefab == null}");

        // Setup button listeners (kun én gang)
        if (!isInitialized)
        {
            createLobbyButton.onClick.RemoveAllListeners();
            inviteFriendsButton.onClick.RemoveAllListeners();
            leaveLobbyButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.RemoveAllListeners();

            createLobbyButton.onClick.AddListener(OnCreateLobby);
            inviteFriendsButton.onClick.AddListener(OnInviteFriends);
            leaveLobbyButton.onClick.AddListener(OnLeaveLobby);
            backToMenuButton.onClick.AddListener(BackToMenu);

            isInitialized = true;
            Debug.Log("[UI] Button listeners initialized");
        }

        // Setup lobby manager listeners (remove old først)
        lobbyManager.OnPhotonSessionCreated.RemoveListener(OnLobbyCreated);
        lobbyManager.OnPhotonSessionJoined.RemoveListener(OnLobbyJoined);
        lobbyManager.OnLobbyCreateFailed.RemoveListener(OnLobbyFailed);
        lobbyManager.OnLobbyMembersUpdated.RemoveListener(UpdatePlayerList);

        lobbyManager.OnPhotonSessionCreated.AddListener(OnLobbyCreated);
        lobbyManager.OnPhotonSessionJoined.AddListener(OnLobbyJoined);
        lobbyManager.OnLobbyCreateFailed.AddListener(OnLobbyFailed);
        lobbyManager.OnLobbyMembersUpdated.AddListener(UpdatePlayerList);

        Debug.Log("[UI] Lobby manager listeners set up");

        UpdateUIForScene();
        UpdateUI();
    }

    private void FindLobbyManager()
    {
        if (lobbyManager == null)
        {
            lobbyManager = FindFirstObjectByType<SteamLobbyManager>();

            if (lobbyManager != null)
            {
                Debug.Log("[UI] Found SteamLobbyManager");
            }
            else
            {
                Debug.LogError("[UI] SteamLobbyManager not found!");
            }
        }
    }

    private void FindPlayerListContainer()
    {
        // Find Content GameObject under PlayerListPanel
        if (PlayerListPanel != null)
        {
            // Søg efter Content i alle children
            Transform[] children = PlayerListPanel.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child.name == playerListContainerName)
                {
                    playerListContainer = child;
                    Debug.Log($"[UI] Found playerListContainer: {child.name}");
                    return;
                }
            }

            Debug.LogError($"[UI] Could not find '{playerListContainerName}' under PlayerListPanel!");
        }
        else
        {
            Debug.LogError("[UI] PlayerListPanel is null!");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[UI] Scene loaded: {scene.name} (index: {scene.buildIndex})");

        EnsureSingleEventSystem();

        // VIGTIGT: Re-find alle references efter scene load
        FindLobbyManager();
        FindPlayerListContainer();

        // Re-initialize hvis nødvendigt
        if (lobbyManager != null && playerListContainer != null)
        {
            InitializeUI();
        }
        else
        {
            Debug.LogError("[UI] Failed to find required components after scene load!");
        }

        UpdateUIForScene();
    }

    public void BackToMenu()
    {
        Debug.Log("[UI] ========== BACK TO MENU ==========");
        Debug.Log($"[UI] Currently in lobby: {(lobbyManager != null ? lobbyManager.IsInLobby.ToString() : "null")}");

        // Leave lobby FØRST
        if (lobbyManager != null && lobbyManager.IsInLobby)
        {
            Debug.Log("[UI] Leaving lobby before scene change...");
            lobbyManager.LeaveLobby();
        }

        // Clear UI
        ClearPlayerList();

        // Find og shutdown Photon runner
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("[UI] Shutting down Photon runner...");
            runner.Shutdown();
        }

        Debug.Log($"[UI] Loading scene: {mainMenu}");
        Debug.Log("[UI] ====================================");
        SceneManager.LoadScene(mainMenu);
    }

    void OnCreateLobby()
    {
        if (lobbyManager == null)
        {
            Debug.LogError("[UI] Cannot create lobby - lobbyManager is null!");
            FindLobbyManager();
            return;
        }

        statusText.text = "Creating lobby...";
        lobbyManager.CreateLobby();
    }

    void OnInviteFriends()
    {
        if (lobbyManager == null) return;
        lobbyManager.InviteFriendToLobby();
    }

    void OnLeaveLobby()
    {
        if (lobbyManager == null) return;
        lobbyManager.LeaveLobby();
        ClearPlayerList();
        UpdateUI();
    }

    void OnLobbyCreated(string sessionName)
    {
        Debug.Log($"[UI] OnLobbyCreated: {sessionName}");
        statusText.text = "Lobby created! Waiting for players...";
        UpdateUI();

        // Re-find container før update (safety check)
        if (playerListContainer == null)
        {
            Debug.LogWarning("[UI] playerListContainer null before UpdatePlayerList, re-finding...");
            FindPlayerListContainer();
        }

        Invoke(nameof(UpdatePlayerList), 0.5f);
    }

    void OnLobbyJoined(string sessionName)
    {
        Debug.Log($"[UI] OnLobbyJoined: {sessionName}");
        statusText.text = "Joined lobby! Connecting...";
        UpdateUI();

        // Re-find container før update (safety check)
        if (playerListContainer == null)
        {
            Debug.LogWarning("[UI] playerListContainer null before UpdatePlayerList, re-finding...");
            FindPlayerListContainer();
        }

        Invoke(nameof(UpdatePlayerList), 0.5f);
    }

    void OnLobbyFailed(string reason)
    {
        statusText.text = $"Failed: {reason}";
        UpdateUI();
    }

    void UpdateUI()
    {
        if (lobbyManager == null)
        {
            Debug.LogWarning("[UI] Cannot update UI - lobbyManager is null");
            return;
        }

        bool inLobby = lobbyManager.IsInLobby;

        Debug.Log($"[UI] UpdateUI - InLobby: {inLobby}");

        createLobbyButton.interactable = !inLobby;
        inviteFriendsButton.interactable = inLobby;
        leaveLobbyButton.interactable = inLobby;

        if (!inLobby && statusText != null)
        {
            statusText.text = "Not in lobby";
        }
    }

    void UpdateUIForScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex == currentSceneIndex)
            return;

        currentSceneIndex = sceneIndex;

        Debug.Log($"[UI] Updating UI for scene index: {sceneIndex}");

        if (sceneIndex == mainMenu)
        {
            createLobbyButton.gameObject.SetActive(true);
            inviteFriendsButton.gameObject.SetActive(true);
            leaveLobbyButton.gameObject.SetActive(true);
            statusText.gameObject.SetActive(true);

            backToMenuButton.gameObject.SetActive(false);
            PlayerListPanel.SetActive(false);
        }
        else
        {
            createLobbyButton.gameObject.SetActive(false);
            inviteFriendsButton.gameObject.SetActive(false);
            leaveLobbyButton.gameObject.SetActive(false);
            statusText.gameObject.SetActive(false);

            backToMenuButton.gameObject.SetActive(true);
            PlayerListPanel.SetActive(true);
        }
    }

    void UpdatePlayerList()
    {
        Debug.Log("[UI] ========== UPDATE PLAYER LIST ==========");

        // Triple check alt er ready
        if (lobbyManager == null)
        {
            Debug.LogError("[UI] lobbyManager is null!");
            FindLobbyManager();
            if (lobbyManager == null) return;
        }

        if (playerListContainer == null)
        {
            Debug.LogError("[UI] playerListContainer is null!");
            FindPlayerListContainer();
            if (playerListContainer == null) return;
        }

        if (memberPrefab == null)
        {
            Debug.LogError("[UI] memberPrefab is null! Assign it in Inspector!");
            return;
        }

        Debug.Log($"[UI] playerListContainer: {playerListContainer.name}");
        Debug.Log($"[UI] In lobby: {lobbyManager.IsInLobby}");

        if (!lobbyManager.IsInLobby)
        {
            ClearPlayerList();
            return;
        }

        ClearPlayerList();

        LobbyMemberData[] members = lobbyManager.GetLobbyMembers();
        Debug.Log($"[UI] Found {members.Length} members to display");

        foreach (var member in members)
        {
            Debug.Log($"[UI]   - Creating UI for: {member.playerName} (Host: {member.isHost})");

            LobbyMemberUI memberUI = Instantiate(memberPrefab, playerListContainer);
            memberUI.SetMemberData(member);
            activeMemberUIs.Add(memberUI);

            Debug.Log($"[UI]   - Instantiated at: {memberUI.transform.position}");
        }

        Debug.Log($"[UI] Created {activeMemberUIs.Count} UI elements");
        Debug.Log($"[UI] Container has {playerListContainer.childCount} children");
        Debug.Log("[UI] ========================================");

        Canvas.ForceUpdateCanvases();
    }

    void ClearPlayerList()
    {
        Debug.Log($"[UI] Clearing {activeMemberUIs.Count} player UI elements");

        foreach (var memberUI in activeMemberUIs)
        {
            if (memberUI != null)
            {
                Destroy(memberUI.gameObject);
            }
        }
        activeMemberUIs.Clear();
    }

    private void EnsureSingleEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        Debug.Log($"[UI] Found {eventSystems.Length} EventSystems in scene");

        if (eventSystems.Length == 0)
        {
            Debug.LogError("[UI] No EventSystem found! Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystemObj);
            return;
        }

        if (eventSystems.Length > 1)
        {
            Debug.LogWarning($"[UI] Found {eventSystems.Length} EventSystems! Removing duplicates...");

            EventSystem persistentEventSystem = null;

            foreach (var es in eventSystems)
            {
                if (es.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    persistentEventSystem = es;
                    break;
                }
            }

            if (persistentEventSystem == null)
            {
                persistentEventSystem = eventSystems[0];
                DontDestroyOnLoad(persistentEventSystem.gameObject);
                Debug.Log($"[UI] Moved {persistentEventSystem.gameObject.name} to DontDestroyOnLoad");
            }

            foreach (var es in eventSystems)
            {
                if (es != persistentEventSystem)
                {
                    Debug.Log($"[UI] Destroying duplicate EventSystem: {es.gameObject.name}");
                    Destroy(es.gameObject);
                }
            }
        }
        else
        {
            Debug.Log("[UI] EventSystem OK - only one found");

            if (eventSystems[0].gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(eventSystems[0].gameObject);
                Debug.Log($"[UI] Moved EventSystem to DontDestroyOnLoad");
            }
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (lobbyManager != null)
        {
            lobbyManager.OnPhotonSessionCreated.RemoveListener(OnLobbyCreated);
            lobbyManager.OnPhotonSessionJoined.RemoveListener(OnLobbyJoined);
            lobbyManager.OnLobbyCreateFailed.RemoveListener(OnLobbyFailed);
            lobbyManager.OnLobbyMembersUpdated.RemoveListener(UpdatePlayerList);
        }
    }
}