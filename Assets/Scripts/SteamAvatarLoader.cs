using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class SteamAvatarLoader : MonoBehaviour
{
    private static SteamAvatarLoader instance;
    public static SteamAvatarLoader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SteamAvatarLoader");
                instance = go.AddComponent<SteamAvatarLoader>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Dictionary<CSteamID, Texture2D> avatarCache = new Dictionary<CSteamID, Texture2D>();
    private Callback<AvatarImageLoaded_t> avatarLoaded;

    public System.Action<CSteamID, Texture2D> OnAvatarLoaded;

    void Awake()
    {
        avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        CSteamID steamID = callback.m_steamID;
        Debug.Log($"[AVATAR] Avatar loaded for {SteamFriends.GetFriendPersonaName(steamID)}");

        Texture2D avatar = GetSteamAvatar(steamID);

        if (avatar != null)
        {
            avatarCache[steamID] = avatar;
            OnAvatarLoaded?.Invoke(steamID, avatar);
        }
    }

    public Texture2D GetSteamAvatar(CSteamID steamID)
    {
        if (avatarCache.ContainsKey(steamID))
        {
            Debug.Log($"[AVATAR] Returning cached avatar for {steamID}");
            return avatarCache[steamID];
        }

        // Request large avatar (184x184)
        int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamID);

        if (avatarHandle == -1)
        {
            // Avatar is being downloaded asynchronously
            Debug.LogWarning($"[AVATAR] Avatar not ready for {steamID}, will load asynchronously...");
            return CreateDefaultAvatar();
        }

        if (avatarHandle == 0)
        {
            Debug.LogWarning($"[AVATAR] No avatar for {steamID}");
            return CreateDefaultAvatar();
        }

        Texture2D texture = GetTextureFromAvatar(avatarHandle);

        if (texture != null)
        {
            avatarCache[steamID] = texture;
        }

        return texture;
    }

    private Texture2D GetTextureFromAvatar(int avatarHandle)
    {
        bool success = SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height);

        if (!success || width == 0 || height == 0)
        {
            Debug.LogWarning("[AVATAR] Failed to get avatar size");
            return CreateDefaultAvatar();
        }

        byte[] imageBuffer = new byte[width * height * 4];
        success = SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, (int)(width * height * 4));

        if (!success)
        {
            Debug.LogWarning("[AVATAR] Failed to get avatar image data");
            return CreateDefaultAvatar();
        }

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        // Steam provides RGBA data in top-down order, Unity expects bottom-up
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (int)((y * width + x) * 4);

                Color pixel = new Color32(
                    imageBuffer[index],
                    imageBuffer[index + 1],
                    imageBuffer[index + 2],
                    imageBuffer[index + 3]
                );

                texture.SetPixel(x, (int)height - y - 1, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateDefaultAvatar()
    {
        Texture2D texture = new Texture2D(184, 184);
        Color[] pixels = new Color[184 * 184];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.5f, 0.5f, 0.5f);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}