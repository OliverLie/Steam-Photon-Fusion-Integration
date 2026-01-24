using UnityEngine;
using System.Collections.Generic;

public class LobbyMemberListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SteamLobbyManager lobbyManager;
    [SerializeField] private Transform memberListContainer;
    [SerializeField] private LobbyMemberUI memberPrefab;

    private List<LobbyMemberUI> activeMemberUIs = new List<LobbyMemberUI>();

    void Start()
    {
        // Lyt til lobby updates
        lobbyManager.OnPhotonSessionCreated.AddListener(OnLobbyChanged);
        lobbyManager.OnPhotonSessionJoined.AddListener(OnLobbyChanged);
        lobbyManager.OnLobbyMembersUpdated.AddListener(UpdateMemberList);
    }

    void OnLobbyChanged(string sessionName)
    {
        // Når vi joiner/creater lobby, update listen
        Invoke(nameof(UpdateMemberList), 0.5f); // Lille delay så Steam kan opdatere
    }

    public void UpdateMemberList()
    {
        // Clear existing UI
        foreach (var memberUI in activeMemberUIs)
        {
            Destroy(memberUI.gameObject);
        }
        activeMemberUIs.Clear();

        // Get current lobby members
        LobbyMemberData[] members = lobbyManager.GetLobbyMembers();

        Debug.Log($"[UI] Updating lobby member list: {members.Length} members");

        // Create UI for each member
        foreach (var member in members)
        {
            LobbyMemberUI memberUI = Instantiate(memberPrefab, memberListContainer);
            memberUI.SetMemberData(member);
            activeMemberUIs.Add(memberUI);
        }
    }

    void OnDestroy()
    {
        lobbyManager.OnPhotonSessionCreated.RemoveListener(OnLobbyChanged);
        lobbyManager.OnPhotonSessionJoined.RemoveListener(OnLobbyChanged);
        lobbyManager.OnLobbyMembersUpdated.RemoveListener(UpdateMemberList);
    }
}