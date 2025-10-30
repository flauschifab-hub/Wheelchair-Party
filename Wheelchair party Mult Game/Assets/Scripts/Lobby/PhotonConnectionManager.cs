using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonConnectionManager : MonoBehaviourPunCallbacks
{
    [Header("Optional Settings")]
    public bool autoConnect = true;   // Connect automatically on Start
    public string gameVersion = "1";  // Versioning for matchmaking

    public static bool IsConnected => PhotonNetwork.IsConnectedAndReady;

    public delegate void ConnectionEvent();
    public event ConnectionEvent OnConnectedToPhotonEvent;

    private void Start()
    {
        if (autoConnect && !PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }
    }

    public void ConnectToPhoton()
    {
        Debug.Log("[Photon] Connecting to Master server...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] Connected to Master server.");
        OnConnectedToPhotonEvent?.Invoke();

        // Don't join a lobby automatically
        // PhotonNetwork.JoinLobby(); <-- removed
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Photon] Disconnected: {cause}. Attempting to reconnect...");
        ConnectToPhoton();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] Failed to join room: {message} ({returnCode})");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[Photon] Failed to create room: {message} ({returnCode})");
    }
}
