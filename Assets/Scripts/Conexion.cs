using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BestHTTP;
using BestHTTP.SocketIO;

public class Conexion : MonoBehaviour
{
    SocketManager socketManager;

    //conexion con node
    public void Connect()
    {
        TimeSpan miliSecForReconnect = TimeSpan.FromMilliseconds(1000);
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = 3;
        options.AutoConnect = true;
        options.ReconnectionDelay = miliSecForReconnect;

        //Server URI
        socketManager = new SocketManager(new Uri("http://fex02.ddns.net:9010/socket.io/"), options);
        socketManager.Socket.On("ping", evento1);
        socketManager.Open();

    }
    private void evento1(Socket socket, Packet packet, params object[] args)
    {
        Debug.Log("recibi un mensaje!");
        Debug.Log(args[0].ToString());
        EnviarMensaje();
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("evento start..");
        Connect();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void EnviarMensaje()
    {
        Debug.Log(socketManager.State);
        Debug.Log("enviando mensaje");
        socketManager.Socket.Emit("pong", "Bonito mensaje desde el cliente");
    }
}
