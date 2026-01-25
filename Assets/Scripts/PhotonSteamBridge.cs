using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class PhotonSteamBridge : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private SteamLobbyManager steamLobbyManager;

    [Header("Settings")]
    [SerializeField] private SceneRef targetScene;

    private NetworkRunner runner;
    private bool isStarting = false;

    void Start()
    {
        steamLobbyManager.OnPhotonSessionCreated.AddListener(OnSteamLobbyCreated);
        steamLobbyManager.OnPhotonSessionJoined.AddListener(OnSteamLobbyJoined);
    }

    private async void OnSteamLobbyCreated(string sessionName)
    {
        if (isStarting)
        {
            Debug.LogWarning("Already starting a session, ignoring...");
            return;
        }

        isStarting = true;
        Debug.Log($"[PHOTON] Starting as HOST for session: {sessionName}");

        // Clean up existing runner if present
        if (runner != null)
        {
            Debug.LogWarning("[PHOTON] Runner already exists, shutting it down first...");
            await runner.Shutdown();
            Destroy(runner.gameObject);
            runner = null;
        }

        runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Host";
        runner.AddCallbacks(this);

        Debug.Log("[PHOTON] Starting game...");

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            Scene = targetScene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("[PHOTON] Host started successfully!");
            isStarting = false;
        }
        else
        {
            Debug.LogError($"[PHOTON] Failed to start Host: {result.ShutdownReason}");
            Debug.LogError($"[PHOTON] Error Message: {result.ErrorMessage}");
            isStarting = false;

            steamLobbyManager.LeaveLobby();
        }
    }

    private async void OnSteamLobbyJoined(string sessionName)
    {
        if (isStarting)
        {
            Debug.LogWarning("Already starting a session, ignoring...");
            return;
        }

        isStarting = true;
        Debug.Log($"[PHOTON] Joining as CLIENT for session: {sessionName}");

        // Clean up existing runner if present
        if (runner != null)
        {
            Debug.LogWarning("[PHOTON] Runner already exists, shutting it down first...");
            await runner.Shutdown();
            Destroy(runner.gameObject);
            runner = null;
        }

        runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Client";
        runner.AddCallbacks(this);

        Debug.Log("[PHOTON] Joining game...");

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = targetScene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log("[PHOTON] Client joined successfully!");
            isStarting = false;
        }
        else
        {
            Debug.LogError($"[PHOTON] Failed to join: {result.ShutdownReason}");
            Debug.LogError($"[PHOTON] Error Message: {result.ErrorMessage}");
            isStarting = false;

            steamLobbyManager.LeaveLobby();
        }
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PHOTON] Player joined: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PHOTON] Player left: {player}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"[PHOTON] Shutdown called! Reason: {shutdownReason}");

        if (this.runner == runner)
        {
            this.runner = null;
        }

        steamLobbyManager.LeaveLobby();
        isStarting = false;
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[PHOTON] Connected to server!");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[PHOTON] Disconnected from server! Reason: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[PHOTON] Connect failed! Reason: {reason}");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log($"[PHOTON] Scene load start...");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"[PHOTON] Scene load done!");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    #endregion
}