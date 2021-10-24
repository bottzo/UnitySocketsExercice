using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class UdpClient : MonoBehaviour
{
    private Socket connectionSocket;
    private Thread listeningThread;
    private object quitLock = new object();
    bool quit = false;
    void Start()
    {
        connectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9051);
        connectionSocket.Bind(ipep);
        listeningThread = new Thread(Connection);
        listeningThread.IsBackground = true;
        listeningThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        bool quitting = false;
        lock (quitLock)
        {
            quitting = quit;
        }
        if (quitting)
        {
            //Debug.Log("Qitting client");
            Application.Quit();
        }
    }

    void OnDestroy()
    {
        SendCloseMessage();
        listeningThread.Join();
        connectionSocket.Shutdown(SocketShutdown.Both);
        connectionSocket.Close();
        Debug.Log("Bye :)");
    }
    private void Connection()
    {
        //byte[] data = new byte[1024];
        IPEndPoint serverIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);
        //EndPoint serverRemote = (EndPoint)serverIpep;
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remote = (EndPoint)ipep;

        Debug.Log("Sending first ping");
        byte[] data = Encoding.ASCII.GetBytes("Ping");
        connectionSocket.SendTo(data, data.Length, SocketFlags.None, serverIpep);
        while (true)
        {
            try
            {
                data = new byte[1024];
                int reciv = connectionSocket.ReceiveFrom(data, ref remote);
                string recieved = Encoding.ASCII.GetString(data, 0, reciv);
                Debug.Log("Recieved " + recieved);
                if (recieved == "Pong" /*&& remote == serverRemote*/)
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Ping to server");
                    data = Encoding.ASCII.GetBytes("Ping");
                    connectionSocket.SendTo(data, data.Length, SocketFlags.None, remote);
                }
                else if (recieved == "exit")
                {
                    Debug.Log("Exit Message recieved");
                    break;
                }
            }
            catch (SocketException e)
            {
                Debug.Log("Exception caught: " + e.GetType());
                Debug.Log(e.Message);
                if(e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Debug.Log("Could not connect to server because it is down");
                    Debug.Log("Exiting client app");
                    lock (quitLock)
                    {
                        quit = true;
                    }
                }
            }
        }
        Debug.Log("Exiting listening thread");
    }

    private void SendCloseMessage()
    {
        Socket closeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        byte[] data = new byte[1024];
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9051);
        EndPoint remote = (EndPoint)ipep;
        data = Encoding.ASCII.GetBytes("exit");
        closeSocket.SendTo(data, remote);
        closeSocket.Shutdown(SocketShutdown.Both);
        closeSocket.Close();
    }
}
