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
        lobbyManager.OnPhotonSessionCreated.AddListener(OnLobbyChanged);
        lobbyManager.OnPhotonSessionJoined.AddListener(OnLobbyChanged);
        lobbyManager.OnLobbyMembersUpdated.AddListener(UpdateMemberList);
    }

    void OnLobbyChanged(string sessionName)
    {
        // Delay allows Steam to update member list before rendering
        Invoke(nameof(UpdateMemberList), 0.5f);
    }

    public void UpdateMemberList()
    {
        foreach (var memberUI in activeMemberUIs)
        {
            Destroy(memberUI.gameObject);
        }
        activeMemberUIs.Clear();

        LobbyMemberData[] members = lobbyManager.GetLobbyMembers();
        Debug.Log($"[UI] Updating lobby member list: {members.Length} members");

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