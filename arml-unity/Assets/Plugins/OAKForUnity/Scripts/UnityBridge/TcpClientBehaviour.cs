/** Basic implementation of TCP socket client using Netly
 *
 *
 * 
 */

using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Netly.Core;
using TcpClient = Netly.TcpClient;
using System.Text;
// Based on TCP Client example provided by Netly

public class TcpClientBehaviour : MonoBehaviour
{
    public string host;
    public int port;
    
    private TcpClient client;
    private Thread clientThread;
    private readonly ManualResetEvent _connectionEstablished = new ManualResetEvent(false);
    private volatile bool _stopConnection;

    private const int ConnectionTimeoutMilliseconds = 10000;
    private const int ConnectionRetryDelayMilliseconds = 250;

    private Texture2D _texture;
    private string _json;
    private byte[] _pendingImageData;
    private byte[] _pendingJsonData;
    private bool _connected;

    private readonly object _receiveLock = new object();
    private readonly MemoryStream _receiveStream = new MemoryStream();
    private bool _receivingImage;
    private bool _hasPendingFrame;
    private int _requestCount;
    private int _receivedChunkCount;
    private int _completedFrameCount;
    private int _processedFrameCount;
    private int _loggedWaitingForFirstJson;
    private const string DELIMITER = "<<END_OF_JSON>>";
    private const string DELIMITER_END = "<<END>>";
    private static readonly byte[] JsonDelimiterBytes = Encoding.ASCII.GetBytes(DELIMITER);
    private static readonly byte[] ImageDelimiterBytes = Encoding.ASCII.GetBytes(DELIMITER_END);

    void Start()
    {
        _connected = false;
        // Initialize the client and connect in a separate thread
        client = new TcpClient(framing: false); 
        
        _texture = new Texture2D(2, 2);
        _json = "";
        
        client.OnOpen(() =>
        {
            if (_stopConnection)
            {
                client.Close();
                return;
            }

            Debug.Log("[UnityBridge TCP] Client connected to " + host + ":" + port + ".");
            _connected = true;
            ResetReceiveState();
            Interlocked.Exchange(ref _loggedWaitingForFirstJson, 0);
            _connectionEstablished.Set();
            RequestFrame("initial connection");
        });

        client.OnClose(() =>
        {
            _connected = false;
            ResetReceiveState();
            Debug.Log("Client disconnected");
        });

        client.OnError((Exception exception) =>
        {
            Debug.LogError("Connection error: " + exception.Message);
        });

        client.OnData((byte[] data) =>
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("[UnityBridge TCP] Received an empty data callback.");
                return;
            }

            int chunkNumber = Interlocked.Increment(ref _receivedChunkCount);
            bool logChunk = chunkNumber <= 10 || chunkNumber % 100 == 0;
            bool jsonDelimiterFound = false;
            int jsonPayloadLength = 0;
            int completedFrameNumber = 0;
            int completedJsonLength = 0;
            int completedImageLength = 0;
            int bufferedLengthAfterParsing;
            string parserStageAfterParsing;

            lock (_receiveLock)
            {
                _receiveStream.Position = _receiveStream.Length;
                _receiveStream.Write(data, 0, data.Length);

                while (true)
                {
                    int bufferedLength = (int)_receiveStream.Length;
                    byte[] bufferedData = _receiveStream.GetBuffer();
                    byte[] delimiter = _receivingImage ? ImageDelimiterBytes : JsonDelimiterBytes;
                    int delimiterIndex = FindDelimiterIndex(bufferedData, bufferedLength, delimiter);
                    if (delimiterIndex < 0)
                    {
                        break;
                    }

                    byte[] payload = new byte[delimiterIndex];
                    Array.Copy(bufferedData, 0, payload, 0, payload.Length);

                    int consumedLength = delimiterIndex + delimiter.Length;
                    int remainingLength = bufferedLength - consumedLength;
                    Array.Copy(bufferedData, consumedLength, bufferedData, 0, remainingLength);
                    _receiveStream.SetLength(remainingLength);
                    _receiveStream.Position = remainingLength;

                    if (!_receivingImage)
                    {
                        _pendingJsonData = payload;
                        _receivingImage = true;
                        jsonDelimiterFound = true;
                        jsonPayloadLength = payload.Length;
                        continue;
                    }

                    _pendingImageData = payload;
                    _receivingImage = false;
                    _hasPendingFrame = true;
                    completedFrameNumber = ++_completedFrameCount;
                    completedJsonLength = _pendingJsonData == null ? 0 : _pendingJsonData.Length;
                    completedImageLength = payload.Length;
                    break;
                }

                bufferedLengthAfterParsing = (int)_receiveStream.Length;
                parserStageAfterParsing = _receivingImage ? "image" : "JSON";
            }
            
        });

        client.OnEvent((string name, byte[] data) =>
        {
            Debug.Log("Event received: " + name);
        });

        client.OnModify((Socket socket) =>
        {
            // Modify socket before opening connection
        });

    }

    // Helper method to find the delimiter in the data
    private static int FindDelimiterIndex(byte[] data, int dataLength, byte[] delimiter)
    {
        for (int i = 0; i <= dataLength - delimiter.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < delimiter.Length; j++)
            {
                if (data[i + j] != delimiter[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1; // Delimiter not found
    }

    private void RequestFrame(string reason)
    {
        int requestNumber = Interlocked.Increment(ref _requestCount);
        client.ToData("DATA");
    }

    private void ResetReceiveState()
    {
        lock (_receiveLock)
        {
            _receiveStream.SetLength(0);
            _receiveStream.Position = 0;
            _receivingImage = false;
            _hasPendingFrame = false;
            _pendingImageData = null;
            _pendingJsonData = null;
        }
    }
    
    public bool InitUB()
    {
        if (_connected)
        {
            return true;
        }

        _stopConnection = false;
        _connectionEstablished.Reset();
        clientThread = new Thread(ConnectWithRetry)
        {
            IsBackground = true
        };
        clientThread.Start();

        if (!_connectionEstablished.WaitOne(ConnectionTimeoutMilliseconds))
        {
            _stopConnection = true;
            Debug.LogError("Timed out waiting for the Unity Bridge connection.");
            return false;
        }

        return _connected;
    }

    private void ConnectWithRetry()
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(ConnectionTimeoutMilliseconds);
        int attempt = 0;

        while (!_stopConnection && !_connected && DateTime.UtcNow < deadline)
        {
            attempt++;

            try
            {
                Debug.Log("Connecting to Unity Bridge (attempt " + attempt + ").");
                client.Open(new Host(host, port));

                if (_connectionEstablished.WaitOne(ConnectionRetryDelayMilliseconds))
                {
                    return;
                }
            }
            catch (SocketException exception)
            {
                Debug.LogWarning("Unity Bridge connection attempt failed: " + exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning("Unity Bridge connection attempt failed: " + exception.Message);
            }

            if (!_connected)
            {
                client.Close();
                Debug.LogWarning("Unity Bridge is not connected; retrying.");
            }

            if (!_stopConnection)
            {
                Thread.Sleep(ConnectionRetryDelayMilliseconds);
            }
        }
    }
    private void Update()
    {
        byte[] imageData;
        byte[] jsonData;
        lock (_receiveLock)
        {
            if (!_hasPendingFrame)
            {
                return;
            }

            imageData = _pendingImageData;
            jsonData = _pendingJsonData;
            _pendingImageData = null; // Clear the data after processing
            _pendingJsonData = null;
            _hasPendingFrame = false;
        }

        bool imageLoaded = _texture.LoadImage(imageData);
        if (!imageLoaded)
        {
            Debug.LogError(
                "[UnityBridge TCP] Failed to decode image payload (" +
                imageData.Length + " bytes).");
        }
        _json = Encoding.UTF8.GetString(jsonData);

        RequestFrame("previous frame processed");
    }

    public string GetResults(out Texture2D texture)
    {
        texture = _texture;
        if (string.IsNullOrEmpty(_json) &&
            Interlocked.Exchange(ref _loggedWaitingForFirstJson, 1) == 0)
        {
            Debug.Log(
                "[UnityBridge TCP] GetResults is being polled, but no complete JSON/image frame has been processed yet.");
        }

        return _json;
    }

    public void CloseUB()
    {
        _stopConnection = true;
        _connectionEstablished.Set();
        _connected = false;

        // Close the client
        if (client != null)
        {
            client.Close();
        }
        if (clientThread != null && clientThread.IsAlive)
        {
            clientThread.Abort();
        }
    }
    
    void OnDestroy()
    {
        CloseUB();
    }
}
