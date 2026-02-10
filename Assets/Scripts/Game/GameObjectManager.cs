using System.Collections.Generic;
using Protocol;
using UnityEngine;

namespace GameClient.Game
{
    public class GameObjectManager : MonoBehaviour
    {
        public static GameObjectManager Instance { get; private set; }

        public GameObject playerPrefab;
        public GameObject monsterPrefab;

        private readonly Dictionary<long, GameObject> _gameObjects = new Dictionary<long, GameObject>();
        private GameObject _localPlayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (playerPrefab == null)
            {
                playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
            }

            if (monsterPrefab == null)
            {
                monsterPrefab = Resources.Load<GameObject>("Prefabs/Monster");
            }

            Managers.GameManager.Instance.OnMapSyncObjects += OnMapSyncObjects;
            Managers.GameManager.Instance.OnPlayerLoginResponse += OnPlayerLoginResponse;
        }

        private void OnPlayerLoginResponse(PlayerLoginResponse response)
        {
            if (response.Success)
            {
                Managers.GameManager.Instance.CurrentMapId = 1;
            }
        }

        private void OnMapSyncObjects(MapSyncObjects sync)
        {
            if (sync == null || sync.Objects == null)
            {
                return;
            }

            foreach (var objInfo in sync.Objects)
            {
                UpdateObject(objInfo);
            }
        }

        private void UpdateObject(MapObjectInfo objInfo)
        {
            if (_gameObjects.TryGetValue(objInfo.ObjectId, out var existingObj))
            {
                UpdateExistingObject(existingObj, objInfo);
            }
            else
            {
                CreateNewObject(objInfo);
            }
        }

        private void UpdateExistingObject(GameObject obj, MapObjectInfo objInfo)
        {
            var position = new Vector3(objInfo.X, objInfo.Y, objInfo.Z);
            obj.transform.position = position;

            if (objInfo.Orientation != 0)
            {
                obj.transform.rotation = Quaternion.Euler(0, objInfo.Orientation, 0);
            }

            if (objInfo.ObjectType == 2)
            {
                var monsterController = obj.GetComponent<MonsterController>();
                if (monsterController != null)
                {
                    monsterController.SetPosition(position);
                }
            }
        }

        private void CreateNewObject(MapObjectInfo objInfo)
        {
            GameObject prefab = null;
            string objectName = "";

            switch (objInfo.ObjectType)
            {
                case 1:
                    prefab = playerPrefab;
                    objectName = $"Player_{objInfo.ObjectId}";
                    break;
                case 2:
                    prefab = monsterPrefab;
                    objectName = $"Monster_{objInfo.ObjectId}";
                    break;
            }

            if (prefab == null)
            {
                Debug.LogWarning($"No prefab found for object type: {objInfo.ObjectType}");
                return;
            }

            var position = new Vector3(objInfo.X, objInfo.Y, objInfo.Z);
            var newObj = Instantiate(prefab, position, Quaternion.identity);
            newObj.name = objectName;

            if (objInfo.Orientation != 0)
            {
                newObj.transform.rotation = Quaternion.Euler(0, objInfo.Orientation, 0);
            }

            if (objInfo.ObjectType == 1)
            {
                var playerController = newObj.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController = newObj.AddComponent<PlayerController>();
                    Debug.Log($"Added PlayerController to {objectName}");
                }
                playerController.SetPosition(position);

                if (objInfo.ObjectId == Managers.GameManager.Instance.PlayerId)
                {
                    _localPlayer = newObj;
                    SetupLocalPlayer(newObj);
                }
            }
            else if (objInfo.ObjectType == 2)
            {
                var monsterController = newObj.GetComponent<MonsterController>();
                if (monsterController == null)
                {
                    monsterController = newObj.AddComponent<MonsterController>();
                    Debug.Log($"Added MonsterController to {objectName}");
                }
                monsterController.monsterId = objInfo.ObjectId;
                monsterController.SetPosition(position);
            }

            _gameObjects[objInfo.ObjectId] = newObj;
            Debug.Log($"Created new object: {objectName} at {position}");
        }

        private void SetupLocalPlayer(GameObject playerObj)
        {
            var followCamera = Camera.main;
            if (followCamera == null)
            {
                followCamera = FindObjectOfType<Camera>();
                if (followCamera != null)
                {
                    Debug.LogWarning("Camera.main is null, using FindObjectOfType<Camera>()");
                }
            }

            if (followCamera != null)
            {
                var cameraFollow = followCamera.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                {
                    cameraFollow = followCamera.gameObject.AddComponent<CameraFollow>();
                    Debug.Log("Added CameraFollow component to camera");
                }
                cameraFollow.target = playerObj.transform;
                Debug.Log($"CameraFollow target set to: {playerObj.name}");
            }
            else
            {
                Debug.LogError("SetupLocalPlayer: No camera found in scene!");
            }

            var playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.followCamera = followCamera;
            }
        }

        public void RemoveObject(long objectId)
        {
            if (_gameObjects.TryGetValue(objectId, out var obj))
            {
                Destroy(obj);
                _gameObjects.Remove(objectId);
                Debug.Log($"Removed object: {objectId}");
            }
        }

        public GameObject GetLocalPlayer()
        {
            return _localPlayer;
        }

        public GameObject GetObject(long objectId)
        {
            _gameObjects.TryGetValue(objectId, out var obj);
            return obj;
        }

        public void ClearAllObjects()
        {
            foreach (var obj in _gameObjects.Values)
            {
                Destroy(obj);
            }
            _gameObjects.Clear();
            _localPlayer = null;
        }
    }
}