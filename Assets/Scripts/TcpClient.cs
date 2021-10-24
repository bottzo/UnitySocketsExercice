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
        int pongsReceived = 0;

        while (true)
        {
            int recv = clientSocket.Receive(data);
            string receivedString = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log("Received " + receivedString + "from server");
            if (receivedString == "Pong")
            {
                ++pongsReceived;
                Debug.Log("Received Pong number " + pongsReceived + " from server");
                if (pongsReceived < 5)
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Ping to server");
                    clientSocket.Send(Encoding.ASCII.GetBytes("Ping"));
                }
                else
                {
                    Debug.Log("Already received all pongs");
                    Debug.Log("Closing client app");
                    break;
                }
            }
            if(receivedString == "Exit")
            {
                Debug.Log("Received exit request from server");
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
