using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP;
using BestHTTP.SocketIO;
using UnityEngine.Scripting;
using UnityEngine.Events;
using System.Reflection;
using System.Linq;

public class GameScene : MonoBehaviour

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
    private int performingAction;
    private List<object> performingActions;
    private bool gameOver;

    //metodo que se ejecuta al iniciar la escena
    private void Start()
    {
        //seteo el panel de carga
        Loading(true, "Loading...");

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
        this.socketManager.Socket.On(SocketIOEventTypes.Disconnect, SocketDisconnect);

        //seteo los eventos que utilizare
        //cuando el servidor me indica que la partida ha empezado
        this.socketManager.Socket.On("GameBegin", GameBegin);
        //cuando el servidor me indica que gané por leave
        this.socketManager.Socket.On("VictoryByLeave", VictoryByLeave);
        //cuando el servidor responde a las acciones que le envié
        this.socketManager.Socket.On("ActionsResponse", ActionsResponse);

        this.socketManager.Open();

        //informo que estoy listo para comenzar la partida identificandome
        socketManager.Socket.Emit("ReadyToBegin", this.playerId);
        //cambio el mensaje del panel de carga
        Loading(true, "Waiting for the opponent...");

    }

    //funcion que se ejecuta cuando hay un error de conexion con el servidor
    private void socketError(Socket socket, Packet packet, params object[] args)
    {
        //Debug.Log("Se ha generado un error de conexión con el servidor");
        //Debug.Log(args);
    }

    //funcion que se ejecuta cuando se desconecta del servidor
    private void SocketDisconnect(Socket socket, Packet packet, params object[] args)
    {
        //Debug.Log("Se ha desconectado del servidor");
        //Debug.Log(args);
    }

    //funcion que genera Personajes por posicion (por ahora para efectos de prueba)
    private GameObject MakeChar(int position)
    {
        //genero las habilidades del personaje
        Stat statSkill = new Stat(-40, 0, 0, 0, 0, 0, 0);
        List<Skill> skillSet = new List<Skill>();
        skillSet.Add(new Skill(1,"Basic Attack", 10, statSkill, 0));

        //genero las estadisticas del personaje
        Stat statChar = new Stat(100, 100, 30, 20, 30, 30, 20);
        Stat actualStatChar = new Stat(100, 100, 30, 20, 30, 30, 20);

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
            //si el personaje murio desactivo el boton
            if (button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.hp == 0)
            {
                button.GetComponent<ButtonChar>().isActive = false;
                button.GetComponent<CanvasGroup>().interactable = false;
            }

        }
    }

    //funcion que se ejecuta al seleccionar un Personaje en el menu
    private void CharClick(Button button)
    {
        //allied.characters[0].GetComponent<Character>().PerformAction(1, new Vector3(145.0f, 0.0f, 164.0f + 12f), new Vector3(145.0f + 6f, 0.0f, 164.0f + 12f));
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
                            StartCoroutine(WaitforSolidificateCharacters(0.7f));
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
    private IEnumerator WaitforSolidificateCharacters(float duration)
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
                this.socketManager.Socket.Emit("Leave", this.playerId);
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
        Message("Turn End!", 20, 1f, delegate { SendActions(); });
    }

    //funcion que envia las acciones al servidor
    private void SendActions()
    {
        //activo el panel de carga
        Loading(true, "Sending information to the server...");
        //guardo los datos que enviare
        List<Hashtable> data = new List<Hashtable>();
        foreach (Action action in this.actions)
        {
            data.Add(action.SerializableAction());
        }
        //envio la informacion al servidor
        this.socketManager.Socket.Emit("Actions", data);
        //informo al usuario de que estamos a la espera del servidor
        Loading(true, "Waiting for the opponent...");

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

    //funcion que se ejecuta al inciar el juego (primer turno)
    private void GameBegin(Socket socket, Packet packet, params object[] args)
    {
        this.gameId = int.Parse(args[0].ToString());
        Loading(false, "");
        Message("Turn Start!", 20, 1f, delegate { });
    }

    //funcion que se ejecuta cuando gano por leave
    private void VictoryByLeave(Socket socket, Packet packet, params object[] args)
    {
        Loading(false, "");
        Message("Victory! (by leave)", 20, 3f, delegate { });
    }

    //funcion que se ejecuta cuando el servidor me envia las respuestas de las acciones realizadas
    private void ActionsResponse(Socket socket, Packet packet, params object[] args)
    {
        //bloqueo el boton end turn
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().interactable = false;
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().blocksRaycasts = false;
        //bloqueo los botones de los personajes
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            //dejo los botones de personajes inactivos;
            button.GetComponent<ButtonChar>().isActive = false;
            button.GetComponent<CanvasGroup>().interactable = false;
        }
        //oculto el panel de carga
        Loading(false, "");
        //informo al jugador que empieza la fase de batalla
        Message("Battle Phase!", 20, 1f, delegate { });
        //guardo las acciones a realizar
        this.performingActions = new List<object>();
        List<object> data = args[0] as List<object>;
        foreach (object actionData in data)
        {
            performingActions.Add(actionData);
        }
        //seteo la primera accion a realizar
        this.performingAction = 0;
        //comienzo a realizar las acciones
        PerformAction();
    }

    //funcion que realizar las acciones
    private void PerformAction()
    {
        //si ya no existen mas acciones para realizar
        if (this.performingActions.Count <= this.performingAction)
        {
            //termino el turno
            //limpio las acciones
            actions = new List<Action>();
            //regreso los botones de los personajes a su estado original
            foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
            {
                //dejo los botones de personajes activos;
                button.GetComponent<ButtonChar>().isActive = true;
                button.GetComponent<CanvasGroup>().interactable = true;
            }
            //actualizo las barras de estado
            UpdateCharacterMenu();
            //regreso el boton end turn estado original
            GameObject.Find("End Turn").GetComponent<CanvasGroup>().interactable = true;
            GameObject.Find("End Turn").GetComponent<CanvasGroup>().blocksRaycasts = true;
            //informo el inicio de un nuevo turno
            Message("Turn Start!", 20, 1f, delegate { });
        }
        //si aun existen, recorro las acciones que se estan realizando
        int count = 0;
        foreach (object actionData in this.performingActions)
        {
            //si la accion que estoy realizando existe
            if(count == this.performingAction)
            {
                Dictionary<string, object> action = actionData as Dictionary<string, object>;
                //identifico al owner
                GameObject owner;
                //si el owner es < 5 quiere decir que es un aliado
                if (Convert.ToInt32(action["owner"]) < 5)
                {
                    owner = allied.characters[Convert.ToInt32(action["owner"])];
                }
                //si no, es un enemigo
                else
                {
                    owner = enemy.characters[Convert.ToInt32(action["owner"]) - 5];
                }
                //identifico al affected
                GameObject affected;
                Vector3 targetPosition;
                //si el affected es < 5 quiere decir que es un aliado
                if (Convert.ToInt32(action["affected"]) < 5)
                {
                    affected = allied.characters[Convert.ToInt32(action["affected"])];
                    //defino la posicion donde quiero llegar
                    targetPosition = new Vector3(affected.transform.position.x - 6f, affected.transform.position.y, affected.transform.position.z);
                }
                //si no, es un enemigo
                else
                {
                    affected = enemy.characters[Convert.ToInt32(action["affected"]) - 5];
                    //defino la posicion donde quiero llegar
                    targetPosition = new Vector3(affected.transform.position.x + 6f, affected.transform.position.y, affected.transform.position.z);
                }
                //obtengo el estado del afectado una vez realizada la accion
                Dictionary<string, object> affectedStatMap = action["affectedStat"] as Dictionary<string, object>;
                Stat affectedStat = new Stat(Convert.ToInt32(affectedStatMap["hp"]), Convert.ToInt32(affectedStatMap["mp"]), Convert.ToInt32(affectedStatMap["atk"]), Convert.ToInt32(affectedStatMap["def"]), Convert.ToInt32(affectedStatMap["spd"]), Convert.ToInt32(affectedStatMap["mst"]), Convert.ToInt32(affectedStatMap["mdf"]));
                //obtengo el estado del owner una vez realizada la accion
                Dictionary<string, object> ownerStatMap = action["ownerStat"] as Dictionary<string, object>;
                Stat ownerStat = new Stat(Convert.ToInt32(ownerStatMap["hp"]), Convert.ToInt32(ownerStatMap["mp"]), Convert.ToInt32(ownerStatMap["atk"]), Convert.ToInt32(ownerStatMap["def"]), Convert.ToInt32(ownerStatMap["spd"]), Convert.ToInt32(ownerStatMap["mst"]), Convert.ToInt32(ownerStatMap["mdf"]));
                //realizo la accion
                owner.GetComponent<Character>().PerformAction(Convert.ToInt32(action["skillId"]), affected.transform.position, targetPosition, delegate { affected.GetComponent<Character>().SetStat(affectedStat); owner.GetComponent<Character>().SetStat(ownerStat); UpdateCharacterMenu(); this.gameOver = IsGameOver(); }, delegate {if (!this.gameOver){ PerformAction(); }; });
            }
            count++;
        }
        //cambio la accion que se esta realizando
        this.performingAction++;
    }

    //funcion que verifica si algun jugador gano
    private bool IsGameOver()
    {
        bool isOver = false;
        //recorro los personajes aliados
        bool charsDead = true;
        foreach (GameObject character in allied.characters)
        {
            //busco si aun existen personajes vivos
            if (character.GetComponent<Character>().actualStat.hp > 0)
            {
                charsDead = false;
            }
        }
        //si no existen personajes vivos entonces pierde el jugador
        if (charsDead)
        {
            Message("Defeat!", 20, 7f, delegate { Loading(true, ""); });
            isOver = true;
        }
        //si aun tengo personajes vivos, verifico que los del rival
        else
        {
            //recorro los personajes enemigos
            bool enemiesDead = true;
            foreach (GameObject character in enemy.characters)
            {
                //busco si aun existen personajes vivos
                if (character.GetComponent<Character>().actualStat.hp > 0)
                {
                    enemiesDead = false;
                }
            }
            //si no existen personajes vivos entonces pierde el rival
            if (enemiesDead)
            {
                Message("Victory!", 20, 7f, delegate { Loading(true, ""); });
                isOver = true;
            }
        }
        return isOver;
    }
}