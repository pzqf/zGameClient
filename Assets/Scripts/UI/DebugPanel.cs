using UnityEngine;

namespace GameClient.UI
{
    public class DebugPanel : MonoBehaviour
    {
        private Vector2 _scrollPos;
        private string _logText = "";
        private bool _showPanel = true;

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error => "[ERROR]",
                LogType.Warning => "[WARN]",
                LogType.Assert => "[ASSERT]",
                LogType.Exception => "[EXCEPTION]",
                _ => "[INFO]"
            };

            _logText += $"{prefix} {condition}\n";
            if (_logText.Length > 10000)
            {
                _logText = _logText.Substring(_logText.Length - 8000);
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button(_showPanel ? "隐藏调试面板" : "显示调试面板", GUILayout.Width(150)))
            {
                _showPanel = !_showPanel;
            }

            if (!_showPanel) return;

            GUILayout.BeginArea(new Rect(10, 40, 400, 400));
            GUILayout.BeginVertical("Box");

            GUILayout.Label("=== 调试面板 ===");

            if (Network.NetworkManager.Instance != null)
            {
                GUILayout.Label($"连接状态: {(Network.NetworkManager.Instance.IsConnected ? "已连接" : "未连接")}");
            }

            if (Managers.GameManager.Instance != null)
            {
                GUILayout.Label($"玩家ID: {Managers.GameManager.Instance.PlayerId}");
                GUILayout.Label($"玩家名称: {Managers.GameManager.Instance.PlayerName}");
                GUILayout.Label($"等级: {Managers.GameManager.Instance.PlayerLevel}");
                GUILayout.Label($"金币: {Managers.GameManager.Instance.PlayerGold}");
            }

            GUILayout.BeginHorizontal();

            if (Network.NetworkManager.Instance != null && !Network.NetworkManager.Instance.IsConnected)
            {
                if (GUILayout.Button("连接服务器"))
                {
                    _ = Network.NetworkManager.Instance.ConnectAsync();
                }
            }

            if (Network.NetworkManager.Instance != null && Network.NetworkManager.Instance.IsConnected)
            {
                if (GUILayout.Button("断开连接"))
                {
                    Network.NetworkManager.Instance.Disconnect();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("=== 日志 ===");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            GUILayout.Label(_logText);
            GUILayout.EndScrollView();

            if (GUILayout.Button("清空日志"))
            {
                _logText = "";
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
