using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP;
using BestHTTP.SocketIO;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class IntroScene : MonoBehaviour
{
    public Button searchOpponent;
    private SocketManager socketManager;
    private int playerId;
    private bool searchingOpponent;

    void Start()
    {
        //seteo el id del jugador
        this.playerId = 1;

        //informo que me conectare al servidor 
        Loading(true, "Connection failed trying to reconnect...");

        //instancio la conexion con el servidor
        //desabilito los logs (ya que yo los voy a realizar solo si los requiero)
        HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.None;

        //seteo las configuraciones de reconexion
        TimeSpan miliSecForReconnect = TimeSpan.FromMilliseconds(1000);
        SocketOptions options = new SocketOptions();
        options.AutoConnect = true;
        options.Reconnection = true;
        options.ReconnectionDelay = miliSecForReconnect;

        //instancio la conexion con el servidor principal
        this.socketManager = new SocketManager(new Uri("http://fex02.ddns.net:9000/socket.io/"), options);

        //cuando la conexion con el servidor falla
        this.socketManager.Socket.On("connect_error", ConnectionError);

        //cuando me conecto correctamente al servidor
        this.socketManager.Socket.On("connect", Connected);

        //cuando se desconecta del servidor
        this.socketManager.Socket.On("disconnect", Disconnected);        

        //inicio la conexion con el servidor
        this.socketManager.Open();

        //cuando el servidor encuentra un oponente
        this.socketManager.Socket.On("OpponentFound", OpponentFound);

        //seteo la funcion en el boton de busca oponente
        searchOpponent.onClick.AddListener(delegate { SearchOpponent(); });

        //informo que aun no empeizo a buscar oponente
        this.searchingOpponent = false;
    }

    void Update()
    {
        
    }

    //funcion que se ejecuta al presionar el boton buscar oponente
    private void SearchOpponent()
    {
        //informo que estoy buscando oponente
        this.searchingOpponent = true;
        //detenego el parpadeo del boton buscar oponente
        searchOpponent.GetComponent<SearchOpponent>().StopBlinking();
        //muestro la pantalla de carga
        Loading(true, "Searching opponent...");
        //informo al servidor que estoy buscando oponente
        socketManager.Socket.Emit("SearchOpponent", this.playerId);
    }

    //funcion que se ejecuta cuando el servidor encuentra un oponente
    private void OpponentFound(Socket socket, Packet packet, params object[] args)
    {
        Loading(false, "");
        Message("Opponent Found!", 20, 1f, delegate {});
        SceneManager.LoadScene("CharacterSelection");
    }

    //funcion que activa el panel de carga
    private void Loading(Boolean loading, String text)
    {
        if (loading)
        {
            //cambio el texto
            GameObject.Find("Loading Panel").GetComponentInChildren<Text>().text = text;
            //hago visible y tangible el panel de carga
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().alpha = 1f;
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().interactable = true;
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().blocksRaycasts = true;
            //lo llevo al frente
            GameObject.Find("Loading Panel").GetComponent<Image>().transform.SetAsLastSibling();
        }
        else
        {
            //hago invisible y intangible el panel de carga
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().alpha = 0f;
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().interactable = false;
            GameObject.Find("Loading Panel").GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
    }

    //funcion que muestra un mensaje en pantalla
    private void Message(String message, int size, float delay, UnityAction action)
    {
        //cambio el texto
        GameObject.Find("Message").GetComponentInChildren<Text>().fontSize = size;
        GameObject.Find("Message").GetComponentInChildren<Text>().text = message;
        //hago visible el mensaje
        GameObject.Find("Message").GetComponent<CanvasGroup>().alpha = 1f;
        //oculto el mensaje despues de un periodo
        StartCoroutine(WaitForHideMessage(delay, action));
    }

    //funcion que oculta un mensaje en pantalla
    private IEnumerator WaitForHideMessage(float duration, UnityAction action)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //oculto el mensaje
        GameObject.Find("Message").GetComponent<CanvasGroup>().alpha = 0f;
        //ejecuto las acciones
        action.Invoke();
    }

    //funcion que se ejecuta cuando falla la conexion con el servidor.
    private void ConnectionError(Socket socket, Packet packet, params object[] args)
    {
        Loading(true, "Connection failed trying again...");
    }

    //funcion que se ejecuta cuando falla la conexion con el servidor.
    private void Disconnected(Socket socket, Packet packet, params object[] args)
    {
        Loading(true, "Connection lost trying to reconnect..");
    }

    //funcion que se ejecuta cuando me conecto con el servidor
    private void Connected(Socket socket, Packet packet, params object[] args)
    {
        socketManager.Socket.Emit("SuscribeClient", this.playerId);
        if (this.searchingOpponent)
        {
            Loading(true, "Searching opponent...");
        }
        else
        {
            Loading(false, "");
        }
    }

}
