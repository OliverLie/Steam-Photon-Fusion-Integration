using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SteamLobbyManager lobbyManager;

    [Header("Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button inviteFriendsButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Player List")]
    [SerializeField] private Transform playerListContainer; // Content i din ScrollView
    [SerializeField] private LobbyMemberUI memberPrefab;

    private List<LobbyMemberUI> activeMemberUIs = new List<LobbyMemberUI>();

    void Start()
    {
        Debug.Log("[UI] Setting up LobbyUI...");

        createLobbyButton.onClick.AddListener(OnCreateLobby);
        inviteFriendsButton.onClick.AddListener(OnInviteFriends);
        leaveLobbyButton.onClick.AddListener(OnLeaveLobby);

        lobbyManager.OnPhotonSessionCreated.AddListener(OnLobbyCreated);
        lobbyManager.OnPhotonSessionJoined.AddListener(OnLobbyJoined);
        lobbyManager.OnLobbyCreateFailed.AddListener(OnLobbyFailed);
        lobbyManager.OnLobbyMembersUpdated.AddListener(UpdatePlayerList);

        Debug.Log("[UI] Listeners added, including OnLobbyMembersUpdated");

        UpdateUI();
    }

    void OnCreateLobby()
    {
        statusText.text = "Creating lobby...";
        lobbyManager.CreateLobby();
    }

    void OnInviteFriends()
    {
        lobbyManager.InviteFriendToLobby();
    }

    void OnLeaveLobby()
    {
        lobbyManager.LeaveLobby();
        ClearPlayerList();
        UpdateUI();
    }

    void OnLobbyCreated(string sessionName)
    {
        statusText.text = "Lobby created! Waiting for players...";
        UpdateUI();
        Invoke(nameof(UpdatePlayerList), 0.5f);
    }

    void OnLobbyJoined(string sessionName)
    {
        statusText.text = "Joined lobby! Connecting...";
        UpdateUI();
        Invoke(nameof(UpdatePlayerList), 0.5f);
    }

    void OnLobbyFailed(string reason)
    {
        statusText.text = $"Failed: {reason}";
        UpdateUI();
    }

    void UpdateUI()
    {
        bool inLobby = lobbyManager.IsInLobby;

        createLobbyButton.interactable = !inLobby;
        inviteFriendsButton.interactable = inLobby;
        leaveLobbyButton.interactable = inLobby;

        if (!inLobby)
        {
            statusText.text = "Not in lobby";
        }
    }

    void UpdatePlayerList()
    {
        Debug.Log("[UI] ========== UPDATE PLAYER LIST ==========");
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
        }

        Debug.Log($"[UI] Created {activeMemberUIs.Count} UI elements");
        Debug.Log("[UI] ========================================");

        Canvas.ForceUpdateCanvases();
    }

    void ClearPlayerList()
    {
        foreach (var memberUI in activeMemberUIs)
        {
            if (memberUI != null)
            {
                Destroy(memberUI.gameObject);
            }
        }
        activeMemberUIs.Clear();
    }

    void OnDestroy()
    {
        lobbyManager.OnPhotonSessionCreated.RemoveListener(OnLobbyCreated);
        lobbyManager.OnPhotonSessionJoined.RemoveListener(OnLobbyJoined);
        lobbyManager.OnLobbyCreateFailed.RemoveListener(OnLobbyFailed);
        lobbyManager.OnLobbyMembersUpdated.RemoveListener(UpdatePlayerList);
    }
}