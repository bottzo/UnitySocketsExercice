using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class TcpServerB : MonoBehaviour
{
    private Thread serverThread;
    bool listening = true;
    bool accepting = false;
    object listeningLock = new object();

    void Start()
    {
        serverThread = new Thread(Listening);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void Update()
    {

    }
    void OnDestroy()
    {
        bool accept;
        lock (listeningLock)
        {
            listening = false;
            accept = accepting;
        }
        if(accept)
        {
            ClosingConnection();
        }
        serverThread.Join();
        Debug.Log("Bye from server :)");
    }
    private void Listening()
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9052);
        serverSocket.Bind(ipep);
        serverSocket.Listen(10);
        bool listen;
        
        while (true)
        {
            lock (listeningLock)
            {
                listen = listening;
                accepting = true;
            }
            if (!listen)
            {
                Debug.Log("Stop listening to clients");
                break;
            }
            Socket clientSocket = serverSocket.Accept();
            IPEndPoint clientIp = (IPEndPoint)clientSocket.RemoteEndPoint;
            Debug.Log("New client connected. Address: " + clientIp.Address + "Port: " + clientIp.Port);
            byte[] data = new byte[1024];
            bool exitSent = false;
            while (true)
            {
                lock (listeningLock)
                {
                    listen = listening;
                    accepting = false;
                }
                if(!listen && !exitSent)
                {
                    clientSocket.Send(Encoding.ASCII.GetBytes("Exit"));
                    exitSent = true;
                }
                int recv = clientSocket.Receive(data);
                if (recv == 0)
                {
                    Debug.Log("Client disconnected");
                    break;
                }
                string received = Encoding.ASCII.GetString(data, 0, recv);
                Debug.Log("Received " + received + " from client");
                if (listen && received == "Ping")
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Pong back to client");
                    clientSocket.Send(Encoding.ASCII.GetBytes("Pong"));
                }
            }
            Debug.Log("Client disconnection: Address: " + clientIp.Address + "Port: " + clientIp.Port);
            clientSocket.Close();
        }
        serverSocket.Close();
        Debug.Log("Exiting server listening thread");
    }
    private void ClosingConnection()
    {
        Socket closeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint serverIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9052);
        closeSocket.Connect((EndPoint)serverIpep);
        //byte[] data = new byte[1024];
        //int recv = closeSocket.Receive(data);
        //string receivedString = Encoding.ASCII.GetString(data, 0, recv);
        //if()
        closeSocket.Shutdown(SocketShutdown.Both);
        closeSocket.Close();
    }
}