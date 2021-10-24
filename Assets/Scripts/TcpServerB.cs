using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using UnityEngine.UI;

public class TcpServerB : MonoBehaviour
{
    private Thread serverThread;
    bool listening = true;
    bool accepting = false;
    object listeningLock = new object();
    public Text OutputLog;
    private object logLock = new object();
    string newText;
    bool textToLog = false;

    void Start()
    {
        serverThread = new Thread(Listening);
        serverThread.IsBackground = true;
        serverThread.Start();
        OutputLog.text = "";
    }

    void Update()
    {
        //Log any new lines issued from the thread
        lock (logLock)
        {
            if (textToLog)
            {
                OutputLog.text += newText;
                newText = "";
                textToLog = false;
            }
        }
    }
    void OnDestroy()
    {
        bool accept;
        lock (listeningLock)
        {
            //Here i tell the thread to stop a connection with a client
            listening = false;
            //Here I check if the server thread is blocked in Accept() waiting for a new connection
            accept = accepting;
        }
        if(accept)
        {
            //If the server thread is blocked on Accept i connect as a client and as the listening bool will be set
            //we will imediately disconect and close the server thread
            ClosingConnection();
        }
        //Joining thr thread to make sure it is not closed by the runtime but by me
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
        Debug.Log("Start listening to clients");
        ThreadLogText("Start listening to clients");

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
                ThreadLogText("Stop listening to clients");
                break;
            }
            Socket clientSocket = serverSocket.Accept();
            IPEndPoint clientIp = (IPEndPoint)clientSocket.RemoteEndPoint;
            Debug.Log("New client connected. Address: " + clientIp.Address + "Port: " + clientIp.Port);
            ThreadLogText("New client connected. Address: " + clientIp.Address + "Port: " + clientIp.Port);
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
                    ThreadLogText("Client disconnected");
                    break;
                }
                string received = Encoding.ASCII.GetString(data, 0, recv);
                Debug.Log("Received " + received + " from client");
                if (listen && received == "Ping")
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Pong back to client");
                    ThreadLogText("Sending Pong back to client");
                    clientSocket.Send(Encoding.ASCII.GetBytes("Pong"));
                }
            }
            Debug.Log("Client disconnection: Address: " + clientIp.Address + "Port: " + clientIp.Port);
            ThreadLogText("Client disconnection: Address: " + clientIp.Address + "Port: " + clientIp.Port);
            clientSocket.Close();
        }
        serverSocket.Close();
        Debug.Log("Exiting server listening thread");
        ThreadLogText("Exiting server listening thread");
    }

    //Function that creates a new socket to connect to the server and close the server
    //Just used when the thread is blocked on the Accept function
    private void ClosingConnection()
    {
        Socket closeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint serverIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9052);
        closeSocket.Connect((EndPoint)serverIpep);
         byte[] data = new byte[1024];
         int recv = closeSocket.Receive(data);
         string receivedString = Encoding.ASCII.GetString(data, 0, recv);
         if (receivedString == "Exit")
         {
             closeSocket.Shutdown(SocketShutdown.Both);
             closeSocket.Close();
         }
         //else
         //{
         //    Debug.Log("This should never happen");
         //}
    }
    //This function is to make the main thread log a text to the output command
    //It is required because the main thread is the only that can change the ui text variable
    private void ThreadLogText(string text)
    {
        lock (logLock)
        {
            textToLog = true;
            newText += System.Environment.NewLine + text;
        }
    }
}