using UnityEngine;
using Steamworks;
using System.Collections;

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

    public Texture2D GetSteamAvatar(CSteamID steamID)
    {
        // Hent large avatar (184x184)
        int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamID);

        if (avatarHandle == -1)
        {
            Debug.LogWarning($"Avatar not ready for {steamID}, requesting...");
            return null;
        }

        if (avatarHandle == 0)
        {
            Debug.LogWarning($"No avatar for {steamID}");
            return CreateDefaultAvatar();
        }

        return GetTextureFromAvatar(avatarHandle);
    }

    private Texture2D GetTextureFromAvatar(int avatarHandle)
    {
        // Hent avatar dimensioner
        bool success = SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height);

        if (!success || width == 0 || height == 0)
        {
            Debug.LogWarning("Failed to get avatar size");
            return CreateDefaultAvatar();
        }

        // Allokér buffer til RGBA data
        byte[] imageBuffer = new byte[width * height * 4];

        // Hent image data
        success = SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, (int)(width * height * 4));

        if (!success)
        {
            Debug.LogWarning("Failed to get avatar image data");
            return CreateDefaultAvatar();
        }

        // Opret texture
        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        // Load pixel data (Steam giver RGBA, men Unity bruger row-order omvendt)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (int)((y * width + x) * 4);

                Color pixel = new Color32(
                    imageBuffer[index],     // R
                    imageBuffer[index + 1], // G
                    imageBuffer[index + 2], // B
                    imageBuffer[index + 3]  // A
                );

                texture.SetPixel(x, (int)height - y - 1, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateDefaultAvatar()
    {
        // Opret en simpel grå placeholder
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