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
        ClickSound.PlaySoundBySource("Audio Source Intro");
        //seteo variables de prueba
        if (PlayerPrefs.GetString("email", "").Equals(""))
        {
            PlayerPrefs.SetString("email", "seba@qwe.cl");
            PlayerPrefs.SetString("name", "qwe1");
        }
        //seteo direccion del servidor principal
        PlayerPrefs.SetString("mainServerAddress", "http://fex02.ddns.net:9000/socket.io/");

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
        this.socketManager = new SocketManager(new Uri(PlayerPrefs.GetString("mainServerAddress")), options);

        //cuando la conexion con el servidor falla
        this.socketManager.Socket.On("connect_error", ConnectionError);

        //cuando me conecto correctamente al servidor
        this.socketManager.Socket.On("connect", Connected);

        //cuando se desconecta del servidor
        this.socketManager.Socket.On("disconnect", Disconnected);        

        //cuando el servidor encuentra un oponente
        this.socketManager.Socket.On("OpponentFound", OpponentFound);

        //cuando el servidor me envia la direccion del servidor modular
        this.socketManager.Socket.On("SetServer", SetServer);

        //inicio la conexion con el servidor
        this.socketManager.Open();

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
        socketManager.Socket.Emit("SearchOpponent", PlayerPrefs.GetString("email"));
    }

    //funcion que se ejecuta cuando el servidor encuentra un oponente
    private void OpponentFound(Socket socket, Packet packet, params object[] args)
    {
        Loading(false, "");
        Message("Opponent Found!", 20, 1f, delegate { this.socketManager.Close(); ClickSound.StopSoundBySource("Audio Source Intro"); SceneManager.LoadScene("CharacterSelection"); });
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
        //creo un arreglo relacional para enviar los datos al servidor
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("email", PlayerPrefs.GetString("email"));
        data.Add("name", PlayerPrefs.GetString("name"));
        //envio los datos al servidor
        socketManager.Socket.Emit("SuscribeClient", data);
        //verifico si debo cambiar el panel de carga
        if (this.searchingOpponent)
        {
            //si actualmente estoy buscando oponente debo poner ese mensaje
            Loading(true, "Searching opponent...");
            SearchOpponent();
        }
        else
        {
            //si no, debo eliminar el panel de carga
            Loading(false, "");
        }
    }

    //funcion que se ejecuta cuando el servidor envia la informacion del servidor modular
    private void SetServer(Socket socket, Packet packet, params object[] args)
    {
        //guardo la direccion del servidor
        PlayerPrefs.SetString("serverAdress", args[0] as string);
        //mando al jugador a la escena de juego
        SceneManager.LoadScene("Game");
    }

}
