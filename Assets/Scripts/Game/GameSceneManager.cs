using System.Collections.Generic;
using Protocol;
using UnityEngine;
using UnityEngine.AI;

namespace GameClient.Game
{
    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance { get; private set; }

        public GameObject playerPrefab;
        public GameObject monsterPrefab;
        public Transform playerSpawnPoint;

        private GameObject _localPlayer;
        private readonly Dictionary<long, GameObject> _monsters = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SpawnLocalPlayer();

            Managers.GameManager.Instance.OnMapSyncObjects += OnMapSyncObjects;
        }

        private void OnDestroy()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnMapSyncObjects -= OnMapSyncObjects;
            }
        }

        private void SpawnLocalPlayer()
        {
            if (playerPrefab != null && playerSpawnPoint != null)
            {
                _localPlayer = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                _localPlayer.name = $"Player_{Managers.GameManager.Instance.PlayerName}";

                var controller = _localPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        var followCam = camera.GetComponent<CameraFollow>();
                        if (followCam != null)
                        {
                            followCam.target = _localPlayer.transform;
                        }
                    }
                }
            }
        }

        private void OnMapSyncObjects(MapSyncObjects sync)
        {
            foreach (var objInfo in sync.Objects)
            {
                if (objInfo.ObjectType == 1)
                {
                    UpdateMonster(objInfo);
                }
            }
        }

        private void UpdateMonster(MapObjectInfo objInfo)
        {
            if (_monsters.TryGetValue(objInfo.ObjectId, out var monster))
            {
                monster.transform.position = new Vector3(objInfo.X, objInfo.Y, objInfo.Z);
            }
            else
            {
                SpawnMonster(objInfo);
            }
        }

        private void SpawnMonster(MapObjectInfo objInfo)
        {
            if (monsterPrefab != null)
            {
                var position = new Vector3(objInfo.X, objInfo.Y, objInfo.Z);
                var monster = Instantiate(monsterPrefab, position, Quaternion.identity);
                monster.name = $"Monster_{objInfo.ObjectId}";

                var controller = monster.GetComponent<MonsterController>();
                if (controller != null)
                {
                    controller.monsterId = objInfo.ObjectId;
                    controller.monsterName = $"Monster_{objInfo.ObjectId}";
                }

                _monsters[objInfo.ObjectId] = monster;
            }
        }

        public GameObject GetLocalPlayer()
        {
            return _localPlayer;
        }

        public void AttackMonster(long monsterId)
        {
            if (_monsters.TryGetValue(monsterId, out var monster))
            {
                var monsterCtrl = monster.GetComponent<MonsterController>();
                if (monsterCtrl != null)
                {
                    monsterCtrl.TakeDamage(20f);
                }
            }
        }
    }
}
