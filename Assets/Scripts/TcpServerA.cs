using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class TcpServerA : MonoBehaviour
{
    private Thread serverThread;
    private object quitLock = new object();
    bool quit = false;

    void Start()
    {
        serverThread = new Thread(Listening);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void Update()
    {
        bool quitting = false;
        lock (quitLock)
        {
            quitting = quit;
        }
        if (quitting)
        {
            //Debug.Log("Server quit request");
            Application.Quit();
        }
    }
    void OnDestroy()
    {
        //serverThread.Join();
        Debug.Log("Bye from server :)");
    }
    private void Listening()
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9052);
        serverSocket.Bind(ipep);
        serverSocket.Listen(1);
        Socket clientSocket = serverSocket.Accept();
        IPEndPoint clientIp = (IPEndPoint)clientSocket.RemoteEndPoint;
        Debug.Log("New client connected. Address: " + clientIp.Address + "Port: " + clientIp.Port);
        byte[] data = new byte[1024];

        while (true)
        {
            int recv = clientSocket.Receive(data);
            if(recv == 0)
            {
                Debug.Log("Recieved empty message: disconnecting");
                break;
            }
            string received = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log("Received " + received + " from client");
            if (received == "Ping")
            {
                Thread.Sleep(500);
                Debug.Log("Sending Pong back to client");
                clientSocket.Send(Encoding.ASCII.GetBytes("Pong"));
            }
        }
        clientSocket.Close();
        serverSocket.Close();
        //Requesting the application to exit
        lock (quitLock)
        {
            quit = true;
        }
        Debug.Log("Exiting server listening thread");
    }
}
