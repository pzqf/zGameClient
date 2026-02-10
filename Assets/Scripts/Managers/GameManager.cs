using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameClient.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int PlayerLevel { get; set; }
        public long PlayerGold { get; set; }

        public long CurrentMapId { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            InitMessageHandlers();
        }

        private void InitMessageHandlers()
        {
            Network.NetworkManager.Instance.OnMessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(int protoId, byte[] data)
        {
            switch (protoId)
            {
                case 1001:
                    HandleAccountCreateResponse(data);
                    break;
                case 1002:
                    HandleAccountLoginResponse(data);
                    break;
                case 1003:
                    HandlePlayerCreateResponse(data);
                    break;
                case 1004:
                    HandlePlayerLoginResponse(data);
                    break;
                case 1005:
                    HandlePlayerLogoutResponse(data);
                    break;
                case 4003:
                    HandleMapMoveResponse(data);
                    break;
                case 4006:
                    HandleMapSyncObjects(data);
                    break;
                default:
                    Debug.LogWarning($"Unknown message ID: {protoId}");
                    break;
            }
        }

        public event Action<AccountCreateResponse> OnAccountCreateResponse;
        public event Action<AccountLoginResponse> OnAccountLoginResponse;
        public event Action<PlayerCreateResponse> OnPlayerCreateResponse;
        public event Action<PlayerLoginResponse> OnPlayerLoginResponse;
        public event Action<PlayerLogoutResponse> OnPlayerLogoutResponse;
        public event Action<MapMoveResponse> OnMapMoveResponse;
        public event Action<MapSyncObjects> OnMapSyncObjects;

        private void HandleAccountCreateResponse(byte[] data)
        {
            var response = AccountCreateResponse.Parser.ParseFrom(data);
            OnAccountCreateResponse?.Invoke(response);
        }

        private void HandleAccountLoginResponse(byte[] data)
        {
            Debug.Log($"HandleAccountLoginResponse: data length={data?.Length ?? 0}");
            var response = AccountLoginResponse.Parser.ParseFrom(data);
            Debug.Log($"AccountLoginResponse parsed: Success={response.Success}, PlayersCount={response.Players?.Count ?? 0}");
            OnAccountLoginResponse?.Invoke(response);
        }

        private void HandlePlayerCreateResponse(byte[] data)
        {
            var response = PlayerCreateResponse.Parser.ParseFrom(data);
            OnPlayerCreateResponse?.Invoke(response);
        }

        private void HandlePlayerLoginResponse(byte[] data)
        {
            var response = PlayerLoginResponse.Parser.ParseFrom(data);

            if (response.Success)
            {
                PlayerId = response.PlayerId;
                PlayerName = response.Name;
                PlayerLevel = response.Level;
                PlayerGold = response.Gold;
            }

            OnPlayerLoginResponse?.Invoke(response);
        }

        private void HandlePlayerLogoutResponse(byte[] data)
        {
            var response = PlayerLogoutResponse.Parser.ParseFrom(data);

            if (response.Success)
            {
                PlayerId = 0;
                PlayerName = null;
                PlayerLevel = 0;
                PlayerGold = 0;
            }

            OnPlayerLogoutResponse?.Invoke(response);
        }

        private void HandleMapMoveResponse(byte[] data)
        {
            var response = MapMoveResponse.Parser.ParseFrom(data);
            OnMapMoveResponse?.Invoke(response);
        }

        private void HandleMapSyncObjects(byte[] data)
        {
            var sync = MapSyncObjects.Parser.ParseFrom(data);
            OnMapSyncObjects?.Invoke(sync);
        }

        public async Task<bool> SendAccountCreate(string account, string password)
        {
            var request = new AccountCreateRequest
            {
                Account = account,
                Password = password,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                DeviceType = 0,
                Version = Application.version
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(1001, request);
        }

        public async Task<bool> SendAccountLogin(string account, string password)
        {
            var request = new AccountLoginRequest
            {
                Account = account,
                Password = password,
                DeviceId = SystemInfo.deviceUniqueIdentifier,
                DeviceType = 0,
                Version = Application.version
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(1002, request);
        }

        public async Task<bool> SendPlayerCreate(string name, int sex)
        {
            var request = new PlayerCreateRequest
            {
                Name = name,
                Sex = sex,
                Age = 0
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(1003, request);
        }

        public async Task<bool> SendPlayerLogin(long playerId)
        {
            var request = new PlayerLoginRequest
            {
                PlayerId = playerId
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(1004, request);
        }

        public async Task<bool> SendPlayerLogout()
        {
            var request = new PlayerLogoutRequest
            {
                PlayerId = PlayerId
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(1005, request);
        }

        public async Task<bool> SendMove(float x, float y, float z)
        {
            var request = new MapMoveRequest
            {
                MapId = CurrentMapId,
                X = x,
                Y = y,
                Z = z,
                Orientation = 0
            };

            return await Network.NetworkManager.Instance.SendMessageAsync(4003, request);
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(sceneName);
        }

        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            StartCoroutine(LoadSceneCoroutine(sceneName, onComplete));
        }

        private System.Collections.IEnumerator LoadSceneCoroutine(string sceneName, Action onComplete)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncOp.isDone)
            {
                yield return null;
            }
            onComplete?.Invoke();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            InitializeSceneManagers();
        }

        private void InitializeSceneManagers()
        {
            if (Game.GameObjectManager.Instance == null)
            {
                var go = new GameObject("GameObjectManager");
                go.AddComponent<Game.GameObjectManager>();
                Debug.Log("Created GameObjectManager in scene");
            }

            if (Camera.main != null && Camera.main.GetComponent<Game.CameraFollow>() == null)
            {
                Camera.main.gameObject.AddComponent<Game.CameraFollow>();
                Debug.Log("Added CameraFollow to main camera");
            }
        }
    }
}
