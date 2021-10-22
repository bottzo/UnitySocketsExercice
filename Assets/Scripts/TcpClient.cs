using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TcpClient : MonoBehaviour
{
    private Thread clientThread;
    private object quitLock = new object();
    bool quit = false;

    void Start()
    {
        clientThread = new Thread(Connection);
        clientThread.IsBackground = true;
        clientThread.Start();
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
            //Debug.Log("Qitting client");
//#if UNITY_EDITOR
//            UnityEditor.EditorApplication.isPlaying = false;
//#endif
            Application.Quit();
        }
    }
    private void OnDestroy()
    {
        Debug.Log("Bye from client :)");
    }
    private void Connection()
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint serverIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9052);
        //FER EL TRY CATCH EN EL CONNECT
        clientSocket.Connect((EndPoint)serverIpep);
        Debug.Log("Succesfully connected with server");
        byte[] data = new byte[1024];
        Debug.Log("Sending first ping to server");
        clientSocket.Send(Encoding.ASCII.GetBytes("Ping"));
        int pingsSent = 1;

        while (true)
        {
            if (pingsSent < 5)
            {
                int recv = clientSocket.Receive(data);
                string receivedString = Encoding.ASCII.GetString(data, 0, recv);
                Debug.Log("Received " + receivedString + "from server");
                if (receivedString == "Pong")
                {
                    Thread.Sleep(500);
                    ++pingsSent;
                    Debug.Log("Sending Ping number " + pingsSent + " to server");
                    clientSocket.Send(Encoding.ASCII.GetBytes("Ping"));
                }
            }
            else
            {
                Debug.Log("Already sent all the pings");
                Debug.Log("Closing client app");
                break;
            }
        }
        clientSocket.Shutdown(SocketShutdown.Both);
        clientSocket.Close();
        lock (quitLock)
        {
            quit = true;
        }
        Debug.Log("Exiting client connection thread");
    }
}
