using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;

public class TcpClient : MonoBehaviour
{
    private Thread clientThread;
    private object quitLock = new object();
    private object appQuitLock = new object();
    bool appQuit = false;
    bool quit = false;
    public Text OutputLog;
    private object logLock = new object();
    string newText;
    bool textToLog = false;

    void Start()
    {
        OutputLog.text = "";
        clientThread = new Thread(Connection);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void Update()
    {
        //Enable a request from the thread to close the app
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
    private void OnDestroy()
    {
        lock(appQuitLock)
        {
            appQuit = true;
        }
        //Joining threads to make sure i end the application correctly before the runtime closes the thread
        clientThread.Join();
        Debug.Log("Bye from client :)");
    }
    private void Connection()
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint serverIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9052);
        Debug.Log("Trying connection with server");
        ThreadLogText("Trying connection with server");
        try
        {
            clientSocket.Connect((EndPoint)serverIpep);
        }
        catch(SocketException e)
        {
            if(e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                Debug.Log("Could not connect to server because server is down");
                ThreadLogText("Could not connect to server because server is down");
                clientSocket.Close();
                Debug.Log("Exiting client connection thread");
                ThreadLogText("Exiting client connection thread");
                lock (quitLock)
                {
                    quit = true;
                }
                return;
            }
        }
        Debug.Log("Succesfully connected with server");
        ThreadLogText("Succesfully connected with server");
        byte[] data = new byte[1024];
        Debug.Log("Sending first ping to server");
        ThreadLogText("Sending first ping to server");
        clientSocket.Send(Encoding.ASCII.GetBytes("Ping"));
        int pongsReceived = 0;

        while (true)
        {
            int recv = clientSocket.Receive(data);
            string receivedString = Encoding.ASCII.GetString(data, 0, recv);
            Debug.Log("Received " + receivedString + "from server");
            ThreadLogText("Received " + receivedString + "from server");
            bool quitting;
            lock(appQuitLock)
            {
                quitting = appQuit;
            }
            if (quitting)
            {
                Debug.Log("Received exit request from client");
                Debug.Log("Closing client app");
                ThreadLogText("Received exit request from client");
                ThreadLogText("Closing client app");
                break;
            }
            if (receivedString == "Pong")
            {
                ++pongsReceived;
                Debug.Log("Received Pong number " + pongsReceived + " from server");
                ThreadLogText("Received Pong number " + pongsReceived + " from server");
                if (pongsReceived < 5)
                {
                    Thread.Sleep(500);
                    Debug.Log("Sending Ping to server");
                    ThreadLogText("Sending Ping to server");
                    clientSocket.Send(Encoding.ASCII.GetBytes("Ping"));
                }
                else
                {
                    Debug.Log("Already received all pongs");
                    Debug.Log("Closing client app");
                    ThreadLogText("Already received all pongs");
                    ThreadLogText("Closing client app");
                    break;
                }
            }
            if(receivedString == "Exit")
            {
                Debug.Log("Received exit request from server");
                Debug.Log("Closing client app");
                ThreadLogText("Received exit request from server");
                ThreadLogText("Closing client app");
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
        ThreadLogText("Exiting client connection thread");
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
