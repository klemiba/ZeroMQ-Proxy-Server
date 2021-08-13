using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class NetMqPublisher {
    private readonly Thread _publisherThread;
    private bool _publisherCancelled;
    public delegate string MessageDelegate(string message);
    private readonly MessageDelegate _messageDelegate;

    public int milisecondIntervals = 2000;
    public string topic = "plc-tags";
    // Variable stores latest data recieve from remote zeroMQ server
    public string LatestZeroMqData;
    
    // Constructor 
    public NetMqPublisher() {
        _publisherThread = new Thread(Publish);
    }
    
    // Creates a publisher socket and sneds frame to specified tags
    private void Publish() {
        AsyncIO.ForceDotNet.Force();
        NetMQConfig.Cleanup();
        using (var server = new PublisherSocket()) {
            // Bind to this machines IP and open on the 12346 port
            server.Bind("tcp://*:12346");

            while (!_publisherCancelled) {
                for (var i = 0; i < 100; i++) {
                    var msg = LatestZeroMqData;
                    Debug.Log("Sending message: " + msg);
                    if(msg != null)
                        server.SendMoreFrame(topic).SendFrame(msg);
                    Thread.Sleep(milisecondIntervals);
                }
            }
        }
        NetMQConfig.Cleanup();
    }

    // Start the loop running the server
    public void Start() {
        _publisherCancelled = false;
        _publisherThread.Start();
    }

    // Stops the loop runing the server
    public void Stop() {
        _publisherCancelled = true;
        _publisherThread.Join();
    }
}
