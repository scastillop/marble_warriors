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
using UnityEngine.SceneManagement;

public class GameScene : MonoBehaviour

{
    public List<GameObject> characterPrefab;
    private Team allied;
    private Team enemy;
    private Vector3[] positions;
    private Quaternion[] rotations;
    public Camera mainCamera;
    private Boolean selectingTarget;
    private ButtonAct lastButtonActPressed;
    private List<Action> actions;
    private SocketManager socketManager;
    private int performingAction;
    private List<object> performingActions;
    private List<object> objectsOnScene;

    //metodo que se ejecuta al iniciar la escena
    private void Start()
    {
        
        //intancio el arreglo de objetos en escena
        this.objectsOnScene = new List<object>();
        //seteo el panel de carga
        Loading(true, "Loading...");

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
        
        /*
        //genero Personajes (agregandolos a su equipo)
        for (int i = 0; i < 5; i++)
        {
            this.allied.AddChar(MakeChar(i));
            this.enemy.AddChar(MakeChar(i+5));
        }
        */

        //seteo el color del boton "end turn" (cuando esta desabilitado)
        ColorBlock cb2 = GameObject.Find("End Turn").GetComponent<Button>().colors;
        cb2.disabledColor = Color.Lerp(GameObject.Find("End Turn").GetComponent<Button>().colors.normalColor, Color.black, 0.2f);
        GameObject.Find("End Turn").GetComponent<Button>().colors = cb2;

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

        //seteo la funcion del boton para rendirse
        GameObject.Find("Surrender").GetComponent<Button>().onClick.AddListener(delegate { Surrender(); });

        //instancio la conexion con el servidor
        //desabilito los logs (ya que yo los voy a realizar solo si los requiero)
        HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.None;
        
        //seteo las configuraciones de reconexion
        TimeSpan miliSecForReconnect = TimeSpan.FromMilliseconds(1000);
        SocketOptions options = new SocketOptions();
        options.AutoConnect = true;
        options.Reconnection = true;
        options.ReconnectionDelay = miliSecForReconnect;
        
        //instancio la conexion con el servidor modular
        this.socketManager = new SocketManager(new Uri(PlayerPrefs.GetString("serverAddress")+"/socket.io/"), options);
        
        //seteo los eventos de error y desconexion
        this.socketManager.Socket.On(SocketIOEventTypes.Error, socketError);
        this.socketManager.Socket.On(SocketIOEventTypes.Disconnect, SocketDisconnect);

        //seteo los eventos que utilizare
        //cuando el servidor me indica que la partida ha empezado
        this.socketManager.Socket.On("GameBegin", GameBegin);
        //cuando el servidor me indica que debo actualizar el estado de los personajes
        this.socketManager.Socket.On("CharactersUpdate", UpdateCharacters);
        //cuando el servidor me indica que gané
        this.socketManager.Socket.On("Victory", Victory);
        //cuando el servidor me indica que perdí
        this.socketManager.Socket.On("Defeat", Defeat);
        //cuando el servidor me manda al intro
        this.socketManager.Socket.On("BackToIntro", BackToIntro);
        //cuando el servidor me manda a mostrar un mensaje
        this.socketManager.Socket.On("ShowMessage", ShowMessage);
        //cuando el servidor responde a las acciones que le envié
        this.socketManager.Socket.On("ActionsResponse", ActionsResponse);
        //cuando la conexion con el servidor falla
        this.socketManager.Socket.On("connect_error", ConnectionError);
        //cuando me conecto correctamente al servidor
        this.socketManager.Socket.On("connect", Connected);
        //cuando se desconecta del servidor
        this.socketManager.Socket.On("disconnect", Disconnected);
        //cambio el mensaje del panel de carga
        Loading(true, "Waiting for the opponent...");

        this.socketManager.Open();

        

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
        skillSet.Add(new Skill(1,"Basic Attack", 10, statSkill, 0, "","","",""));

        //genero las estadisticas del personaje
        Stat statChar = new Stat(100, 100, 30, 20, 30, 30, 20);
        Stat actualStatChar = new Stat(100, 100, 30, 20, 30, 30, 20);

        //instancio el personaje en pantalla
        GameObject character = Instantiate(this.characterPrefab[position], this.positions[position], this.rotations[position]);

        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
        {
            //renderer.material.shader = Shader.Find("Legacy Shaders/Transparent/Parallax Specular");
        }

        //seteo el id, nombbre, habilidades, estadisticas y posicion del personaje
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
                    //hago el boton clickeable si el costo no supera el mana actual
                    if (skill.cost <= button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.mp)
                    {
                        actButton.GetComponent<CanvasGroup>().interactable = true;
                        actButton.GetComponent<CanvasGroup>().blocksRaycasts = true;
                    }
                    else
                    {
                        actButton.GetComponent<CanvasGroup>().interactable = false;
                        actButton.GetComponent<CanvasGroup>().blocksRaycasts = false;
                    }
                }
                j++;
            }
            i++;
        }
    }

    //funcion que se ejecuta al presionar una habilidad en el menu de acciones
    private void SkillClick(Button button)
    {
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

        //si la habilidad no permite seleccionar al afectado
        if (lastButtonActPressed.skill.target.Equals("own")||lastButtonActPressed.skill.target.Equals("ownTeam"))
        {
            //si es el propio usuario de la habilidad
            if (lastButtonActPressed.skill.target.Equals("own")){
                //solidifico el personaje
                foreach (Renderer renderer in lastButtonActPressed.character.GetComponentsInChildren<Renderer>())
                {
                    renderer.material.shader = Shader.Find("Standard (Specular setup)");
                }
            //si es el equipo del usuario de la habilidad
            }else if (lastButtonActPressed.skill.target.Equals("ownTeam"))
            {
                //procedo a solidificar el equipo
                foreach (GameObject teamCharacter in this.allied.characters)
                {
                    //solidifico el personaje
                    foreach (Renderer renderer in teamCharacter.GetComponentsInChildren<Renderer>())
                    {
                        renderer.material.shader = Shader.Find("Standard (Specular setup)");
                    }
                }
            }

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
            actions.Add(new Action(lastButtonActPressed.character, lastButtonActPressed.character, lastButtonActPressed.skill));

            //solidifico los personajes dentro de unos segundos
            StartCoroutine(WaitforSolidificateCharacters(0.7f));
        }
        //si no lo es
        else
        {
            //seteo en true la fase de seleccion de objetivos
            this.selectingTarget = true;
        }
        foreach (Renderer renderer in lastButtonActPressed.character.GetComponentsInChildren<Renderer>())
        {
            renderer.material.shader = Shader.Find("Standard (Specular setup)");
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
            HandleTouch(touch.fingerId, touch.position, touch.phase);
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
                    //Debug.DrawRay(ray.origin, ray.direction * 50, Color.red, 50000000f);
                    if (Physics.Raycast(ray, out hit))
                    {
                        
                        GameObject character = hit.collider.gameObject;
                        //si el raycast golpea un personaje
                        if (character.GetComponent<Character>() && character.GetComponent<Character>().actualStat.hp > 0 && this.lastButtonActPressed.character != character)
                        {
                            //si la habilidad tiene un unico objetivo
                            if (lastButtonActPressed.skill.target.Equals("single"))
                            {
                                //solidifico el personaje
                                foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
                                {
                                    renderer.material.shader = Shader.Find("Standard (Specular setup)");
                                }
                            }
                            //si la habilidad tiene como objetivo un equipo
                            else if(lastButtonActPressed.skill.target.Equals("team"))
                            {
                                List<GameObject> teamCharacters;
                                //si es menor que 5 es el equipo aliado
                                if (character.GetComponent<Character>().position < 5)
                                {
                                    teamCharacters = this.allied.characters;
                                }
                                //si no es el equipo enemigo
                                else
                                {
                                    teamCharacters = this.enemy.characters;
                                }
                                //procedo a solidificar el equipo
                                foreach (GameObject teamCharacter in teamCharacters)
                                {
                                    //solidifico el personaje
                                    foreach (Renderer renderer in teamCharacter.GetComponentsInChildren<Renderer>())
                                    {
                                        renderer.material.shader = Shader.Find("Standard (Specular setup)");
                                    }
                                }
                            //si la habilidad tiene como objetivo 3 personajes
                            }else if (lastButtonActPressed.skill.target.Equals("multi3"))
                            {
                                List<GameObject> teamCharacters;
                                //si es menor que 5 es del equipo aliado
                                if (character.GetComponent<Character>().position < 5)
                                {
                                    teamCharacters = this.allied.characters;
                                }
                                //si no es del equipo enemigo
                                else
                                {
                                    teamCharacters = this.enemy.characters;
                                }
                                //solidifico los personajes que corresponden
                                foreach (GameObject teamCharacter in teamCharacters)
                                {
                                    if (teamCharacter.GetComponent<Character>().position == character.GetComponent<Character>().position - 1 || teamCharacter.GetComponent<Character>().position == character.GetComponent<Character>().position + 1 || teamCharacter.GetComponent<Character>().position == character.GetComponent<Character>().position)
                                    {
                                        //solidifico el personaje
                                        foreach (Renderer renderer in teamCharacter.GetComponentsInChildren<Renderer>())
                                        {
                                            renderer.material.shader = Shader.Find("Standard (Specular setup)");
                                        }
                                    }
                                }
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
                renderer.material.shader = Shader.Find("Standard (Specular setup)");
            }
            //enemigos
            foreach (Renderer renderer in this.enemy.get(i).GetComponentsInChildren<Renderer>())
            {
                renderer.material.shader = Shader.Find("Standard (Specular setup)");
            }
        }
    }

    //funcion que se ejecuta para volver solidos a los personajes
    private IEnumerator WaitforUpdateTargets(float duration)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //actualizo los personajes
        socketManager.Socket.Emit("UpdateCharacters", PlayerPrefs.GetString("email"));
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

    //funcion que termina el turno
    private void EndTurn()
    {
        //desabilitpo el boton de fin de turno
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().interactable = false;
        GameObject.Find("End Turn").GetComponent<CanvasGroup>().blocksRaycasts = false;
        //oculto el menu de acciones
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().alpha = 0f;
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().interactable = false;
        //envio mensaje de termino de turno y envio las acciones
        Message("Turn End!", 20, 1f, delegate { SendActions(); });
        //solidifico los personajes dentro de unos segundos
        StartCoroutine(WaitforSolidificateCharacters(0.7f));
        //seteo el modo de seleccion de personaje en false
        this.selectingTarget = false;
    }

    //funcion que termina el turno
    private void Surrender()
    {
        //Finalizamos el audio
        ClickSound.StopSoundBySource("Audio Source Game");
        ClickSound.StopSoundBySource("Audio Source Battle");
        //seteo pantalla de carga
        Loading(true, "Sending surrender to the server...");
        //envio la rendicion al servidor
        this.socketManager.Socket.Emit("Surrender", PlayerPrefs.GetString("email"));
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
    private void Loading(bool loading, string text)
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
    private void Message(string message, int size, float delay, UnityAction action)
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
        //Inicializo el audio de la escena
        ClickSound.PlaySoundBySource("Audio Source Game");
        //elimino personajes que pudieran estar creados de antes
        foreach(GameObject character in this.objectsOnScene)
        {
            Destroy(character);
        }
        //convierto la informacion en un arreglo relacional
        List<object> allCharacters = args[0] as List<object>;
        //procedo a guardar los personajes aliados
        MakeTeam(allCharacters[0] as List<object>, "allied");
        //y enemigos
        MakeTeam(allCharacters[1] as List<object>, "enemy");

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

        //actualizo la UI de estado de los peronajes
        UpdateCharacterMenu();

        Loading(false, "");
        Message("Turn Start!", 20, 1f, delegate { });
    }

    //funcion que se ejcuta cuando el servidor me envia informacion actualizada de los personajes
    private void UpdateCharacters(Socket socket, Packet packet, params object[] args)
    {
        //convierto la informacion en un arreglo relacional
        List<object> allCharacters = args[0] as List<object>;
        //procedo a guardar los personajes aliados
        UpdateTeam(allCharacters[0] as List<object>, "allied");
        //y enemigos
        UpdateTeam(allCharacters[1] as List<object>, "enemy");
        
        //activo los botones de los personajes
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            button.GetComponent<ButtonChar>().isActive = true;
        }

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
        //Inicializo el audio de la escena
        ClickSound.PlaySoundBySource("Audio Source Game");
        //oculto panel de carga
        Loading(false, "");
        //informo al jugador
        Message("Turn Start!", 20, 1f, delegate { });
    }

    //funcion que instancio los personajes de  un equipo
    private void MakeTeam(List<object> charactersServer, string type)
    {
        int basePosition = 0;
        if(type == "enemy")
        {
            basePosition = 5;
        }
        int count = 0;
        List<GameObject> characters = new List<GameObject>();
        foreach (Dictionary<string, object> characterServer in charactersServer)
        {
            //genero el stat del personaje
            Dictionary<string, object> s1 = characterServer["initialStat"] as Dictionary<string, object>;
            Stat initialStat = new Stat(Convert.ToInt32(s1["hp"]), Convert.ToInt32(s1["mp"]), Convert.ToInt32(s1["atk"]), Convert.ToInt32(s1["def"]), Convert.ToInt32(s1["spd"]), Convert.ToInt32(s1["mst"]), Convert.ToInt32(s1["mdf"]));
            Dictionary<string, object> s2 = characterServer["actualStat"] as Dictionary<string, object>;
            Stat actualStat = new Stat(Convert.ToInt32(s2["hp"]), Convert.ToInt32(s2["mp"]), Convert.ToInt32(s2["atk"]), Convert.ToInt32(s2["def"]), Convert.ToInt32(s2["spd"]), Convert.ToInt32(s2["mst"]), Convert.ToInt32(s2["mdf"]));

            //procedo a generar las skills
            List<Skill> skillSet = new List<Skill>();
            //genero el indice de la posicion de la skill
            int position = 0;
            foreach (Dictionary<string, object> skillServer in characterServer["skills"] as List<object>)
            {
                //genero el stat de la skill
                Dictionary<string, object> s3 = skillServer["stats"] as Dictionary<string, object>;
                Stat statSkill = new Stat(Convert.ToInt32(s3["hp"]), Convert.ToInt32(s3["mp"]), Convert.ToInt32(s3["atk"]), Convert.ToInt32(s3["def"]), Convert.ToInt32(s3["spd"]), Convert.ToInt32(s3["mst"]), Convert.ToInt32(s3["mdf"]));
                skillSet.Add(new Skill(Convert.ToInt32(skillServer["id"]), Convert.ToString(skillServer["name"]), Convert.ToInt32(skillServer["cost"]), statSkill, position, Convert.ToString(skillServer["distance"]), Convert.ToString(skillServer["type"]), Convert.ToString(skillServer["animation"]), Convert.ToString(skillServer["target"])));
                position++;
            }

            //instancio el personaje en pantalla
            GameObject character = Instantiate(this.characterPrefab[Convert.ToInt32(characterServer["index"])], this.positions[basePosition + count], this.rotations[basePosition + count]);
            //guardo el personajes en mis objetos en escena
            this.objectsOnScene.Add(character);
            //seteo el nombbre, habilidades, estadisticas y posicion del personaje
            character.GetComponent<Character>().characterName = Convert.ToString(characterServer["name"]);
            character.GetComponent<Character>().position = basePosition + count;
            character.GetComponent<Character>().initialStat = initialStat;
            character.GetComponent<Character>().actualStat = actualStat;
            character.GetComponent<Character>().skills = skillSet;
            character.GetComponent<Character>().UpdateEffects();
            //agrego el personaje a su grupo
            characters.Add(character);
            count++;
        }
        if (type == "allied")
        {
            this.allied = new Team(characters);
        }
        if (type == "enemy")
        {
            this.enemy = new Team(characters);
        }
    }

    //funcion que actualiza los personajes de un equipo
    private void UpdateTeam(List<object> charactersServer, String type)
    {
        Team team;
        if (type == "enemy")
        {
            team = this.enemy;
        }
        else
        {
            team = this.enemy;
        }
        int count = 0;
        List<GameObject> characters = new List<GameObject>();
        foreach (Dictionary<string, object> characterServer in charactersServer)
        {
            //genero el stat del personaje
            Dictionary<string, object> s2 = characterServer["actualStat"] as Dictionary<string, object>;
            Stat actualStat = new Stat(Convert.ToInt32(s2["hp"]), Convert.ToInt32(s2["mp"]), Convert.ToInt32(s2["atk"]), Convert.ToInt32(s2["def"]), Convert.ToInt32(s2["spd"]), Convert.ToInt32(s2["mst"]), Convert.ToInt32(s2["mdf"]));
            //se lo seteo a su personaje correspondiente
            team.characters[count].GetComponent<Character>().actualStat = actualStat;
            team.characters[count].GetComponent<Character>().UpdateEffects();
            count++;
        }
    }


    //funcion que se ejecuta cuando gano
    private void Victory(Socket socket, Packet packet, params object[] args)
    {
        //guardo la causa de la victoria
        string reason = args[0] as string;
        //quito el panel de carga
        Loading(false, "");

        //reproduzco la musica
        ClickSound.PlaySoundBySource("Audio Source Victory");

        //informo al jugador y lo mando a la pantalla de inicio
        if (reason != "")
        {
            //si existe alguna razon por la que ganó (rendicion o salirse del juego)
            Message("Victory! "+reason, 20, 7f, delegate { this.socketManager.Close(); SceneManager.LoadScene("Intro"); });
        }
        else
        {
            //si no solo envìo el mensaje
            Message("Victory!", 20, 7f, delegate { this.socketManager.Close(); SceneManager.LoadScene("Intro"); });
        }
        
    }

    //funcion que se ejecuta cuando pierdo
    private void Defeat(Socket socket, Packet packet, params object[] args)
    {
        //guardo la causa de la derrota
        string reason = args[0] as string;
        //quito el panel de carga
        Loading(false, "");
        
        ClickSound.PlaySoundBySource("Audio Source Lose");

        //informo al jugador y lo mando a la pantalla de inicio
        if (reason != "")
        {
            //si existe alguna razon por la que perdio (rendicion o salirse del juego)
            Message("Defeat! " + reason, 20, 7f, delegate { this.socketManager.Close(); SceneManager.LoadScene("Intro"); });
        }
        else
        {
            //si no solo envìo el mensaje
            Message("Defeat!", 20, 7f, delegate { this.socketManager.Close(); SceneManager.LoadScene("Intro"); });
        }
    }

    //funcion que se ejecuta cuando el servidor me envia las respuestas de las acciones realizadas
    private void ActionsResponse(Socket socket, Packet packet, params object[] args)
    {
        //Finalizamos el audio de la seleccion de acciones
        ClickSound.StopSoundBySource("Audio Source Game");
        //Inicializo el audio de la batalla
        ClickSound.PlaySoundBySource("Audio Source Battle");
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
            //actualizo el estado de los personajes preguntando al servidor
            StartCoroutine(WaitforUpdateTargets(2f));
            //detengo el audio de la batalla
            ClickSound.StopSoundBySource("Audio Source Battle");
        }
        //si aun existen, recorro las acciones que se estan realizando
        int count = 0;
        foreach (object actionData in this.performingActions)
        {
            //si la accion que estoy realizando existe
            if (count == this.performingAction)
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
                //si no es necesario moverme
                if (Convert.ToString(action["distance"]).Equals("range")){
                    //la posicion de destino es igual a la de origen
                    targetPosition = owner.transform.position;
                }
                //obtengo el estado del owner una vez realizada la accion
                Dictionary<string, object> ownerStatMap = action["ownerStat"] as Dictionary<string, object>;
                Stat ownerStat = new Stat(Convert.ToInt32(ownerStatMap["hp"]), Convert.ToInt32(ownerStatMap["mp"]), Convert.ToInt32(ownerStatMap["atk"]), Convert.ToInt32(ownerStatMap["def"]), Convert.ToInt32(ownerStatMap["spd"]), Convert.ToInt32(ownerStatMap["mst"]), Convert.ToInt32(ownerStatMap["mdf"]));
                //realizo la accion
                owner.GetComponent<Character>().PerformAction(Convert.ToInt32(action["projectileIndex"]), Convert.ToString(action["animation"]), affected.transform.position, targetPosition, delegate { UpdateTargets(action["targets"] as List<object>, Convert.ToInt32(action["effectIndex"])); owner.GetComponent<Character>().SetStat(ownerStat, -1); UpdateCharacterMenu();}, delegate { PerformAction(); });
            }
            count++;
        }
        //cambio la accion que se esta realizando
        this.performingAction++;
    }

    //funcion que actualiza el estado de grupo de objetivos
    private void UpdateTargets(List<object> targets, int effectIndex)
    {
        //recorro la lista de objetivos
        foreach (object targetData in targets)
        {
            //casteo la data
            Dictionary<string, object>  target = targetData as Dictionary<string, object>;
            //identifico al affected
            GameObject affected;
            //si el affected es < 5 quiere decir que es un aliado
            if (Convert.ToInt32(target["affected"]) < 5)
            {
                affected = allied.characters[Convert.ToInt32(target["affected"])];
            }
            //si no, es un enemigo
            else
            {
                affected = enemy.characters[Convert.ToInt32(target["affected"]) - 5];

            }
            //obtengo el estado del afectado una vez realizada la accion
            Dictionary<string, object> affectedStatMap = target["affectedStat"] as Dictionary<string, object>;
            Stat affectedStat = new Stat(Convert.ToInt32(affectedStatMap["hp"]), Convert.ToInt32(affectedStatMap["mp"]), Convert.ToInt32(affectedStatMap["atk"]), Convert.ToInt32(affectedStatMap["def"]), Convert.ToInt32(affectedStatMap["spd"]), Convert.ToInt32(affectedStatMap["mst"]), Convert.ToInt32(affectedStatMap["mdf"]));
            //actualizo el estado del personaje
            affected.GetComponent<Character>().SetStat(affectedStat, effectIndex);

        }
        
    }

    //funcion que se ejecuta cuando el servidor me envia al intro
    private void BackToIntro(Socket socket, Packet packet, params object[] args)
    {
        this.socketManager.Close(); SceneManager.LoadScene("Intro");
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
        //informo que estoy listo para comenzar la partida identificandome
        socketManager.Socket.Emit("ReadyToBegin", PlayerPrefs.GetString("email"));
    }

    //funcion que se ejecuta cuando el servidor me envia un mensaje para mostrar
    private void ShowMessage(Socket socket, Packet packet, params object[] args)
    {
        string message = args[0] as string;
        Message(message, 20, 4f, delegate { });
    }
}