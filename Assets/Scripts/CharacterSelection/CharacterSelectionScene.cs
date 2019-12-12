using BestHTTP;
using BestHTTP.SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionScene : MonoBehaviour
{
    private Dictionary<int, string> allCharacters;
    private List<int> selectedCharacters;
    public List<GameObject> characterPrefab;
    private GameObject character;
    private SocketManager socketManager;
    private bool sendingSelection;
    private string testEmail;

    void Start()
    {
        ClickSound.PlaySoundBySource("Audio Source Background");
        //seteo las variables de prueba
        this.testEmail = "qwe@qwe.cl";

        //dejo seteada la variable que me indica si estoy enviando mi seleccion
        this.sendingSelection = false;

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

        //cuando el servidor envia la informacion de los personajes
        this.socketManager.Socket.On("SetCharacters", SetCharacters);

        //cuando el servidor me envia la direccion del servidor modular
        this.socketManager.Socket.On("SetServer", SetServer);

        //inicio la conexion con el servidor
        this.socketManager.Open();

        //instancio el listado de personajes seleccionados
        this.selectedCharacters = new List<int>();

        //seteo las funciones de los botones de seleccion de personajes
        foreach (Button button in GameObject.Find("Selection Panel").GetComponentsInChildren<Button>())
        {
            //si es el boton de cancelar
            if (button.name.Equals("Cancel Icon"))
            {
                button.onClick.AddListener(delegate { CancelSelection(); });
            }
            //si es el boton ok
            else if (button.name.Equals("Ok Icon"))
            {
                button.onClick.AddListener(delegate { OkSelection(); });
            }
            //si es uno de los otros botones
            else
            {
                button.onClick.AddListener(delegate { CharSelection(button.GetComponent<ButtonCharSelection>().index); });
            }
        }

        //oculto el listado de personajes
        UpdateMenu();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //funcion que actualiza el listado de la izquierda en el menu de seleccion de personajes
    private void UpdateMenu()
    {
        //recorro los botones
        Button[] buttons = GameObject.Find("Character Menu").GetComponentsInChildren<Button>();
        for (int i=0;i<5;i++)
        {
            //si existe el personaje en la lista
            if (i<selectedCharacters.Count)
            {
                //muestro el boton
                buttons[i].GetComponent<CanvasGroup>().alpha = 1;
                //lo agrego al boton
                buttons[i].GetComponentInChildren<Text>().text = this.allCharacters[this.selectedCharacters[i]];
            }
            //si no existe
            else
            {
                //oculto el boton
                buttons[i].GetComponent<CanvasGroup>().alpha = 0;
            }
        }
        //si no hay personajes seleccionados
        if (this.selectedCharacters.Count == 0)
        {
            //desactivo el boton de cancelar
            GameObject.Find("Cancel Icon").GetComponentInChildren<Button>().interactable = false;
            //elimino el personaje
            if (this.character != null)
            {
                Destroy(this.character);
            }
        }
        //si los hay
        else
        {
            //activo el boton cancelar
            GameObject.Find("Cancel Icon").GetComponentInChildren<Button>().interactable = true;
            //cambio el personaje seleccionado
            if(this.character != null)
            {
                Destroy(this.character);
            }
            this.character = Instantiate(this.characterPrefab[this.selectedCharacters[this.selectedCharacters.Count-1]], new Vector3(164.61f, 2.33f, 173.48f), Quaternion.Euler(0.0f, 62.9f, 0.0f));
        }
        //si ya estan todos los personajes seleccionados
        if (this.selectedCharacters.Count == 5)
        {
            //activo el boton de ok
            GameObject.Find("Ok Icon").GetComponentInChildren<Button>().interactable = true;
        }
        //si no estan todos 
        else
        {
            //desactivo el boton ok
            GameObject.Find("Ok Icon").GetComponentInChildren<Button>().interactable = false;
        }
        
    }

    //funcion que se ejecuta al presionar un icono de personaje
    private void CharSelection(int index)
    {
        //verifico que el arreglo de personajes no este lleno 
        if (this.selectedCharacters.Count < 5)
        {
            //si no lo esta agrego al personaje a la lista
            this.selectedCharacters.Add(index);
        }
        //actualizo el listado
        UpdateMenu();
    }

    //funcion que se ejecuta al presionar el icono de cancelar
    private void CancelSelection()
    {
        this.selectedCharacters.RemoveAt(this.selectedCharacters.Count-1);
        UpdateMenu();
    }

    //funcion que se ejecuta al presionar el icono Ok
    private void OkSelection()
    {
        //instancio el panel de carga
        Loading(true, "Sending selection to server");

        //informo que estoy enviando la seleccion
        this.sendingSelection = true;

        //genero un arreglo relacional con la data
        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("email", PlayerPrefs.GetString("email", this.testEmail));
        data.Add("selection", this.selectedCharacters);
        
        //envio la inforacion
        socketManager.Socket.Emit("SendCharactersSelected", data);
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
        //solicito la informacion de los personajes
        socketManager.Socket.Emit("GetCharacters", "");
    }

    //funcion que se ejecuta cuando el servidor envia la informacion de los personajes
    private void SetCharacters(Socket socket, Packet packet, params object[] args)
    {
        //instancio la lista de personajes
        this.allCharacters = new Dictionary<int, string>();
        //recorro los datos recibidos
        List<object> charactersList = new List<object>();
        List<object> charactersData = args[0] as List<object>;
        foreach (object characterData in charactersData)
        {
            Dictionary<string, object> characterDictionary = characterData as Dictionary<string, object>;
            //seteo los personajes
            this.allCharacters.Add(Convert.ToInt32(characterDictionary["index"]), Convert.ToString(characterDictionary["name"]));
        }
        //desactivar panel de carga si corresponde
        if (this.sendingSelection)
        {
            Loading(true, "Sending selection to server");
        }
        else
        {
            Loading(false, "");
        }
    }

    //funcion que se ejecuta cuando el servidor envia la informacion del servidor modular
    private void SetServer(Socket socket, Packet packet, params object[] args)
    {
        //guardo la direccion del servidor
        PlayerPrefs.SetString("serverAddress", args[0] as string);
        //cierro el socket
        this.socketManager.Close();
        //Finalizamos el audio
        ClickSound.StopSoundBySource("Audio Source Background");
        //mando al jugador a la escena de juego
        SceneManager.LoadScene("Game");
    }

    //funcion que se ejecuta al salir del juego
    private void OnApplicationQuit()
    {
        //si estoy conectado a un servidor
        if (this.socketManager != null)
        {

            if (this.socketManager.State.Equals("Open"))
            {
                //me desconecto
                this.socketManager.Socket.Disconnect();
            }
        }
    }
}
