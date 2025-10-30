using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Main Buttons")]
    public Button playButton;
    public Button hostButton;
    public Button joinButton;
    public Button cancelButton;
    public Button exitButton;
    public Button startGameButton;

    [Header("UI Elements")]
    public TMP_InputField nameInputField;
    public Button confirmNameButton;
    public TMP_InputField joinInputField;
    public Button confirmJoinButton;
    public TMP_Text lobbyCodeText;
    public GameObject lobbyPanel;
    public GameObject playerNamePrefab;

    private string currentRoomCode;
    private bool inRoom = false;
    private List<GameObject> playerNameObjects = new List<GameObject>();
    private bool pendingCreateRoom = false;

    private const byte MaxPlayersLimit = 8;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        confirmNameButton.onClick.AddListener(OnNameConfirmed);
        playButton.onClick.AddListener(OnPlayPressed);
        hostButton.onClick.AddListener(OnHostPressed);
        joinButton.onClick.AddListener(OnJoinPressed);
        confirmJoinButton.onClick.AddListener(JoinRoomByCode);
        cancelButton.onClick.AddListener(OnCancelPressed);
        exitButton.onClick.AddListener(OnExitPressed);
        startGameButton.onClick.AddListener(OnStartGamePressed);

        ShowNameInputOnly();
    }

    private void ShowNameInputOnly()
    {
        nameInputField.gameObject.SetActive(true);
        confirmNameButton.gameObject.SetActive(true);

        playButton.gameObject.SetActive(false);
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);
        joinInputField.gameObject.SetActive(false);
        confirmJoinButton.gameObject.SetActive(false);
        lobbyPanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        lobbyCodeText.text = "";
    }

    private void OnNameConfirmed()
    {
        string playerName = !string.IsNullOrEmpty(nameInputField.text) ? nameInputField.text : "Player" + Random.Range(1000, 9999);
        PhotonNetwork.NickName = playerName;

        nameInputField.gameObject.SetActive(false);
        confirmNameButton.gameObject.SetActive(false);

        // We do NOT enable the playButton here. We wait for OnJoinedLobby.
        exitButton.gameObject.SetActive(true);

        // Join default lobby immediately
        Debug.Log("[LobbyManager] Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    private void OnPlayPressed()
    {
        playButton.gameObject.SetActive(false);
        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
    }

    private void OnHostPressed()
    {
        if (PhotonNetwork.InLobby)
        {
            CreateRoom();
        }
        else
        {
            // This flag will be checked in OnJoinedLobby
            pendingCreateRoom = true;
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[LobbyManager] Joined lobby successfully.");

        // *** FIX 1: Enable the play button ONLY after we are in the lobby ***
        playButton.gameObject.SetActive(true);

        if (pendingCreateRoom)
        {
            pendingCreateRoom = false;
            CreateRoom();
        }
    }

    private void CreateRoom()
    {
        currentRoomCode = Random.Range(100000, 999999).ToString();
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = MaxPlayersLimit,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(currentRoomCode, options);
        Debug.Log($"[LobbyManager] Creating room {currentRoomCode} with max {MaxPlayersLimit} players");
    }

    public override void OnCreatedRoom() => SetupRoomUI();
    public override void OnJoinedRoom() => SetupRoomUI();

    private void SetupRoomUI()
    {
        inRoom = true;
        
        // *** FIX 3: Get the room name from the network ***
        // This ensures clients who join also see the correct room code.
        currentRoomCode = PhotonNetwork.CurrentRoom.Name;
        lobbyCodeText.text = $"Room Code: {currentRoomCode}";
        
        lobbyPanel.SetActive(true);

        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        joinInputField.gameObject.SetActive(false);
        confirmJoinButton.gameObject.SetActive(false);

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        foreach (var obj in playerNameObjects)
            Destroy(obj);
        playerNameObjects.Clear();

        if (PhotonNetwork.CurrentRoom == null) return;

        var layout = lobbyPanel.GetComponent<LayoutGroup>();
        bool hasLayoutGroup = layout != null;

        float spacing = 50f;
        int index = 0;

        foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
        {
            GameObject playerObj = Instantiate(playerNamePrefab, lobbyPanel.transform);
            TMP_Text text = playerObj.GetComponent<TMP_Text>() ?? playerObj.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = kvp.Value.NickName;

            if (!hasLayoutGroup)
            {
                RectTransform rt = playerObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0, -index * spacing);
                    rt.localScale = Vector3.one;
                }
            }

            playerNameObjects.Add(playerObj);
            index++;
        }
    }

    private void OnStartGamePressed()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("GameScene");
    }

    private void OnJoinPressed()
    {
        joinInputField.gameObject.SetActive(true);
        confirmJoinButton.gameObject.SetActive(true);
        joinInputField.text = "";
    }

    private void JoinRoomByCode()
    {
        string roomCode = joinInputField.text.Trim();
        if (string.IsNullOrEmpty(roomCode)) return;

        if (PhotonNetwork.InLobby)
        {
            Debug.Log($"[LobbyManager] Attempting to join room: {roomCode}");
            // *** FIX 2: Use JoinRoom, not JoinOrCreateRoom ***
            PhotonNetwork.JoinRoom(roomCode);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Tried to join room but not in lobby yet.");
        }
    }

    // *** FIX 2: Add this callback to handle failed join attempts ***
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[LobbyManager] Join room failed: {message} ({returnCode})");
        
        // Optional: Show an error message to the player
        lobbyCodeText.text = "Error: Invalid room code.";
        joinInputField.text = "";
    }

    private void OnCancelPressed()
    {
        if (inRoom)
            PhotonNetwork.LeaveRoom();

        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        joinInputField.gameObject.SetActive(false);
        confirmJoinButton.gameObject.SetActive(false);
        lobbyPanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);
        
        // Only show play button if we are still in the lobby
        playButton.gameObject.SetActive(PhotonNetwork.InLobby);
        
        lobbyCodeText.text = "";
    }

    public override void OnLeftRoom()
    {
        inRoom = false;
        // The rest of the UI cleanup is handled by OnCancelPressed()
    }

    private void OnExitPressed() => Application.Quit();
}