using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class UdpServer : MonoBehaviour
{
    private Socket serverSocket;
    private Thread listeningThread;
    // Start is called before the first frame update
    void Start()
    {
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);
        serverSocket.Bind(ipep);
        listeningThread = new Thread(Listening);
        listeningThread.IsBackground = true;
        listeningThread.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        SendCloseMessage();
        listeningThread.Join();
        serverSocket.Shutdown(SocketShutdown.Both);
        serverSocket.Close();
        Debug.Log("Bye :)");
    }
    private void Listening()
    {
        Debug.Log("Starting");
        IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remote = (EndPoint)ipep;

        while (true)
        {
            try
            {
                byte[] data = new byte[1024];
                int reciv = serverSocket.ReceiveFrom(data, ref remote);
                string recieved = Encoding.ASCII.GetString(data, 0, reciv);
                Debug.Log("Recieved " + recieved);
                if (recieved == "Ping")
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Pong back");
                    data = Encoding.ASCII.GetBytes("Pong");
                    serverSocket.SendTo(data, data.Length, SocketFlags.None, remote);
                }
                else if (recieved == "exit")
                {
                    Debug.Log("Exit Message recieved");
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("Exception caught: " + e.GetType());
                Debug.Log(e.Message);
            }
        }
        Debug.Log("Exiting listening thread");
    }

    private void SendCloseMessage()
    {
        Socket closeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        byte[] data = new byte[1024];
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9050);
        EndPoint remote = (EndPoint)ipep;
        data = Encoding.ASCII.GetBytes("exit");
        closeSocket.SendTo(data, remote);
        closeSocket.Shutdown(SocketShutdown.Both);
        closeSocket.Close();
    }
}