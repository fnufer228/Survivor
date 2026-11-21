using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NicknameMenu : MonoBehaviour
{
    public const int MaxLength = 16;

    [SerializeField] private InputField nicknameInput;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button[] cancelButtons;
    [SerializeField] private TMP_Text editNicknameLabel;

    private static string sessionNickname;
    private static string defaultNickname;

    public static string DefaultNickname
    {
        get
        {
            if (string.IsNullOrEmpty(defaultNickname)) defaultNickname = "Player" + Random.Range(0, 1000000);
            return defaultNickname;
        }
    }

    public static string CurrentNickname => string.IsNullOrEmpty(sessionNickname) ? DefaultNickname : sessionNickname;

    private void Start()
    {
        PhotonNetwork.NickName = CurrentNickname;
        if (nicknameInput != null)
        {
            nicknameInput.characterLimit = MaxLength;
            nicknameInput.text = sessionNickname ?? string.Empty;
        }
        if (confirmButton != null) confirmButton.onClick.AddListener(Save);
        if (cancelButtons != null)
            foreach (var b in cancelButtons)
                if (b != null) b.onClick.AddListener(Revert);
        UpdateLabel();
    }

    public void Save()
    {
        if (nicknameInput == null) return;
        string value = nicknameInput.text.Trim();
        if (value.Length > MaxLength) value = value.Substring(0, MaxLength);
        if (string.IsNullOrEmpty(value))
        {
            Revert();
            return;
        }
        sessionNickname = value;
        PhotonNetwork.NickName = value;
        nicknameInput.text = value;
        UpdateLabel();
    }

    public void Revert()
    {
        if (nicknameInput != null) nicknameInput.text = sessionNickname ?? string.Empty;
    }

    private void UpdateLabel()
    {
        if (editNicknameLabel != null) editNicknameLabel.text = CurrentNickname;
    }
}
