using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Экран лобби PvP — подключение, создание и вход в комнату по коду.
/// </summary>
public class PvPLobby : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    [SerializeField] private GameObject connectingPanel;   // "Подключение..."
    [SerializeField] private GameObject lobbyPanel;        // кнопки Create/Join
    [SerializeField] private GameObject waitingPanel;      // "Waiting for opponent..."

    [Header("Lobby UI")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button         createRoomButton;
    [SerializeField] private Button         joinRoomButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Waiting UI")]
    [SerializeField] private TextMeshProUGUI roomCodeDisplay;  // показываем код созданной комнаты
    [SerializeField] private Button          cancelButton;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        ShowPanel(connectingPanel);

        createRoomButton.onClick.AddListener(OnCreateRoom);
        joinRoomButton.onClick.AddListener(OnJoinRoom);
        cancelButton.onClick.AddListener(OnCancel);

        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.InRoom)
        {
            // Вернулись после игры — сначала уходим из комнаты
            SetStatus("Leaving game...");
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.JoinedLobby)
        {
            OnJoinedLobby();   // уже в лобби — сразу показываем UI
        }
        else
        {
            // Подключены к мастеру, но ещё не в лобби
            PhotonNetwork.JoinLobby();
        }
    }

    // ── Photon callbacks ──────────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        ShowPanel(lobbyPanel);
        SetStatus("");
    }

    public override void OnCreatedRoom()
    {
        string code = PhotonNetwork.CurrentRoom.Name;
        roomCodeDisplay.text = $"Room code:\n<b>{code}</b>\n\nWaiting for opponent...";
        ShowPanel(waitingPanel);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus($"Failed to create room: {message}");
    }

    public override void OnJoinedRoom()
    {
        // Оба игрока в комнате — мастер-клиент загружает сцену для всех
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(SceneLoader.PvP);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Второй игрок зашёл — запускаем игру
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(SceneLoader.PvP);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus("Room not found. Check the code.");
        ShowPanel(lobbyPanel);
    }

    public override void OnLeftRoom()
    {
        // Вышли из игровой комнаты — возвращаемся на Master Server
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowPanel(connectingPanel);
        PhotonNetwork.ConnectUsingSettings();
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnCreateRoom()
    {
        string code = GenerateRoomCode();
        var options = new RoomOptions { MaxPlayers = 2, IsVisible = false };
        PhotonNetwork.CreateRoom(code, options);
        SetStatus("Creating room...");
    }

    private void OnJoinRoom()
    {
        string code = roomCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Enter room code.");
            return;
        }
        PhotonNetwork.JoinRoom(code);
        SetStatus("Joining...");
    }

    private void OnCancel()
    {
        PhotonNetwork.LeaveRoom();
        ShowPanel(lobbyPanel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateRoomCode()
    {
        return Random.Range(1000, 10000).ToString();
    }

    private void ShowPanel(GameObject panel)
    {
        connectingPanel.SetActive(panel == connectingPanel);
        lobbyPanel.SetActive(panel == lobbyPanel);
        waitingPanel.SetActive(panel == waitingPanel);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
