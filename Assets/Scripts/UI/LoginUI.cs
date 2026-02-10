using System;
using System.Collections.Generic;
using Protocol;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("登录面板")]
        public InputField accountInput;
        public InputField passwordInput;
        public Button loginButton;
        public Button createAccountButton;
        public Text statusText;

        [Header("创建账号面板")]
        public GameObject createAccountPanel;
        public InputField newAccountInput;
        public InputField newPasswordInput;
        public InputField confirmPasswordInput;
        public Button confirmCreateButton;
        public Button cancelCreateButton;

        [Header("角色选择面板")]
        public GameObject characterSelectPanel;
        public Transform characterListContent;
        public GameObject characterItemPrefab;
        public Button createCharacterButton;
        public Button enterGameButton;

        [Header("创建角色面板")]
        public GameObject createCharacterPanel;
        public InputField characterNameInput;
        public Dropdown sexDropdown;
        public Button confirmCreateCharacterButton;
        public Button cancelCreateCharacterButton;

        private PlayerInfo _selectedCharacter;
        private bool _isProcessing;

        private void Start()
        {
            loginButton.onClick.AddListener(OnLoginClick);
            createAccountButton.onClick.AddListener(OnCreateAccountClick);
            confirmCreateButton.onClick.AddListener(OnConfirmCreateClick);
            cancelCreateButton.onClick.AddListener(OnCancelCreateClick);
            createCharacterButton.onClick.AddListener(OnCreateCharacterClick);
            enterGameButton.onClick.AddListener(OnEnterGameClick);
            confirmCreateCharacterButton.onClick.AddListener(OnConfirmCreateCharacterClick);
            cancelCreateCharacterButton.onClick.AddListener(OnCancelCreateCharacterClick);

            Managers.GameManager.Instance.OnAccountLoginResponse += OnAccountLoginResponse;
            Managers.GameManager.Instance.OnAccountCreateResponse += OnAccountCreateResponse;
            Managers.GameManager.Instance.OnPlayerCreateResponse += OnPlayerCreateResponse;
            Managers.GameManager.Instance.OnPlayerLoginResponse += OnPlayerLoginResponse;

            Network.NetworkManager.Instance.OnConnected += OnConnected;

            if (Network.NetworkManager.Instance.IsConnected)
            {
                SetStatus("服务器已连接", Color.green);
            }
            else
            {
                SetStatus("正在连接服务器...", Color.yellow);
            }
        }

        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnAccountLoginResponse -= OnAccountLoginResponse;
                Managers.GameManager.Instance.OnAccountCreateResponse -= OnAccountCreateResponse;
                Managers.GameManager.Instance.OnPlayerCreateResponse -= OnPlayerCreateResponse;
                Managers.GameManager.Instance.OnPlayerLoginResponse -= OnPlayerLoginResponse;
            }

            if (Network.NetworkManager.Instance != null)
            {
                Network.NetworkManager.Instance.OnConnected -= OnConnected;
            }
        }

        private void OnConnected()
        {
            SetStatus("服务器连接成功", Color.green);
        }

        private async void OnLoginClick()
        {
            if (_isProcessing)
            {
                return;
            }

            if (string.IsNullOrEmpty(accountInput.text) || string.IsNullOrEmpty(passwordInput.text))
            {
                SetStatus("请输入账号和密码", Color.red);
                return;
            }

            _isProcessing = true;
            try
            {
                if (!Network.NetworkManager.Instance.IsConnected)
                {
                    SetStatus("正在连接服务器...", Color.yellow);
                    bool connected = await Network.NetworkManager.Instance.ConnectAsync();
                    if (!connected)
                    {
                        SetStatus("连接服务器失败", Color.red);
                        return;
                    }
                    SetStatus("服务器连接成功", Color.green);
                }

                SetStatus("正在登录...", Color.yellow);

                bool success = await Managers.GameManager.Instance.SendAccountLogin(accountInput.text, passwordInput.text);
                if (!success)
                {
                    SetStatus("登录请求失败", Color.red);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnAccountLoginResponse(AccountLoginResponse response)
        {
            Debug.Log($"AccountLoginResponse: Success={response.Success}, Players={response.Players?.Count ?? 0}");
            
            if (response.Success)
            {
                SetStatus("登录成功", Color.green);

                if (response.Players != null && response.Players.Count > 0)
                {
                    Debug.Log($"Showing character select with {response.Players.Count} players");
                    ShowCharacterSelect(response.Players);
                }
                else
                {
                    Debug.Log("No players found, showing create character");
                    ShowCreateCharacter();
                }
            }
            else
            {
                SetStatus(response.ErrorMsg, Color.red);
            }
        }

        private void OnCreateAccountClick()
        {
            createAccountPanel.SetActive(true);
        }

        private async void OnConfirmCreateClick()
        {
            if (_isProcessing)
            {
                return;
            }

            if (string.IsNullOrEmpty(newAccountInput.text) || string.IsNullOrEmpty(newPasswordInput.text))
            {
                SetStatus("请输入账号和密码", Color.red);
                return;
            }

            if (newPasswordInput.text != confirmPasswordInput.text)
            {
                SetStatus("两次密码不一致", Color.red);
                return;
            }

            if (!Network.NetworkManager.Instance.IsConnected)
            {
                SetStatus("请先登录连接服务器", Color.red);
                return;
            }

            _isProcessing = true;
            try
            {
                SetStatus("正在创建账号...", Color.yellow);

                bool success = await Managers.GameManager.Instance.SendAccountCreate(newAccountInput.text, newPasswordInput.text);
                if (!success)
                {
                    SetStatus("创建账号请求失败", Color.red);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnAccountCreateResponse(AccountCreateResponse response)
        {
            if (response.Success)
            {
                SetStatus("账号创建成功，请登录", Color.green);
                createAccountPanel.SetActive(false);
                accountInput.text = newAccountInput.text;
                passwordInput.text = newPasswordInput.text;
            }
            else
            {
                SetStatus(response.ErrorMsg, Color.red);
            }
        }

        private void OnCancelCreateClick()
        {
            createAccountPanel.SetActive(false);
        }

        private void ShowCharacterSelect(System.Collections.Generic.IEnumerable<PlayerInfo> players)
        {
            Debug.Log("ShowCharacterSelect called");
            characterSelectPanel.SetActive(true);

            if (characterListContent == null)
            {
                Debug.LogError("characterListContent is null!");
                return;
            }

            if (characterItemPrefab == null)
            {
                Debug.LogError("characterItemPrefab is null!");
                return;
            }

            foreach (Transform child in characterListContent)
            {
                Destroy(child.gameObject);
            }

            int playerCount = 0;
            foreach (var player in players)
            {
                playerCount++;
                Debug.Log($"Creating UI for player: {player.Name}, Level: {player.Level}");
                
                var item = Instantiate(characterItemPrefab, characterListContent);
                item.name = $"CharacterItem_{player.PlayerId}";
                item.SetActive(true);
                
                var allTexts = item.GetComponentsInChildren<Text>(true);
                Text nameText = null;
                Text levelText = null;
                
                foreach (var t in allTexts)
                {
                    t.enabled = true;
                    t.gameObject.SetActive(true);
                    
                    if (t.gameObject.name.Contains("Name"))
                    {
                        nameText = t;
                    }
                    else if (t.gameObject.name.Contains("Level"))
                    {
                        levelText = t;
                    }
                    Debug.Log($"Found Text: {t.gameObject.name}, text: {t.text}, enabled: {t.enabled}");
                }

                var allButtons = item.GetComponentsInChildren<Button>(true);
                Button selectButton = null;
                foreach (var b in allButtons)
                {
                    b.gameObject.SetActive(true);
                    b.enabled = true;
                    
                    if (b.gameObject.name.Contains("Select") || b.gameObject.name.Contains("Button"))
                    {
                        selectButton = b;
                        break;
                    }
                }

                var allImages = item.GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    img.enabled = true;
                    img.gameObject.SetActive(true);
                }

                if (nameText != null)
                {
                    nameText.text = player.Name;
                    nameText.enabled = true;
                    Debug.Log($"Set name text: {player.Name}");
                }
                else
                {
                    Debug.LogError($"CharacterName Text component not found! Total texts: {allTexts.Length}");
                }

                if (levelText != null)
                {
                    levelText.text = $"Lv.{player.Level}";
                    levelText.enabled = true;
                    Debug.Log($"Set level text: Lv.{player.Level}");
                }
                else
                {
                    Debug.LogError("CharacterLevel Text component not found!");
                }

                if (selectButton != null)
                {
                    selectButton.gameObject.SetActive(true);
                    selectButton.enabled = true;
                    
                    var currentPlayer = player;
                    selectButton.onClick.AddListener(() =>
                    {
                        _selectedCharacter = currentPlayer;
                        Debug.Log($"Selected character: {currentPlayer.Name}");
                    });

                    if (_selectedCharacter == null || player.PlayerId == _selectedCharacter.PlayerId)
                    {
                        _selectedCharacter = player;
                    }
                    
                    Debug.Log($"Button found and configured for {player.Name}");
                }
                else
                {
                    Debug.LogError($"SelectButton component not found for {player.Name}, total buttons: {allButtons.Length}");
                }
            }

            Debug.Log($"Total players in UI: {playerCount}");
        }

        private void OnCreateCharacterClick()
        {
            characterSelectPanel.SetActive(false);
            ShowCreateCharacter();
        }

        private void ShowCreateCharacter()
        {
            createCharacterPanel.SetActive(true);
        }

        private async void OnConfirmCreateCharacterClick()
        {
            if (string.IsNullOrEmpty(characterNameInput.text))
            {
                SetStatus("请输入角色名称", Color.red);
                return;
            }

            if (!Network.NetworkManager.Instance.IsConnected)
            {
                SetStatus("请先登录连接服务器", Color.red);
                return;
            }

            SetStatus("正在创建角色...", Color.yellow);

            bool success = await Managers.GameManager.Instance.SendPlayerCreate(characterNameInput.text, sexDropdown.value);
            if (!success)
            {
                SetStatus("创建角色请求失败", Color.red);
            }
        }

        private void OnPlayerCreateResponse(PlayerCreateResponse response)
        {
            if (response.Success)
            {
                SetStatus("角色创建成功，正在进入游戏...", Color.green);
                createCharacterPanel.SetActive(false);
                Managers.GameManager.Instance.LoadScene("SimpleTownLite/Scenes/Demo");
            }
            else
            {
                SetStatus(response.ErrorMsg, Color.red);
            }
        }

        private void OnCancelCreateCharacterClick()
        {
            createCharacterPanel.SetActive(false);
            if (_selectedCharacter != null)
            {
                characterSelectPanel.SetActive(true);
            }
        }

        private async void OnEnterGameClick()
        {
            if (_selectedCharacter == null)
            {
                SetStatus("请选择一个角色", Color.red);
                return;
            }

            SetStatus("正在进入游戏...", Color.yellow);

            bool success = await Managers.GameManager.Instance.SendPlayerLogin(_selectedCharacter.PlayerId);
            if (!success)
            {
                SetStatus("进入游戏请求失败", Color.red);
            }
        }

        private void OnPlayerLoginResponse(PlayerLoginResponse response)
        {
            if (response.Success)
            {
                SetStatus("进入游戏成功", Color.green);
                Managers.GameManager.Instance.LoadScene("SimpleTownLite/Scenes/Demo");
            }
            else
            {
                SetStatus(response.ErrorMsg, Color.red);
            }
        }

        private void SetStatus(string message, Color color)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}
