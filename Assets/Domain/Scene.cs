using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP;
using BestHTTP.SocketIO;
using UnityEngine.Scripting;
using UnityEngine.Events;

public class Scene : MonoBehaviour

{
    public GameObject characterPrefab;
    private Team allied;
    private Team enemy;
    private Vector3[] positions;
    private Quaternion[] rotations;
    public Camera mainCamera;
    private Boolean selectingTarget;
    private ButtonAct lastButtonActPressed;
    private List<Action> actions;
    private SocketManager socketManager;
    private int playerId;
    private int gameId;

    //metodo que se ejecuta al iniciar la escena
    private void Start()
    {
        //seteo el panel de carga
        //Loading(true, "Cargando...");

        //seteo mi id de jugador
        this.playerId = 2;

        //generando equipos
        this.allied = new Team(1); //id jugador 1
        this.enemy = new Team(2); //id jugador 2

        //genero las posiciones por defecto para los 10 personajes
        this.positions = new Vector3[10];
        for (int i = 0; i < 5; i++)
        {
            this.positions[i] = new Vector3(160.0f, 0.0f, 164.0f + (i*3));
            this.positions[i+5] = new Vector3(145.0f, 0.0f, 164.0f + (i * 3));
        }

        //genero las rotaciones por defecto para los 10 personajes
        this.rotations = new Quaternion[10];
        for (int i = 0; i < 5; i++)
        {
            this.rotations[i] = Quaternion.Euler(0.0f, -90.0f, 0.0f);
            this.rotations[i+5] = Quaternion.Euler(0.0f, 90.0f, 0.0f);
        }

        //genero Personajes (agregandolos a su equipo)
        for (int i = 0; i < 5; i++)
        {
            this.allied.AddChar(MakeChar(i));
            this.enemy.AddChar(MakeChar(i+5));
        }

        //seteo datos de los Personajes en la UI
        int j = 0;
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            button.GetComponent<ButtonChar>().character = allied.characters[j];

            //seteo las funcionalidades de los botones de los personajes
            button.onClick.AddListener(delegate { CharClick(button); });

            //seteo el color de los botones de personajes (cuando esta desabilitado)
            ColorBlock cb = button.colors;
            cb.disabledColor = Color.Lerp(button.colors.normalColor, Color.black, 0.2f);
            button.colors = cb;

            //dejo los botones de personajes activos;
            button.GetComponent<ButtonChar>().isActive = true;

            j++;
        }

        //seteo el color del boton "end turn" (cuando esta desabilitado)
        ColorBlock cb2 = GameObject.Find("End Turn").GetComponent<Button>().colors;
        cb2.disabledColor = Color.Lerp(GameObject.Find("End Turn").GetComponent<Button>().colors.normalColor, Color.black, 0.2f);
        GameObject.Find("End Turn").GetComponent<Button>().colors = cb2;

        //actualizo la UI de estado de los peronajes
        UpdateCharacterMenu();

        //seteo las funcionalidades del menu de acciones
        foreach (Button button in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
        {
            button.onClick.AddListener(delegate { SkillClick(button); });
        }

        //seteo el modo de selccion de personaje en false
        this.selectingTarget = false;

        //defino las acciones en vacio
        actions = new List<Action>();

        //seteo la funcion del boton que termina el turno
        GameObject.Find("End Turn").GetComponent<Button>().onClick.AddListener(delegate { EndTurn(); });

        //instancio la conexion con el servidor
        //desabilito los logs (ya que yo los voy a realizar solo si los requiero)
        HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.None;
        
        //seteo las configuraciones de reconexion
        TimeSpan miliSecForReconnect = TimeSpan.FromMilliseconds(1000);
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = 3;
        options.AutoConnect = true;
        options.Reconnection = true;
        options.ReconnectionDelay = miliSecForReconnect;
        
        //instancio la conexion con el servidor modular
        this.socketManager = new SocketManager(new Uri("http://localhost:9010/socket.io/"), options);
        
        //seteo los eventos de error y desconexion
        this.socketManager.Socket.On(SocketIOEventTypes.Error, socketError);
        this.socketManager.Socket.On(SocketIOEventTypes.Disconnect, socketDisconnect);

        //seteo los eventos que utilizare
        //cuando el servidor me indica que la partida ha empezado
        socketManager.Socket.On("gameBegin", gameBegin);
        //cuando el servidor me indica que gané por leave
        socketManager.Socket.On("victoryByLeave", victoryByLeave);
        //cuando el servidor responde a las acciones que le envié
        socketManager.Socket.On("actionsResponse", actionsResponse);

        this.socketManager.Open();

        //informo que estoy listo para comenzar la partida identificandome
        socketManager.Socket.Emit("readyToBegin", this.playerId);
        //Loading(true, "Esperando al oponente...");
        
    }

    //funcion que se ejecuta cuando hay un error de conexion con el servidor
    private void socketError(Socket socket, Packet packet, params object[] args)
    {
        //Debug.Log("Se ha generado un error de conexión con el servidor");
        //Debug.Log(args);
    }

    //funcion que se ejecuta cuando se desconecta del servidor
    private void socketDisconnect(Socket socket, Packet packet, params object[] args)
    {
        //Debug.Log("Se ha desconectado del servidor");
        //Debug.Log(args);
    }

    //funcion que genera Personajes por posicion (por ahora para efectos de prueba)
    private GameObject MakeChar(int position)
    {
        //genero las habilidades del personaje
        Stat statSkill = new Stat(-10, 0, 0, 0, 0, 0, 0);
        List<Skill> skillSet = new List<Skill>();
        skillSet.Add(new Skill("Basic Attack", 10, statSkill, 0));

        //genero las estadisticas del personaje
        Stat statChar = new Stat(100, 100, 30, 20, 30, 30, 20);
        Stat actualStatChar = new Stat(70, 100, 30, 20, 30, 30, 20);

        //instancio el personaje en pantalla
        GameObject character = Instantiate(this.characterPrefab, this.positions[position], this.rotations[position]);

        //seteo el id, nombbre, habilidades, estadisticas y posicion del personaje
        character.GetComponent<Character>().id = 1;
        character.GetComponent<Character>().characterName = "Swordman";
        character.GetComponent<Character>().position = position;
        character.GetComponent<Character>().actualStat = actualStatChar;
        character.GetComponent<Character>().initialStat = statChar;
        character.GetComponent<Character>().skills = skillSet;

        return character;
    }

    //funcion que actualiza las barras de vida y mana
    private void UpdateCharacterMenu()
    {
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            //seteo el nombre del personaje en el boton
            button.GetComponentInChildren<Text>().text =  button.GetComponent<ButtonChar>().character.GetComponent<Character>().characterName;
            foreach(Slider slider in button.GetComponentsInChildren<Slider>())
            {
                //seteo la barra de vida
                if (slider.name.Equals("hp"))
                {
                    slider.value = (float) button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.hp / button.GetComponent<ButtonChar>().character.GetComponent<Character>().initialStat.hp;
                }
                //seteo la barra de mana
                else if (slider.name.Equals("mp"))
                {
                    slider.value = (float) button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.mp / button.GetComponent<ButtonChar>().character.GetComponent<Character>().initialStat.mp;
                }
            }
        }
    }

    //funcion que se ejecuta al seleccionar un Personaje en el menu
    private void CharClick(Button button)
    {
        allied.characters[0].GetComponent<Character>().Move(new Vector3(145.0f+3f, 0.0f, 164.0f + 12f));
        //cambio la posicion del menu de acciones
        GameObject.Find("Actions Menu").GetComponent<RectTransform>().position = new Vector2(GameObject.Find("Actions Menu").GetComponent<RectTransform>().position.x, button.transform.position.y );

        //muestro el menu de acciones
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().alpha = 1f;
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().interactable = true;

        //vacío los botones del menu acciones
        foreach (Button actButton in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
        {
            actButton.GetComponentInChildren<Text>().text = "";
            actButton.GetComponent<CanvasGroup>().interactable = false;
        }

        //seteo las habilidades en cada boton
        int i = 0;
        foreach (Skill skill in button.GetComponent<ButtonChar>().character.GetComponent<Character>().skills)
        {
            int j = 0;
            foreach (Button actButton in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
            {
                if (i == j)
                {
                    //seteo el nombre de la habilidad
                    actButton.GetComponentInChildren<Text>().text = skill.skillName;
                    //seteo datos de relacionados al personaje y habilidad en el boton
                    actButton.GetComponent<ButtonAct>().character = button.GetComponent<ButtonChar>().character;
                    actButton.GetComponent<ButtonAct>().skill = skill;
                    actButton.GetComponent<ButtonAct>().buttonChar = button;
                    //hago el boton clickeable
                    actButton.GetComponent<CanvasGroup>().interactable = true;
                }
                j++;
            }
            i++;
        }
    }

    //funcion que se ejecuta al presionar una habilidad en el menu de acciones
    private void SkillClick(Button button)
    {
        //seteo en true la fase de seleccion de objetivos
        this.selectingTarget = true;
        //seteo la ultima accion seleccionada
        this.lastButtonActPressed = button.GetComponent<ButtonAct>();
        //opaco los personajes (para que despues se puedan seleccionar)
        for (int i = 0; i < 5; i++)
        {
            //aliados
            foreach (Renderer renderer in this.allied.get(i).GetComponentsInChildren<Renderer>())
            {
                renderer.material.shader = Shader.Find("Legacy Shaders/Transparent/Parallax Specular");
            }
            //enemigos
            foreach (Renderer renderer in this.enemy.get(i).GetComponentsInChildren<Renderer>())
            {
                renderer.material.shader = Shader.Find("Legacy Shaders/Transparent/Parallax Specular");
            }
        }
        //oculto el menu de acciones
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().alpha = 0f;
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().interactable = false;
        //bloqueo el menu de personajes
        foreach (Button charButton in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            charButton.GetComponent<CanvasGroup>().interactable = false;
        }
    }

    //funcion que se ejecuta en cada frame del juego
    private void Update()
    {
        if (this.socketManager != null)
        {
            //Debug.Log(this.socketManager.State);
        }

        //maneja eventos de touch en la pantalla
        foreach (Touch touch in Input.touches)
        {
            HandleTouch(touch.fingerId, mainCamera.ScreenToWorldPoint(touch.position), touch.phase);
        }

        // simula eventos de touch al clickear
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Began);
            }
            if (Input.GetMouseButton(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Moved);
            }
            if (Input.GetMouseButtonUp(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Ended);
            }
        }
    }

    //funcion que recibe los toques o clicks
    private void HandleTouch(int touchFingerId, Vector3 touchPosition, TouchPhase touchPhase)
    {
        switch (touchPhase)
        {
            //fase 1 (cuandos se presiona)
            case TouchPhase.Began:
                //si esta en modo seleccion de objetivos genero el raycast
                if (this.selectingTarget)
                {
                    RaycastHit hit;
                    Ray ray = mainCamera.ScreenPointToRay(touchPosition);
                    Debug.DrawRay(ray.origin, ray.direction * 50, Color.red, 50000000f);
                    if (Physics.Raycast(ray, out hit))
                    {
                        GameObject character = hit.collider.gameObject;
                        //si el raycast golpea un personaje
                        if (character.name.Equals("Character(Clone)"))
                        {
                            //solidifico el personaje
                            foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
                            {
                                renderer.material.shader = Shader.Find("Standard");
                            }
                            
                            //termino la fase de seleccion de objetivo
                            this.selectingTarget = false;

                            //desactivo el boton del personaje
                            lastButtonActPressed.buttonChar.GetComponent<ButtonChar>().isActive = false;

                            //desbloqueo el menu de personajes
                            foreach (Button charButton in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
                            {
                                if (charButton.GetComponent<ButtonChar>().isActive)
                                {
                                    charButton.GetComponent<CanvasGroup>().interactable = true;
                                }
                            }
                            
                            //guardo la accion en el arreglo de acciones
                            actions.Add(new Action(lastButtonActPressed.character, character, lastButtonActPressed.skill));

                            //solidifico los personajes dentro de unos segundos
                            StartCoroutine(waitforSolidificateCharacters(0.7f));
                        }
                    }
                } 
                break;
            //fase 2 (cuando se mantiene)
            case TouchPhase.Moved:
                
                break;
            //fase 3 (cuando se suelta)
            case TouchPhase.Ended:
                // TODO
                break;
        }
    }

    //funcion que se ejecuta para volver solidos a los personajes
    private IEnumerator waitforSolidificateCharacters(float duration)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //solidifico los personajes
        for (int i = 0; i < 5; i++)
        {
            //aliados
            foreach (Renderer renderer in this.allied.get(i).GetComponentsInChildren<Renderer>())
            {
                renderer.material.shader = Shader.Find("Standard");
            }
            //enemigos
            foreach (Renderer renderer in this.enemy.get(i).GetComponentsInChildren<Renderer>())
            {
                renderer.material.shader = Shader.Find("Standard");
            }
        }
    }

    //funcion que se ejecuta al salir del juego
    private void OnApplicationQuit()
    {
        //si estoy conectado a un servidor
        if (this.socketManager != null)
        {
            
            if (this.socketManager.State.Equals("Open"))
            {
                //informo que me voy
                this.socketManager.Socket.Emit("leave", this.playerId);
                //me desconecto
                this.socketManager.Socket.Disconnect();
            }
        }
        
        
    }

    //funcion que termina el turno
    private void EndTurn()
    {
        //desabilitpo el boton de fin de turno
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().interactable = false;
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().blocksRaycasts = false;
        //envio mensaje de termino de turno y envio las acciones
        message("Turn End!", 20, 1f, delegate { sendActions(); });
    }

    //funcion que envia las acciones al servidor
    private void sendActions()
    {
        //activo el panel de carga
        Loading(true, "Enviando información al servidor...");
        //guardo los datos que enviare
        List<Hashtable> data = new List<Hashtable>();
        foreach (Action action in this.actions)
        {
            data.Add(action.SerializableAction());
        }
        //envio la informacion al servidor
        this.socketManager.Socket.Emit("actions", data);
        //informo al usuario de que estamos a la espera del servidor
        Loading(true, "Esperando al oponente...");

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
    private void message(String message, int size, float delay, UnityAction action)
    {
        //cambio el texto
        GameObject.Find("Message").GetComponentInChildren<Text>().fontSize = size;
        GameObject.Find("Message").GetComponentInChildren<Text>().text = message;
        //hago visible el mensaje
        GameObject.Find("Message").GetComponent<CanvasGroup>().alpha = 1f;
        //oculto el mensaje despues de un periodo
        StartCoroutine(waitForHideMessage(delay, action));
    }

    //funcion que oculta un mensaje en pantalla
    private IEnumerator waitForHideMessage(float duration, UnityAction action)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //oculto el mensaje
        GameObject.Find("Message").GetComponent<CanvasGroup>().alpha = 0f;
        //ejecuto las acciones
        action.Invoke();
    }

    //funcion que se ejecuta al inciar el juego (primer turno)
    private void gameBegin(Socket socket, Packet packet, params object[] args)
    {
        this.gameId = int.Parse(args[0].ToString());
        Loading(false, "");
        message("Turn Start!", 20, 1f, delegate { });
    }

    //funcion que se ejecuta cuando gano por leave
    private void victoryByLeave(Socket socket, Packet packet, params object[] args)
    {
        Loading(false, "");
        message("Victory! (by leave)", 20, 3f, delegate { });
    }

    //funcion que se ejecuta cuando el servidor me envia las respuestas de las acciones realizadas
    private void actionsResponse(Socket socket, Packet packet, params object[] args)
    {
        //regreso los botones de los personajes a su estado original
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            //dejo los botones de personajes activos;
            button.GetComponent<ButtonChar>().isActive = true;
            button.GetComponent<CanvasGroup>().interactable = false;
        }
        //regreso el boton end turn estado original
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().interactable = true;
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().blocksRaycasts = true;
        //oculto el panel de carga
        Loading(false, "");
        //informo al jugador que empieza la fase de batalla
        message("Battle Phase!", 20, 1f, delegate { });
        List<object> data = args[0] as List<object>;
        foreach (object actionData in data)
        {
            Debug.Log("aqui");
            Dictionary<string, object> action = actionData as Dictionary<string, object>;
            Debug.Log(action["owner"]);
        }
        //Debug.Log(data);
    }
}