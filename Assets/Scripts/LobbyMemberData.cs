using Steamworks;
using UnityEngine;

[System.Serializable]
public class LobbyMemberData
{
    public CSteamID steamID;
    public string playerName;
    public Texture2D avatar;
    public bool isHost;

    public LobbyMemberData(CSteamID id)
    {
        steamID = id;
        playerName = SteamFriends.GetFriendPersonaName(id);
        isHost = false;
    }
}