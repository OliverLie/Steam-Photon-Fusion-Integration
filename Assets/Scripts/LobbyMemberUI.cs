using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbyMemberUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostCrown;

    private CSteamID memberSteamID;

    void Start()
    {
        SteamAvatarLoader.Instance.OnAvatarLoaded += OnAvatarLoaded;
    }

    void OnDestroy()
    {
        SteamAvatarLoader.Instance.OnAvatarLoaded -= OnAvatarLoaded;
    }

    public void SetMemberData(LobbyMemberData data)
    {
        memberSteamID = data.steamID;
        playerNameText.text = data.playerName;
        UpdateAvatar(data.avatar);

        if (hostCrown != null)
        {
            hostCrown.SetActive(data.isHost);
        }
    }

    private void OnAvatarLoaded(CSteamID steamID, Texture2D avatar)
    {
        // Only update if this is the avatar for our displayed player
        if (steamID == memberSteamID)
        {
            Debug.Log($"[UI] Avatar loaded for {SteamFriends.GetFriendPersonaName(steamID)}, updating UI");
            UpdateAvatar(avatar);
        }
    }

    private void UpdateAvatar(Texture2D avatar)
    {
        if (avatar != null && avatarImage != null)
        {
            avatarImage.sprite = Sprite.Create(
                avatar,
                new Rect(0, 0, avatar.width, avatar.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}