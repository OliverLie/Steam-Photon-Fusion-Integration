using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyMemberUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject hostCrown; // Optional crown icon

    public void SetMemberData(LobbyMemberData data)
    {
        playerNameText.text = data.playerName;

        if (data.avatar != null)
        {
            avatarImage.sprite = Sprite.Create(
                data.avatar,
                new Rect(0, 0, data.avatar.width, data.avatar.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        if (hostCrown != null)
        {
            hostCrown.SetActive(data.isHost);
        }
    }
}