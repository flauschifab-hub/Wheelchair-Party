using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class SteamLobbyManager : MonoBehaviour
{
    [Header("Main Buttons")]
    public Button playButton;
    public Button hostButton;
    public Button joinButton;
    public Button cancelButton;
    public Button exitButton;

    [Header("UI Elements")]
    public TMP_InputField joinInputField;
    public TMP_Text lobbyCodeText;

    private const int MaxPlayers = 4;

    private Callback<LobbyCreated_t> lobbyCreatedCallback;
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;

    private CSteamID currentLobbyId;
    private bool inLobby = false;
    private bool joiningLobby = false;

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized!");
            return;
        }

        // Setup button events
        playButton.onClick.AddListener(OnPlayPressed);
        hostButton.onClick.AddListener(CreateLobby);
        joinButton.onClick.AddListener(OnJoinPressed);
        cancelButton.onClick.AddListener(OnCancelPressed);
        exitButton.onClick.AddListener(OnExitPressed);

        // Steam callbacks
        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        // Initial UI setup
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        playButton.gameObject.SetActive(true);
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
        joinInputField.gameObject.SetActive(false);
        lobbyCodeText.text = "";
        inLobby = false;
        joiningLobby = false;
    }

    private void ShowLobbyOptions()
    {
        playButton.gameObject.SetActive(false);
        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
        joinInputField.gameObject.SetActive(false); // hidden until Join is pressed
    }

    private void OnPlayPressed()
    {
        Debug.Log("Play button pressed");
        ShowLobbyOptions();
    }

    private void OnJoinPressed()
    {
        // Show input field for lobby code entry
        joinInputField.gameObject.SetActive(true);
        joinInputField.text = "";
        joiningLobby = true;
        Debug.Log("Join button pressed, waiting for lobby code input...");

        // Once code is entered and Enter key pressed, we can call JoinLobbyByCode manually
        joinInputField.onSubmit.AddListener(delegate { JoinLobbyByCode(); });
    }

    private void OnCancelPressed()
    {
        if (inLobby)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyId);
            Debug.Log("Left lobby.");
        }

        ShowMainMenu();
    }

    private void OnExitPressed()
    {
        Debug.Log("Exiting game...");
        Application.Quit();
    }

    private void CreateLobby()
    {
        string myName = SteamFriends.GetPersonaName();
        Debug.Log($"[{myName}] is creating a lobby...");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxPlayers);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby creation failed!");
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        string myName = SteamFriends.GetPersonaName();

        Debug.Log($"Lobby created successfully by {myName} (ID: {currentLobbyId.m_SteamID})");
        lobbyCodeText.text = $"Lobby Code: {currentLobbyId.m_SteamID}";
        inLobby = true;

        SteamMatchmaking.SetLobbyData(currentLobbyId, "HostID", SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "HostName", myName);
    }

    private void JoinLobbyByCode()
    {
        string input = joinInputField.text.Trim();
        if (ulong.TryParse(input, out ulong lobbyId))
        {
            Debug.Log($"Attempting to join lobby: {lobbyId}");
            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        }
        else
        {
            Debug.LogWarning("Invalid lobby code entered!");
        }
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        string myName = SteamFriends.GetPersonaName();
        string hostName = SteamMatchmaking.GetLobbyData(currentLobbyId, "HostName");

        inLobby = true;
        Debug.Log($"[{myName}] successfully joined lobby {currentLobbyId.m_SteamID}");
        Debug.Log($"Host: {hostName}");

        lobbyCodeText.text = $"In Lobby: {currentLobbyId.m_SteamID}";
        PrintAllLobbyMembers();
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby == currentLobbyId.m_SteamID)
            PrintAllLobbyMembers();
    }

    private void PrintAllLobbyMembers()
    {
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);
        Debug.Log($"Lobby Member Count: {memberCount}");

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i);
            string name = SteamFriends.GetFriendPersonaName(memberId);
            Debug.Log($"Member {i + 1}: {name} ({memberId})");
        }
    }
}
