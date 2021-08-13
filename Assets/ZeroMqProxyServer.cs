using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class ZeroMqProxyServer : MonoBehaviour {
    public bool Connected;
    private NetMqPublisher _netMqPublisher;
    private string _response;
    
    private readonly Object _thisLock = new Object();
    private string TAG = "ZeroMqClient.cs: ";

    private Thread _clientThread;
    public SubscriberSocket subSocket;
    public int serverTimeoutSeconds = 3;
    public string zeromqServerIp = "192.168.1.105"; //"192.168.100.103";
    public string zeromqServerPort = "12346"; //"12112";
    public string zeroMqTopic = "plc-tags";
    
    public string proxyServerPort = "12346";
    
    private bool isConnected = false;
    public bool isSubscribed = false;
    public string _latestServerResponse;
    public int subscribeIntervalMs = 1000;
    public string debug;

    public InputField zmqIp;
    public InputField zmqPort;
    public InputField zmqTopic;
    public InputField zmqPubInterval;
    public Text subState;
    public Text latestResponse;
    public InputField proxyPort;
    public Text proxyIp;
    
    public void ToggleServer() {
        if (!isSubscribed) {
            // Create NetMqPublisher object
            _netMqPublisher = new NetMqPublisher();
            // Start the publisher server object
            _netMqPublisher.Start();
            // Connect to remote zeroMQ server
            SubscribeToZeroMqServer();
        }
        else {
            isSubscribed = false;
        }
    }

    private void Start() {
        zmqIp.text = zeromqServerIp;
        zmqPort.text = zeromqServerPort;
        zmqTopic.text = zeroMqTopic;
        zmqPubInterval.text = subscribeIntervalMs.ToString();
        proxyIp.text = LocalIPAddress().ToString();
        proxyPort.text = proxyServerPort;
    }
    
    private IPAddress LocalIPAddress()
    {
        if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
        {
            return null;
        }

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        return host
            .AddressList
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
    }

    private void Update() {
        if(isSubscribed)
            _netMqPublisher.LatestZeroMqData = _latestServerResponse;
        
        subState.text = isSubscribed.ToString();
        latestResponse.text = _latestServerResponse;
        zeromqServerIp = zmqIp.text;
        zeromqServerPort = zmqPort.text;
        zeroMqTopic = zmqTopic.text;
        subscribeIntervalMs = Int32.Parse(zmqPubInterval.text);
        proxyServerPort = proxyPort.text;
    }

    private void OnDestroy() {
        _netMqPublisher.Stop();
    }

    /// <summary>
    /// Runs the NetMqPubSubClient method in a separate thread
    /// </summary>
    public void SubscribeToZeroMqServer() {
        Debug.Log(TAG + "Start a request thread.");
        _clientThread = new Thread(NetMqPubSubClient);
        _clientThread.Start();
        // _clientThread.Join();
    }

    /// <summary>
    /// Establishes a publisher-subscriber connection with the zeroMQ server. Stays subscribed as long as isSubscribed is set to true.
    /// </summary>
    private void NetMqPubSubClient() {
        AsyncIO.ForceDotNet.Force();
        //NetMQConfig.Cleanup();
        var timeout = new System.TimeSpan(0, 0, serverTimeoutSeconds); //1sec

        Debug.Log(TAG + "Attempting to connect to the server: " + zeromqServerIp + ":" + zeromqServerPort);
        subSocket = new SubscriberSocket();
        subSocket.Options.ReceiveHighWatermark = 1000;
        subSocket.Connect("tcp://" + zeromqServerIp + ":" + zeromqServerPort);
        Debug.Log(TAG + "Connected!");
        subSocket.Subscribe(zeroMqTopic);
        Debug.Log(TAG + "Subscribed to topic: " + zeroMqTopic);

        isSubscribed = true;
        
        // Runs indefinitely
        while (isSubscribed) {
            // Recieve response
            bool gotMessage = subSocket.TryReceiveFrameString(timeout, out var msg);

            if (!gotMessage) {
                isConnected = false;
                Debug.Log(TAG + "Failed to get data from server.");
                Debug.Log(TAG + "The server might be unreachable due to the firewall settings.");
                msg = "Server connection failed.";
                _latestServerResponse = msg;
            }
            else {
                isConnected = true;
                Debug.Log(TAG + "Received server response: " + msg);
                msg = msg.Replace(zeroMqTopic, "");
                msg = msg.Replace(" ", "");
                Debug.Log(TAG + "Cleaned up response: " + msg);
                _latestServerResponse = msg;
            }

            Thread.Sleep(subscribeIntervalMs);
        }

        Debug.Log(TAG + "Closing socket.");
        subSocket.Close();
        Debug.Log(TAG + "Cleaning up MQ config.");
        try {
            NetMQConfig.Cleanup();
        }
        catch (Exception e) {
            Debug.Log(TAG + "Couldn't cleanup with exception: " + e);
        }
        Debug.Log(TAG + "Socket closed. Cleaned up 0MQ connection.");
    }
    
    /// <summary>
    /// Properly finished a zeroQM connection after exiting the application.
    /// </summary>
    private void OnApplicationQuit() {
        Debug.Log(TAG + "Closing socket.");
        subSocket.Close();
        Debug.Log(TAG + "Cleaning up MQ config.");
        try {
            NetMQConfig.Cleanup();
        }
        catch (Exception e) {
            Debug.Log(TAG + "Couldn't cleanup with exception: " + e);
        }
        Debug.Log(TAG + "Socket closed. Cleaned up 0MQ connection.");
    }
}