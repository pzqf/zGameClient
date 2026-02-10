using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GameClient.Network.Crypto;
using Google.Protobuf;
using UnityEngine;

namespace GameClient.Network
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public string ServerHost = "127.0.0.1";
        public int ServerPort = 8888;
        public float HeartbeatInterval = 5f;

        public bool IsConnected => _client != null && _client.Connected;

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<int, byte[]> OnMessageReceived;

        private TcpClient _client;
        private NetworkStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private Task _heartbeatTask;
        private byte[] _aesKey;

        private readonly object _lock = new();
        private bool _isConnecting;

        private const int HeartbeatProtoId = 0;

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

        private async void Start()
        {
            if (!IsConnected)
            {
                await ConnectAsync();
            }
        }

        private void OnDestroy()
        {
            Disconnect("Application quit");
        }

        public async Task<bool> ConnectAsync()
        {
            if (_isConnecting)
            {
                Debug.LogWarning("Already connecting...");
                return false;
            }

            if (IsConnected)
            {
                Debug.LogWarning("Already connected");
                return true;
            }

            _isConnecting = true;

            try
            {
                Debug.Log($"Connecting to {ServerHost}:{ServerPort}...");

                _client = new TcpClient();
                await _client.ConnectAsync(ServerHost, ServerPort);

                _stream = _client.GetStream();
                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
                _cts = new CancellationTokenSource();

                Debug.Log("Performing ECDH key exchange...");
                _aesKey = ECDH.PerformKeyExchange(_stream);
                Debug.Log($"Key exchange completed, AES key length: {_aesKey.Length}");

                _receiveTask = ReceiveLoop(_cts.Token);
                _heartbeatTask = HeartbeatLoop(_cts.Token);

                Debug.Log("Connected to server!");
                OnConnected?.Invoke();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                Cleanup();
                return false;
            }
            finally
            {
                _isConnecting = false;
            }
        }

        public void Disconnect(string reason = "Disconnected")
        {
            if (!IsConnected && !_isConnecting)
            {
                return;
            }

            Debug.Log($"Disconnecting: {reason}");
            Cleanup();
            OnDisconnected?.Invoke(reason);
        }

        private void Cleanup()
        {
            _cts?.Cancel();

            _reader?.Close();
            _writer?.Close();
            _stream?.Close();
            _client?.Close();

            _reader = null;
            _writer = null;
            _stream = null;
            _client = null;
            _cts = null;
        }

        public async Task<bool> SendAsync(int protoId, byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogError("Not connected to server");
                return false;
            }

            try
            {
                byte[] encryptedData = null;
                if (data != null && data.Length > 0 && _aesKey != null)
                {
                    encryptedData = AES.EncryptGCM(data, _aesKey);
                }

                var packet = new NetPacket(protoId, encryptedData ?? data);
                byte[] bytes = packet.Marshal();

                await _stream.WriteAsync(bytes, 0, bytes.Length);
                await _stream.FlushAsync();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Send failed: {ex.Message}");
                Disconnect($"Send error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMessageAsync<T>(int protoId, T message) where T : IMessage
        {
            byte[] data = message.ToByteArray();
            return await SendAsync(protoId, data);
        }

        public async Task<T> ReceiveMessageAsync<T>(byte[] data) where T : IMessage, new()
        {
            T message = new T();
            message.MergeFrom(data);
            return message;
        }

        private async Task HeartbeatLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    await Task.Delay(TimeSpan.FromSeconds(HeartbeatInterval), cancellationToken);

                    if (!cancellationToken.IsCancellationRequested && IsConnected)
                    {
                        await SendHeartbeat();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"Heartbeat loop error: {ex.Message}");
            }
        }

        private async Task SendHeartbeat()
        {
            try
            {
                var packet = new NetPacket(HeartbeatProtoId, null);
                byte[] bytes = packet.Marshal();

                await _stream.WriteAsync(bytes, 0, bytes.Length);
                await _stream.FlushAsync();

                Debug.Log("Heartbeat sent");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Heartbeat send failed: {ex.Message}");
                Disconnect($"Heartbeat error: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    var packet = await ReadPacketAsync(cancellationToken);
                    if (packet != null)
                    {
                        if (packet.ProtoId == HeartbeatProtoId)
                        {
                            Debug.Log("Heartbeat received from server");
                            continue;
                        }

                        Debug.Log($"Received packet: ProtoId={packet.ProtoId}, DataSize={packet.Data?.Length ?? 0}");

                        byte[] decryptedData = packet.Data;
                        if (packet.Data != null && packet.Data.Length > 0 && _aesKey != null)
                        {
                            try
                            {
                                decryptedData = AES.DecryptGCM(packet.Data, _aesKey);
                                Debug.Log($"Decrypted data length: {decryptedData?.Length ?? 0}");
                            }
                            catch (Exception decryptEx)
                            {
                                Debug.LogError($"Decrypt error: {decryptEx.Message}");
                                continue;
                            }
                        }
                        else if (_aesKey == null)
                        {
                            Debug.LogWarning("AES key is null, skipping decryption");
                        }

                        if (decryptedData != null)
                        {
                            OnMessageReceived?.Invoke(packet.ProtoId, decryptedData);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"Receive loop error: {ex.Message}\n{ex.StackTrace}");
                Disconnect($"Receive error: {ex.Message}");
            }
        }

        private async Task<NetPacket> ReadPacketAsync(CancellationToken cancellationToken)
        {
            if (_stream == null)
            {
                Debug.LogError("Stream is null in ReadPacketAsync");
                return null;
            }

            var headerBuffer = new byte[NetPacket.HeaderSize];
            int headerBytesRead = 0;

            while (headerBytesRead < NetPacket.HeaderSize)
            {
                int bytesRead = await _stream.ReadAsync(
                    headerBuffer,
                    headerBytesRead,
                    NetPacket.HeaderSize - headerBytesRead,
                    cancellationToken);

                if (bytesRead == 0)
                {
                    Disconnect("Server closed connection");
                    return null;
                }

                headerBytesRead += bytesRead;
            }

            var packet = NetPacket.UnmarshalHeader(headerBuffer);
            if (packet == null)
            {
                Debug.LogError($"Failed to unmarshal header, bytes: {BitConverter.ToString(headerBuffer)}");
                Disconnect("Invalid packet header");
                return null;
            }

            Debug.Log($"Packet header parsed: ProtoId={packet.ProtoId}, DataSize={packet.DataSize}");

            if (packet.DataSize > 0)
            {
                packet.Data = new byte[packet.DataSize];
                int dataBytesRead = 0;

                while (dataBytesRead < packet.DataSize)
                {
                    int bytesRead = await _stream.ReadAsync(
                        packet.Data,
                        dataBytesRead,
                        packet.DataSize - dataBytesRead,
                        cancellationToken);

                    if (bytesRead == 0)
                    {
                        Disconnect("Server closed connection during data read");
                        return null;
                    }

                    dataBytesRead += bytesRead;
                }
            }

            return packet;
        }
    }
}
