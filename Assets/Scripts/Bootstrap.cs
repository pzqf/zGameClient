using UnityEngine;

namespace GameClient
{
    public class Bootstrap : MonoBehaviour
    {
        public GameObject networkManagerPrefab;
        public GameObject gameManagerPrefab;

        private void Awake()
        {
            if (Network.NetworkManager.Instance == null && networkManagerPrefab != null)
            {
                Instantiate(networkManagerPrefab);
            }

            if (Managers.GameManager.Instance == null && gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
            }
        }
    }
}
