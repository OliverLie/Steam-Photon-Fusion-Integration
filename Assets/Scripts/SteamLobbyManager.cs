using UnityEngine;
using UnityEngine.Events;
using Steamworks;

public class SteamLobbyManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxPlayers = 8;
    [SerializeField] private ELobbyType lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;

    [Header("Events")]
    public UnityEvent<string> OnPhotonSessionCreated;  // RENAMED
    public UnityEvent<string> OnPhotonSessionJoined;   // RENAMED
    public UnityEvent<string> OnLobbyCreateFailed;     // RENAMED
    public UnityEvent<string> OnLobbyJoinFailed;       // RENAMED
    [Header("Lobby Members")]
    public UnityEvent OnLobbyMembersUpdated;

    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    // Steam callbacks
    private Callback<LobbyCreated_t> lobbyCreated;
    private Callback<LobbyEnter_t> lobbyEntered;
    private Callback<GameLobbyJoinRequested_t> lobbyJoinRequested;
    private Callback<LobbyMatchList_t> lobbyList;

    // Current lobby
    private CSteamID currentLobbyID;
    public bool IsInLobby => currentLobbyID.IsValid();

    void Start()
    {
        if (!SteamManager.IsInitialized)
        {
            Debug.LogError("Steam not initialized!");
            return;
        }

        Debug.Log("[STEAM] Registering callbacks...");

        // Register callbacks
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        lobbyList = Callback<LobbyMatchList_t>.Create(OnLobbyList);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        Debug.Log("[STEAM] Callbacks registered!");
    }

    #region Public Methods

    public void CreateLobby()
    {
        Debug.Log($"[STEAM] CreateLobby called. Currently in lobby: {IsInLobby}");

        if (!SteamManager.IsInitialized)
        {
            Debug.LogError("Cannot create lobby - Steam not initialized");
            OnLobbyCreateFailed?.Invoke("Steam not initialized");
            return;
        }

        // VIGTIGT: Hvis vi allerede er i en lobby, leave først!
        if (IsInLobby)
        {
            Debug.LogWarning("[STEAM] Already in lobby! Leaving first...");
            LeaveLobby();
        }

        Debug.Log($"Creating Steam lobby (Max players: {maxPlayers})...");
        SteamMatchmaking.CreateLobby(lobbyType, maxPlayers);
    }
    // Callback når folk joiner/leaver
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        CSteamID userChanged = new CSteamID(callback.m_ulSteamIDUserChanged);

        EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

        string userName = SteamFriends.GetFriendPersonaName(userChanged);

        Debug.Log($"[STEAM] ============ LOBBY CHAT UPDATE ============");
        Debug.Log($"[STEAM] User: {userName} ({userChanged})");
        Debug.Log($"[STEAM] State Change: {stateChange}");
        Debug.Log($"[STEAM] Current member count: {SteamMatchmaking.GetNumLobbyMembers(currentLobbyID)}");

        switch (stateChange)
        {
            case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                Debug.Log($"[STEAM] {userName} joined the lobby");
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                Debug.Log($"[STEAM] {userName} left the lobby");
                break;
            case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                Debug.Log($"[STEAM] {userName} disconnected");
                break;
        }

        Debug.Log($"[STEAM] Invoking OnLobbyMembersUpdated event...");

        // Trigger update event
        OnLobbyMembersUpdated?.Invoke();

        Debug.Log($"[STEAM] ============================================");
    }

    // Hent alle lobby members
    public LobbyMemberData[] GetLobbyMembers()
    {
        if (!IsInLobby)
        {
            return new LobbyMemberData[0];
        }

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyID);
        LobbyMemberData[] members = new LobbyMemberData[memberCount];

        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(currentLobbyID);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyID, i);
            members[i] = new LobbyMemberData(memberID);
            members[i].isHost = (memberID == lobbyOwner);

            // Hent avatar
            members[i].avatar = SteamAvatarLoader.Instance.GetSteamAvatar(memberID);
        }

        return members;
    }
    public void LeaveLobby()
    {
        Debug.Log($"[STEAM] LeaveLobby called. IsInLobby: {IsInLobby}");

        if (IsInLobby)
        {
            Debug.Log($"[STEAM] Leaving lobby: {currentLobbyID}");
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = CSteamID.Nil;
            Debug.Log("[STEAM] Lobby left successfully");
        }
        else
        {
            Debug.Log("[STEAM] Not in a lobby, nothing to leave");
        }
    }

    public void InviteFriendToLobby()
    {
        if (!IsInLobby)
        {
            Debug.LogWarning("Not in a lobby - cannot invite");
            return;
        }

        // Åbner Steam overlay friend list til invites
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
    }

    public void FindLobbies()
    {
        // Tilføj filtre hvis nødvendigt
        // SteamMatchmaking.AddRequestLobbyListStringFilter("game_version", "1.0", ELobbyComparison.k_ELobbyComparisonEqual);

        SteamMatchmaking.RequestLobbyList();
    }

    #endregion

    #region Steam Callbacks

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"Lobby creation failed: {callback.m_eResult}");
            OnLobbyCreateFailed?.Invoke(callback.m_eResult.ToString());
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"Steam lobby created: {currentLobbyID}");

        // Generer unikt Photon session navn
        string photonSessionName = GenerateSessionName();

        // Gem Photon session navn i Steam lobby metadata
        SteamMatchmaking.SetLobbyData(currentLobbyID, "PhotonSession", photonSessionName);
        SteamMatchmaking.SetLobbyData(currentLobbyID, "HostName", SteamManager.Instance.PlayerName);

        // Gør lobby joinable
        SteamMatchmaking.SetLobbyJoinable(currentLobbyID, true);

        Debug.Log($"Photon session name: {photonSessionName}");

        // Trigger event så Photon kan starte
        OnPhotonSessionCreated?.Invoke(photonSessionName);
    }

    private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Triggered når nogen accepterer en invite eller klikker "Join Game"
        Debug.Log($"Lobby join requested: {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"Failed to enter lobby: {callback.m_EChatRoomEnterResponse}");
            OnLobbyJoinFailed?.Invoke(callback.m_EChatRoomEnterResponse.ToString());
            return;
        }

        Debug.Log($"Entered Steam lobby: {currentLobbyID}");

        // Hent Photon session info fra lobby metadata
        string photonSessionName = SteamMatchmaking.GetLobbyData(currentLobbyID, "PhotonSession");
        string hostName = SteamMatchmaking.GetLobbyData(currentLobbyID, "HostName");

        if (string.IsNullOrEmpty(photonSessionName))
        {
            Debug.LogError("No Photon session found in lobby data!");
            OnLobbyJoinFailed?.Invoke("Invalid lobby data");
            return;
        }

        Debug.Log($"Joining Photon session: {photonSessionName} (Host: {hostName})");

        // Trigger event så Photon kan joine
        OnPhotonSessionJoined?.Invoke(photonSessionName);
    }

    private void OnLobbyList(LobbyMatchList_t callback)
    {
        Debug.Log($"Found {callback.m_nLobbiesMatching} lobbies");

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            string hostName = SteamMatchmaking.GetLobbyData(lobbyID, "HostName");
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyID);

            Debug.Log($"Lobby {i}: Host={hostName}, Players={numMembers}");
        }
    }

    #endregion

    private string GenerateSessionName()
    {
        // Kombination af Steam ID og timestamp sikrer unikhed
        return $"Session_{SteamManager.Instance.SteamID}_{System.DateTime.Now.Ticks}";
    }
}