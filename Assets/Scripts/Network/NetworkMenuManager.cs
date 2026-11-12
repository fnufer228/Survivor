using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Unity.VisualScripting;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Головний менеджер головного меню.
/// 1) Підключається до сервера Photon.
/// 2) За кнопкою "Грати" шукає вільну кімнату (або створює свою).
/// 3) НЕ вантажить ігрову сцену доти, доки в кімнаті не збереться потрібна кількість гравців.
/// </summary>
public class NetworkMenuManager : MonoBehaviourPunCallbacks
{
    public static NetworkMenuManager Instance { get; private set; }
    [Header("Налаштування кімнати")]
    [Tooltip("Скільки гравців треба, щоб почати матч")]
    [SerializeField] private int minPlayersToStart = 2;
    [Tooltip("Максимум гравців у кімнаті")]
    [SerializeField] private int maxPlayers = 4;
    [Tooltip("Назва ігрової сцени (додай її у Build Settings!)")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("UI")]
    [SerializeField] private GameObject menuPanel;      // панель з кнопкою "Грати"
    [SerializeField] private GameObject searchPanel;    // панель "Пошук гравців..."
    [SerializeField] private TextMeshProUGUI txtStatus; // текст статусу
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnCancel;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private GameObject enterPasswordPanel;

    private const string LockedKey = "locked";
    public const string PwdKey = "pwd";


    private void Awake()
    {
        Instance = this;
        // Обов'язково: майстер-клієнт вантажить сцену, а всі інші повторюють за ним.
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        HideEnterPasswordPanel();
        ShowSearch(false);
        SetStatus("Connecting to server...");
        SetPlayButton(false);

        if (btnPlay != null) btnPlay.onClick.AddListener(OnPlayClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
        
        // Дані для підключення беруться з PhotonServerSettings
        PhotonNetwork.ConnectUsingSettings();
    }

    public void ShowEnterPasswordPanel()
    {
        enterPasswordPanel.SetActive(true);
    }

    public void HideEnterPasswordPanel()
    {
        enterPasswordPanel.SetActive(false);
    }

    public static bool IsLocked(RoomInfo info)
    {
        if (info == null || info.CustomProperties == null)
            return false;

        return info.CustomProperties.TryGetValue(LockedKey, out object v) && v is bool b && b;
    }
    // ---------- ПІДКЛЮЧЕННЯ ----------

    public override void OnConnectedToMaster()
    {
        SetStatus("Connected. Ready to play!");
        SetPlayButton(true);
        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[GameManager] Disconnected: {cause}");
        SetStatus("No connection. Reconnecting...");
        SetPlayButton(false);
        ShowSearch(false);
        PhotonNetwork.ConnectUsingSettings();
    }

    // ---------- ПОШУК КІМНАТИ ----------

    private void OnPlayClicked()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        SetPlayButton(false);
        ShowSearch(true);
        SetStatus("Searching for a room...");

        // Спершу пробуємо зайти у вже існуючу кімнату
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Вільних кімнат немає, тому створюємо свою і чекаємо в ній інших гравців
        Debug.Log("[GameManager] No free rooms, creating a new one");

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsVisible = true,
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(null, options); // null = випадкова назва
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    private void OnCancelClicked()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        ShowSearch(false);
        SetStatus("Search cancelled");
        SetPlayButton(true);
    }

    public void CreateRoom()
    {
        string roomName = roomNameInput.text;
        int maxPlayers = int.Parse(maxPlayersInput.text);
        
        bool hasPassword = !string.IsNullOrEmpty(passwordInput.text);
        
        Hashtable props = new Hashtable { { LockedKey, hasPassword } };
        if (hasPassword)
        {
            props[PwdKey] = passwordInput.text;
            Debug.Log(passwordInput.text);
        }
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new[] { LockedKey, PwdKey }
        };
        PhotonNetwork.CreateRoom(roomName, options);
    }
    private void SetStatus(string text)
    {
        if (txtStatus != null) txtStatus.text = text;
    }

    private void SetPlayButton(bool active)
    {
        if (btnPlay != null) btnPlay.interactable = active;
    }

    private void ShowSearch(bool searching)
    {
        if (menuPanel != null) menuPanel.SetActive(!searching);
        if (searchPanel != null) searchPanel.SetActive(searching);
    }
}
